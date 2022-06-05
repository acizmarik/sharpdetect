using MoonSharp.Interpreter;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Scripts.ExpressionBuilder;
using SharpDetect.Common.Services.Scripts;
using SharpDetect.Common.Utilities;
using MethodRecord = System.ValueTuple<SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData>;
using MethodIdentifierFactory = System.Func<string, string, bool, ushort,
    SharpDetect.Common.Utilities.ValueCollection<string>, bool,
    SharpDetect.Common.LibraryDescriptors.MethodIdentifier>;
using MethodRecordFactory = System.Func<SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData,
    (SharpDetect.Common.LibraryDescriptors.MethodIdentifier, SharpDetect.Common.LibraryDescriptors.MethodInterpretationData)>;
using CapturedParameterInfoFactory = System.Func<ushort, ushort, bool, SharpDetect.Common.LibraryDescriptors.CapturedParameterInfo>;
using Microsoft.Extensions.Configuration;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Scripts;
using MoonSharp.Interpreter.Loaders;

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
            var coreModules = configuration.GetSection(Constants.ModuleDescriptors.CoreModulesPaths).Get<string[]>() ?? Enumerable.Empty<string>();
            var extensionModules = configuration.GetSection(Constants.ModuleDescriptors.ExtensionModulePaths).Get<string[]>() ?? Enumerable.Empty<string>();
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
            Guard.True<ArgumentException>(File.Exists(modulePath));

            var code = await File.ReadAllTextAsync(modulePath);
            return scriptFactory(code, luaPaths);
        }

        public AssemblyDescriptorScript CreateAssemblyDescriptor(Script script)
        {
            return new AssemblyDescriptorScript(script);
        }
    }
}
