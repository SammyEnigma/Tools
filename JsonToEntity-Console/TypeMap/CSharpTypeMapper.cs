using JsonToEntity.Model;
using System;
using System.Collections.Generic;

namespace JsonToEntity.Core
{
    public class CSharpTypeMapper : ITypeMapper
    {
        private Dictionary<Type, string> _typeMap;
        public CSharpTypeMapper()
        {
            _typeMap = new Dictionary<Type, string>
            {
                [typeof(byte)] = "byte",
                [typeof(sbyte)] = "sbyte",
                [typeof(short)] = "short",
                [typeof(ushort)] = "unshort",
                [typeof(int)] = "int",
                [typeof(uint)] = "uint",
                [typeof(long)] = "long",
                [typeof(ulong)] = "ulong",
                [typeof(float)] = "float",
                [typeof(double)] = "double",
                [typeof(decimal)] = "decimal",
                [typeof(bool)] = "bool",
                [typeof(string)] = "string",
                [typeof(char)] = "char",
                [typeof(Guid)] = "Guid",
                [typeof(DateTime)] = "DateTime",
                [typeof(DateTimeOffset)] = "DateTimeOffset",
                [typeof(TimeSpan)] = "TimeSpan",
                [typeof(object)] = "object"
            };
        }

        public string MapType(FieldInfo fieldInfo)
        {
            var ret = string.Empty;
            if (!_typeMap.TryGetValue(fieldInfo.RawType, out ret))
                return fieldInfo.RawType.Name;
            return ret;
        }
    }
}
