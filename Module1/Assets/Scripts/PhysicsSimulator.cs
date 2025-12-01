using Mirror;
using TMPro;
using UnityEngine;

/// <summary>
/// Add this script to all scenes we want to run.
/// </summary>
public class PhysicsSimulator : MonoBehaviour
{
    PhysicsScene _physics;
    bool _simulatePhysics;

    void Awake()
    {
        // Get physics scene and check whether it should be simulated or not
        if (NetworkServer.active)
        {
            _physics = gameObject.scene.GetPhysicsScene();
            _simulatePhysics = _physics.IsValid() && _physics != Physics.defaultPhysicsScene;
        }
    }

    void FixedUpdate()
    {
        if (!NetworkServer.active) return;

        if (_simulatePhysics)
            _physics.Simulate(Time.fixedDeltaTime);
    }
}
