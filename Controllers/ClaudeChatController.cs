using Microsoft.AspNetCore.Mvc;
using TesteDeLLMs_MVC.Models;
using TesteDeLLMs_MVC.Services;

namespace TesteDeLLMs_MVC.Controllers
{
    public class ClaudeChatController : Controller
    {
        private readonly ClaudeService _claudeService;
        private readonly string claudeKey = "ChatHistory:claude:claude-3-7-sonnet-latest";

        public ClaudeChatController(ClaudeService claudeService)
        {
            _claudeService = claudeService;
        }

        [HttpGet]
        public IActionResult ClaudeChat() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskClaude(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Prompt cannot be empty");

            try
            {
                var history = ChatSession.Get(HttpContext, claudeKey);

                var response = await _claudeService.GetResponseAsync(userMessage, history);

                history.Add(new ChatTurn("user", userMessage));
                history.Add(new ChatTurn("assistant", response));
                ChatSession.Save(HttpContext, claudeKey, history);

                ViewBag.Prompt = userMessage;
                ViewBag.Response = response;
                ViewBag.History = history;

                return View("ClaudeChat");
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
            ChatSession.Clear(HttpContext, claudeKey);
            return RedirectToAction(nameof(ClaudeChat));
        }
    }
}
