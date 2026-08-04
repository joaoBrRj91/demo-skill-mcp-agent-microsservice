using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JL.Commerce.Tecnology.Service.IntegrationTests.Endpoints;

/// <summary>No-op <see cref="IEventBus"/> so integration tests do not trigger async
/// order processing via the MassTransit consumer.</summary>
internal sealed class NoOpEventBus : IEventBus
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent => Task.CompletedTask;
}

public sealed class OrderEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove DbContextOptions AND provider-specific IDbContextOptionsConfiguration
                // services registered by UseNpgsql. Without removing both, EF Core will have two
                // providers (Npgsql + InMemory) registered simultaneously and throw at request time.
                var toRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        (d.ServiceType.IsGenericType &&
                         d.ServiceType.GetGenericArguments().Length == 1 &&
                         d.ServiceType.GetGenericArguments()[0] == typeof(AppDbContext) &&
                         d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")))
                    .ToList();

                foreach (var d in toRemove)
                    services.Remove(d);

                // Capture the database name ONCE. Evaluating Guid.NewGuid() inside the
                // options lambda would produce a new name per scoped DbContext, so POST
                // and GET would hit different in-memory stores.
                var dbName = $"IntegrationTestDb_{Guid.NewGuid()}";
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // Neutralize the event bus so publishing OrderCreatedEvent does not drive
                // the MassTransit consumer through payment processing. Orders stay in the
                // Processing state, which is what the polling test asserts.
                services.AddScoped<IEventBus, NoOpEventBus>();
            });
        }).CreateClient();
    }

    private static object ValidPixPayload(Guid? transactionId = null) => new
    {
        transactionId = transactionId ?? Guid.NewGuid(),
        userId = Guid.NewGuid(),
        items = new[]
        {
            new { catalogProductId = Guid.NewGuid(), quantity = 2, unitPrice = 49.99m }
        },
        payment = new { method = "Pix", pixKey = "pix@test.com" },
        address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
    };

    private static object ValidCreditCardPayload() => new
    {
        transactionId = Guid.NewGuid(),
        userId = Guid.NewGuid(),
        items = new[]
        {
            new { catalogProductId = Guid.NewGuid(), quantity = 1, unitPrice = 100m }
        },
        payment = new
        {
            method = "CreditCard",
            cardNumber = "4111111111111111",
            holderName = "John Doe",
            expiry = "12/26",
            cvv = "123"
        },
        address = new { street = "Rua B", city = "SP", state = "SP", zipCode = "01001-000", country = "BR" }
    };

    [Fact]
    public async Task PostOrder_ValidPixPayload_Returns_202_Accepted()
    {
        // Arrange
        var payload = ValidPixPayload();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); // [CON-API-1, Spec scenario 1]
    }

    [Fact]
    public async Task PostOrder_ValidCreditCardPayload_Returns_202_Accepted()
    {
        // Arrange
        var payload = ValidCreditCardPayload();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); // [CON-API-1, Spec scenario 2]
    }

    [Fact]
    public async Task PostOrder_Returns_TransactionId_And_Processing_Status()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var payload = ValidPixPayload(transactionId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.GetProperty("transactionId").GetGuid()); // [CON-API-2]
        Assert.Equal("Processing", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostOrder_EmptyItems_Returns_422()
    {
        // Arrange
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = Array.Empty<object>(),
            payment = new { method = "Pix", pixKey = "pix@test.com" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); // [CON-DI-1, Spec scenario 8]
    }

    [Fact]
    public async Task PostOrder_MissingCreditCardNumber_Returns_422()
    {
        // Arrange
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = new[] { new { catalogProductId = Guid.NewGuid(), quantity = 1, unitPrice = 10m } },
            payment = new { method = "CreditCard", holderName = "John", expiry = "12/26", cvv = "123" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); // [Spec scenario 9]
    }

    [Fact]
    public async Task PostOrder_MissingPixKey_Returns_422()
    {
        // Arrange
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = new[] { new { catalogProductId = Guid.NewGuid(), quantity = 1, unitPrice = 10m } },
            payment = new { method = "Pix" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); // [Spec scenario 10]
    }

    [Fact]
    public async Task PostOrder_ItemQuantityBelowMinimum_Returns_422()
    {
        // Arrange
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = new[] { new { catalogProductId = Guid.NewGuid(), quantity = 0, unitPrice = 10m } },
            payment = new { method = "Pix", pixKey = "pix@test.com" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); // [Spec scenario 11]
    }

    [Fact]
    public async Task PostOrder_UnsupportedPaymentMethod_Returns_422()
    {
        // Arrange
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = new[] { new { catalogProductId = Guid.NewGuid(), quantity = 1, unitPrice = 10m } },
            payment = new { method = "BankTransfer" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); // [CON-DI-4, Spec scenario 14]
    }

    [Fact]
    public async Task GetOrderStatus_UnknownTransactionId_Returns_404()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/orders/{unknownId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // [CON-API-3, Spec scenario 7]
    }

    [Fact]
    public async Task GetOrderStatus_InvalidUuid_Returns_400()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/api/v1/orders/not-a-uuid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // [CON-API-6]
    }

    [Fact]
    public async Task GetOrderStatus_ProcessingOrder_Returns_200_With_Processing_Status()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/orders", ValidPixPayload(transactionId));

        // Act
        var response = await _client.GetAsync($"/api/v1/orders/{transactionId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // [Spec scenario 4]
        Assert.Equal("Processing", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostOrder_ErrorResponse_DoesNotExposeStackTrace()
    {
        // Arrange — empty items triggers 422
        var payload = new
        {
            transactionId = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            items = Array.Empty<object>(),
            payment = new { method = "Pix", pixKey = "pix@test.com" },
            address = new { street = "Rua A", city = "Rio", state = "RJ", zipCode = "20000-000", country = "BR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase); // [CON-SEC-3]
        Assert.DoesNotContain("at System.", body, StringComparison.OrdinalIgnoreCase);
    }
}
