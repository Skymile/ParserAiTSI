#if false
using System;
using System.Collections.Generic;

namespace Core
{
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

	interface INode { }
	interface IStatementListNode : INode
	{
		IEnumerable<INode> Statements { get; }
	}
	interface IBinaryNode : INode
	{
		INode Left { get; }
		INode Right { get; }
	}
	interface IVariableNode : INode
	{
		string Name { get; }
	}
	interface IValueNode : INode
	{
		object Value { get; }
	}
	interface IAssignNode : INode
	{
		IVariableNode Left { get; }
		IValueNode Right { get; }
	}
	interface IExpressionNode : IBinaryNode
	{
		IExpressionNode Reduce(IExpressionNode left, IExpressionNode right);
		IValueNode Compute();
	}
	interface IConditionalNode
	{
		INode Condition(Func<bool> condition);

		INode If { get; }
		INode Else { get; }
	}
	interface ILoopNode
	{
		INode Iterate(IConditionalNode conditions);
		INode Statements { get; }
	}

	//		INode Root { get; }
	//
	//		IEnumerable<INode> GetChildren(bool recursive = false);
	//
	//		INode CreateNode<T>() 
	//			where T : INode;
	//
	//		INode GetNthNode(INode node, int n);
	//		INode SetNthNode(INode node, int n);
	//	}
}

#endif