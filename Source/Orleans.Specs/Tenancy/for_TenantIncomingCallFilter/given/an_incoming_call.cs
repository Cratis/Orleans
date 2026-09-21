// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy.for_TenantIncomingCallFilter.given;

public class an_incoming_call : Specification
{
    protected TenantIncomingCallFilter _filter;
    protected ITenancy _tenancy;
    protected IIncomingGrainCallContext _context;
    protected IGrainContext _target;
    protected IDisposable _establishedScope;
    protected string? _establishedTenant;
    protected bool _establishedWhenInvoked;
    protected bool _invoked;

    void Establish()
    {
        _tenancy = Substitute.For<ITenancy>();
        _establishedScope = Substitute.For<IDisposable>();
        _tenancy.Establish(Arg.Any<string>()).Returns(callInfo =>
        {
            _establishedTenant = callInfo.Arg<string>();
            return _establishedScope;
        });

        _filter = new TenantIncomingCallFilter(_tenancy);
        _target = Substitute.For<IGrainContext>();
        _context = Substitute.For<IIncomingGrainCallContext>();
        _context.TargetContext.Returns(_ => _target);
        _context.When(c => c.Invoke()).Do(_ =>
        {
            _invoked = true;
            _establishedWhenInvoked = _establishedTenant is not null;
        });
    }

    /// <summary>
    /// Makes the target a job-family grain of the given interface with the given compound key.
    /// </summary>
    /// <typeparam name="TGrain">The job-family grain interface.</typeparam>
    /// <param name="key">The grain id key holding the job family key string as its extension.</param>
    protected void TargetIs<TGrain>(IdSpan key)
        where TGrain : class, IAddressable
    {
        _target.GrainInstance.Returns(Substitute.For<TGrain>());
        _target.GrainId.Returns(GrainId.Create(GrainType.Create("target"), key));
    }
}
