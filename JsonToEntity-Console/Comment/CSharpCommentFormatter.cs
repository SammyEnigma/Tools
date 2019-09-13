using System;
using System.Collections.Generic;

namespace JsonToEntity.Core
{
    public class CSharpCommentFormatter : ICommentFormatter
    {
        public List<string> Format(string rawComment)
        {
            var ret = new List<string>();
            var arrs = rawComment.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 1; i < arrs.Length - 1; i++)
                ret.Add($"/// {arrs[i]}");

            return ret;
        }
    }
}
