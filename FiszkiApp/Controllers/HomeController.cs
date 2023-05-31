using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FiszkiApp.Models;
using Microsoft.AspNetCore.OutputCaching;

namespace FiszkiApp.Controllers;

// TODO: Zmienić add subject na ekran pokazujący przedioty i ich image dir, gdzieś na górze lub z boku pokazuje sięprzycisk dodaj przedmiot
// TODO: ograniczyć wybór subjectu w dodaniu pojedynczego pytania do przedmiotów, które już figurują w bazie, możliwe, że gdzie indziej podobnie będzie przeba zrobić
// TODO: errory!!!!!!!!!!!!! jak jakikolwiek błąd w ścieżce czy czymkolwiek, to databaseConnector wywala błąd


public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

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
            if (DatabaseConnector.checkLogin(model.name, model.pwd))
            DatabaseConnector.ActiveUser = model.name;
            Console.WriteLine(model.name);
        }

        if (DatabaseConnector.ActiveUser.Equals("admin")) return RedirectToAction("Index", "Admin");
        if (DatabaseConnector.ActiveUser!=null) return RedirectToAction("Index", "Home");
        return RedirectToAction("LogIn", "Home");
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
            if (DatabaseConnector.AddSubject(model.subject, model.imagedir)) Console.WriteLine("Subject added");
            else Console.WriteLine("Subject already exists");
        }

        if (DatabaseConnector.ActiveUser.Equals("admin")) return RedirectToAction("Index", "Admin");
        if (DatabaseConnector.ActiveUser!=null) return RedirectToAction("Index", "Home");
        return RedirectToAction("LogIn", "Home");
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
            if (model.path == null) DatabaseConnector.AddSingleQuestion(model);
            else if (DatabaseConnector.AddQuestionsFromFile(model.path, model.subject)) Console.WriteLine("Dodano pytania");
            else Console.WriteLine("Coś poszło nie tak, nie dodano pytań");

        if (DatabaseConnector.ActiveUser.Equals("admin")) return RedirectToAction("Index", "Admin");
        if (DatabaseConnector.ActiveUser!=null) return RedirectToAction("Index", "Home");
        return RedirectToAction("LogIn", "Home");
    }
}
