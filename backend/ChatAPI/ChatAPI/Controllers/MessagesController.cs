using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using ChatAPI.Data;
using ChatAPI.Models;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore.Storage.Json;



namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MessagesController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Get - api/Messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
        {
            return await _context.Messages.OrderBy(m => m.Timestamp).ToListAsync();
        }

        // Post - api/Messages
        [HttpPost]
        public async Task<ActionResult<Message>> PostMessage(Message message)
        {
            string apiKey = _configuration.GetValue<string>("AIServices:ApiKey");

            string model= _configuration.GetValue<string>("AIServices:Model");

            string url = $"https://api-inference.huggingface.co/models/{model}";

            // HttpClient oluştur
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Analiz edilecek cümle
            var inputText = message.Description;

            var requestBody = new
            {
                inputs = inputText
            };

            string json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);

            string result = await response.Content.ReadAsStringAsync();

             var sentimentResponse = JsonSerializer.Deserialize<List<List<SentimentResponse>>>(result);

            //var dominantSentiment = sentimentResponse
            //.OrderByDescending(r => r.Max(x=>x.score)) // Skora göre büyükten küçüğe sırala
            //.FirstOrDefault();              // İlk elemanı (en büyük skora sahip olanı) al

            //var r = dominantSentiment.First();
          var resultSentiment = sentimentResponse.First().OrderByDescending(x=>x.score).First();

            return Ok(resultSentiment);
        }
    }
}