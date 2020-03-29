namespace Core.Interfaces.PQL
{
	using System;
    
	interface IConditionalNode
	{
		INode Condition(Func<bool> condition);

		INode If { get; }
		INode Else { get; }
	}
}