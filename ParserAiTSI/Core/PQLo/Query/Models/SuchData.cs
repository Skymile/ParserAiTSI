namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		private struct SuchData
		{
			public SuchData(string variable, string left, string right, CommandType type)
			{
				this.Variable = variable;
				this.Left     = left;
				this.Right    = right;
				this.Type     = type;
			}

			public readonly string Variable;
			public readonly string Left;
			public readonly string Right;
			public readonly CommandType Type;
		}
	}
}