using System.Globalization;
using TriUgla.Script.Data.Collections;

namespace TriUgla.Script.Data
{
    public readonly struct Value
    {
        readonly DataKind _kind;
        readonly double _number;
        readonly Obj? _object;

        public DataKind Kind => _kind;
        public Obj? Object => _object;

        public bool IsUndefined => _kind == DataKind.Undefined;
        public bool IsInteger => _kind == DataKind.Integer;
        public bool IsDouble => _kind == DataKind.Double;
        public bool IsBoolean => _kind == DataKind.Boolean;
        public bool IsString => _kind == DataKind.String;
        public bool IsList => _kind == DataKind.List;
        public bool IsRange => _kind == DataKind.Range;
        public bool IsNumber => _kind is DataKind.Integer or DataKind.Double;
        public bool IsObject => _object is not null;
        public bool IsSequence => _kind is DataKind.List or DataKind.Range;

        public static Value Undefined => default;

        Value(DataKind kind, double number, Obj? obj)
        {
            _kind = kind;
            _number = number;
            _object = obj;
        }

        public Value(int value)
            : this(DataKind.Integer, value, null)
        {
        }

        public Value(double value)
            : this(DataKind.Double, value, null)
        {
        }

        public Value(bool value)
            : this(DataKind.Boolean, value ? 1.0 : 0.0, null)
        {
        }

        public Value(Obj obj)
            : this(obj.Kind, 0.0, obj)
        {
        }

        public static Value FromString(string value)
            => new(new ObjString(value));

        public static Value FromList(List<Value> values)
            => new(new ObjList(values));

        public static Value FromRange(
            Value start,
            Value end,
            Value? step = null)
        {
            return new Value(
                new ObjRange(start, end, step ?? new Value(1)));
        }

        public static Value FromObject(Obj obj)
            => new(obj);

        public int AsInt()
        {
            if (_kind != DataKind.Integer)
                throw Error(DataKind.Integer);

            return (int)_number;
        }

        public double AsDouble()
        {
            return _kind switch
            {
                DataKind.Integer => (int)_number,
                DataKind.Double => _number,
                _ => throw Error(DataKind.Double)
            };
        }

        public bool AsBool()
        {
            return _kind switch
            {
                DataKind.Boolean => _number != 0.0,
                DataKind.Integer => (int)_number != 0,
                DataKind.Double => _number != 0.0,
                _ => throw Error(DataKind.Boolean)
            };
        }

        public string AsString()
        {
            if (_object is ObjString str)
                return str.Content;

            throw Error(DataKind.String);
        }

        public ObjList AsObjList()
        {
            if (_object is ObjList list)
                return list;

            throw Error(DataKind.List);
        }

        public ObjRange AsRange()
        {
            if (_object is ObjRange range)
                return range;

            throw Error(DataKind.Range);
        }

        public T AsObject<T>() where T : Obj
        {
            if (_object is T obj)
                return obj;

            throw new InvalidOperationException(
                $"Expected {typeof(T).Name}, got {_kind}.");
        }

        InvalidOperationException Error(DataKind expected)
        {
            return new InvalidOperationException(
                $"Expected {expected}, got {_kind}.");
        }

        public override string ToString()
        {
            return Kind switch
            {
                DataKind.Undefined => "undefined",
                DataKind.Integer => AsInt().ToString(CultureInfo.InvariantCulture),
                DataKind.Double => AsDouble().ToString("0.0################", CultureInfo.InvariantCulture),
                DataKind.Boolean => AsBool() ? "true" : "false",
                DataKind.String => AsString(),
                _ => Object?.ToString() ?? "undefined"
            };
        }
    }
}
