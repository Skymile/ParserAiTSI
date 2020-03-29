namespace Core.Interfaces.PQL
{
    using System.Collections.Generic;
    
    public interface IModifies
    {
        IEnumerable<(int, INode)> ModifiesTable { get; }
        void SetModify(INode statement, IVariable variable);
        IEnumerable<(int, INode)> GetModifies(IVariable variable);
        IEnumerable<(int, IVariable)> GetModifies(INode statement);
        bool isModifies(INode statement, IVariable variable);
    }
}
