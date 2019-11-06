using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace JsonDictConvert
{
    /*
     * 说明:
     * 实践下来第一个converter要好用得多,但是需要额外的一点if (ret != null && ret.GetType() == typeof(JObject))这种处理
     * 
     * link:
     * https://stackoverflow.com/questions/6416017/json-net-deserializing-nested-dictionaries
     * https://stackoverflow.com/questions/5546142/how-do-i-use-json-net-to-deserialize-into-nested-recursive-dictionary-and-list
     * https://stackoverflow.com/questions/29616596/how-to-use-default-serialization-in-a-custom-jsonconverter
     * https://stackoverflow.com/questions/11561597/deserialize-json-recursively-to-idictionarystring-object/31250524
     */
    public class DictConverter : CustomCreationConverter<IDictionary<string, object>>
    {
        public override IDictionary<string, object> Create(Type objectType)
        {
            return new Dictionary<string, object>();
        }

        public override bool CanConvert(Type objectType)
        {
            // in addition to handling IDictionary<string, object>
            // we want to handle the deserialization of dict value
            // which is of type object
            return objectType == typeof(object) || base.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)
            return serializer.Deserialize(reader);
        }
    }

    public static class DictExtension
    {
        public static Dictionary<string, object> ToJsonDict(this string str)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(str, new JsonConverter[] { new DictConverter() });
        }

        public static Dictionary<string, object> GetDict(this Dictionary<string, object> dict, string key)
        {
            return GetValue<Dictionary<string, object>>(dict, key);
        }

        public static Dictionary<string, object> GetDictByPath(this Dictionary<string, object> dict, string keyPath)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp;
        }

        public static List<T> GetList<T>(this Dictionary<string, object> dict, string key)
        {
            object arr = null;
            dict.TryGetValue(key, out arr);
            if (arr == null)
                return null;

            return ((JArray)arr).ToObject<List<T>>();
        }

        public static List<T> GetListByPath<T>(this Dictionary<string, object> dict, string keyPath)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            object arr = null;
            tmp.TryGetValue(keys[i], out arr);
            if (arr == null)
                return null;

            return ((JArray)arr).ToObject<List<T>>();
        }

        public static string GetString(this Dictionary<string, object> dict, string key, string @default = null)
        {
            var ret = GetValue<string>(dict, key);
            if (ret == null && @default != null)
                return @default;

            return ret;
        }

        public static string GetStringByPath(this Dictionary<string, object> dict, string keyPath, string @default = null)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp.GetString(keys[i], @default);
        }

        public static int GetInt(this Dictionary<string, object> dict, string key, int? @default = null)
        {
            object ret = null;
            if (dict.TryGetValue(key, out ret))
            {
                if (ret == null)
                    goto FALLBACK;
                return Convert.ToInt32(ret);
            }

            FALLBACK:
            if (@default.HasValue)
                return @default.Value;
            throw new Exception("获取int异常，键不存在或值不是int类型");
        }

        public static int GetIntByPath(this Dictionary<string, object> dict, string keyPath, int? @default = null)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp.GetInt(keys[i], @default);
        }

        public static long GetLong(this Dictionary<string, object> dict, string key, long? @default = null)
        {
            object ret = null;
            if (dict.TryGetValue(key, out ret))
            {
                if (ret == null)
                    goto FALLBACK;
                return Convert.ToInt64(ret);
            }

            FALLBACK:
            if (@default.HasValue)
                return @default.Value;
            throw new Exception("获取long异常，键不存在或值不是long类型");
        }

        public static long GetLongByPath(this Dictionary<string, object> dict, string keyPath, long? @default = null)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp.GetLong(keys[i], @default);
        }

        public static double GetDouble(this Dictionary<string, object> dict, string key, double? @default = null)
        {
            object ret = null;
            if (dict.TryGetValue(key, out ret))
            {
                if (ret == null)
                    goto FALLBACK;
                return Convert.ToDouble(ret);
            }

            FALLBACK:
            if (@default.HasValue)
                return @default.Value;
            throw new Exception("获取double异常，键不存在或值不是double类型");
        }

        public static double GetDoubleByPath(this Dictionary<string, object> dict, string keyPath, double? @default = null)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp.GetDouble(keys[i], @default);
        }

        public static bool GetBool(this Dictionary<string, object> dict, string key, bool? @default = null)
        {
            object ret = null;
            if (dict.TryGetValue(key, out ret))
            {
                if (ret == null)
                    goto FALLBACK;
                return Convert.ToBoolean(ret);
            }

            FALLBACK:
            if (@default.HasValue)
                return @default.Value;
            throw new Exception("获取bool异常，键不存在或值不是bool类型");
        }

        public static bool GetBoolByPath(this Dictionary<string, object> dict, string keyPath, bool? @default = null)
        {
            var keys = keyPath.Split('\\');
            var i = 0;
            var tmp = dict;
            while (i < keys.Length - 1)
            {
                tmp = tmp.GetDict(keys[i]);
                i++;
            }

            return tmp.GetBool(keys[i], @default);
        }

        public static T GetValue<T>(this Dictionary<string, object> dict, string key)
            where T : class
        {
            object ret = null;
            dict.TryGetValue(key, out ret);
            if (ret != null && ret.GetType() == typeof(JObject))
                return ((JObject)ret).ToObject<T>();

            return ret as T;
        }
    }
}
