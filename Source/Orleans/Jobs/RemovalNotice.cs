// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// A removal notice owed to a step's current run.
/// </summary>
/// <param name="StepId">The <see cref="JobStepId"/> of the step.</param>
/// <param name="Observer">The <see cref="IJobObserver"/> of the step's current run.</param>
/// <param name="Generation">The execution generation the job is removed in.</param>
internal sealed record RemovalNotice(JobStepId StepId, IJobObserver Observer, long Generation);
