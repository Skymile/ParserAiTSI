using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
						var variables = pkb.Variables;
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
					if(this.resultType == "variable")
					{
						List<int> variableIds = new List<int>();
						if (isModifies) // Czy występuje tylko relacja -> Modifies
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
		private List<int> UsesResult(Field field1, Field field2, List<Node> lines, string selectValue) => throw new NotImplementedException();
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
				var a = this.pkbApi.GetNodes(Instruction.Call, false).Select(x => x.Id);
				var b = a.Last();
				var param1 = int.Parse(field1.Value);
				if (int.Parse(field1.Value) != b)
				{
					setLines1.Add(param1);
					//var node = (this.pkbApi.GetNodes(Instruction.Call, false).FirstOrDefault(x => x.Id == int.Parse(field1.Value)) as IProcedureNode);
					//setLines1.UnionWith(this.pkbApi.GetProcedure(node.Name).Nodes.Select(x => x.Id).ToList());
				}

				if (field2.Type == "variable" || field2.Type == "any")
				{

					setLines2.UnionWith(pkb.Variables);
				}
			}

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
								if (resultPart.FirstOrDefault(x=> x == l1) >= resultPart.Last())
								{
									resultPart.Add(l1);
								}
							}
							else if (selectValue == field2.Value && selectValue != "boolean")
							{
								//Jeśli parametr drugi jest tym, którego szukamy to wybieramy z listy drugiej
								if (resultPart.FirstOrDefault(x => x == l2.Id) >= (resultPart.Count == 0 ? 0 : resultPart.Last()))
								{
									resultPart.Add(l1);
								}
							}
							else
							{
								Console.WriteLine("wESZŁO");
								//Jeśli żaden parametr nie jest szukany to zwracane są wszystkie wartości
								return lines.Select(x => x.Id).ToList();
							}
						}
					}
				}
			}

			return resultPart;
		}

		private void WithResults(Field field1, Field field2, List<Core.Node> lines) => throw new NotImplementedException();
	}
}
