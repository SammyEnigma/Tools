using JsonToEntity.Repository;
using JsonToEntity.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonToEntity.Business
{
    public class ParseEntity : ILanguageEntityRepository
    {
        public string CSharpParseEntity(ICSharpEntityRepository csharp)
        {
            var parse = new ParseEntityUtil(csharp.GetTypeMap(), csharp.GetTemplate(), csharp.GetPath());
            return parse.ParseEntity();
        }

        public string JavaParseEneity(IJavaEntityRepository java)
        {
            throw new NotImplementedException();
        }
    }
}
