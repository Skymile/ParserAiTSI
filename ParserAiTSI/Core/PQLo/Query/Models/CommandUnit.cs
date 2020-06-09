using System;
using System.Collections.Generic;

namespace Core.PQLo.QueryPreProcessor
{
	public partial class QueryProcessor
	{
		private struct CommandUnit
		{
			public CommandUnit(
					Dictionary<string, StatementType> declarations, 
					SuchData? such, 
					WithData? with
				)
			{
				this.Declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
				this.Such = such;
				this.With = with;
			}

			public readonly Dictionary<string, StatementType> Declarations;
			public readonly SuchData? Such;
			public readonly WithData? With;
		}
	}
}