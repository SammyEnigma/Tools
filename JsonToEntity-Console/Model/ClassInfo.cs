using System.Collections.Generic;

namespace JsonToEntity.Model
{
    public class ClassInfo
    {
        // 目前按文件生成类，一个文件中的类总是使用同样的using并且从属于同一名称空间
        public string Namespace;
        public List<string> Using;

        public string Name;
        public string FullName;
        public string RawComment;
        public List<string> CommentStr;
        public List<FieldInfo> Fields;
    }
}
