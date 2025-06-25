using UnityEngine;
using Unity.MLAgents; // solo se interagisci con agenti ML

public class WallCollision : MonoBehaviour {
    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("RedCar") || collision.gameObject.CompareTag("BlueCar")) {
            Debug.Log("Collisione col muro: " + collision.gameObject.name);
        }
    }
}
