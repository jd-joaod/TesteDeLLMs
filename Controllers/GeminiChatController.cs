using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using TesteDeLLMs_MVC.Services;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_API.Controllers
{
    public class GeminiChatController : Controller
    {
        private readonly GeminiService _geminiService;
        private readonly string geminiKey = "ChatHistory:gemini:gemini-2.5-flash";

        public GeminiChatController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpGet]
        public IActionResult GeminiChat() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskGemini(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Prompt cannot be empty");
            
            try
            {
                var history = ChatSession.Get(HttpContext, geminiKey);

                var response = await _geminiService.GetResponseAsync(userMessage, history);

                history.Add(new ChatTurn("user", userMessage));
                history.Add(new ChatTurn("model", response));
                ChatSession.Save(HttpContext, geminiKey, history);

                ViewBag.Prompt = userMessage;
                ViewBag.Response = response;
                ViewBag.History = history;

                return View("GeminiChat");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            ChatSession.Clear(HttpContext, geminiKey);
            return RedirectToAction(nameof(GeminiChat));
        }
    }
}
