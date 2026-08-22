using System.Collections.Concurrent;
using Ploch.CommandLine.Spectre.SampleApp.Services.Models;

namespace Ploch.CommandLine.Spectre.SampleApp.Services;

/// <summary>
///     In-memory implementation of <see cref="IUserService" /> for sample demonstrations.
/// </summary>
public class UserService : IUserService
{
    private readonly ConcurrentDictionary<int, UserProfile> _users = new();
    private int _nextId = 1;

    public UserService()
    {
        // Pre-populate with sample users
        AddInternal(new UserProfile(1, "Alice Smith", "alice.smith@example.com", "Administrator", true, DateTime.UtcNow.AddDays(-30)));
        AddInternal(new UserProfile(2, "Bob Jones", "bob.jones@example.com", "Developer", true, DateTime.UtcNow.AddDays(-15)));
        AddInternal(new UserProfile(3, "Charlie Brown", "charlie.brown@example.com", "Contributor", false, DateTime.UtcNow.AddDays(-5)));
        // The last seeded id: Interlocked.Increment returns the incremented value, so the first
        // generated id is 4 and the sequence stays contiguous with the seed data.
        _nextId = 3;
    }

    public Task<UserProfile> CreateUserAsync(string name, string email, string role, CancellationToken cancellationToken = default)
    {
        // A method that takes a token and never looks at it cannot be cancelled. Even a synchronous
        // in-memory implementation checks it, so callers get the behaviour the signature promises.
        cancellationToken.ThrowIfCancellationRequested();

        var id = Interlocked.Increment(ref _nextId);
        var user = new UserProfile(id, name, email, role, true, DateTime.UtcNow);
        _users[id] = user;

        return Task.FromResult(user);
    }

    public Task<IEnumerable<UserProfile>> GetUsersAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _users.Values.AsEnumerable();
        if (activeOnly)
        {
            query = query.Where(u => u.IsActive);
        }

        return Task.FromResult<IEnumerable<UserProfile>>(query.OrderBy(u => u.Id).ToList());
    }

    public Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removed = _users.TryRemove(id, out _);

        return Task.FromResult(removed);
    }

    private void AddInternal(UserProfile user)
    {
        _users[user.Id] = user;
    }
}
