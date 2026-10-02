// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_rehydrating_after_the_host_has_stopped : given.a_controlled_backlog
{
    Exception _error;

    Task Establish() => _applicationStopping.CancelAsync();

    async Task Because() => _error = await Catch.Exception(_manager.Rehydrate);

    [Fact] void should_cancel_the_drain() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_query_job_storage() => _jobStorage.DidNotReceive().GetJobs(Arg.Any<JobStatus[]>());
    [Fact] void should_not_resume_any_jobs() => _resumed.ShouldBeEmpty();
}
