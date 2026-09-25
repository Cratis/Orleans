// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_job_system.when_running_a_job_in_stages;

/// <summary>
/// Ordering has to be observed where it matters - in steps that really run in parallel on a real silo.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class and_every_stage_succeeds : given.a_staged_job
{
    Task Because() => Run(["stages-succeed-first"], ["stages-succeed-second-a", "stages-succeed-second-b"], ["stages-succeed-third"]);

    static IntegrationJobStepPerformed Performed(string item) => IntegrationJobStepTimeline.For(item) ?? throw new Exception($"{item} was never performed");

    [Fact] void should_complete_the_job_successfully() => _jobState!.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_start_the_second_stage_after_the_first_finished() =>
        Performed("stages-succeed-second-a").Started.ShouldBeGreaterThanOrEqual(Performed("stages-succeed-first").Finished);
    [Fact] void should_start_the_third_stage_after_the_first_step_of_the_second_finished() =>
        Performed("stages-succeed-third").Started.ShouldBeGreaterThanOrEqual(Performed("stages-succeed-second-a").Finished);
    [Fact] void should_start_the_third_stage_after_the_second_step_of_the_second_finished() =>
        Performed("stages-succeed-third").Started.ShouldBeGreaterThanOrEqual(Performed("stages-succeed-second-b").Finished);
    [Fact] void should_count_every_step_as_successful() => _jobState!.Progress.SuccessfulSteps.ShouldEqual(4);
}
