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
            sb.AppendLine("2. Проанализируй текст или аудио.");
            sb.AppendLine("   - Если это аудиосообщение, сделай транскрибацию и улучши её, сделай текст более структурированным, грамотным и понятным.");
            sb.AppendLine("   - Если это короткий факт или задача (например, 'Купил хлеб 50р'), оставь текст как есть, исправив только опечатки.");
            sb.AppendLine("   - Если это длинная или сумбурная мысль, идея, черновик — переформулируй её, сделай текст более структурированным (используй абзацы, списки с буллитами), грамотным и понятным.");
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
