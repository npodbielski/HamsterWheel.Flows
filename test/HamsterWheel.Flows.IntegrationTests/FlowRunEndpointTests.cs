using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.Flows.IntegrationTests.Fixtures;
using HamsterWheel.Flows.IntegrationTests.Flows;

namespace HamsterWheel.Flows.IntegrationTests;

/// <summary>
/// The package's MapFlowRunEndpoint + ChannelFlowRunStarter (in-process WebApplication)
/// </summary>
public class FlowRunEndpointTests
{
    [Fact]
    public async Task WhenRunEndpointCalled_ThenFlowRunsAndReturnsOutput()
    {
        //arrange
        var app = FlowRunEndpointBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/flows/{nameof(GreetFlow)}/run", content: null);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json!.RootElement.GetProperty("greeting").GetString().Should().Be("hello world");
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenRunEndpointCalledWithUnknownFlow_ThenReturnsNotFound()
    {
        //arrange
        var app = FlowRunEndpointBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync("/api/flows/UnknownFlow/run", content: null);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        json!.RootElement.GetProperty("flowName").GetString().Should().Be("UnknownFlow");
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenRunEndpointCalledWithFailingFlow_ThenReturnsServerError()
    {
        //arrange
        var app = FlowRunEndpointBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/flows/{nameof(FailingFlow)}/run", content: null);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenTypedRunEndpointCalledWithMatchingOutput_ThenReturnsOutput()
    {
        //arrange
        var app = FlowRunEndpointBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/typed/flows/{nameof(GreetFlow)}/run", content: null);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json!.RootElement.GetProperty("greeting").GetString().Should().Be("hello world");
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenTypedRunEndpointCalledWithMismatchedOutput_ThenReturnsServerError()
    {
        //arrange
        var app = FlowRunEndpointBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/typed/flows/{nameof(MismatchFlow)}/run", content: null);
        var body = await response.Content.ReadAsStringAsync();

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        body.Should().Contain($"Result of '{nameof(MismatchFlow)}' flow should be of type");
        body.Should().Contain(nameof(GreetFlowOutput));
        body.Should().Contain(nameof(MismatchFlowOutput));
        await app.StopAsync();
    }
}
