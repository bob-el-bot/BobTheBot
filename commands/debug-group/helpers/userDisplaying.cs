using System.Reflection;
using System.Text;
using BadgeInterface;
using Database.Types;

namespace Commands.Helpers
{
    public static class UserDebugging
    {
        public static string GetUserPropertyString(User user)
        {
            StringBuilder stringBuilder = new();
            FieldInfo[] fields = typeof(User).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            stringBuilder.AppendLine("```cs\n");

            foreach (var field in fields)
            {
                string fieldName = field.Name;

                if (fieldName.EndsWith(">k__BackingField"))
                {
                    fieldName = fieldName.Replace(">k__BackingField", "");
                }

                if (fieldName.StartsWith("<"))
                {
                    fieldName = fieldName.Replace("<", "");
                }

                if (fieldName != "EarnedBadges")
                {
                    stringBuilder.AppendLine($"{fieldName}: {field.GetValue(user)}");
                }
                else
                {
                    stringBuilder.AppendLine($"{fieldName}: {Badge.GetBadgesProfileString(user.EarnedBadges)}");
                }
            }

            stringBuilder.AppendLine("```");

            return stringBuilder.ToString();
        }
    }
}