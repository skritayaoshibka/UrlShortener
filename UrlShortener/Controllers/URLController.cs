using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Redis;

namespace UrlShortener.Controllers
{
    public class URLController : Controller
    {
        private readonly IHttpContextAccessor _httplContextAccessor;
        private RedisCommands _redis;

        public URLController(IHttpContextAccessor httpContextAccessor)
        {
            _httplContextAccessor = httpContextAccessor;
            _redis = RedisCommands.GetRedisObj();
        }

        public IActionResult GetFullUrl(string shortUrl)
        {
            if (shortUrl != null)
            {
                if (_redis.CheckCountOfRequestForRecordLessThanFive(shortUrl) && _redis.CheckCountOfRequestForHashLessThanFive(shortUrl))
                {
                    string fullUrlString = _redis.GetString(shortUrl);
                    string fullUrlFromHash = _redis.GetHash(shortUrl);

                    if (fullUrlString != null && fullUrlFromHash != null && fullUrlFromHash == fullUrlString)
                    {
                        _redis.IncrementCountOfRequestForRecord(shortUrl);
                        _redis.IncrementCountOfRequestForHash(shortUrl);

                        return Redirect(fullUrlString);
                    }
                }
                else
                {
                    _redis.DeleteRecord(shortUrl);
                    _redis.DeleteHash(shortUrl);
                }
            }

            return NotFound();
        }


        public IActionResult CreateShortUrl(string fullUrl)
        {
            if (fullUrl != null)
            {
                fullUrl = fullUrl.ToLower();

                if (!CheckUrlStartsWithHttpOrHttps(fullUrl))
                { 
                    fullUrl = "http://" + fullUrl;
                }

                string generatedEndOfUrl = GenerateUrl(7);
                
                string shortUrl = "https://" + _httplContextAccessor.HttpContext.Request.Host.Value + "/" +
                                  generatedEndOfUrl;

                //устанавливаем пару ключ-значение
                _redis.SetString(generatedEndOfUrl, fullUrl);
                
                //устанавливаем хэш
                _redis.SetHash(generatedEndOfUrl, fullUrl);

                //добавляем ссылки в список, который будет оображаться в истории
                _redis.AddUrlInList(shortUrl, fullUrl);

                //добавление ссылки во множества http и https ссылок
                _redis.AddUrlToSet(fullUrl);

                return RedirectToAction("ShowShortUrl", "Home", new URLModel {FullUrl = fullUrl, ShortUrl = shortUrl});
            }

            return NotFound();
        }

        public List<string> GetHistory()
        {
            string key = new StringBuilder(DateTime.Now.Day)
                                .Append(".")
                                .Append(DateTime.Now.Month)
                                .ToString();

            return  _redis.GetAllUrlsFromList(key);
        }

        private bool CheckUrlStartsWithHttpOrHttps(string url)
        {
            return ((url.Substring(0, 5).ToLower() == "https") || (url.Substring(0, 4).ToLower() == "http"));
        }

        private string GenerateUrl(int urlLength)
        {
            string url = "";

            while (true)
            {
                url = "";

                Guid guid = Guid.NewGuid();
                string guidString = Convert.ToBase64String(guid.ToByteArray());
                guidString = guidString.Replace("+", "");
                guidString = guidString.Replace("=", "");

                for (int i = 0; i < urlLength; i++)
                {
                    Random rand = new Random();
                    url += guidString[rand.Next(0, guidString.Length)];
                }

                if ((string) _redis.GetString(url) == null)
                {
                    break;
                }
            }

            return url;
        }

        public List<string> GetHttpUrlsList()
        {
            return _redis.GetHttpUrlsList();
        }

        public List<string> GetHttpsUrlsList()
        {
            return _redis.GetHttpsUrlsList();
        }
    }
}