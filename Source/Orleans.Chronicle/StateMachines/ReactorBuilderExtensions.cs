// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Extension methods for declaring typed reactor handlers for event types only known at runtime.
/// </summary>
internal static class ReactorBuilderExtensions
{
    static readonly MethodInfo _onMethod = typeof(ReactorBuilderExtensions).GetMethod(nameof(OnEvent), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Handle a specific event type, known as a <see cref="Type"/>, the way <see cref="IReactorBuilder.On{TEvent}(Func{TEvent, EventContext, Task})"/> does.
    /// </summary>
    /// <param name="builder">The <see cref="IReactorBuilder"/>.</param>
    /// <param name="eventType">The event type to handle. Subscribes the reactor to this event type only.</param>
    /// <param name="handler">The handler receiving the event and its context.</param>
    /// <returns>The builder for continuation.</returns>
    public static IReactorBuilder On(this IReactorBuilder builder, Type eventType, Func<object, EventContext, Task> handler) =>
        (IReactorBuilder)_onMethod
            .MakeGenericMethod(eventType)
            .Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, [builder, handler], culture: null)!;

    static IReactorBuilder OnEvent<TEvent>(IReactorBuilder builder, Func<object, EventContext, Task> handler) =>
        builder.On<TEvent>((@event, context) => handler(@event!, context));
}
