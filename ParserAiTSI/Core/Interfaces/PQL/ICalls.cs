using Core.Interfaces.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.PQL
{
    public interface ICalls
    {
        void SetCall(IProcedureNode proc1, IProcedureNode proc2);

        IEnumerable<IProcedureNode> GetCalls(IProcedureNode proc);

        IEnumerable<IProcedureNode> GetCalledFrom(IProcedureNode proc);

        bool isCall(IProcedureNode proc1, IProcedureNode proc2);
    }
}
