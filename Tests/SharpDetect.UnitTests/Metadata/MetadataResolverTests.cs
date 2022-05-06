using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Metadata;
using Xunit;

namespace SharpDetect.UnitTests.Metadata
{
    public class MetadataResolverTests : TestsBase
    {
        [Fact]
        public void MetadataResolver_TryGetModule()
        {
            // Prepare
            const int processId = 123;
            InjectedData state = new(processId);
            IModuleBindContext modulesContext = GetCleanModuleBindContext();
            MetadataResolver resolver = new(processId, modulesContext, state);
            string coreLibPath = typeof(object).Assembly.Location;
            ModuleInfo moduleInfo = new(new(456));

            // Act & Assert
            var loadedModule = modulesContext.LoadModule(processId, coreLibPath, moduleInfo);
            Assert.True(resolver.TryGetModuleDef(moduleInfo, out var resolvedModule));
            Assert.Equal(loadedModule, resolvedModule);
        }

        [Fact]
        public void MetadataResover_TryGetType()
        {
            // Prepare
            const int processId = 123;
            InjectedData state = new(processId);
            IModuleBindContext modulesContext = GetCleanModuleBindContext();
            MetadataResolver resolver = new(processId, modulesContext, state);
            string coreLibPath = typeof(object).Assembly.Location;
            ModuleInfo moduleInfo = new(new(456));
            MDToken monitorTypeToken = new(typeof(Monitor).MetadataToken);
            TypeInfo typeInfo = new(moduleInfo.Id, monitorTypeToken);

            // Act & Assert
            modulesContext.LoadModule(processId, coreLibPath, moduleInfo);
            Assert.True(resolver.TryGetTypeDef(typeInfo, moduleInfo, out var resolvedType));
            Assert.Equal(monitorTypeToken, resolvedType!.MDToken);
        }

        [Fact]
        public void MetadataResover_TryGetMethod()
        {
            // Prepare
            const int processId = 123;
            InjectedData state = new(processId);
            IModuleBindContext modulesContext = GetCleanModuleBindContext();
            MetadataResolver resolver = new(processId, modulesContext, state);
            string coreLibPath = typeof(object).Assembly.Location;
            ModuleInfo moduleInfo = new(new(456));
            MDToken monitorTypeToken = new(typeof(Monitor).MetadataToken);
            TypeInfo typeInfo = new(moduleInfo.Id, monitorTypeToken);
            MDToken exitMethodToken = new(typeof(Monitor).GetMethod(nameof(Monitor.Exit))!.MetadataToken);
            FunctionInfo functionInfo = new(moduleInfo.Id, monitorTypeToken, exitMethodToken);

            // Act & Assert
            modulesContext.LoadModule(processId, coreLibPath, moduleInfo);
            Assert.True(resolver.TryGetMethodDef(functionInfo, moduleInfo, out var resolvedMethod));
            Assert.Equal(monitorTypeToken, resolvedMethod!.DeclaringType.MDToken);
            Assert.Equal(exitMethodToken, resolvedMethod!.MDToken);
        }

        [Fact]
        public void MetadataResolver_LookupType()
        {
            // Prepare
            const int processId = 123;
            InjectedData state = new(processId);
            IModuleBindContext modulesContext = GetCleanModuleBindContext();
            MetadataResolver resolver = new(processId, modulesContext, state);
            string coreLibPath = typeof(object).Assembly.Location;
            ModuleInfo moduleInfo = new(new(456));
            const string lookupTarget = "System.Threading.Monitor";

            // Act & Assert
            modulesContext.LoadModule(processId, coreLibPath, moduleInfo);
            Assert.True(resolver.TryLookupTypeDef(lookupTarget, moduleInfo, out var loadedType));
            Assert.Equal(lookupTarget, loadedType!.FullName);
        }
    }
}
