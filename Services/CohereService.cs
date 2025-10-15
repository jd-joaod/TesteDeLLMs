using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Services
{
    public class CohereService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey;
        private readonly string _model;


        public CohereService(string apiKey, string model)
        {
            _apiKey = apiKey;
            _model = model;

            _http.BaseAddress = new Uri("https://api.cohere.ai/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetResponseAsync(string userMessage, IReadOnlyList<ChatTurn>? history = null, string? runningSummary = null, int recencyBuffer = 4)
        {
            var messages = new List<object>();

            // map history → cohere message blocks
            if (history is { Count: > 0 })
            {
                int start = Math.Max(0, history.Count - recencyBuffer);
                for (int i = start; i < history.Count; i++)
                {
                    var t = history[i];
                    if (t.Role == "user")
                    {
                        messages.Add(new {
                            role = "user",
                            content = new[] { new { type = "text", text = t.Content } }
                        });
                    }
                    else if (t.Role == "assistant")
                    {
                        messages.Add(new {
                            role = "assistant",
                            content = new[] { new { type = "text", text = t.Content } }
                        });
                    }
                }
            }

            messages.Add(new {
                role = "user",
                content = new[] { new { type = "text", text = userMessage } }
            });

            var payload = new { model = _model, messages = messages };

            using var req = new HttpRequestMessage(HttpMethod.Post, "v2/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Cohere error {(int)res.StatusCode}: {body}");
            }

            using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var parts) &&
                parts.ValueKind == JsonValueKind.Array &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textPart))
            {
                return textPart.GetString() ?? "";
            }

            if (doc.RootElement.TryGetProperty("text", out var legacyText))
                return legacyText.GetString() ?? "";

            return "No response...";
        }
    }
}
