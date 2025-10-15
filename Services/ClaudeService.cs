using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Services
{
    public class ClaudeService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey;
        private readonly string _model;


        public ClaudeService(string apiKey, string model)
        {
            _apiKey = apiKey;
            _model = model;

            _http.BaseAddress = new Uri("https://api.anthropic.com");
            _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> GetResponseAsync(string userMessage, IReadOnlyList<ChatTurn>? history = null, int maxTokens=512, string? runningSummary = null, int recencyBuffer = 4)
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

            var payload = new { model = _model, messages = messages, max_tokens = maxTokens };

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Anthropic error {(int)res.StatusCode}: {body}");
            }

            using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("content", out var contentArr) &&
                contentArr.ValueKind == JsonValueKind.Array &&
                contentArr.GetArrayLength() > 0)
            {
                // Find first text block
                for (int i = 0; i < contentArr.GetArrayLength(); i++)
                {
                    var block = contentArr[i];
                    if (block.TryGetProperty("type", out var typ) && typ.GetString() == "text" &&
                        block.TryGetProperty("text", out var txt))
                    {
                        return txt.GetString() ?? "";
                    }
                }
            }

            return "";
        }
    }
}
