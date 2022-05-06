using dnlib.DotNet;
using SharpDetect.Core.Models;
using SharpDetect.Core.Models.CoreLibrary;
using Xunit;

namespace SharpDetect.UnitTests.Models
{
    public class CoreLibDescriptorTests
    {
        [Fact]
        public void CoreLibDescriptor_ResolvesMonitorLockMethods()
        {
            // Prepare
            var registry = new MethodDescriptorRegistry();
            registry.Register(new CoreLibDescriptor());
            var moduleDef = AssemblyDef.Load(typeof(object).Assembly.Location).ManifestModule;
            var corTypes = moduleDef.CorLibTypes;
            var typeDef = moduleDef.Types.Single(t => t.ReflectionFullName == typeof(Monitor).FullName);
            var enterBlocking1 = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Enter) && m.Parameters.Count == 1);
            var enterBlocking2 = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Enter) && m.Parameters.Count == 2);
            var tryEnter = typeDef.Methods.Single(m => m.Name == "ReliableEnterTimeout");
            var exit = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Exit));

            // Assert
            Assert.True(registry.TryGetMethodInterpretationData(enterBlocking1, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(enterBlocking2, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(tryEnter, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(exit, out var _));
        }

        [Fact]
        public void CoreLibDescriptor_ResolvesMonitorSignalMethods()
        {
            // Prepare
            var registry = new MethodDescriptorRegistry();
            registry.Register(new CoreLibDescriptor());
            var moduleDef = AssemblyDef.Load(typeof(object).Assembly.Location).ManifestModule;
            var corTypes = moduleDef.CorLibTypes;
            var typeDef = moduleDef.Types.Single(t => t.ReflectionFullName == typeof(Monitor).FullName);
            var waitBlocking = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) && m.Parameters.Count == 1);
            var tryWait1 = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) && m.Parameters.Count == 2 && m.Parameters[1].Type == corTypes.Int32);
            var tryWait2 = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) && m.Parameters.Count == 2 && m.Parameters[1].Type.ReflectionFullName == typeof(TimeSpan).FullName!);
            var tryWait3 = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) && m.Parameters.Count == 3 && m.Parameters[1].Type.ReflectionFullName == typeof(int).FullName!);
            var pulse = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Pulse));
            var pulseAll = typeDef.Methods.Single(m => m.Name == nameof(Monitor.PulseAll));

            // Assert
            Assert.True(registry.TryGetMethodInterpretationData(waitBlocking, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(tryWait1, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(tryWait2, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(tryWait3, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(pulse, out var _));
            Assert.True(registry.TryGetMethodInterpretationData(pulseAll, out var _));
        }
    }
}
