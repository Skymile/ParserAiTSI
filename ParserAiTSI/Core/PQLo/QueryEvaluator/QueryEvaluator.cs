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
                    if (this.resultType == "assign" || this.resultType == "procedure")
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
                foreach (var item in temporary)
                {
                    if (this.resultType == "variable")
                    {
                        List<int> variableIds = new List<int>();
                        if (isModifies) // Czy występuje tylko relacja . Modifies
                        {

                            foreach (var dict in pkb.Modifies.dict)
                            {
                                List<int> tmp = dict.Value.Select(x => x.Id).ToList();
                                if (tmp.IndexOf(item) != -1)
                                {
                                    string name = dict.Key.Name;
                                    if (result.FirstOrDefault(i => i == name) == null && variableIds.FirstOrDefault(x => x == dict.Key.Id) == null || variableIds.Count == 0)
                                        result.Add(name);
                                }

                            }
                        }
                    }
                }
            }
            return result;
        }

        private List<int> CallResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
        private List<int> CallStarResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
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
                    var dict = pkbApi.GetNodes(Instruction.Procedure, true);
                    // TODO: zrobic linie procedury
                    foreach (var item in dict)
                    {
                        int procParam = item.Id;
                        foreach (var node in item.Nodes)
                        {

                        }
                    }


                }
                else if (field1.Type == "call") // TODO::
                {
                    //List<int> callLinesStart = pkb.getTablicaLiniiKodu().getLinieCall();
                    //for (int i = 0; i < callLinesStart.Count; i++)
                    //{
                    //    string nazwaProcedury = pkb.getTablicaLiniiKodu().getWywolanaNazwaProcedury(callLinesStart[i]);
                    //    int idProcedury = pkb.getTablicaProcedur().getIdProcedury(nazwaProcedury);
                    //    if (idProcedury != -1)
                    //    {
                    //        List<int> callLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury);

                    //        for (int j = 0; j < callLines.Count; j++)
                    //        {
                    //            setLines1.Add(callLines[j]);
                    //        }
                    //    }
                    //}
                }
                else if (field1.Type == "string")
                {
                    var procedure = pkb.Procedures.FirstOrDefault(x => x.Name == field1.Value);
                    if (procedure != null)
                    {
                        //List<int> procLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury); // TODO:: zrobic linie procedury
                        //int j = 0;
                        //while (j < procLines.Count)
                        //{
                        //    setLines1.Add(procLines[j]);
                        //    j++;
                        //}
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
                        if (true)//pkb.getUses().uses(*l1, pkb.getTablicaZmiennych().getNazwaZmiennej(*l2)) == true)
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
                                if (resultPart.IndexOf(l1) == -1)
                                {
                                    resultPart.Add(l1);
                                }
                            }
                            else if (selectValue == field2.Value && selectValue != "boolean")
                            {
                                //Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
                                if (resultPart.IndexOf(l2) == -1)
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
                    SortedSet<string> actPairsName  = new SortedSet<string>();
                    SortedSet<int> usesPairsLines   = new SortedSet<int>();
                    SortedSet<int> actPairsLines    = new SortedSet<int>();

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

        private List<int> FollowsResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
        private List<int> FollowsStarResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
        private List<int> ParentResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
        private List<int> ParentStarResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();

        private List<int> ModifiesResult(Field field1, Field field2, List<Node> lines, string selectValue)
        {
            var setLines1 = new SortedSet<int>();
            var setLines2 = new SortedSet<INode>();

            if (field1.Type == "constant" && field2.Type != "constant")
            {
                var a = this.pkbApi.GetNodes(Instruction.Call, false).Select(x => x.Id).ToList();

                var param1 = int.Parse(field1.Value);
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
                        var dict = pkbApi.GetNodes(Instruction.Procedure, true);
                        // TODO: zrobic linie procedury
                        foreach (var item in dict)
                        {
                            int procParam = item.Id;
                            foreach (var node in item.Nodes)
                            {

                            }
                        }


                    }
                    else if (field1.Type == "call")
                    {
                        //List<int> callLinesStart = pkb.getTablicaLiniiKodu().getLinieCall();
                        //for (int i = 0; i < callLinesStart.Count; i++)
                        //{
                        //    string nazwaProcedury = pkb.getTablicaLiniiKodu().getWywolanaNazwaProcedury(callLinesStart[i]);
                        //    int idProcedury = pkb.getTablicaProcedur().getIdProcedury(nazwaProcedury);
                        //    if (idProcedury != -1)
                        //    {
                        //        List<int> callLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury);

                        //        for (int j = 0; j < callLines.Count; j++)
                        //        {
                        //            setLines1.Add(callLines[j]);
                        //        }
                        //    }
                        //}
                    }
                    else if (field1.Type == "string")
                    {
                        var procedure = pkb.Procedures.FirstOrDefault(x => x.Name == field1.Value);
                        if (procedure != null)
                        {
                            //List<int> procLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury); // TODO:: zrobic linie procedury
                            //int j = 0;
                            //while (j < procLines.Count)
                            //{
                            //    setLines1.Add(procLines[j]);
                            //    j++;
                            //}
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

        private void WithResults(Field field1, Field field2, List<Core.Node> lines) => throw new NotImplementedException();
    }
}
