using UnityEngine;

public class Rotator : MonoBehaviour {
    public GameObject anchor;
    public GameObject orbit;
    public float speed = 10;
    public float radius;

    void Awake() {
        float distance = radius;
        if (radius == 0) {
            Transform[] children = anchor.GetComponentsInChildren<Transform>();
            foreach (Transform t in children) {
                float d = Vector2.Distance(Vector2.zero, t.localPosition);
                if (d > distance)
                    distance = d;
            }
        }

        SpriteRenderer[] orbitPoints = orbit.GetComponentsInChildren<SpriteRenderer>();
        float angle = 360.0f / orbitPoints.Length;
        for (int i = 0; i < orbitPoints.Length; i++) {
            orbitPoints[i].transform.localRotation = Quaternion.Euler(Vector3.forward * i * angle);
            orbitPoints[i].transform.localPosition = orbitPoints[i].transform.right * distance;
        }
    }
    void Update() {
        anchor.transform.Rotate(Vector3.forward * speed * Time.deltaTime);
    }
}
