# KnightwareCore

A .NET Standard 2.0 library providing helpful utility classes for threading, networking, collections, and other common patterns frequently used across projects.

[![CI](https://github.com/dsmithson/KnightwareCore/actions/workflows/ci.yml/badge.svg)](https://github.com/dsmithson/KnightwareCore/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dsmithson_KnightwareCore&metric=alert_status)](https://sonarcloud.io/dashboard?id=dsmithson_KnightwareCore)
[![codecov](https://codecov.io/gh/dsmithson/KnightwareCore/branch/master/graph/badge.svg)](https://codecov.io/gh/dsmithson/KnightwareCore)
[![NuGet](https://img.shields.io/nuget/v/KnightwareCore.svg)](https://www.nuget.org/packages/KnightwareCore/)

## Installation

```bash
dotnet add package KnightwareCore
```

## Features

### Threading & Async Primitives (`Knightware.Threading` / `Knightware.Threading.Tasks`)

| Class | Description |
|-------|-------------|
| `AsyncLock` | A reentrant async-compatible lock, allowing `await` inside critical sections |
| `AsyncAutoResetEvent` | Async-compatible auto-reset event for signaling between tasks |
| `AsyncSemaphore` | Async-compatible semaphore for limiting concurrent access |
| `AsyncListProcessor<T>` | Thread-safe queue that processes items sequentially with configurable parallelism |
| `AutoResetWorker` | Background worker triggered by an auto-reset event with optional periodic execution |
| `BatchProcessor<TRequest, TResponse>` | Batches incoming requests and processes them based on time/count thresholds |
| `ResourcePool<T>` | Generic connection/resource pool with automatic scaling based on demand |
| `Dispatcher` | `SynchronizationContext`-aware dispatcher for marshaling calls to a specific context |

**Example: AsyncLock**
```csharp
private readonly AsyncLock _lock = new AsyncLock();

public async Task DoWorkAsync()
{
    using (await _lock.LockAsync())
    {
        // Thread-safe async work here
        await SomeAsyncOperation();
    }
}
```

**Example: BatchProcessor**
```csharp
var batchProcessor = new BatchProcessor<Request, Response>();
await batchProcessor.StartupAsync(
    async batch => {
        // Process all requests in the batch at once
        foreach (var item in batch)
            item.SetResponse(await ProcessAsync(item.Request));
    },
    minimumTimeInterval: TimeSpan.FromMilliseconds(100),
    maximumTimeInterval: TimeSpan.FromSeconds(1),
    maximumCount: 50
);
```

### Collections (`Knightware.Collections`)

| Class | Description |
|-------|-------------|
| `NotifyingObservableCollection<T>` | `ObservableCollection` that raises events when item properties change (via `INotifyPropertyChanged`) |
| `CompositeCollection` | Combines multiple collections into a single virtual collection with change notification |
| `Grouping<TKey, TElement>` | Simple `IGrouping<TKey, TElement>` implementation |
| `ListExtensions` | Extension methods for list manipulation |

**Example: NotifyingObservableCollection**
```csharp
var collection = new NotifyingObservableCollection<MyItem>();
collection.CollectionItemChanged += (sender, e) => {
    Console.WriteLine($"Item at index {e.Index} property '{e.PropertyName}' changed");
};
```

### Networking (`Knightware.Net`)

| Class | Description |
|-------|-------------|
| `TCPSocket` | Async TCP client wrapper with simple startup/shutdown lifecycle |
| `UDPSocket` | Async UDP socket for sending/receiving datagrams |
| `UDPMulticastListener` | Listens for UDP multicast traffic on a specified group |

**Example: TCPSocket**
```csharp
var socket = new TCPSocket();
if (await socket.StartupAsync("192.168.1.100", 5000))
{
    await socket.WriteAsync(data, 0, data.Length);
    int bytesRead = await socket.ReadAsync(buffer, 0, buffer.Length);
}
await socket.ShutdownAsync();
```

### Diagnostics (`Knightware.Diagnostics`)

| Class | Description |
|-------|-------------|
| `TraceQueue` | Static trace/logging queue with configurable tracing levels and async processing |
| `TraceMessage` | Represents a single trace message with timestamp, level, and content |
| `TracingLevel` | Enum defining trace levels (Success, Warning, Error, etc.) |

**Example: TraceQueue**
```csharp
TraceQueue.TracingLevel = TracingLevel.Warning;
TraceQueue.TraceMessageRaised += msg => Console.WriteLine($"[{msg.Level}] {msg.Message}");
TraceQueue.Trace(this, TracingLevel.Warning, "Something happened: {0}", details);
```

### Primitives (`Knightware.Primitives`)

Platform-independent primitive types useful in cross-platform scenarios:

| Struct | Description |
|--------|-------------|
| `Color` | ARGB color with parsing and equality support |
| `Point` | 2D point (X, Y) |
| `Size` | Width and Height |
| `Rectangle` | Position and size combined |
| `Thickness` | Four-sided thickness (Left, Top, Right, Bottom) |

### IO (`Knightware.IO`)

| Class | Description |
|-------|-------------|
| `GZipStreamDecompressor` | Decompresses GZip streams |
| `XmlDeserializer` | Helper for deserializing XML content |

### Base Classes (`Knightware`)

| Class | Description |
|-------|-------------|
| `PropertyChangedBase` | Base class implementing `INotifyPropertyChanged` with `[CallerMemberName]` support |
| `DispatcherPropertyChangedBase` | `PropertyChangedBase` with automatic dispatcher marshaling for UI binding |
| `TimedCacheWeakReference<T>` | Weak reference that maintains a strong reference for a configurable duration |

**Example: PropertyChangedBase**
```csharp
public class MyViewModel : PropertyChangedBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
