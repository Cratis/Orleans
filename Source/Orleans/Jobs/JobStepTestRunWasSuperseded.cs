// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// The exception that is thrown when a test run is asked to perform its step after the step has begun a later run.
/// </summary>
/// <remarks>
/// Beginning a test run replaces the step's run. Perform through the test run begun last.
/// </remarks>
/// <param name="message">What was refused and why.</param>
public class JobStepTestRunWasSuperseded(string message) : Exception(message);
