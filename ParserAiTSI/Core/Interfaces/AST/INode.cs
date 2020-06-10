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
        Node       Twin   { get; }
        List<Node> Nodes  { get; }

        string Assignment { get; set; }
        string Variable { get; set; }
        List<string> Variables { get; set; }
    }
}