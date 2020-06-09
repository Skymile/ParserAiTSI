namespace Core.PQLo.QueryPreProcessor
{
	public enum CommandType
	{
		None,

		Modifies,
		Uses,

		Calls,
		Follows,
		Parent,

		CallsStar,
		FollowsStar,
		ParentStar,
	}
}