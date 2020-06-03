using Core.Interfaces.PQL;

namespace Core.Interfaces.AST
{
    public interface IProcedureNode : INode
    {
        string Name { get; set; }
    }
}
