public class HeartLevels
{
    public static HeartLevel[] heartLevels = { new HeartLevel("💔", 0), new HeartLevel("❤️", 10), new HeartLevel("💓", 20), new HeartLevel("💗", 35), new HeartLevel("💕", 50), new HeartLevel("💞", 65), new HeartLevel("💖", 80), new HeartLevel("💘", 90), };

    public static string CalculateHeartLevel(float matchPercent)
    {
        string heartLevel = "";
        foreach (HeartLevel level in heartLevels)
        {
            if (matchPercent >= level.min)
            {
                heartLevel = level.heart;
            }
            else
            {
                break;
            }
        }

        return heartLevel;
    }
}