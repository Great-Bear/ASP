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
using Microsoft.Azure.Cosmos;
using System.ComponentModel.DataAnnotations;

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
            String endpoint = @"https://api.cognitive.microsofttranslator.com";
            String key = "8eeadf9d8a7040f3920fa56c6abbff1b";
            String path = "/translate?api-version=3.0&from=en&to=uk&to=ru";
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
            foreach(var obj in json)
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
            return Content("8eeadf9d8a7040f3920fa56c6abbff1b");
        }
       
        #region config
        String EndpointUri = "https://cosmosbdazure.documents.azure.com:443/";
        String PrimaryKey = "P5YORmwhxdQ7dCAwONUeWcejzi6IkqcOl3fFde8rBi3v77bBUKS2J4cFCcvZLrzmMdHF82lzx05AyeVSmLZfUw==";
        #endregion
        private CosmosClient cosmosClient;
        private Database database;
        private Container container;
        private String databaseId = "Opinion";
        private String containerId = "opinionContainer";

        public async Task<IActionResult> Cosmos()
        {
            String errorMessage = String.Empty;
            try
            {
                cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                database = cosmosClient.GetDatabase(databaseId);  // await .CreateDatabaseIfNotExistsAsync(databaseId);
                container = database.GetContainer(containerId);
                // content = "OK ";
                var sqlQueryText = "SELECT * FROM c ";
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<UserOpinion> queryResultSetIterator = 
                    container.GetItemQueryIterator<UserOpinion>(queryDefinition);
                List<UserOpinion> opinionsGeneral = new List<UserOpinion>();
                List<UserOpinion> opinionsSQL = new List<UserOpinion>();
                List<UserOpinion> opinionsNoSQL = new List<UserOpinion>();
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<UserOpinion> currentResultSet = 
                        await queryResultSetIterator.ReadNextAsync();
                    foreach (UserOpinion opinion in currentResultSet)
                    {
                        if(opinion.group == 0)
                        {
                             opinionsGeneral.Add(opinion);
                        }
                        else if(opinion.group == 1)
                        {
                            opinionsNoSQL.Add(opinion);
                        }
                        else if(opinion.group == -1)
                        {
                            opinionsSQL.Add(opinion);
                        }
                        else 
                        {
                            opinionsGeneral.Add(opinion);
                        }                  
                    }
                }
                ViewData["opinionsGeneral"] = opinionsGeneral.ToArray();
                ViewData["opinionsNoSQL"] = opinionsNoSQL.ToArray();
                ViewData["opinionsSQL"] = opinionsSQL.ToArray();
               
            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                errorMessage = String.Format("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                errorMessage = String.Format("Error: {0}", e);
            }

            ViewData["errorMessage"] = errorMessage;

            return View();  //  Content(content);
        }

        [HttpPost]
        public async Task<IActionResult> CosmosOpinion(UserOpinion opinion)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            database = cosmosClient.GetDatabase(databaseId);  // await .CreateDatabaseIfNotExistsAsync(databaseId);
            container = database.GetContainer(containerId);
            // content = "OK ";
            var sqlQueryText = $"SELECT * FROM c WHERE c.comment = \"{opinion.comment}\"";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<UserOpinion> queryResultSetIterator =
                container.GetItemQueryIterator<UserOpinion>(queryDefinition);          
            if (queryResultSetIterator.ReadNextAsync().Result.Count > 1)
            {
                return Content("Такой комментарий уже существует");
            }


               
           if(opinion.comment.Length == 0)
            {
                return Content("Поле не может быть пустым");
            }
            String errorMessage = String.Empty;
            opinion.id = Guid.NewGuid().ToString();
            try
            {
                cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                database = cosmosClient.GetDatabase(databaseId);  // await .CreateDatabaseIfNotExistsAsync(databaseId);
                container = database.GetContainer(containerId);
                ItemResponse<UserOpinion> result =
                    await container.CreateItemAsync(opinion);

                return Content(result.StatusCode.ToString() + " " + 
                    result.Resource);
            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                errorMessage = String.Format("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                errorMessage = String.Format("Error: {0}", e);

            }
            return Content(errorMessage);
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

    public class UserOpinion
    {
        public String id { get; set; }
        public DateTime date { get; set; } = DateTime.Now;
        [Required]
        [Display(Name = "Название")]
        public String comment { get; set; }
        public int? group { get; set; }
        public override string ToString()
        {
            return String.Format("{0}: {1}", comment,  id);
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