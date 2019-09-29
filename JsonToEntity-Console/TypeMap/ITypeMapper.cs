using JsonToEntity.Model;

namespace JsonToEntity.Core
{
    public interface ITypeMapper
    {
        string MapType(FieldInfo fieldInfo);
    }
}
