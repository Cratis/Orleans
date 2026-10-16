// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStorage.given;

public class jobs_in_storage : Specification
{
    protected JobStorage _storage;
    protected readonly JobId _id = JobId.New();
    protected readonly JobType _type = new("known-job");

    void Establish() => _storage = new(Substitute.For<IJobTypes>());
    void Destroy() => _storage.Dispose();
}
