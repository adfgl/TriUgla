using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Data
{
    public class TuText : TuObject
    {
        public TuText(string content)
        {
            Content = content;
        }

        public string Content { get; private set; }

        public TuText Add(string text)
        {
            Content += text;
            return this;
        }

        public TuText Remove(string text)
        {
            Content = Content.Replace(text, "");
            return this;    
        }

        public override string ToString()
        {
            return Content;
        }

        public override TuText Clone()
        {
            return new TuText(Content);
        }
    }
}
