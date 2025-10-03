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

    public static async Task<string> GenerateTextAsync(
                List<(string role, string content)> messages,
                bool useFlashThinking = false)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY is missing.");

        string model = useFlashThinking
            ? "gemini-2.5-flash"
            : "gemini-2.0-flash-lite";

        // Build Gemini compliant body
        var contents = new List<object>();
        foreach (var (role, content) in messages)
        {
            contents.Add(new
            {
                role,
                parts = new[] { new { text = content } }
            });
        }

        var body = new { contents };
        var json = JsonConvert.SerializeObject(body);

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await Client.SendAsync(req);
        string respText = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini Error {resp.StatusCode}: {respText}");
        }

        dynamic parsed = JsonConvert.DeserializeObject(respText);
        string output = parsed?.candidates?[0]?.content?.parts?[0]?.text ?? "";
        return output.Trim();
    }

    public static async Task<string> GenerateMultimodalAsync(
        List<(string role, string content)> messages,
        List<ImageAttachment> attachments,
        bool useFlashThinking = false)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY missing.");

        string model = useFlashThinking ? "gemini-2.5-flash" : "gemini-2.0-flash-lite";

        // build initial parts from the conversation
        var parts = new List<object>();
        foreach (var (role, content) in messages)
        {
            if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
                parts.Add(new { text = content });
        }

        // append image data
        long totalBytes = 0;
        foreach (var att in attachments)
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(att.Url);
            totalBytes += bytes.Length;
            if (totalBytes > 20 * 1024 * 1024) break; // 20 MB cap

            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = att.MimeType ?? "image/png",
                    data = Convert.ToBase64String(bytes)
                }
            });
        }

        var body = new
        {
            contents = new[] { new { role = "user", parts } }
        };

        var json = JsonConvert.SerializeObject(body);

        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}")
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

    public static async Task<string> AnalyzeImagesAsync(
        List<ImageAttachment> attachments,
        string prompt)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY missing.");

        const int MaxTotalBytes = 20 * 1024 * 1024; // 20 MB limit
        int totalBytes = 0;
        var included = new List<ImageAttachment>();
        var skipped = new List<ImageAttachment>();

        var parts = new List<object> { new { text = prompt } };

        foreach (var att in attachments)
        {
            try
            {
                var bytes = await Client.GetByteArrayAsync(att.Url);
                totalBytes += bytes.Length;

                if (totalBytes > MaxTotalBytes)
                {
                    skipped.Add(att);
                    continue;
                }

                string base64 = Convert.ToBase64String(bytes);
                string mime = string.IsNullOrEmpty(att.MimeType)
                    ? "image/png"
                    : att.MimeType;

                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = mime,
                        data = base64
                    }
                });
                included.Add(att);
            }
            catch (Exception ex)
            {
                skipped.Add(att);
                Console.WriteLine($"[Gemini] Failed to read {att.FileName}: {ex.Message}");
            }
        }

        // Add user note if anything got skipped
        if (skipped.Count > 0)
        {
            string skippedNames = string.Join(", ", skipped.ConvertAll(a => a.FileName));
            parts.Add(new
            {
                text = $"(Note: These files were skipped because the total image size exceeded 20 MB: {skippedNames})"
            });
        }

        var body = new
        {
            contents = new[]
            {
                    new { role = "user", parts }
                }
        };

        string json = JsonConvert.SerializeObject(body);

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await Client.SendAsync(req);
        string respText = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini error {resp.StatusCode}: {respText}");
        }

        dynamic parsed = JsonConvert.DeserializeObject(respText);
        string output = parsed?.candidates?[0]?.content?.parts?[0]?.text ?? "";

        // Append a graceful note if anything skipped
        if (skipped.Count > 0)
        {
            output += $"\n\n*(I could only review {included.Count} image"
                    + (included.Count == 1 ? "" : "s")
                    + " this time; other attachments exceeded my current 20 MB processing limit.)*";
        }

        return output.Trim();
    }
}