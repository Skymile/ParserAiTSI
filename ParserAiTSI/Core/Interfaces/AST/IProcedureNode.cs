using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.AST
{
    public interface IProcedureNode : INode
    {
        string Name { get; set; }
    }
}
