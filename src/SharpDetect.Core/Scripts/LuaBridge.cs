using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Configuration;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Scripts;
using SharpDetect.Common.Scripts.ExpressionBuilder;
using SharpDetect.Common.Services.Scripts;
using SharpDetect.Common.Utilities;
using CapturedParameterInfoFactory = System.Func<ushort, ushort, bool, SharpDetect.Common.LibraryDescriptors.CapturedParameterInfo>;
using MethodIdentifierFactory = System.Func<string, string, bool, ushort,
    SharpDetect.Common.Utilities.ValueCollection<string>, bool,
    SharpDetect.Common.LibraryDescriptors.MethodIdentifier>;
using MethodRecord = System.ValueTuple<SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData>;
using MethodRecordFactory = System.Func<SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData,
    (SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData)>;

namespace SharpDetect.Core.Scripts
{
    internal class LuaBridge : ILuaBridge
    {
        public string[] ModuleDirectories { get; }
        private string[] luaPaths { get; }
        private static readonly Action<Script> globalsInitializer;
        private readonly Func<string, string[], Script> scriptFactory;

        static LuaBridge()
        {
            // Accessible types
            UserData.RegisterType<UIntPtr>();
            UserData.RegisterType<CSharpExpressionBuilder>();
            UserData.RegisterType<MethodIdentifier>();
            UserData.RegisterType<MethodInterpretationData>();
            UserData.RegisterType<CapturedParameterInfo>();
            UserData.RegisterType<(MethodIdentifier, MethodInterpretationData)>();
            UserData.RegisterType<List<(MethodIdentifier, MethodInterpretationData)>>();
            UserData.RegisterType<ResultChecker>();
            UserData.RegisterType<BinaryOperationType>();
            UserData.RegisterType<MethodInterpretation>();
            UserData.RegisterType<MethodRewritingFlags>();

            // Converters
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(ValueCollection<string>), table
                => new ValueCollection<string>(table.ToObject<List<string>>()));

            // Globals
            globalsInitializer = script =>
            {
                // Accessible reference types
                script.Globals[nameof(CSharpExpressionBuilder)] = typeof(CSharpExpressionBuilder);
                script.Globals[nameof(MethodInterpretationData)] = typeof(MethodInterpretationData);
                // Accessible value types
                script.Globals[nameof(MethodIdentifier)] = (MethodIdentifierFactory)((n, d, s, argc, args, i) => new MethodIdentifier(n, d, s, argc, args, i));
                script.Globals[nameof(CapturedParameterInfo)] = (CapturedParameterInfoFactory)((i, s, d) => new CapturedParameterInfo(i, s, d));
                script.Globals[nameof(MethodRecord)] = (MethodRecordFactory)((i, d) => (i, d));
                script.Globals[nameof(UIntPtr)] = UserData.CreateStatic<UIntPtr>();
                script.Globals[nameof(BinaryOperationType)] = UserData.CreateStatic<BinaryOperationType>();
                script.Globals[nameof(MethodInterpretation)] = UserData.CreateStatic<MethodInterpretation>();
                script.Globals[nameof(MethodRewritingFlags)] = UserData.CreateStatic<MethodRewritingFlags>();
            };
        }

        public LuaBridge(IConfiguration configuration)
        {
            var coreModules = configuration.GetSection(Constants.ModuleDescriptors.Core).Get<string[]>() ?? Enumerable.Empty<string>();
            var extensionModules = configuration.GetSection(Constants.ModuleDescriptors.Extensions).Get<string[]>() ?? Enumerable.Empty<string>();
            ModuleDirectories = coreModules.Concat(extensionModules).ToArray();
            luaPaths = ModuleDirectories.SelectMany(dir => { return new[] { $"{dir}/?", $"{dir}/?.lua" }; }).ToArray();

            // Environment setup
            scriptFactory = (code, paths) =>
            {
                var script = new Script();
                // Set script loader paths
                ((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = luaPaths;
                // Setup globals
                globalsInitializer(script);
                // Execute script
                script.DoString(code);
                return script;
            };
        }

        public async Task<Script> LoadModuleAsync(string modulePath)
        {
            if (!File.Exists(modulePath))
                ThrowHelper.ThrowArgumentException($"Module with path {modulePath} does not exist.");

            var code = await File.ReadAllTextAsync(modulePath);
            return scriptFactory(code, luaPaths);
        }

        public AssemblyDescriptorScript CreateAssemblyDescriptor(Script script)
        {
            return new AssemblyDescriptorScript(script);
        }
    }
}
