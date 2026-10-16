// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.given;

public class a_legacy_store : Specification
{
    protected IJobStorage _original;
    protected IJobStorage _legacy;
    protected readonly JobId _id = JobId.New();

    void Establish()
    {
        _original = Substitute.For<IJobStorage>();
        _legacy = new legacy_storage(_original);
    }
}
