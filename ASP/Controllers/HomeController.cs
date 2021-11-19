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
using Newtonsoft.Json.Linq;

namespace DevOpsBasics.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string path = "./comments.txt";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<string> comments;
            try
            {
                comments = new List<string>();

                using var reader = new StreamReader(path);
                var opinions = JsonSerializer
                    .Deserialize<List<Models.Opinion>>
                    (reader.ReadToEnd());

                ViewData["CommentsObjects"] = opinions;

                foreach (var specificOpinions in opinions)
                {
                    comments.Add(specificOpinions.Comment);
                }
            }

            catch
            {
                comments = null;
            }
            ViewData["Comments"] = comments?.ToArray();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UserOpinion(Models.Opinion opinion)
        {
            List<Models.Opinion> opinions;
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
            opinion.Moment = DateTime.Now;
       
          
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