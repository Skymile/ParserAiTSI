using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.PQL
{
    public interface INext
    {
        void SetNext(INode statement1, INode statement2);

        IEnumerable<INode> GetNext(INode statement);

        bool isNext(INode statement1, INode statement2);
    }
}
