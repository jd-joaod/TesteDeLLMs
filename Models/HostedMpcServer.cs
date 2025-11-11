namespace TesteDeLLMs_MVC.Models
{
    public record HostedMcpServer(
        string Label,
        string ServerUrl,
        string[] AllowedTools
    );
}
