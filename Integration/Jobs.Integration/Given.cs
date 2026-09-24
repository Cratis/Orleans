// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Base class for a specification whose context is set up once and then asserted on by many facts.
/// </summary>
/// <typeparam name="TContext">Type of the context.</typeparam>
/// <remarks>
/// The context is an xUnit class fixture, so its Establish and Because run once for the whole specification
/// rather than once per fact. That matters here: the facts observe a job that actually ran, and running it
/// again for every assertion would be both slow and a different job each time.
/// </remarks>
/// <param name="context">The context to assert against.</param>
public class Given<TContext>(TContext context) : IClassFixture<TContext>
    where TContext : class
{
    /// <summary>
    /// Gets the context for the current specification.
    /// </summary>
    public TContext Context { get; } = context;
}
