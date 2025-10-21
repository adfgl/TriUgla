namespace TriScript.Data
{
    using System;
    using System.Collections.Generic;
    using TriScript.Data.Objects;

    public class ObjHeap
    {
        sealed class Entry
        {
            public Obj Obj { get; set; }
            public int RefCount { get; set; }
            public Entry(Obj obj, int rc) { Obj = obj; RefCount = rc; }
        }

        readonly Dictionary<uint, Entry> _map = new Dictionary<uint, Entry>();
        readonly Dictionary<string, Pointer> _stringCache = new Dictionary<string, Pointer>();
        
        uint _nextId = 1; // 0 is Pointer.Null

        public int Count => _map.Count;

        public Pointer Allocate(Obj obj)
        {
            bool isString = obj.Type == EDataType.String;
            if (isString && _stringCache.TryGetValue(((ObjString)obj).Content, out Pointer ptr))
            {
                return ptr;
            }

            uint id = _nextId++;
            _map[id] = new Entry(obj, rc: 1);
            ptr = new Pointer(id);

            if (isString)
            {
                _stringCache[((ObjString)obj).Content] = ptr;
            }
            return ptr;
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
                e.RefCount += n;
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
