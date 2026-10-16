// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The exception that is thrown when a job step cannot copy its state for its work because no
/// <see cref="global::Orleans.Serialization.DeepCopier{T}"/> is registered for the state type.
/// </summary>
/// <param name="stateType">The state type without a copier.</param>
/// <param name="innerException">The failure resolving the copier, if any.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Never caught by callers: the step reports it to its job as a failed outcome, and it adds nothing to the public API.")]
internal sealed class MissingStateCopier(Type stateType, Exception? innerException)
    : Exception(
        $"No DeepCopier<{stateType.FullName}> is registered. A job step hands its work a copy of its state. Orleans registers DeepCopier<T> with AddSerializer, which every silo does; a host without it, such as an Orleans.TestKit silo, must register DeepCopier<{stateType.Name}> itself.",
        innerException);
