using System;

public class FuncPredicate : IPredicate
{
    readonly Func<bool> func;

    public bool Evaluate()
    {
        return func.Invoke();
    }

    public FuncPredicate(Func<bool> func)
    {
        this.func = func;
    }
}