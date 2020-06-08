using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Common;
using Core.Interfaces.AST;
using Core.Interfaces.PQL;

namespace Core.PQLo.QueryEvaluator
{
    public class QueryEvaluator
    {
        public QueryEvaluator(PKB pkb)
        {
            this.pkb = pkb;
            this.pkbApi = new PKBApi(pkb);
        }
        private string resultType;
        private PKBApi pkbApi;
        private PKB pkb;
        private bool isModifies;
        private bool isUses;
        private bool firstUses;
        private SortedSet<(string, int)> UsesPairs = new SortedSet<(string, int)>();

        public List<string> ResultQuery(ITree<PQLNode> Tree)
        {
            var temporary = new List<int>();
            var result = new List<string>();
            var lines = new List<Node>();
            var setLines = new SortedSet<Core.Node>();
            string selectValue = null;
            //WYSZUKIWANIE ODPOWIEDZI
            foreach (var item in Tree.All.Values)
            {
                if (item.Type == "resultNode")
                {
                    this.resultType = item.Field1.Type;
                    if (this.resultType == "assign")
                    {
                        lines = pkbApi.GetNodes(Instruction.Assign, false).ToList();
                        selectValue = item.Field1.Value;
                    }
                    else if (this.resultType == "while")
                    {
                        lines = pkbApi.GetNodes(Instruction.Loop, false).ToList();
                        selectValue = item.Field1.Value;
                    }
                    else if (this.resultType == "variable" || this.resultType == "prog_line")
                    {
                        lines = pkb.Variables.Select(x => (Node)x).ToList();
                        selectValue = item.Field1.Value;
                    }
                    else if (this.resultType == "procedure")
                    {
                        lines = pkbApi.GetNodes(Instruction.Procedure, false).ToList();
                        selectValue = item.Field1.Value;
                    }
                    else if (this.resultType == "stmt" || this.resultType == "boolean")
                    {
                        var tmp2 = pkbApi.GetNodes(Instruction.Assign, false);
                        setLines.UnionWith(tmp2);
                        tmp2 = pkbApi.GetNodes(Instruction.Loop, false).ToList();
                        setLines.UnionWith(tmp2);
                        tmp2 = pkbApi.GetNodes(Instruction.Call, false).ToList();
                        setLines.UnionWith(tmp2);
                        tmp2 = pkbApi.GetNodes(Instruction.If, false).ToList();
                        setLines.UnionWith(tmp2);
                        lines.AddRange(setLines);
                        selectValue = item.Field1.Value;
                    }
                    else if (this.resultType == "if")
                    {
                        lines = pkbApi.GetNodes(Instruction.If, false).ToList();
                        selectValue = item.Field1.Value;
                    }
                }

                if ((item.Type) == "suchNode")
                {
                    //Zweryfikowanie wystapienia relacji Modifies
                    if (item.NodeType == "modifies")
                    {
                        temporary = this.ModifiesResult(item.Field1,
                                item.Field2, lines, selectValue);
                        isModifies = true;
                    }

                    //Zweryfikowanie wystąpienia relacji Parent lub Parent*
                    if (item.NodeType == "parent")
                    {
                        //Pareznt z *
                        if (item.IsStar)
                        {
                            temporary = this.ParentStarResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                        //Parent bez *
                        else
                        {
                            temporary = this.ParentResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                    }
                    //Zweryfikowanie wystąpienia relacji Follows lub Follows*
                    if (item.NodeType == "follows")
                    {
                        //Follows *
                        if (item.IsStar)
                        {
                            temporary = this.FollowsStarResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                        //Follows bez *
                        else
                        {
                            temporary = this.FollowsResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                    }
                    //Zweryfikowanie wystąpienia relacji Uses lub Uses*
                    if (item.NodeType == "uses")
                    {
                        //Uses z *
                        if (item.IsStar)
                        {
                        }
                        //Uses bez *
                        else
                        {
                            temporary = this.UsesResult(item.Field1, item.Field2, lines, selectValue);
                            isUses = true;
                        }
                    }
                    // Zweryfikowanie wystąpienia relacji Calls lub Calls*
                    if (item.NodeType == "calls")
                    {
                        //Calls z *
                        if (item.IsStar)
                        {
                            temporary = this.CallStarResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                        //Calls bez *
                        else
                        {
                            temporary = this.CallResult(item.Field1,
                                    item.Field2, lines,
                                    selectValue);
                        }
                    }

                    if (item.NodeType == "affects" || item.NodeType == "next")
                    {
                        temporary = new List<int>();
                    }
                }

                if ((item.Type) == "withNode")
                {
                    this.WithResults(item.Field1, item.Field2, lines);
                }
            }

            result.Clear();

            if (temporary.Count != 0)
            {
                if (resultType == "boolean")
                {
                    result.Add("true");
                }
                else
                {
                    foreach (var item in temporary)
                    {
                        if (resultType == "procedure")
                        {
                            string name = pkb.Procedures.FirstOrDefault(x => x.Id == item).Name;
                            if (!result.Exists(x => x == name))
                                result.Add(name);
                        }
                        else if (this.resultType == "variable")
                        {
                            if (isUses && !isModifies) // Czy występuje tylko relacja -> Uses
                            {
                                foreach (var uses in UsesPairs)
                                {
                                    if (uses.Item2 == item && result.Exists(x => x == uses.Item1))
                                    {
                                        result.Add(uses.Item1);
                                    }
                                }
                            }
                            var varUsesLines = pkb.Uses.dict;
                            var varModifiesLines = pkb.Modifies.dict;
                            List<int> variableIds = new List<int>();
                            //
                            if (!isUses && isModifies) // Czy występuje tylko relacja . Modifies
                            {

                                foreach (var dict in pkb.Modifies.dict)
                                {
                                    List<int> tmp = dict.Value.Select(x => x.Id).ToList();
                                    if (tmp.Exists(x => x == item))
                                    {
                                        string name = dict.Key.Name;
                                        if (result.Exists(i => i == name) && (variableIds.Exists(i => i == dict.Key.Id) || variableIds.Count == 0))
                                            result.Add(name);
                                    }

                                }
                            }
                            else if (isUses && isModifies) // Wystapienie obu relacji (Uses i Modifies)
                            {
                                List<string> resultUses = new List<string>();
                                foreach (var varUses in varUsesLines)
                                {
                                    if (varUses.Value.Exists(x => x.Id == item))
                                    {
                                        string name = pkb.Variables.FirstOrDefault(x => x.Id == varUses.Key.Id).Name;
                                        if (!resultUses.Exists(x => x == name) && (variableIds.Exists(i => i == varUses.Key.Id) || variableIds.Count == 0))
                                            resultUses.Add(name);
                                    }
                                }

                                List<string> resultModifies = new List<string>();
                                foreach (var varModifies in varModifiesLines)
                                {
                                    if (varModifies.Value.Exists(x => x.Id == item))
                                    {
                                        string name = pkb.Variables.FirstOrDefault(x => x.Id == varModifies.Key.Id).Name;
                                        if (!resultModifies.Exists(x => x == name) && (variableIds.Exists(i => i == varModifies.Key.Id) || variableIds.Count == 0))
                                            resultModifies.Add(name);
                                    }
                                }

                                foreach (var used in resultUses)
                                {
                                    foreach (var modified in resultModifies)
                                    {
                                        if (used == modified && !result.Exists(x => x == used))
                                        {
                                            result.Add(used);
                                        }

                                    }
                                }
                            }
                        }
                    }

                }
            }
            return result;
        }

        private List<int> CallResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            SortedSet<int> candidatesForParameter1 = new SortedSet<int>();
            SortedSet<int> candidatesForParameter2 = new SortedSet<int>();
            SortedSet<int> resultPart = new SortedSet<int>();
            List<int> returnIt = new List<int>();
            if (field1 == null || field2 == null)
            {
                return returnIt;
            }
            string firstParameterType = field1.Type;
            string secondParameterType = field2.Type;
            int firstProcedureId = -1;
            int secondProcedureId = -1;

            //Dla parametru pierwszego tworzy się lista z możliwymi wartościami dla tego parametru
            if (firstParameterType == "string")
            {
                var procedure = pkbApi.GetProcedure(field1.Value);
                firstProcedureId = procedure != null ? procedure.Id : -1;
                if (firstProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter1.Add(firstProcedureId);
            }
            else if (firstParameterType == "constant")
            {
                var procedure = pkb.Procedures.FirstOrDefault(x => x.Id == int.Parse(field1.Value));
                firstProcedureId = procedure != null ? procedure.Id : -1;
                if (firstProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter1.Add(firstProcedureId);
            }
            else
            {
                for (int i = 0; i <= pkb.Procedures.Count; i++)
                {
                    candidatesForParameter1.Add(i);
                }
            }

            //Dla parametru drugiego tworzy się lista z możliwymi wartościami dla tego parametru
            if (secondParameterType == "string")
            {
                var procedure = pkbApi.GetProcedure(field2.Value);
                secondProcedureId = procedure != null ? procedure.Id : -1;
                if (secondProcedureId == -1)
                {
                    return returnIt;
                }
                candidatesForParameter2.Add(secondProcedureId);
            }
            else if (secondParameterType == "constant")
            {

                var procedure = pkb.Procedures.FirstOrDefault(x => x.Id == int.Parse(field2.Value));
                secondProcedureId = procedure != null ? procedure.Id : -1;
                if (secondProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter2.Add(secondProcedureId);
            }
            else
            {
                for (int i = 0; i <= pkb.Procedures.Count; i++)
                {
                    candidatesForParameter1.Add(i);
                }
            }

            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru pierwszego
            if (field1.Type != "constant" && field1.Type != "any")
            {
                candidatesForParameter1 = CutSetLines(field1.Value, candidatesForParameter1);
            }
            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru drugiego
            if (field2.Type != "constant" && field2.Type != "any")
            {
                candidatesForParameter2 = CutSetLines(field2.Value, candidatesForParameter2);
            }

            if (firstParameterType == "constant" && secondParameterType == "constant")
            {
                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId))
                {
                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == firstProcedureId).Id);
                }
                return new List<int>(resultPart);
            }
            else if (firstParameterType == "string" && secondParameterType == "string")
            {
                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId))
                {
                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == firstProcedureId).Id);
                }

                return new List<int>(resultPart);
            }
            else if (firstParameterType == "any" && secondParameterType == "any")
            {
                //Jesli "any" spełnia wymagania to wszystkie linie są dobre
                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId))
                {
                    resultPart.UnionWith(pkb.Procedures.Select(x => x.Id));
                }
                //Jeśli "any" nie spełnia wymagań to wszystkie linie są złe (nie ma procedur w programie)
                returnIt = new List<int>(resultPart);
                return returnIt;
            }
            else if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
            {
                //Jeśli w zapytaniu sa dwie takie same wartości, a nie jest to "any" to zwraca pusty wynik, ponieważ nie można wywołać rekurencyjnie
                return new List<int>(resultPart);
            }
            else
            {
                foreach (var parameter1 in candidatesForParameter1)
                {
                    foreach (var parameter2 in candidatesForParameter2)
                    {
                        if (pkb.Calls.IsCall(parameter1, parameter2) && parameter1 != parameter2)
                        {
                            if (selectValue == field1.Value && selectValue != "boolean")
                            {
                                //Dodaje możliwość z parameter1 do wyniku, gdy call(parmeter1,*) gdzie * = 'any','const','var'
                                if (!resultPart.Contains(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id))// find(resultPart.begin(), resultPart.end(), pkbApi.getPierwszaLiniaProcedury(*parameter1)) == resultPart.end())
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id);
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Dodaje możliwość z parameter2 do wyniku, gdy call(parmeter2,*) gdzie * = 'any','const','var'
                                if (!resultPart.Contains(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id))
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id);
                            }
                            else
                            {
                                //Zwraca wszystko, czy call(1,_) lub call(_,1)
                                if (field1.Type != "constant" && field1.Type != "string")
                                {
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id);
                                }
                                if (field2.Type != "constant" && field2.Type != "string")
                                {
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id);
                                }
                            }
                        }
                    }
                }

            }
            return new List<int>(resultPart);
        }

        private List<int> CallStarResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            SortedSet<int> candidatesForParameter1 = new SortedSet<int>();
            SortedSet<int> candidatesForParameter2 = new SortedSet<int>();
            SortedSet<int> resultPart = new SortedSet<int>();
            List<int> returnIt = new List<int>();
            if (field1 == null || field2 == null)
            {
                return returnIt;
            }
            string firstParameterType = field1.Type;
            string secondParameterType = field2.Type;
            int firstProcedureId = -1;
            int secondProcedureId = -1;

            //Dla parametru pierwszego tworzy się lista z możliwymi wartościami dla tego parametru
            if (firstParameterType == "string")
            {
                var procedure = pkbApi.GetProcedure(field1.Value);
                firstProcedureId = procedure != null ? procedure.Id : -1;
                if (firstProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter1.Add(firstProcedureId);
            }
            else if (firstParameterType == "constant")
            {
                var procedure = pkb.Procedures.FirstOrDefault(x => x.Id == int.Parse(field1.Value));
                firstProcedureId = procedure != null ? procedure.Id : -1;
                if (firstProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter1.Add(firstProcedureId);
            }
            else
            {
                for (int i = 0; i <= pkb.Procedures.Count; i++)
                {
                    candidatesForParameter1.Add(i);
                }
            }

            //Dla parametru drugiego tworzy się lista z możliwymi wartościami dla tego parametru
            if (secondParameterType == "string")
            {
                var procedure = pkbApi.GetProcedure(field2.Value);
                secondProcedureId = procedure != null ? procedure.Id : -1;
                if (secondProcedureId == -1)
                {
                    return returnIt;
                }
                candidatesForParameter2.Add(secondProcedureId);
            }
            else if (secondParameterType == "constant")
            {

                var procedure = pkb.Procedures.FirstOrDefault(x => x.Id == int.Parse(field2.Value));
                secondProcedureId = procedure != null ? procedure.Id : -1;
                if (secondProcedureId == -1)
                {
                    return returnIt;
                }

                candidatesForParameter2.Add(secondProcedureId);
            }
            else
            {
                for (int i = 0; i <= pkb.Procedures.Count; i++)
                {
                    candidatesForParameter1.Add(i);
                }
            }

            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru pierwszego
            if (field1.Type != "constant" && field1.Type != "any")
            {
                candidatesForParameter1 = CutSetLines(field1.Value, candidatesForParameter1);
            }
            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru drugiego
            if (field2.Type != "constant" && field2.Type != "any")
            {
                candidatesForParameter2 = CutSetLines(field2.Value, candidatesForParameter2);
            }

            if (firstParameterType == "constant" && secondParameterType == "constant")
            {

                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId)) // TODO:: dać to star
                {
                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == firstProcedureId).Id);
                }
                return new List<int>(resultPart);
            }
            else if (firstParameterType == "string" && secondParameterType == "string")
            {
                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId))
                {
                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == firstProcedureId).Id);
                }

                return new List<int>(resultPart);
            }
            else if (firstParameterType == "any" && secondParameterType == "any")
            {
                //Jesli "any" spełnia wymagania to wszystkie linie są dobre
                if (pkb.Calls.IsCall(firstProcedureId, secondProcedureId))
                {
                    resultPart.UnionWith(pkb.Procedures.Select(x => x.Id));
                }
                //Jeśli "any" nie spełnia wymagań to wszystkie linie są złe (nie ma procedur w programie)
                returnIt = new List<int>(resultPart);
                return returnIt;
            }
            else if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
            {
                //Jeśli w zapytaniu sa dwie takie same wartości, a nie jest to "any" to zwraca pusty wynik, ponieważ nie można wywołać rekurencyjnie
                return new List<int>(resultPart);
            }
            else
            {
                foreach (var parameter1 in candidatesForParameter1)
                {
                    foreach (var parameter2 in candidatesForParameter2)
                    {
                        if (pkb.Calls.IsCall(parameter1, parameter2) && parameter1 != parameter2)// TODO:: dać to star
                        {
                            if (selectValue == field1.Value && selectValue != "boolean")
                            {
                                //Dodaje możliwość z parameter1 do wyniku, gdy call(parmeter1,*) gdzie * = 'any','const','var'
                                if (!resultPart.Contains(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id))// find(resultPart.begin(), resultPart.end(), pkbApi.getPierwszaLiniaProcedury(*parameter1)) == resultPart.end())
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id);
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Dodaje możliwość z parameter2 do wyniku, gdy call(parmeter2,*) gdzie * = 'any','const','var'
                                if (!resultPart.Contains(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id))
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id);
                            }
                            else
                            {
                                //Zwraca wszystko, czy call(1,_) lub call(_,1)
                                if (field1.Type != "constant" && field1.Type != "string")
                                {
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter1).Id);
                                }
                                if (field2.Type != "constant" && field2.Type != "string")
                                {
                                    resultPart.Add(pkb.Procedures.FirstOrDefault(x => x.Id == parameter2).Id);
                                }
                            }
                        }
                    }
                }

            }
            return new List<int>(resultPart);
        }

        private List<int> UsesResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            SortedSet<int> setLines1 = new SortedSet<int>();
            SortedSet<int> setLines2 = new SortedSet<int>();

            if (field1.Type == "constant" && field2.Type != "constant")
            {
                int param1 = int.Parse(field1.Value);
                List<int> a = this.pkbApi.GetNodes(Instruction.Call, false).Select(x => x.Id).ToList();
                if (a.IndexOf(param1) != -1)
                {
                    var name = this.pkbApi.GetNodes(Instruction.Call, false).FirstOrDefault(x => x.Id == param1).Instruction;
                    name = name.Substring(4, name.Length);
                    setLines1.UnionWith(this.pkb.Procedures.FirstOrDefault(x => x.Name == name).Nodes.Select(x => x.Id));
                }
                //else if (pkb.Procedures.Count - 1 >= param1)
                //{
                //	var tmp = pkbApi.GetNodes(Instruction.Procedure, false).ToList();
                //	setLines1.UnionWith(pkb.Procedures.FirstOrDefault(x => x.Id == param1)?.Nodes?.Select(x => x.Id));
                //}
                else
                    setLines1.Add(param1);

                if (field2.Type == "variable" || field2.Type == "any")
                {

                    setLines2.UnionWith(pkb.Variables.Select(x => x.Id));
                }
                else if (field2.Type == "string")
                {
                    var param2 = pkb.Variables.FirstOrDefault(x => x.Name == field2.Value);
                    if (param2 != null)
                        setLines2.Add(param2.Id);
                }
            }
            else if (field1.Type != "constant" && field2.Type != "constant")
            {
                //Sprawdzenie atrybutu z pola pierwszego
                if (field1.Type == "if")
                {
                    SortedSet<Node> ifLinesStart = new SortedSet<Node>(pkbApi.GetNodes(Instruction.If, false));
                    foreach (var item in ifLinesStart)
                    {
                        setLines1.Add(item.Id);
                        setLines1.UnionWith(item.Nodes.Select(x => x.Id));
                    }
                }
                else if (field1.Type == "while")
                {
                    SortedSet<Node> ifLinesStart = new SortedSet<Node>(pkbApi.GetNodes(Instruction.Loop, false));
                    foreach (var item in ifLinesStart)
                    {
                        setLines1.Add(item.Id);
                        setLines1.UnionWith(item.Nodes.Select(x => x.Id));
                    }
                }
                else if (field1.Type == "assign")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign, false).Select(x => x.Id));
                }
                else if (field1.Type == "procedure")
                {
                    foreach (var item in pkbApi.GetNodesDictionary(Instruction.Procedure))
                    {
                        setLines1.Add(item.Key);
                        setLines1.UnionWith(item.Value);
                    }
                }
                else if (field1.Type == "call")
                {
                    foreach (var item in pkbApi.GetNodesDictionary(Instruction.Call))
                    {
                        setLines1.UnionWith(item.Value);
                    }
                }
                else if (field1.Type == "string")
                {
                    var procedure = pkb.Procedures.FirstOrDefault(x => x.Name == field1.Value);
                    if (procedure != null)
                    {
                        var tmp = pkbApi.GetNodesDictionary(Instruction.Procedure).FirstOrDefault(x => x.Key == procedure.Id).Value;
                        setLines1.UnionWith(tmp);
                    }
                }
                else if (field1.Type == "stmt")
                {
                    setLines1.UnionWith(pkbApi.ArrayForm.Select(x => x.Id).ToList());
                }

                //Zweryfikowanie atrybutu z pola 2
                if (field2.Type == "variable" || field2.Type == "any")
                {
                    setLines2.UnionWith(pkb.Variables.Select(x => x.Id));
                }
                else if (field2.Type == "string")
                {
                    var param2 = pkb.Variables.FirstOrDefault(x => x.Name == field2.Value);
                    if (param2 != null)
                        setLines2.Add(param2.Id);
                }
            }

            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru pierwszego
            if (field1.Type != "constant" && field1.Type != "any")
                setLines1 = this.CutSetLines(field1.Value, setLines1);
            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru drugiego
            if (field2.Type != "constant" && field2.Type != "any")
                setLines2 = this.CutSetLines(field2.Value, setLines2);

            List<int> resultPart = new List<int>();
            SortedSet<(string, int)> tmpUsesPairs = new SortedSet<(string, int)>();
            //Sprawdzenie cz wszystkie parametry były dobre, jesli nie sa to zwracana jest pusta lista 
            if (setLines1.Count != 0 && setLines2.Count != 0)
            {
                foreach (var l1 in setLines1)
                {

                    foreach (var l2 in setLines2)
                    {
                        if (pkb.Uses.dict.FirstOrDefault(x => x.Key.Id == l1).Value.Exists(x => x.Id == l2))
                        {
                            var addingObj = (pkb.Variables.FirstOrDefault(x => x.Id == l2).Name, l1);
                            if (firstUses == true)
                            {
                                UsesPairs.Add(addingObj);
                            }
                            else
                            {
                                tmpUsesPairs.Add(addingObj);
                            }

                            if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeżeli oba parametry śa takie same, a nie są to constant, znaczy to że nie ma odpowiedzi
                                firstUses = false;
                                UsesPairs.Clear();
                                return resultPart;
                            }
                            else if (selectValue == field1.Value && selectValue != "boolean")
                            {
                                //Jeśli parametr pierwszy jest tym, którego szukamy to wybieramy z listy pierwszej
                                if (!resultPart.Exists(x => x == l1))
                                {
                                    resultPart.Add(l1);
                                }
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
                                if (!resultPart.Exists(x => x == l2))
                                {
                                    resultPart.Add(l2);
                                }
                            }
                            else
                            {
                                //Jeśli żaden parametr nie jest szukany to zwracane są wszystkie wartości
                                firstUses = false;
                                return lines.Select(x => x.Id).ToList();
                            }
                        }
                    }
                }


                if (firstUses != true)
                {
                    SortedSet<string> usesPairsName = new SortedSet<string>();
                    SortedSet<string> actPairsName = new SortedSet<string>();
                    SortedSet<int> usesPairsLines = new SortedSet<int>();
                    SortedSet<int> actPairsLines = new SortedSet<int>();

                    Console.WriteLine();
                    foreach (var item in tmpUsesPairs)
                    {
                        actPairsName.Add(item.Item1);
                        actPairsLines.Add(item.Item2);

                        Console.WriteLine($"TMP USES --. {item.Item1} {item.Item2}");

                    }
                    Console.WriteLine();
                    foreach (var item in actPairsName)
                    {
                        Console.WriteLine($"ACT NAME --. {item}");
                    }
                    Console.WriteLine();
                    foreach (var item in actPairsLines)
                    {
                        Console.WriteLine($"ACT LINE --. {item}");
                    }
                    Console.WriteLine();
                    foreach (var item in UsesPairs)
                    {
                        if (actPairsName.Contains(item.Item1) || actPairsLines.Contains(item.Item2))
                        {
                            Console.WriteLine($"Usuwam: {item.Item1} {item.Item2}");
                            UsesPairs.Remove(item);
                        }
                    }
                    foreach (var item in UsesPairs)
                    {
                        Console.WriteLine($"PAIR -->: {item.Item1} {item.Item2}");
                    }
                }

            }

            firstUses = false;
            return resultPart;
        }

        private List<int> FollowsResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            throw new NotImplementedException();
        }

        private List<int> FollowsStarResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            throw new NotImplementedException();
        }

        private List<int> ParentResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            SortedSet<int> setLines1 = new SortedSet<int>();
            SortedSet<int> setLines2 = new SortedSet<int>();
            //Pierwszy parametr (Field1) sprawdzany w relacji Parent
            if (field1.Type == "constant" && field2.Type != "constant")
            {
                int param = int.Parse(field1.Value);
                setLines1.Add(param);
                if (field2.Type == "stmt" || field2.Type == "any")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign).Select(x => x.Id));
                    SortedSet<int> tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                    tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                    tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.Call).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                }
                else if (field2.Type == "while")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                }
                else if (field2.Type == "if")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                }
                else if (field2.Type == "call")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Call).Select(x => x.Id));
                }
                else if (field2.Type == "assign")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign).Select(x => x.Id));
                }
            }
            else if (field1.Type != "constant" && field2.Type == "constant")
            {
                int param = int.Parse(field2.Value);
                setLines2.Add(param);
                if (field1.Type == "stmt" || field1.Type == "any")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                    SortedSet<int> tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                    setLines1.UnionWith(tmp);
                }
                else if (field1.Type == "while")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                }
                else if (field1.Type == "if")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                }
            }
            else if (field1.Type == "constant" && field2.Type == "constant")
            {
                int param1 = int.Parse(field1.Value);
                int param2 = int.Parse(field2.Value);
                if (pkb.Parent.dict.FirstOrDefault(x => x.Key.Id == param1).Value.Exists(x => x.Id == param2))
                {
                    return lines.Select(x => x.Id).ToList();
                }
            }
            else
            {
                if (field1.Type == "stmt" || field1.Type == "any")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                    SortedSet<int> tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                    setLines1.UnionWith(tmp);
                }
                else if (field1.Type == "while")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                }
                else if (field1.Type == "if")
                {
                    setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                }
                if (field2.Type == "stmt" || field2.Type == "prog_line" || field2.Type == "any")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign).Select(x => x.Id));
                    SortedSet<int> tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                    tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                    tmp = new SortedSet<int>(pkbApi.GetNodes(Instruction.Call).Select(x => x.Id));
                    setLines2.UnionWith(tmp);
                }
                else if (field2.Type == "while")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Loop).Select(x => x.Id));
                }
                else if (field2.Type == "if")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.If).Select(x => x.Id));
                }
                else if (field2.Type == "call")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Call).Select(x => x.Id));
                }
                else if (field2.Type == "assign")
                {
                    setLines2 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign).Select(x => x.Id));
                }
            }

            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru pierwszego
            if (field1.Type != "constant" && field1.Type != "any")
                setLines1 = CutSetLines(field1.Value, setLines1);

            //Biorąc pod uwage części zapytania z "with", skracana jest lista parametru drugiego
            if (field2.Type != "constant" && field2.Type != "any")
                setLines2 = CutSetLines(field2.Value, setLines2);

            List<int> resultPart = new List<int>();
            //Dla pobranych parametrow setLines1 oraz setLines2 sprawdzane są zależności, a następnie porównanie ich z wartościami na lines (szukana wartość)
            if (setLines1.Count != 0 && setLines2.Count != 0)
            {
                foreach (var l1 in setLines1)
                {
                    foreach (var l2 in setLines2)
                    {
                        if (pkb.Parent.dict.FirstOrDefault(x => x.Key.Id == l1).Value.Exists(x => x.Id == l2))
                        {
                            if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeżeli oba parametry śa takie same, a nie są to constant, znaczy to że nie ma odpowiedzi 
                                return resultPart;
                            }
                            else if (selectValue == field1.Value && selectValue != "boolean" && lines.Exists(x => x.Id == l1))
                            {
                                //Jeśli parametr pierwszy jest tym, którego szukamy to wybieramy z listy pierwszej
                                if (!resultPart.Exists(x => x == l1))
                                    resultPart.Add(l1);
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean" && lines.Exists(x => x.Id == l2))
                            {
                                //Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
                                if (!resultPart.Exists(x => x == l2))
                                    resultPart.Add(l2);
                            }
                            else
                            {
                                //Jeśli żaden parametr nie jest szukany to zwracane są wszystkie wartości
                                return lines.Select(x => x.Id).ToList();
                            }
                        }
                    }
                }
            }


            return resultPart;
        }

        private List<int> ParentStarResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            throw new NotImplementedException();
        }

        private List<int> ModifiesResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            var setLines1 = new SortedSet<int>();
            var setLines2 = new SortedSet<INode>();

            if (field1.Type == "constant" && field2.Type != "constant")
            {
                var a = this.pkbApi.GetNodes(Instruction.Call).Select(x => x.Id).ToList();

                var param1 = int.Parse(field1.Value);
                if (a.Exists(x => x == param1))
                {
                    var name = this.pkbApi.GetNodes(Instruction.Call, false).FirstOrDefault(x => x.Id == param1).Instruction;
                    name = name.Substring(4, name.Length);
                    var procedureLines = this.pkbApi.GetNodesDictionary(Instruction.Procedure).FirstOrDefault(x => x.Key == this.pkbApi.GetProcedure(name).Id);
                    setLines1.UnionWith(procedureLines.Value);

                }
                //else if (pkb.Procedures.Count - 1 >= param1)
                //{
                //    var tmp = pkbApi.GetNodes(Instruction.Procedure, false).FirstOrDefault(x => x.Id == param1);
                //    setLines1.UnionWith(pkb.Procedures.FirstOrDefault(x => x.Id == param1)?.Nodes?.Select(x => x.Id));
                //}
                else
                    setLines1.Add(param1);

                if (field2.Type == "variable" || field2.Type == "any")
                {

                    setLines2.UnionWith(pkb.Variables);
                }
                else if (field2.Type == "string")
                {
                    var param2 = pkb.Variables.FirstOrDefault(x => x.Name == field2.Value);
                    if (param2 != null)
                        setLines2.Add(param2);
                }
                else if (field1.Type != "constant" && field2.Type != "constant")
                {
                    //Zweryfikowanie atrybutu z pola 1
                    if (field1.Type == "if")
                    {
                        SortedSet<Node> ifLinesStart = new SortedSet<Node>(pkbApi.GetNodes(Instruction.If, false));
                        foreach (var item in ifLinesStart)
                        {
                            setLines1.UnionWith(item.Nodes.Select(x => x.Id));
                        }
                    }
                    else if (field1.Type == "while")
                    {
                        SortedSet<Node> ifLinesStart = new SortedSet<Node>(pkbApi.GetNodes(Instruction.Loop, false));
                        foreach (var item in ifLinesStart)
                        {
                            setLines1.UnionWith(item.Nodes.Select(x => x.Id));
                        }
                    }
                    else if (field1.Type == "assign")
                    {
                        setLines1 = new SortedSet<int>(pkbApi.GetNodes(Instruction.Assign, false).Select(x => x.Id));
                    }
                    else if (field1.Type == "procedure")
                    {
                        foreach (var item in pkbApi.GetNodesDictionary(Instruction.Procedure))
                        {
                            setLines1.Add(item.Key);
                            setLines1.UnionWith(item.Value);
                        }
                    }
                    else if (field1.Type == "call")
                    {
                        foreach (var item in pkbApi.GetNodesDictionary(Instruction.Call))
                        {
                            setLines1.UnionWith(item.Value);
                        }
                    }
                    else if (field1.Type == "string")
                    {
                        var procedure = pkb.Procedures.FirstOrDefault(x => x.Name == field1.Value);
                        if (procedure != null)
                        {
                            var tmp = pkbApi.GetNodesDictionary(Instruction.Procedure).FirstOrDefault(x => x.Key == procedure.Id).Value;
                            setLines1.UnionWith(tmp);
                        }
                    }
                    else if (field1.Type == "stmt")
                    {
                        setLines1.UnionWith(pkbApi.ArrayForm.Select(x => x.Id).ToList());
                    }

                    //Zweryfikowanie atrybutu z pola 2
                    if (field2.Type == "variable" || field2.Type == "any")
                    {
                        setLines2.UnionWith(pkb.Variables);
                    }
                    else if (field2.Type == "string")
                    {
                        var param2 = pkb.Variables.FirstOrDefault(x => x.Name == field2.Value);
                        if (param2 != null)
                            setLines2.Add(param2);
                    }
                }
            }

            //Biorąc pod uwagę części zapytanie z "with" następuje skrócenie listy parametru 1
            if (field1.Type != "constant" && field1.Type != "any")
                setLines1 = CutSetLines(field1.Value, setLines1);
            //Biorąc pod uwagę części zapytanie z "with" następuje skrócenie listy parametru 2
            if (field2.Type != "constant" && field2.Type != "any")
                CutSetLines(field2.Value, new SortedSet<int>(setLines2.Select(x => x.Id))); // naprawic


            List<int> resultPart = new List<int>();
            //Sprawdzenie czy wszystkie parametry były dobre przy parsowaniu lub walidacji. Jeżeli nie to zwracana jest pusta lista
            if (setLines1.Count != 0 && setLines2.Count != 0)
            {
                foreach (var l1 in setLines1)
                {
                    foreach (var l2 in setLines2)
                    {
                        var xd = pkb.Modifies.dict.FirstOrDefault(a => a.Key.Id == l1 && a.Key.Name == ((IVariableNode)l2).Name).Key;
                        if (xd != null)
                        {
                            if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeżeli oba parametry śa takie same, a nie są to constant, znaczy to że nie ma odpowiedzi
                                return resultPart;
                            }
                            else if (selectValue == field1.Value && selectValue != "boolean")
                            {
                                //Jeśli parametr pierwszy jest tym, którego szukamy to wybieramy z listy pierwszej
                                if (resultPart.IndexOf(l1) != -1)
                                {
                                    resultPart.Add(l1);
                                }
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
                                if (resultPart.IndexOf(l2.Id) != -1)
                                {
                                    resultPart.Add(l1);
                                }
                            }
                            else
                            {
                                //Jeśli żaden parametr nie jest szukany to zwracane są wszystkie wartości
                                return lines.Select(x => x.Id).ToList();
                            }
                        }
                    }
                }
            }

            return resultPart;
        }

        SortedSet<int> CutSetLines(string fieldValue, SortedSet<int> setLines)
        {
            List<int> fieldMap = new List<int>();
            SortedDictionary<string, List<int>> withMap = new SortedDictionary<string, List<int>>();
            if (withMap.Count(x => x.Key == fieldValue) > 0) fieldMap = withMap[fieldValue];
            SortedSet<int> setLines1tmp = new SortedSet<int>();
            List<int> allMap = new List<int>();
            if (withMap.Count(x => x.Key == "all") > 0) allMap = withMap["all"];

            if (withMap.Count != 0)
            {
                if (allMap.Count != 0)
                {
                    foreach (var item in setLines)
                    {
                        if (allMap.FirstOrDefault(x => x == item) != 0)
                        {
                            setLines1tmp.Add(item);
                        }

                    }
                    setLines.Clear();
                    setLines.UnionWith(setLines);
                }
            }

            if (allMap.Count == 0 && withMap.Count(x => x.Key == "all") > 0)
            {
                setLines.Clear();
            }

            if (fieldMap.Count != 0)
            {
                foreach (var item in setLines)
                {
                    if (allMap.FirstOrDefault(x => x == item) != 0)
                    {
                        setLines1tmp.Add(item);
                    }

                }
                setLines.Clear();
                setLines.UnionWith(setLines);

            }
            return setLines;
        }

        private void WithResults(Field field1, Field field2, List<Node> lines)
        {
            throw new NotImplementedException();
        }
    }
}
