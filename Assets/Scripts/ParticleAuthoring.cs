using UnityEngine;
using Unity.Entities;

public class ParticleAuthoring : MonoBehaviour
{
    public float objectArea = 1;
    private class Baker : Baker<ParticleAuthoring>
    {
        public override void Bake(ParticleAuthoring authoring)
        {
            AddSharedComponent(GetEntity(authoring, TransformUsageFlags.Dynamic), new Particle
            {
                ObjectArea = authoring.objectArea,
            });
        }
    }
}

public struct Particle : ISharedComponentData
{
    public float ObjectArea;
}
