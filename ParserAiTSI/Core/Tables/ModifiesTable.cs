using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class ModifiesTable : TableBase<INode, IVariableNode>, IModifies
    {
        public IEnumerable<INode> GetModified(IVariableNode var) => GetFrom(var);
        public IEnumerable<INode> GetModifies(INode statement) => Get(statement);
        public bool IsModifies(INode statement, IVariableNode var) => Is(statement, var);
        public void SetModifies(INode statement, IVariableNode var) => Set(statement, var);

        public void Test()
        {
            var n1 = new Node { Id = 1 };
            var v1 = new VariableNode { Name = "v1" };
            var n3 = new Node { Id = 3 };
            var v2 = new VariableNode { Name = "v2" };

            SetModifies(n1, v1);
            SetModifies(n3, v1);
            SetModifies(n3, v2);

            var nodes = GetModifies(n3);
            var nodes2 = GetModified(v1);

            bool test1 = IsModifies(n1, v1);
            bool test2 = IsModifies(n1, v2);
            bool test3 = IsModifies(n3, v1);
            bool test5 = IsModifies(n3, v2);
        }
    }
}
