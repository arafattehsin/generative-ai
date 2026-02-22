namespace IncidentCommandCenter.Api.Services;

public sealed class SkillRunContextAccessor
{
    private readonly AsyncLocal<string?> _runId = new();

    public string? CurrentRunId => _runId.Value;

    public IDisposable Push(string? runId)
    {
        string? previous = _runId.Value;
        _runId.Value = runId;
        return new Scope(this, previous);
    }

    private sealed class Scope(SkillRunContextAccessor accessor, string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            accessor._runId.Value = previous;
            _disposed = true;
        }
    }
}
