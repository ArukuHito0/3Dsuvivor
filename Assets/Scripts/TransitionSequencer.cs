using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class TransitionSequencer
{
    public readonly StateMachine Machine;

    ISequence sequencer;
    Action nextPhase;
    (StateNode from, StateNode to)? pending;    // 保留中のステート遷移
    StateNode lastFrom, lastTo;

    public TransitionSequencer(StateMachine machine)
    {
        Machine = machine;
    }

    public void RequestTransition(StateNode from, StateNode to)
    {
        if(to == null || from == to) return;

        if (sequencer != null)
        {
            pending = (from, to);
            return;
        }
        BeginTransition(from, to);
    }

    /// <summary>
    /// 渡されたノードチェインのアクティビティをフェーズステップとしてリスト化して返す
    /// <br>deactivate == true: 非活性化させるアクティビティをリスト化</br>
    /// <br>deactivate == false: 活性化させるアクティビティをリスト化</br>
    /// </summary>
    /// <param name="chain"></param>
    /// <param name="deactivate"></param>
    /// <returns></returns>
    private static List<PhaseStep> GatherPhaseSteps(List<StateNode> chain, bool deactivate)
    {
        var steps = new List<PhaseStep>();

        for (int i = 0; i < chain.Count; i++)
        {
            var acts = chain[i].Activities; // ノードの全てのアクティビティを取得

            for (int j = 0; j < acts.Count; j++)
            {
                var a = acts[j];

                if (deactivate)
                {
                    if (a.Mode == ActivityMode.Active) steps.Add(ct => a.DeactivateAsync(ct));  // 非活性化させるアクティビティを追加
                }
                else
                {
                    if(a.Mode == ActivityMode.Inactive) steps.Add(ct => a.ActivateAsync(ct));   // 活性化させるアクティビティを追加
                }
            }
        }

        return steps;
    }

    // fromから親ノードまでルートをたどり、通ったノードをリスト化して返す(from, …, lca)
    private static List<StateNode> NodesToExit(StateNode from, StateNode lca)
    {
        var list = new List<StateNode>();
        for(var s = from; s != null && s != lca; s = s.Parent)
            list.Add(s);
        return list;
    }

    // 親ノードからtoまでルートをたどり、通ったノードをリスト化して返す(lca, …, to)
    private static List<StateNode> NodesToEnter(StateNode to, StateNode lca)
    {
        var stack = new Stack<StateNode>();
        for(var s = to; s != lca; s = s.Parent)
            stack.Push(s);
        return new List<StateNode>(stack);
    }

    private CancellationTokenSource cts;
    public readonly bool UseSequential = true;  // falseの場合、並行処理を行う

    /// <summary>
    /// 直列または並列で非同期処理を行うシーケンスを返す
    /// </summary>
    /// <param name="steps"></param>
    /// <param name="sequential"></param>
    /// <returns>
    /// <br>SequentialPhase</br>
    /// <br>ParallelPhase</br>
    /// </returns>
    private ISequence GetSequencer(List<PhaseStep> steps, bool sequential)
    {
        return sequential
            ? new SequentialPhase(steps, cts.Token)
            : new ParallelPhase(steps, cts.Token);
    }

    // 遷移開始
    private void BeginTransition(StateNode from, StateNode to)
    {
        var lca = Lca(from, to);                   // from,to の共通の親ノードを取得
        var exitChain = NodesToExit(from, lca);    // fromから共通の親まで遡って、通ったノード全てを取得(降順)
        var enterChain = NodesToEnter(to, lca);    // 共通の親からtoまで追跡し、通ったノードを全て取得(昇順)
        
        // 遷移元のブランチを非活性化させるステップのリストを取得
        var exitSteps = GatherPhaseSteps(exitChain, deactivate: true);

        // シーケンスを開始
        sequencer = GetSequencer(exitSteps, UseSequential);
        sequencer.Start();

        // 非活性化シーケンスが終了した後、次のシーケンスを実行
        nextPhase = () =>
        {
            Machine.ChangeState(from, to);  // ステートを遷移

            // 遷移先のブランチを活性化させるステップのリストを取得
            var enterSteps = GatherPhaseSteps(enterChain, deactivate: false);

            // シーケンスを開始
            sequencer = GetSequencer(enterSteps, UseSequential);
            sequencer.Start();
        };
    }

    // 遷移終了
    private void EndTransition()
    {
        sequencer = null;

        // 保留中のステート遷移がある場合、実行する
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
        int min = Mathf.Min(a.PathToRootCache.Count, b.PathToRootCache.Count);

        StateNode lca = null;

        for (int i = 0; i < min; i++)
        {
            if (a.PathToRootCache[i] == b.PathToRootCache[i])
                lca = a.PathToRootCache[i];
            else
                break;
        }

        return lca;
    }
}
