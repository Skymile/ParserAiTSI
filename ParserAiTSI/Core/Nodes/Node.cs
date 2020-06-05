using System;
using System.Collections.Generic;
using System.Diagnostics;

using Core.Interfaces.PQL;

namespace Core
{
	[DebuggerDisplay("{Level}|{Id}|{Nodes?.Count??0}: {Instruction}")]
	public class Node : INode
	{
		public Node() { }
		public Node(IEnumerable<Node> nodes) =>
			this.Nodes.AddRange(nodes);

		public List<Node> Nodes { get; } = new List<Node>();

		public int Id { get; set; }
		public int Level { get; set; }

		public string Instruction 
		{ 
			get => this.instruction; 
			set 
			{
				this.instruction = value;
				this.Token = FindToken(value);
			}
		}

		private string instruction;

		public Instruction Token { get; private set; }

		private static Instruction FindToken(string instruction)
		{
			string s = instruction.ToUpperInvariant().Trim();

			if (s.Contains("PROCEDURE"))
				return Core.Instruction.Procedure;
			if (s.Contains("IF"))
				return Core.Instruction.If;
			if (s.Contains("ELSE"))
				return Core.Instruction.Else;

			if (s.Contains("+") || s.Contains("-") || s.Contains("*") || s.Contains("/"))
				return Core.Instruction.Expression;
			if (s.Contains("="))
				return Core.Instruction.Assign;

			if (s.Contains("WHILE"))
				return Core.Instruction.Loop;
			if (s.Contains("CALL"))
				return Core.Instruction.Call;

			throw new NotImplementedException($"Unrecognized instruction: \"{instruction}\"");
		}

		public int CompareTo(INode other) 
			=> other == null ? 1 : this.Id.CompareTo(other.Id);
	}

	[Flags]
	public enum Instruction : byte
	{
		None       = 0,
		If         = 1 << 0,
		Else       = 1 << 1,
		Assign     = 1 << 2,
		Expression = 1 << 3,
		Loop       = 1 << 4,
		Call       = 1 << 5,
		Procedure  = 1 << 6,
		All        = (1 << 7) - 1,
	}
}
