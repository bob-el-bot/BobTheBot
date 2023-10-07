public class HeartLevels
{
    private static readonly HeartLevel[] heartLevels = { new HeartLevel("💔 `0`", 0), new HeartLevel("❤️ `1`", 10), new HeartLevel("💓 `2`", 20), new HeartLevel("💗 `2`", 35), new HeartLevel("💕 `3`", 50), new HeartLevel("💞 `4`", 65), new HeartLevel("💖 `5`", 80), new HeartLevel("💘 `6`", 90), };

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