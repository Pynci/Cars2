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
    public CheckpointManager checkpointManager;
    private int nextCheckpointIndex = 0;
    public Raycast raycast;

    private float lastDistanceToCheckpoint = float.MaxValue;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
        raycast = GetComponent<Raycast>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Posizionamento iniziale sicuro e coerente
        transform.position = new Vector3(-213.1f, 0.1f, -25f);
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        nextCheckpointIndex = 0;
        lastDistanceToCheckpoint = float.MaxValue;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Osservazioni da Raycast
        foreach (float distance in raycast.rayDistances)
        {
            sensor.AddObservation(distance / raycast.rayLength);
        }

        // 2. Velocit� lungo l'asse Z locale (avanti)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        sensor.AddObservation(forwardSpeed / 20f);

        // 3. Distanza normalizzata dall'avversario (assumendo max 100 unit�)
        float opponentDistance = Vector3.Distance(transform.position, opponent.position);
        sensor.AddObservation(opponentDistance / 100f);

        // 4. Rotazione Y normalizzata
        float rotationY = transform.eulerAngles.y / 360f;
        sensor.AddObservation(rotationY);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);

        car.Move(accel, steer, brake);

        // Checkpoint logic
        Transform targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float distance = Vector3.Distance(transform.position, targetCheckpoint.position);

        float delta = lastDistanceToCheckpoint - distance;
        AddReward(delta * 0.05f); // Reward per avvicinamento
        lastDistanceToCheckpoint = distance;

        if (distance < 5f)
        {
            AddReward(2.0f);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;
            lastDistanceToCheckpoint = float.MaxValue;
        }

        // Penalita se troppo lento
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed < 0.1f)
            AddReward(-0.02f);
        else
            AddReward(0.5f * speed); // Premio per muoversi

        // Penalita tempo
        AddReward(-0.001f);

        // Penalita per ostacoli vicini tramite Raycast
        foreach (float d in raycast.rayDistances)
        {
            if (d < raycast.rayLength * 0.2f)
                AddReward(-0.01f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ROSSA " + collision.gameObject.tag);
        if (collision.gameObject.CompareTag("bulkheads") || collision.gameObject.CompareTag("BlueCar"))
        {
            Debug.Log("rossa dentro l'if");
            AddReward(-3.0f);
            EndEpisode();
        }
    }
}
