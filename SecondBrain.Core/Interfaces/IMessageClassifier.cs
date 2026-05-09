using SecondBrain.Domain;

namespace SecondBrain.Core.Interfaces;

/// <summary>
/// Интерфейс для классификации текстовых сообщений с помощью нейросети.
/// </summary>
public interface IMessageClassifier
{
    /// <summary>
    /// Анализирует текст или аудио, определяет маршрут и улучшает текст (если требуется).
    /// </summary>
    /// <param name="text">Входящий текст (может быть null, если это голосовое сообщение).</param>
    /// <param name="audioData">Массив байтов голосового сообщения (может быть null).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат анализа или null, если подходящий топик не найден.</returns>
    Task<MessageAnalysisResult?> ClassifyAsync(string? text, byte[]? audioData, CancellationToken cancellationToken = default);
}
