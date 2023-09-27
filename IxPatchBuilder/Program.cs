using IxPatchBuilder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

const string settingsFile = "appsettings.json";
const string sectionPaths = "paths";
const string sectionRules = "rules";
const string sectionLogLevel = "logLevel";
const string sectionLogFile = "logFile";

IConfiguration config = new ConfigurationBuilder()
	.AddJsonFile(settingsFile)
	.Build();
Paths? paths = config.GetSection(sectionPaths).Get<Paths>();
if (paths == null)
{
	Console.WriteLine($"Configuration file {settingsFile} does not contain '{sectionPaths}' section.");
	return;
}
Rules? rules = config.GetSection(sectionRules).Get<Rules>();
if (rules == null)
{
	Console.WriteLine($"Configuration file {settingsFile} does not contain '{sectionRules}' section.");
	return;
}
LogLevel? logLevel = config.GetSection(sectionLogLevel).Get<LogLevel>();
string? fileName = config.GetSection(sectionLogFile).Get<string>();
if (!string.IsNullOrWhiteSpace(fileName))
{
	fileName = string.Format(fileName, DateTime.Now);
}

ConsoleLogger logger = new ConsoleLogger(logLevel ?? LogLevel.Trace, fileName ?? String.Empty);

DateTime timeStart = DateTime.Now;
logger.LogInformation($"Start time: {timeStart}");
Comparer comparer = new Comparer(rules, logger);

comparer.CompareFolders(paths.Old, paths.New, paths.Patch);
comparer.DeleteEmptyFolders(paths.Patch);
DateTime timeEnd = DateTime.Now;
int duration = (timeEnd - timeStart).Minutes;
logger.LogInformation($"End time: {timeEnd}, Duration:{duration} minutes.");