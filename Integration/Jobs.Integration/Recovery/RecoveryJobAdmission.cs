// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Recovery;

// Admits every job unless a scenario asks it to refuse, and signals each refusal.
public sealed class RecoveryJobAdmission(RecoverySignals signals) : IJobAdmission
{
    public Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken)
    {
        if (!signals.RefuseAdmission)
        {
            return Task.FromResult(true);
        }

        signals.AdmissionRefusals.Release();
        return Task.FromResult(false);
    }
}
