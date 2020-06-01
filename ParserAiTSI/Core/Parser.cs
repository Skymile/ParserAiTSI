﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
	public class Parser
	{
		public Parser Load(string filename)
		{
			this.Root.Nodes.Clear();
			this.Root.Nodes.AddRange(LoadNodes(filename));
			return this;
		}

		public IEnumerable<string> NormalizedForm { get; private set; }
		public IEnumerable<Node  > ArrayForm      { get; private set; }

		public readonly Node Root = new Node();

		private IEnumerable<Node> LoadNodes(string filename) => 
			(
				this.ArrayForm = 
				(
					this.NormalizedForm = LoadScript(filename)
						.ToNormalized()
						.ToList()
				).ToArrayForm()
				 .ToList()
			).ToRolledUpForm();

		private static IEnumerable<string> LoadScript(string filename) =>
			File.ReadAllText(filename)
				.Replace('\t', '\n')
				.Replace('\r', '\n')
				.Replace(';', '\n')
				.Split('\n');
	}
}
