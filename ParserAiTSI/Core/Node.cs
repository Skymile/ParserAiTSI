using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Core
{
	[DebuggerDisplay("{Id}|{Nodes?.Count??0}: {Instruction}")]
	public class Node
	{
		public readonly List<Node> Nodes = new List<Node>();

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
	}

	public enum Instruction : byte
	{
		If,
		Else,
		Assign,
		Expression,
		Loop,
		Call,
		Procedure,
	}
}
