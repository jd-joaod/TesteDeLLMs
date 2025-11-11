namespace TesteDeLLMs_MVC.Models
{
    public record ChatTurn(string Role, string Content, string? Html = null);
    public static class ChatSession
    {
        public static List<ChatTurn> Get(HttpContext httpContext, string key)
        {
            var json = httpContext.Session.GetString(key);
            return string.IsNullOrEmpty(json)
                ? new List<ChatTurn>()
                : System.Text.Json.JsonSerializer.Deserialize<List<ChatTurn>>(json)!;
        }

        public static void Save(HttpContext httpContext, string key, List<ChatTurn> turns)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(turns);
            httpContext.Session.SetString(key, json);
        }

        public static void Clear(HttpContext httpContext, string key)
            => httpContext.Session.Remove(key);
    }
}
