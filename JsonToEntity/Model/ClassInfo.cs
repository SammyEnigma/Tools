using System.Collections.Generic;

namespace JsonToEntity.Model
{
    public class ClassInfo
    {
        public string Name;
        public string RawComment;
        public List<string> CommentStr;
        public List<FieldInfo> Fields;
    }
}
