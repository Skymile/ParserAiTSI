﻿namespace Core.PQLo.QueryPreProcessor
{
	public class QueryNode
	{
		public QueryNode(StatementType type, string value)
		{
			this.Type = type;
			this.Value = value;
		}

		public StatementType Type { get; set; }
		public string Value { get; set; }
	}
}