using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tables
{
    public class FollowsTable : IFollows
    {
        private Dictionary<INode, List<INode>> followsDictionary;

        public FollowsTable()
        {
            followsDictionary = new Dictionary<INode, List<INode>>();
        }

        public IEnumerable<INode> GetFollowed(INode statement)
        {
            if (followsDictionary.ContainsKey(statement))
            {
                List<INode> followsList = new List<INode>();
                followsDictionary.TryGetValue(statement, out followsList);
                return followsList;
            }
            return null;
        }

        public IEnumerable<INode> GetFollows(INode statement)
        {
            List<INode> nodes = new List<INode>();

            foreach(var node in followsDictionary)
            {
                List<INode> statementsValues = new List<INode>();
                followsDictionary.TryGetValue(node.Key, out statementsValues);

                foreach(var val in statementsValues)
                {
                    if (val == statement)
                    {
                        nodes.Add(node.Key);
                    }
                }
            }

            return nodes;
        }

        public bool isFollows(INode statement1, INode statement2)
        {
            if (!followsDictionary.ContainsKey(statement2))
            {
                return false;
            }
            else
            {
                List<INode> followsList = new List<INode>();
                followsDictionary.TryGetValue(statement2, out followsList);

                return followsList.Contains(statement1);
            }
        }

        public void SetFollows(INode statement1, INode statement2)
        {
            if (!followsDictionary.ContainsKey(statement2))
            {
                List<INode> followsList = new List<INode>();
                followsDictionary.Add(statement2, followsList);
            }

            List<INode> followsList2 = new List<INode>();
            followsDictionary.TryGetValue(statement2,out followsList2);
            if (!followsList2.Contains(statement1))
            {
                followsList2.Add(statement1);
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
            SetFollows(n1, n2);
            SetFollows(n3, n2);
            SetFollows(n3, n4);
            var nodes = GetFollows(n3);
            var nodes2 = GetFollowed(n2);

            bool test1 = isFollows(n2, n3);
            bool test2 = isFollows(n1, n3);
            bool test3 = isFollows(n4, n3);
            bool test4 = isFollows(n1, n2);
            bool test5 = isFollows(n3, n4);

        }

    }
}
