namespace CampusOne.Infrastructure.AI;

public class EmbeddedChunk
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = [];
}
