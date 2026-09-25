// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Stages;

/// <summary>
/// Defines what a job does after a stage in which one or more steps failed.
/// </summary>
public enum JobStageFailureBehavior
{
    /// <summary>
    /// The stage is a barrier: the job stops there, and the steps in every later stage are recorded as unreachable
    /// instead of being started. This is the default, because a later stage usually depends on an earlier one
    /// having actually run.
    /// </summary>
    StopJob = 0,

    /// <summary>
    /// The stage is best-effort: its failures are recorded and the next stage starts anyway.
    /// </summary>
    ContinueWithNextStage = 1,
}
