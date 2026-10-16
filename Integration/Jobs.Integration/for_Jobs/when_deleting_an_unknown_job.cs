// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_Jobs;

[Collection(JobsClusterCollection.Name)]
public class when_deleting_an_unknown_job : given.an_unknown_job
{
    DeleteJobResult _result;
    async Task Because() => _result = await _jobs.Delete(_key, _id);

    [Fact] void should_report_the_unknown_type() => _result.ShouldEqual(new DeleteJobResult.UnknownJobType(_id, _type));
    [Fact] async Task should_leave_the_stored_state_unchanged() => (await ReadStored()).ShouldEqual(_original);
}
