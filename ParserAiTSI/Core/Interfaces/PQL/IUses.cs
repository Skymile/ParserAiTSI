using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
    internal interface IUses
    {
        void SetUses(INode statement, IVariableNode var);

        IEnumerable<INode> GetUses(INode var);

        IEnumerable<INode> GetUsedBy(IVariableNode statement);

        bool IsUses(INode statement, IVariableNode var);
    }
}
