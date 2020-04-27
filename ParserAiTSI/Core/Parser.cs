using System.Collections.Generic;
using System.IO;

namespace Core
{
	public class Parser
	{
		public static Node LoadRoot(string filename)
		{
			var r = new Node();
			r.Nodes.AddRange(LoadNodes(filename));
			return r;
		}

		public Parser Load(string filename)
		{
			this.Root.Nodes.Clear();
			this.Root.Nodes.AddRange(LoadNodes(filename));
			return this;
		}

		private static IEnumerable<Node> LoadNodes(string filename) =>
			LoadScript(filename)
				.ToNormalized()
				.ToArrayForm()
				.ToRolledUpForm();

		private static IEnumerable<string> LoadScript(string filename) =>
			File.ReadAllText(filename).Replace('\t', '\n').Replace('\r', '\n').Replace(';', '\n').Split('\n');

		public readonly Node Root = new Node();
	}
}
