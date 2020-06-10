using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces.PQL;

namespace Core.PQLo.QueryPreProcessor
{
    public partial class QueryProcessor
    {
        private IEnumerable<string> ProcessSuch(SuchData data, Dictionary<string, StatementType> d)
        {
            switch (data.Type)
            {
                case CommandType.Modifies:
                    switch (Find(d, data.Variable))
                    {
                        case StatementType.Procedure:
                            {
                                var en = this.Api.PKB.Procedures
                                    .ToNodeEnumerator()
                                    .Where(true, i => i.Instruction == data.Variable)
                                    .Select(false, i => i.LineNumber.ToString());
                                return en;
                            }
                        case StatementType.Call:
                        case StatementType.Variable:
                            {
                                var f = this.Api.PKB.ArrayForm
                                    .Where(i => i.LineNumber.ToString() == data.Left)
                                    .ToArray();

                                if (f.Length == 0)
                                    f = this.Api.PKB.Procedures
                                        .Where(i => i.Name == data.Left)
                                        .Select(i => i as Node)
                                        .ToArray();

                                var calls = f
                                    .ToNodeEnumerator()
                                    .Where(true, Instruction.Call)
                                    .Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
                                    .ToArray();

                                bool change = false;
                                int callCount = calls.Count();
                                do
                                {
                                    calls = calls.Concat(
                                            calls
                                                .ToNodeEnumerator()
                                                .Where(true, Instruction.Call)
                                                .Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
                                        ).Distinct().ToArray();
                                    change = callCount != calls.Length;
                                    callCount = calls.Length;
                                } while (change);

                                var en = f
                                    .ToNodeEnumerator()
                                    .Where(true, Instruction.Assign | Instruction.Expression, i => i.Variable != null)
                                    .Select(false, i => i.Variable.ToUpperInvariant())
                                    .Distinct()
                                    .ToArray();

                                var enC = calls
                                    .ToNodeEnumerator()
                                    .Where(true, Instruction.Assign | Instruction.Expression, i => i.Variable != null)
                                    .Select(false, i => i.Variable.ToUpperInvariant())
                                    .Distinct()
                                    .ToArray();

                                return en.Concat(enC);
                            }
                        case StatementType.Stmt:
                            {
                                var f = this.Api.PKB.ArrayForm
                                    .Where(i => i.Variable == data.Right)
                                    .ToArray();

                                if (f.Length == 0)
                                    f = this.Api.PKB.Procedures
                                        .Where(i => i.Name == data.Right)
                                        .Select(i => i as Node)
                                        .ToArray();

                                var calls = f
                                    .ToNodeEnumerator()
                                    .Where(true, Instruction.Call)
                                    .Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
                                    .ToArray();

                                bool change = false;
                                int callCount = calls.Count();
                                do
                                {
                                    calls = calls.Concat(
                                            calls
                                                .ToNodeEnumerator()
                                                .Where(true, Instruction.Call)
                                                .Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
                                        ).Distinct().ToArray();
                                    change = callCount != calls.Length;
                                    callCount = calls.Length;
                                } while (change);

                                var en = f
                                    .ToNodeEnumerator()
                                    .Where(true, i => i.Variable != null)
                                    .Select(false, i => i.LineNumber)
                                    .Distinct()
                                    .ToArray();

                                var enC = calls
                                    .ToNodeEnumerator()
                                    .Where(true, i => i.Variable != null)
                                    .Select(false, i => i.LineNumber)
                                    .Distinct()
                                    .ToArray();

                                return en.Concat(enC).Select(i => i.ToString());
                            }
                        case StatementType.Assign:
                            {
                                var en = this.Api.PKB.Modifies.dict;
                                var f = en.Keys.FirstOrDefault(
                                    i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                                );
                                return en[f].Select(i => i.LineNumber.ToString());
                            }
                        case StatementType.Stmtlst:
                        case StatementType.While:
                        case StatementType.ProgLine:
                        case StatementType.If:
                            var modifiedValues = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;
                            var inProcedure = modifiedValues
                                .Select(x => new { Node = x, Parents = GetParents(x as Node) })
                                .ToList();
                            var result = inProcedure
                                .Select(x => x.Parents
                                    .Where(z => z.Token == Instruction.If)
                                    .Select(z => z.LineNumber)
                                    .ToList())
                                .SelectMany(x => x)
                                .ToList();
                            var list = new List<INode>();
                            foreach (var item in inProcedure)
                            {
                                list.AddRange(this.Api.PKB.Procedures
                                    .ToNodeEnumerator()
                                    .Where(true, Instruction.Call, x => x.Variable == item.Parents.Last().Variable)
                                    .Select(false, x => x).ToList());
                            }
                            var calledIf = list.Select(x => new { Node = x, Parents = GetParents(x as Node) }).ToList();
                            result.AddRange(calledIf.Select(x => x.Parents.Where(z => z.Token == Instruction.If).Select(z => z.LineNumber).ToList()).SelectMany(x => x).ToList());
                            return result.Distinct().Select(x => x.ToString());

                        case StatementType.Constant:
                            break;
                    }
                    break;
                case CommandType.Uses:
                    {
                        var en = this.Api.PKB.Uses.dict;
                        var f = en.Keys.FirstOrDefault(
                            i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                        );
                        return en[f].Select(i => i.LineNumber.ToString());
                    }
                case CommandType.Calls:
                case CommandType.Follows:
                case CommandType.Parent:
                    break;
            }
            return Enumerable.Empty<string>();
        }
        private IEnumerable<Node> GetParents(Node node)
        {
            if (node == null) return new List<Node>();
            var parents = new List<Node>();
            setParent(node.Parent);

            return parents;
            void setParent(Node parent)
            {
                parents.Add(parent);
                if (parent.Parent != null)
                {
                    setParent(parent.Parent);
                }
            }
        }
    }
}
