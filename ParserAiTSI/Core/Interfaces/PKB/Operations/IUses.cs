namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;

    internal interface IUses
    {
        IEnumerable<(int, INode)> UsesTable { get; }
        void SetUses(INode statement, IVariableNode variable);
        IEnumerable<(int, INode)> GetUses(IVariableNode variable);
        IEnumerable<(int, IVariableNode)> GetUsed(INode statement);
        bool isUsed(INode statement, IVariableNode variable);
    }
}