using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Data;

namespace TriUgla.Parsing.Runtime
{
    public sealed class RuntimeFlow
    {
        private readonly Stack<EFlowControl> _loops = new();

        public bool HasReturn { get; private set; }
        public TuValue ReturnValue { get; private set; } = TuValue.Nothing;

        public void EnterLoop() => _loops.Push(EFlowControl.None);
        public void LeaveLoop() { if (_loops.Count > 0) _loops.Pop(); }

        public void SignalBreak()
        {
            if (_loops.Count == 0) return;
            _loops.Pop(); _loops.Push(EFlowControl.Break);
        }
        public void SignalContinue()
        {
            if (_loops.Count == 0) return;
            _loops.Pop(); _loops.Push(EFlowControl.Continue);
        }
        public void SignalReturn(TuValue v)
        {
            HasReturn = true;
            ReturnValue = v;
        }

        public bool IsBreak => _loops.Count > 0 && _loops.Peek() == EFlowControl.Break;
        public bool IsContinue => _loops.Count > 0 && _loops.Peek() == EFlowControl.Continue;

        public void ConsumeBreakOrContinue()
        {
            if (_loops.Count == 0) return;
            var top = _loops.Pop();
            if (top == EFlowControl.Break || top == EFlowControl.Continue) top = EFlowControl.None;
            _loops.Push(top);
        }
    }
}
