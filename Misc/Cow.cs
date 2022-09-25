using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cow : MonoBehaviour {
    public Rigidbody2D rb;
    public GameObject eye;
    public GameObject eyeAnchor;
    const float MAX_SPEED = 30;

    void Start() {
        GameController.RandomizeRotation(gameObject);
        GameController.RandomizeTorque(rb, 4, 8);
    }
    void Update() {
        if (PlayerController.Player == null)
            return;

        // Look at player
        Vector2 playerDirection = PlayerController.Player.transform.position - eye.transform.position;
        eye.transform.position = (Vector2)eyeAnchor.transform.position + playerDirection.normalized;
    }
    private void FixedUpdate() {
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, MAX_SPEED);
    }
    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player"))
            PlayerController.Player.CollectCow(gameObject);

        if (collision.gameObject.CompareTag("LassoLoop"))
            PlayerController.Player.LassoCow(rb);
    }
}
