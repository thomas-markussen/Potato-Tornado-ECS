using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
public partial struct CameraSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InputData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetMouseButton(0)) return;

        var config = SystemAPI.GetSingleton<InputData>();

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, 10000))
        {
            float2 pos = new float2(hit.point.x, hit.point.z);

            var idEntity = SystemAPI.GetSingletonEntity<InputData>();
            state.EntityManager.SetComponentData(idEntity, new InputData
            {
                MousePosition = pos,
            });
            // Yes, don't do this.
            ECSWorldInterface.position = pos;
        }
    }
}

