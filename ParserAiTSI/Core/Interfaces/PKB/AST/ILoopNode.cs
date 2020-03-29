namespace Core.Interfaces.PQL
{
    interface ILoopNode
	{
		INode Iterate(IConditionalNode conditions);
		INode Statements { get; }
	}
}