using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecondBrain.Core.Interfaces;
using SecondBrain.Domain;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SecondBrain.Telegram;

/// <summary>
/// Сервис для работы с Telegram Bot API. 
/// Инкапсулирует логику подписки на обновления (Long Polling) и обработки входящих сообщений.
/// </summary>
public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageClassifier _messageClassifier;
    private readonly RoutingOptions _routingOptions;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient, 
        IMessageClassifier messageClassifier,
        IOptions<RoutingOptions> routingOptions,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _messageClassifier = messageClassifier;
        _routingOptions = routingOptions.Value;
        _logger = logger;
    }

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

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;
        var threadId = message.MessageThreadId;

        // Режим отладки: Если сообщение из группы, выводим ID, чтобы пользователь мог добавить их в конфиг.
        if (message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            _logger.LogWarning("Group Message Detected! ChatId: {ChatId}, ThreadId: {ThreadId}, GroupName: {Title}", 
                chatId, threadId, message.Chat.Title);
            return;
        }

        // Если это личное сообщение, но без текста (поддержку медиа добавим позже)
        if (message.Text is not { } messageText)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Пока я понимаю только текстовые сообщения.",
                cancellationToken: cancellationToken);
            return;
        }

        _logger.LogInformation("Received private message: '{MessageText}'", messageText);

        // Передаем текст в классификатор (Gemini)
        var destinationId = await _messageClassifier.ClassifyAsync(messageText, cancellationToken);

        if (destinationId == null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Не удалось определить подходящий маршрут для этого сообщения.",
                cancellationToken: cancellationToken);
            return;
        }

        var destination = _routingOptions.Destinations.FirstOrDefault(d => d.Id == destinationId);
        if (destination == null || destination.ChatId == 0)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"Маршрут '{destinationId}' определен, но он не настроен (отсутствует ChatId в конфиге).",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Используем CopyMessage, чтобы переслать сообщение "от имени бота" в нужную группу и топик
            await botClient.CopyMessage(
                chatId: destination.ChatId,
                fromChatId: chatId,
                messageId: message.MessageId,
                messageThreadId: destination.ThreadId == 0 ? null : destination.ThreadId,
                cancellationToken: cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: $"✅ Отправлено в: {destination.GroupName} -> {destination.TopicName}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy message to destination {DestinationId}", destination.Id);
            await botClient.SendMessage(
                chatId: chatId,
                text: $"❌ Ошибка при отправке в топик '{destination.TopicName}'. Проверьте логи (возможно, бот не добавлен в группу).",
                cancellationToken: cancellationToken);
        }
    }

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
