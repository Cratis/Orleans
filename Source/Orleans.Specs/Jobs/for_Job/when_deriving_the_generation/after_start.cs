// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_deriving_the_generation;

public class after_start : given.a_job_whose_steps_report_the_generation_they_are_started_in
{
    Task Because() => _job.Start(new());

    [Fact] void should_start_the_step_in_the_first_generation() => _startedIn.ShouldContainOnly(1L);
}
