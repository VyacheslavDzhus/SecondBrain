using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecondBrain.Core.Interfaces;
using SecondBrain.Domain;
using System.Text;

namespace SecondBrain.Integrations.Gemini;

/// <summary>
/// Реализация классификатора сообщений через официальный SDK Google.GenAI.
/// </summary>
public class GeminiMessageClassifier : IMessageClassifier
{
    private readonly Client _client;
    private readonly GeminiOptions _options;
    private readonly RoutingOptions _routingOptions;
    private readonly ILogger<GeminiMessageClassifier> _logger;

    public GeminiMessageClassifier(
        IOptions<GeminiOptions> options,
        IOptions<RoutingOptions> routingOptions,
        ILogger<GeminiMessageClassifier> logger)
    {
        _options = options.Value;
        _routingOptions = routingOptions.Value;
        _logger = logger;
        
        // Создаем клиент, передавая ключ напрямую
        _client = new Client(apiKey: _options.ApiKey);
    }

    public async Task<MessageAnalysisResult?> ClassifyAsync(string? text, byte[]? audioData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) && (audioData == null || audioData.Length == 0))
            return null;

        if (_routingOptions.Destinations.Count == 0)
            return null;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ты — умный ассистент-классификатор и личный редактор. Твоя задача — проанализировать сообщение пользователя.");
            sb.AppendLine("Если пользователь отправил аудио, внимательно прослушай его.");
            sb.AppendLine("1. Выбери наиболее подходящий маршрут (Id) из списка доступных. Если ничего не подходит, верни null.");
            sb.AppendLine("2. Проанализируй текст или транскрибируй аудио.");
            sb.AppendLine("3. ОБЯЗАТЕЛЬНО улучши, сожми и отформатируй мысль пользователя, превратив поток сознания в четкий, структурированный список или формальную запись.");
            sb.AppendLine($"   - Текущая дата и время: {DateTime.Now:dd.MM.yyyy HH:mm}. Обязательно добавляй дату и время в начало или конец сообщения.");
            sb.AppendLine("   - Используй форматирование HTML (например, <b>жирный текст</b>, <i>курсив</i>, <u>подчеркнутый</u>, <code>моноширинный</code>).");
            sb.AppendLine("   - ВАЖНО: Telegram поддерживает ТОЛЬКО теги <b>, <i>, <u>, <s>, <code>. СТРОГО ЗАПРЕЩЕНО использовать теги <br>, <br/>, <p>, <ul>, <li>! Для переноса строк используй обычный перенос (Enter/\\n). Списки оформляй обычным текстом (1., 2., •).");
            sb.AppendLine("   - Обязательно используй подходящие по смыслу эмодзи для красоты и визуального разделения 📅🛒💰💡.");
            sb.AppendLine("   - Пример красивого результата:");
            sb.AppendLine("     <b>📅 09.05.2026 15:30</b>");
            sb.AppendLine("     <b>🛍 Покупки:</b>");
            sb.AppendLine("     • <i>Продукты</i> — 456 грн 🥦");
            sb.AppendLine("     • <i>Еда питомцам</i> — 150 грн 🐈");
            sb.AppendLine("   - Применяй такой подход формализации АБСОЛЮТНО КО ВСЕМ сообщениям. Убирай воду, оставляй суть и красиво оформляй текст.");
            sb.AppendLine();
            sb.AppendLine("Ответ верни СТРОГО в формате JSON с двумя строковыми полями: 'DestinationId' и 'ImprovedText'. Никаких других слов или форматирования быть не должно.");
            sb.AppendLine();
            sb.AppendLine("Доступные маршруты:");
            
            foreach (var dest in _routingOptions.Destinations)
            {
                sb.AppendLine($"- Id: {dest.Id} | Группа: {dest.GroupName} | Топик: {dest.TopicName}");
                sb.AppendLine($"  Описание: {dest.Description}");
            }

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Role = "system",
                    Parts = new List<Part> { new Part { Text = sb.ToString() } }
                },
                ResponseMimeType = "application/json"
            };

            var userParts = new List<Part>();
            if (!string.IsNullOrWhiteSpace(text))
            {
                userParts.Add(new Part { Text = text });
            }
            if (audioData != null && audioData.Length > 0)
            {
                userParts.Add(new Part
                {
                    InlineData = new Blob { MimeType = "audio/ogg", Data = audioData }
                });
                
                if (string.IsNullOrWhiteSpace(text))
                {
                    userParts.Add(new Part { Text = "Пожалуйста, расшифруй это аудиосообщение и отформатируй его согласно системным инструкциям." });
                }
            }

            var response = await _client.Models.GenerateContentAsync(
                model: _options.ModelId,
                contents: new List<Content> { new Content { Role = "user", Parts = userParts } },
                config: config,
                cancellationToken: cancellationToken);

            var answerText = response.Text?.Trim();

            _logger.LogInformation("Gemini raw response: '{Answer}'", answerText);

            if (string.IsNullOrWhiteSpace(answerText))
                return null;

            // Парсим JSON-ответ от Gemini
            var result = System.Text.Json.JsonSerializer.Deserialize<MessageAnalysisResult>(
                answerText, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || string.IsNullOrWhiteSpace(result.DestinationId) || result.DestinationId == "null")
                return null;

            // Проверяем, существует ли такой Id реально
            var destination = _routingOptions.Destinations.FirstOrDefault(d => d.Id.Equals(result.DestinationId, StringComparison.OrdinalIgnoreCase));
            if (destination == null)
            {
                _logger.LogWarning("Gemini returned unknown DestinationId: {Id}", result.DestinationId);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while classifying and improving message via Google.GenAI API.");
            return null;
        }
    }
}
