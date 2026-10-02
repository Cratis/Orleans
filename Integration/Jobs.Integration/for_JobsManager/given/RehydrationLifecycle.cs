// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public class RehydrationLifecycle
{
    public bool PauseDiscovery { get; set; }
    public TaskCompletionSource DiscoveryStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource AllowDiscovery { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource FirstReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource AllowFirstRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DeactivationRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DrainStopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource FinishRequest { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource OnDeactivateCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DeactivationCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task BackgroundDrain { get; set; }
    public bool DrainWasCompleteWhenDeactivated { get; set; }
    public IJobStorage JobStorage { get; } = Substitute.For<IJobStorage>();
}
