using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LibraryTest
{

    public class TestOutputLogger<T> : ILogger<T>
    {
        public TestContext TestContext { get; set; }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            TestContext?.WriteLine($"{logLevel}: {formatter(state, exception)}");
        }
    }
}
