namespace SecondBrain.Domain;

/// <summary>
/// Категории входящих сообщений, определяемые LLM-классификатором.
/// </summary>
public enum MessageCategory
{
    /// <summary>
    /// Не удалось определить категорию или иное сообщение.
    /// </summary>
    Other = 0,

    /// <summary>
    /// Сообщение является задачей (to-do, action item).
    /// </summary>
    Task = 1,

    /// <summary>
    /// Сообщение является просто заметкой для сохранения в базу знаний.
    /// </summary>
    Note = 2,

    /// <summary>
    /// Сообщение касается финансов (траты, доходы).
    /// </summary>
    Finance = 3
}
