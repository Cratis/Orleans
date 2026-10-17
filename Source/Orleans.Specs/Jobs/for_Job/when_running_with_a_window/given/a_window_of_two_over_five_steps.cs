// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window.given;

public class a_window_of_two_over_five_steps : for_Job.given.a_running_job_with_steps
{
    protected override int NumberOfSteps => 5;

    protected override int? Window => 2;
}
