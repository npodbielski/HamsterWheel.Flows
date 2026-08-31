using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.Flows.IntegrationTests.Fixtures;
using HamsterWheel.Flows.IntegrationTests.Flows;

namespace HamsterWheel.Flows.IntegrationTests;

public class FlowApiTests
{
    [Fact]
    public async Task WhenRunFlowEndpointCalled_ThenFlowRunsAndReturnsOutput()
    {
        //arrange
        var app = FlowApiBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/flows/{nameof(GreetFlow)}/run", content: null);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json!.RootElement.GetProperty("flowName").GetString().Should().Be(nameof(GreetFlow));
        json.RootElement.GetProperty("output").GetProperty("greeting").GetString().Should().Be("hello world");
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenRunFlowEndpointCalledWithUnknownFlow_ThenReturnsNotFound()
    {
        //arrange
        var app = FlowApiBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync("/api/flows/UnknownFlow/run", content: null);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await app.StopAsync();
    }

    [Fact]
    public async Task WhenRunFlowEndpointCalledWithFailingFlow_ThenReturnsServerError()
    {
        //arrange
        var app = FlowApiBuilder.Build();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        //act
        var response = await client.PostAsync($"/api/flows/{nameof(FailingFlow)}/run", content: null);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await app.StopAsync();
    }
}
