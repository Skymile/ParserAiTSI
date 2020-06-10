using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core
{
    [DebuggerDisplay("{Level}|{Id}|{Nodes?.Count??0}: {Instruction}")]
    public class Node : INode
    {
        public Node() { }
        public Node(IEnumerable<Node> nodes)  => 
            this.Nodes.AddRange(nodes);

        public Node Parent { get; set; }

        public Node Twin =>
            Token == Core.Instruction.Else ? this.Parent.Nodes.ElementAt(this.Parent.Nodes.IndexOf(this) - 1) : null;
//            Token == Core.Instruction.If   ? this.Parent.Nodes.ElementAt(this.Parent.Nodes.IndexOf(this) + 1) : null;

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


        public string Assignment
        {
            get
            {
                if (!this.init)
                    InitVars();
                return this.assignment;
            }
            set => this.assignment = value;
        }
        private string assignment;

        public string Variable
        {
            get
            {
                if (!this.init)
                    InitVars();
                return this.variable;
            }
            set => this.variable = value;
        }
        private string variable;

        public List<string> Variables
        {
            get
            {
                if (!this.init)
                    InitVars();
                return this.variables;
            }
            set => this.variables = value;
        }
        private List<string> variables;


        private bool init;

        private void InitVars()
        {
            this.init = true;
            if (this.instruction == null)
                return;

            int i = this.instruction.IndexOf(' ');
            string var = this.instruction.Substring(i + 1);
            this.Variable = var;

            switch (this.Token)
            {
                case Core.Instruction.If:
                    this.Variable = var.Substring(0, var.Length - " THEN".Length);
                    break;
                case Core.Instruction.Else:
                    this.Variable = null;
                    break;
                case Core.Instruction.Assign:
                case Core.Instruction.Expression:
                    this.Variable = this.instruction.Substring(0, this.instruction.IndexOf('=')).Trim();
                    this.Variables = var
                        .Replace("*", " ")
                        .Replace("=", " ")
                        .Replace("-", " ")
                        .Replace("+", " ")
                        .Replace("/", " ")
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct()
                        .ToList();
                    break;
                case Core.Instruction.Loop:
                    this.Variable = var;
                    break;
                case Core.Instruction.Call:
                    break;
                case Core.Instruction.Procedure:
                    break;
                case Core.Instruction.All:
                    break;
            }
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
