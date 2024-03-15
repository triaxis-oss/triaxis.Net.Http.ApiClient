using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using triaxis.Reflection;

namespace triaxis.Net.Http
{
    internal class QueryStringFormatter<T>
    {
        private static readonly IValueTypePropertyGetter<T>[] _getters =
            typeof(T).GetProperties().Select(p => p.GetValueTypeGetter<T>()).ToArray();

        public static readonly Func<string, string> DefaultNameFormat = s => s;
        public static readonly Func<object, string?> DefaultValueFormat = o => o.ToString();

        public static string Format(string query, T args, Func<string, string>? nameFormat = null, Func<object, string?>? valueFormat = null)
        {
            nameFormat ??= DefaultNameFormat;
            valueFormat ??= DefaultValueFormat;

            StringBuilder? sb = null;
            void AddValue(string name, string value)
            {
                if (sb == null)
                {
                    sb = new StringBuilder(query);
                    sb.Append('?');
                }
                else
                {
                    sb.Append('&');
                }
                sb.Append(name);
                sb.Append('=');
                sb.Append(HttpUtility.UrlEncode(value));
            }

            foreach (var g in _getters)
            {
                var name = nameFormat(g.Property.Name);
                var value = g.Get(ref args);
                if (value is IEnumerable e && !(value is string))
                {
                    foreach (var item in e)
                    {
                        if (item is not null && valueFormat(item) is string s)
                            AddValue(name, s);
                    }
                }
                else if (value is not null && valueFormat(value) is string s)
                {
                    AddValue(name, s);
                }
            }
            return sb?.ToString() ?? query;
        }
    }
}
