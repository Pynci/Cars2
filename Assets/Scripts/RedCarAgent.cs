using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class RedCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;
    public CheckpointManager checkpointManager;
    //public Raycast raycast;
    //public RayPerceptionSensorComponent3D rayCast;

    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;
    private float rayLength;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
        //raycast = GetComponent<Raycast>();
        //rayLength = Mathf.Max(raycast.rayLength, 0.01f);
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = new Vector3(-213.1f, 0f, 53f);
        transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /*for (int i = 0; i < raycast.rayDistances.Length; i++)
        {
            float dist = float.IsFinite(raycast.rayDistances[i]) ? Mathf.Clamp(raycast.rayDistances[i], 0f, rayLength) : rayLength;
            sensor.AddObservation(dist / rayLength);
            float angleNorm = raycast.rayAngles[i] / (raycast.angleSpan / 2f);
            sensor.AddObservation(angleNorm);
        }*/

        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        forwardSpeed = float.IsFinite(forwardSpeed) ? Mathf.Clamp(forwardSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(forwardSpeed / 20f);

        // Velocità laterale dell'agente
        float lateralSpeed = transform.InverseTransformDirection(rb.linearVelocity).x;
        lateralSpeed = float.IsFinite(lateralSpeed) ? Mathf.Clamp(lateralSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(lateralSpeed / 20f);

        // Distanza dall'opponent (normalizzata)
        float opponentDistance = Vector3.Distance(transform.position, opponent.position);
        opponentDistance = float.IsFinite(opponentDistance) ? Mathf.Clamp(opponentDistance, 0f, 100f) : 100f;
        sensor.AddObservation(opponentDistance / 100f);

        // Rotazione Y normalizzata
        float yaw = (transform.eulerAngles.y % 360f + 360f) % 360f;
        sensor.AddObservation(yaw / 360f);

        // Distanza dal checkpoint target (normalizzata)
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float checkpointDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        checkpointDistance = float.IsFinite(checkpointDistance) ? Mathf.Clamp(checkpointDistance, 0f, 100f) : 100f;
        sensor.AddObservation(checkpointDistance / 100f);

        // Direzione verso il checkpoint target (in coordinate locali)
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDirection.x);
        sensor.AddObservation(localDirection.z);

        // Rotazione dell'agente (quaternion)
        sensor.AddObservation(transform.localRotation);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];
        if (!float.IsFinite(accel) || !float.IsFinite(steer) || !float.IsFinite(brake)) return;

        car.Move(accel, steer, brake);

        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float newDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDistanceToCheckpoint - newDist;
        AddReward(delta * 0.1f);
        lastDistanceToCheckpoint = newDist;

        if (lastDistanceToCheckpoint < 5f)
        {
            AddReward(5.0f);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;
            if (nextCheckpointIndex == 0)
            {
                AddReward(10f);
                EndEpisode();
            }
        }

        // Penalità per lentezza
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed < 0.1f)
            AddReward(-0.01f);
        else
            AddReward(2.5f * speed);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("bulkheads")) { AddReward(-10f); EndEpisode(); }
        else if (col.gameObject.CompareTag("BlueCar") || col.gameObject.CompareTag("RedCar")) AddReward(-5f);
    }
}