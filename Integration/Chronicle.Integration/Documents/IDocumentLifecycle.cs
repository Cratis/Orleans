// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.Documents;

public interface IDocumentLifecycle : IEventSourcedStateMachine
{
    Task<string> GetStateName();

    Task<int> GetModelChanges();

    Task Deactivate();
}
