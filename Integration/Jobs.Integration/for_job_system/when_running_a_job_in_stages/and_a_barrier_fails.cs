// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system.when_running_a_job_in_stages;

[Collection(JobsClusterCollection.Name)]
public class and_a_barrier_fails : given.a_staged_job
{
    Task Because() => Run(["stages-barrier-first"], [IntegrationJobStep.FailingItem, "stages-barrier-second"], ["stages-barrier-third-a", "stages-barrier-third-b"]);

    [Fact] void should_complete_the_job_with_failures() => _jobState!.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_not_perform_the_first_step_behind_the_barrier() => IntegrationJobStepTimeline.For("stages-barrier-third-a").ShouldBeNull();
    [Fact] void should_not_perform_the_second_step_behind_the_barrier() => IntegrationJobStepTimeline.For("stages-barrier-third-b").ShouldBeNull();
    [Fact] void should_count_the_steps_behind_the_barrier_as_unreachable() => _jobState!.Progress.UnreachableSteps.ShouldEqual(2);
    [Fact] void should_record_the_steps_behind_the_barrier_as_unreachable() => _jobSteps.Count(_ => _.Status == JobStepStatus.Unreachable).ShouldEqual(2);
    [Fact] void should_keep_the_stage_of_a_step() => _jobSteps.Where(_ => _.Status == JobStepStatus.Unreachable).All(_ => _.Stage.Value == 2).ShouldBeTrue();
}
