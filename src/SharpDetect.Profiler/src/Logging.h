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

#ifndef LOGGING_HEADER_GUARD
#define LOGGING_HEADER_GUARD

#include "EasyLogging.h"

#define LOG_ERROR_AND_RET_IF(expr, logger, message) \
if (expr) { logger->error(message); return hr; } \
else (void)0

#define LOG_ERROR_IF(expr, logger, message) \
if (expr) logger->error(message); \
else (void)0

#define LOG_INFO(logger, message) logger->info(message)

#endif