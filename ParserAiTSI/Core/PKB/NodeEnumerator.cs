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

		public IEnumerable<INode> Gather(Mode recursion, Instruction instruction, Func<INode, INode> func)
		{
			int c;
			do
			{
				c = this.Nodes.Count();
				this.Nodes = this.Nodes.Concat(Where(recursion, instruction).Select(recursion, func)).Distinct();
			} while (this.Nodes.Count() != c);
			return this.Nodes;
		}

		public IEnumerable<T> Select<T>(Mode recursion, Instruction instruction, Func<INode, T> func) => 
			Where(recursion, instruction).Select(recursion, func);

		public IEnumerable<T> Select<T>(Mode recursion, Func<INode, T> func)
		{
			if (recursion == Mode.GreedyRecursion)
			{
				var list = new List<T>();
				int i;
				do
				{
					i = list.Count;
					RecSelect(this.Nodes, list);
					list = list.Distinct().ToList();
				} while (i != list.Count);
				return list;
			}
			else if (recursion == Mode.StandardRecursion)
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

		public NodeEnumerator Where(Mode recursion, Instruction instruction, Func<INode, bool> filter) =>
			Where(recursion, i => instruction.HasFlag(i.Token) && filter(i));

		public NodeEnumerator Where(Mode recursion, Instruction instruction) =>
			Where(recursion, i => instruction.HasFlag(i.Token));

		public NodeEnumerator Where(Mode recursion, Func<INode, bool> filter)
		{
			if (recursion == Mode.GreedyRecursion)
			{
				var list = new List<INode>();
				int i;
				do
				{
					i = list.Count;
					RecWhere(this.Nodes, list);
					list = list.Distinct().ToList();
				} while (i != list.Count);
				this.Nodes = list;
			}
			else if (recursion == Mode.StandardRecursion)
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
