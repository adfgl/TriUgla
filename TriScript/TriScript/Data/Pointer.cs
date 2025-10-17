using System.Runtime.InteropServices;

namespace TriScript.Data
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public readonly struct Pointer : IEquatable<Pointer>
    {
        public static readonly Pointer Null = new Pointer(0);

        [FieldOffset(0)] public readonly uint id;      

        public Pointer(uint id)
        {
            this.id = id;
        }

        public bool IsNull => id == 0;

        public override string ToString() => IsNull ? "null" : $"ptr[{id}]";
        public bool Equals(Pointer other) => id == other.id;

        public override bool Equals(object? obj) => obj is Pointer p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(id);

        public static bool operator ==(Pointer a, Pointer b) => a.Equals(b);
        public static bool operator !=(Pointer a, Pointer b) => !a.Equals(b);
    }
}
