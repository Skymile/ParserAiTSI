namespace Core.Interfaces.PQL
{
    interface IAssignNode : INode
	{
		IVariableNode Left { get; }
		IValueNode Right { get; }
	}
}