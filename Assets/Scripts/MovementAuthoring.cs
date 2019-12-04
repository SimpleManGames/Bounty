using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public struct Movement : IComponentData
{
    public float speed;
    public float jumpHeight;
}

public class MovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpHeight;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Movement()
        {
            speed = this.speed,
            jumpHeight = jumpHeight
        });
    }
}

public class MovementSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulation;

    protected override void OnCreate()
    {
        endSimulation = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    private struct MovementJob : IJobForEachWithEntity<Movement, Translation>
    {
        private float deltaTime;
        private EntityCommandBuffer.Concurrent endCommandBuffer;

        public MovementJob(EntityCommandBuffer.Concurrent ecb, float deltaTime)
        {
            this.deltaTime = deltaTime;
            endCommandBuffer = ecb;
        }

        public void Execute(Entity entity, int index, ref Movement movement, ref Translation translation)
        {
            Move(entity, index, movement, translation);
        }

        public void Jump(Movement movement, Translation translation)
        {
            translation.Value.y += movement.jumpHeight;
        }

        private void Move(Entity entity, int index, Movement movement, Translation translation)
        {
            Vector2 movementInput = new Controls().Player.Movement.ReadValue<Vector2>();
            Vector3 vecMovement = new Vector3()
            {
                x = movementInput.x,
                z = movementInput.y
            }.normalized;

            endCommandBuffer.SetComponent(index, entity, new Translation
            {
                Value = (Vector3)translation.Value + vecMovement * movement.speed * deltaTime
            });
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new MovementJob(endSimulation.CreateCommandBuffer().ToConcurrent(), Time.deltaTime);

        JobHandle jobHandle = job.Schedule(this, inputDeps);
        endSimulation.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }
}
