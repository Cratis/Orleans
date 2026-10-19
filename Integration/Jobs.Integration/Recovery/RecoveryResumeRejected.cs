// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Recovery;

/// <summary>
/// The exception that is thrown when the recovery job's resume hook is told to reject a resume.
/// </summary>
public class RecoveryResumeRejected() : Exception("The resume hook rejected the resume");
