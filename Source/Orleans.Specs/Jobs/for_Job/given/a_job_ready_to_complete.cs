// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Core;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_ready_to_complete : the_job
{
    protected IStorage<SomeJobState> _stateStorage;
    protected SomeJobState? _durableState;
    protected JobStatus _storedStatus = JobStatus.Running;
    protected bool _failWrites;
    protected bool _failClears;
    protected int _writeAttempts;
    protected int _clearAttempts;
    protected List<JobStatus> _writtenStatuses = [];
    protected List<JobStatus> _statusesAtClear = [];

    protected IEnumerable<GrainTimerCreationOptions> TimerOptions => _silo.TimerRegistry.Mock.Invocations
        .SelectMany(invocation => invocation.Arguments).OfType<GrainTimerCreationOptions>();

    protected int Deactivations => _silo.GrainRuntime.Mock.Invocations.Count(invocation => invocation.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));

    protected Task HandleCompletion() => (Task)typeof(Job<SomeRequest, SomeJobState>)
        .GetMethod("HandleCompletion", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_job, null)!;

    protected async Task ActivateFromDurableState()
    {
        _silo = new();
        ConfigureSilo();
        _silo.StorageManager.GetStorage<SomeJobState>(typeof(SomeJob).FullName!).State = _durableState!;
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
    }

    async Task Establish()
    {
        _silo = new();
        ConfigureSilo();
        _stateStorage = Substitute.For<IStorage<SomeJobState>>();
        _stateStorage.State = new();
        _stateStorage.WriteStateAsync().Returns(_ =>
        {
            _writeAttempts++;
            if (_failWrites)
            {
                return Task.FromException(new InvalidOperationException("The terminal write failed"));
            }
            var state = _stateStorage.State;
            _storedStatus = state.Status;
            _writtenStatuses.Add(state.Status);
            _durableState = new()
            {
                Id = state.Id,
                Type = state.Type,
                Request = state.Request,
                Status = state.Status,
                StatusChanges = state.StatusChanges.ToList(),
                Progress = state.Progress with { }
            };
            return Task.CompletedTask;
        });
        _stateStorage.ClearStateAsync().Returns(_ =>
        {
            _clearAttempts++;
            _statusesAtClear.Add(_storedStatus);
            return _failClears
                ? Task.FromException(new InvalidOperationException("The clear failed"))
                : Task.CompletedTask;
        });
        _silo.Options.StorageFactory = _ => _stateStorage;
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
        _job.ShouldBeRemovedAfterCompleted = true;
        _job.CurrentState.Request = new SomeRequest();
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
    }
}
