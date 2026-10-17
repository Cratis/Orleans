// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// The exception that is thrown when the specification cannot build a cancelled partial result.
/// </summary>
public class CancelledResultUnavailable() : Exception("The cancelled partial result could not be built.");
