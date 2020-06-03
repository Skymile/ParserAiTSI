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

        public void test()
        {
            var n1 = new Node { Id = 1 };
            var n2 = new Node { Id = 2 };
            var n3 = new Node { Id = 3 };
            var n4 = new Node { Id = 4 };
            
            SetParent(n1, n2);
            SetParent(n3, n2);
            SetParent(n3, n4);
            
            var children = GetChildren(n2);
            var children2 = GetChildren(n4);
            var children3 = GetChildren(n3);

            var parent = GetParent(n3);
            var parent1 = GetParent(n1);
            var parent2 = GetParent(n4);

            bool test1 = IsChild(n3, n2);
            bool test2 = IsChild(n1, n3);
            bool test3 = IsChild(n3, n4);

            bool test4 = IsParent(n2, n3);
            bool test5 = IsParent(n1, n3);
            bool test6 = IsParent(n3, n4);
            //  bool test4 = isFollows(n1, n2);
            //  bool test5 = isFollows(n3, n4);
        }
    }
}
