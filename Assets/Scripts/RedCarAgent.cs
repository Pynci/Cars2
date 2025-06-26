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
    private float steerToDirection = 0f; // Steering verso direzione libera

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

        transform.position = new Vector3(-213.1f, 0.1f, -25f);
        transform.rotation = Quaternion.identity;

        nextCheckpointIndex = 0;
        lastDistanceToCheckpoint = float.MaxValue;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Raycast
        foreach (float distance in raycast.rayDistances)
        {
            sensor.AddObservation(distance / raycast.rayLength);
        }

        // 2. Velocità avanti normalizzata (max 20)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        sensor.AddObservation(forwardSpeed / 20f);

        // 3. Distanza dall'avversario (max 100)
        float opponentDistance = Vector3.Distance(transform.position, opponent.position);
        sensor.AddObservation(opponentDistance / 100f);

        // 4. Rotazione Y normalizzata
        float rotationY = transform.eulerAngles.y / 360f;
        sensor.AddObservation(rotationY);

        // 5. Calcolo direzione sicura
        steerToDirection = raycast.BestRayAngle / (raycast.angleSpan / 2f); // Normalizzato tra -1 e 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steerRL = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);

        // Steering combinato: direzione sicura + RL
        float steer = Mathf.Clamp(0.7f * steerToDirection + 0.3f * steerRL, -1f, 1f);

        car.Move(accel, steer, brake);

        // Ricompensa per avvicinamento al checkpoint
        Transform targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float distance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDistanceToCheckpoint - distance;
        AddReward(delta * 0.05f); // premio se si avvicina
        lastDistanceToCheckpoint = distance;

        if (distance < 5f)
        {
            AddReward(2.0f);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;
            lastDistanceToCheckpoint = float.MaxValue;
        }

        // Penalità per lentezza
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed < 0.1f)
            AddReward(-0.02f);
        else
            AddReward(1.5f * speed);

        // Penalità costante per evitare attesa
        AddReward(-0.05f);

        // Penalità per vicinanza a ostacoli
        foreach (float d in raycast.rayDistances)
        {
            if (d < raycast.rayLength * 0.2f)
                AddReward(-0.05f);
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

