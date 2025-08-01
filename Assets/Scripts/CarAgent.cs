﻿using System.Xml.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;

public class CarAgent : Agent
{
    private Rigidbody rb;
    private CarController controller;
    private CheckpointManager checkpointManager;
    private RaceManager raceManager;

    [Header("Agent Settings")]
    public float maxSpeed = 20f;
    public Transform nextCheckpoint;
    public int nextCheckpointIndex;

    [Header("Rewards (hardcoded)")]
    
    private const float collisionPenalty = -1.5f;
    private const float idlePenaltyPerSecond = -0.005f;
    private const float lapCompletedReward = 1.0f;
    private const float timePenalty = -0.2f;
    private float speedReward = 0.2f;

    [Header("Idle Timeout")]
    private float maxIdleTime = 10f;

    [Header("Progress Smoothing")]
    [Range(0f, 1f)] public float smoothingAlpha = 0.2f;

    private float idleTimer = 0f;
    private const int maxLap = 2;
    private int lap = 0;
    private bool isRespawn = false;
    public int lastRank = -1; // -1 iniziale: mai classificato

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
        raceManager = FindFirstObjectByType<RaceManager>();
        SetIsRespawn(false);

        /*
        if(raceManager.spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            speedReward = 0.02f;
        } else
        {
            speedReward = 0.2f;
        }
        */
    }

    public void SetRaceManager(RaceManager rm)
    {
        raceManager = rm;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lap = 0;
        lastRank = -1;

        if (isRespawn)
        {
            SetIsRespawn(false);
            Transform respawn;
            respawn = raceManager.RespawnAgent();
            

            transform.position = respawn.position;
            transform.rotation = respawn.rotation;
        }

            var (cp, idx) = checkpointManager.DetectNextCheckpointWithIndex(this);
        nextCheckpoint = cp;
        nextCheckpointIndex = idx;
    }

    public void AddLap()
    {
        lap++;
        if(lap >= 1)
        {
            AddReward(lapCompletedReward);
        }
        if (raceManager != null && raceManager.spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            if (lap == maxLap)
            {
                SetIsRespawn(true);
                raceManager.NotifyMaxLapReached(this);
            }
        }
    }

    public void SetIsRespawn(bool value)
    {
        isRespawn = value;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direzione verso il checkpoint
        Vector3 dir = (nextCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        // Velocità localizzata
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxSpeed);
        sensor.AddObservation(localVel.x / (maxSpeed * 0.5f));

        // Progresso sui checkpoint
        float progress = checkpointManager.GetCurrentCheckpointIndex(this) / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

        float distToCheckpoint = Vector3.Distance(transform.position, nextCheckpoint.position) / 100f;
        sensor.AddObservation(distToCheckpoint);
        //sensor.AddObservation((float)nextCheckpointIndex / checkpointManager.TotalCheckpoints);

        // direzione dell’auto rispetto al checkpoint
        float facing = Vector3.Dot(transform.forward, dir);
        sensor.AddObservation(facing); 
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float motor = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];
        
        controller.Move(motor, steer, brake);

        var (cp, idx) = checkpointManager.DetectNextCheckpointWithIndex(this);
        nextCheckpoint = cp;
        nextCheckpointIndex = idx;

        checkpointManager.EvaluateCheckpointProgress(this, raceManager.spawnManager.trainingPhase);

        if (rb.linearVelocity.magnitude < 1f)
        {
            idleTimer += Time.fixedDeltaTime;
            float idlePenalty = idlePenaltyPerSecond * idleTimer;
            AddReward(idlePenalty);
            if (idleTimer > maxIdleTime)
            {
                AddReward(timePenalty);
                SetIsRespawn(true);
                idleTimer = 0f;
                EndEpisode();
            }
        }
        else
        {
            idleTimer = 0f;
        }

        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed > 0.1f)
            AddReward(speed * speedReward);

        // Solo in fase di gara
        if (raceManager != null && raceManager.spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            raceManager.UpdateRaceProgress();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            SetIsRespawn(true);
            EndEpisode();
        }

        else if (collision.gameObject.CompareTag("Car"))
        {
            AddReward(collisionPenalty);

            Rigidbody otherRb = collision.rigidbody;
            if (otherRb != null)
            {
                Vector3 relativeVelocity = rb.linearVelocity - otherRb.linearVelocity;
                float impactForce = Vector3.Dot(relativeVelocity.normalized, transform.forward);

                // Se l'agente stava "andando addosso all'altro" con sufficiente velocità
                if (impactForce > 0.3f) // soglia regolabile
                {
                    AddReward(collisionPenalty);
                    SetIsRespawn(true);
                    EndEpisode();
                }
            }
        }
    }
}
