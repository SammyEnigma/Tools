using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonToEntity.Repository
{
    public interface IEntityRepository
    {
        Dictionary<Type, string> GetTypeMap();
        /// <summary>
        /// 获取模板文件地址
        /// </summary>
        /// <returns></returns>
        string GetTemplate();
        /// <summary>
        /// 要读取的entity文件地址
        /// </summary>
        /// <returns></returns>
        string GetPath();
        string GetLanguage();

    }
}
