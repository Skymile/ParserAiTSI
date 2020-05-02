using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.PQL
{
    public interface IParent
    {
        void SetParent(INode child, INode parent);

        INode GetParent(INode child);

        IEnumerable<INode> GetChildren(INode parent);

        bool isParent(INode child, INode parent);

        bool isChild(INode child, INode parent);
    }
}
