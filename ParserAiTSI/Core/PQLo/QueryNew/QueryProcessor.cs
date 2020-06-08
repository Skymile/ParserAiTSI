using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.PQLo.QueryPreProcessor
{
	public class QueryProcessor
	{
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
					yield return this.statementsList[split[0]](split[1]);
			}
		}

		private string ProcessQuery(string query, IEnumerable<QueryNode> statements)
		{
			var split = QuerySplit(query);

			if (split.Count > 0 && split[0] == "SELECT")
			{
				string variable = split[1];

				;
			}
			return string.Empty;
		}

		private List<string> QuerySplit(string query)
		{
			int i = query.IndexOfAny(this.characters);

			if (i == -1)
				return new List<string> { query };

			List<string> split = new List<string> { query.Substring(0, i) };

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

		private readonly string[] joinTypes      = { "OR", "AND" };
		private readonly string[] queryTypes     = { "SELECT", "SUCH THAT", "WITH" };
	}
}