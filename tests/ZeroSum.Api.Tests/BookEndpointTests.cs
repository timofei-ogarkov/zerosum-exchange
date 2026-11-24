using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using ZeroSum.Engine;

namespace ZeroSum.Api.Tests;

public class BookEndpointTests
{
    [Fact]
    public async Task GetBook_EmptyBook_ReturnsNullBestPrices()
    {
      var factory = new WebApplicationFactory<Program>();
      var client = factory.CreateClient();

      var response = await client.GetAsync("/book");
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      var body = await response.Content.ReadAsStringAsync();
      
      Assert.Contains("\"bestBid\":null", body); 
      Assert.Contains("\"bestAsk\":null", body); 
    }

    [Fact]
    public async Task PostOrder_LimitSell_ShowsUpAsBestAsk()
    {
      var factory = new WebApplicationFactory<Program>();
      var client = factory.CreateClient();

      var response = await client.PostAsJsonAsync("/orders", new {side = "Sell", type = "Limit", price = 100.50m, quantity = 5m});

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      var body = await client.GetStringAsync("/book");
      Assert.Contains("\"bestAsk\":100.50", body);
    }
    [Fact]
    public async Task PostOrder_PriceOffTick_ReturnsBadRequest()
    {
      var factory = new WebApplicationFactory<Program>();
      var client = factory.CreateClient();

      var response = await client.PostAsJsonAsync("/orders", new {side = "Buy", type = "Limit", price = 100.005m, quantity = 5m});
      Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task DeleteOrder_ExistingOrder_ReturnsOkAndRemovesFromBook()
    {
      var factory = new WebApplicationFactory<Program>();
      var client = factory.CreateClient();

      var response = await client.PostAsJsonAsync("/orders", new {side = "Sell", type = "Limit", price = 101.50m, quantity = 10m});

      var body = await response.Content.ReadFromJsonAsync<JsonElement>();
      long orderId = body.GetProperty("orderId").GetInt64();

      var deleteResponse = await client.DeleteAsync($"/orders/{orderId}");

      Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
      var bookBody = await client.GetStringAsync("/book");
      Assert.Contains("\"bestAsk\":null", bookBody);
    }
    [Fact]
    public async Task DeleteOrder_UnknownId_ReturnsNotFound()
    {
      var factory = new WebApplicationFactory<Program>();
      var client = factory.CreateClient();

      var response = await client.DeleteAsync("/orders/999");

      Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}