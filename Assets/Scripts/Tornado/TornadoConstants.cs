namespace Tornado
{
    public static partial class Math
    {
        // universal constants
        const float GasConstant = 8.31436f; // (m³•Pa•K⁻¹•mol⁻¹)

        // earthly constants
        public const float Gravity = 9.80665f; // (m/s²)
        public const float AtmosphericPressure = 101325f; // (Pa)

        // gas constants
        const float DryAirMolarMass = 0.0289644f; // (kg/mol)
        const float WaterVaporMolarMass = 0.01802f; // (kg/mol)

        const float TemperatureLapseRate = 0.0065f; // (C°/m)
        const float TemperatureDewLapseRate = 0.0018f; // (C°/m)

        const float AirGasConstant = GasConstant / DryAirMolarMass; // (J•kg⁻¹•K⁻¹)
        const float VaporGasConstant = GasConstant / WaterVaporMolarMass; // (J•kg⁻¹•K⁻¹)

        const float DryAirHeatCapacity = 1003.5f; // (J/(kg•K))
        const float DryAirHeatCapacityRatio = DryAirHeatCapacity / AirGasConstant; // (dimensionless)

        // inferred gas constants
        const float PressureExponent = Gravity * DryAirMolarMass / (GasConstant * TemperatureLapseRate); // (dimensionless)
        const float InvPressureExponent = 1f / PressureExponent; // (dimensionless)

        // https://doi.org/10.1175/1520-0450(1967)006%3C0203:OTCOSV%3E2.0.CO;2
        const float ReferenceVaporPressure = 610.78f; // (Pa)
        const float SaturationVaporPressureGrowthFactor = 21.875f; // (dimensionless)
        const float VaporNonLinearityOffset = 265.5f; // (C°)
    }
}