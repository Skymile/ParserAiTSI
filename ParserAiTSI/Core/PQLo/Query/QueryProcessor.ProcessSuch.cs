using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

                    switch (Find(d, data.Variable))
                    {
                        case StatementType.Procedure:
                            if (data.Left == data.Variable)
                            {
                                var a = this.Api.ArrayForm.Where(Mode.NoRecursion, Instruction.Call, x => x.Variable == data.Right).Select(x => x.Parents.Last().Variable).Distinct().ToList();
                                return a;
                            }
                            else
                            {
                                var a = this.Api.PKB.Calls.dict.FirstOrDefault(x => x.Key.Variable == data.Left).Value;
                                return a.Select(x => x.Variable);
                            }
                    }
                    break;
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
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        var z = this.Api.ArrayForm.Where(x => x.LineNumber.ToString() == data.Right).ToList();
                                        return z.Where(x => x.Parent.Token != Instruction.Procedure).Select(x => x.Parent.LineNumber.ToString());
                                    }
                                    else
                                    {
                                        var result = FindLinesForGivenParentInstruction(Instruction.Call, data);

                                        return result.Select(x => x.ToString());
                                    }
                                }
                            case StatementType.Stmt:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        var z = this.Api.ArrayForm.Where(x => x.LineNumber.ToString() == data.Right).ToList();
                                        return z.Where(x => x.Parent.Token != Instruction.Procedure).Select(x => x.Parent.LineNumber.ToString());
                                    }
                                    else
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber.ToString() == data.Left);
                                        var result = new List<int>();
                                        if (z.Key == null)
                                            return result.Select(x => x.ToString());
                                        if (z.Key.Token == Instruction.If)
                                        {
                                            result.AddRange(this.FindResultForGivenParent(z.Key.Twin.Nodes));
                                            result.AddRange(z.Key.Twin.Nodes.Where(x => x.Token == Instruction.Loop).Select(x => x.LineNumber));
                                        }
                                        result.AddRange(this.FindResultForGivenParent(z.Value.Select(x => x as Node).ToList()));


                                        return result.Select(x => x.ToString());
                                    }

                                }
                            case StatementType.Assign:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        var z = this.Api.ArrayForm.Where(x => x.LineNumber.ToString() == data.Right).ToList();
                                        return z.Where(x => x.Parent.Token != Instruction.Procedure).Select(x => x.Parent.LineNumber.ToString());
                                    }
                                    else
                                    {
                                        var result = FindLinesForGivenParentInstruction(Instruction.Assign, data);

                                        return result.Select(x => x.ToString());
                                    }
                                }
                            case StatementType.While:
                                {
                                    if (data.Left == data.Variable)
                                    {
                                        var z = this.Api.ArrayForm.Where(x => x.LineNumber.ToString() == data.Right).ToList();
                                        return z.Where(x => x.Parent.Token == Instruction.Loop).Select(x => x.Parent.LineNumber.ToString());
                                    }
                                    else
                                    {
                                        var result = FindLinesForGivenParentInstruction(Instruction.Loop, data);

                                        return result.Select(x => x.ToString());
                                    }
                                }
                            case StatementType.If:
                                break;
                        }
                        break;
                    }
            }
            return Enumerable.Empty<string>();
        }

        private List<int> FindLinesForGivenParentInstruction(Instruction instruction, SuchData suchData)
        {
            var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber.ToString() == suchData.Left);
            var result = new List<int>();
            if (z.Key.Token == Instruction.If)
            {
                result.AddRange(z.Key.Twin.Nodes.Where(x => x.Token == instruction).Select(x => x.LineNumber));
            }
            result.AddRange(z.Value.Where(x => x.Token == instruction).Select(x => x.LineNumber));
            return result;
        }

        private List<int> FindResultForGivenParent(List<Node> z)
        {
            var result = new List<int>();
            foreach (var item in z)
            {
                if (item.Token == Instruction.If)
                {
                    result.Add(item.LineNumber);
                    result.Add(item.Twin.LineNumber);
                }
                else
                {
                    result.Add(item.LineNumber);
                }
            }
            return result;
        }

        private IEnumerable<string> FindInstructionLinesForModifies(Instruction instruction, SuchData data)
        {
            var inProcedure = this.Api.PKB.Modifies.dict.FirstOrDefault(x => x.Key.Name == data.Right).Value;

            var list = new List<INode>();
            var result = inProcedure
                .Select(x => x.Parents
                    .Where(z => z.Token == instruction)
                    .Select(z => z).Distinct(new KScheduleComparer()).Distinct(new KScheduleComparer())
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
                             .Where(z => z.Token == instruction)
                             .Select(z => z)
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