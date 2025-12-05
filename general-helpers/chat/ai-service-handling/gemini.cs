using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BobTheBot.Chat.AiServiceHandling;

public static class Gemini
{
    private static readonly HttpClient Client = new();

    private static async Task<string> SendRequestAsync(object body, string model)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY is missing.");

        string url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var json = JsonConvert.SerializeObject(body);

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var resp = await Client.SendAsync(req);
        string respText = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini error {resp.StatusCode}: {respText}");

        dynamic parsed = JsonConvert.DeserializeObject(respText);
        return parsed?.candidates?[0]?.content?.parts?[0]?.text ?? "";
    }

    public static async Task<string> GenerateTextAsync(
        List<(string role, string content)> messages,
        bool useFlashThinking = false)
    {
        string model = useFlashThinking ? "gemini-2.5-flash" : "gemini-2.0-flash-lite";

        var contents = new List<object>();
        foreach (var (role, content) in messages)
            contents.Add(new { role, parts = new[] { new { text = content } } });

        return (await SendRequestAsync(new { contents }, model)).Trim();
    }

    public static async Task<string> GenerateMultimodalAsync(
        List<(string role, string content)> messages,
        List<ImageAttachment> attachments,
        bool useFlashThinking = false)
    {
        string model = useFlashThinking ? "gemini-2.5-flash" : "gemini-2.0-flash-lite";

        var parts = new List<object>();
        foreach (var (role, content) in messages)
            if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
                parts.Add(new { text = content });

        const int MaxTotalBytes = 20 * 1024 * 1024;
        long totalBytes = 0;
        var skipped = new List<string>();

        foreach (var att in attachments)
        {
            try
            {
                var bytes = await Client.GetByteArrayAsync(att.Url);
                totalBytes += bytes.Length;
                if (totalBytes > MaxTotalBytes)
                {
                    skipped.Add(att.FileName);
                    break;
                }

                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = att.MimeType ?? "image/png",
                        data = Convert.ToBase64String(bytes)
                    }
                });
            }
            catch
            {
                skipped.Add(att.FileName);
            }
        }

        if (skipped.Count > 0)
            parts.Add(new { text = $"(Note: Skipped {string.Join(", ", skipped)} over 20 MB limit.)" });

        var body = new { contents = new[] { new { role = "user", parts } } };
        return (await SendRequestAsync(body, model)).Trim();
    }

    public static Task<string> AnalyzeImagesAsync(List<ImageAttachment> attachments, string prompt)
    {
        var messages = new List<(string, string)> { ("user", prompt) };
        return GenerateMultimodalAsync(messages, attachments, useFlashThinking: false);
    }
}