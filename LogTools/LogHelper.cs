using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LogTools
{
    public sealed class PropertyValue
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public sealed class LoggerEx
    {
        sealed class ExcludeFieldsContractResolver : DefaultContractResolver
        {
            bool _retain;
            int _level = 0;
            string[] _fields;

            public ExcludeFieldsContractResolver(string[] props, bool retain = true)
            {
                this._fields = props;
                this._retain = retain;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var list = base.CreateProperties(type, memberSerialization);
                var res = list.Where(p =>
                {
                    if (_retain)
                        return _fields.Contains(p.PropertyName);
                    else
                        return !_fields.Contains(p.PropertyName);
                }).ToList();

                // 实际中对象的字段类型可能是一个复杂类型，当期望输出该字段时，我们需要一并输出其内部的
                // 信息，如果要自己去处理该字段的CreateProperties会很麻烦；
                // 这里我们取巧，当CreateProperties发生递归调用时我们即知道对应字段是一个复杂类型，剩下
                // 的交给base.CreateProperties即可，最为保险
                if (++_level >= 2)
                    return base.CreateProperties(type, memberSerialization);
                return res;
            }
        }

        Logger _log;
        public LoggerEx()
        {
            _log = LogManager.GetLogger(GetType().ToString());
        }

        public LoggerEx(string name)
        {
            _log = LogManager.GetLogger(name);
        }

        public void Log(string message, object parameter = null)
        {
            if (_log.IsDebugEnabled)
            {
                if (parameter != null)
                {
                    if (parameter is string)
                    {
                        _log.ConditionalDebug($"{message} ===> 参数：【{parameter}】");
                    }
                    else if (parameter is Exception)
                    {
                        _log.ConditionalDebug($"{message} ===>【发生异常，消息：{Environment.NewLine}{((Exception)parameter).Message}{Environment.NewLine}堆栈：{((Exception)parameter).StackTrace}】");
                    }
                    else
                    {
                        _log.ConditionalDebug($"{message} ===> 参数：【{DumpObj(parameter)}】");
                    }
                }
                else
                {
                    _log.ConditionalDebug(message);
                }
            }
        }

        public void Error(string message, object parameter = null)
        {
            if (_log.IsErrorEnabled || _log.IsFatalEnabled)
            {
                if (parameter != null)
                {
                    if (parameter is Exception)
                    {
                        _log.Error($"{message} ===>【发生异常，消息：{Environment.NewLine}{((Exception)parameter).Message}{Environment.NewLine}堆栈：{((Exception)parameter).StackTrace}】");
                    }
                    else if (parameter is string)
                    {
                        _log.Error($"{message} ===> 参数：【{parameter}】");
                    }
                    else
                    {
                        _log.Error($"{message} ===> 参数：【{DumpObj(parameter)}】");
                    }
                }
                else
                {
                    _log.Error(message);
                }
            }
        }

        public string DumpObj<T>(T obj, params Expression<Func<T, object>>[] fieldSelector)
        {
            if (fieldSelector != null && fieldSelector.Length > 0)
            {
                JsonSerializerSettings jsetting = new JsonSerializerSettings();
                jsetting.ContractResolver = new ExcludeFieldsContractResolver(fieldSelector.Select(p => GetName(p)).ToArray());
                return JsonConvert.SerializeObject(obj, Formatting.Indented, jsetting);
            }
            else
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
        }

        public string DumpObjs<T>(IList<T> objs, params Expression<Func<T, object>>[] fieldSelector)
        {
            return DumpObjs(objs, 0, objs.Count, fieldSelector);
        }

        public string DumpObjs<T>(IList<T> objs, int skip = -1, int take = -1, params Expression<Func<T, object>>[] fieldSelector)
        {
            // https://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression
            IEnumerable<T> tmp = objs;
            if (skip > 0)
                tmp = tmp.Skip(skip);
            if (take > 0)
                tmp = tmp.Take(take);

            if (fieldSelector != null && fieldSelector.Length > 0)
            {
                JsonSerializerSettings jsetting = new JsonSerializerSettings();
                jsetting.ContractResolver = new ExcludeFieldsContractResolver(fieldSelector.Select(p => GetName(p)).ToArray());
                return JsonConvert.SerializeObject(tmp, Formatting.Indented, jsetting);
            }
            else
            {
                return JsonConvert.SerializeObject(tmp, Formatting.Indented);
            }
        }

        private string GetName<TSource, TField>(Expression<Func<TSource, TField>> Field)
        {
            if (object.Equals(Field, null))
                throw new NullReferenceException("Field is required");

            MemberExpression expr = null;
            if (Field.Body is MemberExpression)
            {
                expr = (MemberExpression)Field.Body;
            }
            else if (Field.Body is UnaryExpression)
            {
                expr = (MemberExpression)((UnaryExpression)Field.Body).Operand;
            }
            else
            {
                const string Format = "Expression '{0}' not supported.";
                var message = string.Format(Format, Field);

                throw new ArgumentException(message, "Field");
            }

            return expr.Member.Name;
        }

        private IEnumerable<PropertyValue> GetProperties(object obj)
        {
            // https://stackoverflow.com/questions/6025858/get-names-of-the-params-passed-to-a-c-sharp-method
            if (obj != null)
            {
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
                foreach (PropertyDescriptor prop in props)
                {
                    object val = prop.GetValue(obj);
                    if (val != null)
                    {
                        yield return new PropertyValue { Name = prop.Name, Value = val };
                    }
                }
            }
        }
    }
}
