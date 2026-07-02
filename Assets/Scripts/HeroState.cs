using System.Collections;
using System.Collections.Generic;

public abstract class HeroState : IState
{
    protected HeroController ctx;

    public HeroState(HeroController ctx) => this.ctx = ctx;

    public List<IActivity> Activities { get; private set; } = new List<IActivity>();

    public void AddActivity(IActivity a)
    {
        if (a != null) Activities.Add(a);
    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void OnFixedUpdate(float deltaTime) { }
    public virtual void OnUpdate(float deltaTime) { }
}
