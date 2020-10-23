using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;

namespace UrlShortener.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            ViewBag.ShortUrl = "";
            return View();
        }

        public IActionResult ShowShortUrl(URLModel model)
        {
            return View(model);
        }

        // TODO: Добавить страницу с информацией о программе

        public IActionResult About()
        {
            return View();
        }
    }
}