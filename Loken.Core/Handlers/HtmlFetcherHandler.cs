namespace Loken.Core;

using System.Text.Json;

public class HtmlFetcherHandler(IHttpClientFactory httpClientFactory) : IToolHandler
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private const int MaxResponseSize = 10 * 1024 * 1024; // 10MB limit
    private const int TimeoutSeconds = 30;

    public string Name => "fetch_html";

    public string Description => "Fetch HTML content from a URL. Only downloads HTML content, never executes JavaScript.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
        new
        {
            type = "object",
            properties = new
            {
                url = new
                {
                    type = "string",
                    description = "The URL to fetch HTML from"
                },
                timeout = new
                {
                    type = "integer",
                    description = "Timeout in seconds (optional, default: 30)",
                    minimum = 1,
                    maximum = 300
                },
                maxSize = new
                {
                    type = "integer",
                    description = "Maximum response size in bytes (optional, default: 10485760 = 10MB)",
                    minimum = 1024,
                    maximum = 50 * 1024 * 1024
                }
            },
            required = new[] { "url" }
        });

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            if (!doc.RootElement.TryGetProperty("url", out var urlProp))
                throw new MissingParameterException("url");

            var url = urlProp.GetString()
                       ?? throw new MissingParameterException("url");

            int timeout = doc.RootElement.TryGetProperty("timeout", out var timeoutProp)
                        ? timeoutProp.GetInt32()
                        : TimeoutSeconds;

            int maxSize = doc.RootElement.TryGetProperty("maxSize", out var maxSizeProp)
                        ? maxSizeProp.GetInt32()
                        : MaxResponseSize;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ExecutionFailedException($"Invalid URL format: {url}");

            if (uri.Scheme != "http" && uri.Scheme != "https")
                throw new ExecutionFailedException($"Only HTTP and HTTPS protocols are allowed. Got: {uri.Scheme}");

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Loken-HTML-Fetcher/1.0");

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!headResponse.IsSuccessStatusCode)
                throw new ExecutionFailedException($"HTTP error: {(int)headResponse.StatusCode} {headResponse.ReasonPhrase}");

            var contentType = headResponse.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (contentType != "text/html")
                throw new ExecutionFailedException($"Content type '{contentType}' is not HTML. Only text/html is allowed.");

            using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new ExecutionFailedException($"HTTP error: {(int)response.StatusCode} {response.ReasonPhrase}");

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > maxSize)
                throw new ExecutionFailedException($"Response too large: {contentLength.Value} bytes exceeds limit of {maxSize} bytes");

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var buffer = new char[Math.Min(maxSize, 1024 * 1024)]; // Read in 1MB chunks
            var result = new System.Text.StringBuilder();
            int totalRead = 0;
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                result.Append(buffer, 0, charsRead);
                totalRead += charsRead;

                if (totalRead > maxSize)
                    throw new ExecutionFailedException($"Response exceeds size limit of {maxSize} bytes");
            }

            var html = result.ToString();

            var metadata = $"\n\n[HTML fetched from: {url}]";
            metadata += $"\n[Content-Type: {contentType}]";
            metadata += $"\n[Status: {(int)response.StatusCode} {response.ReasonPhrase}]";
            metadata += $"\n[Size: {totalRead} characters]";

            return html + metadata;
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid JSON input.");
        }
        catch (HttpRequestException ex)
        {
            throw new ExecutionFailedException($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new ExecutionFailedException("Request timed out.");
        }
        catch (Exception ex) when (ex is not ExecutionFailedException && ex is not MissingParameterException)
        {
            throw new ExecutionFailedException($"Unexpected error: {ex.Message}");
        }
    }
}
