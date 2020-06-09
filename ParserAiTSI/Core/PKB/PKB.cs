using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.AST;
using Core.Interfaces.PQL;
using Core.Tables;

namespace Core
{
	public class PKB
	{
		public PKB(Parser parser)
		{
			this.Root = parser.Root;
			this.ArrayForm = new NodeCollection(parser.ArrayForm);
			Compute();
		}

		public readonly FollowsTable  Follows  = new FollowsTable ();
		public readonly ParentTable   Parent   = new ParentTable  ();
		public readonly CallsTable    Calls    = new CallsTable   ();
		public readonly ModifiesTable Modifies = new ModifiesTable();
		public readonly NextTable     Next     = new NextTable    ();
		public readonly UsesTable     Uses     = new UsesTable    ();
		
		public readonly List<IVariableNode > Variables  = new List<IVariableNode >();
		public readonly List<IProcedureNode> Procedures = new List<IProcedureNode>();

		public readonly NodeCollection ArrayForm;
		public readonly Node Root;

		private void Compute()
		{
			var arrayForm = this.ArrayForm;
			var root = this.Root;
			
			SetVariables (arrayForm);
			SetProcedures(root);

			SetFollows (arrayForm);
			SetParents (root);
			SetCalls   (arrayForm);
			SetModifies(arrayForm);
			SetNext    (root);
			SetUses    (arrayForm);
		}

		private void SetVariables(NodeCollection arrayForm) =>
			this.Variables.AddRange(
				from i in arrayForm
				where i.Token == Instruction.Assign || i.Token == Instruction.Expression
				let name = i.Instruction.Substring(0, i.Instruction.IndexOf('=')).Trim()
				group i by name into k
				let first = k.First()
				select new VariableNode(first.Nodes)
				{
					Id    = first.Id,
					Level = first.Level,
					Name  = k.Key
				}
			);

		private void SetProcedures(Node root) =>
			this.Procedures.AddRange(
				from i in root.Nodes
				where i.Token == Instruction.Procedure
				select new ProcedureNode(i.Nodes)
				{
					Id          = i.Id,
					Instruction = i.Instruction,
					Level       = i.Level,
					Name        = i.Instruction.Substring("procedure ".Length),
				}
			);

		private void SetFollows(NodeCollection arrayForm)
		{
			for (int i = 0; i < arrayForm.Count - 1; i++)
				if (arrayForm[i].Level == arrayForm[i + 1].Level)
					this.Follows.SetFollows(arrayForm[i + 1], arrayForm[i]);
					// i+1 node follows i node
		}

		private void SetParents(Node node)
		{
			foreach (var i in node.Nodes)
			{
				this.Parent.SetParent(i, node);
				SetParents(i);
			}
		}

		private void SetCalls(NodeCollection arrayForm)
		{
			int p = -1;
			foreach (var i in arrayForm)
				if (i.Level == 0)
					++p;
				else
				{
					if (i.Token == Instruction.Call)
					{
						string name = i.Instruction.Substring("CALL ".Length);

						this.Calls.SetCall(
							this.Procedures.Single(j => j.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)),
							this.Procedures[p]
						);
						// proc1 calls proc2
					}
				}
		}

		private void SetModifies(NodeCollection arrayForm)
		{
			foreach (var i in arrayForm)
				if (i.Token == Instruction.Expression || i.Token == Instruction.Assign)
				{
					string varName = i.Instruction.Substring(0, i.Instruction.IndexOf('=')).Trim();

					this.Modifies.SetModifies(
						i,
						this.Variables.First(j => j.Name == varName)
					);
				}
		}

		private void SetNext(Node parent)
		{
			var nodes = parent.Nodes;

			for (int i = 0; i < nodes.Count; i++)
			{
				Node f = nodes[i];
				if (f.Token == Instruction.If && f.Nodes.Count > 0)
					this.Next.SetNext(f, f.Nodes[0]);
				if (i + 1 < nodes.Count && nodes[i + 1].Token == Instruction.Else)
					this.Next.SetNext(f, nodes[i + 1]);

				if (nodes[i].Nodes.Count != 0)
					SetNext(nodes[i]);
				else if (i + 1 < nodes.Count)
					this.Next.SetNext(f, nodes[i + 1]);
			}

			if (parent.Token == Instruction.Loop)
				this.Next.SetNext(parent, nodes[nodes.Count - 1]);
		}

		private void SetUses(NodeCollection arrayForm)
		{
			foreach (var i in arrayForm)
			{
				string[] vars = null;
				if (i.Token == Instruction.Assign || i.Token == Instruction.Expression)
					vars = i.Instruction.Substring(i.Instruction.IndexOf('=') + 1).Trim().Split(' ');
				else
				{
					int ind = i.Instruction.IndexOf(' ');
	
					if (ind != -1)
						vars = i.Instruction.Substring(ind).Trim().Split(' ');
				}

				if (!(vars is null) && 
					this.Variables.FirstOrDefault(j => vars.Any(k => k == j.Name)) is var used && 
					!(used is null))
					this.Uses.SetUses(i, used);
			}
		}
	}
}
