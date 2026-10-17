// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_the_second_of_two_admissions_refuses : given.the_job
{
    List<string> _order;
    given.SomeAdmission _first;
    given.SomeAdmission _second;
    given.SomeAdmission _third;
    Result<StartJobError> _result;

    void Establish()
    {
        _order = [];
        _first = new() { Answer = () => Answered("first", true) };
        _second = new() { Answer = () => Answered("second", false) };
        _third = given.SomeAdmission.Admitting();
        Admit(_first, _second, _third);
    }

    Task<bool> Answered(string name, bool answer)
    {
        _order.Add(name);
        return Task.FromResult(answer);
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_not_admitted() => ((StartJobError)_result).ShouldEqual(StartJobError.NotAdmitted);
    [Fact] void should_ask_both_in_registration_order() => _order.ShouldEqual(["first", "second"]);
    [Fact] void should_not_ask_the_one_after_the_refusal() => _third.Asked.Count.ShouldEqual(0);
    [Fact] void should_write_nothing() => Writes.ShouldEqual(0);
}
