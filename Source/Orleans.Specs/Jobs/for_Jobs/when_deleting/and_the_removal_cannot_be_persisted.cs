// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Jobs.when_deleting;

public class and_the_removal_cannot_be_persisted : given.jobs
{
    DeleteJobResult _result;

    void Establish() => _job.Remove().Returns(Task.FromResult(Result.Failed<RemoveJobError>(RemoveJobError.CouldNotPersistRemoval)));

    async Task Because() => _result = await _jobs.Delete(_key, _id);

    [Fact] void should_preserve_the_persistence_refusal() => _result.ShouldEqual(new DeleteJobResult.Refused(_id, RemoveJobError.CouldNotPersistRemoval));
    [Fact] void should_not_report_deleted() => _result.IsDeleted.ShouldBeFalse();
}
