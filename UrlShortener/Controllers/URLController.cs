using System;
using System.Collections.Generic;
using System.Text;
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

        public IActionResult GetFullUrl(string shortUrl)
        {
            if (shortUrl != null)
            {
                if (CheckCountOfRequestForRecord(shortUrl) && CheckCountOfRequestForHash(shortUrl))
                {
                    string fullUrlString = _db.StringGet(shortUrl);
                    string fullUrlFromHash = GetHash(shortUrl);

                    if (fullUrlString != null && fullUrlFromHash != null && fullUrlFromHash == fullUrlString)
                    {
                        IncrementCountOfRequestForRecord(shortUrl);
                        IncrementCountOfRequestForHash(shortUrl);

                        return Redirect(fullUrlString);
                    }
                }
                else
                {
                    DeleteRecord(shortUrl);
                    DeleteHash(shortUrl);
                }
            }

            return NotFound();
        }


        public IActionResult CreateShortUrl(string fullUrl)
        {
            if (fullUrl != null)
            {
                fullUrl = fullUrl.ToLower();

                if (!fullUrl.Contains("http://") && !fullUrl.Contains("https://"))
                { 
                    fullUrl = "http://" + fullUrl;
                }
                

                string generatedUrl = "";
                
                while (true)
                {
                    generatedUrl = GenerateUrl(7);
                    
                    if ((string) _db.StringGet(generatedUrl) == null)
                        break;
                }

                //устанавливаем пару ключ-значение
                _db.StringSet(generatedUrl, fullUrl);
                
                //время жизни короткой ссылки 2 минуты
                _db.KeyExpire(generatedUrl, TimeSpan.FromMinutes(2));

                //устанавливаем счетчику переходов по ссылке нулевое значение
                SetCountOfRequestForRecordTo0(generatedUrl);
                
                //устанавливаем хэш
                SetHash(generatedUrl, fullUrl);
                
                string shortUrl = "https://" + _httplContextAccessor.HttpContext.Request.Host.Value + "/" +
                                  generatedUrl;

                //добавляем ссылки в список, который будет оображаться в истории
                AddUrlInList(shortUrl, fullUrl);

                //добавление ссылки во множества http и https ссылок
                AddUrlToSet(fullUrl);

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

            return GetAllUrlsFromList(key);
        }

        private string GenerateUrl(int urlLength)
        {
            string url = "";

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

        //Установка счетчику переходов по ссылке нулевого значения
        private void SetCountOfRequestForRecordTo0(string key)
        {
            if (key != null)
            {
                if ((string) _db.StringGet(key) != null)
                {
                    string countKey = new StringBuilder(key).Append(":count").ToString();

                    _db.StringSet(countKey, 0);
                    _db.KeyExpire(countKey, TimeSpan.FromMinutes(2));
                }
            }
        }

        /*
            Проверка числа переходов по ссылке.
            Если число переходов меньше 5, возвращаем true, иначе возвращаем false
        */
        private bool CheckCountOfRequestForRecord(string key)
        {
            if (key != null)
            {
                if ((string) _db.StringGet(key) != null)
                {
                    string countKey = new StringBuilder(key).Append(":count").ToString();
                    int count = (int) _db.StringGet(countKey);

                    if (count < 5)
                    { 
                        return true;
                    }
                }
            }

            return false;
        }

        //Увеличение значения счетчика переходов по ссылке на 1
        private void IncrementCountOfRequestForRecord(string key)
        {
            if (key != null)
            {
                if ((string) _db.StringGet(key) != null)
                {
                    string countKey = new StringBuilder(key).Append(":count").ToString();
                    
                    _db.StringIncrement(countKey);

                }
            }
        }

        //Удаление записи
        private void DeleteRecord(string key)
        {
            if (key != null)
            {
                if ((string) _db.StringGet(key) != null)
                {
                    _db.KeyDelete(key);
                }
            }
        }

        //Установка хэша
        private void SetHash(string key, string value)
        {
            if (key != null && value != null)
            {
                string hashKey = new StringBuilder(key).Append(":hash").ToString();

                if ((string)_db.HashGet(hashKey, hashKey) == null)
                {
                    _db.HashSet(hashKey, new HashEntry[]
                    {
                        new HashEntry(hashKey, value),
                        new HashEntry("count", 0)
                    });

                    _db.KeyExpire(hashKey, TimeSpan.FromMinutes(2));
                }
            }
        }

        //Получение хэша
        private string GetHash(string key)
        {
            if (key != null)
            {
                string hashKey = new StringBuilder(key).Append(":hash").ToString();

                return (string) _db.HashGet(hashKey, hashKey);
            }

            return null;
        }

        /*
            Проверка числа переходов по ссылке для хэша.
            Если число переходов меньше 5, возвращаем true, иначе возвращаем false
        */
        private bool CheckCountOfRequestForHash(string key)
        {
            if (key != null)
            {
                string hashKey = new StringBuilder(key).Append(":hash").ToString();
                var a1s = _db.HashGetAll(hashKey);

                if ((string)_db.HashGet(hashKey, hashKey) != null)
                {
                    if (_db.HashExists(hashKey, "count"))                
                    {
                        int count = (int) _db.HashGet(hashKey, "count");

                        if (count < 5)
                        { 
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        //Увеличение значения счетчика переходов по ссылке для хеша на 1
        private void IncrementCountOfRequestForHash(string key)
        {
            if (key != null)
            {
                string hashKey = new StringBuilder(key).Append(":hash").ToString();

                if ((string)_db.HashGet(hashKey, hashKey) != null)
                {    
                    if (_db.HashExists(hashKey, "count"))                
                    {
                        _db.HashIncrement(hashKey, "count");
                    }
                }
            }
        }

        //Удаление хеша
        private void DeleteHash(string key)
        {
            if (key != null)
            {
                string hashKey = new StringBuilder(key).Append(":hash").ToString();

                if ((string)_db.HashGet(hashKey, hashKey) != null)
                {                    
                    _db.KeyDelete(hashKey);
                }
            }
        }

        private void AddUrlInList(string shortUrl, string fullUrl)
        {
            if (shortUrl != null && fullUrl != null)
            {
                string key = new StringBuilder(DateTime.Now.Day)
                                .Append(".")
                                .Append(DateTime.Now.Month)
                                .ToString();

                _db.ListRightPush(key, shortUrl+" "+fullUrl);
                _db.KeyExpire(key, TimeSpan.FromDays(1));
            }
        }
        
        private List<string> GetAllUrlsFromList(string key)
        {
            List<string> urls = new List<string>();

            if (key != null)
            {
                var urlsFromList = _db.ListRange(key, 0, -1);
                foreach (var url in urlsFromList)
                {
                    urls.Add(url.ToString());
                }
            }

            return urls;
        }

        private void AddUrlToSet(string url)
        {
            if (url != null)
            {
                if (url.Substring(0, 5).ToLower() == "https")
                {
                    _db.SetAdd("https", url);
                    _db.KeyExpire("https", TimeSpan.FromMinutes(30));
                    
                    return;
                }

                if (url.Substring(0, 4).ToLower() == "http")
                {
                    _db.SetAdd("http", url);
                    _db.KeyExpire("http", TimeSpan.FromDays(30));

                    return;
                }
            }
        }

        public List<string> GetHttpUrlsList()
        {
            var urls = new List<string>();

            var httpUrls = _db.SetMembers("http");
            
            foreach (var url in httpUrls)
            {
                urls.Add(url.ToString());
            }

            return urls;
        }

        public List<string> GetHttpsUrlsList()
        {
            var urls = new List<string>();

            var httpsUrls = _db.SetMembers("https");
            
            foreach (var url in httpsUrls)
            {
                urls.Add(url.ToString());
            }

            return urls;
        }
    }
}