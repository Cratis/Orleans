// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using Microsoft.Extensions.DependencyInjection;
using context = Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_is_stopped_while_its_step_reports_a_failure.context;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.when_starting.job_with_single_step;

/// <summary>
/// A step reports a failure to its job through a request of its own, and that request waits for the job. If the
/// job is stopping at the same moment and waits for the step to acknowledge the stop, each waits for the other
/// until the grain call times out - which held up everything waiting on the stop, such as an observer
/// unsubscribing during a reconnect.
/// </summary>
/// <param name="context">The context of the scenario.</param>
[Collection(JobsClusterCollection.Name)]
public class and_job_is_stopped_while_its_step_reports_a_failure(context context) : Given<context>(context)
{
    public class context : a_jobs_manager
    {
        public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Grain messages within one in-process silo are delivered in well under a millisecond; this only has to
        /// be long enough for a queued call to reach the job before the next one is sent.
        /// </summary>
        static readonly TimeSpan _queueing = TimeSpan.FromMilliseconds(500);

        public JobId JobId;
        public bool StopCompletedWithinDeadline;
        public JobState JobState;
        public IImmutableList<JobStepState> JobSteps = [];

        async Task Because()
        {
            var hold = Fixture.Services.GetRequiredService<JobTurnHold>();
            hold.Reset();

            var stepMayFail = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobStepProcessor.SetStartTask(stepMayFail.Task);
            JobStepProcessor.SetNumJobStepsToComplete(1);

            var startResult = await JobsManager.Start<IJobWithHeldTurn, JobWithSingleStepRequest>(new(KeepAfterCompleted: true, ShouldFail: true));
            JobId = startResult.AsT0;
            await JobStepProcessor.WaitForAllPreparedStepsToBeStarted(Deadline);

            var job = hold.Job!;

            // Keep the job busy so the order of the calls queued behind it is ours to decide: first the stop,
            // then the step's failure report.
            var held = job.HoldTurn();
            await hold.Holding.WaitAsync(Deadline);
            var stop = job.Stop();
            await Task.Delay(_queueing);

            stepMayFail.SetResult();
            await JobStepProcessor.WaitForStepsToBeCompleted(Deadline);
            await Task.Delay(_queueing);
            hold.Release();
            await held.WaitAsync(Deadline);
            StopCompletedWithinDeadline = await Task.WhenAny(stop, Task.Delay(Deadline)) == stop;

            try
            {
                await JobStorage.WaitTillJobMeetsPredicate(JobId, state => state.Progress.FailedSteps == 1, Deadline);
                await JobStepStorage.WaitTillJobStepsMeetPredicate(JobId, step => step.Status == JobStepStatus.CompletedWithFailure, Deadline);
            }
            catch (TimeoutException)
            {
                // Each outcome is asserted on its own, against what was persisted when the wait gave up.
            }

            JobState = await JobStorage.TryGetJob(JobId);
            JobSteps = await JobStepStorage.GetJobSteps(JobId);
        }
    }

    [Fact]
    public void should_stop_without_waiting_for_the_step() => Context.StopCompletedWithinDeadline.ShouldBeTrue();

    [Fact]
    public void should_leave_the_job_stopped() => Context.JobState.Status.ShouldEqual(JobStatus.Stopped);

    [Fact]
    public void should_record_the_failure_the_step_reported() => Context.JobState.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_persist_the_step_as_completed_with_failure() => Context.JobSteps.Single().Status.ShouldEqual(JobStepStatus.CompletedWithFailure);
}
