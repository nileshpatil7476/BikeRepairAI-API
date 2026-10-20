using BikeRepairAI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BikeRepairAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<DocumentChunk> DocumentChunks =>
            Set<DocumentChunk>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentChunk>()
                .ToTable("DocumentChunks");

            modelBuilder.Entity<DocumentChunk>()
                .Property(x => x.EmbeddingJson)
                .HasColumnType("nvarchar(max)");
        }
    }
}
