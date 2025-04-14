using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;


[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(SceneSystemGroup))]
public partial struct TornadoSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<InputData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    { 
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var config = SystemAPI.GetSingleton<Config>();
        var input = SystemAPI.GetSingleton<InputData>();
        var random = Random.CreateFromIndex(config.Seed);

        for (int i = 0; i < config.Particles; i++)
        {
            ecb.Instantiate(config.ParticlePrefab);
        }

        ecb.Playback(state.EntityManager);

        JobHandle initJob = new PotatoInitializationJob
        {
            Random = random,
            Config = config,
            Tornado = input.MousePosition,
        }.Schedule(state.Dependency);

        initJob.Complete();

        state.Enabled = false;
    }
}

[BurstCompile]
public partial struct PotatoInitializationJob : IJobEntity
{
    [ReadOnly] public Random Random;
    [ReadOnly] public Config Config;
    [ReadOnly] public float2 Tornado;

    [BurstCompile]
    public void Execute(in Particle particle, ref LocalTransform transform)
    {
        float2 spawnPoint = Config.SpawnMode switch
        {
            SpawnMode.Circle => Tornado + Random.NextFloat2Direction() * (Random.NextFloat() * Config.TornadoBlob.Value.SpawnRadius),
            SpawnMode.Square => Tornado + new float2(Random.NextFloat(-Config.TornadoBlob.Value.SpawnRadius, Config.TornadoBlob.Value.SpawnRadius),
                Random.NextFloat(-Config.TornadoBlob.Value.SpawnRadius, Config.TornadoBlob.Value.SpawnRadius)),
            _ => throw new ArgumentOutOfRangeException()
        };

        transform.Position = new float3(spawnPoint.x, Random.NextFloat(Config.SpawnHeight.x, Config.SpawnHeight.y),
            spawnPoint.y);

        transform.Scale = Random.NextFloat(Config.ParticleSizeRange.x, Config.ParticleSizeRange.y);
    }
}
