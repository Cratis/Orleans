// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Serialization.for_LinqCollectionCopier.when_deep_copying;

public class a_linq_collection : Specification
{
    LinqCollectionCopier _copier;
    IEnumerable<int> _original;
    object _copy;

    void Establish()
    {
        _copier = new LinqCollectionCopier();
        _original = new[] { 1, 2, 3 }.Select(x => x);
    }

    void Because() => _copy = _copier.DeepCopy(_original, null!)!;

    [Fact] void should_produce_a_copy_as_an_array() => _copy.ShouldBeOfExactType<int[]>();
    [Fact] void should_contain_the_same_elements() => ((IEnumerable<int>)_copy).ShouldContainOnly(1, 2, 3);
}
