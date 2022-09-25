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
            foreach (var report in reports)
            {
                var messageBuilder = new StringBuilder();
                var argumentsBuilder = new List<object?>();

                messageBuilder.Append("[{category}][PID={pid}]: ");
                argumentsBuilder.Add(report.Category);
                argumentsBuilder.Add(report.ProcessId);
                messageBuilder.Append(report.MessageFormat.Trim());
                foreach (var messageArgument in report.Arguments ?? Enumerable.Empty<object?>())
                    argumentsBuilder.Add(messageArgument);
                messageBuilder.Append(" reported by {plugin}");
                argumentsBuilder.Add(report.Reporter);

                foreach (var sourceLink in report.SourceLinks ?? Enumerable.Empty<SourceLink>())
                {
                    if (sourceLink.SequencePoint is SequencePoint sequencePoint)
                    {
                        // PDB is available, we can provide better source mapping information
                        messageBuilder.AppendLine();
                        messageBuilder.Append($"\t at {{source}}:{{line}}:{{column}} occurred {{event}}");
                        argumentsBuilder.Add(sequencePoint.Document.Url);
                        argumentsBuilder.Add(sequencePoint.StartLine);
                        argumentsBuilder.Add(sequencePoint.StartColumn);
                        argumentsBuilder.Add(sourceLink.Type);
                    }
                    else
                    {
                        // PDB is not available, we should provide instruction offsets
                        messageBuilder.AppendLine();
                        messageBuilder.Append($"\t at {{method}} on offset {{instruction}} occurred {{event}}");
                        argumentsBuilder.Add(sourceLink.Method);
                        argumentsBuilder.Add($"IL_{sourceLink.Instruction.Offset:X4}");
                        argumentsBuilder.Add(sourceLink.Type);
                    }
                }

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
