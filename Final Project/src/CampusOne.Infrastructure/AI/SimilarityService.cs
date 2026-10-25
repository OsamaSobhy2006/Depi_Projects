namespace CampusOne.Infrastructure.AI
{
    public class SimilarityService
    {
        public List<(EmbeddedChunk Chunk, double Score)> Search(
            float[] queryEmbedding,
            IReadOnlyCollection<EmbeddedChunk> chunks,
            int topK = 5)
        {
            return chunks
                .Select(chunk => (Chunk: chunk, Score: CosineSimilarity(queryEmbedding, chunk.Embedding)))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();
        }

        public static double CosineSimilarity(float[] a, float[] b)
        {
            if(a.Length != b.Length)
                throw new ArgumentException("Embedding dimensions do not match.");

            double dot = 0, magnitudeA = 0, magnitudeB = 0;

            for (var i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}
