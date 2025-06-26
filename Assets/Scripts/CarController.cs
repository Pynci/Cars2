using UnityEngine;
using UnityEngine.UIElements;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    public float maxMotorTorque;
    public float maxSteeringAngle;
    public float brakeForce;

    public void Move(float motorInput, float steeringInput, float brakeInput)
    {
        float motor = Mathf.Clamp(motorInput, 0f, 1f) * maxMotorTorque;
        float steering = Mathf.Clamp(steeringInput, -1f, 1f) * maxSteeringAngle;
        float brake = Mathf.Clamp(brakeInput, 0f, 1f) * brakeForce;

        // Steering
        frontLeftCollider.steerAngle = steering;
        frontRightCollider.steerAngle = steering;

        // Motor
        //frontLeftCollider.motorTorque = motor;
        //frontRightCollider.motorTorque = motor;
        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;


        // Brake
        frontLeftCollider.brakeTorque = brake * 0.55f;
        frontRightCollider.brakeTorque = brake * 0.55f;
        rearLeftCollider.brakeTorque = brake * 0.45f;
        rearRightCollider.brakeTorque = brake * 0.45f;

        // Sync mesh
        UpdateWheelPose(frontLeftCollider, frontLeftMesh);
        UpdateWheelPose(frontRightCollider, frontRightMesh);
        UpdateWheelPose(rearLeftCollider, rearLeftMesh);
        UpdateWheelPose(rearRightCollider, rearRightMesh);


    }

    void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
}