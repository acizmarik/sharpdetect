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
                (ReportBase OriginalReport, StringBuilder FormatBuilder, List<object?> ArgumentsBuilder)>();

            foreach (var report in reports)
            {
                var messageBuilder = new StringBuilder();
                var argumentsBuilder = new List<object?>();

                messageBuilder.Append("[{0}][PID={1}]: ");
                argumentsBuilder.Add(report.Category);
                argumentsBuilder.Add(report.ProcessId);
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
                    reportsLookup[identifier] = (report, messageBuilder, argumentsBuilder);
                }
                else
                {
                    // This error was already pre-constructed, add more information
                    messageBuilder = builders.FormatBuilder;
                    argumentsBuilder = builders.ArgumentsBuilder;
                }

                foreach (var sourceLink in report.SourceLinks ?? Enumerable.Empty<SourceLink>())
                {
                    if (sourceLink.SequencePoint is SequencePoint sequencePoint)
                    {
                        // PDB is available, we can provide better source mapping information
                        messageBuilder.AppendLine();
                        messageBuilder.Append($"\t at {{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(sequencePoint.Document.Url);
                        messageBuilder.Append($":{{{argumentsBuilder.Count}}}:");
                        argumentsBuilder.Add(sequencePoint.StartLine);
                        messageBuilder.Append($"{{{argumentsBuilder.Count}}} occurred ");
                        argumentsBuilder.Add(sequencePoint.StartColumn);
                        messageBuilder.Append($"{{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(sourceLink.Type);
                    }
                    else
                    {
                        // PDB is not available, we should provide instruction offsets
                        messageBuilder.AppendLine();
                        messageBuilder.Append($"\t at {{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(sourceLink.Method);
                        messageBuilder.Append($" on offset {{{argumentsBuilder.Count}}} occurred ");
                        argumentsBuilder.Add($"IL_{sourceLink.Instruction.Offset:X4}");
                        messageBuilder.Append($"{{{argumentsBuilder.Count}}}");
                        argumentsBuilder.Add(sourceLink.Type);
                    }
                }
            }

            foreach (var (report, messageBuilder, argumentsBuilder) in reportsLookup.Values)
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
