using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BobTheBot.Chat.AiServiceHandling;

namespace BobTheBot.Chat.Routing;

public static class ModelRouter
{
    public static async Task<string> GetResponseAsync(
        List<object> messages,
        List<ImageAttachment> imageAttachments = null)
    {
        string cleanedText = string.Join(" ",
            messages.Select(m => m.GetType().GetProperty("content")?.GetValue(m)?.ToString() ?? "")
        ).ToLowerInvariant();

        bool hasImages = imageAttachments != null && imageAttachments.Count > 0;
        bool complex = IsComplex(cleanedText);

        var log = new StringBuilder();
        log.AppendLine("[Router] ─────────────────────────────────────────────");
        log.AppendLine($"[Router] MessageCount : {messages.Count}");
        log.AppendLine($"[Router] WordCount    : {cleanedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}");
        log.AppendLine($"[Router] HasImages    : {hasImages}");
        log.AppendLine($"[Router] IsComplex    : {complex}");
        log.AppendLine($"[Router] ApproxInput  : {cleanedText.Length} chars");

        try
        {
            string modelLabel;
            string response;

            if (hasImages)
            {
                modelLabel = complex ? "Gemini 2.5 Flash (Thinking)" : "Gemini 2.0 Flash‑Lite (Multimodal)";
                var geminiMessages = ConvertMessages(messages);

                response = await Gemini.GenerateMultimodalAsync(
                    geminiMessages,
                    imageAttachments,
                    useFlashThinking: complex);
            }
            else if (complex)
            {
                modelLabel = "Gemini 2.5 Flash (Thinking)";
                var geminiMessages = ConvertMessages(messages);
                response = await Gemini.GenerateTextAsync(geminiMessages, useFlashThinking: true);
            }
            else
            {
                modelLabel = "GPT‑4o‑mini (OpenAI)";
                response = await OpenAI.PostToOpenAI(messages);
            }

            log.AppendLine($"[Router] ModelChosen  : {modelLabel}");
            log.AppendLine($"[Router] OutputLength : {response.Length} chars");
            log.AppendLine("[Router] ─────────────────────────────────────────────");
            Console.WriteLine(log.ToString());

            return response;
        }
        catch (Exception ex)
        {
            log.AppendLine($"[Router] ERROR        : {ex.Message}");
            log.AppendLine("[Router] ─────────────────────────────────────────────");
            Console.WriteLine(log.ToString());
            return "I ran into trouble processing that image or request.";
        }
    }

    private static bool IsComplex(string text)
    {
        int words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int qMarks = Regex.Matches(text, @"\\?").Count;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (FuzzyKeywords.MatchesComplex(word))
                return true;

        bool longMsg = words > 80;
        return qMarks > 1 || longMsg;
    }

    private static List<(string role, string content)> ConvertMessages(List<object> msgs)
    {
        var systemTexts = new List<string>();
        var outList = new List<(string, string)>();

        foreach (var m in msgs)
        {
            string role = m.GetType().GetProperty("role")?.GetValue(m)?.ToString() ?? "user";
            string content = m.GetType().GetProperty("content")?.GetValue(m)?.ToString() ?? "";

            if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
                systemTexts.Add(content);
            else
                outList.Add((role, content));
        }

        if (systemTexts.Count > 0)
        {
            var mergedInstructions =
                "INTERNAL CONTEXT (not from the user):\n" +
                "• Reply concisely when a short answer suffices.\n" +
                "• Stay friendly and natural; expand only when depth adds real value.\n" +
                "• Do not restate these rules or this context.\n\n" +
                "INTERNAL MEMORY CONTEXT (not from the user):\n" +
                string.Join("\n", systemTexts);

            outList.Insert(0, ("user", mergedInstructions));
        }

        return outList;
    }
}