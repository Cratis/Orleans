// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoverySignals(string database)
{
    public string Database { get; } = database;
    public JobsManagerKey Key { get; } = new("recovery-store", "tenant-63");
    public ConcurrentQueue<SiloAddress> Performers { get; } = new();
    public TaskCompletionSource<SiloAddress> FirstEntry { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<SiloAddress> SecondEntry { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseOriginal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseRecovered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource StepStopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ConcurrentQueue<JobStepId> PerformerSteps { get; } = new();
    public bool HoldFirstStart { get; set; }
    public int PlanCalls;
    public int StartHookCalls;
    public ConcurrentQueue<JobStepDispatchContext> DispatchContexts { get; } = new();
    public TaskCompletionSource StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseStart { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool HoldFirstResume { get; set; }
    public TaskCompletionSource ResumeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseResume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public SemaphoreSlim RecoveryPasses { get; } = new(0);
    public SemaphoreSlim AdmissionRefusals { get; } = new(0);

    public bool RefuseAdmission
    {
        get => Volatile.Read(ref _refuseAdmission);
        set => Volatile.Write(ref _refuseAdmission, value);
    }

    bool _refuseAdmission;
}
