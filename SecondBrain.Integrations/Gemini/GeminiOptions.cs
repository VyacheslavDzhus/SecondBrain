namespace SecondBrain.Integrations.Gemini;

/// <summary>
/// Конфигурация для доступа к Google Gemini API.
/// </summary>
public class GeminiOptions
{
    /// <summary>
    /// API ключ (из Google AI Studio).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор модели (например, "gemini-1.5-flash").
    /// </summary>
    public string ModelId { get; set; } = "gemini-1.5-flash";
}
