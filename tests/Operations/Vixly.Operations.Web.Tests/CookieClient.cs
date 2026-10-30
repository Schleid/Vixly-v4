using System.Net;
using System.Text.RegularExpressions;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: CookieClient.cs
//
// PURPOSE:
// A small browser for static forms: keeps cookies and posts forms with their antiforgery token.
//
// RESPONSIBILITIES:
// - Send requests with the cookies of the host
// - Read hidden inputs and post forms
//
// IMPORTANT NOTES:
// - Never follows redirects, so tests see each step
// =========================================================
internal sealed class CookieClient : IDisposable
{
    private static readonly Regex Antiforgery = new("name=\"__RequestVerificationToken\"[^>]*value=\"(?<value>[^\"]+)\"", RegexOptions.CultureInvariant);

    private readonly HttpClient _client;

    private readonly Uri _base;

    public CookieClient(HttpMessageHandler inner, Uri baseAddress)
    {
        _base = baseAddress;
        _client = new HttpClient(new CookieHandler(inner, Cookies)) { BaseAddress = baseAddress };
    }

    public CookieContainer Cookies { get; } = new();

    public string? Cookie(string name) => Cookies.GetCookies(_base)[name]?.Value;

    // Sent with every later request (a browser's user agent, Cloudflare's country); none removes it.
    public void Header(string name, string? value)
    {
        _client.DefaultRequestHeaders.Remove(name);

        if (value is not null)
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }
    }

    public async Task<(HttpResponseMessage Response, string Html)> GetAsync(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);
        return (response, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    // Posts a form the way the page would: with the antiforgery token from the page it came from.
    public async Task<(HttpResponseMessage Response, string Html)> PostAsync(string path, string fromHtml, params (string Name, string Value)[] fields)
    {
        string token = Antiforgery.Match(fromHtml).Groups["value"].Value;
        List<KeyValuePair<string, string>> form = fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)).ToList();
        form.Add(new KeyValuePair<string, string>("__RequestVerificationToken", token));
        HttpResponseMessage response = await _client.PostAsync(path, new FormUrlEncodedContent(form));
        return (response, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    // Posts one file the way a browser upload form would, with the antiforgery token from the page.
    public async Task<(HttpResponseMessage Response, string Html)> PostFileAsync(string path, string fromHtml, string fileName, string contentType, byte[] content)
    {
        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(Antiforgery.Match(fromHtml).Groups["value"].Value), "__RequestVerificationToken");
        HttpResponseMessage response = await _client.PostAsync(path, form);
        return (response, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    public async Task<(HttpResponseMessage Response, string Html)> PostFormWithFileAsync(string path, string fromHtml, string fileField, string fileName, string contentType, byte[] content, params (string Name, string Value)[] fields)
    {
        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, fileField, fileName);

        foreach ((string name, string value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        form.Add(new StringContent(Antiforgery.Match(fromHtml).Groups["value"].Value), "__RequestVerificationToken");
        HttpResponseMessage response = await _client.PostAsync(path, form);
        return (response, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    // Calls a page's JSON API the way its script does: the antiforgery token from the page goes in a header.
    public async Task<(HttpResponseMessage Response, System.Text.Json.JsonElement Json)> PostJsonAsync(string path, string fromHtml, object body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = System.Net.Http.Json.JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", Antiforgery.Match(fromHtml).Groups["value"].Value);
        HttpResponseMessage response = await _client.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        return (response, text.Length == 0 ? default : System.Text.Json.JsonDocument.Parse(text).RootElement.Clone());
    }

    // Posts a multipart form with several files (name, file name, content), with the antiforgery token from the page.
    public async Task<(HttpResponseMessage Response, string Html)> PostFilesAsync(string path, string fromHtml, (string Field, string FileName, byte[] Content)[] files, params (string Name, string Value)[] fields)
    {
        using MultipartFormDataContent form = new();

        foreach ((string field, string fileName, byte[] content) in files)
        {
            ByteArrayContent file = new(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            form.Add(file, field, fileName);
        }

        foreach ((string name, string value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        form.Add(new StringContent(Antiforgery.Match(fromHtml).Groups["value"].Value), "__RequestVerificationToken");
        HttpResponseMessage response = await _client.PostAsync(path, form);
        return (response, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    // The body as sent, not decoded: for checks on HTML encoding.
    public async Task<(HttpResponseMessage Response, string Raw)> GetRawAsync(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);
        return (response, await response.Content.ReadAsStringAsync());
    }

    public void Dispose() => _client.Dispose();

    private sealed class CookieHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies;

        public CookieHandler(HttpMessageHandler inner, CookieContainer cookies)
            : base(inner)
        {
            _cookies = cookies;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string header = _cookies.GetCookieHeader(request.RequestUri!);

            if (header.Length > 0)
            {
                request.Headers.Add("Cookie", header);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
            {
                foreach (string value in values)
                {
                    _cookies.SetCookies(request.RequestUri!, value);
                }
            }

            return response;
        }
    }
}
