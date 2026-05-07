using SecondBrain.Core.Interfaces;
using SecondBrain.Host;
using SecondBrain.Integrations.Gemini;
using SecondBrain.Telegram;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// --- Настройка Telegram ---
var botToken = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken))
{
    throw new InvalidOperationException("Telegram:BotToken is not configured in appsettings.json or User Secrets.");
}

builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        TelegramBotClientOptions options = new(botToken);
        return new TelegramBotClient(options, httpClient);
    });

// --- Настройка Gemini ---
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// Регистрируем классификатор (HttpClient больше не нужен, используется официальный SDK)
builder.Services.AddSingleton<IMessageClassifier, GeminiMessageClassifier>();

// --- Регистрация сервисов ---
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
