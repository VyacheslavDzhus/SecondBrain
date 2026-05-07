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

    public async Task<string?> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || _routingOptions.Destinations.Count == 0)
            return null;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ты — классификатор текста. Твоя единственная задача: прочитать сообщение пользователя и вернуть ОДНО слово — идентификатор (Id) подходящего маршрута из списка ниже.");
            sb.AppendLine("Если ни один маршрут не подходит, верни слово 'null'. Никаких других слов или объяснений писать нельзя.");
            sb.AppendLine();
            sb.AppendLine("Доступные маршруты:");
            
            foreach (var dest in _routingOptions.Destinations)
            {
                sb.AppendLine($"- Id: {dest.Id}");
                sb.AppendLine($"  Группа: {dest.GroupName}");
                sb.AppendLine($"  Топик: {dest.TopicName}");
                sb.AppendLine($"  Описание: {dest.Description}");
                sb.AppendLine();
            }

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Role = "system",
                    Parts = new List<Part> { new Part { Text = sb.ToString() } }
                }
            };

            var response = await _client.Models.GenerateContentAsync(
                model: _options.ModelId,
                contents: text,
                config: config,
                cancellationToken: cancellationToken);

            var answerText = response.Text?.Trim();

            _logger.LogInformation("Gemini classified message as: '{Answer}'", answerText);

            if (answerText == "null" || string.IsNullOrWhiteSpace(answerText))
                return null;

            // Проверяем, существует ли такой Id реально
            var destination = _routingOptions.Destinations.FirstOrDefault(d => d.Id.Equals(answerText, StringComparison.OrdinalIgnoreCase));
            return destination?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while classifying message via Google.GenAI API.");
            return null;
        }
    }
}
