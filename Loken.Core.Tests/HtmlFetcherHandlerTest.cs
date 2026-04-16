namespace Loken.Core;

using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;

public class HtmlFetcherHandlerTest : IDisposable
{
    private readonly TestHttpMessageHandler _testHandler;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HtmlFetcherHandler _handler;

    public HtmlFetcherHandlerTest()
    {
        _testHandler = new TestHttpMessageHandler();

        _httpClientFactory = new TestHttpClientFactory(_testHandler);
        _handler = new HtmlFetcherHandler(_httpClientFactory);
    }

    public void Dispose()
    {
        _testHandler.Dispose();
    }

    [Fact]
    public void Name_ShouldBe_fetch_html()
    {
        var name = _handler.Name;

        name.ShouldBe("fetch_html");
    }

    [Fact]
    public void Description_ShouldContain_SafetyWarning()
    {
        var description = _handler.Description;

        description.ShouldContain("HTML");
        description.ShouldContain("never executes JavaScript");
    }

    [Fact]
    public void Parameters_ShouldHave_UrlAsRequired()
    {
        var parameters = _handler.Parameters;
        var json = JsonDocument.Parse(parameters);
        var required = json.RootElement.GetProperty("required");

        required.EnumerateArray().ShouldContain(element => element.GetString() == "url");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHtmlUrl_ShouldReturnHtml()
    {
        const string url = "https://example.com";
        const string expectedHtml = "<html><body>Test</body></html>";

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "text/html", expectedHtml);

        var input = BinaryData.FromObjectAsJson(new { url });

        var result = await _handler.ExecuteAsync(input);

        result.ShouldContain(expectedHtml);
        result.ShouldContain("[HTML fetched from:");
        result.ShouldContain("[Content-Type: text/html]");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonHtmlContentType_ShouldThrow()
    {
        const string url = "https://example.com/image.jpg";

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "image/jpeg", "binary data");

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonContentType_ShouldThrow()
    {
        const string url = "https://example.com/api/data";

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "application/json", "{\"data\":\"test\"}");

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithJavaScriptContentType_ShouldThrow()
    {
        const string url = "https://example.com/script.js";

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "application/javascript", "console.log('test')");

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpError_ShouldThrow()
    {
        const string url = "https://example.com/notfound";

        _testHandler.SetupResponse(url, HttpStatusCode.NotFound, "text/html", "Not found");

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUrl_ShouldThrow()
    {
        var input = BinaryData.FromObjectAsJson(new { url = "not-a-valid-url" });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonHttpProtocol_ShouldThrow()
    {
        var input = BinaryData.FromObjectAsJson(new { url = "ftp://example.com/file.html" });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingUrl_ShouldThrow()
    {
        var input = BinaryData.FromObjectAsJson(new { });

        await Should.ThrowAsync<MissingParameterException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ShouldThrow()
    {
        var input = new BinaryData("not valid json");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomTimeout_ShouldUseTimeout()
    {
        const string url = "https://example.com";
        const int timeout = 10;

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "text/html", "<html>Test</html>");

        var input = BinaryData.FromObjectAsJson(new { url, timeout });

        var result = await _handler.ExecuteAsync(input);

        result.ShouldContain("[HTML fetched from:");
    }

    [Fact]
    public async Task ExecuteAsync_WithSizeLimitExceeded_ShouldThrow()
    {
        const string url = "https://example.com";
        const int maxSize = 100; // Very small limit

        var largeHtml = new string('x', maxSize + 1);

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "text/html", largeHtml);

        var input = BinaryData.FromObjectAsJson(new { url, maxSize });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithContentLengthExceedingLimit_ShouldThrowEarly()
    {
        const string url = "https://example.com";
        const int maxSize = 100;

        _testHandler.SetupResponse(url, HttpStatusCode.OK, "text/html", "<html>Test</html>", contentLength: maxSize + 1);

        var input = BinaryData.FromObjectAsJson(new { url, maxSize });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithNetworkError_ShouldThrow()
    {
        const string url = "https://example.com";

        _testHandler.ThrowExceptionOnRequest(new HttpRequestException("Network error"));

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldThrow()
    {
        const string url = "https://example.com";

        _testHandler.ThrowExceptionOnRequest(new TaskCanceledException());

        var input = BinaryData.FromObjectAsJson(new { url });

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(input));
    }

    private class TestHttpMessageHandler : HttpMessageHandler, IDisposable
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = [];
        private Exception? _exceptionToThrow;

        public void SetupResponse(string url, HttpStatusCode statusCode, string contentType, string content, long? contentLength = null)
        {
            var normalizedUrl = url.TrimEnd('/');

            var headResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("", Encoding.UTF8, contentType)
            };

            var getResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, contentType)
            };

            if (contentLength.HasValue)
            {
                headResponse.Content.Headers.ContentLength = contentLength.Value;
                getResponse.Content.Headers.ContentLength = contentLength.Value;
            }

            _responses[$"HEAD:{normalizedUrl}"] = headResponse;
            _responses[$"GET:{normalizedUrl}"] = getResponse;

            _responses[$"HEAD:{url}"] = headResponse;
            _responses[$"GET:{url}"] = getResponse;
        }

        public void ThrowExceptionOnRequest(Exception exception)
        {
            _exceptionToThrow = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exceptionToThrow != null)
                throw _exceptionToThrow;

            if (request.RequestUri == null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var url = request.RequestUri.ToString().TrimEnd('/');
            var key = $"{request.Method}:{url}";

            if (_responses.TryGetValue(key, out var response))
                return Task.FromResult(response);

            var altKey = $"{request.Method}:{request.RequestUri}";
            if (_responses.TryGetValue(altKey, out response))
                return Task.FromResult(response);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        public new void Dispose()
        {
            foreach (var response in _responses.Values)
            {
                response.Dispose();
            }
            _responses.Clear();
            base.Dispose();
        }
    }

    private class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler = handler;

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler);
        }
    }
}
