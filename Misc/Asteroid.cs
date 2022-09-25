using UnityEngine;

public class Asteroid : MonoBehaviour {
    public Rigidbody2D rb;
    const float ANGULAR_VELOCITY = 30;
    const float ASTEROID_BOUNCE_FORCE = 50;

    void Start() {
        GameController.RandomizeRotation(gameObject);
        GameController.RandomizeTorque(rb, 300, 500);
    }
    void FixedUpdate() {
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -ANGULAR_VELOCITY, ANGULAR_VELOCITY);    
    }
    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player"))
            PlayerController.Player.rb.AddForce((PlayerController.Player.transform.position - transform.position).normalized * ASTEROID_BOUNCE_FORCE, ForceMode2D.Impulse);
    }
    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("LassoLoop"))
            PlayerController.Player.LassoAsteroid(rb);
    }
}
