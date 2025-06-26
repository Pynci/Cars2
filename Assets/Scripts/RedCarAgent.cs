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
    private bool checkpointReached = false;

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

        transform.position = new Vector3(-213.1f, 0f, -23f);
        transform.rotation = Quaternion.identity;

        nextCheckpointIndex = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (float distance in raycast.rayDistances)
        {
            sensor.AddObservation(distance / raycast.rayLength);
        }

        // 1. Velocità lungo l'asse locale Z (in avanti)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        sensor.AddObservation(forwardSpeed / 20f); // Normalizza su una velocità massima attesa

        // 2: Distanza normalizzata dall'avversario (assumendo max 100 unità)
        float opponentDistance = Vector3.Distance(transform.position, opponent.position);
        sensor.AddObservation(opponentDistance / 100f); // Normalizzata tra 0 e 1

        // 3: Rotazione Y normalizzata
        float rotationY = transform.eulerAngles.y / 360f;
        sensor.AddObservation(rotationY);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        car.Move(accel, steer, brake);

        // Ricompensa per avanzamento continuo
        AddReward(0.1f);

        // Ricompensa se raggiunge il checkpoint
        Transform targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);

        float distance = Vector3.Distance(transform.position, targetCheckpoint.position);
        if (!checkpointReached && distance < 5f)
        {
            AddReward(1.0f);
            Debug.Log("Checkpoint raggiunto: " + targetCheckpoint.name);
            checkpointReached = true;
            nextCheckpointIndex++;
        }

        // Se l'agente si allontana dal checkpoint, resetta lo stato per poter raggiungere il prossimo reward
        if (checkpointReached && distance > 5f)
        {
            checkpointReached = false;
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bulkheads")){
            AddReward(-1.0f);
        }

        if (collision.gameObject.CompareTag("BlueCar"))
        {
            AddReward(-1.0f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    //mlagents-learn race_competition.yaml --run-id competizione_race
}
