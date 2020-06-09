using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces.PQL;

namespace Core.PQLo.QueryPreProcessor
{
	public class QueryProcessor
	{
		public QueryProcessor(PKBApi api) => 
			this.Api = api;

		public string ProcessQuery(string query) =>
			SplitQuery(BreakdownQuery(query));

		private string SplitQuery(string[] q) =>
			ProcessQuery(q.Last(), ProcessStatements(q.Take(q.Length - 1)));

		private string[] BreakdownQuery(string query) => query
			.ToUpperInvariant()
			.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
			.Select(i => i.Trim())
			.Where(i => !string.IsNullOrWhiteSpace(i))
			.ToArray();

		private IEnumerable<QueryNode> ProcessStatements(IEnumerable<string> statements)
		{
			foreach (var i in statements)
			{
				string[] split = i.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (split.Length == 2)
					yield return this.statementsList[split[0]](split[1].Trim('\"'));
			}
		}

		private enum CommandType
		{
			None,

			ModifiesPV,
			ModifiesSV,

			UsesPV,
			UsesSV,

			CallsPP,
			FollowsSS,
			ParentSS,

			CallsStarPP,
			FollowsStarSS,
			ParentStarSS,
		}

		private string ProcessQuery(string query, IEnumerable<QueryNode> statements)
		{
			var split = QuerySplit(query);

			var d = statements.ToDictionary(i => i.Value, i => i.Type);

			var command = CommandType.None;
			
			if (split.Count > 0 && split[0] == "SELECT")
			{
				string variable      = split[1];
				string such          = split[2];
				string modifies      = split[3];
				string leftModified  = split[4];
				string rightModified = split[5];

				if (such == "SUCH_THAT")
				{
					var type = d[variable];

					if (modifies == "MODIFIES")
					{
						if (variable == leftModified)
							if (type == StatementType.Procedure)
								command = CommandType.ModifiesPV;
							else if (type == StatementType.Stmt)
								command = CommandType.ModifiesSV;
							else throw new NotImplementedException($"Nierozpoznany {type} w Modifies");
					}
					else if (modifies == "USES")
					{
						if (variable == leftModified)
							if (type == StatementType.Procedure)
								command = CommandType.UsesPV;
							else if (type == StatementType.Stmt)
								command = CommandType.UsesSV;
							else throw new NotImplementedException($"Nierozpoznany {type} w Uses");
					}
					else if (this.SimpleCommands.TryGetValue(modifies, out var value))
						command = value;
					else
						throw new NotImplementedException(modifies);
				}
			}

			return ProcessCommand(command, d);
		}

		private static string ProcessCommand(
				CommandType command, 
				Dictionary<string, StatementType> statements
			)
		{

			return string.Empty;
		}

		//IEnumerable<INode> GetAssign()
		//{
		//	if (modifies == "MODIFIES")
		//	{
		//		var en = this.Api.PKB.Modifies.dict;
		//		var f = en.Keys.FirstOrDefault(
		//			i => i.Name.Equals(rightModified, StringComparison.InvariantCultureIgnoreCase)
		//		);
		//		return en[f];
		//	}
		//	else if (modifies == "USES")
		//	{
		//		var en = this.Api.PKB.Uses.dict;
		//		var f = en.Keys.FirstOrDefault(
		//			i => i.Name.Equals(rightModified, StringComparison.InvariantCultureIgnoreCase)
		//		);
		//		return en[f];
		//	}
		//	return null;
		//}
		//var en = this.Api.PKB.ArrayForm
		//	.Where(i => i.LineNumber.ToString() == leftModified)
		//	.ToNodeEnumerator()
		//	.Where(true, i => i.TryGetVariable(out string var) && var == variable)
		//	.Select(false, i => i.Id)
		//	.ToArray();
		//
		//return string.Join(", ", en);
		//var en = this.Api.PKB.Procedures
		//	.ToNodeEnumerator()
		//	.Where(true, i => i.Instruction == variable)
		//	.Select(false, i => i.Id);
		//return string.Join(", ", en);

		private List<string> QuerySplit(string query)
		{
			int i = query.IndexOfAny(this.characters);

			if (i == -1)
				return new List<string> { query };

			var split = new List<string> { query.Substring(0, i) };

			int j;
			while ((j = query.IndexOfAny(this.characters, i)) != -1)
			{
				string sub = query.Substring(i, j - i);
				if (!string.IsNullOrWhiteSpace(sub))
					split.Add(sub);
				if (query[i] == '\"')
				{	
					i = query.IndexOf('\"', j + 1);
					split.Add(query.Substring(j + 1, i - j - 1));
					i += 1;
				}
				else i = j + 1;
			}

			for (int k = 0; k < split.Count - 1; k++)
				if (split[k] == "SUCH" && split[k + 1] == "THAT")
				{
					split.RemoveAt(k + 1);
					split[k] = "SUCH_THAT";
				}

			return split;
		}

		private readonly Dictionary<string, CommandType> SimpleCommands = new Dictionary<string, CommandType>
		{
			{ "CALLS"  , CommandType.CallsPP   },
			{ "FOLLOWS", CommandType.FollowsSS },
			{ "PARENT" , CommandType.ParentSS  }
		};

		private readonly Dictionary<string, Func<string, QueryNode>> statementsList = new Dictionary<string, Func<string, QueryNode>> { 
			{"ASSIGN"   , t => new QueryNode(StatementType.Assign   , t) }, 
			{"STMTLST"  , t => new QueryNode(StatementType.Stmtlst  , t) },
			{"STMT"     , t => new QueryNode(StatementType.Stmt     , t) },
			{"WHILE"    , t => new QueryNode(StatementType.While    , t) },
			{"VARIABLE" , t => new QueryNode(StatementType.Variable , t) },
			{"CONSTANT" , t => new QueryNode(StatementType.Constant , t) },
			{"PROG_LINE", t => new QueryNode(StatementType.ProgLine , t) },
			{"IF"       , t => new QueryNode(StatementType.If       , t) },
			{"CALL"     , t => new QueryNode(StatementType.Call     , t) },
			{"PROCEDURE", t => new QueryNode(StatementType.Procedure, t) },
		};

		private readonly char[] characters = { ' ', ')', '(', ',', '\"' };
		public PKBApi Api { get; }
	}
}