// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the question an <see cref="IJobAdmission"/> is asked about a job.
/// </summary>
/// <remarks>
/// Only handed to implementations running in the same silo as the job, so it is never serialized.
/// </remarks>
/// <param name="JobKey">The <see cref="JobKey"/> of the job, naming its scope and namespace.</param>
/// <param name="JobId">The <see cref="JobId"/> of the job.</param>
/// <param name="JobType">The <see cref="JobType"/> of the job.</param>
/// <param name="Request">The <see cref="IJobRequest"/> the job was started with.</param>
public sealed record JobAdmissionRequest(JobKey JobKey, JobId JobId, JobType JobType, IJobRequest Request);
