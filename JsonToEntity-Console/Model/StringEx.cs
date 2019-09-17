using System;
using System.Linq;

namespace JsonToEntity.Model
{
    public static class StringEx
    {
        public static string Indent(this string str, int count = 4)
        {
            return string.Join(string.Empty, Enumerable.Repeat(' ', count)) + str;
        }

        public static string NewLine(this string str)
        {
            return str + Environment.NewLine;
        }
    }
}
