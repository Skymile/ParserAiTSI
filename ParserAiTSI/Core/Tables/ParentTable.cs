using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class ParentTable : TableBase<INode, INode>, IParent
    {
        public IEnumerable<INode> GetChildren(INode parent) => GetFrom(parent);

        public INode GetParent(INode child) =>
            this.dict.FirstOrDefault(i => i.Value.Contains(child)).Key;

        public bool IsChild(INode child, INode parent) =>
            IsParent(child, parent);

        public bool IsParent(INode child, INode parent) => Is(child, parent);
        public void SetParent(INode child, INode parent) => Set(child, parent);
    }
}
