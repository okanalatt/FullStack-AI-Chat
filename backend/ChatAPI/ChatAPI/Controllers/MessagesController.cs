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
        private readonly IConfiguration _configuration;

        // IHttpClientFactory artık Singleton olduğu için constructor'dan kaldırıldı
        public MessagesController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
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
            // 1. AI Servisi Ayarlarını Al
            string apiKey = _configuration.GetValue<string>("AIServices:ApiKey");
            string model = _configuration.GetValue<string>("AIServices:Model");
            string url = $"https://api-inference.huggingface.co/models/{model}";

            // 2. HttpClient oluştur ve Auth Header ekle
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 3. AI Servisine Gönderilecek İstek Gövdesini Hazırla
            var inputText = message.Description;
            var requestBody = new { inputs = inputText };
            string json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 4. AI Servisine İstek Gönder
            HttpResponseMessage response = await client.PostAsync(url, content);

            // KRİTİK: AI Servisi Yanıtını Kontrol Etme (Hata Ayıklama için Eklendi)
            if (!response.IsSuccessStatusCode)
            {
                // Yanıt başarılı değilse (Örn: 401 Unauthorized), ham hata içeriğini oku
                string errorContent = await response.Content.ReadAsStringAsync();

                // Hata detayını Logla (Debug Console/Render Logları)
                System.Diagnostics.Debug.WriteLine($"AI API Hatası! Durum Kodu: {response.StatusCode}. Yanıt: {errorContent}");

                // Frontend'e açıklayıcı bir hata döndür
                // Bu, Frontend'deki try-catch bloğunuza düşecektir.
                return StatusCode((int)response.StatusCode, new { error = "AI Servisi Hatası", details = errorContent });
            }

            // 5. Ham Yanıtı Oku
            string result = await response.Content.ReadAsStringAsync();

            // 6. Yanıtı Serileştir ve En Yüksek Skorlu Duyguyu Bul
            // JSON serileştirme hatası bu satırda oluşuyordu (önceki hata)
            var sentimentResponse = JsonSerializer.Deserialize<List<List<SentimentResponse>>>(result);
            var resultSentiment = sentimentResponse.First().OrderByDescending(x => x.score).First();

            // 7. Mesajı Veritabanına Kaydet (Eksik Olan Mantık Eklendi)
            message.Feeling = resultSentiment.label;
            message.Score = resultSentiment.score;
            message.Timestamp = DateTime.UtcNow; // Opsiyonel: Kontrol amacıyla eklenmiştir

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 8. Kaydedilen ve güncellenen mesajı döndür (201 Created Status)
            return CreatedAtAction(nameof(GetMessages), new { id = message.Id }, message);
        }
    }
}