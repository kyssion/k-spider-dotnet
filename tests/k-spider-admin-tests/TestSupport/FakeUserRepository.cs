using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;

namespace KSpider.Admin.Tests.TestSupport;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<long, SysUserRecord> _users = new();
    private long _nextId = 1;

    public IReadOnlyCollection<SysUserRecord> Users => _users.Values;

    public void Seed(SysUserRecord user)
    {
        _users[user.Id] = user;
        _nextId = Math.Max(_nextId, user.Id + 1);
    }

    public Task<SysUserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.Values.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<SysUserRecord?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<SysUserRecord>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SysUserRecord>>(_users.Values.OrderBy(user => user.Id).ToList());
    }

    public Task<long> CreateAsync(
        string username,
        string passwordHash,
        string? displayName,
        CancellationToken cancellationToken)
    {
        var id = _nextId++;
        var now = DateTime.UtcNow;
        _users[id] = new SysUserRecord(id, username, passwordHash, displayName, "active", now, now);
        return Task.FromResult(id);
    }

    public Task<bool> UpdateDisplayNameAsync(long id, string? displayName, CancellationToken cancellationToken)
    {
        if (!_users.TryGetValue(id, out var user))
        {
            return Task.FromResult(false);
        }

        _users[id] = user with { DisplayName = displayName, UpdateTime = DateTime.UtcNow };
        return Task.FromResult(true);
    }

    public Task<bool> TryDisableAsync(long id, CancellationToken cancellationToken)
    {
        if (!_users.TryGetValue(id, out var user) ||
            !string.Equals(user.Status, "active", StringComparison.Ordinal) ||
            _users.Values.Count(item => string.Equals(item.Status, "active", StringComparison.Ordinal)) <= 1)
        {
            return Task.FromResult(false);
        }

        _users[id] = user with { Status = "disabled", UpdateTime = DateTime.UtcNow };
        return Task.FromResult(true);
    }

    public Task<bool> EnableAsync(long id, CancellationToken cancellationToken)
    {
        if (!_users.TryGetValue(id, out var user) ||
            !string.Equals(user.Status, "disabled", StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        _users[id] = user with { Status = "active", UpdateTime = DateTime.UtcNow };
        return Task.FromResult(true);
    }

    public Task<bool> UpdatePasswordHashAsync(long id, string passwordHash, CancellationToken cancellationToken)
    {
        if (!_users.TryGetValue(id, out var user))
        {
            return Task.FromResult(false);
        }

        _users[id] = user with { PasswordHash = passwordHash, UpdateTime = DateTime.UtcNow };
        return Task.FromResult(true);
    }
}
