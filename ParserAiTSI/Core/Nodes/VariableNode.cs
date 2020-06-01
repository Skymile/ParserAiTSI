using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core
{
    public class VariableNode : Node, IVariableNode
    {
        public VariableNode() { }
        public VariableNode(IEnumerable<Node> nodes) : base(nodes) { }

        public string Name  { get; set; }
    }
}
