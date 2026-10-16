// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RecoveryPlacementAttribute() : PlacementAttribute(new RecoveryPlacement());
