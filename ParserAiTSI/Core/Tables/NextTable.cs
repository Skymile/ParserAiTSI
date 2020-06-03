using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class NextTable : TableBase<INode, INode>, INext
    {
        public IEnumerable<INode> GetNext(INode statement) => GetFrom(statement);
        public bool IsNext(INode statement1, INode statement2) => Is(statement1, statement2);
        public void SetNext(INode statement1, INode statement2) => Set(statement1, statement2);
    }
}
