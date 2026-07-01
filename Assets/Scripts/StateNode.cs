using System.Collections.Generic;

/// <summary>
/// ステートのノード
/// </summary>
public class StateNode
{
    public IState State { get; }    // ステート

    public HashSet<ITransition> transitions;     // 遷移

    public readonly StateMachine Machine;
    public readonly StateNode Parent;
    public StateNode ActiveChild;
    private IState initState;

    /// <summary>
    /// 遷移先を追加する関数
    /// </summary>
    /// <param name="to"></param>
    /// <param name="condition"></param>
    public void AddTransition(IState to, IPredicate condition)
    {
        transitions.Add(new Transition(to, condition));
    }

    /// <summary>
    /// ステートをセットするコンストラクタ
    /// </summary>
    /// <param name="state"></param>
    public StateNode(StateMachine machine, IState state, IState init = null, StateNode parent = null)
    {
        State = state;
        Machine = machine;
        Parent = parent;
        initState = init;

        transitions = new HashSet<ITransition>();
    }

    public StateNode GetInitialNode()
    {
        if(initState == null) return null;

        return Machine.GetOrAddNode(initState);
    }

    public bool TryGetTransition(out ITransition transition)
    {
        transition = Machine.GetTransition();
        
        if(transition == null) return false;
        else return true;
    }

    public void Enter()
    {
        if (Parent != null) Parent.ActiveChild = this;

        this.State.OnEnter();

        StateNode init = GetInitialNode();
        if (init != null) init.Enter();
    }

    public void Exit()
    {
        if (ActiveChild != null) ActiveChild.Exit();
        ActiveChild = null;
        this.State.OnExit();
    }

    public void Update(float deltaTime)
    {
        if (TryGetTransition(out ITransition transition))
        {
            IState t = transition.To;
            if (t != null)
            {
                Machine.Sequencer.RequestTransition(State, t);
                return;
            }
        }

        if (ActiveChild != null) ActiveChild.Update(deltaTime);
        this.State.OnUpdate(deltaTime);
    }

    public void FixedUpdate(float deltaTime)
    {
        if (ActiveChild != null) ActiveChild.FixedUpdate(deltaTime);
        this.State.OnFixedUpdate(deltaTime);
    }

    public StateNode Leaf()
    {
        StateNode s = this;
        while (s.ActiveChild != null) s = s.ActiveChild;
        return s;
    }

    public IEnumerable<StateNode> PathToRoot()
    {
        for (StateNode state = this; state != null; state = state.Parent) yield return state;
    }
}