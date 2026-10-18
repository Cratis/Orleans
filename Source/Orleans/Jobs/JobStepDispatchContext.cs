// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Describes initialization before the job dispatches steps.
/// </summary>
/// <param name="InitializationMayBeIncomplete">
/// True when recovering preparation or startup interrupted before Running was durable.
/// False on a clean first start and on an ordinary resume of a running or stopped job.
/// </param>
public record JobStepDispatchContext(bool InitializationMayBeIncomplete);
