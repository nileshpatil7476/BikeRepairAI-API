
using System.Text.Json;
using BikeRepairAI.Data;
using BikeRepairAI.Models;
using Microsoft.EntityFrameworkCore;

namespace BikeRepairAI.Services;

public partial class RagService
{
    private readonly AppDbContext _db;
    private readonly OpenAIService _openAI;

    public RagService(AppDbContext db, OpenAIService openAI)
    {
        _db = db;
        _openAI = openAI;
    }

    private static string ExtractPdfText(string filePath)
    {
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath);

        return string.Join(
            "\n",
            pdf.GetPages().Select(page => page.Text));
    }

    private static List<string> SplitIntoChunks(
        string text,
        int chunkSize = 3000,
        int overlap = 300)
    {
        var chunks = new List<string>();

        for (int start = 0; start < text.Length;)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            string chunk = text.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (start + length >= text.Length)
                break;

            start += chunkSize - overlap;
        }

        return chunks;
    }

    // Steps 1-4: Extract, chunk, embed, and store the PDF.
    public async Task<int> IngestPdfAsync(
        string filePath, string source)
    {
        var text = ExtractPdfText(filePath);
        var chunks = SplitIntoChunks(text);

        foreach (var chunk in chunks)
        {
            var embedding = await _openAI.GetEmbeddingAsync(chunk);

            _db.DocumentChunks.Add(new DocumentChunk
            {
                Source = source,
                Section = "General",
                Content = chunk,
                EmbeddingJson = JsonSerializer.Serialize(embedding)
            });
        }

        await _db.SaveChangesAsync();

        return chunks.Count;
    }

    // Steps 5-7: Embed question, find similar chunks, generate answer.
    public async Task<string> AskAsync(string question)
    {
        var questionVector =
            await _openAI.GetEmbeddingAsync(question);

        var documents = await _db.DocumentChunks
            .AsNoTracking()
            .Where(x => x.EmbeddingJson != null)
            .ToListAsync();

        var matches = documents
            .Select(doc => new
            {
                Document = doc,
                Score = CosineSimilarity(
                    questionVector,
                    JsonSerializer.Deserialize<float[]>(
                        doc.EmbeddingJson!) ?? Array.Empty<float>())
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        if (matches.Count == 0)
        {
            return "I couldn't find relevant information in the repair manuals.";
        }

        var context = string.Join(
            "\n\n",
            matches.Select(x =>
                $"Source: {x.Document.Source}\n{x.Document.Content}"));

        return await _openAI.GenerateAnswerAsync(question, context);
    }

    private static double CosineSimilarity(
        float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
            return 0;

        double dot = 0, normA = 0, normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
