using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly TodoDbContext _db;

    public TodoService(TodoDbContext db) => _db = db;

    public async Task<IEnumerable<TodoItem>> GetAllAsync() =>
        await _db.Todos.ToListAsync();

    public async Task<TodoItem?> GetByIdAsync(int id) =>
        await _db.Todos.FindAsync(id);

    public async Task<TodoItem> CreateAsync(TodoItem item)
    {
        _db.Todos.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(int id, TodoItem updated)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return null;

        item.Title = updated.Title;
        item.Description = updated.Description;
        item.IsCompleted = updated.IsCompleted;

        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return false;

        _db.Todos.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}