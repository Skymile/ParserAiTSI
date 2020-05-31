using Common;
using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.PQLo.QueryEvaluator
{
    public class QueryEvaluator
    {
		private bool firstUses;
		private string resultType;

		public List<string> ResultQuery(ITree<PQLNode> Tree)
        {
			List<string> result = new List<string>();
			List<int> lines = new List<int>();
			SortedSet<int> setLines;
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
					if (this.resultType == "assign")
					{
						//lines = pkb.getTablicaLiniiKodu().getLinieAssign();
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "while")
					{
						//lines = pkb.getTablicaLiniiKodu().getLinieWhile();
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "variable")
					{
						//lines = pkb.getTablicaLiniiKodu().getLines();
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "prog_line")
					{
						//lines = pkb.getTablicaLiniiKodu().getLines();
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "procedure")
					{
						//lines = pkbApi.getProceduresLines();
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "stmt" || this.resultType == "boolean")
					{
						List<int> tmp2;
						//tmp2 = pkb.getTablicaLiniiKodu().getLinieAssign();
						//setLines.insert(tmp2.begin(), tmp2.end());
						//tmp2 = pkb.getTablicaLiniiKodu().getLinieWhile();
						//setLines.insert(tmp2.begin(), tmp2.end());
						//tmp2 = pkb.getTablicaLiniiKodu().getLinieCall();
						//setLines.insert(tmp2.begin(), tmp2.end());
						//tmp2 = pkb.getTablicaLiniiKodu().getLinieIf();
						//setLines.insert(tmp2.begin(), tmp2.end());
						//std::copy(setLines.begin(), setLines.end(), std::inserter(lines, lines.end()));
						selectValue = item.Field1.Value;
					}
					else if (this.resultType == "if")
					{
						//lines = pkb.getTablicaLiniiKodu().getLinieIf();
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
						List<int> empty = new List<int>();
						lines = empty;
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
			//		result.push_back("true");
			//	}
			//	else
			//	{
			//		for (size_t i = 0; i < lines.size(); i++)
			//		{
			//			if (wynikType == "procedure")
			//			{
			//				string name = pkbApi->getNazwaProcedury(pkbApi->getIdProcedury(lines[i]));
			//				if (find(result.begin(), result.end(), name) >= result.begin())
			//					result.push_back(name);
			//			}
			//			else if (wynikType == "variable")
			//			{
			//				if (isUses && !isModifies) // Czy występuje tylko relacja -> Uses
			//				{
			//					for (set<std::pair<string, int>>::iterator it = usesPairs.begin(); it != usesPairs.end(); ++it)
			//					{
			//						if ((*it).second == lines[i] && find(result.begin(), result.end(), (*it).first) == result.end())
			//						{
			//							result.push_back((*it).first);
			//						}
			//					}
			//				}

			//				SortedDictionary<int, List<int>> varUsesLines = pkb->getUses()->getAllUses();
			//				SortedDictionary<int, List<int>> varModifiesLines = pkb->getModifies()->getWszystkieModifies();
			//				vector<int> variableIds;
			//				if (withMap.count(selectValue) > 0) variableIds = withMap[selectValue];

			//				if (!isUses && isModifies) // Czy występuje tylko relacja -> Modifies
			//				{

			//					for (map<int, vector<int>>::iterator it = varModifiesLines.begin(); it != varModifiesLines.end(); ++it)
			//					{
			//						vector<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb->getTablicaZmiennych()->getNazwaZmiennej((*it).first);
			//							if (find(result.begin(), result.end(), name) == result.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								result.push_back(name);
			//						}
			//					}
			//				}
			//				else if (isUses && isModifies) // Wystapienie obu relacji (Uses i Modifies)
			//				{
			//					vector<string> resultUses;
			//					for (map<int, vector<int>>::iterator it = varUsesLines.begin(); it != varUsesLines.end(); ++it)
			//					{
			//						vector<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb->getTablicaZmiennych()->getNazwaZmiennej((*it).first);
			//							if (find(resultUses.begin(), resultUses.end(), name) == resultUses.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								resultUses.push_back(name);
			//						}
			//					}

			//					vector<string> resultModifies;
			//					for (map<int, vector<int>>::iterator it = varModifiesLines.begin(); it != varModifiesLines.end(); ++it)
			//					{
			//						vector<int> tmp = (*it).second;
			//						if (find(tmp.begin(), tmp.end(), lines[i]) != tmp.end())
			//						{
			//							string name = pkb->getTablicaZmiennych()->getNazwaZmiennej((*it).first);
			//							if (find(resultModifies.begin(), resultModifies.end(), name) == resultModifies.end() && (find(variableIds.begin(), variableIds.end(), (*it).first) != variableIds.end() || variableIds.empty()))
			//								resultModifies.push_back(name);
			//						}
			//					}

			//					for (size_t iU = 0; iU < resultUses.size(); iU++)
			//					{
			//						for (size_t iM = 0; iM < resultModifies.size(); iM++)
			//						{
			//							if (resultUses[iU] == resultModifies[iM] && find(result.begin(), result.end(), resultUses[iU]) == result.end())
			//							{
			//								result.push_back(resultUses[iU]);
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
			//					result.push_back(ss.str());
			//			}
			//		}
			//	}
			//}
			//else
			//{
			//	if (wynikType == "boolean")
			//	{
			//		result.push_back("false");
			//	}
			//	else
			//	{
			//		//result.push_back("NONE");
			//	}
			//}

			return result;
		}

		private List<int> ModifiesResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> ParentStarResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> ParentResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> FollowsStarResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> FollowsResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> UsesResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> CallResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private List<int> CallStarResult(Field field1, Field field2, List<int> lines, string selectValue) => throw new NotImplementedException();
		private void WithResults(Field field1, Field field2, List<int> lines) => throw new NotImplementedException();
	}
}
