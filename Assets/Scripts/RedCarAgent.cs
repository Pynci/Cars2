using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(CarController))]
public class RedCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = new Vector3(-213f, 0f, -30f);
        transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(rb.linearVelocity));
        sensor.AddObservation(Vector3.Distance(transform.position, opponent.position));
        sensor.AddObservation(transform.localRotation);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        car.Move(accel, steer, brake);
        Debug.Log($"Accel: {accel}, Steer: {steer}, Brake: {brake}");


        AddReward(0.01f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }
}
