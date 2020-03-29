namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;
    
    internal interface IModifies
    {
        IEnumerable<(int, INode)> ModifiesTable { get; }
        void SetModify(INode statement, IVariableNode variable);
        IEnumerable<(int, INode)> GetModifies(IVariableNode variable);
        IEnumerable<(int, IVariableNode)> GetModifies(INode statement);
        bool isModifies(INode statement, IVariableNode variable);
    }
}
