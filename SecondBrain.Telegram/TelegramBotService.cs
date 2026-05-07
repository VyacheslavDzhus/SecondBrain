using Microsoft.Extensions.Logging;
using SecondBrain.Core.Interfaces;
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
    private readonly IMessageClassifier _messageClassifier;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient, 
        IMessageClassifier messageClassifier,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _messageClassifier = messageClassifier;
        _logger = logger;
    }

    /// <summary>
    /// Запускает процесс Long Polling для прослушивания входящих обновлений от Telegram.
    /// Метод не блокирует выполнение, StartReceiving запускает фоновую задачу внутри себя.
    /// </summary>
    public void StartReceiving(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

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
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        _logger.LogInformation("Received message '{MessageText}' in chat {ChatId}.", messageText, chatId);

        // Передаем текст в классификатор (Gemini)
        var category = await _messageClassifier.ClassifyAsync(messageText, cancellationToken);

        // Формируем ответ с указанием определенной категории
        var responseText = $"[{category}] Вы написали: {messageText}";

        // Отправляем ответ пользователю
        await botClient.SendMessage(
            chatId: chatId,
            text: responseText,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обработчик ошибок, возникающих в процессе Long Polling или при обращении к API Telegram.
    /// </summary>
    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError("Telegram Error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}
