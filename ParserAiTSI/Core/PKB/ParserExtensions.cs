using System.Collections.Generic;
using System.Linq;

namespace Core
{
	internal static class ParserExtensions
	{
		public static IEnumerable<(int LineNumber, string line)> ToNormalized(this IEnumerable<string> lines)
		{
			int count = 1;
			foreach (string j in lines)
			{
				string[] ii = j
					.Replace('\t', '\n')
					.Replace('\r', '\n')
					.Replace(';' , '\n')
					.Replace("}" , "}\n")
					.Replace("{" , "{\n")
					.Split('\n')
					.Select(i => i.Trim())
					.Where(i => !string.IsNullOrWhiteSpace(i))
					.ToArray();

				foreach (var k in ii)
					if (!string.IsNullOrWhiteSpace(k))
					{
						string i = k;

						int start = k.Count(ch => ch == '{');
						int close = k.Count(ch => ch == '}');

						if (start > 0)
							i = i.Replace("{", string.Empty).Trim();
						if (close > 0)
							i = i.Replace("}", string.Empty).Trim();

						if (!string.IsNullOrWhiteSpace(i))
							yield return i.Contains("procedure") ? (-1, i) : (count++, i);
						for (int m = 0; m < start; m++)
							yield return (count, "{");
						for (int m = 0; m < close; m++)
							yield return (count, "}");

					}
			}

		}

		public static IEnumerable<Node> ToArrayForm(this IEnumerable<(int LineNumber, string Line)> ins)
		{
			int nestLevel = 0;
			int id = 0;

			foreach (var (LineNumber, Line) in ins)
			{
				if (Line == "{")
					++nestLevel;
				else if (Line == "}")
					--nestLevel;
				else
					yield return new Node
					{
						Id          = id++,
						LineNumber  = LineNumber,
						Level       = nestLevel,
						Instruction = Line
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

			foreach (var i in list)
				SetParents(i, i.Nodes);

			void SetParents(Node parent, IEnumerable<Node> children)
			{
				foreach (Node i in children)
				{
					i.Parent = parent;
					SetParents(i, i.Nodes);
				}
			}

			return list;
		}
	}
}
