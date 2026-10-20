namespace BikeRepairAI.Models
{
    public class DocumentChunk
    {
        public int Id { get; set; }

        public string Source { get; set; } = string.Empty;

        public string? Section { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? EmbeddingJson { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
