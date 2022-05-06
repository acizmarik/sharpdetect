using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Core.Communication;
using Xunit;

namespace SharpDetect.UnitTests.Communication
{
    public class RewritingMessageHubTests : TestsBase
    {
        [Fact]
        public void RewritingMessageHubTests_TypeInjected()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            MDToken typeToken = new(654);

            var raised = false;
            var type = default(TypeInfo);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.TypeInjected += args =>
            {
                type = args.TypeInfo;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                TypeInjected = new Notify_TypeInjected()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeToken.Raw
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, type.ModuleId);
            Assert.Equal(typeToken, type.TypeToken);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void RewritingMessageHubTests_MethodInjected()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            MDToken typeToken = new(654);
            MDToken functionToken = new(321);
            MethodType methodType = MethodType.FieldAccess;

            var raised = false;
            var function = default(FunctionInfo);
            var type = default(MethodType);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.MethodInjected += args =>
            {
                function = args.FunctionInfo;
                type = args.Type;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                MethodInjected = new Notify_MethodInjected()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeToken.Raw,
                    FunctionToken = functionToken.Raw,
                    Type = methodType
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeToken, function.TypeToken);
            Assert.Equal(functionToken, function.FunctionToken);
            Assert.Equal(methodType, type);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void RewritingMessageHubTests_MethodWrapperInjected()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            MDToken typeToken = new(654);
            MDToken functionToken = new(321);
            MDToken wrapperToken = new(101);

            var raised = false;
            var function = default(FunctionInfo);
            var wrapper = default(MDToken);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.MethodWrapperInjected += args =>
            {
                function = args.FunctionInfo;
                wrapper = args.WrapperToken;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                MethodWrapperInjected = new Notify_MethodWrapperInjected()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeToken.Raw,
                    OriginalFunctionToken = functionToken.Raw,
                    WrapperFunctionToken = wrapperToken.Raw
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeToken, function.TypeToken);
            Assert.Equal(functionToken, function.FunctionToken);
            Assert.Equal(wrapperToken, wrapper);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void RewritingMessageHubTests_TypeReferenced()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            MDToken typeToken = new(654);

            var raised = false;
            var type = default(TypeInfo);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.TypeReferenced += args =>
            {
                type = args.TypeInfo;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                TypeReferenced = new Notify_TypeReferenced()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeToken.Raw
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, type.ModuleId);
            Assert.Equal(typeToken, type.TypeToken);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void RewritingMessageHubTests_HelperMethodReferenced()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            MDToken typeToken = new(654);
            MDToken functionToken = new(321);
            MethodType methodType = MethodType.FieldAccess;

            var raised = false;
            var function = default(FunctionInfo);
            var type = default(MethodType);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.HelperMethodReferenced += args =>
            {
                function = args.FunctionInfo;
                type = args.Type;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                HelperMethodReferenced = new Notify_HelperMethodReferenced()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeToken.Raw,
                    FunctionToken = functionToken.Raw,
                    Type = methodType
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeToken, function.TypeToken);
            Assert.Equal(functionToken, function.FunctionToken);
            Assert.Equal(methodType, type);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void RewritingMessageHubTests_WrapperMethodReferenced()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr defModuleId = new(1987);
            MDToken defTypeToken = new(1654);
            MDToken defFunctionToken = new(1321);
            UIntPtr refModuleId = new(2987);
            MDToken refTypeToken = new(2654);
            MDToken refFunctionToken = new(2321);

            var raised = false;
            var definition = default(FunctionInfo);
            var reference = default(FunctionInfo);
            var info = default(EventInfo);
            var hub = new RewritingMessageHub(LoggerFactory);
            hub.WrapperMethodReferenced += args =>
            {
                definition = args.Definition;
                reference = args.Reference;
                info = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                WrapperMethodReferenced = new Notify_WrapperMethodReferenced()
                {
                    DefModuleId = defModuleId.ToUInt64(),
                    DefTypeToken = defTypeToken.Raw,
                    DefFunctionToken = defFunctionToken.Raw,
                    RefModuleId = refModuleId.ToUInt64(),
                    RefTypeToken = refTypeToken.Raw,
                    RefFunctionToken = refFunctionToken.Raw
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(defModuleId, definition.ModuleId);
            Assert.Equal(defTypeToken, definition.TypeToken);
            Assert.Equal(defFunctionToken, definition.FunctionToken);
            Assert.Equal(refModuleId, reference.ModuleId);
            Assert.Equal(refTypeToken, reference.TypeToken);
            Assert.Equal(refFunctionToken, reference.FunctionToken);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }
    }
}
