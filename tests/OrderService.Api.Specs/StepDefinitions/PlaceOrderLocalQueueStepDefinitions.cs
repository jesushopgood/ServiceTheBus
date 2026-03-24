using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using OrderService.Api.Specs.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace OrderService.Api.Specs.StepDefinitions;

[Binding]
public sealed class PlaceOrderLocalQueueStepDefinitions : IDisposable
{
    private static readonly Lazy<PlaceOrderLocalQueueWebApplicationFactory> Factory =
        new(() => new PlaceOrderLocalQueueWebApplicationFactory());

    private HttpClient? _httpClient;
    private HttpResponseMessage? _response;
    private Guid _orderId;

    [When("I place an order with total items (.*) using local queue strategy")]
    public async Task WhenIPlaceAnOrderWithTotalItemsUsingLocalQueueStrategy(int totalItems)
    {
        if (!await IsPostgresAvailableAsync())
        {
            throw Xunit.Sdk.SkipException.ForSkip("PostgreSQL is not available on localhost:5432. Start Postgres to run local queue integration scenarios.");
        }

        _httpClient ??= Factory.Value.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        _response = await _httpClient.PostAsync($"/api/order/place-order?totalItems={totalItems}", content: null);

        if (_response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var content = await _response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("orderId", out var orderIdElement) && orderIdElement.TryGetGuid(out var orderId))
            {
                _orderId = orderId;
            }
        }
    }

    [Then("the place order response status code should be (.*)")]
    public void ThenThePlaceOrderResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)expectedStatusCode, _response!.StatusCode);
    }

    [Then("the place order response should contain a valid order id")]
    public void ThenThePlaceOrderResponseShouldContainAValidOrderId()
    {
        Assert.NotEqual(Guid.Empty, _orderId);
    }

    [Then("the local OrdersTopic should contain (.*) messages for that order")]
    public async Task ThenTheLocalOrdersTopicShouldContainMessagesForThatOrder(int expectedCount)
    {
        Assert.NotEqual(Guid.Empty, _orderId);

        await using var connection = new NpgsqlConnection(PlaceOrderLocalQueueWebApplicationFactory.TestConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM \"OrdersTopic\" WHERE \"OrderId\" = @orderId",
            connection);
        command.Parameters.AddWithValue("orderId", _orderId);

        var result = await command.ExecuteScalarAsync();
        var count = Convert.ToInt32(result);

        Assert.Equal(expectedCount, count);
    }

    private static async Task<bool> IsPostgresAvailableAsync()
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(PlaceOrderLocalQueueWebApplicationFactory.TestConnectionString)
            {
                Timeout = 2,
                CommandTimeout = 2
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _response?.Dispose();
        _httpClient?.Dispose();
    }
}
