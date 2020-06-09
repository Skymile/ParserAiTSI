using System;
using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
	public interface INode : IComparable<INode>
    {
        int Id         { get; set; }
		int LineNumber { get; set; }
        int Level      { get; set; }

        string      Instruction { get; }
		Instruction Token       { get; }

        Node       Parent { get; }
        List<Node> Nodes  { get; }

        bool TryGetVariable(out string variable);
    }
}