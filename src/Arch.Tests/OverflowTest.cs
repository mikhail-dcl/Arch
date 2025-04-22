using Arch.Core;
using Arch.Core.Utils;

namespace Arch.Tests;

[TestFixture]
public class OverflowTest
{
    private World _world;

    [SetUp]
    public void Setup()
    {
        _world = World.Create();
    }

    [TearDown]
    public void Teardown()
    {
       World.Destroy(_world);
    }

    [Test]
    public void ChunkDoesNotOverflowOnBulkRemoval()
    {
        for (var i = 0; i < 100; i++)
        {
            // Create entities with a single archetype
            _world.Create(new Transform());
        }

        var archetype = _world.Archetypes[0];

        var entityCapacity = archetype.EntityCapacity;

        for (var i = 0; i <= entityCapacity; i++)
        {
            // Create entities with a duplex archetype
            _world.Create(new Transform(), new Rotation());
        }

        // both archetypes should be created

        // Try move entities from duplex archetype to single archetype
        _world.Remove<Rotation>(new QueryDescription().WithAll<Transform, Rotation>());

        // Just a big value to ensure it goes beyond the original capacity
        entityCapacity = archetype.EntityCapacity;

        // Capacity should grow properly
        for (var i = 0; i <= entityCapacity; i++)
        {
            // Create entities with a duplex archetype
            // Assert.DoesNotThrow(() => _world.Create(new Transform(), new Rotation()), "Overflow at {0}", i);
            _world.Create(new Transform(), new Rotation());
        }
    }

    [Test]
    public void RemoveEntityAndBulkRemoveProducesTheSameResults()
    {
        var singleWorld = World.Create();
        var bulkWorld = World.Create();

        for (var i = 0; i < 100; i++)
        {
            // Create entities with a single archetype
            singleWorld.Create(new Transform());
            bulkWorld.Create(new Transform());
        }

        var archetype1 = singleWorld.Archetypes[0];
        var archetype2 = bulkWorld.Archetypes[0];

        // Create entities in single world

        var entities1 = Enumerable.Range(0, archetype1.EntityCapacity + 1)
            .Select(_ => singleWorld.Create(new Transform(), new Rotation()))
            .ToList();

        // Create entities in bulk world

        for (var i = 0; i <= archetype2.EntityCapacity; i++)
        {
            // Create entities with a duplex archetype
            bulkWorld.Create(new Transform(), new Rotation());
        }

        archetype1 = singleWorld.Archetypes[1];
        archetype2 = bulkWorld.Archetypes[1];

        // Try move entities from duplex archetype to single archetype

        // in a single world
        foreach (var entity in entities1)
            singleWorld.Remove<Rotation>(entity);

        // in a bulk world
        bulkWorld.Remove<Rotation>(new QueryDescription().WithAll<Transform, Rotation>());

        Assert.That(archetype2.EntityCapacity, Is.EqualTo(archetype1.EntityCapacity));
        Assert.That(archetype2.ChunkCount, Is.EqualTo(archetype1.ChunkCount));
        Assert.That(archetype2.EntityCount, Is.EqualTo(archetype1.EntityCount));
        Assert.That(archetype2.Chunks.Count, Is.EqualTo(archetype1.Chunks.Count));
        Assert.That(archetype2.EntitiesPerChunk, Is.EqualTo(archetype1.EntitiesPerChunk));
        Assert.That(archetype2.BaseChunkSize, Is.EqualTo(archetype1.BaseChunkSize));

        // Just a big value to ensure it goes beyond the original capacity
        const int ToCreate = 2000;

        for (var i = 0; i <= ToCreate; i++)
        {
            // Create entities with a duplex archetype
            // Assert.DoesNotThrow(() => _world.Create(new Transform(), new Rotation()), "Overflow at {0}", i);
            singleWorld.Create(new Transform(), new Rotation());
            bulkWorld.Create(new Transform(), new Rotation());
        }

        // entityCapacity = archetype1.EntityCapacity;
        //
        // // Capacity should grow properly
        // for (var i = 0; i <= entityCapacity; i++)
        // {
        //     // Create entities with a duplex archetype
        //     // Assert.DoesNotThrow(() => _world.Create(new Transform(), new Rotation()), "Overflow at {0}", i);
        //     _world.Create(new Transform(), new Rotation());
        // }
    }
}
