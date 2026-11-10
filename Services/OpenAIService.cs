using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BikeRepairAI.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private string ApiKey =>
        _configuration["HuggingFace:ApiKey"]
        ?? throw new InvalidOperationException(
            "Hugging Face API key is missing.");

    public OpenAIService(IHttpClientFactory factory, IConfiguration config)
    {
        _httpClient = factory.CreateClient("HuggingFace");
        _configuration = config;
    }

    // OpenAI embeddings
    public async Task<float[]> GetEmbeddingAsync(string text, bool isQuery = false)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var baseUrl = _configuration["Gemini:BaseUrl"];
        var model = _configuration["Gemini:EmbeddingModel"];
        var dimensions = _configuration
            .GetValue<int>("Gemini:OutputDimensionality");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Gemini API key is missing.");

        var url =
            $"{baseUrl}models/{model}:embedContent" +
            $"?key={Uri.EscapeDataString(apiKey)}";

        var requestBody = new
        {
            model = $"models/{model}",
            content = new
            {
                parts = new[]
                {
                    new { text }
                }
            },
            taskType = isQuery
                ? "RETRIEVAL_QUERY"
                : "RETRIEVAL_DOCUMENT",
            outputDimensionality = dimensions
        };

        using var response = await _httpClient.PostAsJsonAsync(
            url, requestBody);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API error: {(int)response.StatusCode} " +
                responseBody);
        }

        using var json = JsonDocument.Parse(responseBody);

        return json.RootElement
            .GetProperty("embedding")
            .GetProperty("values")
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }

    // Groq chat
    public async Task<string> GenerateAnswerAsync(
        string question,
        string context)
    {
        var apiKey = _configuration["Groq:ApiKey"];
        var baseUrl = _configuration["Groq:BaseUrl"];
        var model = _configuration["Groq:ChatModel"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Groq API key is missing.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Content = JsonContent.Create(new
        {
            model = model,
            messages = new []
            {
                new
                {
                    role = "system",
                    content = """
                        You are a bicycle repair assistant.
                        Answer using only the supplied context.
                        If the context does not contain the answer,
                        say you do not have enough information.
                        Never invent repair instructions.
                        For safety-critical brake problems,
                        recommend consulting a qualified bicycle mechanic.
                        """
                },
                new
                {
                    role = "user",
                    content = $"""
                        Context:
                        {context}
                        Question:
                        {question}
                        """
                }
            },
            max_completion_tokens = 500,
            stream = false
        });

        var response = await _httpClient.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Groq API error: {(int)response.StatusCode} " +
                $"{responseBody}");
        }

        using var json = JsonDocument.Parse(responseBody);

        return json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}