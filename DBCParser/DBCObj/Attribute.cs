using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DBCParser.DBCObj {
    public class DBCAttribute : DBCAttributeBase {
        private readonly IntDBCAttributeDefinition _intDef;
        private readonly HexDBCAttributeDefinition _hexDef;
        private readonly FloatDBCAttributeDefinition _floatDef;
        private readonly StringDBCAttributeDefinition _stringDef;
        private readonly EnumDBCAttributeDefinition _enumDef;
        public AttributeValue AttriValue { get; private set; }

        public string ValueToString {
            get {
                switch (ValType) {
                case AttriValType.STRING:
                    return AttriValue.strValue;
                case AttriValType.INT:
                    return AttriValue.iValue.ToString();
                case AttriValType.HEX:
                    return AttriValue.uValue.ToString();
                case AttriValType.FLOAT:
                    return AttriValue.dValue.ToString();
                case AttriValType.ENUM:
                    return AttriValue.strValue;
                default:
                    return string.Empty;
                }
            }
        }

        public DBCAttributeBase Definition {
            get {
                switch (ValType) {
                case AttriValType.STRING:
                    return _stringDef;
                case AttriValType.INT:
                    return _intDef;
                case AttriValType.HEX:
                    return _hexDef;
                case AttriValType.FLOAT:
                    return _floatDef;
                case AttriValType.ENUM:
                    return _enumDef;
                default:
                    return null;
                }
            }
        }

        public DBCAttribute(IntDBCAttributeDefinition definition, int value) {
            _intDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                iValue = value
            };
        }

        public DBCAttribute(HexDBCAttributeDefinition definition, uint value) {
            _hexDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                uValue = value
            };
        }

        public DBCAttribute(FloatDBCAttributeDefinition definition, double value) {
            _floatDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                dValue = value
            };
        }

        public DBCAttribute(StringDBCAttributeDefinition definition, string value) {
            _stringDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                strValue = value
            };
        }

        public DBCAttribute(EnumDBCAttributeDefinition definition, string value) {
            _enumDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                strValue = _enumDef.Cast(value)
            };
        }

        public DBCAttribute(EnumDBCAttributeDefinition definition, int value) {
            _enumDef = definition;
            Name = definition.Name;
            ObjType = definition.ObjType;
            ValType = definition.ValType;
            AttriValue = new AttributeValue {
                valType = definition.ValType,
                strValue = _enumDef.Cast(value)
            };
        }

    }

    public abstract class DBCAttributeBase {
        public string Name { get; protected set; }
        public DBCObjType ObjType { get; protected set; }
        public AttriValType ValType { get; protected set; }
    }

    public abstract class DBCAttributeDefinition<T> : DBCAttributeBase {
        private T _defaultValue;
        public T DefaultValue {
            get { return _defaultValue; }
            set {
                if (CheckValue(value)) {
                    _defaultValue = Cast(value);
                }
            }
        }

        protected DBCAttributeDefinition(string name, DBCObjType objType) {
            Name = name;
            ObjType = objType;
        }

        public virtual bool CheckValue(T value) {
            return true;
        }

        public virtual T Cast(T value) {
            return value;
        }
    }

    public class StringDBCAttributeDefinition : DBCAttributeDefinition<string> {
        public StringDBCAttributeDefinition(string name, DBCObjType objType) : base(name, objType) {
            ValType = AttriValType.STRING;
            DefaultValue = string.Empty;
        }
    }

    public class EnumDBCAttributeDefinition : DBCAttributeDefinition<string> {
        public List<string> Values;

        public EnumDBCAttributeDefinition(string name, DBCObjType objType, List<string> values) : base(name, objType) {
            ValType = AttriValType.ENUM;
            Values = values;
            DefaultValue = values.Count > 0 ? values[0] : null;
        }

        public override bool CheckValue(string value) {
            try {
                _ = Cast(value);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public override string Cast(string value) {
            if (Values.Contains(value)) {
                return value;
            }
            throw new ApplicationException("Value not in enum");
        }

        public string Cast(int value) {
            if (value < 0 || value >= Values.Count) {
                throw new ApplicationException("Enum index out of range");
            }
            return Values[value];
        }

    }

    public class FloatDBCAttributeDefinition : DBCAttributeDefinition<double> {
        public double ValueMin { get; set; }
        public double ValueMax { get; set; }

        public FloatDBCAttributeDefinition(string name, DBCObjType objType, double valueMin, double valueMax) : base(name, objType) {
            ValType = AttriValType.FLOAT;
            ValueMin = valueMin;
            ValueMax = valueMax;
            DefaultValue = valueMin;
        }

        public override bool CheckValue(double value) {
            if ((ValueMin <= value && value <= ValueMax) || (ValueMin == 0 && ValueMax == 0)) {
                return true;
            } else {
                return false;
            }
        }
    }

    public class IntDBCAttributeDefinition : DBCAttributeDefinition<int> {
        public int ValueMin { get; set; }
        public int ValueMax { get; set; }

        public IntDBCAttributeDefinition(string name, DBCObjType objType, int valueMin, int valueMax) : base(name, objType) {
            ValType = AttriValType.INT;
            ValueMin = valueMin;
            ValueMax = valueMax;
            DefaultValue = valueMin;
        }

        public override bool CheckValue(int value) {
            if ((ValueMin <= value && value <= ValueMax) || (ValueMin == 0 && ValueMax == 0)) {
                return true;
            } else {
                return false;
            }
        }
    }

    public class HexDBCAttributeDefinition : DBCAttributeDefinition<uint> {
        public uint ValueMin { get; set; }
        public uint ValueMax { get; set; }

        public HexDBCAttributeDefinition(string name, DBCObjType objType, uint valueMin, uint valueMax) : base(name, objType) {
            ValType = AttriValType.HEX;
            ValueMin = valueMin;
            ValueMax = valueMax;
            DefaultValue = valueMin;
        }

        public override bool CheckValue(uint value) {
            if ((ValueMin <= value && value <= ValueMax) || (ValueMin == 0 && ValueMax == 0)) {
                return true;
            } else {
                return false;
            }
        }

    }

    public enum DBCObjType {
        DBCObjBase,
        Signal,
        Message,
        Node
    }

    public enum AttriValType {
        STRING,
        INT,
        HEX,
        FLOAT,
        ENUM
    }

    public class AttributeValue {
        public ValueType valType;
        public int iValue;
        public uint uValue;
        public double dValue;
        public string strValue;
    }

}
