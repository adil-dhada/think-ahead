using UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Data;

namespace UI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(ProductContext.Products);
        }

        public IActionResult Detail(int id)
        {
            var product = ProductContext.Products.FirstOrDefault(p => p.Id == id);
            if (product is null) return NotFound();
            return View(product);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string name, string description)
        {
            ProductContext.Add(name, description);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
