using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core.Tables
{
	public class FollowsTable : TableBase<INode, INode>, IFollows
	{
		public IEnumerable<INode> GetFollowed(INode statement) => Get(statement);
		public IEnumerable<INode> GetFollows(INode statement) => Get(statement);
		public bool IsFollows(INode statement1, INode statement2) => Is(statement1, statement2);
		public void SetFollows(INode statement1, INode statement2) => Set(statement1, statement2);
	}
}
