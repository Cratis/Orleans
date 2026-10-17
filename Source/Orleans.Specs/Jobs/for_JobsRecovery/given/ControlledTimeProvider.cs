// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Orleans.Jobs.for_JobsRecovery.given;

public class ControlledTimeProvider : TimeProvider
{
    readonly Channel<PendingDelay> _delays = Channel.CreateUnbounded<PendingDelay>();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new PendingDelay(callback, state, dueTime);
        _delays.Writer.TryWrite(timer);
        return timer;
    }

    public async Task<PendingDelay> NextDelay(TimeSpan deadline) =>
        await _delays.Reader.ReadAsync().AsTask().WaitAsync(deadline, System);

    public sealed class PendingDelay(TimerCallback callback, object? state, TimeSpan duration) : ITimer
    {
        int _disposed;

        public TimeSpan Duration { get; } = duration;

        public void Elapse()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                callback(state);
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _disposed, 1);
            return ValueTask.CompletedTask;
        }
    }
}
