using System;
using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
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
								var f = this.Api.PKB.ArrayForm
									.Where(i => i.LineNumber.ToString() == data.Left)
									.ToArray();

								if (f.Length == 0)
									f = this.Api.PKB.Procedures
										.Where(i => i.Name == data.Left)
										.Select(i => i as Node)
										.ToArray();

								var calls = f
									.ToNodeEnumerator()
									.Where(true, Instruction.Call)
									.Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
									.ToArray();

								bool change = false;
								int callCount = calls.Count();
								do
								{
									calls = calls.Concat(
											calls
												.ToNodeEnumerator()
												.Where(true, Instruction.Call)
												.Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
										).Distinct().ToArray();
									change = callCount != calls.Length;
									callCount = calls.Length;
								} while (change);

								var en = f
									.ToNodeEnumerator()
									.Where(true, Instruction.Assign | Instruction.Expression, i => i.Variable != null)
									.Select(false, i => i.Variable.ToUpperInvariant())
									.Distinct()
									.ToArray();

								var enC = calls
									.ToNodeEnumerator()
									.Where(true, Instruction.Assign | Instruction.Expression, i => i.Variable != null)
									.Select(false, i => i.Variable.ToUpperInvariant())
									.Distinct()
									.ToArray();

								return en.Concat(enC);
							}
						case StatementType.Stmt:
							{
								var f = this.Api.PKB.ArrayForm
									.Where(i => i.Variable == data.Right)
									.ToArray();

								if (f.Length == 0)
									f = this.Api.PKB.Procedures
										.Where(i => i.Name == data.Right)
										.Select(i => i as Node)
										.ToArray();

								var calls = f
									.ToNodeEnumerator()
									.Where(true, Instruction.Call)
									.Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
									.ToArray();

								bool change = false;
								int callCount = calls.Count();
								do
								{
									calls = calls.Concat(
											calls
												.ToNodeEnumerator()
												.Where(true, Instruction.Call)
												.Select(true, i => this.Api.PKB.Procedures.Single(j => j.Name == i.Variable) as INode)
										).Distinct().ToArray();
									change = callCount != calls.Length;
									callCount = calls.Length;
								} while (change);

								var en = f
									.ToNodeEnumerator()
									.Where(true, i => i.Variable != null)
									.Select(false, i => i.LineNumber)
									.Distinct()
									.ToArray();

								var enC = calls
									.ToNodeEnumerator()
									.Where(true, i => i.Variable != null)
									.Select(false, i => i.LineNumber)
									.Distinct()
									.ToArray();

								return en.Concat(enC).Select(i => i.ToString());
							}
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
	}
}
