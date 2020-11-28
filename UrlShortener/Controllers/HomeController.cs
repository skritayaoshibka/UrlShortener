using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;

namespace UrlShortener.Controllers
{
    public class HomeController : Controller
    {
        private URLController urlController;
        public HomeController(IHttpContextAccessor httpContextAccessor)
        {
            urlController = new URLController(httpContextAccessor);
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

        public IActionResult About()
        {
            return View();
        }

        public IActionResult History()
        {
            return View(urlController.GetHistory());
        }

        public IActionResult HttpAndHttps()
        {
            var vm = new HttpAndHttpsViewModel();
            vm.HttpUrls = urlController.GetHttpUrlsList();
            vm.HttpsUrls = urlController.GetHttpsUrlsList();

            return View(vm);
        } 
    }
}