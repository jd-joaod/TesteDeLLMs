using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chat;

        public OpenAIService(string apiKey, string model)
        {
            var client = new OpenAIClient(apiKey);
            _chat = client.GetChatClient(model);
        }

        public async Task<string> GetResponseAsync(string userMessage, IReadOnlyList<ChatTurn>? history = null, string? runningSummary = null, int recencyBuffer = 4)
        {
            var messages = new List<ChatMessage>();

            //// Compressed summary of earlier turns
            //if (!string.IsNullOrWhiteSpace(runningSummary))
            //    messages.Add(new UserChatMessage($"Conversation summary (context): {runningSummary}"));

            // Recency buffer from history (Last N items)
            if (history is { Count: > 0 })
            {
                int start = Math.Max(0, history.Count - recencyBuffer);
                for (int i = start; i < history.Count; i++)
                {
                    var t = history[i];
                    if (t.Role == "user")
                        messages.Add(new UserChatMessage(t.Content));
                    else if (t.Role == "assistant")
                        messages.Add(new AssistantChatMessage(t.Content));
                }
            }

            // New user message
            messages.Add(new UserChatMessage(userMessage));

            // Call the API
            var completion = await _chat.CompleteChatAsync(messages);

            return completion.Value.Content.FirstOrDefault()?.Text ?? "No response...";
        }
    }
}