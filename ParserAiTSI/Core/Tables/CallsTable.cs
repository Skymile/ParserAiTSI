using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.AST;
using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class CallsTable : TableBase<IProcedureNode, IProcedureNode>, ICalls
    {
        public IEnumerable<IProcedureNode> GetCalledFrom(IProcedureNode proc) => GetFrom(proc);
        public IEnumerable<IProcedureNode> GetCalls(IProcedureNode proc) => Get(proc);
        public bool IsCall(IProcedureNode proc1, IProcedureNode proc2) =>  Is(proc1, proc2);
        public bool IsCall(int first, int second)
        {
            if (first == -1 && second == -1)
            {
                return this.dict.Count != 0;
            }

            if (first == -1)
            {
                foreach (var item in this.dict)
                {
                    foreach (var value in item.Value)
                    {
                        if (value.Id == second)
                            return true;
                    }
                }
                return false;
            }

            if (this.dict.Keys.FirstOrDefault(x => x.Id == first) == null)
                return false;

            if (second == -1)
            {
                return this.dict.FirstOrDefault(x => x.Key.Id == first).Value.Count != 0;
            }

            foreach (var item in this.dict.FirstOrDefault(x => x.Key.Id == first).Value)
            {
                if (item.Id == second)
                    return true;
            }
            
            return false;
        }

        public void SetCall(IProcedureNode proc1, IProcedureNode proc2) => Set(proc1, proc2);
    }
}
