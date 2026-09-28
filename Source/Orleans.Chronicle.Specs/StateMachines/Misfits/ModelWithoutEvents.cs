// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Orleans.Chronicle.StateMachines.Misfits;

public record ModelWithoutEvents([FromEvery(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated);
