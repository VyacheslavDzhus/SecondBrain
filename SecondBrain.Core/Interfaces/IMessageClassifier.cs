namespace SecondBrain.Core.Interfaces;

/// <summary>
/// Интерфейс для классификации текстовых сообщений с помощью нейросети.
/// </summary>
public interface IMessageClassifier
{
    /// <summary>
    /// Анализирует текст и возвращает идентификатор подходящего маршрута (топика).
    /// </summary>
    /// <param name="text">Входящий текст (например, из Telegram).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Строковый ID маршрута (например, "dzhus_notes") или null, если не подошло ничего.</returns>
    Task<string?> ClassifyAsync(string text, CancellationToken cancellationToken = default);
}
