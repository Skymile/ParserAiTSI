namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;
    
    internal interface IModifies
    {
        void SetModifies(INode statement, IVariableNode var);

        IEnumerable<INode> GetModifies(INode var);

        IEnumerable<INode> GetModified(IVariableNode statement);

        bool isModifies(INode statement, IVariableNode var);
    }
}
