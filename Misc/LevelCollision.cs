using UnityEngine;

public class LevelCollision : MonoBehaviour {
    public ParticleSystem gunParticles;

    private void OnParticleCollision(GameObject other) {
        MenuController.Menu.SelectObject(other);
        gunParticles.Clear();
    }
}
