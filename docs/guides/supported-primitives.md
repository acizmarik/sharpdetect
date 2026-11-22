# Supported Synchronization Primitives

This page lists what each analysis plugin understands. Anything not listed here is not treated as synchronization; see [Limitations](limitations.md).

## Data Race Detection — FastTrack

Two accesses to the same field race when at least one of them is a write and nothing listed below orders them.

- **Locks**: `lock` / `Monitor` (including `Wait` and `Pulse`), `Lock`, `ReaderWriterLockSlim`, and `[MethodImpl(MethodImplOptions.Synchronized)]` on .NET 10
- **Semaphores and wait handles**: `SemaphoreSlim` (including `WaitAsync`), and unnamed `Semaphore`, `Mutex`, `AutoResetEvent` and `ManualResetEvent`, including `WaitHandle.WaitAll` / `WaitAny`
- **Signals**: `ManualResetEventSlim`, `CountdownEvent`, `Barrier`, `CancellationTokenSource`
- **Threads and tasks**: thread start and join, `ThreadPool.QueueUserWorkItem`, starting and waiting for tasks (`Task.Run`, `Wait`, `Result`, `await`, `ContinueWith`, `WhenAll`, `WaitAll`), `async` methods, `TaskCompletionSource<T>`, and `Parallel.For` / `ForEach` / `Invoke`
- **Value publication**: an object handed over through `Lazy<T>`, `ConcurrentDictionary`, `ConcurrentQueue`, `ConcurrentStack`, `ConcurrentBag` or an unbounded channel is ordered with the code that published it
- **Type initialization**: accesses to static fields are ordered after the type's static constructor

The analysis covers direct reads and writes of static and instance fields.
Fields accessed through `volatile`, `Volatile` or `Interlocked` never race with each other, but they do race with unordered plain writes of the same field.

## Deadlock Detection

A deadlock is reported when threads wait for each other in a cycle through `lock` / `Monitor`, `Lock`, `ReaderWriterLockSlim` write locks or `Thread.Join`.
