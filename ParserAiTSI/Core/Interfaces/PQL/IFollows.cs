using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
    public interface IFollows
    {
        void SetFollows(INode statement1, INode statement2);

        IEnumerable<INode> GetFollows(INode statement);

        IEnumerable<INode> GetFollowed(INode statement);

        bool IsFollows(INode statement1, INode statement2);
    }
}
