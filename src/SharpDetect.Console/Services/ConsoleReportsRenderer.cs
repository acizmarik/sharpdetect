using CliWrap.Builders;
using dnlib.DotNet.Pdb;
using Serilog;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Common.SourceLinks;
using System.Text;

namespace SharpDetect.Console.Services
{
    internal class ConsoleReportsRenderer : IReportsRenderer
    {
        private readonly ILogger logger;

        public ConsoleReportsRenderer()
        {
            logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Level}]{Message:lj}{NewLine}")
                .CreateLogger();
        }

        public void Render(IEnumerable<ReportBase> reports)
        {
            var reportsLookup = new Dictionary<string, 
                (ReportBase OriginalReport, StringBuilder FormatBuilder, List<object?> ArgumentsBuilder, HashSet<ReportDataEntry> VisitedEntries)>();

            foreach (var report in reports)
            {
                var messageBuilder = new StringBuilder();
                var argumentsBuilder = new List<object?>();
                var visitedEntries = new HashSet<ReportDataEntry>();

                messageBuilder.Append("[{0}]: ");
                argumentsBuilder.Add(report.Category);
                messageBuilder.Append(report.MessageFormat.Trim());
                foreach (var messageArgument in report.Arguments ?? Enumerable.Empty<object?>())
                    argumentsBuilder.Add(messageArgument);
                messageBuilder.Append($" reported by {{{argumentsBuilder.Count}}}");
                argumentsBuilder.Add(report.Reporter);

                // Construct report identifier
                var identifier = string.Format(messageBuilder.ToString(), argumentsBuilder.ToArray());
                if (!reportsLookup.TryGetValue(identifier, out var builders))
                {
                    // This is the first time this report was found, initialize it
                    reportsLookup[identifier] = (report, messageBuilder, argumentsBuilder, visitedEntries);
                }
                else
                {
                    // This error was already pre-constructed, add more information
                    messageBuilder = builders.FormatBuilder;
                    argumentsBuilder = builders.ArgumentsBuilder;
                    visitedEntries = builders.VisitedEntries;
                }

                if (report.Entries is ReportDataEntry[] entries)
                {
                    foreach (var entry in entries.Where(e => !visitedEntries.Contains(e)))
                    {
                        visitedEntries.Add(entry);
                        if (entry.SourceLink.SequencePoint is SequencePoint sequencePoint)
                        {
                            // PDB is available, we can provide better source mapping information
                            messageBuilder.AppendLine();
                            messageBuilder.Append($"\t at {{{argumentsBuilder.Count}}}");
                            argumentsBuilder.Add(sequencePoint.Document.Url);
                            messageBuilder.Append($":{{{argumentsBuilder.Count}}} occurred ");
                            argumentsBuilder.Add(sequencePoint.StartLine);
                        }
                        else
                        {
                            // PDB is not available, we should provide instruction offsets
                            messageBuilder.AppendLine();
                            messageBuilder.Append($"\t at {{{argumentsBuilder.Count}}}");
                            argumentsBuilder.Add(entry.SourceLink.Method);
                            messageBuilder.Append($" on offset {{{argumentsBuilder.Count}}} occurred ");
                            argumentsBuilder.Add($"IL_{entry.SourceLink.Instruction.Offset:X4}");
                        }

                        // Common info about threads
                        messageBuilder.Append($"{{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(entry.Type);
                        messageBuilder.Append($" executed by thread {{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(entry.Thread);
                    }
                }
            }

            foreach (var (report, messageBuilder, argumentsBuilder, _) in reportsLookup.Values)
            {
                var messageFormat = messageBuilder.ToString();
                var arguments = argumentsBuilder.ToArray();

                switch (report)
                {
                    case ErrorReport:
                        logger.Error(messageFormat, arguments);
                        break;
                    case WarningReport:
                        logger.Warning(messageFormat, arguments);
                        break;
                    case InformationReport:
                        logger.Information(messageFormat, arguments);
                        break;
                }
            }
        }
    }
}
