using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
	public class NodeCollection : IEnumerable<Node>
	{
		public NodeCollection(IEnumerable<Node> nodes) => 
			this.nodes = new List<Node>(nodes);

		public int Count => this.nodes.Count;

		public Node this[int index]
		{
			get => this.nodes.FirstOrDefault(i => i.Id == index);
			set 
			{
				for (int i = 0; i < this.nodes.Count; i++)
					if (this.nodes[i].Id == index)
					{
						this.nodes[i] = value;
						break;
					}
			}
		}

		public IEnumerator<Node> GetEnumerator() => this.nodes.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.nodes.GetEnumerator();

		private readonly List<Node> nodes;
	}
}
