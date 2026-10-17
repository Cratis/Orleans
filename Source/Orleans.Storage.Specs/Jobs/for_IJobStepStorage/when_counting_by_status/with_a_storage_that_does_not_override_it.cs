// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStepStorage.when_counting_by_status;

public class with_a_storage_that_does_not_override_it : Specification
{
    IJobStepStorage _storage;
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    void Establish()
    {
        var storage = Substitute.For<IJobStepStorage>();
        storage.GetForJob(Arg.Any<JobId>()).Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(new JobStepState[]
        {
            new() { Status = JobStepStatus.Running },
            new() { Status = JobStepStatus.Running },
            new() { Status = JobStepStatus.CompletedWithFailure }
        }.ToImmutableList())));
        _storage = new StorageWithoutCountByStatus(storage);
    }

    async Task Because() => _result = await _storage.CountByStatus(JobId.New());

    [Fact] void should_group_the_steps_it_reads() => _result.AsT0.ShouldEqual(new Dictionary<JobStepStatus, int> { [JobStepStatus.Running] = 2, [JobStepStatus.CompletedWithFailure] = 1 });

    sealed class StorageWithoutCountByStatus(IJobStepStorage inner) : IJobStepStorage
    {
        public Task<Catch> RemoveAllForJob(JobId jobId) => inner.RemoveAllForJob(jobId);

        public Task<Catch> RemoveAllNonFailedForJob(JobId jobId) => inner.RemoveAllNonFailedForJob(jobId);

        public Task<Catch> Remove(JobId jobId, JobStepId jobStepId) => inner.Remove(jobId, jobStepId);

        public Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses) => inner.GetForJob(jobId, statuses);

        public Task<Catch<int>> CountForJob(JobId jobId, params JobStepStatus[] statuses) => inner.CountForJob(jobId, statuses);

        public Catch<IObservable<IEnumerable<JobStepState>>> ObserveForJob(JobId jobId) => inner.ObserveForJob(jobId);

        public Task<Catch<TJobStepState, JobStepError>> Read<TJobStepState>(JobId jobId, JobStepId jobStepId) => inner.Read<TJobStepState>(jobId, jobStepId);

        public Task<Catch<OneOf.Types.None, JobStepError>> Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state) => inner.Save(jobId, jobStepId, state);

        public Task<Catch<OneOf.Types.None, JobStepError>> MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState) => inner.MoveToFailed(jobId, jobStepId, jobStepState);
    }
}
