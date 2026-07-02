using System.Threading;
using System.Threading.Tasks;

public enum ActivityMode
{
    Inactive, Activating, Active, Deactivating
}

public interface IActivity
{
    ActivityMode Mode { get; }
    Task ActivateAsync(CancellationToken ct);
    Task DeactivateAsync(CancellationToken ct);
}

/// <summary>
/// ステート遷移中に実行したい処理
/// </summary>
public abstract class Acitivity : IActivity
{
    public ActivityMode Mode { get; private set; } = ActivityMode.Inactive;

    public virtual async Task ActivateAsync(CancellationToken ct)
    {
        if (Mode != ActivityMode.Inactive) return;

        Mode = ActivityMode.Activating;
        await Task.CompletedTask;
        Mode = ActivityMode.Active;
    }

    public virtual async Task DeactivateAsync(CancellationToken ct)
    {
        if (Mode != ActivityMode.Active) return;

        Mode = ActivityMode.Deactivating;
        await Task.CompletedTask;
        Mode = ActivityMode.Inactive;
    }
}