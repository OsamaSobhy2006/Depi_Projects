using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CampusOne.Infrastructure.AI;

public class EmbeddingStore
{
    private readonly List<EmbeddedChunk> _chunks;
    public IReadOnlyCollection<EmbeddedChunk> Chunks => _chunks;

    public EmbeddingStore(IConfiguration configuration)
    {
        var path = configuration["AI:EmbeddingsPath"];

        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(AppContext.BaseDirectory, "embeddings.json");

        else if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(path))
            throw new FileNotFoundException("RAG embeddings file was not found.", path);

        var json = File.ReadAllText(path);

        _chunks = JsonSerializer.Deserialize<List<EmbeddedChunk>>(json) ?? [];
    }
}
