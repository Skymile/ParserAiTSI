namespace Core.Interfaces.PQL
{
	interface IBinaryNode : INode
	{
		INode Left { get; }
		INode Right { get; }
	}
}