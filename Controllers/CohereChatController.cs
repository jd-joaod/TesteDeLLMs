using Microsoft.AspNetCore.Mvc;
using TesteDeLLMs_MVC.Models;
using TesteDeLLMs_MVC.Services;

namespace TesteDeLLMs_MVC.Controllers
{
    public class CohereChatController : Controller
    {
        private readonly CohereService _cohereService;
        private readonly string cohereKey = "ChatHistory:cohere:command-r7b-12-2024";

        public CohereChatController(CohereService cohereService)
        {
            _cohereService = cohereService;
        }

        [HttpGet]
        public IActionResult CohereChat() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskCohere(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Prompt cannot be empty");

            try
            {
                var history = ChatSession.Get(HttpContext, cohereKey);

                var response = await _cohereService.GetResponseAsync(userMessage, history);

                history.Add(new ChatTurn("user", userMessage));
                history.Add(new ChatTurn("assistant", response));
                ChatSession.Save(HttpContext, cohereKey, history);

                ViewBag.Prompt = userMessage;
                ViewBag.Response = response;
                ViewBag.History = history;

                return View("CohereChat");
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
            ChatSession.Clear(HttpContext, cohereKey);
            return RedirectToAction(nameof(CohereChat));
        }
    }
}
