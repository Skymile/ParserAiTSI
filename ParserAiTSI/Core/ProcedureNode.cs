using System.Collections.Generic;

using Core.Interfaces.AST;

namespace Core
{
    public class ProcedureNode : Node, IProcedureNode
    {
        public ProcedureNode() { }
        public ProcedureNode(IEnumerable<Node> nodes) : base(nodes) { }

        public string Name { get; set; }
    }
}
