using Microsoft.AspNetCore.Mvc;

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

        // TODO: Добавить страницу с информацией о программе

        public IActionResult About()
        {
            return View();
        }

        //[HttpPost]
        //public IActionResult Index() 
        //{

        //}

    }
}
