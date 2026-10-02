// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.given;

/// <summary>
/// Serializes observation specs because Arc's application service provider is process-wide.
/// </summary>
[CollectionDefinition("MongoDBJobObservations", DisableParallelization = true)]
public class ObservationCollection;
