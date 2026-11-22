using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;

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
}