using System;
using Tornado;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using TMath = Tornado.Math;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct TornadoSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InputData>();
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var config = SystemAPI.GetSingleton<Config>();
        var input = SystemAPI.GetSingleton<InputData>();

        if (config.UsedBlob)
        {
            TornadoParticleJobBlob handle = new()
            {
                Seed = config.Seed + (uint)SystemAPI.Time.ElapsedTime,

                Pause = config.Pause,
                Drop = config.Drop,

                Tornado = input.MousePosition,
                SpawnHeight = config.SpawnHeight,
                DeltaTime = deltaTime,

                TornadoScale = config.Scale,
                InverseScale = 1 / config.Scale,

                SpawnMode =  (int)config.SpawnMode,

                EnvRef = config.TornadoBlob,
            };

            if (config.RunMode == RunMode.SingleThreaded) handle.Schedule();
            else handle.ScheduleParallel();
        }
        else
        {
            TornadoParticleJobDynamic handle = new()
            {
                Seed = config.Seed + (uint)SystemAPI.Time.ElapsedTime,
                Pause = config.Pause,
                Drop = config.Drop,

                Tornado = input.MousePosition,
                DeltaTime = deltaTime,

                TornadoScale = config.Scale,
                InverseScale = 1 / config.Scale,

                GroundTemperatureKelvin = config.GroundTemperatureKelvin,
                DewPoint = config.DewPoint,
                CoreFunnelPressure = config.CoreFunnelPressure,
                Width = config.Width,

                SpawnMode = (int)config.SpawnMode,
                SpawnHeight = config.SpawnHeight,

                EnvRef = config.TornadoBlob,
            };

            if (config.RunMode == RunMode.SingleThreaded) handle.Schedule();
            else handle.ScheduleParallel();
        }
    }

    [BurstCompile]
    public static bool RunTimeVariables(
        bool pause,
        bool drop,
        ref LocalTransform transform,
        ref PhysicsVelocity velocity,
        float deltaTime
        )
    {
        if (pause)
        {
            velocity.Linear = float3.zero;
            return true;
        }

        if (transform.Position.y < 1) transform.Position.y = 1;

        if (drop)
        {
            velocity.Linear.x = 0;
            velocity.Linear.y -= TMath.Gravity * deltaTime;
            velocity.Linear.z = 0;
            return true;
        }

        return false;
    }

    [BurstCompile]
    public static bool Despawn(
        float altitude,
        float distance,
        ref TornadoBlob tBlob,
        uint seed,
        int spawnMode,
        in float2 tornado,
        ref LocalTransform transform,
        in float2 spawnHeight,
        ref PhysicsVelocity velocity
        )
    {
        if (altitude <= tBlob.DespawnHeight && distance <= tBlob.DespawnRadius) return false;
        Random random = new(seed);
        float2 spawnPoint = spawnMode switch
        {
            0 => tornado + random.NextFloat2Direction() * (random.NextFloat() * tBlob.SpawnRadius),
            1 => tornado + new float2(random.NextFloat(-tBlob.SpawnRadius, tBlob.SpawnRadius),
                random.NextFloat(-tBlob.SpawnRadius, tBlob.SpawnRadius)),
            _ => throw new ArgumentOutOfRangeException()
        };

        transform.Position = new float3(spawnPoint.x, random.NextFloat(spawnHeight.x, spawnHeight.y), spawnPoint.y);
        velocity.Linear = float3.zero;
        velocity.Angular = float3.zero;
        return true;
    }

    [BurstCompile]
    public static void ApplyForces(
        float altitude,
        float distance,
        ref TornadoBlob tBlob,
        in float2 tornado,
        ref LocalTransform transform,
        ref PhysicsVelocity velocity,
        float radius,
        float maxWindSpeed,
        float airDensity,
        float objectArea,
        float maxPressuresDeficit,
        float deltaTime,
        float inverseMass
        )
    {
        float2 negRadial = math.normalizesafe(tornado - transform.Position.xz);

        bool inSideCore = distance < radius;
        float ratio = inSideCore ? distance / radius : radius / distance;

        float3 wind = new float3(negRadial.y, 0, -negRadial.x) *
                      TMath.WindSpeed(maxWindSpeed, ratio);
        float3 tangent = new(negRadial.x, 0, negRadial.y);

        Forces.WindForces(
            out float3 windForces,
            airDensity,
            in wind,
            in velocity.Linear,
            objectArea,
            tBlob.LiftCoefficient,
            tBlob.WindCoefficient,
            tBlob.DragCoefficient * (altitude < tBlob.MouthHeight ? 5 : 1)
        );

        Forces.PressureForces(out float3 pressureForce,
            TMath.PressureDeficit(maxPressuresDeficit, ratio, inSideCore),
            distance,
            in tangent,
            tBlob.PressureGradientCoefficient
        );

        velocity.Linear += deltaTime * inverseMass * (windForces + pressureForce);

        float velocityValue = math.length(velocity.Linear);
        if (velocityValue > 100f) velocity.Linear = velocity.Linear / velocityValue * 100f;

    }
}

[BurstCompile]
public partial struct TornadoParticleJobBlob : IJobEntity
{
    [ReadOnly] public uint Seed;

    [ReadOnly] public bool Pause;
    [ReadOnly] public bool Drop;

    [ReadOnly] public float2 Tornado;
    [ReadOnly] public float2 SpawnHeight;
    [ReadOnly] public float DeltaTime;

    [ReadOnly] public float TornadoScale;
    [ReadOnly] public float InverseScale;
    [ReadOnly] public int SpawnMode;

    [ReadOnly] public BlobAssetReference<TornadoBlob> EnvRef;


    [BurstCompile]
    private void Execute(ref PhysicsVelocity velocity, in PhysicsMass physicsMass, ref LocalTransform transform, in Particle particle, in Entity entity)
    {
        if (TornadoSystem.RunTimeVariables(Pause, Drop, ref transform, ref velocity, DeltaTime)) return;

        ref TornadoBlob tBlob = ref EnvRef.Value;

        float distance = math.distance(Tornado, transform.Position.xz);

        int roundAltitude = (int)math.round(transform.Position.y * InverseScale);

        // check despawn
        if (TornadoSystem.Despawn(
                roundAltitude,
                distance,
                ref tBlob,
                Seed + (uint)entity.Index,
                SpawnMode,
                in Tornado,
                ref transform,
                in SpawnHeight,
                ref velocity
            )) return;

        if (roundAltitude >= tBlob.TornadoHeight || roundAltitude < 0 || !(distance <= tBlob.Range)) return;

        TornadoSystem.ApplyForces(
            roundAltitude,
            distance,
            ref tBlob,
            in Tornado,
            ref transform,
            ref velocity,
            tBlob.CoreRadii[roundAltitude],
            tBlob.MaxWindSpeeds[roundAltitude],
            tBlob.AirDensities[roundAltitude],
            particle.ObjectArea * TornadoScale,
            tBlob.MaxPressuresDeficits[roundAltitude],
            DeltaTime,
            physicsMass.InverseMass
        );
    }
}

[BurstCompile]
public partial struct TornadoParticleJobDynamic : IJobEntity
{
    [ReadOnly] public uint Seed;

    [ReadOnly] public bool Pause;
    [ReadOnly] public bool Drop;

    [ReadOnly] public float2 Tornado;
    [ReadOnly] public float DeltaTime;

    [ReadOnly] public float TornadoScale;
    [ReadOnly] public float InverseScale;

    [ReadOnly] public float GroundTemperatureKelvin;
    [ReadOnly] public float DewPoint;
    [ReadOnly] public float CoreFunnelPressure;
    [ReadOnly] public float Width;

    [ReadOnly] public int SpawnMode;
    [ReadOnly] public float2 SpawnHeight;

    [ReadOnly] public BlobAssetReference<TornadoBlob> EnvRef;

    [BurstCompile]
    private void Execute(ref PhysicsVelocity velocity, in PhysicsMass physicsMass, ref LocalTransform transform, in Particle particle, in Entity entity)
    {
        if (TornadoSystem.RunTimeVariables(Pause, Drop, ref transform, ref velocity, DeltaTime)) return;

        float distance = math.distance(Tornado, transform.Position.xz);

        float altitude = transform.Position.y * InverseScale;

        float altitudinalTemperature = TMath.TemperatureAtAltitude(altitude, GroundTemperatureKelvin);
        float altitudinalDewPoint = TMath.DewPointAtAltitude(altitude, DewPoint);
        float pressure = TMath.AltitudeToPressure(altitude, GroundTemperatureKelvin);
        float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
        float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, CoreFunnelPressure);
        float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
        float radius = TMath.CoreRadius(pressure, CoreFunnelPressure, maxPressuresDeficit, maxWindSpeed, GroundTemperatureKelvin) * Width;

        ref TornadoBlob tBlob = ref EnvRef.Value;

        // check despawn
        if (TornadoSystem.Despawn(
                altitude,
                distance,
                ref tBlob,
                Seed + (uint)entity.Index,
                SpawnMode,
                in Tornado,
                ref transform,
                in SpawnHeight,
                ref velocity
                )
        ) return;

        if (!(altitude < tBlob.TornadoHeight) || !(altitude >= 0) || !(distance <= tBlob.Range)) return;

        TornadoSystem.ApplyForces(
            altitude,
            distance,
            ref tBlob,
            in Tornado,
            ref transform,
            ref velocity,
            radius,
            maxWindSpeed,
            airDensity,
            particle.ObjectArea * TornadoScale,
            maxPressuresDeficit,
            DeltaTime,
            physicsMass.InverseMass
        );
    }
}