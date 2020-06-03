using System;
using System.Collections.Generic;
using System.Linq;
using Common;
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
		public List<string> ResultQuery(ITree<PQLNode> Tree)
        {
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
						var linesa = this.ModifiesResult(item.Field1,
								item.Field2, lines, selectValue);
					}
				}

				if ((item.Type) == "withNode")
				{
					this.WithResults(item.Field1, item.Field2, lines);
				}
			}

			result.Clear();
			return result;
		}

		private List<int> ModifiesResult(Field field1, Field field2, List<Node> lines, string selectValue)
		{
			var setLines1 = new SortedSet<int>();
			var setLines2 = new SortedSet<int>();

			if (field1.Type == "constant" && field2.Type != "constant")
				if (int.Parse(field1.Value) != this.pkbApi.GetNodes(Instruction.Call, false).Select(x => x.Id).Last())
				{
					string name = this.pkb.Procedures.FirstOrDefault(x => x.Id == int.Parse(field1.Value)).Name;
					setLines1.UnionWith(this.pkbApi.GetProcedure(name).Nodes.Select(x => x.Id).ToList());
				}
			return new List<int>()/*resultPart*/;
		}

		private List<int> ParentResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> FollowsResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> UsesResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private List<int> CallResult(Field field1, Field field2, List<Core.Node> lines, string selectValue) => throw new NotImplementedException();
		private void WithResults(Field field1, Field field2, List<Core.Node> lines) => throw new NotImplementedException();
	}
}
