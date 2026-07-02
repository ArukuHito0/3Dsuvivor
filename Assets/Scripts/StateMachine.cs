using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class StateMachine
{
    private HashSet<ITransition> anyTranditions = new HashSet<ITransition>();
    private Dictionary<Type, StateNode> nodes = new Dictionary<Type, StateNode>();  // 各ステートのノードを保存している辞書

    public readonly StateNode Root;
    public readonly TransitionSequencer Sequencer;
    bool isStarted = false;

    public StateMachine(StateNode root)
    {
        Root = root;
        Sequencer = new TransitionSequencer(this);
    }

    public void Start()
    {
        if(isStarted) return;

        isStarted = true;
        Root.Enter();
    }

    public void Tick(float deltaTime)
    {
        if (!isStarted)
        {
            Start();
        }

        Sequencer.Tick(deltaTime);
    }

    internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

    public void FixedTick(float deltaTime)
    {
        InternalFixedTick(deltaTime);
    }

    internal void InternalFixedTick(float deltaTime) => Root.FixedUpdate(deltaTime);

    public void ChangeState(StateNode from, StateNode to)
    {
        if(from == null || to == null || from.State == to) return;

        var fromPath = from.PathToRootCache;
        var toPath = to.PathToRootCache;

        GetDiff(fromPath, toPath, out var exitList, out var enterList);

        for (int i = 0; i < exitList.Count; i++)
        {
            exitList[i].Exit();
        }

        for (int i = enterList.Count - 1; i >= 0; i--)
        {
            enterList[i].Enter();
        }
    }

    /// <summary>
    /// fromノードにtoノードへの遷移を追加する
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="condition"></param>
    public void AddTransition(IState from, IState to, IPredicate condition)
    {
        GetOrAddNode(from).AddTransition(to, condition);
    }

    /// <summary>
    /// 全てのノードからtoノードへの遷移を追加する
    /// </summary>
    /// <param name="to"></param>
    /// <param name="condition"></param>
    public void AddAnyTransition(IState to, IPredicate condition)
    {
        anyTranditions.Add(new Transition(to, condition));
    }
    
    public ITransition GetAnyTransition()
    {
        foreach (var transition in anyTranditions)
            if (transition.Condition.Evaluate())
                return transition;

        return null;
    }

    /// <summary>
    /// 引数として受け取ったステートのノードを返す。対象のノードがない場合、作成してから返す
    /// </summary>
    /// <param name="state"></param>
    /// <returns>StateNode</returns>
    public StateNode GetOrAddNode(IState state)
    {
        var stateType = state.GetType();

        if (!nodes.ContainsKey(stateType))
        {
            nodes.Add(stateType, new StateNode(this, state));
        }

        return nodes[stateType];
    }

    // 各ステートノードのパスの差分のリストを取得する
    // Root.Ground.Move → Root.Ground.Attack
    // 返り値 exitList:Move, enterList:Attack
    private static void GetDiff(
        List<StateNode> fromPath,
        List<StateNode> toPath,
        out List<StateNode> exitList,
        out List<StateNode> enterList)
    {
        int min = Mathf.Min(fromPath.Count, toPath.Count);

        int idx = 0;
        while (idx < min && fromPath[idx] == toPath[idx])
            idx++;

        exitList = new List<StateNode>();
        enterList = new List<StateNode>();

        for (int i = fromPath.Count - 1; i > idx; i--)
        {
            exitList.Add(fromPath[i]);
        }

        for (int i = idx + 1; i < toPath.Count; i++)
        {
            enterList.Add(toPath[i]);
        }
    }
}
