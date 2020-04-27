using System.Collections.Generic;
using System.Linq;

namespace Core
{
	public static class ParserExtensions
	{
		public static IEnumerable<string> ToNormalized(this IEnumerable<string> lines)
		{
			foreach (string j in lines)
				if (!string.IsNullOrWhiteSpace(j))
				{
					string i = j;

					int start = j.Count(ch => ch == '{');
					int close = j.Count(ch => ch == '}');

					if (start > 0)
						i = i.Replace("{", string.Empty).Trim();
					if (close > 0)
						i = i.Replace("}", string.Empty).Trim();

					if (!string.IsNullOrWhiteSpace(i))
						yield return i;
					for (int k = 0; k < start; k++)
						yield return "{";
					for (int k = 0; k < close; k++)
						yield return "}";
				}

		}

		public static IEnumerable<Node> ToArrayForm(this IEnumerable<string> ins)
		{
			int nestLevel = 0;
			int id = 0;

			foreach (string i in ins)
			{
				if (i == "{")
					++nestLevel;
				else if (i == "}")
					--nestLevel;
				else
					yield return new Node
					{
						Id = id++,
						Level = nestLevel,
						Instruction = i
					};
			}
		}

		public static IEnumerable<Node> ToRolledUpForm(this IEnumerable<Node> nodes)
		{
			var list = nodes.ToList();
			int maxLevel = list.Max(i => i.Id);

			for (int level = maxLevel; level >= 0; level--)
			{
				for (int i = 0; i < list.Count - 1;)
					if (list[i].Level == level - 1 && list[i + 1].Level == level)
					{
						list[i].Nodes.Add(list[i + 1]);
						list.RemoveAt(i + 1);
					}
					else ++i;
			}

			return list;
		}
	}
}
