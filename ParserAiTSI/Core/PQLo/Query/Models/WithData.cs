namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		private struct WithData
		{
			public WithData(string variable, string left, string value, WithType type)
			{
				this.Variable = variable;
				this.Left     = left;
				this.Value    = value;
				this.Type     = type;
			}

			public readonly string Variable;
			public readonly string Left;
			public readonly string Value;
			public readonly WithType Type;
		}
	}
}