using System.Collections.Generic;
using System.IO;

namespace Core
{
	public class Parser
	{
		public Parser LoadFile(string filename) =>
			File.Exists(filename)
				? LoadScript(File.ReadAllText(filename))
				: throw new FileNotFoundException($"\"{filename}\" was not found.");

		public Parser LoadScript(string text)
		{


			return this;
		}

		public Parser Add(Node node)
		{
			Node next = this.Root;

			while (next.First != null)
				next = next.First;
			next.First = node;

			return this;
		}

		public readonly Node Root = new Node(Instruction.Statements);
	}

	public class Node
	{
		public Node(Instruction instruction, params Node[] list)
		{
			this.InsType = instruction;
			if (list.Length > 0)
				this.List = new List<Node>(list);
		}

		public Node First  { get; set; }
		public Node Second { get; set; }

		public object Value { get; set; }
		public string Token { get; set; }


		public readonly IEnumerable<Node> List;
		public readonly Instruction InsType;
	}

	public enum Instruction : byte
	{
		NoOp,
		Conditional,
		Assign,
		Declare,
		Expression,
		Loop,
		Statements,
		Call,
		Constant,
	}
}
