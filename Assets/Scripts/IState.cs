using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    List<IActivity> Activities { get; }
    void AddActivity(IActivity a);

    void OnEnter();
    void OnExit();

    void OnUpdate(float deltaTime);
    void OnFixedUpdate(float deltaTime);
}
