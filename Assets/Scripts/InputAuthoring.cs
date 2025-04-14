using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class InputAuthoring : MonoBehaviour
{
    public float2 mousePosition;
}

class InputDataBaker : Baker<InputAuthoring>
{
    public override void Bake(InputAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new InputData
        {
            MousePosition = authoring.mousePosition,
        });
    }
}

public struct InputData : IComponentData
{
    public float2 MousePosition;
}
