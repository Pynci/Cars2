using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider;
    [Header("Wheel Meshes")]
    public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;
    [Header("Car Settings")]
    public float maxMotorTorque = 8000f;
    public float maxSteeringAngle = 30f;
    public float brakeForce = 10000f;
    public float engineBrakeForce = 150f;

    public void Move(float motorInput, float steeringInput, float brakeInput)
    {
        float motor = Mathf.Clamp(motorInput, 0f, 1f) * maxMotorTorque;
        float steer = Mathf.Clamp(steeringInput, -1f, 1f) * maxSteeringAngle;
        float brake = Mathf.Clamp01(brakeInput) * brakeForce;
        float engineBrake = motorInput < 0.01f ? engineBrakeForce : 0f;

        // Sterzo
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;

        // Motore
        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;

        // Freno + engine brake
        rearLeftCollider.brakeTorque = brake * 0.45f + engineBrake * 0.45f;
        rearRightCollider.brakeTorque = brake * 0.45f + engineBrake * 0.45f;
        frontLeftCollider.brakeTorque = brake * 0.55f + engineBrake * 0.55f;
        frontRightCollider.brakeTorque = brake * 0.55f + engineBrake * 0.55f;

        UpdateWheelPose(frontLeftCollider, frontLeftMesh);
        UpdateWheelPose(frontRightCollider, frontRightMesh);
        UpdateWheelPose(rearLeftCollider, rearLeftMesh);
        UpdateWheelPose(rearRightCollider, rearRightMesh);
    }

    void UpdateWheelPose(WheelCollider col, Transform mesh)
    {
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
