using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BobTheBot.Chat.AiServiceHandling;

namespace BobTheBot.Chat.Routing
{
    public static class ModelRouter
    {
        /// <summary>
        /// Decides which model to use based on complexity and attachments:
        /// - GPT‑4o‑mini (OpenAI) → default
        /// - Gemini 2.0 Flash‑Lite → if image input
        /// - Gemini 2.5 Flash → if complex reasoning
        /// </summary>
        public static async Task<string> GetResponseAsync(
            List<object> messages,
            List<ImageAttachment> imageAttachments = null)
        {
            // Combine all message contents for complexity checks
            string cleanedText = "";
            foreach (var m in messages)
            {
                if (m.GetType().GetProperty("content")?.GetValue(m) is string text)
                    cleanedText += text + " ";
            }
            cleanedText = cleanedText.ToLowerInvariant();

            // Image handling
            if (imageAttachments != null && imageAttachments.Count > 0)
            {
                try
                {
                    return await Gemini.AnalyzeImagesAsync(
                        imageAttachments,
                        "Describe these image(s) in detail and explain relevant visual information.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Router] Gemini image analysis failed: {ex.Message}");
                    return "I tried to look at the image(s) you uploaded, but they were too large or unreadable.";
                }
            }

            if (IsComplex(cleanedText))
            {
                var geminiMessages = ConvertMessages(messages);
                return await Gemini.GenerateTextAsync(geminiMessages, useFlashThinking: true);
            }

            return await OpenAI.PostToOpenAI(messages);
        }

        private static bool IsComplex(string text)
        {
            int qMarks = new Regex(@"\?").Matches(text).Count;
            bool fuzzyComplex = FuzzyKeywords.MatchesComplex(text);
            bool longMsg = text.Split(' ').Length > 60;
            return qMarks > 1 || fuzzyComplex || longMsg;
        }

        /// <summary>
        /// Converts generic `{ role, content }` objects to `(role, content)` tuples
        /// for the Gemini API.
        /// </summary>
        private static List<(string role, string content)> ConvertMessages(List<object> msgs)
        {
            var systemTexts = new List<string>();
            var outList = new List<(string, string)>();

            foreach (var m in msgs)
            {
                string role = m.GetType().GetProperty("role")?.GetValue(m)?.ToString() ?? "user";
                string content = m.GetType().GetProperty("content")?.GetValue(m)?.ToString() ?? "";

                if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    systemTexts.Add(content);
                }
                else
                {
                    outList.Add((role, content));
                }
            }

            // Merge multiple system messages into a single context block 
            if (systemTexts.Count > 0)
            {
                var merged = "CONTEXT (instructions + memory, not from user):\n" +
                             string.Join("\n", systemTexts);

                // Insert at start, but still role=user (Gemini only allows user/model)
                outList.Insert(0, ("user", merged));
            }

            return outList;
        }
    }
}