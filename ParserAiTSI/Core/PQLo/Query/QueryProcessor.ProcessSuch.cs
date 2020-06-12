using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
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
                        case StatementType.If:
                            return FindInstructionLinesForModifies(Instruction.If, data);
                        case StatementType.ProgLine:
                            break;
                        case StatementType.Constant:
                            break;
                    }
                    break;
                case CommandType.Uses:
                    switch (statement)
                    {
                        case StatementType.Procedure:
                            break;
                        case StatementType.Call:
                            var calls = this.Api.ArrayForm.Where(x => x.Token == Instruction.Expression ? x.Variables.Contains(data.Right) : x.Variable == data.Right).ToList();
                            calls = calls.Where(x => x.Token != Instruction.Assign && x.Variables == null).ToList();
                            var invokedProcedures = calls.Select(x => x.Parents.Last()).ToList();
                            invokedProcedures = invokedProcedures.Distinct(new NodeStringComparer()).Select(x => x as Node).ToList();
                            var result = new List<Node>();
                            foreach (var item in invokedProcedures)
                            {
                                result.AddRange(this.Api.ArrayForm.Where(Mode.NoRecursion, Instruction.Call, x => x.Variable == item.Variable).Select(x => x as Node));
                            }
                            return result;
                        case StatementType.Variable:
                            return GetUsesVariable(data, ref overwrite);
                        case StatementType.Constant:
                            break;
                        case StatementType.Stmt:
                            break;
                        case StatementType.Assign:
                            break;
                        case StatementType.Stmtlst:
                            break;
                        case StatementType.While:
                            break;
                        case StatementType.ProgLine:
                            break;
                        case StatementType.If:
                            break;
                    }
                    break;
                //var en = this.Api.PKB.Uses.dict;
                //    var f = en.Keys.FirstOrDefault(
                //        i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
                //    );
                //    return en[f];

                case CommandType.Calls:
                    switch (Find(d, data.Variable))
                    {
                        case StatementType.Procedure:
                            overwrite = i => i.Variable;
                            return data.Left == data.Variable
                                ? this.Api.ArrayForm.Where(Mode.NoRecursion, Instruction.Call, x => x.Variable == data.Right).Select(x => x.Parents.Last())
                                : (IEnumerable<INode>)this.Api.PKB.Calls.dict.FirstOrDefault(x => x.Key.Variable == data.Left).Value;
                    }
                    break;
                case CommandType.CallsStar:
                    switch (Find(d, data.Variable))
                    {
                        case StatementType.Procedure:
                            overwrite = i => i.Variable;
                            if (data.Left == data.Variable)
                            {
                                var procedures = this.Api.ArrayForm
                                    .Where(Mode.NoRecursion, Instruction.Call, i => i.Variable == data.Right)
                                    .Select(i => this.Api.GetProcedure(i.Id));

                                var hash = procedures.Select(i => i.Variable.ToUpperInvariant()).ToHashSet();

                                procedures = procedures.Concat(
                                    this.Api.ArrayForm
                                        .Where(
                                            Mode.NoRecursion,
                                            Instruction.Call,
                                            i => hash.Contains(i.Variable.ToUpperInvariant())
                                        )
                                        .Select(i => this.Api.GetProcedure(i.Id))
                                );

                                return procedures;
                            }
                            else if (data.Right == data.Variable)
                            {
                                var procedures = this.Api.ArrayForm
                                    .Where(Mode.NoRecursion, Instruction.Procedure, i => i.Variable == data.Left)
                                    .Select(Mode.StandardRecursion, Instruction.Call, i => i)
                                    .ToList();

                                var calls = new List<INode>();

                                bool change = true;
                                while (change)
                                {
                                    var set = procedures
                                        .Select(i => i.Variable)
                                        .ToHashSet();

                                    calls = this.Api.ArrayForm
                                        .Where(Mode.StandardRecursion, Instruction.Procedure, i => set.Contains(i.Variable))
                                        .ToList();

                                    int len = procedures.Count;

                                    procedures = procedures
                                        .Concat(calls
                                            .Select(i => this.Api.GetProcedure(i.Id))
                                            .Select(Mode.StandardRecursion, Instruction.Call, i => i)
                                        )
                                        .Distinct()
                                        .ToList();

                                    change = len != procedures.Count;
                                }

                                return procedures;
                            }
                            return this.Api.PKB.Calls.dict.FirstOrDefault(x => x.Key.Variable == data.Left).Value;
                    }
                    break;
                case CommandType.Follows:
                    {
                        if (IsFollows(false, data, ref overwrite) is IEnumerable<Node> n)
                            return n;
                        if (int.TryParse(data.Right, out var number))
                        {
                            var ins = stateToInstruction[statement];
                            return this.Api.PKB.ArrayForm
                                .Where(i => i.LineNumber == number)
                                .Select(i =>
                                {
                                    var j = i.Previous;
                                    if (j.Token == Instruction.Else)
                                        j = j.Previous;
                                    return j;
                                })
                                .Where(i => ins.HasFlag(i.Token));
                        }
                        else if (d.TryGetValue(data.Right, out var statementLeft))
                        {
                            Instruction ins = stateToInstruction[statementLeft];

                            if (data.Left == "_")
                            {
                                return this.Api.ArrayForm
                                    .Where(i => i.Next?.Token != Instruction.Else)
                                    .Where(i => ins.HasFlag(i.Token))
                                    .Select(i => i.Next);
                            }
                            else if (int.TryParse(data.Left, out var number4))
                            {
                                return this.Api.ArrayForm
                                    .Where(i => i.LineNumber == number4)
                                    .Where(i => i.Next?.Token != Instruction.Else)
                                    .Where(i => ins.HasFlag(i.Next.Token))
                                    .Select(i => i.Next);
                            }

                        }
                        else if (d.TryGetValue(data.Left, out var statementRight))
                        {
                            if (data.Right == "_")
                            {
                                Instruction ins = stateToInstruction[statementRight];

                                return this.Api.ArrayForm
                                    .Where(i => i.Previous != null && ins.HasFlag(i.Previous.Token))
                                    .Select(i => i.Previous);
                            }
                        }
                    }
                    return null;
                case CommandType.Parent:
                    {
                        switch (Find(d, data.Variable))
                        {
                            case StatementType.Call:
                                return data.Left == data.Variable
                                    ? this.Api.ArrayForm
                                        .Where(x => x.LineNumber.ToString() == data.Right)
                                        .Where(x => x.Parent.Token != Instruction.Procedure)
                                        .Select(i => i.Parent)
                                    : (IEnumerable<INode>)FindLinesForGivenParentInstruction(Instruction.Call, data);
                            case StatementType.Stmt:
                                if (data.Left == data.Variable)
                                {
                                    return this.Api.ArrayForm
                                        .Where(x => x.LineNumber.ToString() == data.Right)
                                        .Where(x => x.Parent.Token != Instruction.Procedure)
                                        .Select(i => i.Parent);
                                }
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
                            case StatementType.Assign:
                                return data.Left == data.Variable
                                    ? this.Api.ArrayForm
                                        .Where(x => x.LineNumber.ToString() == data.Right)
                                        .Where(x => x.Parent.Token != Instruction.Procedure)
                                        .Select(i => i.Parent)
                                    : (IEnumerable<INode>)FindLinesForGivenParentInstruction(Instruction.Assign, data);
                            case StatementType.While:
                                return data.Left == data.Variable
                                    ? this.Api.ArrayForm
                                        .Where(x => x.LineNumber.ToString() == data.Right)
                                        .Where(x => x.Parent.Token == Instruction.Loop)
                                        .Select(i => i.Parent)
                                    : (IEnumerable<INode>)FindLinesForGivenParentInstruction(Instruction.Loop, data);
                            case StatementType.If:
                                return data.Left == data.Variable
                                    ? this.Api.ArrayForm
                                        .Where(x => x.LineNumber.ToString() == data.Right)
                                        .Where(x => x.Parent.Token == Instruction.If)
                                        .Select(i => i.Parent)
                                    : (IEnumerable<INode>)FindLinesForGivenParentInstruction(Instruction.If, data);
                        }
                        break;
                    }
                case CommandType.ParentStar:
                    {
                        switch (Find(d, data.Variable))
                        {
                            case StatementType.Assign:
                                {
                                    if (int.TryParse(data.Left, out var number))
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber == number);
                                        var flags = stateToInstruction[statement];
                                        var result = new List<INode>();
                                        if (z.Key.Token == Instruction.If || z.Key.Token == Instruction.Else)
                                            result.AddRange(z.Key.Twin.Nodes.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        result.AddRange(z.Value.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        var a = result.Where(x => flags.HasFlag(x.Token));
                                        return a;
                                    }
                                    return null;
                                }
                            case StatementType.Stmt:
                                {
                                    if (int.TryParse(data.Left, out var number))
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber == number && x.Key.Token != Instruction.Else);
                                        var flags = stateToInstruction[statement];
                                        var result = new List<INode>();
                                        if (z.Key.Token == Instruction.If)
                                            result.AddRange(z.Key.Twin.Nodes.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        result.AddRange(z.Value.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        var a = result.Where(x => flags.HasFlag(x.Token));
                                        return a;
                                    }
                                    else if (int.TryParse(data.Right, out number))
                                    {
                                        var flags = stateToInstruction[statement];
                                        var a = this.Api.ArrayForm
                                            .FirstOrDefault(x => x.LineNumber == number && x.Token != Instruction.Else);
                                        var result = new List<INode>();
                                        foreach (var item in a.Parents)
                                        {
                                            if (item.Token == Instruction.If || item.Token == Instruction.Else)
                                                result.Add(item.Twin);

                                        }
                                        result.AddRange(a.Parents.Where(x => flags.HasFlag(x.Token) && x.LineNumber != number));
                                        return result;
                                    }
                                    break;
                                }
                            case StatementType.While:
                                {
                                    if (int.TryParse(data.Right, out var number))
                                    {
                                        var flags = stateToInstruction[statement];
                                        var a = this.Api.ArrayForm
                                            .FirstOrDefault(x => x.LineNumber == number && x.Token != Instruction.Else);
                                        var b = a.Parents.Where(x => flags.HasFlag(x.Token) || x.Token == Instruction.Else);
                                        return b;
                                    }
                                    if (int.TryParse(data.Left, out number))
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber == number && x.Key.Token != Instruction.Else);
                                        var flags = stateToInstruction[statement];
                                        var result = new List<INode>();
                                        if (z.Key.Token == Instruction.If)
                                            result.AddRange(z.Key.Twin.Nodes.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        result.AddRange(z.Value.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        var a = result.Where(x => flags.HasFlag(x.Token));
                                        return a;
                                    }
                                    break;
                                }
                            case StatementType.If:
                                {
                                    if (int.TryParse(data.Left, out var number))
                                    {
                                        var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber == number && x.Key.Token != Instruction.Else);
                                        if (z.Key == null)
                                            return null;
                                        var flags = stateToInstruction[statement];
                                        var result = new List<INode>();
                                        if (z.Key.Token == Instruction.If)
                                            result.AddRange(z.Key.Twin.Nodes.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        result.AddRange(z.Value.Where(Mode.StandardRecursion, x => x.Nodes != null));
                                        var a = result.Where(x => flags.HasFlag(x.Token));
                                        return a;
                                    }
                                    else if (int.TryParse(data.Right, out number))
                                    {
                                        var flags = stateToInstruction[statement];
                                        var a = this.Api.ArrayForm
                                            .FirstOrDefault(x => x.LineNumber == number && x.Token != Instruction.Else);
                                        var b = a.Parents.Where(x => flags.HasFlag(x.Token) || x.Token == Instruction.Else);
                                        return b;

                                    }
                                }
                                break;
                        }
                        break;
                    }
                case CommandType.FollowsStar:
                    {
                        if (IsFollows(true, data, ref overwrite) is IEnumerable<Node> n)
                            return n;
                        if (int.TryParse(data.Left, out var number))
                        {
                            var flags = stateToInstruction[statement];
                            var next = this.Api.ArrayForm.FirstOrDefault(i => i.LineNumber == number).Next;
                            var nodes = new List<INode>();
                            while (next != null)
                            {
                                if (flags.HasFlag(next.Token) && next.LineNumber != number)
                                    nodes.Add(next);
                                if (next.Level <= next.Next.Level)
                                    next = next.Next;
                                else
                                    break;
                            }
                            return nodes;
                        }
                        else if (data.Left == "_")
                        {
                            var nodes = this.Api.ArrayForm;
                            var flags = stateToInstruction[statement];
                            return nodes
                                .Where(i => i.Next != null && flags.HasFlag(i.Next.Token) && i.Next.LineNumber != number)
                                .Select(i => i.Next);
                        }
                        else if (int.TryParse(data.Right, out number))
                        {
                            var flags = stateToInstruction[statement];
                            var next = this.Api.ArrayForm.FirstOrDefault(i => i.LineNumber == number).Previous;
                            var nodes = new List<INode>();
                            while (next != null)
                            {
                                if (flags.HasFlag(next.Token) && next.LineNumber != number)
                                    nodes.Add(next);
                                if (next.Level <= next.Previous.Level)
                                    next = next.Previous;
                                else
                                    break;
                            }
                            return nodes;
                        }
                    }
                    break;
            }
            return null;
        }

        private IEnumerable<Node> IsFollows(bool isStar, SuchData data, ref Func<INode, string> overwrite)
        {
            if (data.Variable == "BOOLEAN")
            {
                bool isF = false;
                overwrite = i => isF.ToString();

                if (data.Left == "_" && data.Right == "_")
                    isF = true;
                else if (int.TryParse(data.Left, out var left))
                {
                    if (int.TryParse(data.Right, out var right))
                        isF = checkFollows(this.Api.ArrayForm[left], this.Api.ArrayForm[right]);
                }
                else if (int.TryParse(data.Right, out var right) && int.TryParse(data.Left, out left))
                    isF = checkFollows(this.Api.ArrayForm[left], this.Api.ArrayForm[right]);
                return new[] { new Node() };
            }
            return null;

            bool checkFollows(Node l, Node r)
            {
                if (isStar)
                {
                    bool isFollows = this.Api.PKB.Follows.IsFollows(l, r);
                    var next = l.Next;

                    while (next.Next != null)
                    {
                        isFollows |= this.Api.PKB.Follows.IsFollows(next, r);
                        next = next.Next;
                    }

                    return isFollows;
                }
                return this.Api.PKB.Follows.IsFollows(l, r);
            }
        }

        private List<INode> FindLinesForGivenParentInstruction(Instruction instruction, SuchData suchData)
        {
            var z = this.Api.PKB.Parent.dict.FirstOrDefault(x => x.Key.LineNumber.ToString() == suchData.Left);
            if (z.Key == null)
                return null;
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

        private IEnumerable<Node> GetUsesVariable(SuchData data, ref Func<INode, string> overwrite)
        {
            if (int.TryParse(data.Left, out var number))
            {
                var lines = this.Api.PKB.ArrayForm.FirstOrDefault(x => x.LineNumber == number);
                var result = new List<string>() { lines.Variable };
                var xd = new List<INode>();
                overwrite = x => string.Join(",", result);

                if (lines.Nodes == null)
                {
                    return new[] { new Node() }; ;
                }
                var calls = lines.Nodes.Select(Mode.StandardRecursion, Instruction.Call, x => x).Distinct(new NodeStringComparer()).ToList();
                foreach (var item in calls.Distinct(new NodeStringComparer()).ToList())
                {
                    xd.AddRange(this.Api.PKB.Procedures.Where(x => x.Name == item.Variable).Select(Mode.StandardRecursion, x => x).Where(x => x.Token != Instruction.Procedure));
                }
                xd = xd.Distinct(new KScheduleComparer()).ToList();
                xd.AddRange(lines.Nodes.Select(Mode.StandardRecursion, x => x));
                result.AddRange(
                    xd
                    .Where
                    (x =>
                        x.LineNumber != number
                        && x.Token != Instruction.Procedure
                        && x.Token != Instruction.Call
                        && x.Token != Instruction.Assign
                        && !string.IsNullOrWhiteSpace(x.Variable)
                    ).Select(x => x.Token == Instruction.Expression ? x.Variables : x
                        .Variables?
                        .Append(x.Variable)
                        ?? new List<string> { x.Variable })
                    .SelectMany(z => z.
                        Where(x => !int.TryParse(x, out int res))));
                result = result.Distinct().ToList();

            }
            return new[] { new Node() }; ;
        }
        private static IEnumerable<INode> GetWithTwin(INode node)
        {
            if (node.Twin != null) // && node.Token == Instruction.If)
                yield return node.Twin;
            yield return node;
        }

        private static readonly Dictionary<StatementType, Instruction> stateToInstruction = new Dictionary<StatementType, Instruction>
        {
            { StatementType.Assign   , Instruction.Assign | Instruction.Expression },
            { StatementType.Call     , Instruction.Call        },
            { StatementType.Constant , Instruction.Expression  },
            { StatementType.If       , Instruction.If          },
            { StatementType.Procedure, Instruction.Procedure   },
            { StatementType.ProgLine , Instruction.Procedure   },
            { StatementType.Stmt     , Instruction.Loop | Instruction.If | Instruction.Call | Instruction.Expression | Instruction.Assign  },
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

    public class NodeStringComparer : IEqualityComparer<INode>
    {
        public bool Equals(INode x, INode y) =>
            string.IsNullOrEmpty(x?.Variable) || string.IsNullOrEmpty(y?.Variable) ? false : x.Variable.Equals(y.Variable);
        public int GetHashCode(INode obj) => obj.GetHashCode();
    }

    //public class NodeNameComparer : IEqualityComparer<INode>
    //{
    //    public bool Equals(INode x, INode y) =>
    //        string.IsNullOrEmpty(x?.Variable) || string.IsNullOrEmpty(y?.Variable) ? false : x.Variable.Equals(y.Variable);
    //    public int GetHashCode(INode obj) => obj.GetHashCode();
    //}
}