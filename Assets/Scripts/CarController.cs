using UnityEngine;

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
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeForce = 3000f;

    public void Move(float motorInput, float steeringInput, float brakeInput)
    {
        float motor = Mathf.Clamp(motorInput, -1f, 1f) * maxMotorTorque;
        float steering = Mathf.Clamp(steeringInput, -1f, 1f) * maxSteeringAngle;
        float brake = Mathf.Clamp(brakeInput, 0f, 1f) * brakeForce;

        // Steering
        frontLeftCollider.steerAngle = steering;
        frontRightCollider.steerAngle = steering;

        // Motor
        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;

        // Brake
        frontLeftCollider.brakeTorque = brake;
        frontRightCollider.brakeTorque = brake;
        rearLeftCollider.brakeTorque = brake;
        rearRightCollider.brakeTorque = brake;

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
