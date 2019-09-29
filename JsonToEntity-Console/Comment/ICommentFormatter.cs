using System.Collections.Generic;

namespace JsonToEntity.Core
{
    public interface ICommentFormatter
    {
        List<string> Format(string rawComment);
    }
}
