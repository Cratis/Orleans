// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping_and_draining;

public class with_an_unknown_type : given.jobs
{
    DrainJobResult _result;

    void Establish()
    {
        _types.GetClrTypeFor(_type).Returns(Result.Failed<Type, IJobTypes.GetClrTypeForError>(IJobTypes.GetClrTypeForError.CouldNotFindType));
        _factory.ClearReceivedCalls();
    }

    async Task Because() => _result = await _jobs.StopAndDrain(_key, _id, TimeSpan.Zero);

    [Fact] void should_report_the_stored_type() => _result.ShouldEqual(new DrainJobResult.UnknownJobType(_id, _type));
    [Fact] void should_not_resolve_a_grain() => _factory.ReceivedCalls().ShouldBeEmpty();
}
