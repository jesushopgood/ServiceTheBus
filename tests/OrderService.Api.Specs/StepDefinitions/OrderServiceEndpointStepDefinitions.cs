using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderService.Api.Specs.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace OrderService.Api.Specs.StepDefinitions;

[Binding]
public sealed class OrderServiceEndpointStepDefinitions : IDisposable
{
    private static readonly Lazy<CustomWebApplicationFactory> Factory = new(() => new CustomWebApplicationFactory());

    private readonly HttpClient _httpClient;
    private HttpResponseMessage? _response;
    private JsonDocument? _responseJson;

    public OrderServiceEndpointStepDefinitions()
    {
        _httpClient = Factory.Value.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    [When("I send a GET request to \"(.*)\"")]
    public async Task WhenISendAGetRequestTo(string path)
    {
        _response = await _httpClient.GetAsync(path);

        if (_response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var content = await _response.Content.ReadAsStringAsync();
            _responseJson?.Dispose();
            _responseJson = JsonDocument.Parse(content);
        }
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)expectedStatusCode, _response!.StatusCode);
    }

    [Then("the ping response service should be \"(.*)\"")]
    public void ThenThePingResponseServiceShouldBe(string expectedService)
    {
        Assert.NotNull(_responseJson);
        var service = _responseJson!.RootElement.GetProperty("service").GetString();
        Assert.Equal(expectedService, service);
    }

    [Then("the ping response message should contain \"(.*)\"")]
    public void ThenThePingResponseMessageShouldContain(string expectedMessageText)
    {
        Assert.NotNull(_responseJson);
        var message = _responseJson!.RootElement.GetProperty("message").GetString();
        Assert.Contains(expectedMessageText, message, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the products response should contain at least (.*) product")]
    public void ThenTheProductsResponseShouldContainAtLeastProduct(int minimumCount)
    {
        Assert.NotNull(_responseJson);
        var products = _responseJson!.RootElement.EnumerateArray().ToList();
        Assert.True(products.Count >= minimumCount, $"Expected at least {minimumCount} products but got {products.Count}.");
    }

    [Then("the products response should contain sku \"(.*)\"")]
    public void ThenTheProductsResponseShouldContainSku(string expectedSku)
    {
        Assert.NotNull(_responseJson);

        var containsSku = _responseJson!.RootElement
            .EnumerateArray()
            .Any(product => string.Equals(
                product.GetProperty("sku").GetString(),
                expectedSku,
                StringComparison.OrdinalIgnoreCase));

        Assert.True(containsSku, $"Expected at least one product with sku '{expectedSku}'.");
    }

    [Then("the product response sku should be \"(.*)\"")]
    public void ThenTheProductResponseSkuShouldBe(string expectedSku)
    {
        Assert.NotNull(_responseJson);
        var sku = _responseJson!.RootElement.GetProperty("sku").GetString();
        Assert.Equal(expectedSku, sku);
    }

    public void Dispose()
    {
        _responseJson?.Dispose();
        _response?.Dispose();
        _httpClient.Dispose();
    }
}
