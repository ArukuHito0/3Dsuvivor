using System.Collections.Generic;

/// <summary>
/// ステートのノード
/// </summary>
public class StateNode
{
    public IState State { get; }    // ステート
    public List<StateNode> PathToRootCache { get; private set; } = new List<StateNode>();

    public HashSet<ITransition> transitions;    // 特定のステートからの遷移

    public readonly StateMachine Machine;   // ステートマシン
    public readonly StateNode Parent;       // 親ノード
    public StateNode ActiveChild;           // 子ノード

    private IState initState;   // 初期ステート

    public IReadOnlyList<IActivity> Activities => State.Activities;

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
        
        BuildPathCache();

        transitions = new HashSet<ITransition>();
    }

    private void BuildPathCache()
    {
        PathToRootCache.Clear();

        var list = new List<StateNode>();

        StateNode current = this;

        while (current != null)
        {
            list.Add(current);
            current = current.Parent;
        }

        list.Reverse();

        PathToRootCache = list;
    }

    // 初期のステートノードを取得
    public StateNode GetInitialNode()
    {
        if(initState == null) return null;

        return Machine.GetOrAddNode(initState);
    }

    // 遷移先を取得
    private ITransition GetTransition()
    {
        var any = Machine.GetAnyTransition();
        if(any != null) return any;

        foreach(var transition in transitions)
            if(transition.Condition.Evaluate())
                return transition;

        return null;
    }

    // ノードに進入時処理
    public void Enter()
    {
        // 親ノードの子ノードにセット
        if (Parent != null) Parent.ActiveChild = this;

        // ステート進入時の処理を実行
        this.State.OnEnter();

        // 子ノードの初期ステートのを取得し、そのノードへ遷移する
        StateNode init = GetInitialNode();
        if (init != null) init.Enter();
    }

    // ノード離脱時処理
    public void Exit()
    {
        // 子ノードの離脱時処理を実行
        if (ActiveChild != null) ActiveChild.Exit();

        // 離脱時に子ノードへを空にする
        ActiveChild = null;

        // ステートの離脱時の処理を実行
        this.State.OnExit();
    }

    // ノードの毎フレーム処理
    public void Update(float deltaTime)
    {
        // 条件が真の遷移を取得し、シーケンサーに遷移をリクエスト
        var transition = GetTransition();
        if (transition != null)
        {
            IState t = transition.To;
            if (t != null)
            {
                Machine.Sequencer.RequestTransition(this, Machine.GetOrAddNode(t));
                return;
            }
        }

        // 子ノードがいるなら子ノードの毎フレーム処理を実行
        if (ActiveChild != null) ActiveChild.Update(deltaTime);

        // ステートの毎フレーム処理を実行
        this.State.OnUpdate(deltaTime);
    }

    // ノードの固定更新処理
    public void FixedUpdate(float deltaTime)
    {
        if (ActiveChild != null) ActiveChild.FixedUpdate(deltaTime);

        this.State.OnFixedUpdate(deltaTime);
    }
}