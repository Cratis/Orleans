// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Inspects event-sourced state machine types at startup, producing their <see cref="EventSourcedStateMachineDefinition"/>.
/// </summary>
/// <remarks>
/// A state machine describes its projection and transitions in instance methods, so they are called on an instance
/// created without running any constructor - which is why those methods must describe the state machine from its type
/// alone. Nothing about the instance is kept.
/// </remarks>
internal static class EventSourcedStateMachineInspector
{
    static readonly MethodInfo _inspectMethod = typeof(EventSourcedStateMachineInspector).GetMethod(nameof(Inspect), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Check whether a type is a concrete event-sourced state machine.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsEventSourcedStateMachine(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } && GetModelType(type) is not null;

    /// <summary>
    /// Inspect an event-sourced state machine type.
    /// </summary>
    /// <param name="stateMachineType">The type of state machine.</param>
    /// <param name="artifacts">The <see cref="IClientArtifactsProvider"/> with what Chronicle has discovered.</param>
    /// <returns>The <see cref="EventSourcedStateMachineDefinition"/>.</returns>
    /// <exception cref="TypeIsNotAnEventSourcedStateMachine">Thrown when the type is not a concrete event-sourced state machine.</exception>
    /// <exception cref="StateMachineModelAlreadyHasProjection">Thrown when the state machine defines a projection for a model Chronicle already has one for.</exception>
    /// <exception cref="MissingProjectionForStateMachineModel">Thrown when nothing defines a projection for the model.</exception>
    /// <exception cref="StateMachineHasNoEventInterest">Thrown when neither the projection nor the transitions refer to any event type.</exception>
    public static EventSourcedStateMachineDefinition Inspect(Type stateMachineType, IClientArtifactsProvider artifacts)
    {
        if (!IsEventSourcedStateMachine(stateMachineType))
        {
            throw new TypeIsNotAnEventSourcedStateMachine(stateMachineType);
        }

        return (EventSourcedStateMachineDefinition)_inspectMethod
            .MakeGenericMethod(GetModelType(stateMachineType)!)
            .Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, [stateMachineType, artifacts], culture: null)!;
    }

    static Type? GetModelType(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EventSourcedStateMachine<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    static EventSourcedStateMachineDefinition Inspect<TModel>(Type stateMachineType, IClientArtifactsProvider artifacts)
        where TModel : class
    {
        var knownEventTypes = artifacts.EventTypes.ToArray();
        var modelType = typeof(TModel);
        var discoveredProjection = artifacts.Projections.FirstOrDefault(_ => typeof(IProjectionFor<TModel>).IsAssignableFrom(_));
        var discoveredAsModelBound = artifacts.ModelBoundProjections.Contains(modelType);
        var stateMachine = (EventSourcedStateMachine<TModel>)RuntimeHelpers.GetUninitializedObject(stateMachineType);

        var projection = InspectProjection(stateMachine, stateMachineType, discoveredProjection, discoveredAsModelBound, knownEventTypes);
        var transitionEventTypes = stateMachine.DescribeTransitions().EventTypes
            .SelectMany(_ => _.GetEventTypes(knownEventTypes))
            .ToImmutableHashSet();

        var definition = new EventSourcedStateMachineDefinition(
            stateMachineType,
            modelType,
            projection.Source,
            projection.IsPassive,
            projection.EventTypes.ToImmutableHashSet(),
            transitionEventTypes)
        {
            RegisterProjection = projection.Registration
        };

        if (definition.EventTypes.Count == 0)
        {
            throw new StateMachineHasNoEventInterest(stateMachineType);
        }

        return definition;
    }

    static ProjectionInspection InspectProjection<TModel>(
        EventSourcedStateMachine<TModel> stateMachine,
        Type stateMachineType,
        Type? discoveredProjection,
        bool discoveredAsModelBound,
        Type[] knownEventTypes)
        where TModel : class
    {
        var modelType = typeof(TModel);
        if (EventSourcedStateMachine<TModel>.DefinesProjection(stateMachineType))
        {
            if (discoveredProjection is not null || discoveredAsModelBound)
            {
                throw new StateMachineModelAlreadyHasProjection(stateMachineType, modelType, discoveredProjection);
            }

            return Describe<TModel>(
                StateMachineProjectionSource.DefinedByStateMachine,
                stateMachine.DescribeProjection,
                knownEventTypes,
                registrations => registrations.RegisterProjection<TModel>(
                    projection => ((EventSourcedStateMachine<TModel>)RuntimeHelpers.GetUninitializedObject(stateMachineType)).DescribeProjection(projection)));
        }

        if (discoveredProjection is not null)
        {
            return Describe<TModel>(StateMachineProjectionSource.AlreadyDiscovered, CreateProjection<TModel>(discoveredProjection).Define, knownEventTypes, registration: null);
        }

        if (discoveredAsModelBound)
        {
            return new(StateMachineProjectionSource.AlreadyDiscovered, modelType.IsPassive(), ModelBoundEventTypes.For(modelType, knownEventTypes), Registration: null);
        }

        if (modelType.HasModelBoundProjectionAttributes())
        {
            return new(
                StateMachineProjectionSource.ModelBound,
                modelType.IsPassive(),
                ModelBoundEventTypes.For(modelType, knownEventTypes),
                registrations => registrations.RegisterReadModel<TModel>());
        }

        throw new MissingProjectionForStateMachineModel(stateMachineType, modelType);
    }

    static ProjectionInspection Describe<TModel>(
        StateMachineProjectionSource source,
        Action<IProjectionBuilderFor<TModel>> describe,
        Type[] knownEventTypes,
        Action<IStateMachineArtifactRegistrations>? registration)
        where TModel : class
    {
        var builder = new InspectedProjectionBuilder<TModel>(new EventTypesRecorder(knownEventTypes));
        describe(builder);
        return new(source, builder.IsPassive, builder.EventTypes, registration);
    }

    static IProjectionFor<TModel> CreateProjection<TModel>(Type projectionType)
        where TModel : class =>
        projectionType.GetConstructor(Type.EmptyTypes) is not null
            ? (IProjectionFor<TModel>)Activator.CreateInstance(projectionType)!
            : (IProjectionFor<TModel>)RuntimeHelpers.GetUninitializedObject(projectionType);

    sealed record ProjectionInspection(
        StateMachineProjectionSource Source,
        bool IsPassive,
        IEnumerable<Type> EventTypes,
        Action<IStateMachineArtifactRegistrations>? Registration);
}
