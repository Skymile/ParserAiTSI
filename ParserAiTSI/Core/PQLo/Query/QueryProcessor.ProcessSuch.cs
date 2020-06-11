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
                                    .ToList();
                                var list = new List<INode>();
                                foreach (var item in inProcedure)
                                {
                                    list.AddRange(this.Api.PKB.Procedures
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

                                return result
                                    .Where(i => i.Token != Instruction.Procedure)
                                    .Select(i => i.LineNumber.ToString())
                                    .Distinct();
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
                        return en[f].Select(i => i.LineNumber.ToString());
                    }
                case CommandType.Calls:
                case CommandType.Follows:
                    if (int.TryParse(data.Right, out var number))
                    {
                        return this.Api.PKB.ArrayForm
                            .Where(i => i.LineNumber == number)
                            .Select(i => i.Previous.LineNumber.ToString());
                    }
                    //else if (int.TryParse(data.Left, out number))
                    //{
                    //    return this.Api.PKB.ArrayForm
                    //        .Where(i => i.LineNumber == number)
                    //        .Select(i => i.Previous.LineNumber.ToString());
                    //}
                    return new List<string> { "NONE" };
                case CommandType.Parent:
                    {
                        switch (Find(d, data.Variable))
                        {
                            case StatementType.Call:
                                break;
                            case StatementType.Stmt:
                                {
                                var z = this.Api.PKB.Parent.dict.Where(x => x.Key.LineNumber.ToString() == data.Left).Select(x => x.Key).ToList();

                                }
                            break;
                            case StatementType.Assign: // TODO: JAK BEDZIE ELSE
                                {
                                var z = this.Api.PKB.Parent.dict.Where(x => x.Key.LineNumber.ToString() == data.Left).Select(x => x.Key).ToList();
                                var c = this.Api.GetNodes(Instruction.Else);
                               
                                break;
                            case StatementType.While:
                                //var a = this.Api.PKB.Parent.dict.Where(Mode.StandardRecursion,Instruction.Loop, x => x.Key.LineNumber.ToString() == data.Right).ToList(); /TODO:: naprawic
                                break;
                            case StatementType.ProgLine:
                                break;
                            case StatementType.If:
                                break;
                        }
                        break;
                    }
                    break;
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> FindInstructionLinesForModifies(Instruction instruction, SuchData data)
        {
            var inProcedure = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;

            var list = new List<INode>();
            var result = inProcedure
                .Select(x => x.Parents
                    .Where(Mode.StandardRecursion, z => z.Token == instruction || (z.Twin?.Nodes.Exists(i => i.Variable == data.Right) ?? false))
                    .Select(Mode.NoRecursion, z => z).Distinct(new KScheduleComparer()).Distinct(new KScheduleComparer())
                )
                .SelectMany(x => x.Distinct(new KScheduleComparer()))
                .ToList();
            //var dupa = inProcedure
            //    .Select(x => x.Parents
            //        .Where(Mode.StandardRecursion, z => z.Token == instruction)
            //        .Select(Mode.NoRecursion, z => z).Distinct(new KScheduleComparer()).ToList()).ToList();
            //foreach (var item in dupa)
            //{
            //    var xddd = item.Where(Mode.StandardRecursion, x => x.Nodes.Exists(a => a.Variable == data.Right)).ToList();
            //}
            foreach (var item in inProcedure)
            {
                list.AddRange(this.Api.PKB.Procedures
                    .Where(Mode.StandardRecursion, Instruction.Call, x => x.Variable == item.Parents.Last().Variable)
                    .Select(Mode.NoRecursion, x => x).ToList());
            }
            result.AddRange(list
                .Select(x => x
                             .Parents
                             .Where(z => z.Token == instruction
                                || (z.Twin?.Nodes.Exists(i => i.Variable == data.Right) ?? false)
                             ).Select(z => z)
                             .ToList()
                ).SelectMany(x => x)
                .ToList());
            return result.Distinct().Select(x => x.LineNumber.ToString());
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