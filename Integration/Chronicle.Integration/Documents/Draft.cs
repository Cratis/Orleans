// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.Documents;

public class Draft : State<Document>
{
    protected override IImmutableList<Type> AllowedTransitions => [typeof(Submitted)];
}
