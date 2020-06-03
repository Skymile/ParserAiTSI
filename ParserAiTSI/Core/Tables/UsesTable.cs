using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core.Tables
{
	public class UsesTable : TableBase<INode, IVariableNode>, IUses
	{
		public IEnumerable<INode> GetUsedBy(IVariableNode var) => GetFrom(var);
		public IEnumerable<INode> GetUses(INode statement) => Get(statement);
		public bool IsUses(INode statement, IVariableNode var) => Is(statement, var);
		public void SetUses(INode statement, IVariableNode var) => Set(statement, var);

		public void test()
		{
			var n1 = new Node { Id = 1 };
			var v1 = new VariableNode { Name = "v1" };
			var n3 = new Node { Id = 3 };
			var v2 = new VariableNode { Name = "v2" };

			SetUses(n1, v1);
			SetUses(n3, v1);
			SetUses(n3, v2);

			var nodes = GetUses(n3);
			var nodes2 = GetUsedBy(v1);

			bool test1 = IsUses(n1, v1);
			bool test2 = IsUses(n1, v2);
			bool test3 = IsUses(n3, v1);
			bool test5 = IsUses(n3, v2);
		}
	}
}
