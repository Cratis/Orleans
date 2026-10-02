// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

public sealed class WatchLogger : ILoggerProvider, ILogger
{
    public Exception? Error { get; private set; }

    public ILogger CreateLogger(string categoryName) => this;
    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => Disposable.Empty;
    public void Dispose() => GC.SuppressFinalize(this);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            Error = exception;
        }
    }
}
