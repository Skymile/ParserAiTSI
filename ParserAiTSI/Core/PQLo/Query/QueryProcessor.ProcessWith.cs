using System.Collections.Generic;
using System.Linq;

namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		private IEnumerable<string> ProcessWith(WithData data, Dictionary<string, StatementType> d)
		{
			switch (data.Type)
			{
				case WithType.Procedure:
					break;
				case WithType.Call:
					break;
				case WithType.Variable:
					break;
				case WithType.Constant:
					break;
				case WithType.Stmt:
					break;
			}
			return Enumerable.Empty<string>();
		}
	}
}
