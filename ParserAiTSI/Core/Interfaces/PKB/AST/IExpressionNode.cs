namespace Core.Interfaces.PQL
{
    interface IExpressionNode : IBinaryNode
	{
		IExpressionNode Reduce(IExpressionNode left, IExpressionNode right);
		IValueNode Compute();
	}
}