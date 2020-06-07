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
			return applyRecursive ? RecSelect(this.Nodes) : this.Nodes.Select(func);

			IEnumerable<T> RecSelect(IEnumerable<INode> nodes)
			{
				foreach (var i in nodes)
				{
					yield return func(i);
					RecSelect(i.Nodes);
				}
			}
		}

		public NodeEnumerator Where(bool applyRecursive, Instruction instruction, Func<INode, bool> filter) =>
			Where(applyRecursive, i => instruction.HasFlag(i.Token) && filter(i));

		public NodeEnumerator Where(bool applyRecursive, Instruction instruction) =>
			Where(applyRecursive, i => instruction.HasFlag(i.Token));

		public NodeEnumerator Where(bool applyRecursive, Func<INode, bool> filter)
		{
			this.Nodes = applyRecursive ? RecWhere(this.Nodes) : this.Nodes.Where(filter);
			return this;

			IEnumerable<INode> RecWhere(IEnumerable<INode> nodes)
			{
				foreach (var i in nodes)
					if (filter(i))
					{
						yield return i;
						RecWhere(i.Nodes);
					}
			}
		}
	}
}
