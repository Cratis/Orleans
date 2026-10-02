// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.for_TransientRetry.given;

public class a_transient_retry : Specification
{
    protected RecordingTimeProvider _time;
    protected TransientRetry _retry;
    protected int _attempts;

    void Establish()
    {
        _time = new();
        _retry = new(3, TimeSpan.FromMilliseconds(100), _time, jitter: () => 1d);
    }

    protected static MongoWaitQueueFullException WaitQueueFull() => new("The wait queue is full");
}
