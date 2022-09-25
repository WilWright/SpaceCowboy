using UnityEngine;
using UnityEngine.UI;

public class Cursor : MonoBehaviour {
    public Rigidbody2D rb;
    public BoxCollider2D trigger;
    GameObject currentSelection;

    public GameObject gunArmPivot;
    public ParticleSystem gunParticles;

    public GameObject eye;
    public GameObject eyeAnchor;

    bool shoot = false;

    void Awake() {
        rb.MovePosition(GameController.mousePos);
    }

    void Update() {
        rb.MovePosition(GameController.mousePos);
        GameController.LookAt(gunArmPivot, transform.position);

        // Menu cow looks at cursor
        Vector2 eyeDirection = transform.position - eye.transform.position;
        if (eyeDirection.sqrMagnitude > 10f)
            eye.transform.position = (Vector2)eyeAnchor.transform.position + eyeDirection.normalized * 3.25f;
        
        if (Input.GetMouseButtonDown(0) && !MenuController.Menu.selectingLevel) {
            // First click doesn't shoot and is used to advance from splash screen to level select screen
            if (!shoot) {
                shoot = true;
                return;
            }

            GameController.PlayRandomSound(AudioController.shoot);
            gunParticles.Play();

            if (currentSelection != null) {
                // Enable collider of hovered over item so it gets "clicked" when the bullet hits it
                switch (currentSelection.tag) {
                    case "Asteroid":
                        MenuController.Menu.SelectAsteroid(currentSelection);
                        trigger.enabled = false;
                        break;

                    case "Link":
                        MenuController.SelectLink(currentSelection);
                        MenuController.HightlightLink(currentSelection.GetComponent<Text>(), false);
                        break;
                }
            }
        }
    }
    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Asteroid")) {
            if (currentSelection != collision.gameObject) {
                GameController.PlayRandomSound(AudioController.levelHover);
                if (currentSelection != null && currentSelection.CompareTag("Asteroid"))
                    MenuController.Menu.HighlightAsteroid(currentSelection, false);
            }

            currentSelection = collision.gameObject;
            MenuController.Menu.HighlightAsteroid(currentSelection, true);
        }

        if (collision.gameObject.CompareTag("Link")) {
            currentSelection = collision.gameObject;
            MenuController.HightlightLink(collision.GetComponent<Text>(), true);
        }
    }
    void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Asteroid"))
            MenuController.Menu.HighlightAsteroid(collision.gameObject, false);

        if (collision.gameObject.CompareTag("Link"))
            MenuController.HightlightLink(collision.GetComponent<Text>(), false);

        currentSelection = null;
    }
}
