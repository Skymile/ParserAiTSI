using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;

namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		public QueryProcessor(PKBApi api) => 
			this.Api = api;

		public string ProcessQuery(string query) =>
			SplitQuery(BreakdownQuery(query));

		public PKBApi Api { get; }


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

		private string ProcessQuery(string query, IEnumerable<QueryNode> statements)
		{
			var split = QuerySplit(query);

			var d = statements.ToDictionary(i => i.Value, i => i.Type);

			SuchData? suchData = null;
			WithData? withData = null;
			
			if (split.Count > 0 && split[0] == "SELECT")
			{
				string variable      = split[1];
				string such          = split[2];
				string modifies      = split[3];
				string leftModified  = split[4];
				string rightModified = split[5];

				if (such == "SUCH_THAT")
					suchData = new SuchData(variable, leftModified, rightModified, this.commandsDict[modifies]);
				if (such == "WITH")
					withData = new WithData(variable, leftModified, split[6], (WithType)d[modifies.Substring(0, modifies.IndexOf('.'))]);
			}

			return string.Join(
				", ", 
				ProcessCommands(
					(true, new CommandUnit(d, suchData, withData))
				)
			);
		}

		private IEnumerable<string> ProcessCommands(params (bool isAnd, CommandUnit command)[] commands)
		{
			if (commands.Length < 0)
				return Enumerable.Empty<string>();

			IEnumerable<string> result = ProcessCommand(commands[0].command);
			for (int i = 1; i < commands.Length; i++)
			{
				var (isAnd, command) = commands[i];
				var next = ProcessCommand(command);

				result = isAnd ? result.Intersect(next) : result.Concat(next);
			}

			return result;
		}

		private IEnumerable<string> ProcessCommand(CommandUnit command)
		{
			var first = new List<string>();
			var second = new List<string>();

			if (command.Such is SuchData such)
				first.AddRange(ProcessSuch(such, command.Declarations));
			if (command.With is WithData with)
				second.AddRange(ProcessWith(with, command.Declarations));

			return second.Count == 0 
				? first 
				: first.Count == 0
				? second
				: first.Intersect(second);
		}

		private StatementType Find(Dictionary<string, StatementType> d, string variable) =>
			d.TryGetValue(variable, out StatementType value) ? value : StatementType.Constant;

		private IEnumerable<string> ProcessWith(WithData data, Dictionary<string, StatementType> d) => 
			Enumerable.Empty<string>();

		private IEnumerable<string> ProcessSuch(SuchData data, Dictionary<string, StatementType> d)
		{
			switch (data.Type)
			{
				case CommandType.Modifies:
					switch (Find(d, data.Variable))
					{
						case StatementType.Procedure:
							{
								var en = this.Api.PKB.Procedures
									.ToNodeEnumerator()
									.Where(true, i => i.Instruction == data.Variable)
									.Select(false, i => i.LineNumber.ToString());
								return en;
							}
						case StatementType.Call:
						case StatementType.Variable:
							{
								var en = this.Api.PKB.ArrayForm
									.Where(i => i.LineNumber.ToString() == data.Left)
									.ToArray().ToNodeEnumerator()
									.Where(true, i => i.Variable != null)
									.Select(false, i => i.Variable.ToLowerInvariant())
									.Distinct();
								return en;
							}
						case StatementType.Stmt:
						case StatementType.Assign:
							{
								var en = this.Api.PKB.Modifies.dict;
								var f = en.Keys.FirstOrDefault(
									i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
								);
								return en[f].Select(i => i.LineNumber.ToString());
							}
						case StatementType.Stmtlst:
						case StatementType.While:
						case StatementType.ProgLine:
						case StatementType.If:
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
				case CommandType.Follows:
				case CommandType.Parent:
					break;
			}
			return Enumerable.Empty<string>();
		}

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

		private readonly Dictionary<string, CommandType> commandsDict = new Dictionary<string, CommandType>
		{
			{ "MODIFIES", CommandType.Modifies },
			{ "CALLS"   , CommandType.Calls    },
			{ "USES"    , CommandType.Uses     },
			{ "FOLLOWS" , CommandType.Follows  },
			{ "PARENT"  , CommandType.Parent   },
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
	}
}
