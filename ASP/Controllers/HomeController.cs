using DevOpsBasics.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace DevOpsBasics.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string path = "./comments.txt";

        private List<Models.Opinion> opinions;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            opinions = null;
        }

        public IActionResult Index()
        {
            if (opinions == null)
            {
                try
                {
                    using var reader = new StreamReader(path);
                    opinions = JsonSerializer
                        .Deserialize<List<Models.Opinion>>
                        (reader.ReadToEnd());

                    ViewData["Mode"] = "Read";
                }
                catch
                {
                    opinions = null;
                }
            }
            else
            {
                ViewData["Mode"] = "Repeat";
            }
            ViewData["Comments"] = opinions?.ToArray();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Translate()
        {
            String translateFrom = Request.Query["originLang"].ToString();
            if (String.IsNullOrEmpty(translateFrom))
            {
                translateFrom = "en";
            }
            String translateTo = Request.Query["translateTo"].ToString();
            if (String.IsNullOrEmpty(translateTo))
            {
                translateTo = "uk";
            }

            String endpoint = @"https://api.cognitive.microsofttranslator.com";
            String key = "8eeadf9d8a7040f3920fa56c6abbff1b";
            String path = $"/translate?api-version=3.0&from={translateFrom}&to={translateTo}";
            String region = "global";
            String txt = Request.Query["txt"].ToString();
            if(txt.Length == 0)
            {
                txt = "Hello, world!";
            }
            String body = JsonSerializer.Serialize(
                new object[] { new { Text = txt } });
            String resp;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + path);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", region);
                
                // request.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                // request.Headers.Add("Content-Length", body.Length.ToString());
                request.Content = new StringContent(
                    body, System.Text.Encoding.UTF8, "application/json");

                resp = client
                    .SendAsync(request).Result
                    .Content.ReadAsStringAsync().Result;
            }
            ViewData["resp"] = resp;
            // [{"translations":[{"text":"Привіт, народ!","to":"uk"},{"text":"Всем привет!","to":"ru"}]}]
            var json = JsonSerializer
                .Deserialize<Translations[]>(resp);
            ViewData["ukTranslation"] = "";
            ViewData["ruTranslation"] = "";
            
            foreach (var obj in json)
            {
                foreach(var trans in obj.translations)
                {
                    if (trans.to.Equals("uk"))
                        ViewData["ukTranslation"] = trans.text;
                    if (trans.to.Equals("ru"))
                        ViewData["ruTranslation"] = trans.text;
                }
            }
            return View();
        }

        public IActionResult ApiKey()
        {
            return Content("{ \"Key\": \"8eeadf9d8a7040f3920fa56c6abbff1b\"," +
                              "\"Region\": \"global\" }");
        }

        [HttpPost]
        public IActionResult UserOpinion(Models.Opinion opinion)
        {
            if (opinions == null)
            {
                ViewData["Mode"] = "Read";
                try
                {
                    using var reader = new StreamReader(path);
                    opinions = JsonSerializer
                        .Deserialize<List<Models.Opinion>>
                        (reader.ReadToEnd());
                }
                catch
                {
                    opinions = new List<Models.Opinion>();
                }
            }
            else
            {
                ViewData["Mode"] = "Repeat";
            }
            opinions.Add(opinion);
            // Serialization
            string comment = JsonSerializer.Serialize(opinions);
            using (StreamWriter writetext = new StreamWriter(path))
            {
                writetext.WriteLine(comment);
            }
            return Redirect("/");
            // return Content(comment);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    class Translation
    {
        public String text { get; set; }
        public String to { get; set; }
    }
    class Translations
    {
        public Translation[] translations { get; set; }
    }
}
/*
 * XML  <data>10</data>
 *      <x>10</x>
 *      <item prop1=10 prop2=20 />
 *      <item prop1=10 prop2=20>100</item>
 * 
 * JSON {"data":"10"}
 *      {"x":"10"} 
 *      { "item":{} }
 * 
 */