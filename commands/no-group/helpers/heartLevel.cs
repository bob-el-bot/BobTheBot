namespace Commands.Helpers
{
    public class HeartLevel
    {
        public string Heart { get; set; }
        public int Min { get; set; }

        public HeartLevel(string heart, int min)
        {
            this.Heart = heart;
            this.Min = min;
        }
    }
}