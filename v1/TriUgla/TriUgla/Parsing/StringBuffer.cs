using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing
{
    public class StringBuffer
    {
        char[] s_buffer;
        int _count = 0;

        public StringBuffer(int capacity = 1024)
        {
            s_buffer = new char[capacity];
        }

        public bool AllowResize { get; set; } = true;

        public int Count => _count;

        public void Reset()
        {
            _count = 0;
        }

        public void Push(char ch)
        {
            Grow(1);
            s_buffer[_count++] = ch;
        }

        public void Push(string str)
        {
            Grow(str.Length);
            str.CopyTo(0, s_buffer, _count, str.Length);
            _count += str.Length;
        }

        public ReadOnlySpan<char> ToSpan()
        {
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(s_buffer, 0, _count);
            Reset();
            return span;
        }

        public override string ToString()
        {
            string value = new string(s_buffer, 0, _count);
            Reset();
            return value;
        }

        void Grow(int offset)
        {
            if (_count + offset >= s_buffer.Length)
            {
                if (false == AllowResize)
                {
                    throw new OutOfMemoryException("String buffer is full.");
                }

                int newSize = Math.Max((_count + offset) * 2, s_buffer.Length * 2);
                char[] newBuffer = new char[newSize];
                Buffer.BlockCopy(s_buffer, 0, newBuffer, 0, _count * sizeof(char));
                s_buffer = newBuffer;
            }
        }
    }
}
