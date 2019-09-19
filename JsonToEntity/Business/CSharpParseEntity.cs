using JsonToEntity.Repository;
using System;
using System.Collections.Generic;
using JsonToEntity.Util;
using System.IO;

namespace JsonToEntity.Business
{
    public class CSharpParseEntity : ICSharpEntityRepository
    {
        public string GetLanguage()
        {
            return "CSharp";
        }

        public string GetPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "test", "testclass.cs");
        }

        public string GetTemplate()
        {
            return "csharp/template.cshtml";
        }

        public Dictionary<Type, string> GetTypeMap()
        {
            return new Dictionary<Type, string>
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
    }
}
