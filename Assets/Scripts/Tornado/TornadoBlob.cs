using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TMath = Tornado.Math;

namespace Tornado
{
    [BurstCompile]
    public struct TornadoBlob
    {
        public int TornadoHeight;
        public float SpawnRadius;
        public float DespawnHeight;
        public float DespawnRadius;
        public float MouthHeight;

        public float Range;

        public float PressureGradientCoefficient;
        public float LiftCoefficient;
        public float WindCoefficient;
        public float DragCoefficient;

        public BlobArray<float> AirDensities;
        public BlobArray<float> MaxWindSpeeds;
        public BlobArray<float> MaxPressuresDeficits;
        public BlobArray<float> CoreRadii;

        public static BlobAssetReference<TornadoBlob> CreateBlobAsset(
            float temperature,
            float dewPoint,
            float widthScale = 1f,
            float range = 5f,
            float despawnHeight = 1.69f,
            float despawnRadius = 1.69f,
            float spawnRadius = 0.5f,
            float pressureGradientCoefficient = 2f,
            float liftCoefficient = 0.5f,
            float windCoefficient = 0.5f,
            float dragCoefficient = 0.5f
            )
        {
            using BlobBuilder builder = new(Allocator.Temp);
            ref TornadoBlob blob = ref builder.ConstructRoot<TornadoBlob>();

            float groundTemperatureKelvin = TMath.CToKelvin(temperature);
            float coreFunnelPressure = TMath.CoreFunnelPressure(groundTemperatureKelvin, dewPoint);

            float groundAirDensity = TMath.AirDensity(groundTemperatureKelvin, dewPoint, TMath.AtmosphericPressure);
            float maxWind = TMath.MaxWindSpeed(TMath.AtmosphericPressure, groundAirDensity, coreFunnelPressure);
            float maxPressureDifference = TMath.MaxPressureDeficit(maxWind, groundAirDensity);
            float tornadoHeight = TMath.PressureToAltitude(coreFunnelPressure, groundTemperatureKelvin);
            float absHeight = TMath.PressureToAltitude(coreFunnelPressure - maxPressureDifference / 2, groundTemperatureKelvin);

            blob.TornadoHeight = (int)math.floor(tornadoHeight);
            blob.MouthHeight = tornadoHeight * 0.05f;
            blob.DespawnHeight = absHeight * despawnHeight;

            blob.PressureGradientCoefficient = pressureGradientCoefficient;
            blob.LiftCoefficient = liftCoefficient;
            blob.WindCoefficient = windCoefficient;
            blob.DragCoefficient = dragCoefficient;

            BlobBuilderArray<float> airDensitiesArrayBuilder = builder.Allocate(ref blob.AirDensities, blob.TornadoHeight);
            BlobBuilderArray<float> maxPressuresDeficitsArrayBuilder = builder.Allocate(ref blob.MaxPressuresDeficits, blob.TornadoHeight);
            BlobBuilderArray<float> maxWindSpeedsArrayBuilder = builder.Allocate(ref blob.MaxWindSpeeds, blob.TornadoHeight);
            BlobBuilderArray<float> coreRadiiArrayBuilder = builder.Allocate(ref blob.CoreRadii, blob.TornadoHeight);

            for (int altitude = 0; altitude < blob.TornadoHeight; altitude++)
            {
                float altitudinalTemperature = TMath.TemperatureAtAltitude(altitude, groundTemperatureKelvin);
                float altitudinalDewPoint = TMath.DewPointAtAltitude(altitude, dewPoint);

                float pressure = TMath.AltitudeToPressure(altitude, groundTemperatureKelvin);
                float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
                float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, coreFunnelPressure);
                float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);

                airDensitiesArrayBuilder[altitude] = airDensity;
                maxWindSpeedsArrayBuilder[altitude] = maxWindSpeed;
                maxPressuresDeficitsArrayBuilder[altitude] = maxPressuresDeficit;
                coreRadiiArrayBuilder[altitude] = TMath.CoreRadius(pressure, coreFunnelPressure, maxPressuresDeficit, maxWindSpeed, groundTemperatureKelvin) * widthScale;
            }

            blob.Range = range * coreRadiiArrayBuilder[0];
            blob.SpawnRadius = blob.Range * spawnRadius;
            blob.DespawnRadius = blob.Range * despawnRadius;

            return builder.CreateBlobAssetReference<TornadoBlob>(Allocator.Persistent);
        }
    }
}