using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecondBrain.Core.Interfaces;
using SecondBrain.Domain;

namespace SecondBrain.Integrations.Gemini;

/// <summary>
/// Реализация классификатора сообщений через официальный SDK Google.GenAI.
/// </summary>
public class GeminiMessageClassifier : IMessageClassifier
{
    private readonly Client _client;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiMessageClassifier> _logger;

    public GeminiMessageClassifier(
        IOptions<GeminiOptions> options,
        ILogger<GeminiMessageClassifier> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Создаем клиент, передавая ключ напрямую
        _client = new Client(apiKey: _options.ApiKey);
    }

    public async Task<MessageCategory> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return MessageCategory.Other;

        try
        {
            var systemInstruction = "Ты — классификатор текста. Твоя единственная задача: прочитать сообщение пользователя и вернуть ОДНО слово-категорию на английском из списка: Task, Note, Finance, Other. Больше ничего не пиши. Task - это задачи или дела. Note - просто полезная информация, заметки. Finance - покупки, траты, доходы. Other - если ничто не подходит.";

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Role = "system",
                    Parts = new List<Part> { new Part { Text = systemInstruction } }
                }
            };

            var response = await _client.Models.GenerateContentAsync(
                model: _options.ModelId,
                contents: text,
                config: config,
                cancellationToken: cancellationToken);

            var answerText = response.Text?.Trim();

            _logger.LogInformation("Gemini classified message as: '{Answer}'", answerText);

            if (Enum.TryParse<MessageCategory>(answerText, true, out var category))
            {
                return category;
            }

            return MessageCategory.Other;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while classifying message via Google.GenAI API.");
            return MessageCategory.Other;
        }
    }
}
