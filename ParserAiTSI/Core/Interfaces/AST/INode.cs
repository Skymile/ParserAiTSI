using System;
using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
	public interface INode : IComparable<INode>
    {
        int Id { get; set; }
        int Level { get; set; }
		Instruction Token { get; }
        List<Node> Nodes { get; }
    }
}