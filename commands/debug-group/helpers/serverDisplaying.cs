using System;
using System.Reflection;
using System.Text;
using Bob.Database.Types;

namespace Bob.Commands.Helpers
{
    public static class ServerDebugging
    {
        public static string GetServerPropertyString(Server server)
        {
            StringBuilder stringBuilder = new();
            PropertyInfo[] properties = typeof(Server).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            stringBuilder.AppendLine("```cs\n");

            foreach (var property in properties)
            {
                string propertyName = property.Name;
                object value = property.GetValue(server) ?? "null"; // Handle null values gracefully
                
                stringBuilder.AppendLine($"{propertyName}: {value}");
            }

            stringBuilder.AppendLine("```");

            return stringBuilder.ToString();
        }
    }
}
