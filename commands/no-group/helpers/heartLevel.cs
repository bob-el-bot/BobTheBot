namespace Bob.Commands.Helpers
{
    public class HeartLevel(string heart, int min)
    {
        public string Heart { get; set; } = heart;
        public int Min { get; set; } = min;
    }
}