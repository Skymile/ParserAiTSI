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

		public Node Parent { get; set; }
		public List<Node> Nodes { get; } = new List<Node>();

		public int LineNumber { get; set; }
		public int Id { get; set; }
		public int Level { get; set; }

		public string Instruction 
		{ 
			get => this.instruction; 
			set 
			{
				this.instruction = value.ToUpperInvariant().Trim();
				this.Token = FindToken(this.instruction);
			}
		}

		private string instruction;

		public Instruction Token { get; private set; }

		private static Instruction FindToken(string ins)
		{
			if (ins.StartsWith("PROCEDURE "))
				return Core.Instruction.Procedure;
			if (ins.StartsWith("IF "))
				return Core.Instruction.If;
			if (ins.StartsWith("ELSE"))
				return Core.Instruction.Else;

			if (ins.Contains("+") || ins.Contains("-") || ins.Contains("*") || ins.Contains("/"))
				return Core.Instruction.Expression;
			if (ins.Contains("="))
				return Core.Instruction.Assign;

			if (ins.StartsWith("WHILE "))
				return Core.Instruction.Loop;
			if (ins.StartsWith("CALL "))
				return Core.Instruction.Call;

			throw new NotImplementedException($"Unrecognized instruction: \"{ins}\"");
		}

		public bool TryGetVariable(out string variable)
		{
			int i = instruction.IndexOf(' ');
			string var = instruction.Substring(i + 1);
			switch (this.Token)
			{
				case Core.Instruction.If:
					break;
				case Core.Instruction.Else:
					break;
				case Core.Instruction.Assign:
					break;
				case Core.Instruction.Expression:
					break;
				case Core.Instruction.Loop:
					break;
				case Core.Instruction.Call:
					break;
				case Core.Instruction.Procedure:
					break;
				case Core.Instruction.All:
					break;
				default:
					variable = null;
					return false;
			}
			variable = var;
			return true;
		}


		public int CompareTo(INode other) => 
			other == null ? 1 : this.Id.CompareTo(other.Id);
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
