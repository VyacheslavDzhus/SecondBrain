using SecondBrain.Host;
using SecondBrain.Telegram;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Извлекаем токен из конфигурации. В рабочей среде он должен храниться безопасно (например, в User Secrets или переменных окружения).
var botToken = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken))
{
    // Завершаем работу с понятной ошибкой, если токен не задан.
    throw new InvalidOperationException("Telegram:BotToken is not configured in appsettings.json or User Secrets.");
}

// Регистрируем ITelegramBotClient с использованием IHttpClientFactory (рекомендуемый подход для избежания проблем с сокетами)
builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        // Опции клиента, куда передается токен
        TelegramBotClientOptions options = new(botToken);
        return new TelegramBotClient(options, httpClient);
    });

// Регистрируем наш сервис как Singleton (существует в единственном экземпляре на протяжении работы приложения)
builder.Services.AddSingleton<TelegramBotService>();

// Добавляем фоновый Worker, который будет управлять жизненным циклом бота
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
