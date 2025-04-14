using System;
using Tornado;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TMath = Tornado.Math;

public class ConfigAuthoring : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Random seed for the simulation")]
    [SerializeField] public uint randomSeed;
    [Tooltip("Choose between single or multi threaded")]
    public RunMode runMode;
    [Tooltip("Choose between cached (use blob asset) or dynamic tornado physics calculations")]
    public PhysicsCache physicsCaching;
    [Tooltip("Choose the shape that the particles should spawn in")]
    public SpawnMode spawnMode;

    [Header("Runtime particle states")]
    [Tooltip("Freezes the simulation")]
    [SerializeField] public bool pause;
    [Tooltip("Causes all particles to drop to the ground")]
    [SerializeField] public bool drop;

    [Header("Prefabs")]
    [Tooltip("The prefab used for the particles in the simulation")]
    [SerializeField] public GameObject particlePrefab;

    [Header("Simulation Size")]
    [Tooltip("Number of particles simulated")]
    [SerializeField] public int particles;
    [Tooltip("Minimum and maximum size of particles")]
    [SerializeField] public float2 particleSizeRange;

    [Header("Tornado Properties")]
    [Tooltip("Ambient temperature at sea level (c\u00b0)")]
    [Range(0f, 90f)] public float temperature = 15;
    [Tooltip("Temperature point at which the air can hold no more water (c\u00b0)")]
    [Range(0f, 90f)] public float dewPoint = 8;
    [Tooltip("Model Scale (Doesn't affect physics calculations, but how object distances are calculated)")]
    [Range(0f, 1f)] public float scale = 1f;
    [Tooltip("Scales the tornado's width (Does affect physics calculations!!!)")]
    public float width = 1f;
    [Tooltip("Tornado's effective range scaler")]
    public float tornadoRange = 5f;
    [Tooltip("Scales the spawning area's height")]
    public float2 spawnHeight = new float2(1f, 10f);
    [Tooltip("Scales the spawning area's width ")]
    public float spawnRadius = 0.5f;
    [Tooltip("Controls the range for potatoes resetting")]
    public float2 despawnRange = 2f;
    [Tooltip("The starting position of the tornado")]
    public float2 startPosition = new Vector2(0f, 0f);

    [Header("Coefficients")]
    public bool negativeCoefficients;
    [Tooltip("Coefficient modeling all of the complex dependencies of shape, inclination, and some flow conditions")]
    public float pressureGradientCoefficient = 1f; // TODO not universal, object specific
    [Tooltip("")] public float liftCoefficient = 0.5f; // TODO not universal, object specific
    [Tooltip("")] public float windCoefficient = 0.5f; // TODO not universal, object specific
    [Tooltip("")] public float dragCoefficient = 0.5f; // TODO not universal, object specific

    #region GIZMOS!!!
    [Header("Gizmos")]
    [Tooltip("Enable to hide the tornado gizmo")]
    public bool hide;

    public bool showSpawnArea;
    public bool showDespawnRange;

    void OnValidate()
    {
        temperature = math.max(temperature, dewPoint);
        dewPoint = math.min(temperature, dewPoint);
    }

    // Resets static values 
    private void OnDisable()
    {
        ECSWorldInterface.position = startPosition;
    }


    void OnDrawGizmos()
    {
        if (hide) return;

        float groundTemperatureKelvin = TMath.CToKelvin(temperature);
        float coreFunnelPressure = TMath.CoreFunnelPressure(groundTemperatureKelvin, dewPoint);
        float tornadoHeight = TMath.PressureToAltitude(coreFunnelPressure, groundTemperatureKelvin);

        // draw tornado
        int range = 0;
        for (int i = 0; i < tornadoHeight; i++)
        {
            float altitudinalTemperature = TMath.TemperatureAtAltitude(i, groundTemperatureKelvin);
            float altitudinalDewPoint = TMath.DewPointAtAltitude(i, dewPoint);
            float pressure = TMath.AltitudeToPressure(i, groundTemperatureKelvin);
            float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
            float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, coreFunnelPressure);
            float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
            float radius = TMath.CoreRadius(pressure, coreFunnelPressure, maxPressuresDeficit, maxWindSpeed, groundTemperatureKelvin) * width;
            var color = math.lerp(new float3(1, 0.5f, 0), new float3(0, 0, 1), i / tornadoHeight);
            Gizmos.color = new Color(color.x, color.y, color.z);

            GizmosExtra.DrawWireCircle(new Vector3(ECSWorldInterface.position.x, i * scale, ECSWorldInterface.position.y), radius);

            if (++range % 5 == 0)
            {
                range = 0;
                GizmosExtra.DrawWireCircle(new Vector3(ECSWorldInterface.position.x, i * scale, ECSWorldInterface.position.y), radius * tornadoRange);
            }
        }

        float groundAirDensity = TMath.AirDensity(groundTemperatureKelvin, dewPoint, TMath.AtmosphericPressure);
        float groundMaxWindSpeed = TMath.MaxWindSpeed(TMath.AtmosphericPressure, groundAirDensity, coreFunnelPressure);
        float groundMaxPressuresDeficit = TMath.MaxPressureDeficit(groundMaxWindSpeed, groundAirDensity);
        float groundRadius = TMath.CoreRadius(TMath.AtmosphericPressure, coreFunnelPressure, groundMaxPressuresDeficit, groundMaxWindSpeed, groundTemperatureKelvin) * width;


        if (showSpawnArea)
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            float height = spawnHeight.x;
            switch (spawnMode)
            {
                case SpawnMode.Circle:
                {
                    while (height++ < spawnHeight.y)
                    {
                        GizmosExtra.DrawWireCircle(
                            new Vector3(ECSWorldInterface.position.x, height, ECSWorldInterface.position.y),
                            groundRadius * tornadoRange * spawnRadius);
                    }

                    break;
                }
                case SpawnMode.Square:
                {
                    float halfHeight = math.lerp(height, spawnHeight.y, 0.5f);
                    Gizmos.DrawCube(new Vector3(ECSWorldInterface.position.x, halfHeight, ECSWorldInterface.position.y), new Vector3(groundRadius * tornadoRange * spawnRadius, halfHeight, groundRadius * tornadoRange * spawnRadius));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (showDespawnRange)
        {
            Gizmos.color = new Color(1, 0, 0);
            float absHeight = TMath.PressureToAltitude(coreFunnelPressure - groundMaxPressuresDeficit / 2, groundTemperatureKelvin) * despawnRange.y;
            for (int i = 0; i < absHeight; i += 10)
            {
                GizmosExtra.DrawWireCircle(new Vector3(ECSWorldInterface.position.x, i * scale, ECSWorldInterface.position.y), groundRadius * tornadoRange * despawnRange.x);
            }
        }
    }
    #endregion

    private class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            float groundTemperatureKelvin = TMath.CToKelvin(authoring.temperature);
            float coreFunnelPressure = TMath.CoreFunnelPressure(groundTemperatureKelvin, authoring.dewPoint);

            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Config
            {
                RunMode = authoring.runMode,
                SpawnMode = authoring.spawnMode,

                Pause = authoring.pause,
                Drop = authoring.drop,

                Seed = authoring.randomSeed,

                ParticlePrefab = GetEntity(authoring.particlePrefab, TransformUsageFlags.Dynamic),

                SpawnHeight = authoring.spawnHeight,

                Particles = authoring.particles,
                ParticleSizeRange = authoring.particleSizeRange,

                Scale = authoring.scale,
                UsedBlob = authoring.physicsCaching is PhysicsCache.CachedPhysics,

                TornadoBlob = TornadoBlob.CreateBlobAsset(
                    authoring.temperature,
                    authoring.dewPoint,
                    authoring.width,
                    authoring.tornadoRange,
                    authoring.despawnRange.y,
                    authoring.despawnRange.x,
                    authoring.spawnRadius,
                    authoring.pressureGradientCoefficient * (authoring.negativeCoefficients ? -1 : 1),
                    authoring.liftCoefficient * (authoring.negativeCoefficients ? -1 : 1),
                    authoring.windCoefficient * (authoring.negativeCoefficients ? -1 : 1),
                    authoring.dragCoefficient * (authoring.negativeCoefficients ? -1 : 1)
                ),
                GroundTemperatureKelvin = groundTemperatureKelvin,
                DewPoint = authoring.dewPoint,
                Width = authoring.width,
                CoreFunnelPressure = coreFunnelPressure,
            });
        }
    }
}
public enum RunMode { SingleThreaded, MultiThreaded }
public enum PhysicsCache { DynamicPhysics, CachedPhysics }
public enum SpawnMode { Circle, Square }

public struct Config : IComponentData
{
    public RunMode RunMode;
    public SpawnMode SpawnMode;

    public bool Pause;
    public bool Drop;

    public uint Seed;

    public Entity ParticlePrefab;

    public int Particles;
    public float2 ParticleSizeRange;

    public float Scale;
    public bool UsedBlob;

    public BlobAssetReference<TornadoBlob> TornadoBlob;

    public float2 SpawnHeight;

    public float GroundTemperatureKelvin;
    public float DewPoint;
    public float Width;
    public float CoreFunnelPressure;
}
