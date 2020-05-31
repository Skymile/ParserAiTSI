using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tables
{
    public class NextTable : INext
    {
        private Dictionary<INode, List<INode>> nextDictionary;

        public NextTable()
        {
            nextDictionary = new Dictionary<INode, List<INode>>();
        }

        public IEnumerable<INode> GetNext(INode statement)
        {
            if (nextDictionary.ContainsKey(statement))
            {
                List<INode> nextList = new List<INode>();
                nextDictionary.TryGetValue(statement, out nextList);
                return nextList;
            }
            return null;
        }

        public bool isNext(INode statement1, INode statement2)
        {
            if (!nextDictionary.ContainsKey(statement1))
            {
                return false;
            }
            else
            {
                List<INode> nextList = new List<INode>();
                nextDictionary.TryGetValue(statement1, out nextList);

                return nextList.Contains(statement2);
            }
        }

        public void SetNext(INode statement1, INode statement2)
        {
            if (!nextDictionary.ContainsKey(statement1))
            {
                List<INode> nextList = new List<INode>();
                nextDictionary.Add(statement1, nextList);
            }

            List<INode> nextList2 = new List<INode>();
            nextDictionary.TryGetValue(statement1, out nextList2);
            if (!nextList2.Contains(statement2))
            {
                nextList2.Add(statement2);
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
            SetNext(n1, n2);
            SetNext(n3, n2);
            SetNext(n3, n4);
            var nodes = GetNext(n3);
            var nodes2 = GetNext(n2);

            bool test1 = isNext(n2, n3);
            bool test2 = isNext(n1, n3);
            bool test3 = isNext(n4, n3);
            bool test4 = isNext(n1, n2);
            bool test5 = isNext(n3, n4);
        }
    }
}
