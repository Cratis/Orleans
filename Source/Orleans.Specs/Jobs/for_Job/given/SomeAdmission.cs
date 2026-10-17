// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// An <see cref="IJobAdmission"/> whose answer a specification can change, and that records what it was asked.
/// </summary>
public class SomeAdmission : IJobAdmission
{
    public List<JobAdmissionRequest> Asked { get; } = [];

    public Func<Task<bool>> Answer { get; set; } = () => Task.FromResult(true);

    public static SomeAdmission Admitting() => new();

    public static SomeAdmission Refusing() => new() { Answer = () => Task.FromResult(false) };

    public static SomeAdmission Throwing() => new() { Answer = () => Task.FromException<bool>(new InvalidOperationException("The admission failed")) };

    public Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken)
    {
        Asked.Add(request);
        return Answer();
    }
}
