using System.Text.Json;

namespace CharityCrm.Core.Abstractions;

/// <summary>Collects field changes for the audit log as <c>{ "field": { "old": ..., "new": ... } }</c>.</summary>
public sealed class AuditChanges
{
    private readonly Dictionary<string, object?> _changes = new();

    public bool IsEmpty => _changes.Count == 0;

    /// <summary>Records the field only if the value actually changed.</summary>
    public AuditChanges Track<T>(string field, T oldValue, T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            _changes[field] = new { old = Format(oldValue), @new = Format(newValue) };
        }
        return this;
    }

    /// <summary>Records a value for a newly created entity (no old value).</summary>
    public AuditChanges Set<T>(string field, T value)
    {
        _changes[field] = new { old = (object?)null, @new = Format(value) };
        return this;
    }

    public string ToJson() => JsonSerializer.Serialize(_changes);

    private static object? Format<T>(T value) => value switch
    {
        null => null,
        Enum e => e.ToString(),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        _ => value,
    };
}
