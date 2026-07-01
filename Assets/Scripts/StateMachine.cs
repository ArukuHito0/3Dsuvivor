using System;
using System.Collections.Generic;

public class StateMachine
{
    private HashSet<ITransition> anyTransitions = new HashSet<ITransition>();       // どのステートからでも行うことの出来る遷移
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

    public void ChangeState(IState from, IState to)
    {
        if(from == to || from == null || to == null) return;

        var previousNode = GetOrAddNode(from);
        var nextNode = GetOrAddNode(to);

        StateNode lca = TransitionSequencer.Lca(previousNode, nextNode);    // from, toステートの共通の親ステートノードを取得

        for(StateNode s = previousNode; s != lca; s = s.Parent) s.Exit();   // 親ステートノードまで遡り、通ったノードのExitメソッドを実行

        // 遷移先のステートノードまで通るノードを格納しておくスタック
        var stack = new Stack<StateNode>();

        // 親ステートノードまで遡り、通ったノードをスタックに格納
        for (StateNode s = nextNode; s != lca; s = s.Parent)
        {
            stack.Push(s);  
        }

        // スタックに格納されているノードを親ノードから順番にEnterメソッドを実行
        while (stack.Count > 0)
        {
            stack.Pop().Enter();    
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
    /// 全てのノードからのtoノードへの遷移を追加する
    /// </summary>
    /// <param name="to"></param>
    /// <param name="condition"></param>
    public void AddAnyTransition(IState to, IPredicate condition)
    {
        anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    /// <summary>
    /// 遷移条件を満たしている遷移を返す
    /// </summary>
    /// <returns>ITransition</returns>
    public ITransition GetTransition()
    {
        foreach (var transition in anyTransitions)
        {
            if (transition.Condition.Evaluate())
            {
                return transition;
            }
        }

        foreach (var transition in Root.Leaf().transitions)
        {
            if (transition.Condition.Evaluate())
            {
                return transition;
            }
        }

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
}
