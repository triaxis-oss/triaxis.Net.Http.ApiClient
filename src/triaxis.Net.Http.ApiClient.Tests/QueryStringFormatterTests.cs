using System;
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

        private static string FormatQuery<T>(string query, T args)
            => QueryStringFormatter<T>.Format(query, args);
    }
}
