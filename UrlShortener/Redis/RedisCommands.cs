using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace UrlShortener.Redis
{
    public class RedisCommands
    {
        private static RedisCommands redisObj;
        private ConnectionMultiplexer _connection;
        private IDatabase _db;


        private RedisCommands()
        {
            _connection = ConnectionMultiplexer.Connect("localhost");
            _db = _connection.GetDatabase();
        }

        public static RedisCommands GetRedisObj()
        {
            if (redisObj == null)
            {
                redisObj = new RedisCommands();
            }

            return redisObj;
        }

        //Установка пары ключ-значение c временем жизни 2 минуты
        public void SetString(string key, string value)
        {
            if (key != null && value != null)
            {
                if (GetString(key) == null)
                {
                    _db.StringSet(key, value);
                    _db.KeyExpire(key, TimeSpan.FromMinutes(2));
                    SetCountOfRequestForRecordTo0(key);
                }
            }
        }

        //Получение значения по ключу
        public string GetString(string key)
        {
            if (key != null)
            {
                return _db.StringGet(key);
            }

            return null;
        }

        //Установка счетчику переходов по ссылке нулевого значения
        public void SetCountOfRequestForRecordTo0(string key)
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
        public bool CheckCountOfRequestForRecordLessThanFive(string key)
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
        public void IncrementCountOfRequestForRecord(string key)
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
        public void DeleteRecord(string key)
        {
            if (key != null)
            {
                if ((string) _db.StringGet(key) != null)
                {
                    _db.KeyDelete(key);
                }
            }
        }

        //Установка хэша с временем жизни 2 минуты
        public void SetHash(string key, string value)
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
        public string GetHash(string key)
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
        public bool CheckCountOfRequestForHashLessThanFive(string key)
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
        public void IncrementCountOfRequestForHash(string key)
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
        public void DeleteHash(string key)
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

        /*
        Добавление сокращенной и полной ссылок в список, 
        ключом для которого является сегодняшняя дата
        (список импользуется для просмотра истрории)
        */
        public void AddUrlInList(string shortUrl, string fullUrl)
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
        
        /*
        Получение сокращенной и полной ссылок из списка, 
        ключом для которого является сегодняшняя дата
        (список импользуется для просмотра истрории)
        */
        public List<string> GetAllUrlsFromList(string key)
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

        /*
        Добавление ссылки во множество
        Ссылки распределяются по двум множествам: начинающиеся с http и с https
        */
        public void AddUrlToSet(string url)
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

        //Добавление ссылки в упорядоченный список
        public void AddUrlInSortedSet(string url)
        {
            if (url != null)
            {
                _db.SortedSetAdd("history", url, _db.SortedSetRangeByRank("history").Length+1);
            }
        }

        //Получение ссылок из упорядоченного списка
        public Dictionary<string, string> GetUrlsFromSortedSet()
        {
            var urls = new Dictionary<string, string>();

            var urlsFromHistory = _db.SortedSetRangeByRank("history");

            foreach(var url in urlsFromHistory)
            {
                var score = _db.SortedSetScore("history", url).ToString();
                urls.Add(score ,url.ToString());
            }

            return urls;
        }
        
        //Получение ссылок из множества http-ссылок
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

        //Получение ссылок из множества httpы-ссылок
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