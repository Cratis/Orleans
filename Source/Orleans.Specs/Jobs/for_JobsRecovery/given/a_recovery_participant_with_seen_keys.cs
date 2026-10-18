// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobsRecovery.given;

public class a_recovery_participant_with_seen_keys : a_recovery_participant
{
    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        var seen = new JobsManagerKeys();
        var remember = typeof(JobsManagerKeys).GetMethod("Remember", BindingFlags.Instance | BindingFlags.NonPublic)!;
        remember.Invoke(seen, [new JobsManagerKey("scope", "first")]);
        remember.Invoke(seen, [new JobsManagerKey("scope", "second")]);
        return new JobsManagerKeyProviders([(typeof(JobsManagerKeys), () => seen)], NullLogger<JobsRecovery>.Instance);
    }
}
