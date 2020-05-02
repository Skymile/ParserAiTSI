using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tables
{
    public class ParentTable : IParent
    {
        private Dictionary<INode, List<INode>> parentDictionary;

        public ParentTable()
        {
            parentDictionary = new Dictionary<INode, List<INode>>();
        }

        public IEnumerable<INode> GetChildren(INode parent)
        {
            if (parentDictionary.ContainsKey(parent))
            {
                List<INode> childrenList = new List<INode>();
                parentDictionary.TryGetValue(parent, out childrenList);
                return childrenList;
            }
            return null;
        }

        public INode GetParent(INode child)
        {
            foreach (var parent in parentDictionary)
            {
                List<INode> childNodes = new List<INode>();
                parentDictionary.TryGetValue(parent.Key, out childNodes);

                if (childNodes.Contains(child))
                {
                    return parent.Key;
                }
            }

            return null;
        }

        public bool isChild(INode child, INode parent)
        {
            return isParent(child, parent);
        }

        public bool isParent(INode child, INode parent)
        {
            if (!parentDictionary.ContainsKey(parent))
            {
                return false;
            }
            else
            {
                List<INode> childNodes = new List<INode>();
                parentDictionary.TryGetValue(parent, out childNodes);

                return childNodes.Contains(child);
            }
        }

        public void SetParent(INode child, INode parent)
        {
            if (!parentDictionary.ContainsKey(parent))
            {
                List<INode> childList = new List<INode>();
                parentDictionary.Add(parent, childList);
            }

            List<INode> childList2 = new List<INode>();
            parentDictionary.TryGetValue(parent, out childList2);
            if (!childList2.Contains(child))
            {
                childList2.Add(child);
            }
        }

        public void test()
        {
            Node n1 = new Node();
            n1.Id = 1;
            Node n2 = new Node();
            n2.Id = 2;
            Node n3 = new Node();
            n3.Id = 3;
            Node n4 = new Node();
            n4.Id = 4;
            SetParent(n1, n2);
            SetParent(n3, n2);
            SetParent(n3, n4);
            var children = GetChildren(n2);
            var children2 = GetChildren(n4);
            var children3 = GetChildren(n3);

            var parent = GetParent(n3);
            var parent1 = GetParent(n1);
            var parent2 = GetParent(n4);

            bool test1 = isChild(n3, n2);
            bool test2 = isChild(n1, n3);
            bool test3 = isChild(n3, n4);

            bool test4 = isParent(n2, n3);
            bool test5 = isParent(n1, n3);
            bool test6 = isParent(n3, n4);
            //  bool test4 = isFollows(n1, n2);
            //  bool test5 = isFollows(n3, n4);

        }
    }
}
