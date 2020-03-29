namespace Core.Interfaces.PQL
{
	using System.Collections.Generic;
    
	interface IStatementListNode : INode
	{
		IEnumerable<INode> Statements { get; }
	}
}