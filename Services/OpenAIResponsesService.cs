using Google.GenAI.Types;
using Microsoft.AspNetCore.Hosting.Server;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Services
{
    public class OpenAIResponsesService
    {
        private readonly HttpClient _http = new() { BaseAddress = new Uri("https://api.openai.com/") };
        private readonly string _model;

        public OpenAIResponsesService(string apiKey, string model)
        {
            _model = model; // e.g., "gpt-5.1"
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> AskWithHostedMcpAsync(
            string userMessage, IReadOnlyList<ChatTurn>? history, IEnumerable<HostedMcpServer> servers)
        {
            var messages = new List<object>();
            if (history is { Count: > 0 })
            {
                foreach (var t in history)
                    messages.Add(new { role = t.Role == "assistant" ? "assistant" : "user", content = t.Content });
            }
            messages.Add(new { role = "user", content = userMessage });

            foreach (var s in servers)
                Console.WriteLine($"DEBUG server -> Label={s.Label}  Url={s.ServerUrl}");

            var mcpTools = servers.Select(s => new
            {
                type = "mcp",
                server_label = s.Label,
                server_url = s.ServerUrl,        // <-- your SSE URL
                allowed_tools = s.AllowedTools,   // keep tight while testing
                require_approval = "never"
            }).ToArray();

            var payload = new
            {
                model = _model,
                input = messages,
                tools = mcpTools
                // stream = true // add later once the basic path works
            };

            var json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses")
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };

            Console.WriteLine($"DEBUG server -> request: {req}");

            var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"OpenAI error {res.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // 1) Fast path: output_text (when present)
            if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
                return ot.GetString() ?? "No response 2...";

            // 2) If there's a 'response' object, check status for troubleshooting
            if (root.TryGetProperty("response", out var respObj)
                && respObj.TryGetProperty("status", out var statusProp)
                && statusProp.ValueKind == JsonValueKind.String)
            {
                var status = statusProp.GetString();
                if (status == "requires_action")
                {
                    var ra = respObj.TryGetProperty("required_action", out var raObj) ? raObj.ToString() : "(none)";
                    return $"Run requires action/approval; tool call didn't complete. Details: {ra}";
                }
            }

            // 3) Fallback: scan output[].content[].text
            if (root.TryGetProperty("output", out var outputArr) && outputArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in outputArr.EnumerateArray())
                {
                    if (item.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var part in contentArr.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                                return textProp.GetString() ?? "No response 3...";
                        }
                    }
                }
            }

            return "No response 1...";
        }
    }
}
