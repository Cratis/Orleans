// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement.given;

public class a_placement_target : Specification
{
    protected static readonly SiloAddress _answer = SiloAddress.New(new(System.Net.IPAddress.Loopback, 11111), 1);

    protected PlacementTarget _target;
    protected IPlacementContext _context;

    void Establish()
    {
        _target = new PlacementTarget(GrainId.Create("some-job", "some-key"), [], GrainInterfaceType.Create("some-interface"), 0);
        _context = Substitute.For<IPlacementContext>();
    }
}
