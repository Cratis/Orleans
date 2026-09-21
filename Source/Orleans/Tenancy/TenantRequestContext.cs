// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy;

/// <summary>
/// The vocabulary the two tenant call filters share - the <see cref="global::Orleans.Runtime.RequestContext"/>
/// key a caller's tenant travels under.
/// </summary>
public static class TenantRequestContext
{
    /// <summary>
    /// The <see cref="global::Orleans.Runtime.RequestContext"/> key carrying the tenant across a grain call.
    /// </summary>
    public const string Key = "Cratis-Tenant-ID";
}
