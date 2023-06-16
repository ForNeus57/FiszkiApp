using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FiszkiApp.Models;
using Microsoft.AspNetCore.OutputCaching;

namespace FiszkiApp.Controllers;

// TODO: Zmienić add subject na ekran pokazujący przedmioty i ich image dir, gdzieś na górze lub z boku pokazuje sięprzycisk dodaj przedmiot
// TODO: ograniczyć wybór subject'u w dodaniu pojedynczego pytania do przedmiotów, które już figurują w bazie, możliwe, że gdzie indziej podobnie będzie przeba zrobić
// TODO: errory!!!!!!!!!!!!! jak jakikolwiek błąd w ścieżce czy czymkolwiek, to databaseConnector wywala błąd
// TODO: przenieść w każdej metodzie kontrolera sprawdzenie czy zalogowany na sam początek

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public static string message = "";

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    [HttpPost]
    [OutputCache(NoStore = true)]
    public IActionResult LogIn(User model)
    {
        if (ModelState.IsValid)
        {
            if (DatabaseConnector.CheckLogin(model.name, model.pwd))
                DatabaseConnector.ActiveUser = model.name;
        }

        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        if (DatabaseConnector.ActiveUser.Equals("admin")) return RedirectToAction("Index", "Admin");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Privacy()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    public IActionResult LogIn()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult LogOut()
    {
        DatabaseConnector.ActiveUser = null;
        return RedirectToAction("LogIn", "Home");
    }

    public IActionResult AddSubject()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    [HttpPost]
    [OutputCache(NoStore = true)]
    public IActionResult AddSubject(Subject model)
    {
        if (ModelState.IsValid)
        {
            if (DatabaseConnector.AddSubject(model.subject, model.imagedir))
            {
                Console.WriteLine("Subject added");
                HomeController.message = "Subject added succesfully";
            }
            else Console.WriteLine("Subject already exists");
        }
        if (DatabaseConnector.ActiveUser==null) return RedirectToAction("LogIn", "Home");
        if (DatabaseConnector.ActiveUser.Equals("admin")) return RedirectToAction("Index", "Admin");
        return RedirectToAction("Index", "Home");
        
    }

    public IActionResult AddQuestion()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    [HttpPost]
    [OutputCache(NoStore = true)]
    public IActionResult AddQuestion(Question model)
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        if (model.path == null)
        {
            DatabaseConnector.AddSingleQuestion(model);
            DatabaseConnector.Status = "Question added!";
        }
        else DatabaseConnector.Status = "Question is incorect!";

        return View();
    }

    [HttpPost]
    public ActionResult Upload(IFormFile file, string subject)
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        if (file != null && file.Length > 0)
        {
            // Zapisz plik w określonym miejscu lub przetwórz go
            string filePath = Path.Combine("Data", DatabaseConnector.ActiveUser, file.FileName);
            Directory.CreateDirectory(Path.Combine("Data", DatabaseConnector.ActiveUser));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Przetwarzaj temat lub przechowuj go razem z plikiem
            if (DatabaseConnector.AddQuestionsFromFile(filePath, subject))
            {
                DatabaseConnector.Status = "File added!";
            }
            else
            {
                DatabaseConnector.Status = "File is incorrect!";
            }

            // Wykonaj dodatkowe przetwarzanie lub przekieruj do strony sukcesu
        }

        return View("AddQuestion");
    }

    public IActionResult ChooseSubject()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    [HttpPost]
    [OutputCache(NoStore = true)]
    public IActionResult ChooseSubject(Question model)
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        if (DatabaseConnector.StartAddQuestions(model.subject, model.batch))
        {
            DatabaseConnector.ShuffleQuestions();
            return RedirectToAction("Learning", "Home");
        }

        return RedirectToAction("ChooseSubject", "Home");
    }

    [HttpPost]
    public IActionResult BatchQuestions(string batch)
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        Console.WriteLine(batch);
        if (DatabaseConnector.batches.Contains(batch))
        {
            DatabaseConnector.batch = batch;

            DatabaseConnector.addQuestionsMemory();
            
            return RedirectToAction("Questions", "Home");

        }
        
        return RedirectToAction("Learning", "Home");
    }

    [HttpPost]
    public IActionResult SubjectQuestions(string subject)
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        Console.WriteLine(subject);
        
        if (DatabaseConnector.subjects.Contains(subject))
        {
            DatabaseConnector.subject = subject;
            DatabaseConnector.getBatches();
        }
        return RedirectToAction("Learning", "Home");
    }

    public IActionResult Learning()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        // DatabaseConnector.getBatches();
        DatabaseConnector.getSubjects();
        DatabaseConnector.Ques.Clear();
        return View();
    }

    public IActionResult Questions()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        if (!Statistics.stopwatchStarted)
        {
            Statistics.batchSize = DatabaseConnector.Ques.Count;
            Statistics.skips = 0;
            Statistics.stopwatchStarted = true;
            Statistics.stopwatch.Start();
        }

        DatabaseConnector.showAnswer = false;
        if (DatabaseConnector.Ques.Count == 0)
        {
            Statistics.stopwatch.Stop();
            DatabaseConnector.AddStat();
            DatabaseConnector.subject = "";
            DatabaseConnector.batch = "";
            return RedirectToAction("Index", "Home");
        }

        DatabaseConnector.NextQuestion();

        return View("Questions");
    }

    public IActionResult QuestionsSkip()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        DatabaseConnector.showAnswer = false;
        DatabaseConnector.RemoveKnownQuestion();
        Statistics.skips += 1;
        if (DatabaseConnector.Ques.Count == 0)
        {
            Statistics.stopwatch.Stop();
            DatabaseConnector.AddStat();
            DatabaseConnector.subject = "";
            DatabaseConnector.batch = "";
            message = "Congratulations, you know everything!";
            return RedirectToAction("Index", "Home");
        }
        DatabaseConnector.NextQuestion();

        return View("Questions");
    }

    public IActionResult QuestionsAwnser()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        DatabaseConnector.showAnswer = true;
        Statistics.skips += 1;
        return View("Questions");
    }

    public IActionResult Stats()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    public IActionResult Contributions()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }
}