// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Represents a step having performed its item.
/// </summary>
/// <param name="Item">The item performed.</param>
/// <param name="Started">When the step started performing it.</param>
/// <param name="Finished">When the step finished performing it.</param>
public record IntegrationJobStepPerformed(string Item, DateTimeOffset Started, DateTimeOffset Finished);
