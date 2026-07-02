using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public interface ISequence
{
    bool IsDone { get; }    // 終了フラグ
    void Start();           // シーケンス起動
    bool Update();          // シーケンス実行
}

// アクティビティの非同期処理( ActivateAsync / DeactivateAsync )を格納するデリゲート
public delegate Task PhaseStep(CancellationToken ct);

/// <summary>
/// アクティビティを並列実行するシーケンスクラス
/// </summary>
public class ParallelPhase : ISequence
{
    private readonly List<PhaseStep> steps;
    private readonly CancellationToken ct;
    private List<Task> tasks;   // 現在実行しているタスクを格納するリスト
    public bool IsDone { get; private set; }

    public ParallelPhase(List<PhaseStep> steps, CancellationToken ct)
    {
        this.steps = steps;
        this.ct = ct;
    }

    public void Start()
    {
        if (steps == null || steps.Count == 0)
        {
            IsDone = true;
            return;
        }

        tasks = new List<Task>(steps.Count);

        for (int i = 0; i < steps.Count; i++)
        {
            tasks.Add(steps[i](ct));    // 全てのステップのアクティビティを実行し、tasksに格納
        }
    }

    public bool Update()
    {
        if (IsDone) return true;

        // tasksに格納されているアクティビティが全て完了しているか、
        // tasksに何も格納されていないならシーケンスを終了させる
        IsDone = tasks == null || tasks.TrueForAll(t => t.IsCompleted);

        return IsDone;
    }
}

/// <summary>
/// アクティビティを直列実行するシーケンスクラス
/// </summary>
public class SequentialPhase : ISequence
{
    private readonly List<PhaseStep> steps; // 実行するステップ
    private readonly CancellationToken ct;
    private int index = -1;
    private Task current; // 現在実行しているアクティビティ

    public bool IsDone { get; private set;}

    public SequentialPhase(List<PhaseStep> steps, CancellationToken ct)
    {
        this.steps = steps;
        this.ct = ct;
    }

    public void Start() => Next();

    public bool Update()
    {
        if (IsDone) return true;

        // 現在実行中のタスクが無いか、完了していたら次のステップに進む
        if (current == null || current.IsCompleted) Next();

        return IsDone;
    }

    // シーケンスのステップを進める関数
    // 進めた時にそのステップが無ければシーケンスを終了させる
    private void Next()
    {
        index++;
        // 渡されたステップ数を超過しているのならば、シーケンスを終了
        if (index >= steps.Count)
        {
            IsDone = true;
            return;
        }
        current = steps[index](ct); // 現在のステップのアクティビティを実行
    }
}

public class NoopPhase : ISequence
{
    public bool IsDone { get; private set; }
    public void Start() => IsDone = true;
    public bool Update() => IsDone;
}
