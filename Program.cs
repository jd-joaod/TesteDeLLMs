using dotenv.net;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI.Embeddings;
using TesteDeLLMs_MVC.Models;
using TesteDeLLMs_MVC.Services;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var geminiKey = builder.Configuration["Gemini:APIKey"];
var geminiModel = builder.Configuration["Gemini:Model"];
var openAIKey = builder.Configuration["OpenAI:APIKey"];
var openAIModel = builder.Configuration["OpenAI:Model"];
var cohereKey = builder.Configuration["Cohere:ApiKey"];
var cohereModel = builder.Configuration["Cohere:Model"];
var claudeKey = builder.Configuration["Claude:ApiKey"];
var claudeModel = builder.Configuration["Claude:Model"];
var weatherApiKey = builder.Configuration["WeatherApi:ApiKey"];
var openWeatherMapApiKey = builder.Configuration["OpenWeatherMap:ApiKey"];

//var hostedServers = builder.Configuration
//    .GetSection("HostedMcpServers")
//    .Get<List<HostedMcpServer>>() ?? new();
//builder.Services.AddSingleton(hostedServers);

// Add services to the container.
builder.Services.AddSingleton(new OpenAIService(openAIKey, openAIModel));
builder.Services.AddSingleton(new GeminiService(geminiKey, geminiModel));
builder.Services.AddSingleton(new CohereService(cohereKey, cohereModel));
builder.Services.AddSingleton(new ClaudeService(claudeKey, claudeModel));

builder.Services.AddSingleton(new OpenAIResponsesService(openAIKey!, openAIModel!));

var weatherApi = builder.Configuration.GetSection("HostedMcp:WeatherApi").Get<HostedMcpServer>()!;
var openWeatherMap = builder.Configuration.GetSection("HostedMcp:OpenWeatherMap").Get<HostedMcpServer>()!;
builder.Services.AddSingleton<HostedMcpServer>(weatherApi);
builder.Services.AddSingleton<HostedMcpServer>(openWeatherMap);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Home}/{id?}"
);

app.Run();