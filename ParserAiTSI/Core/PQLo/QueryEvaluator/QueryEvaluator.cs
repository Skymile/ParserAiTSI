using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Core.PQLo.QueryEvaluator
{
	public class QueryEvaluator
    {
		private bool firstUses;
		private string resultType;
		private PKBApi pkbApi;
		private PKB pkb;
		public List<string> ResultQuery(ITree<PQLNode> Tree)
        {
			List<string> result = new List<string>();
			List<Core.Node> lines = new List<Core.Node>();
			SortedSet<Core.Node> setLines = new SortedSet<Core.Node>();
			string selectValue = null;
			
			var beginNode = Tree.Root;
			bool isModifies = false;
			bool isUses = false;

			this.firstUses = true;

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
						lines = pkbApi.GetNodes(Instruction.All, false).ToList();
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
						lines = this.ModifiesResult(item.Field1,
								item.Field2, lines, selectValue);
						isModifies = true;
					}
					//Zweryfikowanie wystąpienia relacji Parent lub Parent*
					if (item.NodeType == "parent")
					{
						//Pareznt z *
						if (item.IsStar)
						{
							lines = this.ParentStarResult(item.Field1,
									item.Field2, lines,
									selectValue);
						}
						//Parent bez *
						else
						{
							lines = this.ParentResult(item.Field1,
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
							lines = this.FollowsStarResult(item.Field1,
									item.Field2, lines,
									selectValue);
						}
						//Follows bez *
						else
						{
							lines = this.FollowsResult(item.Field1,
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
							lines = this.UsesResult(item.Field1, item.Field2, lines, selectValue);
							isUses = true;
						}
					}
					// Zweryfikowanie wystąpienia relacji Calls lub Calls*
					if (item.NodeType == "calls")
					{
						//Calls z *
						if (item.IsStar)
						{
							lines = this.CallStarResult(item.Field1,
									item.Field2, lines,
									selectValue);
						}
						//Calls bez *
						else
						{
							lines = this.CallResult(item.Field1,
									item.Field2, lines,
									selectValue);
						}
					}

					if (item.NodeType == "affects" || item.NodeType == "next")
					{
						lines = new List<Core.Node>();
					}
				}

				if ((item.Type) == "withNode")
				{
					this.WithResults(item.Field1, item.Field2, lines);
				}
			}

			result.Clear();


			//if (!lines.empty())
			//{
			//	if (wynikType == "boolean")
			//	{
			//		result.Add("true");
			//	}
			//	else
			//	{
			//		for (int i = 0; i < lines.Count; i++)
			//		{
			//			if (wynikType == "procedure")
			//			{
			//				string name = pkbApi.getNazwaProcedury(pkbApi.getIdProcedury(lines[i]));
			//				if (find(result.begin(), result.end(), name) >= result.begin())
			//					result.Add(name);
			//			}
			//			else if (wynikType == "variable")
			//			{
			//				if (isUses && !isModifies) // Czy występuje tylko relacja . Uses
			//				{
			//					for (set<std::pair<string, int>>  it = usesPairs.begin(); it != usesPairs.end(); ++it)
			//					{
			//						if ((*it).second == lines[i] && find(result.begin(), result.end(), (*it).first) == result.end())
			//						{
			//							result.Add((*it).first);
			//						}
			//					}
			//				}

			//				SortedDictionary<int, List<int>> varUsesLines = pkb.getUses().getAllUses();
			//				SortedDictionary<int, List<int>> varModifiesLines = pkb.getModifies().getWszystkieModifies();
			//				List<int> variableIds;
			//				if (withMap.count(selectValue) > 0) variableIds = withMap[selectValue];

			//				if (!isUses && isModifies) // Czy występuje tylko relacja . Modifies
			//				{

			//					for (SortedDictionary<int, List<int>>  it = varModifiesLines.begin(); it != varModifiesLines.end(); ++it)
			//					{
			//						List<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb.getTablicaZmiennych().getNazwaZmiennej((*it).first);
			//							if (find(result.begin(), result.end(), name) == result.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								result.Add(name);
			//						}
			//					}
			//				}
			//				else if (isUses && isModifies) // Wystapienie obu relacji (Uses i Modifies)
			//				{
			//					List<string> resultUses;
			//					for (SortedDictionary<int, List<int>>  it = varUsesLines.begin(); it != varUsesLines.end(); ++it)
			//					{
			//						List<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb.getTablicaZmiennych().getNazwaZmiennej((*it).first);
			//							if (find(resultUses.begin(), resultUses.end(), name) == resultUses.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								resultUses.Add(name);
			//						}
			//					}

			//					List<string> resultModifies;
			//					for (SortedDictionary<int, List<int>>  it = varModifiesLines.begin(); it != varModifiesLines.end(); ++it)
			//					{
			//						List<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb.getTablicaZmiennych().getNazwaZmiennej((*it).first);
			//							if (find(resultModifies.begin(), resultModifies.end(), name) == resultModifies.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								resultModifies.Add(name);
			//						}
			//					}

			//					for (int iU = 0; iU < resultUses.Count; iU++)
			//					{
			//						for (int iM = 0; iM < resultModifies.Count; iM++)
			//						{
			//							if (resultUses[iU] == resultModifies[iM] && find(result.begin(), result.end(), resultUses[iU]) == result.end())
			//							{
			//								result.Add(resultUses[iU]);
			//							}
			//						}
			//					}
			//				}

			//			}
			//			else
			//			{
			//				stringstream ss;
			//				ss << lines[i];
			//				if (find(result.begin(), result.end(), ss.str()) >= result.begin())
			//					result.Add(ss.str());
			//			}
			//		}
			//	}
			//}
			//else
			//{
			//	if (wynikType == "boolean")
			//	{
			//		result.Add("false");
			//	}
			//	else
			//	{
			//		//result.Add("NONE");
			//	}
			//}

			return result;
		}

		private List<int> ModifiesResult(Field field1, Field field2, List<Node> lines, string selectValue)
		{
			SortedSet<int> setLines1;
			SortedSet<int> setLines2;

			if (field1.Type == "constant" && field2.Type != "constant")
			{
				int param1 = int.Parse(field1.Value);
				List<int> a = pkbApi.GetNodes(Instruction.Call, false).Select(x => x.Id).ToList();
				if (param1 != a[a.Count - 1]) 
				{
					string name = pkb.Procedures.FirstOrDefault(x => x.Id == param1).Name
					List<int> b = pkbApi.GetProcedure(name);
					setLines1.UnionWith(b);
				}
				else if (pkb.getTablicaProcedur().getMaxIdProcedury() >= param1)
				{
					string name = pkb//pkb.getTablicaProcedur().getNazwaProcedury(param1);
					List<int> b = pkb.getTablicaProcedur().getLinieProcedury(pkb.getTablicaProcedur().getIdProcedury(name));
					setLines1.UnionWith(b);
				}
				else
					setLines1.Add(param1);

				if (field2.Type == "variable" || field2.Type == "any")
				{

					List<string> tmp = pkb.getTablicaZmiennych().getAllVar();
					int i = 0;
					while (i < tmp.Count)
					{
						setLines2.Add(pkb.getTablicaZmiennych().getZmiennaId(tmp[i]));
						i++;
					}
				}
				else if (field2.Type == "string")
				{
					int param2 = pkb.getTablicaZmiennych().getZmiennaId(field2.Value);
					if (param2 != -1)
						setLines2.Add(param2);
				}
			}
			else if (field1.Type != "constant" && field2.Type != "constant")
			{
				//Zweryfikowanie atrybutu z pola 1
				if (field1.Type == "if")
				{
					SortedDictionary<int, List<int>> ifLinesStart = pkb.getTablicaLiniiKodu().getLinieZawartosciIf();

					for (SortedDictionary<int, List<int>> it = ifLinesStart.begin(); it != ifLinesStart.end(); ++it)
					{
						List<int> ifLines = (*it).second;
						for (int i = 0; i < ifLines.Count; i++)
						{
							setLines1.Add(ifLines[i]);
						}
					}
				}
				else if (field1.Type == "while")
				{
					SortedDictionary<int, List<int>> whileLinesStart = pkb.getTablicaLiniiKodu().getLinieZawartosciWhile();

					for (SortedDictionary<int, List<int>>  it = whileLinesStart.begin(); it != whileLinesStart.end(); ++it)
					{
						List<int> whileLines = (*it).second;
						for (int i = 0; i < whileLines.Count; i++)
						{
							setLines1.Add(whileLines[i]);
						}
					}
				}
				else if (field1.Type == "assign")
				{
					setLines1 = pkb.getTablicaLiniiKodu().getOrdredAssignLines();
				}
				else if (field1.Type == "procedure")
				{
					SortedDictionary<int, List<int>> procLinesStart = pkb.getTablicaProcedur().getProceduresBodyLines();

					for (SortedDictionary<int, List<int>>  it = procLinesStart.begin(); it != procLinesStart.end(); ++it)
					{
						List<int> procLines = (*it).second;
						int procParam = pkb.getTablicaProcedur().getPierwszaLiniaProcedury((*it).first);
						setLines1.insert(procParam);
						for (int i = 0; i < procLines.Count; i++)
						{
							setLines1.insert(procLines[i]);
						}
					}
				}
				else if (field1.Type == "call")
				{
					List<int> callLinesStart = pkb.getTablicaLiniiKodu().getLinieCall();
					for (int i = 0; i < callLinesStart.Count; i++)
					{
						string nazwaProcedury = pkb.getTablicaLiniiKodu().getWywolanaNazwaProcedury(callLinesStart[i]);
						int idProcedury = pkb.getTablicaProcedur().getIdProcedury(nazwaProcedury);
						if (idProcedury != -1)
						{
							List<int> callLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury);

							for (int j = 0; j < callLines.Count; j++)
							{
								setLines1.insert(callLines[j]);
							}
						}
					}
				}
				else if (field1.Type == "string")
				{
					int idProcedury = pkb.getTablicaProcedur().getIdProcedury(field1.Value);
					if (idProcedury != -1)
					{
						List<int> procLines = pkb.getTablicaProcedur().getLinieProcedury(idProcedury);
						int j = 0;
						while (j < procLines.Count)
						{
							setLines1.insert(procLines[j]);
							j++;
						}
					}
				}
				else if (field1.Type == "stmt")
				{
					List<int> allLines = pkb.getTablicaLiniiKodu().getLines();
					int j = 0;
					while (j < allLines.Count)
					{
						setLines1.insert(allLines[j]);
						j++;
					}
				}

				//Zweryfikowanie atrybutu z pola 2
				if (field2.Type == "variable" || field2.Type == "any")
				{
					List<string> tmp = pkb.getTablicaZmiennych().getAllVar();
					int i = 0;
					while (i < tmp.Count)
					{
						setLines2.insert(pkb.getTablicaZmiennych().getZmiennaId(tmp[i]));
						i++;
					}
				}
				else if (field2.Type == "string")
				{
					int param2 = pkb.getTablicaZmiennych().getZmiennaId(field2.Value);
					if (param2 != -1)
						setLines2.insert(param2);
				}
			}

			//Biorąc pod uwagę części zapytanie z "with" następuje skrócenie listy parametru 1
			if (field1.Type != "constant" && field1.Type != "any")
				setLines1 = cutSetLines(field1.Value, setLines1);
			//Biorąc pod uwagę części zapytanie z "with" następuje skrócenie listy parametru 2
			if (field2.Type != "constant" && field2.Type != "any")
				setLines2 = cutSetLines(field2.Value, setLines2);


			List<int> resultPart;
			//Sprawdzenie czy wszystkie parametry były dobre przy parsowaniu lub walidacji. Jeżeli nie to zwracana jest pusta lista
			if (!setLines1.empty() && !setLines2.empty())
			{
				for (set<int>  l1 = setLines1.begin(); l1 != setLines1.end(); ++l1)
				{
					for (set<int>  l2 = setLines2.begin(); l2 != setLines2.end(); ++l2)
					{
						if (pkb.getModifies().modifies(*l1, pkb.getTablicaZmiennych().getNazwaZmiennej(*l2)) == true)
						{
							if (selectValue == field1.Value && selectValue == field2.Value && selectValue != "boolean")
							{
								//Jeżeli oba parametry śa takie same, a nie są to constant, znaczy to że nie ma odpowiedzi
								return resultPart;
							}
							else if (selectValue == field1.Value && selectValue != "boolean")
							{
								//Jeśli parametr pierwszy jest tym, którego szukamy to wybieramy z listy pierwszej
								if (find(resultPart.begin(), resultPart.end(), *l1) >= resultPart.end())
								{
									resultPart.push_back(*l1);
								}
							}
							else if (selectValue == field2.Value && selectValue != "boolean")
							{
								//Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
								if (find(resultPart.begin(), resultPart.end(), *l1) >= resultPart.end())
								{
									resultPart.push_back(*l1);
								}
							}
							else
							{
								cout << "wlazi" << endl;
								//Jeśli żaden parametr nie jest szukany to zwracane są wszystkie wartości
								return lines;
							}
						}
					}
				}
			}

			return resultPart;
		}

		private List<int> ParentStarResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> ParentResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> FollowsStarResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> FollowsResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> UsesResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> CallResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> CallStarResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private void WithResults(Field field1, Field field2, List<Core.Node> lines) => throw new NotImplementedException();


	}
}
