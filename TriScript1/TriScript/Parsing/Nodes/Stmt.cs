using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public abstract class Stmt : INode
    {
        public abstract bool Accept<T>(INodeVisitor<T> visitor, out T? result);
    }
}
