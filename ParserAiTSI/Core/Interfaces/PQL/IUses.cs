namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;

    internal interface IUses
    {
        void SetUses(INode statement, IVariableNode var);

        IEnumerable<INode> GetUses(INode var);

        IEnumerable<INode> GetUsedBy(IVariableNode statement);

        bool isUses(INode statement, IVariableNode var);
    }
}
