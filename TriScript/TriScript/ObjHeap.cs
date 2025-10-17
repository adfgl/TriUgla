namespace TriScript
{
    using System;
    using System.Collections.Generic;
    using TriScript.Data;

    public class ObjHeap
    {
        sealed class Entry
        {
            public Obj Obj;
            public int RefCount;
            public Entry(Obj obj, int rc) { Obj = obj; RefCount = rc; }
        }

        readonly Dictionary<uint, Entry> _map = new();
        uint _nextId = 1; // 0 is Pointer.Null

        public int Count => _map.Count;

        public Pointer Allocate(Obj obj)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));
            uint id = _nextId++;
            _map[id] = new Entry(obj, rc: 1);
            return new Pointer(id);
        }

        public bool TryGet(Pointer p, out Obj? obj)
        {
            if (p.IsNull) { obj = null; return false; }
            if (_map.TryGetValue(p.id, out var e)) { obj = e.Obj; return true; }
            obj = null; return false;
        }

        public Obj Get(Pointer p)
        {
            if (!_map.TryGetValue(p.id, out var e))
            {
                throw new InvalidOperationException($"Invalid pointer: {p}");
            }
            return e.Obj;
        }

        public bool AddRef(Pointer p, int n = 1)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (_map.TryGetValue(p.id, out var e))
            {
                checked { e.RefCount += n; } // will throw on overflow
                return true;
            }
            return false;
        }

        public bool Release(Pointer p, int n = 1)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (_map.TryGetValue(p.id, out var e))
            {
                if (e.RefCount < n) throw new InvalidOperationException($"Release would underflow: rc={e.RefCount}, n={n}");
                e.RefCount -= n;
                if (e.RefCount == 0)
                {
                    _map.Remove(p.id); // auto-free
                }
                return true;
            }
            return false;
        }

        public int GetRefCount(Pointer p)
            => _map.TryGetValue(p.id, out var e) ? e.RefCount : -1;

        public bool ForceFree(Pointer p) => _map.Remove(p.id);

        public bool IsAlive(Pointer p) => !p.IsNull && _map.ContainsKey(p.id);

        public void Clear()
        {
            _map.Clear();
            _nextId = 1;
        }
    }

}
