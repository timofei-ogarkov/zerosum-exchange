using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
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
}