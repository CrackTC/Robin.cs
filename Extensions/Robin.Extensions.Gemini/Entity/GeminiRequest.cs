using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Extensions.Gemini.Entity.Responses;

namespace Robin.Extensions.Gemini.Entity;

internal class GeminiRequest
{
    private static readonly HttpClient _httpClient = new();

    private readonly JsonSerializerOptions _serializerOptions = new();
    private readonly string _apiKey;
    private readonly string _apiVersion;
    private readonly string _model;
    private readonly string _apiPrefix;

    public GeminiRequest(
        string apiKey,
        string apiVersion = "v1beta",
        string model = "gemini-pro",
        string apiPrefix = "https://generativelanguage.googleapis.com"
    )
    {
        _apiKey = apiKey;
        _apiVersion = apiVersion;
        _model = model;
        _apiPrefix = apiPrefix;
        _serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    public async Task<GeminiResponse?> GenerateContentAsync(
        GeminiRequestBody body,
        CancellationToken token
    )
    {
        const string endpoint = "generateContent";

        var url = $"{_apiPrefix}/{_apiVersion}/models/{_model}:{endpoint}?key={_apiKey}";

        var response = await _httpClient.PostAsync(
            url,
            new StringContent(
                JsonSerializer.Serialize(body, _serializerOptions),
                Encoding.UTF8,
                "application/json"
            ),
            token
        );

        if (!response.IsSuccessStatusCode)
            return await JsonSerializer.DeserializeAsync<GeminiErrorResponse>(
                await response.Content.ReadAsStreamAsync(token),
                cancellationToken: token
            );

        return await JsonSerializer.DeserializeAsync<GeminiGenerateDataResponse>(
            await response.Content.ReadAsStreamAsync(token),
            _serializerOptions,
            cancellationToken: token
        );
    }
}
