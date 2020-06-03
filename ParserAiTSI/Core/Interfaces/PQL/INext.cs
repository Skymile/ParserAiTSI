using System.Collections.Generic;

namespace Core.Interfaces.PQL
{
    public interface INext
    {
        void SetNext(INode statement1, INode statement2);

        IEnumerable<INode> GetNext(INode statement);

        bool IsNext(INode statement1, INode statement2);
    }
}
