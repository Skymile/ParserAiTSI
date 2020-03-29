namespace Core.Interfaces.PQL
{
    interface IValueNode : INode
	{
		object Value { get; }
	}
}