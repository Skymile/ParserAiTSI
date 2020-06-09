namespace Core.PQLo.QueryPreProcessor
{
	public enum StatementType
	{
		Procedure,
		Call     ,
		Variable ,
		Constant ,
		Stmt     ,
		Assign   ,
		Stmtlst  ,
		While    ,
		ProgLine,
		If       ,
	}
}