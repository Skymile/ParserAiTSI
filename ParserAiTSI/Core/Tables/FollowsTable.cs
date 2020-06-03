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

		public void Test()
		{
			var n1 = new Node { Id = 1 };
			var n2 = new Node { Id = 2 };
			var n3 = new Node { Id = 3 };
			var n4 = new Node { Id = 4 };

			SetFollows(n1, n2);
			SetFollows(n3, n2);
			SetFollows(n3, n4);

			var nodes = GetFollows(n3);
			var nodes2 = GetFollowed(n2);

			bool test1 = IsFollows(n2, n3);
			bool test2 = IsFollows(n1, n3);
			bool test3 = IsFollows(n4, n3);
			bool test4 = IsFollows(n1, n2);
			bool test5 = IsFollows(n3, n4);
		}
	}
}
