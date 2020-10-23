using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using UrlShortener.Models;

namespace UrlShortener.Controllers
{
    public class URLController : Controller
    {
        private readonly IHttpContextAccessor _httplContextAccessor;
        private ConnectionMultiplexer _connection;
        IDatabase _db;

        public URLController(IHttpContextAccessor httpContextAccessor)
        {
            _httplContextAccessor = httpContextAccessor;
            _connection = ConnectionMultiplexer.Connect("localhost");
            _db = _connection.GetDatabase();
        }

        // TODO: Доделать метод
        public IActionResult GetFullUrl(string? shortUrl)
        {
            if (shortUrl != null)
            {
                string fullUrl = _db.StringGet(shortUrl);
                if (fullUrl != null)
                {
                    return Redirect(fullUrl);
                }
            }

            return NotFound();
        }


        public IActionResult CreateShortUrl(string? fullUrl)
        {
            if (fullUrl != null)
            {
                fullUrl = fullUrl.ToLower();

                if (!fullUrl.Contains("http://") && !fullUrl.Contains("https://"))
                    fullUrl = "http:\\\\" + fullUrl;

                string generatedUrl = "";
                
                while (true)
                {
                    generatedUrl = GenerateUrl(7);
                    if ((string) _db.StringGet(generatedUrl) == null)
                        break;
                }

                _db.StringSet(generatedUrl, fullUrl);
                
                string shortUrl = "https://" + _httplContextAccessor.HttpContext.Request.Host.Value + "/" +
                                  generatedUrl;

                return RedirectToAction("ShowShortUrl", "Home", new URLModel {FullUrl = fullUrl, ShortUrl = shortUrl});
            }

            return NotFound();
        }

        private string GenerateUrl(int urlLength)
        {
            string url = "";
            //url += _httplContextAccessor.HttpContext.Request.Host.Value + "/";

            Guid guid = Guid.NewGuid();
            string guidString = Convert.ToBase64String(guid.ToByteArray());
            guidString = guidString.Replace("+", "");
            guidString = guidString.Replace("=", "");

            for (int i = 0; i < urlLength; i++)
            {
                Random rand = new Random();
                url += guidString[rand.Next(0, guidString.Length)];
            }

            return url;
        }
    }
}