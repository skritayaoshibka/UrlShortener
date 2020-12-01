using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Redis;

namespace UrlShortener.Controllers
{
    public class HomeController : Controller
    {
        private URLController _urlController;
        public HomeController(IHttpContextAccessor httpContextAccessor)
        {
            _urlController = new URLController(httpContextAccessor);
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

        public IActionResult HistoryFromList()
        {
            return View(_urlController.GetHistoryFromList());
        }

        public IActionResult HistoryFromSortedSet()
        {
            return View(_urlController.GetHistoryFromSortedSet());
        }

        public IActionResult HttpAndHttps()
        {
            var vm = new HttpAndHttpsViewModel();
            vm.HttpUrls = _urlController.GetHttpUrlsList();
            vm.HttpsUrls = _urlController.GetHttpsUrlsList();

            return View(vm);
        } 
    }
}