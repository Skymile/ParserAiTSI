using System.Collections.Generic;

using Core.Interfaces.AST;
using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class CallsTable : TableBase<IProcedureNode, IProcedureNode>, ICalls
    {
        public IEnumerable<IProcedureNode> GetCalledFrom(IProcedureNode proc) => GetFrom(proc);
        public IEnumerable<IProcedureNode> GetCalls(IProcedureNode proc) => Get(proc);
        public bool IsCall(IProcedureNode proc1, IProcedureNode proc2) =>  Is(proc1, proc2);
        public void SetCall(IProcedureNode proc1, IProcedureNode proc2) => Set(proc1, proc2);

        public void Test()
        {
            var n1 = new ProcedureNode { Id = 1 };
            var n2 = new ProcedureNode { Id = 2 };
            var n3 = new ProcedureNode { Id = 3 };
            var n4 = new ProcedureNode { Id = 4 };

            SetCall(n1, n2);
            SetCall(n3, n2);
            SetCall(n3, n4);

            var nodes = GetCalls(n3);
            var nodes2 = GetCalledFrom(n2);

            bool test1 = IsCall(n2, n3);
            bool test2 = IsCall(n1, n3);
            bool test3 = IsCall(n4, n3);
            bool test4 = IsCall(n1, n2);
            bool test5 = IsCall(n3, n4);
        }
    }
}
