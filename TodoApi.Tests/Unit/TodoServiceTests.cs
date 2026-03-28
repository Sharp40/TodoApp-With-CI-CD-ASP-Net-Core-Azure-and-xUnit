using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests.Unit;

public class TodoServiceTests : IDisposable
{
    private readonly TodoDbContext _db;
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        // Each test gets its own in-memory database — fully isolated
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TodoDbContext(options);
        _service = new TodoService(_db);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        _db.Todos.AddRange(
            new TodoItem { Title = "Task 1" },
            new TodoItem { Title = "Task 2" }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        var item = new TodoItem { Title = "Buy milk" };
        _db.Todos.Add(item);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Buy milk", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AddsItemToDatabase()
    {
        var item = new TodoItem { Title = "New task" };

        var created = await _service.CreateAsync(item);

        Assert.True(created.Id > 0);
        Assert.Equal(1, await _db.Todos.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var item = new TodoItem { Title = "Old title" };
        _db.Todos.Add(item);
        await _db.SaveChangesAsync();

        var updated = await _service.UpdateAsync(item.Id, new TodoItem
        {
            Title = "New title",
            IsCompleted = true
        });

        Assert.NotNull(updated);
        Assert.Equal("New title", updated.Title);
        Assert.True(updated.IsCompleted);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.UpdateAsync(999, new TodoItem { Title = "X" });
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesItem()
    {
        var item = new TodoItem { Title = "Delete me" };
        _db.Todos.Add(item);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(item.Id);

        Assert.True(result);
        Assert.Equal(0, await _db.Todos.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);
        Assert.False(result);
    }

    public void Dispose() => _db.Dispose();
}