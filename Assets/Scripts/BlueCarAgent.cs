using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(CarController))]
public class BlueCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;
    public CheckpointManager checkpointManager;
    private int nextCheckpointIndex = 0;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = new Vector3(-207f, 0f, -25f);
        transform.rotation = Quaternion.identity;

        nextCheckpointIndex = 0;
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

        // Ricompensa per avanzamento continuo
        AddReward(0.01f);

        // Ricompensa se raggiunge il checkpoint
        Transform targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        if (Vector3.Distance(transform.position, targetCheckpoint.position) < 5f)
        {
            AddReward(1.0f);
            nextCheckpointIndex++;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.LeftArrow) ? -1f : Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("BLU " + collision.gameObject.tag);
        if (collision.gameObject.CompareTag("bulkheads") || collision.gameObject.CompareTag("RedCar"))
        {
            Debug.Log("blue dentro l'if");
            AddReward(-3.0f);
            EndEpisode();
            
        }
    }
}
