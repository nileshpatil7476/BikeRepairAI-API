
using System.Net.Http.Json;
using System.Text.Json;

namespace BikeRepairAI.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:11434/");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/embed",
            new
            {
                model = "embeddinggemma",
                input = text
            });

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Ollama embedding error: {(int)response.StatusCode}. {body}");
        }

        using var json = JsonDocument.Parse(body);

        return json.RootElement
            .GetProperty("embeddings")[0]
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string context)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            new
            {
                model = "llama3.2:3b",
                stream = false,
                messages = new object[]
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
                }
            });

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Ollama chat error: {(int)response.StatusCode}. {body}");
        }

        using var json = JsonDocument.Parse(body);

        return json.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "No answer was returned.";
    }
}
