namespace Core.Interfaces.PQL
{
	using System;
    using System.Collections.Generic;

    interface ITree
	{
		INode CreateNode<T>() where T : INode;

		ITree SetRoot(INode node);
		ITree Modify(INode node, Action<INode> action);
		ITree Link(INode parent, INode child);

		INode GetParent(INode node);
		void SetParent(INode node);

		IEnumerable<INode> GetChildren(INode node, bool recursive = false);
		void SetChildren(INode node, IEnumerable<INode> children);
	}
}