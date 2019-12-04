using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Only data for the Rotate Component. No functionality 
public struct Rotate : IComponentData
{
    public float radiansPerSecond;
}

// Actual component that gets added to Entities
public class RotateAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // Editable inside of Unity3D inspector
    [SerializeField]
    private float degreesPerSecond;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Rotate() { radiansPerSecond = math.radians(degreesPerSecond) });
        dstManager.AddComponentData(entity, new RotationEulerXYZ());
    }
}

// System to propagate updated the Rotate Component
public class RotateSystem : JobComponentSystem
{
    // Thread Job for Rotate
    [BurstCompile]
    private struct RotateJob : IJobForEach<Rotate, RotationEulerXYZ>
    {
        // We pass in deltaTime because we can't access Time.deltaTime
        // while not on main thread
        public float deltaTime;

        public void Execute(ref Rotate rotate, ref RotationEulerXYZ eulerXYZ)
        {
            eulerXYZ.Value.y += rotate.radiansPerSecond * deltaTime;
        }
    }

    // Propagates the Job
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new RotateJob { deltaTime = Time.deltaTime };
        return job.Schedule(this, inputDeps);
    }
}