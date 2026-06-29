using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PRM.Server.Services.Ai;

namespace PRM.Tests.Services;

public class CustomGenerateClientTests
{
	[Fact]
	public async Task GenerateResponseAsync_PostsGenerateRequestAndReturnsResponseText()
	{
		var handler = new CapturingHandler("""
			{
			  "model": "gemma3:12b-it-q8_0",
			  "response": "{\"summary\":\"ok\"}",
			  "done": true
			}
			""");
		var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("http://localhost:11434")
		};
		var client = new CustomGenerateClient(
			httpClient,
			BuildConfiguration());

		var result = await client.GenerateResponseAsync("Hello", "test-key");

		result.Should().Be("{\"summary\":\"ok\"}");
		handler.RequestUri.Should().Be("http://localhost:11434/api/generate");
		handler.ApiKey.Should().Be("test-key");
		handler.RequestBody.Should().Contain("\"model\":\"gemma3:12b-it-q8_0\"");
		handler.RequestBody.Should().Contain("\"prompt\":\"Hello\"");
		handler.RequestBody.Should().Contain("\"stream\":false");
	}

	[Fact]
	public void LlmClientFactory_WithCustomProvider_ReturnsCustomGenerateClient()
	{
		var httpClient = new HttpClient(new CapturingHandler("{}"))
		{
			BaseAddress = new Uri("http://localhost:11434")
		};
		var registration = new CustomLlmClientRegistration(
			new StaticHttpClientFactory(httpClient),
			BuildConfiguration());
		var factory = new LlmClientFactory([registration]);

		var client = factory.Create("Custom");

		client.Should().BeOfType<CustomGenerateClient>();
	}

	[Fact]
	public async Task GenerateResponseAsync_WithRemoteHttpAndAllowInsecureHttp_PostsRequest()
	{
		var handler = new CapturingHandler("""{"response":"ok"}""");
		var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("http://164.52.211.238/api/generate")
		};
		var client = new CustomGenerateClient(
			httpClient,
			BuildConfiguration(new Dictionary<string, string?>
			{
				["Llm:Custom:AllowInsecureHttp"] = "true"
			}));

		var result = await client.GenerateResponseAsync("Hello", "test-key");

		result.Should().Be("ok");
		handler.RequestUri.Should().Be("http://164.52.211.238/api/generate");
	}

	[Fact]
	public async Task GenerateResponseAsync_WithRemoteHttpWithoutAllowInsecureHttp_Throws()
	{
		var httpClient = new HttpClient(new CapturingHandler("""{"response":"ok"}"""))
		{
			BaseAddress = new Uri("http://164.52.211.238/api/generate")
		};
		var client = new CustomGenerateClient(
			httpClient,
			BuildConfiguration());

		Func<Task> act = async () => await client.GenerateResponseAsync("Hello", "test-key");

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*AllowInsecureHttp*");
	}

	private static IConfiguration BuildConfiguration(Dictionary<string, string?>? overrides = null)
	{
		var values = new Dictionary<string, string?>
		{
			["Llm:Custom:BaseUrl"] = "http://localhost:11434",
			["Llm:Custom:Model"] = "gemma3:12b-it-q8_0"
		};

		foreach (var (key, value) in overrides ?? [])
		{
			values[key] = value;
		}

		return new ConfigurationBuilder()
			.AddInMemoryCollection(values)
			.Build();
	}

	private sealed class CapturingHandler : HttpMessageHandler
	{
		private readonly string _responseBody;

		public CapturingHandler(string responseBody)
		{
			_responseBody = responseBody;
		}

		public string? RequestUri { get; private set; }
		public string? ApiKey { get; private set; }
		public string RequestBody { get; private set; } = string.Empty;

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			RequestUri = request.RequestUri?.ToString();
			ApiKey = request.Headers.TryGetValues("apikey", out var values) ? values.SingleOrDefault() : null;
			RequestBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
			};
		}
	}

	private sealed class StaticHttpClientFactory : IHttpClientFactory
	{
		private readonly HttpClient _httpClient;

		public StaticHttpClientFactory(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public HttpClient CreateClient(string name) => _httpClient;
	}
}
