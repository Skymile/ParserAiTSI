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
	}
}
