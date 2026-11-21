# Limitations / Known Issues

## Data Race Detection

### Races That Are Not Reported

- Accesses to array elements, to fields of a struct stored inside an object (`holder.Point.X`), and through `ref` parameters are not analyzed.
- A value-type field used through a method or property call on the field itself is not analyzed, because the call receives the field's address instead of its value. Examples: `h.Count.ToString()`.
- Assemblies whose names start with `System.` or `Microsoft.` are not analyzed by default, so races inside collections such as `List<T>` are missed. See [`--instrument-system-libraries`](running-analysis-against-executables.md#instrumenting-system-libraries).
- Code generated at run time (`System.Reflection.Emit`) is not analyzed.
- Tasks that ran one after another on the same thread-pool thread are treated as ordered.
- Constructor writes can be missed when the constructor publishes `this` to another thread before writing its fields.
- Plain reads racing with `Interlocked` or `Volatile` writes are reported only with `ReportPlainReadsRacingWithAtomicWrites` in a [configuration file](running-analysis-with-configuration-file.md#data-race-plugin-options).

### Reports That May Not Be Real Races

Accesses ordered only by synchronization that is not [supported](supported-primitives.md) are reported as races. Common examples:
- `SpinLock`, `LazyInitializer`, named wait handles, and `[MethodImpl(MethodImplOptions.Synchronized)]` before .NET 10
- `Interlocked` and `Volatile` calls on anything other than a field of a class, such as an array element, a field of a struct or a `ref` parameter
- Value types passed through a concurrent collection, since publication is tracked by object identity

### Overhead

Every analyzed field access, synchronization operation and thread or task action emits an event, and the target runs without JIT optimizations and inlining, so programs that do a lot of these slow down significantly.
Stack-trace collection adds to this; pass `--no-stack-traces` to report a single frame instead.
Memory use also grows with the number of thread-pool and native threads that exit during a long run.

## Deadlock Detection

Deadlocks involving synchronization not listed in [Supported synchronization primitives](supported-primitives.md#deadlock-detection) are not detected.
