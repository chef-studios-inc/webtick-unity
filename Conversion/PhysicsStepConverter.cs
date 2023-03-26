using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace WebTick.Conversion
{
    public class PhysicsStepConverter : MonoBehaviour, IConverter
    {
        public void Convert(IConverter.ConvertParams p)
        {
            var physicsStepAuthoring = p.go.GetComponent<PhysicsStepAuthoring>();
            var result = default(PhysicsStep);
            result.SimulationType = physicsStepAuthoring.SimulationType;
            result.Gravity = physicsStepAuthoring.Gravity;
            result.SolverIterationCount = physicsStepAuthoring.SolverIterationCount;
            result.SolverStabilizationHeuristicSettings = (physicsStepAuthoring.EnableSolverStabilizationHeuristic ? new Solver.StabilizationHeuristicSettings
            {
                EnableSolverStabilization = true,
                EnableFrictionVelocities = PhysicsStep.Default.SolverStabilizationHeuristicSettings.EnableFrictionVelocities,
                VelocityClippingFactor = PhysicsStep.Default.SolverStabilizationHeuristicSettings.VelocityClippingFactor,
                InertiaScalingFactor = PhysicsStep.Default.SolverStabilizationHeuristicSettings.InertiaScalingFactor
            } : Solver.StabilizationHeuristicSettings.Default);
            result.MultiThreaded = (byte)(physicsStepAuthoring.MultiThreaded ? 1u : 0u);
            result.SynchronizeCollisionWorld = (byte)(physicsStepAuthoring.SynchronizeCollisionWorld ? 1u : 0u);

            p.entityManager.AddComponentData(p.entity, result);
        }
    }

}
