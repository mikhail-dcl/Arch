namespace Arch.SourceGen;

/// <summary>
///     Adds extension methods for generating `World.Add(in query, T0...TN);` methods.
/// </summary>
public static class AddWithQueryDescription
{
    /// <summary>
    ///     Appends `World.Add(in query, T0...TN)` methods.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> instance.</param>
    /// <param name="amount">The amount.</param>
    /// <returns></returns>
    public static StringBuilder AppendAddWithQueryDescriptions(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendAddWithQueryDescription(index);
        }

        return sb;
    }

    /// <summary>
    ///     Appends a `World.Add(in query, T0...TN)` method.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> instance.</param>
    /// <param name="amount">The amount of generic parameters.</param>
    public static void AppendAddWithQueryDescription(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInDefaultParams(amount);
        var inParameters = new StringBuilder().InsertGenericInParams(amount);
        var types = new StringBuilder().GenericTypeParams(amount);

        var setIds = new StringBuilder();
        for (var index = 0; index <= amount; index++)
        {
            setIds.AppendLine($"spanBitSet.SetBit(Component<T{index}>.ComponentType.Id);");
        }

        var template =
            $$"""
            [SkipLocalsInit]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<{{generics}}>(in QueryDescription queryDescription, {{parameters}})
            {
                // BitSet to stack/span bitset, size big enough to contain ALL registered components.
                Span<uint> stack = stackalloc uint[BitSet.RequiredLength(ComponentRegistry.Size)];

                var query = Query(in queryDescription);
                foreach (var archetype in query.GetArchetypeIterator())
                {
                    // Archetype with T shouldnt be skipped to prevent undefined behaviour.
                    if(archetype.Entities == 0 || archetype.Has<{{generics}}>())
                    {
                        continue;
                    }

                    // Create local bitset on the stack and set bits to get a new fitting bitset of the new archetype.
                    var bitSet = archetype.BitSet;
                    var spanBitSet = new SpanBitSet(bitSet.AsSpan(stack));
                    {{setIds}}

                    // Get or create new archetype.
                    if (!TryGetArchetype(spanBitSet.GetHashCode(), out var newArchetype))
                    {
                        newArchetype = GetOrCreate(archetype.Types.Add({{types}}));
                    }

                    // Get last slots before copy, for updating entityinfo later
                    var archetypeSlot = archetype.LastSlot;
                    var newArchetypeLastSlot = newArchetype.LastSlot;
                    Slot.Shift(ref newArchetypeLastSlot, newArchetype.EntitiesPerChunk);

                    Archetype.Copy(archetype, newArchetype);
                    archetype.Clear();

                    // Set added value and update the entity info
                    var lastSlot = newArchetype.LastSlot;
                    newArchetype.SetRange(in lastSlot, in newArchetypeLastSlot, {{inParameters}});
                    ShiftEntityInfo(archetype, archetypeSlot, newArchetype, newArchetypeLastSlot);
                }
            }
            """;

        sb.AppendLine(template);
    }
}