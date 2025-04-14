using Unity.Burst;
using Unity.Mathematics;

namespace Tornado
{
    /// <summary>
    /// A static class to contain various tornado related math functions and constants.
    /// </summary>
    [BurstCompile]
    public static partial class Math
    {
        [BurstCompile]
        public static float CToKelvin(float celsius) => celsius + 273.13f;
        [BurstCompile]
        public static float TemperatureAtAltitude(float altitude, float groundTemperature) =>
            groundTemperature - TemperatureLapseRate * altitude;
        [BurstCompile]
        public static float DewPointAtAltitude(float altitude, float groundTempDewPoint) =>
            groundTempDewPoint - TemperatureDewLapseRate * altitude;
        [BurstCompile]
        public static float AltitudeToPressure(float altitude, float groundTemperature) =>
            AtmosphericPressure * math.pow(1 - TemperatureLapseRate * altitude/groundTemperature, PressureExponent);
        [BurstCompile]
        public static float AirDensity(float altitudinalTemperature, float altitudinalDewPoint, float absolutePressure)
        {
            float vaporPressure = VaporPressure();
            float dryAirPressure = absolutePressure - vaporPressure;

            float vaporDensity = vaporPressure / (VaporGasConstant * altitudinalTemperature);
            float dryAirDensity = dryAirPressure / (AirGasConstant * altitudinalTemperature);
            return dryAirDensity + vaporDensity;

            // Tetens' equation
            float VaporPressure() => ReferenceVaporPressure * math.exp(
                SaturationVaporPressureGrowthFactor * altitudinalDewPoint /
                (altitudinalDewPoint + VaporNonLinearityOffset)
                );
        }
        [BurstCompile]
        public static float CoreFunnelPressure(float groundTemp, float groundDewPoint)
        {
            float airDensity = AirDensity(groundTemp, groundDewPoint, AtmosphericPressure);
            return AtmosphericPressure * math.pow(1 - airDensity * (groundTemp - CToKelvin(groundDewPoint)) / groundTemp, DryAirHeatCapacityRatio);
        }
        [BurstCompile]
        public static float PressureToAltitude(float pressure, float groundTemperature) =>
            (1 - math.pow(pressure / AtmosphericPressure, InvPressureExponent)) * groundTemperature / TemperatureLapseRate;
        [BurstCompile]
        public static float MaxWindSpeed(float altitudinalPressure, float altitudinalAirDensity, float coreFunnelPressure) => 
            math.sqrt((altitudinalPressure - coreFunnelPressure) / altitudinalAirDensity);

        [BurstCompile]
        public static float WindSpeed(float maxWindSpeed, float radio) =>
            maxWindSpeed * radio;
        [BurstCompile]
        public static float MaxPressureDeficit(float maxWindSpeed, float altitudinalAirDensity)
        {
            return altitudinalAirDensity * maxWindSpeed * maxWindSpeed;
        }

        [BurstCompile]
        public static float PressureDeficit(float maxPressureDeficit, float ratio, bool insideCore)
        {
            ratio *= ratio;
            ratio *= 0.5f;
            if (insideCore) ratio = 1 - ratio;
            return maxPressureDeficit * ratio;
        }
        [BurstCompile]
        public static float CoreRadius(float altitudinalPressure, float pcf, float maxAltitudinalPressureDeficit, float maxAltitudinalWindSpeed, float groundTemperature) =>
            math.sqrt(2 * (pcf + maxAltitudinalPressureDeficit) / altitudinalPressure) / maxAltitudinalWindSpeed * PressureToAltitude(pcf - 0.5f * maxAltitudinalPressureDeficit, groundTemperature);

    }
}