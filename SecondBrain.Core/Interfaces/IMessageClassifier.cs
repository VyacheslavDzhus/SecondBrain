using SecondBrain.Domain;

namespace SecondBrain.Core.Interfaces;

/// <summary>
/// Интерфейс для классификации текстовых сообщений с помощью нейросети.
/// </summary>
public interface IMessageClassifier
{
    /// <summary>
    /// Анализирует текст и возвращает категорию сообщения.
    /// </summary>
    /// <param name="text">Входящий текст (например, из Telegram).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Категория сообщения (Task, Note, Finance, Other).</returns>
    Task<MessageCategory> ClassifyAsync(string text, CancellationToken cancellationToken = default);
}
