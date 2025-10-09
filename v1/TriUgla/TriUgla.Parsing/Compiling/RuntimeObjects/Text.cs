using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling.RuntimeObjects;

namespace TriUgla.Parsing.Compiling
{
    public class Text : Object
    {
        public Text(string content)
        {
            Content = content;
        }

        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }
}
