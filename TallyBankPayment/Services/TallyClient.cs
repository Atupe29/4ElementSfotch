using System.Net.Mime;
using System.Text;

namespace TallyBankPayment.Services;

public class TallyOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 9000;
    public string Company { get; set; } = string.Empty;
}

public interface ITallyClient
{
    Task<TallyResponse> SendToTallyAsync(string xmlData, TallyOptions options, CancellationToken cancellationToken = default);
}

public sealed class TallyClient : ITallyClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    public TallyClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TallyResponse> SendToTallyAsync(string xmlData, TallyOptions options, CancellationToken cancellationToken = default)
    {
        var url = $"http://{options.Host}:{options.Port}/tally/";
        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(xmlData, Encoding.UTF8, MediaTypeNames.Text.Xml)
        };
        request.Headers.Accept.ParseAdd(MediaTypeNames.Text.Xml);

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var status = (int)response.StatusCode;
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            return new TallyResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = status,
                ResponseText = responseText
            };
        }
        catch (Exception ex)
        {
            return new TallyResponse
            {
                Success = false,
                StatusCode = 0,
                ErrorMessage = ex.Message
            };
        }
    }
}

public sealed class TallyResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseText { get; set; }
    public string? ErrorMessage { get; set; }
}