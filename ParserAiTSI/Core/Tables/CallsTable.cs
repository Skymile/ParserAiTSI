using System.Collections.Generic;

using Core.Interfaces.AST;
using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class CallsTable : ICalls
    {
        private Dictionary<IProcedureNode, List<IProcedureNode>> callsDictionary;

        public CallsTable()
        {
            callsDictionary = new Dictionary<IProcedureNode, List<IProcedureNode>>();
        }

        public IEnumerable<IProcedureNode> GetCalledFrom(IProcedureNode proc)
        {
            if (callsDictionary.ContainsKey(proc))
            {
                List<IProcedureNode> calls = new List<IProcedureNode>();
                callsDictionary.TryGetValue(proc, out calls);
                return calls;
            }
            return null;
        }

        public IEnumerable<IProcedureNode> GetCalls(IProcedureNode proc)
        {
            List<IProcedureNode> calls = new List<IProcedureNode>();

            foreach (var call in callsDictionary)
            {
                List<IProcedureNode> callsValues = new List<IProcedureNode>();
                callsDictionary.TryGetValue(call.Key, out callsValues);

                foreach (var val in callsValues)
                {
                    if (val == proc)
                    {
                        calls.Add(call.Key);
                    }
                }
            }

            return calls;
        }

        public bool isCall(IProcedureNode proc1, IProcedureNode proc2)
        {
            if (!callsDictionary.ContainsKey(proc2))
            {
                return false;
            }
            else
            {
                List<IProcedureNode> calls = new List<IProcedureNode>();
                callsDictionary.TryGetValue(proc2, out calls);

                return calls.Contains(proc1);
            }
        }

        public void SetCall(IProcedureNode proc1, IProcedureNode proc2)
        {
            if (!callsDictionary.ContainsKey(proc2))
            {
                List<IProcedureNode> calls1 = new List<IProcedureNode>();
                callsDictionary.Add(proc2, calls1);
            }

            List<IProcedureNode> calls2 = new List<IProcedureNode>();
            callsDictionary.TryGetValue(proc2, out calls2);
            if (!calls2.Contains(proc1))
            {
                calls2.Add(proc1);
            }
        }

        public void test()
        {
            ProcedureNode n1 = new ProcedureNode();
            n1.Id = 1;
            ProcedureNode n2 = new ProcedureNode();
            n2.Id = 2;
            ProcedureNode n3 = new ProcedureNode();
            n3.Id = 3;
            ProcedureNode n4 = new ProcedureNode();
            n4.Id = 4;
            SetCall(n1, n2);
            SetCall(n3, n2);
            SetCall(n3, n4);
            var nodes = GetCalls(n3);
            var nodes2 = GetCalledFrom(n2);

            bool test1 = isCall(n2, n3);
            bool test2 = isCall(n1, n3);
            bool test3 = isCall(n4, n3);
            bool test4 = isCall(n1, n2);
            bool test5 = isCall(n3, n4);
        }
    }
}
