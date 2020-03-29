namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;

    public interface IUses
    {
        IEnumerable<(int, INode)> UsesTable { get; }
        void SetUses(INode statement, IVariable variable);
        IEnumerable<(int, INode)> GetUses(IVariable variable);
        IEnumerable<(int, IVariable)> GetUsed(INode statement);
        bool isUsed(INode statement, IVariable variable);
    }
}
