using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class BlueCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;
    public CheckpointManager checkpointManager;
    public Raycast raycast;

    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;
    private float rayLength;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
        raycast = GetComponent<Raycast>();
        rayLength = Mathf.Max(raycast.rayLength, 0.01f);
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = new Vector3(-207.7f, 0f, 53f);
        transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        for (int i = 0; i < raycast.rayDistances.Length; i++)
        {
            float dist = float.IsFinite(raycast.rayDistances[i]) ? Mathf.Clamp(raycast.rayDistances[i], 0f, rayLength) : rayLength;
            sensor.AddObservation(dist / rayLength);
            float angleNorm = raycast.rayAngles[i] / (raycast.angleSpan / 2f);
            sensor.AddObservation(angleNorm);
        }

        float fs = transform.InverseTransformDirection(rb.linearVelocity).z;
        fs = float.IsFinite(fs) ? Mathf.Clamp(fs, -20f, 20f) : 0f;
        sensor.AddObservation(fs / 20f);

        float od = Vector3.Distance(transform.position, opponent.position);
        od = float.IsFinite(od) ? Mathf.Clamp(od, 0f, 100f) : 100f;
        sensor.AddObservation(od / 100f);

        float yaw = (transform.eulerAngles.y % 360f + 360f) % 360f;
        sensor.AddObservation(yaw / 360f);

        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float cd = Vector3.Distance(transform.position, targetCheckpoint.position);
        cd = float.IsFinite(cd) ? Mathf.Clamp(cd, 0f, 100f) : 100f;
        sensor.AddObservation(cd / 100f);

        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDir.x);
        sensor.AddObservation(localDir.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float rawA = actions.ContinuousActions[0];
        float rawS = actions.ContinuousActions[1];
        float rawB = actions.ContinuousActions[2];
        if (!float.IsFinite(rawA) || !float.IsFinite(rawS) || !float.IsFinite(rawB)) return;

        float accel = Mathf.Clamp(rawA, -1f, 1f);
        float steer = Mathf.Clamp(rawS, -1f, 1f);
        float brake = Mathf.Clamp01(rawB);
        car.Move(accel, steer, brake);

        float newDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDistanceToCheckpoint - newDist;
        AddReward(delta * 0.1f);
        lastDistanceToCheckpoint = newDist;

        if (newDist < 5f)
        {
            AddReward(5f);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;
            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
            lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        }

        float desired = raycast.BestRayAngle / (raycast.angleSpan / 2f);
        AddReward(0.2f * (1f - Mathf.Abs(steer - desired)));

        foreach (float d in raycast.rayDistances)
        {
            float pen = float.IsFinite(d) ? Mathf.Clamp01((rayLength - d) / rayLength) : 0f;
            AddReward(-pen * 0.05f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.LeftArrow) ? -1f : Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("bulkheads")) { AddReward(-10f); EndEpisode(); }
        else if (col.gameObject.CompareTag("BlueCar") || col.gameObject.CompareTag("RedCar")) AddReward(-5f);
    }
}