using System.Collections.Generic;

using Core.Interfaces.AST;

namespace Core.Interfaces.PQL
{
    public interface ICalls
    {
        void SetCall(IProcedureNode proc1, IProcedureNode proc2);

        IEnumerable<IProcedureNode> GetCalls(IProcedureNode proc);

        IEnumerable<IProcedureNode> GetCalledFrom(IProcedureNode proc);

        bool IsCall(IProcedureNode proc1, IProcedureNode proc2);
        bool IsCall(int first, int second);
    }
}
 