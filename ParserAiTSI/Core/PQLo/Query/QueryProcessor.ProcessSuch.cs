using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.PQLo.QueryPreProcessor
{
    public partial class QueryProcessor
    {
        private IEnumerable<string> ProcessToString(IEnumerable result) =>
            result.Cast<object>().Select(i => i.ToString()).Distinct();

        private IEnumerable ProcessToObject(IEnumerable<INode> nodes, StatementType statement, Func<INode, string> overwrite)
        {
            if (nodes is null)
                return Enumerable.Empty<string>();
            if (!(overwrite is null))
                return nodes.Select(overwrite);
            switch (statement)
            {
                case StatementType.Procedure:
                case StatementType.Variable: return nodes.Where(i => !(i is null)).Select(i => i.Variable);
                case StatementType.While:
                case StatementType.If:
                case StatementType.Assign:
                case StatementType.Constant:
                case StatementType.Call:
                case StatementType.Stmtlst:
                case StatementType.ProgLine:
                case StatementType.Stmt: return nodes.Where(i => !(i is null)).Select(i => i.LineNumber);
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<INode> ProcessSuch(SuchData data, Dictionary<string, StatementType> d, out StatementType statement, out Func<INode, string> overwrite)
        {
            overwrite = null;
            statement = Find(d, data.Variable);

            switch (data.Type)
            {
                case CommandType.Modifies:
                    switch (statement)
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

                                return main.Concat(calls);
                            }
                        case StatementType.Call:
                            {
                                var modifiedValues = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;
                                if (modifiedValues == null)
                                    return null;
                                var inProcedure = modifiedValues
                                    .ToList();
                                var list = new List<INode>();
                                foreach (var item in inProcedure)
                                {
                                    list.AddRange(this.Api.PKB.Procedures
                                        .Where(Mode.StandardRecursion, Instruction.Call, x => x.Variable == item.Parents.Last().Variable)
                                        .Select(Mode.NoRecursion, x => x).ToList());
                                }
                                return list.Distinct(new KScheduleComparer());
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
                                    .Select(Mode.StandardRecursion, Instruction.Assign | Instruction.Expression, i => i);

                                var en = f
                                    .Select(Mode.StandardRecursion, Instruction.Assign | Instruction.Expression, i => i);

                                return en.Concat(calls).Distinct();
                            }
                        case StatementType.Assign:
                            {
                                var en = this.Api.PKB.Modifies.dict;
                                var f = en.Keys.FirstOrDefault(
                                    i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                                );
                                return en[f];
                            }
                        case StatementType.Stmt:
                            {
                                var en = this.Api.PKB.Modifies.dict;
                                var f = en.Keys.FirstOrDefault(
                                    i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                                );
                                var main = en[f].SelectMany(i => i.Parents.Append(i));

                                var procedures = main
                                    .Select(i => this.Api.GetProcedure(i.Id));
                                var hash = procedures.Select(i => i.Variable).ToHashSet();

                                var calls = this.Api.PKB.ArrayForm
                                    .Where(i => i.Token == Instruction.Call && hash.Contains(i.Variable))
                                    .SelectMany(i => i.Parents.Append(i))
                                    .ToList();

                                var result = main.Concat(calls);

                                result = result.Concat(
                                    result
                                        .Where(Mode.NoRecursion, Instruction.Else, i => i.Twin != null)
                                        .Select(Mode.NoRecursion, i => i.Twin)
                                ).Distinct();

                                return result.Where(i => i.Token != Instruction.Procedure);
                            }
                        case StatementType.Stmtlst:
                            break;
                        case StatementType.While:
                            return FindInstructionLinesForModifies(Instruction.Loop, data);
                        case StatementType.If: // TODO: JAK BEDZIE ELSE
                            return FindInstructionLinesForModifies(Instruction.If, data);
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
                        return en[f];
                    }
                case CommandType.Calls:

                    switch (Find(d, data.Variable))
                    {
                        case StatementType.Procedure:
                            {
                                overwrite = i => i.Variable;
                                if (data.Left == data.Variable)
                                    return this.Api.ArrayForm.Where(Mode.NoRecursion, Instruction.Call, x => x.Variable == data.Right).Select(x => x.Parents.Last()).Distinct().ToList();
                                return this.Api.PKB.Calls.dict.FirstOrDefault(x => x.Key.Variable == data.Left).Value;
                            }
                    }
                    break;
                case CommandType.Follows:
                    if (int.TryParse(data.Right, out var number))
                    {
                        var ins = stateToInstruction[statement];
                        return this.Api.PKB.ArrayForm
                            .Where(i => i.LineNumber == number)
                            .Select(i => {
                                var j = i.Previous;
                                if (j.Token == Instruction.Else)
                                    j = j.Previous;
                                return j;
                            })
                            .Where(i => i.Token == ins);
                    }
                    else if (d.TryGetValue(data.Right, out var statementLeft))
                    {
                        if (data.Left == "_")
                        {
                            var ins = stateToInstruction[statementLeft];

                            var k = this.Api.ArrayForm.Where(Mode.NoRecursion, ins);

                            return k.Select(i => i.Next);
                        }
                    }
                    return null;
                case CommandType.Parent:
                    {
                        switch (Find(d, data.Variable))
                        {
                            case StatementType.Call:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        return this.Api.ArrayForm
                                            .Where(x => x.LineNumber.ToString() == data.Right)
                                            .Where(x => x.Parent.Token != Instruction.Procedure)
                                            .Select(i => i.Parent);
                                    }
                                    return FindLinesForGivenParentInstruction(Instruction.Call, data);
                                }
                            case StatementType.Stmt:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        return this.Api.ArrayForm
                                            .Where(x => x.LineNumber.ToString() == data.Right)
                                            .Where(x => x.Parent.Token != Instruction.Procedure)
                                            .Select(i => i.Parent);
                                    }
                                    else
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber.ToString() == data.Left);
                                        var result = new List<INode>();
                                        if (z.Key == null)
                                            return null;
                                        if (z.Key.Token == Instruction.If)
                                        {
                                            result.AddRange(FindResultForGivenParent(z.Key.Twin.Nodes));
                                            result.AddRange(z.Key.Twin.Nodes.Where(x => x.Token == Instruction.Loop));
                                        }
                                        result.AddRange(FindResultForGivenParent(z.Value));
                                        return result;
                                    }

                                }
                            case StatementType.Assign:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        return this.Api.ArrayForm
                                            .Where(x => x.LineNumber.ToString() == data.Right)
                                            .Where(x => x.Parent.Token != Instruction.Procedure)
                                            .Select(i => i.Parent);
                                    }
                                    return FindLinesForGivenParentInstruction(Instruction.Assign, data);
                                }
                            case StatementType.While:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        return this.Api.ArrayForm
                                            .Where(x => x.LineNumber.ToString() == data.Right)
                                            .Where(x => x.Parent.Token == Instruction.Loop)
                                            .Select(i => i.Parent);
                                    }
                                    return FindLinesForGivenParentInstruction(Instruction.Loop, data);
                                }
                            case StatementType.If:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        return this.Api.ArrayForm
                                            .Where(x => x.LineNumber.ToString() == data.Right)
                                            .Where(x => x.Parent.Token == Instruction.If)
                                            .Select(i => i.Parent);
                                    }
                                    return FindLinesForGivenParentInstruction(Instruction.If, data);
                                }
                                break;
                        }
                        break;
                    }
            }
            return null;
        }

        private List<INode> FindLinesForGivenParentInstruction(Instruction instruction, SuchData suchData)
        {
            var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber.ToString() == suchData.Left);
            var result = new List<INode>();
            if (z.Key.Token == Instruction.If)
                result.AddRange(z.Key.Twin.Nodes.Where(x => x.Token == instruction));
            result.AddRange(z.Value.Where(x => x.Token == instruction));
            return result;
        }

        private List<INode> FindResultForGivenParent<T>(List<T> z)
            where T : INode
        {
            var result = new List<INode>();
            foreach (var item in z)
            {
                result.Add(item);
                if (item.Token == Instruction.If)
                    result.Add(item.Twin);
            }
            return result;
        }

        private IEnumerable<INode> FindInstructionLinesForModifies(Instruction instruction, SuchData data)
        {
            var inProcedure = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;

            return inProcedure
                .Select(i => i.Parents)
                .SelectMany(i => i)
                .Concat(inProcedure
                    .Select(i => this.Api.PKB.Procedures
                        .Where(Mode.StandardRecursion, Instruction.Call, x => x.Variable == i.Parents.Last().Variable)
                        .Select(j => j.Parents)
                    )
                    .SelectMany(i => i)
                    .SelectMany(i => i)
                )
                .SelectMany(GetWithTwin)
                .Where(Mode.NoRecursion, instruction);
        }

        private static IEnumerable<INode> GetWithTwin(INode node)
        {
            if (node.Twin != null) // && node.Token == Instruction.If)
                yield return node.Twin;
            yield return node;
        }

        private static readonly Dictionary<StatementType, Instruction> stateToInstruction = new Dictionary<StatementType, Instruction>
        {
            { StatementType.Assign   , Instruction.Assign      },
            { StatementType.Call     , Instruction.Call        },
            { StatementType.Constant , Instruction.Expression  },
            { StatementType.If       , Instruction.If          },
            { StatementType.Procedure, Instruction.Procedure   },
            { StatementType.ProgLine , Instruction.Procedure   },
            { StatementType.Stmt     , Instruction.Expression  },
            { StatementType.Stmtlst  , Instruction.Expression  },
            { StatementType.Variable , Instruction.Expression  },
            { StatementType.While    , Instruction.Loop        },
        };
    }

    public class KScheduleComparer : IEqualityComparer<INode>
    {
        public bool Equals(INode x, INode y) => 
            x?.LineNumber == 0 || y?.LineNumber == 0 ? false : x?.LineNumber == y?.LineNumber;

        public int GetHashCode(INode obj) => obj.GetHashCode();
    }
}