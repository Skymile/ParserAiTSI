using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class ModifiesTable : TableBase<INode, IVariableNode>, IModifies
    {
        public IEnumerable<INode> GetModified(IVariableNode var) => GetFrom(var);
        public IEnumerable<INode> GetModifies(INode statement) => Get(statement);
        public bool IsModifies(INode statement, IVariableNode var) => Is(statement, var);
        public void SetModifies(INode statement, IVariableNode var) => Set(statement, var);
    }
}
