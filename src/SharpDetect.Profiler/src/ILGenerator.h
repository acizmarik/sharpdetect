/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

 // Based on project microsoftarchive/clrprofiler/ILRewrite
 // Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
 // Copyright (c) .NET Foundation and contributors. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef ILGENERATOR_HEADER_GUARD
#define ILGENERATOR_HEADER_GUARD

#include "ILDefines.h"
#include "TinyMethodUser.h"

HRESULT GenerateTinyMethod(ModuleMetadata& metadata, ILInstr* instructions, ULONG instructionsCount, LPCBYTE& code);

#endif