using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.PQL
{
    public interface IFollows
    {
        void SetFollows(INode statement1, INode statement2);

        IEnumerable<INode> GetFollows(INode statement);

        IEnumerable<INode> GetFollowed(INode statement);

        bool isFollows(INode statement1, INode statement2);
    }
}
