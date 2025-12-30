// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using OperationResult;

namespace SharpDetect.Core.Loader;

public interface IAssemblyLoadContext
{
    Result<AssemblyDef, AssemblyLoadErrorType> LoadAssemblyFromPath(string path);
    Result<AssemblyDef, AssemblyLoadErrorType> LoadAssemblyFromStream(Stream stream, string virtualPath);
}
