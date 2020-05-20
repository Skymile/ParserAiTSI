using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tables
{
    public class ModifiesTable : IModifies
    {
        private Dictionary<IVariableNode, List<INode>> modifiesDictionary;

        public ModifiesTable()
        {
            modifiesDictionary = new Dictionary<IVariableNode, List<INode>>();
        }

        public IEnumerable<INode> GetModified(IVariableNode var)
        {
            if (modifiesDictionary.ContainsKey(var))
            {
                List<INode> modifiesList = new List<INode>();
                modifiesDictionary.TryGetValue(var, out modifiesList);
                return modifiesList;
            }
            return null;
        }

        public IEnumerable<INode> GetModifies(INode statement)
        {
            List<INode> nodes = new List<INode>();

            foreach (var node in modifiesDictionary)
            {
                List<INode> statementsValues = new List<INode>();
                modifiesDictionary.TryGetValue(node.Key, out statementsValues);

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

        public bool isModifies(INode statement, IVariableNode var)
        {
            if (!modifiesDictionary.ContainsKey(var))
            {
                return false;
            }
            else
            {
                List<INode> modifiesList = new List<INode>();
                modifiesDictionary.TryGetValue(var, out modifiesList);

                return modifiesList.Contains(statement);
            }
        }

        public void SetModifies(INode statement, IVariableNode var)
        {
            if (!modifiesDictionary.ContainsKey(var))
            {
                List<INode> modifiesList = new List<INode>();
                modifiesDictionary.Add(var, modifiesList);
            }

            List<INode> modifiesList2 = new List<INode>();
            modifiesDictionary.TryGetValue(var, out modifiesList2);
            if (!modifiesList2.Contains(statement))
            {
                modifiesList2.Add(statement);
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
            SetModifies(n1, v1);
            SetModifies(n3, v1);
            SetModifies(n3, v2);
            var nodes = GetModifies(n3);
            var nodes2 = GetModified(v1);

            bool test1 = isModifies(n1, v1);
            bool test2 = isModifies(n1, v2);
            bool test3 = isModifies(n3, v1);
            bool test5 = isModifies(n3, v2);

        }
    }
}
