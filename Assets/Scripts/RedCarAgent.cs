using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RedCarAgent : Agent
{
    Rigidbody rb;
    public Transform opponent;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = new Vector3(-293.5f, 0f, -30f); // partenza rossa
        transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(rb.linearVelocity));
        sensor.AddObservation(Vector3.Distance(transform.position, opponent.position));
        sensor.AddObservation(transform.localRotation);
        Debug.Log("Opponent is at: " + opponent.position);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp(actions.ContinuousActions[2], 0f, 1f);

        rb.AddForce(transform.forward * accel * 100f, ForceMode.Force);
        transform.Rotate(Vector3.up, steer * 2f);
        if (brake > 0.1f)
            rb.linearVelocity *= 0.9f;

        // Reward personalizzata per RedCar
        AddReward(0.01f); // base reward
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.S) ? 1f : 0f;
    }
}
