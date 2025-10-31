namespace TempletonTestApi.Options
{
    public class HackerNewsServiceOptions
    {
        public static string SectionName => "HackerNewsService";
        public int MaxDegreeOfParallelism { get; init; }
        public int ItemTTLInMinutes { get; init; }
    }
}
