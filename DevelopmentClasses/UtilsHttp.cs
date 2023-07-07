using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DevelopmentClasses
{
    /// <summary>
    /// Класс, используемый для работы с сетью. Отвечает за отправку http-запросов, получение и парсинг http-ответов, а также за сохранение данных в .json файл.
    /// </summary>
    public class UtilsHttp
    {
        private static readonly int NUMBER_OF_CONCURRENT_REQUESTS = 10; // такое число http-запросов будет посылаться параллельно

        /// <summary>
        /// Начальная дата (левая граница временного отрезка). Дублирует соответствующее свойство основного класса, чтобы не прокидывать в метод DownloadData и GetPageAsync.
        /// </summary>
        public string LastUpdateFrom { get; private set; }

        /// <summary>
        /// Конечная дата (правая граница временного отрезка). Дублирует соответствующее свойство основного класса, чтобы не прокидывать в метод DownloadData и GetPageAsync.
        /// </summary>
        public string LastUpdateTo { get; private set; }

        /// <summary>
        /// Конструктор класса.
        /// </summary>
        /// <param name="lastUpdateFrom"></param>
        /// <param name="lastUpdateTo"></param>
        public UtilsHttp(string lastUpdateFrom, string lastUpdateTo)
        {
            LastUpdateFrom = lastUpdateFrom;
            LastUpdateTo = lastUpdateTo;
        }

        /// <summary>
        /// Асинхронный метод, скачивающий данные из сети и сохраняющий их в .json файле.
        /// </summary>
        /// <param name="fileName">Путь к файлу в файловой системе, в который записываются скачанные данные.</param>
        public async Task DownloadData(string fileName)
        {
            try
            {
                using (StreamWriter streamWriter = File.CreateText(fileName))
                {
                    int iteration = 0;
                    bool isLastPage = false;
                    streamWriter.Write("{\"content\":\r\n[");
                    while (!isLastPage)
                    {
                        var tasks = new List<Task<string>>();
                        for (int i = 0; i < NUMBER_OF_CONCURRENT_REQUESTS; ++i)
                        {
                            Task<string> task = GetPageAsync(page: iteration * NUMBER_OF_CONCURRENT_REQUESTS + i);
                            tasks.Add(Task.Run(() => task));
                        }
                        try
                        {
                            await Task.WhenAll(tasks);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Ошибка получения данных с сайта", e);
                        }
                        for (int i = 0; i < NUMBER_OF_CONCURRENT_REQUESTS; ++i)
                        {
                            string jsonStr = tasks[i].Result;
                            tasks[i].Dispose();
                            if (!string.IsNullOrEmpty(jsonStr))
                            {
                                JObject json = JObject.Parse(jsonStr);
                                string contentValue = json.GetValue("content").ToString();
                                streamWriter.Write(contentValue.Substring(1, contentValue.Length - 2));
                                if (bool.Parse(json.GetValue("last").ToString()) == true)
                                {
                                    isLastPage = true;
                                    break;
                                }
                                else
                                {
                                    streamWriter.WriteLine(",");
                                }
                            }
                        }
                        if (isLastPage != true)
                        {
                            Console.WriteLine("Получено " + NUMBER_OF_CONCURRENT_REQUESTS * 100 * (++iteration) + " элементов");
                        }
                    }
                    streamWriter.WriteLine("]\r\n}");
                    streamWriter.Flush();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Ошибка скачивания данных", e);
            }
        }

        /// <summary>
        /// Асинхронный метод, выполняющий http-запрос к серверу и получающий http-ответ.
        /// </summary>
        /// <param name="page">Номер страницы, которая запрашивается у сервера.</param>
        /// <returns>Асинхронная операция, возвращающая ответ сервера. Эту задачу можно выполнять параллельно с другими http-запросами.</returns>
        private async Task<String> GetPageAsync(int page)
        {
            using (HttpClient clnt = new HttpClient())
            {
                clnt.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
                var response = await clnt.GetAsync($"https://bus.gov.ru/public-rest/api/epbs/fap?lastUpdateFrom=" + LastUpdateFrom + "&lastUpdateTo=" + LastUpdateTo + "&page=" + page + "&size=100");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }
            }
            return null;
        }
    }
}
