using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using TesteDeLLMs_MVC.Services;
using TesteDeLLMs_MVC.Models;

namespace TesteDeLLMs_MVC.Controllers
{
    public class OpenAIChatController : Controller
    {
        private readonly OpenAIService _openAIService;
        private readonly string openAIKey = "ChatHistory:openai:gpt-4o-mini";

        public OpenAIChatController(OpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        [HttpGet]
        public IActionResult OpenAIChat() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskOpenAI(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Prompt cannot be empty");
            
            try
            {
                var history = ChatSession.Get(HttpContext, openAIKey);

                var response = await _openAIService.GetResponseAsync(userMessage, history);

                history.Add(new ChatTurn("user", userMessage));
                history.Add(new ChatTurn("assistant", response));
                ChatSession.Save(HttpContext, openAIKey, history);

                ViewBag.Prompt = userMessage;
                ViewBag.Response = response;
                ViewBag.History = history;

                return View("OpenAIChat");
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
            ChatSession.Clear(HttpContext, openAIKey);
            return RedirectToAction(nameof(OpenAIChat));
        }
    }
}
