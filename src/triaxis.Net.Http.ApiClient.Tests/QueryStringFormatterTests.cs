using System;
using System.Text.Json;
using NUnit.Framework;

namespace triaxis.Net.Http.ApiClient.Tests
{
    public class QueryStringFormatterTests
    {
        [Test]
        public void Format_Simple()
        {
            Assert.That(FormatQuery("test", new { a = 3}), Is.EqualTo("test?a=3"));
        }

        [Test]
        public void Format_Overrides()
        {
            Assert.That(FormatQuery("test", new { abcDef = "test" },
                JsonNamingPolicy.KebabCaseLower.ConvertName,
                o => o.ToString()?.ToUpperInvariant()), Is.EqualTo("test?abc-def=TEST"));

        }
        private static string FormatQuery<T>(string query, T args, Func<string, string>? nameFormat = null, Func<object, string?>? valueFormat = null)
            => QueryStringFormatter<T>.Format(query, args, nameFormat, valueFormat);
    }
}
