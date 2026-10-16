// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_Jobs;

[Collection(JobsClusterCollection.Name)]
public class when_stopping_an_unknown_job : given.an_unknown_job
{
    StopJobResult _result;
    async Task Because() => _result = await _jobs.Stop(_key, _id);

    [Fact] void should_report_the_unknown_type() => _result.ShouldEqual(new StopJobResult.UnknownJobType(_id, _type));
    [Fact] void should_read_metadata_despite_the_unreadable_request() => _storedType.AsT0.ShouldEqual(_type);
    [Fact] void should_fail_a_full_read() => _fullRead.TryGetException(out _).ShouldBeTrue();
    [Fact] async Task should_leave_the_stored_state_unchanged() => (await ReadStored()).ShouldEqual(_original);
}
