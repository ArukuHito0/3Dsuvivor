using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class TransitionSequencer
{
    public readonly StateMachine Machine;

    ISequence sequencer;
    Action nextPhase;
    (IState from, IState to)? pending;
    IState lastFrom, lastTo;

    public TransitionSequencer(StateMachine machine)
    {
        Machine = machine;
    }

    public void RequestTransition(IState from, IState to)
    {
        if(to == null || from == to) return;
        if (sequencer != null)
        {
            pending = (from, to);
            return;
        }
        BeginTransition(from, to);
    }

    private void BeginTransition(IState from, IState to)
    {
        sequencer = new NoopPhase();
        sequencer.Start();

        nextPhase = () =>
        {
            Machine.ChangeState(from, to);
            sequencer = new NoopPhase();
            sequencer.Start();
        };
    }


    private void EndTransition()
    {
        sequencer = null;

        if (pending.HasValue)
        {
            var p = pending.Value;
            pending = null;
            BeginTransition(p.from, p.to);
        }
    }

    public void Tick(float deltaTime)
    {
        if (sequencer != null)
        {
            if (sequencer.Update())
            {
                if (nextPhase != null)
                {
                    var n = nextPhase;
                    nextPhase = null;
                    n();
                }
                else
                {
                    EndTransition();
                }
            }
            return;
        }
        Machine.InternalTick(deltaTime);
    }

    /// <summary>
    /// 引数として渡された2つのステートノードの一番近い共通の親ノードを返す
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static StateNode Lca(StateNode a, StateNode b)
    {
        // aステートノードの親を全て格納
        var ap = new HashSet<StateNode>();
        for(var s = a; s != null; s = s.Parent) ap.Add(s);

        // bステートノードの親がハッシュセット内にある場合、それが最も近い共通の親なので返す
        for(var s = b; s != null; s = s.Parent)
            if(ap.Contains(s)) return s;

        return null;
    }
}

public interface ISequence
{
    bool IsDone { get; }
    void Start();
    bool Update();
}

public class NoopPhase : ISequence
{
    public bool IsDone { get; private set; }
    public void Start() => IsDone = true;
    public bool Update() => IsDone;
}

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