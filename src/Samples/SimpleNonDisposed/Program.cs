// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

Method();

void Method()
{
    var disposable = new CustomDisposable();
}

class CustomDisposable : IDisposable
{
    public void Dispose()
    {

    }
}