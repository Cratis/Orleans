// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Orleans.Chronicle.StateMachines;

/// <summary>
/// Finds the event types a model-bound projection consumes, from the projection attributes on the read model.
/// </summary>
/// <remarks>
/// Every model-bound projection attribute that names an event does so through its generic type argument -
/// <c language="csharp">[FromEvent&lt;T&gt;]</c>, <c language="csharp">[SetFrom&lt;T&gt;]</c>,
/// <c language="csharp">[ChildrenFrom&lt;T&gt;]</c>, <c language="csharp">[RemovedWith&lt;T&gt;]</c> and the rest. The
/// attributes are collected from the read model, its properties and the parameters of its primary constructor, and from
/// the types of its members that carry projection attributes of their own - children and nested models. An interface or
/// base type expands to every known event type implementing it, as Chronicle does.
/// </remarks>
internal static class ModelBoundEventTypes
{
    /// <summary>
    /// Find the event types a model-bound projection consumes.
    /// </summary>
    /// <param name="modelType">The type of read model carrying the model-bound projection attributes.</param>
    /// <param name="knownEventTypes">The CLR event types the client knows.</param>
    /// <returns>The event types the projection consumes.</returns>
    public static IEnumerable<Type> For(Type modelType, IEnumerable<Type> knownEventTypes)
    {
        var known = knownEventTypes.ToArray();
        var eventTypes = new HashSet<Type>();
        Collect(modelType, known, eventTypes, []);
        return eventTypes;
    }

    static void Collect(Type type, Type[] knownEventTypes, HashSet<Type> eventTypes, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return;
        }

        var primaryConstructor = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(_ => _.GetParameters().Length)
            .FirstOrDefault();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var attributes = type.GetCustomAttributes()
            .Concat(properties.SelectMany(_ => _.GetCustomAttributes()))
            .Concat(primaryConstructor?.GetParameters().SelectMany(_ => _.GetCustomAttributes()) ?? []);

        foreach (var eventType in attributes.OfType<IProjectionAnnotation>().SelectMany(_ => EventTypesOf(_, knownEventTypes)))
        {
            eventTypes.Add(eventType);
        }

        foreach (var memberType in properties.Select(_ => ElementTypeOf(_.PropertyType)).Where(IsModelBoundModel))
        {
            Collect(memberType, knownEventTypes, eventTypes, visited);
        }
    }

    static IEnumerable<Type> EventTypesOf(IProjectionAnnotation attribute, Type[] knownEventTypes)
    {
        var attributeType = attribute.GetType();
        if (!attributeType.IsGenericType)
        {
            return [];
        }

        return attributeType
            .GetGenericArguments()
            .Where(_ => _.IsEventType(knownEventTypes))
            .SelectMany(_ => _.GetEventTypes(knownEventTypes));
    }

    static Type ElementTypeOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string))
        {
            return type;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }

    static bool IsModelBoundModel(Type type) =>
        !type.IsPrimitive && type != typeof(string) && type.HasModelBoundProjectionAttributes();
}
