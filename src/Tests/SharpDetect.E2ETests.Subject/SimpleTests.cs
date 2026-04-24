// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using SharpDetect.E2ETests.Subject.Helpers;
using SharpDetect.E2ETests.Subject.Helpers.Arrays;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;
using SharpDetect.E2ETests.Subject.Helpers.Fields;

namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);
        
        public static void Test_MonitorMethods_EnterExit1()
        {
            lock (new object()) { }
        }

        public static void Test_MonitorMethods_EnterExit2()
        {
            var obj = new object();
            Monitor.Enter(obj);
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit1()
        {
            var obj = new object();
            Monitor.TryEnter(obj);
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit2()
        {
            var obj = new object();
            Monitor.TryEnter(obj, TimeSpan.FromSeconds(1));
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit3()
        {
            var obj = new object();
            var lockTaken = false;
            Monitor.TryEnter(obj, TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
                Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_Wait1()
        {
            var obj = new object();
            var terminate = new ManualResetEvent(initialState: false);
            
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                while (!terminate.WaitOne(TimeSpan.FromMilliseconds(1)))
                {
                    lock (obj)
                    {
                        Monitor.Pulse(obj);
                    }
                }
            });
            thread1.Start();
            
            lock (obj)
            {
                Monitor.Wait(obj, 1);
            }
            
            terminate.Set();
            thread1.Join();
        }

        public static void Test_MonitorMethods_Wait2()
        {
            var obj = new object();
            lock (obj)
            {
                Monitor.Wait(obj, 1);
            }
        }

        public static void Test_MonitorMethods_Wait3_Reentrancy()
        {
            var obj = new object();
            Monitor.Enter(obj);
            Monitor.Enter(obj);
            Monitor.Wait(obj, 1);
            Monitor.Exit(obj);
            Monitor.Exit(obj);
        }
        
#if NET10_0_OR_GREATER
        static class MonitorSdk10InternalApiAccessor
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ExitIfLockTaken")]
            public static extern void ExitIfLockTaken(
                [UnsafeAccessorType("System.Threading.Monitor")]
                object _,
                object obj,
                ref bool lockTaken);
        }
        
        public static void Test_MonitorMethods_ExitIfLockTaken()
        {
            var lockTaken = false;
            var lockObj = new object();
            Monitor.Enter(lockObj, ref lockTaken);
            MonitorSdk10InternalApiAccessor.ExitIfLockTaken(null!, lockObj, ref lockTaken);
        }
#endif

        public static void Test_ShadowCallstack_MonitorPulse()
        {
            var obj1 = new object();
            lock (obj1)
                Monitor.Pulse(obj1);
        }

        public static void Test_ShadowCallstack_MonitorPulseAll()
        {
            var obj1 = new object();
            lock (obj1)
                Monitor.PulseAll(obj1);
        }


        public static void Test_ShadowCallstack_MonitorWait_ReentrancyWithPulse()
        {
            var obj = new object();
            var ready = new ManualResetEvent(false);

            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                ready.WaitOne();
                lock (obj)
                    Monitor.Pulse(obj);
            });

            thread1.Start();
            lock (obj)
            {
                lock (obj)
                {
                    lock (obj)
                    {
                        ready.Set();
                        Monitor.Wait(obj);
                    }
                }
            }
            thread1.Join();
        }

        public static void Test_ShadowCallstack_MonitorTryEnter_LockNotTaken()
        {
            var obj1 = new object();
            var ready = new ManualResetEvent(false);
            var finish = new ManualResetEvent(false);

            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                lock (obj1)
                {
                    ready.Set();
                    finish.WaitOne();
                }
            });

            thread1.Start();
            ready.WaitOne();
            
            var lockTaken = false;
            Monitor.TryEnter(obj1, ref lockTaken);
            finish.Set();
            thread1.Join();
        }

#if NET10_0_OR_GREATER
        public static void Test_ShadowCallstack_MonitorExitIfLockTaken()
        {
            var lockObj = new object();
            var lockTaken = false;
            Monitor.Enter(lockObj, ref lockTaken);
            MonitorSdk10InternalApiAccessor.ExitIfLockTaken(null!, lockObj, ref lockTaken);
            MonitorSdk10InternalApiAccessor.ExitIfLockTaken(null!, lockObj, ref lockTaken);
        }
#endif

        public static void Test_ThreadMethods_Join1()
        {
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
            });
            thread1.Start();
            thread1.Join();
        }
        
        public static void Test_ThreadMethods_Join2()
        {
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
            });
            thread1.Start();
            thread1.Join(millisecondsTimeout: 1000);
        }
        
        public static void Test_ThreadMethods_Join3()
        {
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
            });
            thread1.Start();
            thread1.Join(timeout: TimeSpan.FromSeconds(1));
        }

        public static void Test_ThreadMethods_StartCallback1()
        {
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
            });
            thread1.Start();
            thread1.Join();
        }

        public static void Test_ThreadMethods_StartCallback2()
        {
            Task.Run(() => { }).Wait();
        }

        public static void Test_ThreadMethods_get_CurrentThread()
        {
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                var currentThread = Thread.CurrentThread;
            });
            thread1.Start();
            thread1.Join();
        }

        public static void Test_TaskMethods_ScheduleAndStart1()
        {
            Task.Run(() => { }).Wait();
        }

        public static void Test_TaskMethods_InnerInvoke1()
        {
            Task.Run(() => { }).Wait();
        }

        public static void Test_TaskMethods_Wait1()
        {
            Task.Run(() => { }).Wait();
        }

        public static void Test_TaskMethods_Wait2()
        {
            Task.CompletedTask.Wait();
        }

        public static void Test_TaskMethods_Wait3()
        {
            Task.Run(() => { }).Wait(millisecondsTimeout: 100);
        }
        
        public static void Test_TaskMethods_Wait4()
        {
            Task.Run(() => { }).Wait(timeout: TimeSpan.FromMilliseconds(100));
        }
        
        public static void Test_TaskMethods_Wait5()
        {
            Task.Run(() => { }).Wait(timeout: TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        public static void Test_TaskMethods_Result1()
        {
            _ = Task.Run(() => 42).Result;
        }

        public static void Test_TaskMethods_Await1()
        {
            Task.Run(() => { }).GetAwaiter().GetResult();
        }
        
        public static void Test_TaskMethods_Await2()
        {
            Task.Run(async () => await Task.Run(() => { })).Wait();
        }

#if NET9_0_OR_GREATER
        public static void Test_LockMethods_EnterExit1()
        {
            var lockObj = new Lock();
            lockObj.Enter();
            lockObj.Exit();
        }

        public static void Test_LockMethods_EnterExit2()
        {
            var lockObj = new Lock();
            using (lockObj.EnterScope())
            {
                
            }
        }

        public static void Test_LockMethods_TryEnterExit1()
        {
            var lockObj = new Lock();
            if (lockObj.TryEnter())
            {
                lockObj.Exit();
            }
        }

        public static void Test_LockMethods_TryEnterExit2()
        {
            var lockObj = new Lock();
            if (lockObj.TryEnter(millisecondsTimeout: 1000))
            {
                lockObj.Exit();
            }
        }
#endif

        public static void Test_Field_ValueType_Instance_Read()
        {
            var instance = new InstanceFieldValueType();
            _ = instance.Test_Field_ValueType_Instance;
        }

        public static void Test_Property_ValueType_Instance_Read()
        {
            var instance = new InstanceFieldValueType();
            _ = instance.Test_Property_ValueType_Instance;
        }

        public static void Test_Field_ValueType_Instance_Write()
        {
            var instance = new InstanceFieldValueType();
            instance.Test_Field_ValueType_Instance = 123;
        }

        public static void Test_Property_ValueType_Instance_Write()
        {
            var instance = new InstanceFieldValueType();
            instance.Test_Property_ValueType_Instance = 123;
        }

        public static void Test_Field_ReferenceType_Instance_Read()
        {
            var instance = new InstanceFieldReferenceType();
            _ = instance.Test_Field_ReferenceType_Instance;

        }

        public static void Test_Property_ReferenceType_Instance_Read()
        {
            var instance = new InstanceFieldReferenceType();
            _ = instance.Test_Property_ReferenceType_Instance;
        }

        public static void Test_Field_ReferenceType_Instance_Write()
        {
            var instance = new InstanceFieldReferenceType();
            instance.Test_Field_ReferenceType_Instance = new object();
        }

        public static void Test_Field_ValueType_OnValueType_Instance_Read()
        {
            var instance = new InstanceFieldOnValueType { ValueField = 42 };
            _ = instance.ValueField;
        }

        public static void Test_Field_ValueType_OnValueType_Instance_Write()
        {
            var instance = new InstanceFieldOnValueType();
            instance.ValueField = 42;
        }

        public static void Test_Field_ReferenceType_OnValueType_Instance_Read()
        {
            var instance = new InstanceFieldOnValueType { ReferenceField = new object() };
            _ = instance.ReferenceField;
        }

        public static void Test_Field_ReferenceType_OnValueType_Instance_Write()
        {
            var instance = new InstanceFieldOnValueType();
            instance.ReferenceField = new object();
        }

        public static void Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnReferenceType<int> { ValueField = 42 };
            _ = instance.ValueField;
        }

        public static void Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnReferenceType<int>();
            instance.ValueField = 42;
        }

        public static void Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnReferenceType<object> { ReferenceField = new object() };
            _ = instance.ReferenceField;
        }

        public static void Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnReferenceType<object>();
            instance.ReferenceField = new object();
        }

        public static void Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Read()
        {
            var instance = new NonGenericWithGenericMethods { ValueField = 42 };
            instance.GenericMethodReadValue<int>();
        }

        public static void Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Write()
        {
            var instance = new NonGenericWithGenericMethods();
            instance.GenericMethodWriteValue<int>(42);
        }

        public static void Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new NonGenericWithGenericMethods { ReferenceField = new object() };
            instance.GenericMethodReadReference<object>();
        }

        public static void Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new NonGenericWithGenericMethods();
            instance.GenericMethodWriteReference<object>(new object());
        }

        public static void Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnReferenceType<int> { ValueField = 42 };
            _ = instance.GenericMethodRead<int>();
        }

        public static void Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnReferenceType<int>();
            instance.GenericMethodWrite<int>(42);
        }

        public static void Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnReferenceType<object> { ReferenceField = new object() };
            _ = instance.GenericMethodRead<object>();
        }

        public static void Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnReferenceType<object>();
            instance.GenericMethodWrite<object>(new object());
        }

        public static void Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnValueType<int> { ValueField = 42 };
            _ = instance.ValueField;
        }

        public static void Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnValueType<int>();
            instance.ValueField = 42;
        }

        public static void Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Read()
        {
            var instance = new GenericInstanceFieldOnValueType<object> { ReferenceField = new object() };
            _ = instance.ReferenceField;
        }

        public static void Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Write()
        {
            var instance = new GenericInstanceFieldOnValueType<object>();
            instance.ReferenceField = new object();
        }

        public static void Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Read()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<int> { ArrayField = new int[1] };
            _ = instance.ArrayField;
        }

        public static void Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Write()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<int>();
            instance.ArrayField = new int[1];
        }

        public static void Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<object> { ArrayField = new object[1] };
            _ = instance.ArrayField;
        }

        public static void Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<object>();
            instance.ArrayField = new object[1];
        }

        public static void Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Read()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<int> { ListField = new List<int> { 42 } };
            _ = instance.ListField;
        }

        public static void Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Write()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<int>();
            instance.ListField = new List<int> { 42 };
        }

        public static void Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<object> { ListField = new List<object> { new object() } };
            _ = instance.ListField;
        }

        public static void Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new ComplexTypeInstanceFieldOnReferenceType<object>();
            instance.ListField = new List<object> { new object() };
        }

        public static void Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Read()
        {
            var instance = new TwoTypeParamInstanceFieldOnReferenceType<object, int> { SecondField = 42 };
            _ = instance.SecondField;
        }

        public static void Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Write()
        {
            var instance = new TwoTypeParamInstanceFieldOnReferenceType<object, int>();
            instance.SecondField = 42;
        }

        public static void Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Read()
        {
            var instance = new TwoTypeParamInstanceFieldOnReferenceType<int, object> { SecondField = new object() };
            _ = instance.SecondField;
        }

        public static void Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Write()
        {
            var instance = new TwoTypeParamInstanceFieldOnReferenceType<int, object>();
            instance.SecondField = new object();
        }

        public static void Test_Property_ReferenceType_Instance_Write()
        {
            var instance = new InstanceFieldReferenceType();
            instance.Test_Property_ReferenceType_Instance = new object();
        }

        public static void Test_Field_ValueType_Static_Read()
        {
            _ = StaticFieldValueType.Test_Field_ValueType_Static;

        }

        public static void Test_Property_ValueType_Static_Read()
        {
            _ = StaticFieldValueType.Test_Property_ValueType_Static;
        }

        public static void Test_Field_ValueType_Static_Write()
        {
            StaticFieldValueType.Test_Field_ValueType_Static = 123;

        }

        public static void Test_Property_ValueType_Static_Write()
        {
            StaticFieldValueType.Test_Property_ValueType_Static = 123;
        }

        public static void Test_Field_ReferenceType_Static_Read()
        {
            _ = StaticFieldReferenceType.Test_Field_ReferenceType_Static;
        }

        public static void Test_Property_ReferenceType_Static_Read()
        {
            _ = StaticFieldReferenceType.Test_Property_ReferenceType_Static;
        }

        public static void Test_Field_ReferenceType_Static_Write()
        {
            StaticFieldReferenceType.Test_Field_ReferenceType_Static = new object();
        }

        public static void Test_Property_ReferenceType_Static_Write()
        {
            StaticFieldReferenceType.Test_Property_ReferenceType_Static = new object();
        }

        public static void Test_Field_Volatile_ValueType_Static_Read()
        {
            _ = StaticFieldValueType.Test_Field_Volatile_ValueType_Static;
        }

        public static void Test_Field_Volatile_ValueType_Static_Write()
        {
            StaticFieldValueType.Test_Field_Volatile_ValueType_Static = 123;
        }

        public static void Test_Field_Volatile_ValueType_Instance_Read()
        {
            var instance = new InstanceFieldValueType();
            _ = instance.Test_Field_Volatile_ValueType_Instance;
        }

        public static void Test_Field_Volatile_ValueType_Instance_Write()
        {
            var instance = new InstanceFieldValueType();
            instance.Test_Field_Volatile_ValueType_Instance = 123;
        }

        public static void Test_Array_I_Read()
        {
            ArrayElement.Test_Array_I = new nuint[1];
            var cpy = ArrayElement.Test_Array_I[0];
        }

        public static void Test_Array_I_Write()
        {
            ArrayElement.Test_Array_I = new nuint[1];
            ArrayElement.Test_Array_I[0] = 123;
        }

        public static void Test_Array_I1_Read()
        {
            ArrayElement.Test_Array_I1 = new sbyte[1];
            var cpy = ArrayElement.Test_Array_I1[0];
        }

        public static void Test_Array_I1_Write()
        {
            ArrayElement.Test_Array_I1 = new sbyte[1];
            ArrayElement.Test_Array_I1[0] = 123;
        }

        public static void Test_Array_I2_Read()
        {
            ArrayElement.Test_Array_I2 = new short[1];
            var cpy = ArrayElement.Test_Array_I2[0];
        }

        public static void Test_Array_I2_Write()
        {
            ArrayElement.Test_Array_I2 = new short[1];
            ArrayElement.Test_Array_I2[0] = 123;
        }

        public static void Test_Array_I4_Read()
        {
            ArrayElement.Test_Array_I4 = new int[1];
            var cpy = ArrayElement.Test_Array_I4[0];
        }

        public static void Test_Array_I4_Write()
        {
            ArrayElement.Test_Array_I4 = new int[1];
            ArrayElement.Test_Array_I4[0] = 123;
        }

        public static void Test_Array_I8_Read()
        {
            ArrayElement.Test_Array_I8 = new long[1];
            var cpy = ArrayElement.Test_Array_I8[0];
        }

        public static void Test_Array_I8_Write()
        {
            ArrayElement.Test_Array_I8 = new long[1];
            ArrayElement.Test_Array_I8[0] = 123;
        }

        public static void Test_Array_U1_Read()
        {
            ArrayElement.Test_Array_U1 = new byte[1];
            var cpy = ArrayElement.Test_Array_U1[0];
        }

        public static void Test_Array_U1_Write()
        {
            ArrayElement.Test_Array_U1 = new byte[1];
            ArrayElement.Test_Array_U1[0] = 123;
        }

        public static void Test_Array_U2_Read()
        {
            ArrayElement.Test_Array_U2 = new ushort[1];
            var cpy = ArrayElement.Test_Array_U2[0];
        }

        public static void Test_Array_U2_Write()
        {
            ArrayElement.Test_Array_U2 = new ushort[1];
            ArrayElement.Test_Array_U2[0] = 123;
        }

        public static void Test_Array_U4_Read()
        {
            ArrayElement.Test_Array_U4 = new uint[1];
            var cpy = ArrayElement.Test_Array_U4[0];
        }

        public static void Test_Array_U4_Write()
        {
            ArrayElement.Test_Array_U4 = new uint[1];
            ArrayElement.Test_Array_U4[0] = 123;
        }

        public static void Test_Array_U8_Read()
        {
            ArrayElement.Test_Array_U8 = new ulong[1];
            var cpy = ArrayElement.Test_Array_U8[0];
        }

        public static void Test_Array_U8_Write()
        {
            ArrayElement.Test_Array_U8 = new ulong[1];
            ArrayElement.Test_Array_U8[0] = 123;
        }

        public static void Test_Array_R4_Read()
        {
            ArrayElement.Test_Array_R4 = new float[1];
            var cpy = ArrayElement.Test_Array_R4[0];
        }

        public static void Test_Array_R4_Write()
        {
            ArrayElement.Test_Array_R4 = new float[1];
            ArrayElement.Test_Array_R4[0] = 123;
        }

        public static void Test_Array_R8_Read()
        {
            ArrayElement.Test_Array_R8 = new double[1];
            var cpy = ArrayElement.Test_Array_R8[0];
        }

        public static void Test_Array_R8_Write()
        {
            ArrayElement.Test_Array_R8 = new double[1];
            ArrayElement.Test_Array_R8[0] = 123;
        }

        public static void Test_Array_Ref_Read()
        {
            ArrayElement.Test_Array_Ref = new object[1];
            var cpy = ArrayElement.Test_Array_Ref[0];
        }

        public static void Test_Array_Ref_Write()
        {
            ArrayElement.Test_Array_Ref = new object[1];
            ArrayElement.Test_Array_Ref[0] = 123;
        }

        public static void Test_Array_Struct_Read()
        {
            ArrayElement.Test_Array_Struct = new DateTime[1];
            var cpy = ArrayElement.Test_Array_Struct[0];
        }

        public static void Test_Array_Struct_Write()
        {
            ArrayElement.Test_Array_Struct = new DateTime[1];
            ArrayElement.Test_Array_Struct[0] = DateTime.UtcNow;
        }

        public static void Test_NoDeadlock()
        {
            var lockObj1 = new object();
            var lockObj2 = new object();

            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                for (;;)
                {
                    lock (lockObj1)
                    {
                        lock (lockObj2)
                        {
                        }
                    }
                }
            });

            var thread2 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread2";
                for (;;)
                {
                    lock (lockObj1)
                    {
                        lock (lockObj2)
                        {
                        }
                    }
                }
            });

            thread1.IsBackground = true;
            thread2.IsBackground = true;
            thread1.Start();
            thread2.Start();

            Thread.Sleep(2000);
        }

        public static void Test_Deadlock_SimpleDeadlock_UsingMonitor()
        {
            using var mutex = Mutex.OpenExisting("SharpDetect_E2E_Tests");
            var lockObj1 = new object();
            var lockObj2 = new object();
            var syncEvent = new AutoResetEvent(true);
            
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                for (;;)
                {
                    syncEvent.WaitOne();
                    lock (lockObj1)
                    {
                        syncEvent.Set();
                        lock (lockObj2)
                        {
                        }
                    }
                }
            });

            var thread2 = new Thread(() =>
            {
                for (;;)
                {
                    Thread.CurrentThread.Name = "TEST_Thread2";
                    syncEvent.WaitOne();
                    lock (lockObj2)
                    {
                        syncEvent.Set();
                        lock (lockObj1)
                        {
                        }
                    }
                }
            });

            thread1.IsBackground = true;
            thread2.IsBackground = true;
            thread1.Start();
            thread2.Start();
            mutex.WaitOne(_waitTimeout);
        }
        
#if NET9_0_OR_GREATER
        public static void Test_Deadlock_SimpleDeadlock_UsingLock()
        {
            using var mutex = Mutex.OpenExisting("SharpDetect_E2E_Tests");
            var lockObj1 = new Lock();
            var lockObj2 = new Lock();
            var syncEvent = new AutoResetEvent(true);
            
            var thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                for (;;)
                {
                    syncEvent.WaitOne();
                    lock (lockObj1)
                    {
                        syncEvent.Set();
                        lock (lockObj2)
                        {
                        }
                    }
                }
            });

            var thread2 = new Thread(() =>
            {
                for (;;)
                {
                    Thread.CurrentThread.Name = "TEST_Thread2";
                    syncEvent.WaitOne();
                    lock (lockObj2)
                    {
                        syncEvent.Set();
                        lock (lockObj1)
                        {
                        }
                    }
                }
            });

            thread1.IsBackground = true;
            thread2.IsBackground = true;
            thread1.Start();
            thread2.Start();
            mutex.WaitOne(_waitTimeout);
        }
#endif

        public static void Test_Deadlock_ThreadJoinDeadlock()
        {
            using var mutex = Mutex.OpenExisting("SharpDetect_E2E_Tests");
            Thread? thread1 = null;
            Thread? thread2 = null;
            var thread1Ready = new AutoResetEvent(false);
            var thread2Ready = new AutoResetEvent(false);

            thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                thread1Ready.Set();
                thread2Ready.WaitOne();
                thread2!.Join();
            });

            thread2 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread2";
                thread2Ready.Set();
                thread1Ready.WaitOne();
                thread1!.Join();
            });

            thread1.IsBackground = true;
            thread2.IsBackground = true;
            thread1.Start();
            thread2.Start();
            mutex.WaitOne(_waitTimeout);
        }

        public static void Test_Deadlock_MixedMonitorAndThreadJoinDeadlock()
        {
            using var mutex = Mutex.OpenExisting("SharpDetect_E2E_Tests");
            Thread? thread1 = null;
            Thread? thread2 = null;
            Thread? thread3 = null;
            var lockObj = new object();
            var sync1 = new AutoResetEvent(false);
            var sync2 = new AutoResetEvent(false);
            var sync3 = new AutoResetEvent(false);
            var sync4 = new AutoResetEvent(false);

            // Thread 1: Acquires lock, then waits for Thread 2 to join
            thread1 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread1";
                lock (lockObj)
                {
                    sync1.Set();
                    sync2.WaitOne();
                    thread2!.Join();
                }
            });

            // Thread 2: Waits for Thread 3 to join
            thread2 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread2";
                sync1.WaitOne();
                sync2.Set();
                // Signal that we're ready to wait for thread3
                sync4.Set();
                sync3.WaitOne();
                thread3!.Join();
            });

            // Thread 3: Tries to acquire the lock held by Thread 1
            thread3 = new Thread(() =>
            {
                Thread.CurrentThread.Name = "TEST_Thread3";
                // Wait for Thread2 to be ready before we signal sync3
                sync4.WaitOne();
                sync3.Set();
                lock (lockObj)
                {
                    // Never reached
                }
            });

            thread1.IsBackground = true;
            thread2.IsBackground = true;
            thread3.IsBackground = true;
            thread1.Start();
            thread2.Start();
            thread3.Start();
            mutex.WaitOne(_waitTimeout);
        }

        public static void Test_DataRace_ReferenceType_Static_ReadWriteRace()
        {
            DataRace.Test_DataRace_ReferenceType_Static = new object();
            var task1 = Task.Run(() => { _ = DataRace.Test_DataRace_ReferenceType_Static; });
            var task2 = Task.Run(() => { DataRace.Test_DataRace_ReferenceType_Static = new object(); });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Static_ReadWriteRace()
        {
            DataRace.Test_DataRace_ValueType_Static = 0;
            var task1 = Task.Run(() => { _ = DataRace.Test_DataRace_ValueType_Static; });
            var task2 = Task.Run(() => { DataRace.Test_DataRace_ValueType_Static = 123; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Instance_ReadWriteRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ReferenceType_Instance = new object();
            var task1 = Task.Run(() => { _ = instance.Test_DataRace_ReferenceType_Instance; });
            var task2 = Task.Run(() => { instance.Test_DataRace_ReferenceType_Instance = new object(); });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Instance_ReadWriteRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ValueType_Instance = 0;
            var task1 = Task.Run(() => { _ = instance.Test_DataRace_ValueType_Instance; });
            var task2 = Task.Run(() => { instance.Test_DataRace_ValueType_Instance = 123; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Static_WriteReadRace()
        {
            DataRace.Test_DataRace_ReferenceType_Static = new object();
            var task1 = Task.Run(() => { DataRace.Test_DataRace_ReferenceType_Static = new object(); });
            var task2 = Task.Run(() => { _ = DataRace.Test_DataRace_ReferenceType_Static; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Static_WriteReadRace()
        {
            DataRace.Test_DataRace_ValueType_Static = 0;
            var task1 = Task.Run(() => { DataRace.Test_DataRace_ValueType_Static = 123; });
            var task2 = Task.Run(() => { _ = DataRace.Test_DataRace_ValueType_Static; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Instance_WriteReadRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ReferenceType_Instance = new object();
            var task1 = Task.Run(() => { instance.Test_DataRace_ReferenceType_Instance = new object(); });
            var task2 = Task.Run(() => { _ = instance.Test_DataRace_ReferenceType_Instance; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Instance_WriteReadRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ValueType_Instance = 0;
            var task1 = Task.Run(() => { instance.Test_DataRace_ValueType_Instance = 123; });
            var task2 = Task.Run(() => { _ = instance.Test_DataRace_ValueType_Instance; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ReferenceType_Static_ReadReadNoRace()
        {
            DataRace.Test_DataRace_ReferenceType_Static = new object();
            var task1 = Task.Run(() => { _ = DataRace.Test_DataRace_ReferenceType_Static; });
            var task2 = Task.Run(() => { _ = DataRace.Test_DataRace_ReferenceType_Static; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ValueType_Static_ReadReadNoRace()
        {
            DataRace.Test_DataRace_ValueType_Static = 123; // Initialize first
            var task1 = Task.Run(() => { _ = DataRace.Test_DataRace_ValueType_Static; });
            var task2 = Task.Run(() => { _ = DataRace.Test_DataRace_ValueType_Static; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Instance_WriteWriteRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ReferenceType_Instance = new object();
            var task1 = Task.Run(() => { instance.Test_DataRace_ReferenceType_Instance = new object(); });
            var task2 = Task.Run(() => { instance.Test_DataRace_ReferenceType_Instance = new object(); });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Instance_WriteWriteRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ValueType_Instance = 0;
            var task1 = Task.Run(() => { instance.Test_DataRace_ValueType_Instance = 123; });
            var task2 = Task.Run(() => { instance.Test_DataRace_ValueType_Instance = 456; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ReferenceType_Instance_ReadReadNoRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ReferenceType_Instance = new object();
            var task1 = Task.Run(() => { _ = instance.Test_DataRace_ReferenceType_Instance; });
            var task2 = Task.Run(() => { _ = instance.Test_DataRace_ReferenceType_Instance; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ValueType_Instance_ReadReadNoRace()
        {
            var instance = new DataRace();
            instance.Test_DataRace_ValueType_Instance = 123;
            var task1 = Task.Run(() => { _ = instance.Test_DataRace_ValueType_Instance; });
            var task2 = Task.Run(() => { _ = instance.Test_DataRace_ValueType_Instance; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ThreadStatic_ReferenceType()
        {
            var task1 = Task.Run(() => { DataRace.Test_ThreadStatic_ReferenceType = new object(); });
            var task2 = Task.Run(() => { DataRace.Test_ThreadStatic_ReferenceType = new object(); });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ThreadStatic_ValueType()
        {
            var task1 = Task.Run(() => { DataRace.Test_ThreadStatic_ValueType = 123; });
            var task2 = Task.Run(() => { DataRace.Test_ThreadStatic_ValueType = 321; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ThreadStatic_ReadWrite()
        {
            DataRace.Test_ThreadStatic_ValueType = 0;
            var task1 = Task.Run(() => { _ = DataRace.Test_ThreadStatic_ValueType; });
            var task2 = Task.Run(() => { DataRace.Test_ThreadStatic_ValueType = 123; });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_VolatileField_Static_ReadWriteNoRace()
        {
            DataRace.Test_Volatile_ValueType_Static = 0;
            var task1 = Task.Run(() => { _ = DataRace.Test_Volatile_ValueType_Static; });
            var task2 = Task.Run(() => { DataRace.Test_Volatile_ValueType_Static = 123; });
            Task.WaitAll(task1, task2);
        }
        
        public static void Test_NoDataRace_VolatileField_Instance_ReadWriteNoRace()
        {
            DataRace dataRace = new DataRace();
            var task1 = Task.Run(() => { _ = dataRace.Test_Volatile_ValueType_Instance; });
            var task2 = Task.Run(() => { dataRace.Test_Volatile_ValueType_Instance = 123; });
            Task.WaitAll(task1, task2);
        }
        
        public static void Test_NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace()
        {
            DataRace.Test_Volatile_ValueType_Static = 0;
            var task1 = Task.Run(() => { Volatile.Read(ref DataRace.Test_DataRace_ValueType_Static); });
            var task2 = Task.Run(() => { Volatile.Write(ref DataRace.Test_DataRace_ValueType_Static, 123); });
            Task.WaitAll(task1, task2);
        }
        
        public static void Test_NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace()
        {
            DataRace dataRace = new DataRace();
            var task1 = Task.Run(() => { Volatile.Read(ref dataRace.Test_DataRace_ValueType_Instance); });
            var task2 = Task.Run(() => { Volatile.Write(ref dataRace.Test_DataRace_ValueType_Instance, 123); });
            Task.WaitAll(task1, task2);
        }
        
        public static void Test_NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin()
        {
            Task.Run(() => { DataRace.Test_DataRace_ValueType_Static = 42; }).Wait();
            _ = DataRace.Test_DataRace_ValueType_Static;
        }

        public static void Test_NoDataRace_Task_SequentialTasks_WriteRead()
        {
            DataRace.Test_DataRace_ValueType_Static = 0;
            Task.Run(() => { DataRace.Test_DataRace_ValueType_Static = 42; }).Wait();
            Task.Run(() => { _ = DataRace.Test_DataRace_ValueType_Static; }).Wait();
        }

        public static void Test_SemaphoreSlimMethods_WaitRelease1()
        {
            var sem = new SemaphoreSlim(1, 1);
            sem.Wait();
            sem.Release();
        }

        public static void Test_SemaphoreSlimMethods_WaitRelease2()
        {
            var sem = new SemaphoreSlim(1, 1);
            sem.Wait(CancellationToken.None);
            sem.Release();
        }

        public static void Test_SemaphoreSlimMethods_WaitRelease3()
        {
            var sem = new SemaphoreSlim(1, 1);
            sem.Wait();
            sem.Release(releaseCount: 1);
        }

        public static void Test_SemaphoreSlimMethods_TryWaitRelease1()
        {
            var sem = new SemaphoreSlim(1, 1);
            if (sem.Wait(millisecondsTimeout: Timeout.Infinite))
                sem.Release();
        }

        public static void Test_SemaphoreSlimMethods_TryWaitRelease2()
        {
            var sem = new SemaphoreSlim(1, 1);
            if (sem.Wait(timeout: TimeSpan.FromMilliseconds(Timeout.Infinite)))
                sem.Release();
        }

        public static void Test_SemaphoreSlimMethods_TryWaitRelease3()
        {
            var sem = new SemaphoreSlim(1, 1);
            if (sem.Wait(millisecondsTimeout: Timeout.Infinite, cancellationToken: CancellationToken.None))
                sem.Release();
        }

        public static void Test_SemaphoreSlimMethods_TryWaitRelease4()
        {
            var sem = new SemaphoreSlim(1, 1);
            if (sem.Wait(timeout: TimeSpan.FromMilliseconds(Timeout.Infinite), cancellationToken: CancellationToken.None))
                sem.Release();
        }

        public static void Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead()
        {
            var sem = new SemaphoreSlim(1, 1);
            var task1 = Task.Run(() =>
            {
                sem.Wait();
                DataRace.Test_DataRace_ValueType_Static = 42;
                sem.Release();
            });
            var task2 = Task.Run(() =>
            {
                sem.Wait();
                _ = DataRace.Test_DataRace_ValueType_Static;
                sem.Release();
            });
            Task.WaitAll(task1, task2);
        }

        public static void Test_SingleGarbageCollection_ObjectTracking_Simple()
        {
            // Generate garbage
            for (var i = 0; i < 1000; i++)
                new object();

            // Create tracked object
            var lockObj = new object();
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }

            // Perform compacting GC (lockObj should be moved)
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Access the same object again
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }
        }

        public static void Test_MultipleGarbageCollection_ObjectTracking_Simple()
        {
            // Generate garbage
            var garbage = new object();
            for (var i = 0; i < 1000; i++)
                new object();

            // Create tracked object
            var lockObj = new object();
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }

            // Perform compacting GC (lockObj should be moved)
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Access the same object again
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }

            // Generate garbage and release all
            for (var i = 0; i < 1000; i++)
                new object();
            garbage = null;

            // Perform another compacting GC (lockObj should be moved again)
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Access the same object again
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }
        }

        public static void Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject()
        {
            // Generate garbage
            for (var i = 0; i < 1000; i++)
                new object();

            // Create tracked object
            var lockObj = new object();
            lock (lockObj)
            {
                // Create some more garbage
                new object();
                new object();
            }

            bool lockTaken = false;
            Monitor.Enter(lockObj, ref lockTaken);

            // Perform compacting GC (lockObj should be moved)
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            if (lockTaken)
                Monitor.Exit(lockObj);

            // Create some more garbage
            new object();
            new object();
        }

        public static void Test_NonDisposedAnalysis_CustomDisposable_NonDisposed()
        {
            var disposable = new Disposables.CustomDisposable();
        }

        public static void Test_NonDisposedAnalysis_CustomDisposable_Disposed()
        {
            var disposable = new Disposables.CustomDisposable();
            disposable.Dispose();
        }
        
        public static void Test_MultiProcess_ChildExitsBeforeParent()
        {
            var hostPath = Environment.ProcessPath!;
            var assemblyPath = Environment.GetCommandLineArgs()[0];
            var child = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = hostPath,
                Arguments = $"{assemblyPath} {nameof(Test_MultiProcess_Child_LockAndExit)}",
                UseShellExecute = false
            })!;
            lock (new object()) { }
            child.WaitForExit(timeout: TimeSpan.FromSeconds(3));
        }
        
        public static void Test_MultiProcess_Child_LockAndExit()
        {
            lock (new object()) { }
        }
    }
}

