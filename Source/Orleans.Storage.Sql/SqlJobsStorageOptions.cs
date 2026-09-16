// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Orleans.Storage.Sql;

/// <summary>
/// Represents the options for <see cref="SqlJobsStorage"/>.
/// </summary>
public class SqlJobsStorageOptions
{
    /// <summary>
    /// Gets the function that resolves the <see cref="DbContextOptions"/> for the <see cref="JobsDbContext"/>
    /// of a scope and namespace.
    /// </summary>
    /// <remarks>
    /// The resolver decides both which database the scope and namespace map to and which EF Core provider is
    /// used - SQL Server, PostgreSQL or SQLite - giving the host full flexibility in how jobs data is laid out.
    /// </remarks>
    public required Func<string, string, DbContextOptions<JobsDbContext>> OptionsResolver { get; init; }
}
