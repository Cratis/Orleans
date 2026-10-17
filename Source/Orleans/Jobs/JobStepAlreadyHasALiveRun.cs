// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The exception that is thrown when a test run is begun on a job step that still has a live run, or when a test
/// run is asked to perform its step a second time.
/// </summary>
/// <remarks>
/// A step has one live run at a time, and a test run performs its step once. Wait for the earlier run's work and
/// callbacks to settle, then begin a new test run.
/// </remarks>
/// <param name="message">What was refused and why.</param>
public class JobStepAlreadyHasALiveRun(string message) : Exception(message);
