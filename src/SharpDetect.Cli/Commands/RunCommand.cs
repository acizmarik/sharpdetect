// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;

namespace SharpDetect.Cli.Commands;

[Command("run", Description = "Executes analysis using inline options, or based on a provided configuration file")]
public sealed partial class RunCommand : AnalysisCommandBase
{
    [CommandParameter(0, Description = "Configuration file (omit to use inline options)")]
    public string? ConfigurationFile { get; set; }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        ValidateOptions();

        var useInlineForm = string.IsNullOrEmpty(ConfigurationFile);
        var previousDirectory = Directory.GetCurrentDirectory();
        RunCommandHandler? commandHandler = null;

        try
        {
            if (useInlineForm)
            {
                var arguments = InitCommandHandler.BuildRunCommandArgs(ToAnalysisOptions());
                var rawJson = InitCommandHandler.SerializeRunCommandArgs(arguments);
                commandHandler = new RunCommandHandler(arguments, rawJson);
            }
            else
            {
                var directoryName = Path.GetDirectoryName(ConfigurationFile);
                var workingDirectory = string.IsNullOrEmpty(directoryName) ? previousDirectory : directoryName;
                Directory.SetCurrentDirectory(workingDirectory);
                var fileName = Path.GetFileName(ConfigurationFile!);
                commandHandler = new RunCommandHandler(fileName);
            }

            var cancellationToken = console.RegisterCancellationHandler();
            await commandHandler.ExecuteAsync(console, cancellationToken);
        }
        catch (CommandException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new CommandException(
                message: "Analysis was cancelled by user",
                exitCode: (int)ExitCode.Cancelled);
        }
        catch (Exception exception)
        {
            throw new CommandException(
                message: $"Analysis failed: {ExceptionMessages.Flatten(exception)}",
                exitCode: (int)ExitCode.AnalysisError,
                innerException: exception);
        }
        finally
        {
            commandHandler?.Dispose();
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    internal void ValidateOptions()
    {
        var hasConfigFile = !string.IsNullOrEmpty(ConfigurationFile);

        if (hasConfigFile && !File.Exists(ConfigurationFile))
            throw new CommandException(
                $"Configuration file {ConfigurationFile} does not exist.",
                (int)ExitCode.ConfigurationError);

        if (hasConfigFile && GetSpecifiedInlineOptions().ToArray() is { Length: > 0 } inlineOptions)
            throw UsageError(
                $"{string.Join(", ", inlineOptions)} cannot be combined with a configuration file; set the corresponding values in the file instead.");

        if (!hasConfigFile && string.IsNullOrEmpty(TargetPath))
            throw UsageError("Either provide a configuration file or specify a target (--target).");

        ValidateSharedOptions();
    }
}
