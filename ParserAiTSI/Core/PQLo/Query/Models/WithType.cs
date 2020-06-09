namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		private enum WithType
		{
			Procedure,
			Call,
			Variable,
			Constant,
			Stmt
		}
	}
}