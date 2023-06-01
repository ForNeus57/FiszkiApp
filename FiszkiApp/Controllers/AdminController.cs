using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FiszkiApp.Models;
using Microsoft.AspNetCore.OutputCaching;

namespace FiszkiApp.Controllers;
/*
 * controller dla admina
 */
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (DatabaseConnector.ActiveUser == null) return RedirectToAction("LogIn", "Home");
        return View();
    }

    /*
     * widok dodawania uzytkownikow
     */
    public IActionResult AddUserView()
    {
        if(DatabaseConnector.ActiveUser==null) return RedirectToAction("LogIn", "Home");
        return View();
    }
    
    [HttpPost]
    [OutputCache(NoStore = true)]
    public IActionResult AddUserView(User model)
    {
        if (ModelState.IsValid)
        {
            if (!DatabaseConnector.CheckLogin(model.name, model.pwd))
            {
                DatabaseConnector.AddUser(model.name, model.pwd);
                return RedirectToAction("Index", "Admin");
            }
        }
    
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}