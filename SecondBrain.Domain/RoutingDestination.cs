namespace SecondBrain.Domain;

/// <summary>
/// Описание маршрута (топика в Telegram), куда бот может переслать сообщение.
/// </summary>
public class RoutingDestination
{
    /// <summary>
    /// Уникальный идентификатор (например, "dzhus_notes")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Название группы (для удобства чтения конфига)
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Название топика (для удобства чтения конфига)
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Описание для LLM (что именно сюда попадает)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID чата в Telegram (отрицательное число для супергрупп)
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// ID топика в Telegram (MessageThreadId)
    /// </summary>
    public int ThreadId { get; set; }
}
