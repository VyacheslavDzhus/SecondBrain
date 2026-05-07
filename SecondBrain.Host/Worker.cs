using SecondBrain.Telegram;

namespace SecondBrain.Host;

public class Worker : BackgroundService
{
    private readonly TelegramBotService _botService;
    private readonly ILogger<Worker> _logger;

    public Worker(TelegramBotService botService, ILogger<Worker> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);

        // Запускаем подписку на события Telegram.
        // Этот метод внутри использует Task.Run или асинхронные вызовы, поэтому не блокирует поток.
        _botService.StartReceiving(stoppingToken);

        try
        {
            // Worker Service должен продолжать работать в фоне.
            // Task.Delay с Timeout.InfiniteTimeSpan позволяет этому методу "спать", 
            // пока не придет сигнал на остановку через stoppingToken.
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Ожидаемое поведение при корректном завершении приложения (например, по Ctrl+C)
            _logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
        }
    }
}
