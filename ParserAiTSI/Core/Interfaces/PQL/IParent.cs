using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
    public interface IParent
    {
        void SetParent(INode child, INode parent);

        INode GetParent(INode child);

        IEnumerable<INode> GetChildren(INode parent);

        bool IsParent(INode child, INode parent);

        bool IsChild(INode child, INode parent);
    }
}
