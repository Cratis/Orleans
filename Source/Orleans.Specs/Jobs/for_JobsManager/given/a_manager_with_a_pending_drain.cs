// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Orleans.Jobs.for_JobsManager.given;

public class a_manager_with_a_pending_drain : the_manager
{
    protected TaskCompletionSource _pendingDrain = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => typeof(JobsManager).GetField("_rehydration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_manager, _pendingDrain.Task);

    void Destroy() => _pendingDrain.TrySetResult();
}
