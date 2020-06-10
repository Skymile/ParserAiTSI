using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
                                var main = this.Api.PKB.ArrayForm
                                    .Where(Mode.NoRecursion, Instruction.Procedure)
                                    .Where(Mode.GreedyRecursion, Instruction.Assign | Instruction.Expression, i => i.Variable == data.Right)
                                    .Select(Mode.NoRecursion, i => this.Api.GetProcedure(i.Id));

                                var calls = this.Api.PKB.ArrayForm
                                    .Where(Mode.StandardRecursion, Instruction.Call, i => main.Any(j => j.Variable == i.Variable))
                                    .Select(Mode.NoRecursion, i => this.Api.GetProcedure(i.Id));
                                
                                return main
                                    .Concat(calls)
                                    .Select(i => i.Variable)
                                    .Distinct();
                            }
                        case StatementType.Call:
                            {
                                var modifiedValues = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;
                                if (modifiedValues == null)
                                    return Enumerable.Empty<string>();
                                var inProcedure = modifiedValues
                                    .Select(x => new { Node = x, Parents = GetParents(x as Node) })
                                    .ToList();
                                var list = new List<INode>();
                                foreach (var item in inProcedure)
                                {
                                    list.AddRange(this.Api.PKB.Procedures
                                        .ToNodeEnumerator()
                                        .Where(Mode.StandardRecursion, Instruction.Call, x => x.Variable == item.Parents.Last().Variable)
                                        .Select(Mode.NoRecursion, x => x).ToList());
                                }
                                return list.Distinct(new KScheduleComparer()).Select(x => x.LineNumber.ToString());
                            }
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
                                    .Gather(
                                        Mode.GreedyRecursion,
                                        Instruction.Call,
                                        i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable)
                                    )
                                    .Select(Mode.StandardRecursion, Instruction.Assign | Instruction.Expression, i => i.Variable);

                                var en = f
                                    .Select(Mode.StandardRecursion, Instruction.Assign | Instruction.Expression, i => i.Variable);

                                return en.Concat(calls).Distinct();
                            }

                        case StatementType.Assign:
                            {
                                var en = this.Api.PKB.Modifies.dict;
                                var f = en.Keys.FirstOrDefault(
                                    i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                                );
                                return en[f].Select(i => i.LineNumber.ToString());
                            }
                        case StatementType.Stmt:
                            {
                                var en = this.Api.PKB.Modifies.dict;
                                var f = en.Keys.FirstOrDefault(
                                    i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                                );
                                var main = en[f];

                                for (int i = 0; i < main.Count; i++)
                                    GatherParents(main[i], main);

                                var procedures = main
                                    .Select(i => this.Api.GetProcedure(i.Id));
                                var hash = procedures.Select(i => i.Variable).ToHashSet();

                                var calls = this.Api.PKB.ArrayForm
                                    .Where(i => i.Token == Instruction.Call && hash.Contains(i.Variable))
                                    .Select(i => i as INode)
                                    .ToList();

                                for (int i = 0; i < calls.Count; i++)
                                    GatherParents(calls[i], calls);

                                return main.Concat(calls)
                                    .Where(i => i.Token != Instruction.Procedure)
                                    .Select(i => i.LineNumber.ToString())
                                    .Distinct();

                                void GatherParents(INode node, List<INode> nodes)
                                {
                                    if (node.Parent != null)
                                    {
                                        nodes.Add(node.Parent);
                                        GatherParents(node.Parent, nodes);
                                    }
                                }
                            }
                        case StatementType.Stmtlst:
                            break;
                        case StatementType.While:
                            {
                                return FindInstructionLinesForModifies(Instruction.Loop, data);
                            }
                        case StatementType.If:
                            {
                                return FindInstructionLinesForModifies(Instruction.If, data);
                            }
                            break;
                        case StatementType.ProgLine:
                            break;
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
                    return new List<string> {
                        int.TryParse(data.Right, out int r) ? (r - 1).ToString() : "NONE"
                    };
                case CommandType.Parent:
                    break;
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> FindInstructionLinesForModifies(Instruction instruction, SuchData data)
        {
            var modifiedValues = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;
            var inProcedure = modifiedValues
                .Select(x => new { Node = x, Parents = GetParents(x as Node) })
                .ToList();
            var result = inProcedure
                .Select(x => x.Parents
                    .Where(z => z.Token == instruction)
                    .Select(z => z.LineNumber)
                    .ToList())
                .SelectMany(x => x)
                .ToList();
            var list = new List<INode>();
            foreach (var item in inProcedure)
            {
                list.AddRange(this.Api.PKB.Procedures
                    .Where(Mode.StandardRecursion, Instruction.Call, x => x.Variable == item.Parents.Last().Variable)
                    .Select(Mode.NoRecursion, x => x).ToList());
            }
            var calledIf = list.Select(x => new { Node = x, Parents = GetParents(x as Node) }).ToList();
            result.AddRange(calledIf.Select(x => x.Parents.Where(z => z.Token == instruction).Select(z => z.LineNumber).ToList()).SelectMany(x => x).ToList());
            return result.Distinct().Select(x => x.ToString());
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

    public class KScheduleComparer : IEqualityComparer<INode>
    {
        public bool Equals(INode x, INode y)
        {
            if (x?.LineNumber == 0 || y?.LineNumber == 0)
                return false;
            return x?.LineNumber == y?.LineNumber;
        }

        public int GetHashCode(INode obj)
        {
            return obj.GetHashCode();
        }
    }
}