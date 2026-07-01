using System.Collections;
using System.Collections.Generic;

public abstract class State : IState
{
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void OnFixedUpdate(float deltaTime) { }
    public virtual void OnUpdate(float deltaTime) { }
}
