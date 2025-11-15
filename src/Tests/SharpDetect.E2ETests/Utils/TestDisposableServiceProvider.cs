// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Utils;

internal sealed class TestDisposableServiceProvider : IServiceProvider, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;
    
    public TestDisposableServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public object? GetService(Type serviceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _serviceProvider.GetService(serviceType);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}