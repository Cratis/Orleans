// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobsManager.when_cleaning_up_terminal_jobs;

public class with_more_jobs_than_the_cleanup_page : given.the_manager
{
    static readonly JobStatus[] _terminalStatuses = [JobStatus.CompletedSuccessfully, JobStatus.CompletedWithFailures, JobStatus.Failed];
    readonly JobId _oldestTerminalJob = Guid.Parse("83f7c6f3-66d7-4053-8fd8-c4b0ce85d6a1");
    readonly JobId _secondOldestTerminalJob = Guid.Parse("5e249433-28aa-45e9-a4fc-df6f2398b823");
    readonly JobId _thirdOldestTerminalJob = Guid.Parse("e421e562-d6af-4af0-8cfb-04ef8f40ac2a");
    readonly JobId _runningJob = Guid.Parse("8e2f16b1-4e81-474d-97b9-4b819bc9ff16");
    readonly JobId _preparingJob = Guid.Parse("8e2eb188-0f46-48eb-ae71-3450bd3ea502");
    readonly JobId _removingJob = Guid.Parse("0e4b5978-9f83-4415-aeaa-4c4bd77bffcf");
    readonly JobId _recentTerminalJob = Guid.Parse("de725a1a-6006-4628-91f5-85b9b2668999");

    protected override JobsOptions CreateOptions() => new()
    {
        TerminalJobRetention = TimeSpan.FromHours(1),
        MaxTerminalJobsPerCleanup = 2
    };

    void Establish()
    {
        AddStoredJob(_oldestTerminalJob, JobStatus.CompletedWithFailures, DateTimeOffset.UtcNow.AddHours(-4));
        AddStoredJob(_secondOldestTerminalJob, JobStatus.Failed, DateTimeOffset.UtcNow.AddHours(-3));
        AddStoredJob(_thirdOldestTerminalJob, JobStatus.CompletedSuccessfully, DateTimeOffset.UtcNow.AddHours(-2));
        AddStoredJob(_runningJob, JobStatus.Running, DateTimeOffset.UtcNow.AddHours(-4));
        AddStoredJob(_preparingJob, JobStatus.PreparingSteps, DateTimeOffset.UtcNow);
        AddStoredJob(_removingJob, JobStatus.Removing, DateTimeOffset.UtcNow.AddHours(-4));
        AddStoredJob(_recentTerminalJob, JobStatus.Failed, DateTimeOffset.UtcNow.AddMinutes(-30));

        _jobStepStorage.RemoveAllForJob(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success()));
        _jobStorage.RemoveTerminal(Arg.Any<JobId>()).Returns(Task.FromResult(Catch.Success(true)));
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] void should_select_a_bounded_oldest_first_terminal_page() => _jobStorage.Received(1).GetJobs(Arg.Is<JobQuery>(query => MatchesTerminalQuery(query)));

    static bool MatchesTerminalQuery(JobQuery query) =>
        query.Statuses.SequenceEqual(_terminalStatuses) &&
        query.CreatedBefore.HasValue &&
        query.Take == 2;

    [Fact] void should_remove_steps_for_the_oldest_terminal_job() => _jobStepStorage.Received(1).RemoveAllForJob(_oldestTerminalJob);
    [Fact] void should_remove_steps_for_the_second_oldest_terminal_job() => _jobStepStorage.Received(1).RemoveAllForJob(_secondOldestTerminalJob);
    [Fact] void should_remove_the_oldest_terminal_job_conditionally() => _jobStorage.Received(1).RemoveTerminal(_oldestTerminalJob);
    [Fact] void should_remove_the_second_oldest_terminal_job_conditionally() => _jobStorage.Received(1).RemoveTerminal(_secondOldestTerminalJob);
    [Fact] void should_not_remove_a_terminal_job_outside_the_page() => _jobStorage.DidNotReceive().RemoveTerminal(_thirdOldestTerminalJob);
    [Fact] void should_not_remove_a_recent_terminal_job() => _jobStorage.DidNotReceive().RemoveTerminal(_recentTerminalJob);
    [Fact] void should_not_remove_a_running_job() => _jobStorage.DidNotReceive().RemoveTerminal(_runningJob);
    [Fact] void should_not_remove_a_preparing_job() => _jobStorage.DidNotReceive().RemoveTerminal(_preparingJob);
    [Fact] void should_not_remove_a_removing_job() => _jobStorage.DidNotReceive().RemoveTerminal(_removingJob);

    void AddStoredJob(JobId id, JobStatus status, DateTimeOffset created) => _storedJobs.Add(new JobState
    {
        Id = id,
        Status = status,
        Created = created
    });
}
