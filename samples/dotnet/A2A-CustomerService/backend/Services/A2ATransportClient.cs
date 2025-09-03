using System.Net.Http.Json;
using System.Text.Json;
using A2A;

namespace A2ACustomerService.Services;

public interface IA2ATransportClient
{
    Task<Message> SendMessageAsync(string agentPath, Message message, CancellationToken cancellationToken = default);
}

internal sealed class A2ATransportClient : IA2ATransportClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _json;

    private const string JsonRpcVersion = "2.0";
    private const string MethodMessageSend = "message/send"; // per A2A JSON-RPC

    public A2ATransportClient(HttpClient httpClient, IConfiguration configuration)
    {
        _http = httpClient;
        _baseUrl = configuration["A2A:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<Message> SendMessageAsync(string agentPath, Message message, CancellationToken cancellationToken = default)
    {
        var url = _baseUrl + agentPath; // e.g., http://localhost:5000/billing

        var req = new JsonRpcRequest
        {
            Id = Guid.NewGuid().ToString(),
            Method = MethodMessageSend,
            Params = new MessageSendParamsWrapper { Message = message }
        };

        using var response = await _http.PostAsJsonAsync(url, req, _json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rpc = await response.Content.ReadFromJsonAsync<JsonRpcResponse<Message>>(_json, cancellationToken)
                  ?? throw new InvalidOperationException("Empty JSON-RPC response.");

        if (rpc.Error != null)
        {
            throw new InvalidOperationException($"A2A JSON-RPC error: {rpc.Error.Code} {rpc.Error.Message}");
        }

        return rpc.Result ?? throw new InvalidOperationException("A2A response missing result message.");
    }

    private sealed class JsonRpcRequest
    {
        public string Jsonrpc { get; set; } = JsonRpcVersion;
        public string Id { get; set; } = string.Empty;
        public string Method { get; set; } = MethodMessageSend;
        public object? Params { get; set; }
    }

    private sealed class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    private sealed class JsonRpcResponse<TResult>
    {
        public string Jsonrpc { get; set; } = JsonRpcVersion;
        public string? Id { get; set; }
        public TResult? Result { get; set; }
        public JsonRpcError? Error { get; set; }
    }

    private sealed class MessageSendParamsWrapper
    {
        public Message? Message { get; set; }
    }
}
