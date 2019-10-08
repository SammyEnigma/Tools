using System;
using System.Collections.Generic;

namespace JsonToEntity.Model
{
    public class FieldInfo
    {
        public string RawName;
        public string Name;
        public string RawComment;
        public List<string> CommentStr;
        public Type RawType;
        public string TypeStr;
        public List<string> AttributeStr;
    }
}
