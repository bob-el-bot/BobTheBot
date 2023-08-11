using System;

public class Status
{
    public static string RandomStatus()
    {
        // Possible Statuses
        string[] statuses = { "with RaspberryPI", "with C#", "with new commands!", "with new ideas!" };

        Random random = new Random();

        return statuses[random.Next(0, statuses.Length)];
    }
}