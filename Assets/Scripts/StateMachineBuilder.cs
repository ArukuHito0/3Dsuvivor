using System.Collections.Generic;
using System.Reflection;

public class StateMachineBuilder
{
    readonly StateNode root;

    public StateMachineBuilder(StateNode root)
    {
        this.root = root;
    }

    public StateMachine Build()
    {
        var m = new StateMachine(root);
        Wire(root, m, new HashSet<StateNode>());
        return m;
    }

    private void Wire(StateNode s, StateMachine m, HashSet<StateNode> visited)
    {
        if (s == null) return;
        if (!visited.Add(s)) return;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        var machineField = typeof(StateNode).GetField("Machine", flags);
        if(machineField != null) machineField.SetValue(s, m);

        foreach (var fld in s.GetType().GetFields(flags))
        {
            if (!typeof(StateNode).IsAssignableFrom(fld.FieldType)) continue;
            if (fld.Name == "Parent") continue;

            var child = (StateNode)fld.GetValue(s);
            if (child == null) continue;
            if(!ReferenceEquals(child.Parent, s)) continue;

            Wire(child, m, visited);
        }
    }
}