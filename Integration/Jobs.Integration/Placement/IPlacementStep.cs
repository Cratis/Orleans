// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Placement;

public interface IPlacementStep : IJobStep<PlacementRequest, string, JobStepState>;
