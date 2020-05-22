using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tables
{
    public class UsesTable : IUses
    {
        private Dictionary<IVariableNode, List<INode>> usesDictionary;

        public UsesTable()
        {
            usesDictionary = new Dictionary<IVariableNode, List<INode>>();
        }

        public IEnumerable<INode> GetUsedBy(IVariableNode var)
        {
            if (usesDictionary.ContainsKey(var))
            {
                List<INode> usesList = new List<INode>();
                usesDictionary.TryGetValue(var, out usesList);
                return usesList;
            }
            return null;
        }

        public IEnumerable<INode> GetUses(INode statement)
        {
            List<INode> nodes = new List<INode>();

            foreach (var node in usesDictionary)
            {
                List<INode> statementsValues = new List<INode>();
                usesDictionary.TryGetValue(node.Key, out statementsValues);

                foreach (var val in statementsValues)
                {
                    if (val == statement)
                    {
                        nodes.Add(node.Key);
                    }
                }
            }

            return nodes;
        }

        public bool isUses(INode statement, IVariableNode var)
        {
            if (!usesDictionary.ContainsKey(var))
            {
                return false;
            }
            else
            {
                List<INode> usesList = new List<INode>();
                usesDictionary.TryGetValue(var, out usesList);

                return usesList.Contains(statement);
            }
        }

        public void SetUses(INode statement, IVariableNode var)
        {
            if (!usesDictionary.ContainsKey(var))
            {
                List<INode> usesList = new List<INode>();
                usesDictionary.Add(var, usesList);
            }

            List<INode> usesList2 = new List<INode>();
            usesDictionary.TryGetValue(var, out usesList2);
            if (!usesList2.Contains(statement))
            {
                usesList2.Add(statement);
            }
        }

        public void test()
        {
            Node n1 = new Node();
            n1.Id = 1;
            VariableNode v1 = new VariableNode();
            v1.Name = "v1";
            Node n3 = new Node();
            n3.Id = 3;
            VariableNode v2 = new VariableNode();
            v2.Name = "v2";
            SetUses(n1, v1);
            SetUses(n3, v1);
            SetUses(n3, v2);
            var nodes = GetUses(n3);
            var nodes2 = GetUsedBy(v1);

            bool test1 = isUses(n1, v1);
            bool test2 = isUses(n1, v2);
            bool test3 = isUses(n3, v1);
            bool test5 = isUses(n3, v2);

        }
    }
}
