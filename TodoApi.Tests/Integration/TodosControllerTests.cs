using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Tests.Integration;

public class TodosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public TodosControllerTests(WebApplicationFactory<Program> factory)
    {
        // Override the DB with an in-memory one so tests don't touch real SQLite
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TodoDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database
                services.AddDbContext<TodoDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_api_todos_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task POST_api_todos_CreatesItem_ReturnsCreated()
    {
        var newItem = new TodoItem { Title = "Integration test task" };

        var response = await _client.PostAsJsonAsync("/api/todos", newItem);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.NotNull(created);
        Assert.Equal("Integration test task", created.Title);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task GET_api_todos_id_ExistingItem_ReturnsOk()
    {
        // Arrange — seed via the API itself
        var created = await CreateTodoAsync("Find me");

        var response = await _client.GetAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GET_api_todos_id_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/todos/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_api_todos_id_UpdatesItem_ReturnsOk()
    {
        var created = await CreateTodoAsync("Before update");

        var updated = new TodoItem { Title = "After update", IsCompleted = true };
        var response = await _client.PutAsJsonAsync($"/api/todos/{created.Id}", updated);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("After update", result!.Title);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task DELETE_api_todos_id_ExistingItem_ReturnsNoContent()
    {
        var created = await CreateTodoAsync("Delete me");

        var response = await _client.DeleteAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_api_todos_id_NonExisting_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/todos/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Helper: creates a todo via the API and returns the created item
    private async Task<TodoItem> CreateTodoAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new TodoItem { Title = title });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
    }
}