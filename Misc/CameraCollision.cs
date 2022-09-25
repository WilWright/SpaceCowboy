using UnityEngine;

public class CameraCollision : MonoBehaviour {
    void OnTriggerEnter2D(Collider2D collision) {
        if (GameController.cacti == null)
            return;

        if (collision.gameObject.CompareTag("Cactus"))
            GameController.cacti[collision.gameObject].spriteRenderer.enabled = true;
    }
    void OnTriggerExit2D(Collider2D collision) {
        if (GameController.cacti == null)
            return;

        if (collision.gameObject.CompareTag("Cactus"))
            GameController.cacti[collision.gameObject].spriteRenderer.enabled = false;
    }
}
