// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace SharpDetect.E2ETests.Subject.Helpers.DataRaces
{
    public class DataRace
    {
        public static object? Test_DataRace_ReferenceType_Static;
        public static int Test_DataRace_ValueType_Static;
        public static int Test_DataRace_ValueType_StaticWrittenInCtor;
        public static int Test_DataRace_ValueType_StaticProperty { get; set; }
        public static volatile int Test_Volatile_ValueType_Static;
        public static volatile int Test_Volatile_ValueType_Static_Back;
        public static int Test_TaskCompletionSource_ValueType_Static;
        public static int Test_AsyncContinuation_ValueType_Static;
        public static int Test_Atomic_ValueType_Static;
        public static string? Test_Atomic_ReferenceType_Static;
        [ThreadStatic]
        public static object? Test_ThreadStatic_ReferenceType;
        [ThreadStatic]
        public static int Test_ThreadStatic_ValueType;
        public static Action? Test_StaticDelegate;
        
        public object? Test_DataRace_ReferenceType_InstanceField;
        public object? Test_DataRace_ReferenceType_InstanceProperty { get; set; }
        public int Test_DataRace_ValueType_InstanceField;
        public int Test_DataRace_ValueType_InstanceProperty { get; set; }
        public volatile int Test_Volatile_ValueType_Instance;
        public int Test_Atomic_ValueType_Instance;

        public DataRace()
        {
            
        }

        public DataRace(int value, object referenceValue, bool setProperties)
        {
            if (setProperties)
            {
                Test_DataRace_ValueType_InstanceProperty = value;
                Test_DataRace_ReferenceType_InstanceProperty = referenceValue;
            }
            else
            {
                Test_DataRace_ValueType_InstanceField = value;
                Test_DataRace_ReferenceType_InstanceField = referenceValue;
                Test_DataRace_ValueType_StaticWrittenInCtor = value;
            }
        }
    }

    public class StaticCctorHelperInit
    {
        public static int Value;

        static StaticCctorHelperInit()
        {
            InitializeValue(42);
        }

        private static void InitializeValue(int value)
        {
            Value = value;
        }
    }

    public class StaticHelperWrite
    {
        public static int Value;

        public static void WriteValue(int value)
        {
            Value = value;
        }
    }

    public class CtorSetterInit
    {
        public int Value { get; set; }

        public CtorSetterInit(int value)
        {
            Value = value;
        }
    }

    public class CrossObjectWriter
    {
        public int Value;

        public CrossObjectWriter()
        {
        }

        public CrossObjectWriter(CrossObjectWriter other, int value)
        {
            other.Value = value;
        }
    }

    public class NativeHandleOwner
    {
        public nint Handle;

        public NativeHandleOwner(nint handle)
        {
            Handle = handle;
        }

        ~NativeHandleOwner()
        {
            Handle = 0;
        }
    }

    public class AsyncExecution
    {
        private const int SuspendMilliseconds = 50;

        private int _attemptNumber;
        private object? _activeContext;
        public Task? ExecutionTask { get; private set; }
        public int ObservedAttemptNumber { get; private set; }

        public void Initialize(int attemptNumber, object activeContext)
        {
            _attemptNumber = attemptNumber;
            _activeContext = activeContext;
            ExecutionTask = ExecuteAsync();
        }
        
        private async Task ExecuteAsync()
        {
            await Task.Delay(SuspendMilliseconds).ConfigureAwait(false);
            ObservedAttemptNumber = _attemptNumber;
            _ = _activeContext;
        }
    }
}