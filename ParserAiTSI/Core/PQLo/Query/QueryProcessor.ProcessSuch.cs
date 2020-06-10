﻿using System;
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
								var main = this.Api.PKB.ArrayForm
									.Where(i => i.Token == Instruction.Procedure)
									.ToNodeEnumerator()
									.Where(true, Instruction.Assign | Instruction.Expression, i => i.Variable == data.Right)
									.Select(true, i => this.Api.GetProcedure(i.Id) as INode)
									.Distinct()
									.ToArray();

								bool change = false;

								var calls = this.Api.PKB.ArrayForm
									.ToNodeEnumerator()
									.Where(true, Instruction.Call, i => main.Any(j => j.Variable == i.Variable))
									.Select(true, i => this.Api.GetProcedure(i.Id) as INode)
									.ToArray();

								main = main.Concat(calls).Distinct().ToArray();

								int callCount = calls.Length;
								do
								{
									calls = calls.Concat(
										calls
											.ToNodeEnumerator()
											.Where(true, Instruction.Call, i => main.Any(j => j.Variable == i.Variable))
											.Select(true, i => this.Api.GetProcedure(i.Id) as INode)
											.ToArray()
										).Distinct().ToArray();
									
									main = main.Concat(calls).Distinct().ToArray();

									change = callCount != calls.Length;
									callCount = calls.Length;
								} while (change);

								return main.Select(i => i.Variable);
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
						case StatementType.Assign:
							{
								var en = this.Api.PKB.Modifies.dict;
								var f = en.Keys.FirstOrDefault(
									i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
								);
								return en[f].Select(i => i.LineNumber.ToString());
							}
						case StatementType.Stmt:
							{
								var en = this.Api.PKB.Modifies.dict;
								var f = en.Keys.FirstOrDefault(
									i => i.Name.Equals(data.Right, StringComparison.InvariantCultureIgnoreCase)
								);
								var main = en[f];

								for (int i = 0; i < main.Count; i++)
									GatherParents(main[i], main);

								var procedures = main
									.Select(i => this.Api.GetProcedure(i.Id));
								var hash = procedures.Select(i => i.Variable).ToHashSet();

								var calls = this.Api.PKB.ArrayForm
									.Where(i => i.Token == Instruction.Call && hash.Contains(i.Variable))
									.Select(i => i as INode)
									.ToList();

								for (int i = 0; i < calls.Count; i++)
									GatherParents(calls[i], calls);

								return main.Concat(calls)
									.Where(i => i.Token != Instruction.Procedure)
									.Select(i => i.LineNumber.ToString())
									.Distinct();

								void GatherParents(INode node, List<INode> nodes)
								{
									if (node.Parent != null)
									{
										nodes.Add(node.Parent);
										GatherParents(node.Parent, nodes);
									}
								}	
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
					return new List<string> { 
						int.TryParse(data.Right, out int r) ? (r - 1).ToString() : "NONE"
					};
				case CommandType.Parent:
					break;
			}
			return Enumerable.Empty<string>();
		}
	}
}
