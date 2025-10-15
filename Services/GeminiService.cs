using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Text;
using System.Text.Json;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Services
{
    public class GeminiService
    {
        private readonly string _model;
        private readonly Client _client;

        public GeminiService(string apiKey, string model)
        {
            _client = new Client(apiKey: apiKey);
            _model = model;
        }

        public async Task<string> GetResponseAsync(string userMessage, IReadOnlyList<ChatTurn>? history = null, string? runningSummary = null, int recencyBuffer = 4)
        {
            var contents = new List<Content>();

            //// Compressed summary of earlier turns
            //if (!string.IsNullOrWhiteSpace(runningSummary))
            //{
            //    contents.Add(new Content
            //    {
            //        Role = "user",
            //        Parts = new List<Part> { new Part { Text = $"Conversation summary (for context): {runningSummary}" } }
            //    });
            //}

            // Recency buffer from history (Last N items)
            if (history is { Count: > 0 })
            {
                int start = Math.Max(0, history.Count - recencyBuffer);
                for (int i = start; i < history.Count; i++)
                {
                    var t = history[i];
                    contents.Add(new Content{
                        Role = t.Role == "model" ? "model" : "user",
                        Parts = new List<Part> { new Part { Text = t.Content } }
                    });
                }
            }

            // New user message
            contents.Add(new Content{
                Role = "user",
                Parts = new List<Part> { new Part { Text = userMessage } }
            });

            // Call the API
            var result = await _client.Models.GenerateContentAsync(
                model: _model,
                contents: contents
            );

            return result.Candidates[0].Content.Parts[0].Text ?? "No response...";
        }
    }
}
