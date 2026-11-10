using KSpider.Admin.Common.Json;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tasks;

namespace KSpider.Admin.Tests.TestSupport;

internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly Dictionary<long, SysTaskRecord> _tasks = new();
    private long _nextId = 1;

    public IReadOnlyCollection<SysTaskRecord> Tasks => _tasks.Values;

    public void Seed(SysTaskRecord task)
    {
        _tasks[task.Id] = task;
        _nextId = Math.Max(_nextId, task.Id + 1);
    }

    public Task<SysTaskRecord> InsertAsync(SysTaskInsert insert, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = new SysTaskRecord(
            _nextId++,
            insert.NodeId,
            insert.TaskType,
            insert.TaskDetailJson,
            insert.Status,
            insert.ExtraJson,
            now,
            now);
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<SysTaskRecord?> GetAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tasks.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SysTaskRecord>>(
            _tasks.Values.Where(task => !IsTerminal(task.Status)).ToList());
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindNonTerminalByNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SysTaskRecord>>(
            _tasks.Values
                .Where(task => !IsTerminal(task.Status) && task.NodeId == nodeId)
                .ToList());
    }

    public Task<IReadOnlyList<SysTaskRecord>> FindStaleAsync(
        DateTime beforeUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SysTaskRecord>>(
            _tasks.Values
                .Where(task => !IsTerminal(task.Status) && task.UpdateTime < beforeUtc)
                .OrderBy(task => task.UpdateTime)
                .ThenBy(task => task.Id)
                .Take(limit)
                .ToList());
    }

    public Task<IReadOnlyDictionary<long, DateTime>> GetLastRunTimesAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<long, DateTime>();
        foreach (var task in _tasks.Values)
        {
            var detail = AdminJson.Deserialize<TaskDetail>(task.TaskDetailJson);
            if (detail is null)
            {
                continue;
            }

            if (!result.TryGetValue(detail.ChannelId, out var last) || task.CreateTime > last)
            {
                result[detail.ChannelId] = task.CreateTime;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<long, DateTime>>(result);
    }

    public Task<bool> TryMarkPushedAsync(long id, string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateIf(id, "pushing", token, task => task with
        {
            Status = "pushed",
            UpdateTime = DateTime.UtcNow,
        }));
    }

    public Task<bool> TryMarkPushFailedAsync(
        long id,
        string token,
        string failure,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateIf(id, "pushing", token, task => task with
        {
            Status = "push_failed",
            UpdateTime = DateTime.UtcNow,
        }));
    }

    public Task<bool> TryCancelAsync(long id, string reason, CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateIf(id, null, null, task => task with
        {
            Status = "cancelled",
            UpdateTime = DateTime.UtcNow,
        }));
    }

    public Task<bool> TryRedispatchAsync(
        long id,
        Guid nodeId,
        string taskDetailJson,
        string token,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateIf(id, null, null, task =>
        {
            var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson) ?? new TaskExtraView();
            return task with
            {
                NodeId = nodeId,
                Status = "pushing",
                TaskDetailJson = taskDetailJson,
                ExtraJson = AdminJson.Serialize(new
                {
                    dispatch_token = token,
                    fail_count = extra.FailCount + 1,
                }),
                UpdateTime = DateTime.UtcNow,
            };
        }));
    }

    public Task<bool> TryAbandonAsync(long id, string reason, CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateIf(id, null, null, task => task with
        {
            Status = "abandoned",
            UpdateTime = DateTime.UtcNow,
        }));
    }

    public Task<int> DeleteTerminalOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = _tasks.Values
            .Where(task => IsTerminal(task.Status) && task.UpdateTime < cutoffUtc)
            .OrderBy(task => task.UpdateTime)
            .Take(batchSize)
            .ToList();
        foreach (var task in expired)
        {
            _tasks.Remove(task.Id);
        }

        return Task.FromResult(expired.Count);
    }

    public Task<PageResult<SysTaskRecord>> QueryAsync(TaskQuery query, CancellationToken cancellationToken)
    {
        var items = _tasks.Values
            .Where(task => query.Status is null || task.Status == query.Status)
            .Where(task => query.NodeId is null || task.NodeId == query.NodeId)
            .OrderByDescending(task => task.Id)
            .Skip((Math.Max(1, query.Page) - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();
        return Task.FromResult(new PageResult<SysTaskRecord>(items, query.Page, query.PageSize, items.Count));
    }

    private bool UpdateIf(
        long id,
        string? expectedStatus,
        string? expectedToken,
        Func<SysTaskRecord, SysTaskRecord> update)
    {
        if (!_tasks.TryGetValue(id, out var task))
        {
            return false;
        }

        if (expectedStatus is not null && task.Status != expectedStatus)
        {
            return false;
        }

        if (expectedToken is not null)
        {
            var extra = AdminJson.Deserialize<TaskExtraView>(task.ExtraJson);
            if (extra?.DispatchToken != expectedToken)
            {
                return false;
            }
        }

        _tasks[id] = update(task);
        return true;
    }

    private static bool IsTerminal(string status)
    {
        return status is "success" or "abandoned" or "cancelled";
    }
}
