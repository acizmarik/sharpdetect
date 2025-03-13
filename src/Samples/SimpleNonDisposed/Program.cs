// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

Method();

void Method()
{
    using var ms = new StreamReader(new MemoryStream());
}

class CustomDisposable : IDisposable
{
    public void Dispose()
    {

    }
}