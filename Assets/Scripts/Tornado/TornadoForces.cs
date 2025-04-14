using Unity.Burst;
using Unity.Mathematics;

namespace Tornado
{
    [BurstCompile]
    public static class Forces
    {
        [BurstCompile]
        public static void WindForces(
            out float3 forces,
            float airDensity,
            in float3 wind,
            in float3 linearVelocity,
            float surfaceArea,
            float liftCoefficient = 0.5f,
            float windCoefficient = 0.5f,
            float dragCoefficient = 0.5f
            )
        {
            float3 liftForce, dragForce, windForce;

            float linearVelocitySq = math.lengthsq(linearVelocity);
            if (linearVelocitySq > math.EPSILON)
            {
                float windPressureValue = 0.5f * airDensity * linearVelocitySq * surfaceArea;
                // Lift force
                float liftForceMagnitude = windPressureValue * liftCoefficient;
                liftForce = new float3(0, liftForceMagnitude, 0);

                // Drag force
                float3 dragDirection = -math.normalizesafe(linearVelocity);
                float dragForceMagnitude = windPressureValue * dragCoefficient;
                dragForce = dragDirection * dragForceMagnitude;
            }
            else
            {
                liftForce = float3.zero;
                dragForce = float3.zero;
            }

            // Wind force
            float windSpeedSq = math.lengthsq(wind);
            if (windSpeedSq > math.EPSILON)
            {
                float3 windDirection = math.normalizesafe(wind);
                float windForceMagnitude = 0.5f * airDensity * windSpeedSq * windCoefficient * surfaceArea;
                windForce = windDirection * windForceMagnitude;
            }
            else
            {
                windForce = float3.zero;
            }

            // Total force acting on the object
            forces = liftForce + dragForce + windForce ;
        }
        [BurstCompile]
        public static void PressureForces(out float3 force, float pressureDeficit, float distance, in float3 direction, float pressureGradientCoefficient = 2f)
        {
            force = (pressureDeficit / distance) * pressureGradientCoefficient * direction;
        }
    }
}