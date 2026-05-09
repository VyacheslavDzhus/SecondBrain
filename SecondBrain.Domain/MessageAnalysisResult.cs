namespace SecondBrain.Domain;

/// <summary>
/// Результат анализа текста нейросетью.
/// </summary>
public class MessageAnalysisResult
{
    /// <summary>
    /// Идентификатор маршрута (топика), куда следует отправить сообщение.
    /// Может быть null, если подходящий топик не найден.
    /// </summary>
    public string? DestinationId { get; set; }

    /// <summary>
    /// Улучшенный и отформатированный текст пользователя.
    /// </summary>
    public string? ImprovedText { get; set; }
}
