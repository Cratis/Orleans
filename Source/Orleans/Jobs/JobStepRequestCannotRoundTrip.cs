// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The exception that is thrown when a step request cannot be saved and restored for recovery.
/// </summary>
/// <param name="stepId">The step whose request cannot round-trip.</param>
/// <param name="innerException">The serialization failure, if any.</param>
public class JobStepRequestCannotRoundTrip(JobStepId stepId, Exception? innerException = null)
    : Exception($"The request for step '{stepId}' cannot round-trip with the job's JSON serializer options.", innerException);
