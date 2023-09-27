using Microsoft.Extensions.Logging;

namespace IxPatchBuilder
{
	public class ConsoleLogger : ILogger
	{
		private readonly LogLevel _logLevel;
		private readonly string _fileName;

		public ConsoleLogger(LogLevel logLevel, string fileName)
		{
			_logLevel = logLevel;
			_fileName = fileName;
		}

		public IDisposable BeginScope<TState>(TState state) => default;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string formattedString = formatter(state, exception);
			if (logLevel >= _logLevel)
			{
				string logRow = @$"{DateTime.Now:T}: {logLevel}: {formattedString}";
				Console.WriteLine(logRow);
				if (!string.IsNullOrWhiteSpace(_fileName))
				{
					File.AppendAllText(_fileName, $"{logRow}\r\n");
				}
			}
		}
	}
}
