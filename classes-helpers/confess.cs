using Discord.Interactions;

public class Confession
{
    public enum SignOffs
    {
        Anon,
        [ChoiceDisplay("Secret Admirer")]
        Secret_Admirer,
        [ChoiceDisplay("You know who")]
        You_Know_Who,
        Guess,
        FBI,
        [ChoiceDisplay("Your Dad")]
        Your_Dad
    }
}