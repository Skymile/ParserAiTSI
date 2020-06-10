using System;
using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core
{
	public class NodeEnumerator
	{
		public NodeEnumerator(IEnumerable<INode> chunk) =>
			this.Nodes = chunk;

		public IEnumerable<INode> Nodes { get; private set; }
		public IEnumerable<T> Select<T>(bool applyRecursive, Func<INode, T> func)
		{
			if (applyRecursive)
			{
				var list = new List<T>();
				RecSelect(this.Nodes, list);
				return list;
			}
			return this.Nodes.Select(func);

			void RecSelect(IEnumerable<INode> nodes, List<T> list)
			{
				foreach (var i in nodes)
				{
					list.Add(func(i));
					RecSelect(i.Nodes, list);
				}
			}
		}

		public NodeEnumerator Where(bool applyRecursive, Instruction instruction, Func<INode, bool> filter) =>
			Where(applyRecursive, i => instruction.HasFlag(i.Token) && filter(i));

		public NodeEnumerator Where(bool applyRecursive, Instruction instruction) =>
			Where(applyRecursive, i => instruction.HasFlag(i.Token));

		public NodeEnumerator Where(bool applyRecursive, Func<INode, bool> filter)
		{
			if (applyRecursive)
			{
				var list = new List<INode>();
				RecWhere(this.Nodes, list);
				this.Nodes = list;
			}
			else this.Nodes = this.Nodes.Where(filter);
			return this;

			void RecWhere(IEnumerable<INode> nodes, List<INode> list)
			{
				foreach (var i in nodes)
				{
					if (filter(i))
						list.Add(i);
					RecWhere(i.Nodes, list);
				}
			}
		}
	}
}
