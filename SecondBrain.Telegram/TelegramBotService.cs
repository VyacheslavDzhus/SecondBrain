using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace SecondBrain.Telegram;

/// <summary>
/// Сервис для работы с Telegram Bot API. 
/// Инкапсулирует логику подписки на обновления (Long Polling) и обработки входящих сообщений.
/// </summary>
public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    /// <summary>
    /// Запускает процесс Long Polling для прослушивания входящих обновлений от Telegram.
    /// Метод не блокирует выполнение, StartReceiving запускает фоновую задачу внутри себя.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для остановки прослушивания.</param>
    public void StartReceiving(CancellationToken cancellationToken)
    {
        // Настройка параметров получения обновлений.
        // Пустой массив AllowedUpdates означает, что мы хотим получать все типы обновлений.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        // Запуск приема обновлений.
        // Передаем делегаты для обработки успешных обновлений и ошибок.
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );
        
        _logger.LogInformation("Telegram Bot started receiving updates.");
    }

    /// <summary>
    /// Основной обработчик входящих обновлений от Telegram.
    /// </summary>
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Извлекаем сообщение и его текст.
        // Если это не сообщение или в нем нет текста (например, это картинка или системное событие), просто игнорируем.
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        _logger.LogInformation("Received message '{MessageText}' in chat {ChatId}.", messageText, chatId);

        // Реализация логики MVP 1 (Echo-bot):
        // Отправляем обратно полученный текст с префиксом "Echo:"
        await botClient.SendMessage(
            chatId: chatId,
            text: $"Echo: {messageText}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обработчик ошибок, возникающих в процессе Long Polling или при обращении к API Telegram.
    /// </summary>
    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            // Ошибки, возвращаемые самим API Telegram (например, неверный токен, блокировка бота)
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            // Прочие исключения (например, сетевые проблемы)
            _ => exception.ToString()
        };

        _logger.LogError("Telegram Error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}
