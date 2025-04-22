using Arch.Core;

namespace Arch.Buffer;

/// <summary>
///     Right the same as <see cref="CommandBuffer"/> but doesn't use Pooled Collections under the hood.
///     It's meant to be reused multiple times when the Shared Pooling Behaviour is undesirable.
/// </summary>
public class PersistentCommandBuffer : IDisposable
{
    private readonly List<ComponentType> _addTypes;
    private readonly List<ComponentType> _removeTypes;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandBuffer"/> class
    ///     with the specified <see cref="Core.World"/> and an optional <paramref name="initialCapacity"/> (default: 128).
    /// </summary>
    /// <param name="initialCapacity">The initial capacity.</param>
    public PersistentCommandBuffer(int initialCapacity = 128)
    {
        Entities = new List<Entity>(initialCapacity);
        BufferedEntityInfo = new Dictionary<int, BufferedEntityInfo>(initialCapacity);
        Creates = new List<CreateCommand>(initialCapacity);
        Sets = new SparseSet(initialCapacity);
        Adds = new StructuralSparseSet(initialCapacity);
        Removes = new StructuralSparseSet(initialCapacity);
        Destroys = new List<int>(initialCapacity);
        _addTypes = new List<ComponentType>(16);
        _removeTypes = new List<ComponentType>(16);
    }

    /// <summary>
    ///     Gets the amount of <see cref="Entity"/> instances targeted by this <see cref="CommandBuffer"/>.
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    ///     All <see cref="Entity"/>'s created or modified in this <see cref="CommandBuffer"/>.
    /// </summary>
    internal List<Entity> Entities { get; set; }

    /// <summary>
    ///     A map that stores some additional information for each <see cref="Entity"/>, which is needed for the internal <see cref="CommandBuffer"/> operations.
    /// </summary>
    internal Dictionary<int, BufferedEntityInfo> BufferedEntityInfo { get; set; }

    /// <summary>
    ///     All create commands recorded in this <see cref="CommandBuffer"/>. Used to create <see cref="Entity"/>'s during <see cref="Playback"/>.
    /// </summary>
    internal List<CreateCommand> Creates { get; set; }

    /// <summary>
    ///     Saves set operations for components to play them back later during <see cref="Playback"/>.
    /// </summary>
    internal SparseSet Sets { get; set; }

    /// <summary>
    ///     Saves add operations for components to play them back later during <see cref="Playback"/>.
    /// </summary>
    internal StructuralSparseSet Adds { get; set; }

    /// <summary>
    ///     Saves remove operations for components to play them back later during <see cref="Playback"/>.
    /// </summary>
    internal StructuralSparseSet Removes { get; set; }

    /// <summary>
    ///     Saves remove operations for <see cref="Entity"/>'s to play them back later during <see cref="Playback"/>.
    /// </summary>
    internal List<int> Destroys { get; set; }

    /// <summary>
    ///     Registers a new <see cref="Entity"/> into the <see cref="CommandBuffer"/>.
    ///     An <see langword="out"/> parameter contains its <see cref="Arch.Buffer.BufferedEntityInfo"/>.
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to register.</param>
    /// <param name="info">Its <see cref="BufferedEntityInfo"/> which stores indexes used for <see cref="CommandBuffer"/> operations.</param>

    internal void Register(in Entity entity, out BufferedEntityInfo info)
    {
        var setIndex = Sets.Create(in entity);
        var addIndex = Adds.Create(in entity);
        var removeIndex = Removes.Create(in entity);

        info = new BufferedEntityInfo(Size, setIndex, addIndex, removeIndex);

        Entities.Add(entity);
        BufferedEntityInfo.Add(entity.Id, info);
        Size++;
    }

    /// TODO : Probably just run this if the wrapped entity is negative? To save some overhead?
    /// <summary>
    ///     Resolves an <see cref="Entity"/> originally either from a <see cref="StructuralSparseArray"/> or <see cref="SparseArray"/> to its real <see cref="Entity"/>.
    ///     This is required since we can also create new entities via this buffer and buffer operations for it. So sometimes there negative entities stored in the arrays and those must then be resolved to its newly created real entity.
    ///     <remarks>Probably hard to understand, blame genaray for this.</remarks>
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> with a negative or positive id to resolve.</param>
    /// <returns>Its real <see cref="Entity"/>.</returns>

    internal Entity Resolve(Entity entity)
    {
        var entityIndex = BufferedEntityInfo[entity.Id].Index;
        return Entities[entityIndex];
    }

    /// <summary>
    ///     Records a Create operation for an <see cref="Entity"/> based on its component structure.
    ///     Will be created during <see cref="Playback"/>.
    /// </summary>
    /// <param name="types">The <see cref="Entity"/>'s component structure/<see cref="Archetype"/>.</param>
    /// <returns>The buffered <see cref="Entity"/> with an index of <c>-1</c>.</returns>

    public Entity Create(ComponentType[] types)
    {
        lock (this)
        {
            var entity = new Entity(-(Size + 1), -1);
            Register(entity, out _);

            var command = new CreateCommand(Size - 1, types);
            Creates.Add(command);

            return entity;
        }
    }

    /// <summary>
    ///     Record a Destroy operation for an (buffered) <see cref="Entity"/>.
    ///     Will be destroyed during <see cref="Playback"/>.
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to destroy.</param>

    public void Destroy(in Entity entity)
    {
        lock (this)
        {
            if (!BufferedEntityInfo.TryGetValue(entity.Id, out var info))
            {
                Register(entity, out info);
            }

            Destroys.Add(info.Index);
        }
    }

    /// <summary>
    ///     Records a set operation for an (buffered) <see cref="Entity"/>.
    ///     Overwrites previous values.
    ///     Will be set during <see cref="Playback"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The <see cref="Entity"/>.</param>
    /// <param name="component">The component value.</param>

    public void Set<T>(in Entity entity, in T? component = default)
    {
        BufferedEntityInfo info;
        lock (this)
        {
            if (!BufferedEntityInfo.TryGetValue(entity.Id, out info))
            {
                Register(entity, out info);
            }
        }

        Sets.Set(info.SetIndex, in component);
    }

    /// <summary>
    ///     Records a add operation for an (buffered) <see cref="Entity"/>.
    ///     Overwrites previous values.
    ///     Will be added during <see cref="Playback"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The <see cref="Entity"/>.</param>
    /// <param name="component">The component value.</param>

    public void Add<T>(in Entity entity, in T? component = default)
    {
        BufferedEntityInfo info;
        lock (this)
        {
            if (!BufferedEntityInfo.TryGetValue(entity.Id, out info))
            {
                Register(entity, out info);
            }
        }

        Adds.Set<T>(info.AddIndex);
        Sets.Set(info.SetIndex, in component);
    }

    /// <summary>
    ///     Records a remove operation for an (buffered) <see cref="Entity"/>.
    ///     Will be removed during <see cref="Playback"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The <see cref="Entity"/>.</param>

    public void Remove<T>(in Entity entity)
    {
        BufferedEntityInfo info;
        lock (this)
        {
            if (!BufferedEntityInfo.TryGetValue(entity.Id, out info))
            {
                Register(entity, out info);
            }
        }

        Removes.Set<T>(info.RemoveIndex);
    }

    /// <summary>
    ///     Plays back all recorded commands, modifying the world.
    /// </summary>
    /// <remarks>
    ///     This operation should only happen on the main thread.
    /// </remarks>
    /// <param name="world">The <see cref="World"/> where the commands will be playbacked too.</param>
    /// <param name="dispose">If true it will clear the recorded operations after they were playbacked, if not they will stay.</param>

    public void Playback(World world, bool dispose = true)
    {
        // Create recorded entities.
        foreach (var cmd in Creates)
        {
            var entity = world.Create(cmd.Types);
            Entities[cmd.Index] = entity;
        }

        // Play back additions.
        for (var index = 0; index < Adds.Count; index++)
        {
            var wrappedEntity = Adds.Entities[index];
            for (var i = 0; i < Adds.UsedSize; i++)
            {
                var usedIndex = Adds.Used[i];
                var sparseSet = Adds.Components[usedIndex];

                if (!sparseSet.Contains(wrappedEntity.Index))
                {
                    continue;
                }

                _addTypes.Add(sparseSet.Type);
            }

            if (_addTypes.Count <= 0)
            {
                continue;
            }

            // Resolves the entity to get the real one (e.g. for newly created negative entities and stuff).
            var entity = Resolve(wrappedEntity.Entity);
            Debug.Assert(world.IsAlive(entity), $"CommandBuffer can not to add components to the dead {wrappedEntity.Entity}");

            CommandBuffer.AddRange(world, entity, _addTypes.AsSpan());
            _addTypes.Clear();
        }

        // Play back sets.
        for (var index = 0; index < Sets.Count; index++)
        {
            // Get wrapped entity
            var wrappedEntity = Sets.Entities[index];
            var entity = Resolve(wrappedEntity.Entity);
            var id = wrappedEntity.Index;

            Debug.Assert(world.IsAlive(entity), $"CommandBuffer can not to set components to the dead {wrappedEntity.Entity}");

            // Get entity chunk
            var entityInfo = world.EntityInfo.GetEntityData(entity.Id);
            var archetype = entityInfo.Archetype;
            ref readonly var chunk = ref archetype.GetChunk(entityInfo.Slot.ChunkIndex);
            var chunkIndex = entityInfo.Slot.Index;

            // Loop over all sparset component arrays and if our entity is in one, copy the set component to its chunk
            for (var i = 0; i < Sets.UsedSize; i++)
            {
                var used = Sets.Used[i];
                var sparseArray = Sets.Components[used];

                if (!sparseArray.Contains(id))
                {
                    continue;
                }

                var chunkArray = chunk.GetArray(sparseArray.Type);
                Array.Copy(sparseArray.Components, sparseArray.Entities[id], chunkArray, chunkIndex, 1);

#if EVENTS
                // Entity also exists in add and the set component was added recently
                if (Adds.Used.Length > i && Adds.Components[Adds.Used[i]].Contains(id))
                {
                    world.OnComponentAdded(entity, sparseArray.Type);
                }
                else
                {
                    world.OnComponentSet(entity, sparseArray.Type);
                }
#endif
            }
        }

        // Play back removals.
        for (var index = 0; index < Removes.Count; index++)
        {
            var wrappedEntity = Removes.Entities[index];
            for (var i = 0; i < Removes.UsedSize; i++)
            {
                var usedIndex = Removes.Used[i];
                var sparseSet = Removes.Components[usedIndex];
                if (!sparseSet.Contains(wrappedEntity.Index))
                {
                    continue;
                }

                _removeTypes.Add(sparseSet.Type);
            }

            if (_removeTypes.Count <= 0)
            {
                continue;
            }

            var entity = Resolve(wrappedEntity.Entity);
            Debug.Assert(world.IsAlive(entity), $"CommandBuffer can not to remove components from the dead {wrappedEntity.Entity}");

            world.RemoveRange(entity, _removeTypes.AsSpan());
            _removeTypes.Clear();
        }

        // Play back destructions.
        foreach (var cmd in Destroys)
        {
            world.Destroy(Entities[cmd]);
        }

        // Reset values.
        if (!dispose)
        {
            return;
        }

        Size = 0;
        Entities.Clear();
        BufferedEntityInfo.Clear();
        Creates.Clear();
        Sets.Clear();
        Adds.Clear();
        Removes.Clear();
        Destroys.Clear();
        _addTypes.Clear();
        _removeTypes.Clear();
    }

    /// <summary>
    ///     Disposes the <see cref="CommandBuffer"/>.
    /// </summary>
    public void Dispose()
    {
        Creates.Clear();
        Sets.Clear();
        Adds.Clear();
        Removes.Clear();
        GC.SuppressFinalize(this);
    }
}
