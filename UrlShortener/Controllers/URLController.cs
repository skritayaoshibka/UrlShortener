using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace UrlShortener.Controllers
{
    public class URLController : Controller
    {
        private ConnectionMultiplexer redis;
        IDatabase db;

        // TODO: Доделать метод
        public IActionResult GetFullUrl(string? shortUrl)
        {
            if (shortUrl!=null)
            {
                
                return Redirect("https://www.google.com/");
            }

            return NotFound();
        }

        // TODO: Доделать метод
        public IActionResult CreateShortUrl(string? fullUrl)
        {
            if (fullUrl!=null)
            {
                
                return Redirect("Home/Index");
            }

            return NotFound();
        }
    }
}
