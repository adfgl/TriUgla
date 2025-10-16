using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Runtime;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.Expressions
{
    public sealed class NodeExprAssignmentCompound : NodeExprBase
    {
        public NodeExprAssignmentCompound(NodeExprBase id, Token op, NodeExprBase expr) : base(op)
        {
            Assignee = id;
            Expression = expr;
        }

        public NodeExprBase Assignee { get; }
        public NodeExprBase Expression { get; }

        TuValue AssignToTuple(TuRuntime rt, NodeExprValueAt valueAt, TuValue value)
        {
            TuValue curr = valueAt.Evaluate(rt);
            if (curr.type != EDataType.Tuple)
            {
                throw new RunTimeException($"Expected '{EDataType.Tuple}' but got '{curr.type}'", valueAt.TupleExp.Token);
            }

            TuTuple tpl = curr.AsTuple();
            if (value.type != curr.type)
            {
                throw new RunTimeException($"Expected '{curr.type}' but got '{value.type}'", Expression.Token);
            }

            ETokenType op = Token.type;

            TuValue newValue;
            switch (value.type)
            {
                case EDataType.Numeric:
                    double curDbl = curr.AsNumeric();
                    double newDbl = EvalNumeric(curDbl, value.AsNumeric());
                    newValue = new TuValue(newDbl);
                    break;

                case EDataType.Text:
                    TuText txt = curr.AsText();
                    if (op == ETokenType.PlusEqual)
                    {
                        txt = txt.Add(value.AsString());
                    }
                    else if (op == ETokenType.MinusEqual)
                    {
                        txt = txt.Remove(value.AsString());
                    }
                    else
                    {
                        throw new RunTimeException($"", Token);
                    }
                    newValue = new TuValue(txt);
                    break;

                default:
                    throw new RunTimeException($"", Token);
            }

            tpl[valueAt.Index] = newValue;
            return new TuValue(tpl);
        }


        TuValue AssignToIdentifier(TuRuntime rt, NodeExprIdentifier id, TuValue value)
        {
            id.DeclareIfMissing = false;
            id.Evaluate(rt);
            Variable vrbl = id.Variable!;
            TuValue curr = vrbl.Value;

            TuValue newValue;
            ETokenType op = Token.type;
            switch (curr.type)
            {
                case EDataType.Numeric:
                    if (value.type != EDataType.Numeric)
                    {
                        throw new Exception();
                    }
                    double curDbl = curr.AsNumeric();
                    double newDbl = EvalNumeric(curDbl, value.AsNumeric());
                    newValue = new TuValue(newDbl);
                    break;

                case EDataType.Text:
                    TuText txt = curr.AsText();
                    if (op == ETokenType.PlusEqual)
                    {
                        txt = txt.Add(value.AsString());
                    }
                    else if (op == ETokenType.MinusEqual)
                    {
                        txt = txt.Remove(value.AsString());
                    }
                    else
                    {
                        throw new RunTimeException($"Invalid operation for type '{vrbl.Value.type}'", vrbl.Identifier);
                    }
                    newValue = new TuValue(txt);
                    break;

                case EDataType.Tuple:
                    if (value.type != EDataType.Numeric &&
                        value.type != EDataType.Range &&
                        value.type != EDataType.Tuple)
                    {
                        throw new Exception();
                    }

                    TuTuple curTpl = curr.AsTuple();
                    if (op == ETokenType.PlusEqual)
                    {
                        curTpl.Add(value);
                    }
                    else if (op == ETokenType.MinusEqual)
                    {
                        curTpl.Remove(value);
                    }
                    else
                    {
                        throw new RunTimeException($"Invalid operation for type '{vrbl.Value.type}'", vrbl.Identifier);
                    }
                    newValue = new TuValue(curTpl);
                    break;

                default:
                    throw new RunTimeException($"Invalid operation for type '{vrbl.Value.type}'", vrbl.Identifier);
            }
            vrbl.Assign(newValue);
            return vrbl.Value;
        }

        double EvalNumeric(double curDbl, double newDbl)
        {
            switch (Token.type)
            {
                case ETokenType.MinusEqual:
                    curDbl -= newDbl;
                    break;

                case ETokenType.PlusEqual:
                    curDbl += newDbl;
                    break;

                case ETokenType.StarEqual:
                    curDbl *= newDbl;
                    break;

                case ETokenType.SlashEqual:
                    curDbl /= newDbl;
                    break;

                case ETokenType.ModuloEqual:
                    curDbl %= newDbl;
                    break;

                case ETokenType.PowerEqual:
                    curDbl = Math.Pow(curDbl, newDbl);
                    break;

                default:
                    throw new CompileTimeException($"Unsupported operator '{Token.value}'", Token);
            }
            return curDbl;
        }

        protected override TuValue EvaluateInvariant(TuRuntime rt)
        {
            TuValue value = Expression.Evaluate(rt);
            if (Assignee is NodeExprIdentifier id)
            {
                return AssignToIdentifier(rt, id, value);
            }

            if (Assignee is NodeExprValueAt at)
            {
                return AssignToTuple(rt, at, value);
            }

            throw new CompileTimeException($"Unsupported operator '{Token.value}'", Token);
        }
    }
}
