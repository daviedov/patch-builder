using Microsoft.Extensions.Logging;

namespace IxPatchBuilder
{
	public class ConsoleLogger : ILogger
	{
		private readonly LogLevel _logLevel;

		public ConsoleLogger(LogLevel logLevel)
		{
			_logLevel = logLevel;
		}

		public IDisposable BeginScope<TState>(TState state) => default;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string formattedString = formatter(state, exception);
			if (logLevel >= _logLevel)
			{
				Console.WriteLine(@"{0}: {1}", logLevel, formattedString);
			}
		}
	}
}
