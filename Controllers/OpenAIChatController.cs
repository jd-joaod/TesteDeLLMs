using AngleSharp.Io;
using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using TesteDeLLMs_MVC.Models;
using TesteDeLLMs_MVC.Services;

namespace TesteDeLLMs_MVC.Controllers
{
    public class OpenAIChatController : Controller
    {
        private readonly string openAIKey = "ChatHistory:openai:gpt-4o-mini";
        private readonly OpenAIService _openAIService;
        private readonly OpenAIResponsesService _responses;
        private readonly HostedMcpServer _weatherServer;

        public OpenAIChatController(OpenAIService openAIService, OpenAIResponsesService responsesService, HostedMcpServer weatherServer)
        {
            _openAIService = openAIService;
            _responses = responsesService;
            _weatherServer = weatherServer;
        }

        [HttpGet]
        public IActionResult OpenAIChat() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskWeatherHostedMcp(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Prompt cannot be empty");

            var history = ChatSession.Get(HttpContext, "ChatHistory:openai:gpt-4o-mini");

            var response = await _responses.AskWithHostedMcpAsync(
                userMessage,
                history,
                _weatherServer
            );

            history.Add(new ChatTurn("user", userMessage));

            // Convert assistant Markdown -> HTML and sanitize
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()// tables, strikethrough, etc.
                .Build();

            var html = Markdown.ToHtml(response, pipeline);
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedSchemes.Add("data");
            var safeHtml = sanitizer.Sanitize(html);

            // Add assistant turn
            history.Add(new ChatTurn("assistant", response, safeHtml));
            ChatSession.Save(HttpContext, "ChatHistory:openai:gpt-4o-mini", history);

            ViewBag.Prompt = userMessage;
            ViewBag.Response = response;
            ViewBag.History = history;
            return View("OpenAIChat");
        }

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

                // Add user turn
                history.Add(new ChatTurn("user", userMessage));

                // Convert assistant Markdown -> HTML and sanitize
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()// tables, strikethrough, etc.
                    .Build();

                var html = Markdown.ToHtml(response, pipeline);
                var sanitizer = new HtmlSanitizer();
                sanitizer.AllowedSchemes.Add("data");
                var safeHtml = sanitizer.Sanitize(html);

                // Add assistant turn
                history.Add(new ChatTurn("assistant", response, safeHtml));

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
