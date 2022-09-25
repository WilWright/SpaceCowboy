using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Rigidbody2D rb;
    public DistanceJoint2D asteroidJoint;
    public SpringJoint2D cowJoint;
    public Rigidbody2D[] limbRbs;
    public HingeJoint2D[] limbHinges;

    public GameObject gunArmPivot;
    public Rigidbody2D gunArmRb;
    public ParticleSystem gunParticles;

    public GameObject lassoArmPivot;
    public GameObject lassoArm;
    public Rigidbody2D lassoArmRb;
    Vector2 lassoArmPosition;

    public GameObject lasso;
    public SpriteRenderer lassoSpriteRenderer;
    public SpriteRenderer lassoSnapSpriteRenderer;

    public GameObject lassoLoop;
    public SpriteRenderer lassoLoopSpriteRenderer;
    public Rigidbody2D lassoLoopRb;
    public BoxCollider2D lassoLoopCollider;
    Vector2 lassoLoopPosition;

    public GameObject reticle;
    public GameObject lassoLimitCenter;
    public GameObject[] lassoLimitPoints;
    float lassoLimitRadius;
    IEnumerator setPlayerUIRadiusCoroutine;
    
    public GameObject revolverCylinder;
    public SpriteRenderer[] revolverBullets;
    public AnimationCurve spinCurve;
    IEnumerator spinCylinderCoroutine;
    IEnumerator fireBulletCoroutine;
    int currentBullet = 0;
    float currentTargetAngle;
    const float BULLET_SIZE_MULTIPLIER = 1.3f;

    public GameObject oxygenTank;
    public SpriteRenderer oxygenMeterSpriteRenderer;
    float oxygenAmount = 20;
    const float OXYGEN_LOSS_SPEED = 1;

    public ParticleSystem[] punctureParticles;
    float[] puncturePitches;
    int punctureCount;
    const float PUNCTURE_FORCE = 60;

    bool dead;
    bool initBullets;
    int bullets = 0;
    const float MAX_SPEED = 200;
    const float MAX_ANGULAR_VELOCITY = 30;
    const float MOVE_FORCE = 10;
    const float SHOOT_FORCE = 130;

    bool extendingLasso;
    bool snapLasso;
    bool resetLasso;
    float lassoTime;
    float lassoLength;
    const float MAX_LASSO_LENGTH = 100;
    const float LASSO_EXTEND_SPEED = 520;

    Vector2 shootDirection;
    Vector2 lassoDirection;
    Rigidbody2D currentAsteroid;
    public Rigidbody2D currentCow;
    LayerMask cactusMask;

    static readonly KeyCode[] KEY_CODES = new KeyCode[] { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
    static readonly Vector2[] DIRECTIONS = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    bool[] holdingKey = new bool[4];

    public static PlayerController Player;

    void Awake() {
        if (GameController.Game == null)
            return;

        Player = this;
        oxygenTank                = GameController.Game.oxygenTank;
        oxygenMeterSpriteRenderer = GameController.Game.oxygenMeterSpriteRenderer;
        revolverCylinder          = GameController.Game.revolverCylinder;
        revolverBullets           = GameController.Game.revolverBullets;

        // Init cursor range circle
        float angle = 360.0f / lassoLimitPoints.Length;
        for (int i = 0; i < lassoLimitPoints.Length; i++)
            lassoLimitPoints[i].transform.localRotation = Quaternion.Euler(Vector3.forward * i * angle);
        
        lassoArmPosition  = lassoArm.transform.localPosition;
        lassoLoopPosition = lasso   .transform.localPosition;

        GameController.RandomizeRotation(gameObject);
        GameController.RandomizeTorque(rb, 400, 800);

        puncturePitches = new float[punctureParticles.Length];
        cactusMask = LayerMask.GetMask("Cactus");
    }
    public IEnumerator InitPlayerUI(bool restart = false) {
        InitBullets(GameController.levelData.initialBulletCount, restart);

        if (!restart)
            yield return new WaitForSeconds(0.5f);

        lassoLimitCenter.SetActive(true);
        SetPlayerUIRadius(MAX_LASSO_LENGTH, restart);
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape))
            GameController.Pause();

        if (GameController.paused) {
            if (Input.GetKeyDown(KeyCode.Backspace)) {
                GameController.Pause();
                GameController.LoadLevel("_MainMenu");
            }
        }
        else {
            lassoLimitCenter.transform.rotation = Quaternion.Euler(Vector3.zero);
            shootDirection = GameController.mousePos - (Vector2)transform.position;
            reticle.transform.position = GameController.mousePos;
            if (!snapLasso)
                reticle.transform.localPosition = Vector3.ClampMagnitude(reticle.transform.localPosition, lassoLimitRadius);
            GameController.LookDirection(reticle, shootDirection);
            if (!dead && bullets > 0)
                GameController.LookDirection(gunArmPivot, shootDirection);
        }

        if (GameController.playingCameraIntro)
            return;

        if (!dead) {
            Vector3 cameraPos = new Vector3(transform.position.x, transform.position.y, -10);
            GameController.Game.gameCamera.transform.position = cameraPos;
        }

        if (GameController.showingDialogue || GameController.paused)
            return;

        for (int i = 0; i < KEY_CODES.Length; i++)
            holdingKey[i] = Input.GetKey(KEY_CODES[i]);

        if (Input.GetKeyDown(KeyCode.R) && !GameController.winLevel) {
            GameController.ShowRetry(false);
            GameController.RestartLevel();
        }

        if (initBullets)
            return;

        if (!dead && bullets > 0) {
            if (Input.GetMouseButtonDown(0))
                Shoot();
        }

        if (Input.GetMouseButtonUp(1)) {
            if (currentAsteroid != null) LassoAsteroid(null);
            if (currentCow      != null) LassoCow     (null);
        }

        ExtendLasso(Input.GetMouseButton(1));
        UpdateOxygen();

        if (GameController.winLevel)
            rb.velocity *= 0.99f;
    }
    void FixedUpdate() {
        for (int i = 0; i < holdingKey.Length; i++) {
            if (holdingKey[i])
                rb.AddForce(DIRECTIONS[i] * MOVE_FORCE);
        }
        if (punctureCount > 0) {
            for (int i = 0; i < punctureCount; i++)
                rb.AddForce(punctureParticles[i].transform.right * PUNCTURE_FORCE);
        }
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, MAX_SPEED);
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -MAX_ANGULAR_VELOCITY, MAX_ANGULAR_VELOCITY);
    }
    void OnTriggerEnter2D(Collider2D collision) {
        if (dead)
            return;

        if (collision.gameObject.CompareTag("Cactus"))
            Puncture(collision.gameObject);
    }

    public static void InitBullets(int amount, bool restart = false) {
        Player.initBullets = true;
        Player.StartCoroutine(Player.ieInitBullets(amount, restart));
    }
    IEnumerator ieInitBullets(int amount, bool restart) {
        for (int i = 0; i < amount; i++) {
            LoadBullet(i, restart);
            if (!restart)
                yield return new WaitForSeconds(0.3f);
        }

        if (restart) {
            initBullets = false;
            yield break;
        }
        yield return new WaitForSeconds(0.2f);

        SpinCylinder(1080, true, true);
    }
    public void LoadBullet(int bulletIndex, bool restart = false) {
        GameController.PlayRandomSound(AudioController.revolverLoad);
        SpriteRenderer sr = revolverBullets[bulletIndex];
        sr.enabled = true;
        bullets++;

        if (!restart)
            StartCoroutine(ExpandBullet(sr));
    }
    IEnumerator ExpandBullet(SpriteRenderer spriteRenderer, bool alsoDisable = false) {
        Transform t = spriteRenderer.transform;
        Vector3 fromScale = Vector3.one;
        Vector3 toScale   = fromScale * BULLET_SIZE_MULTIPLIER;
        float speed = 10;

        float time = 0;
        while (time < 1) {
            time += Time.deltaTime * speed;
            t.localScale = Vector3.Lerp(fromScale, toScale, time);
        }
        t.localScale = toScale;

        yield return new WaitForSeconds(0.2f);

        time = 1;
        while (time > 0) {
            time -= Time.deltaTime * speed;
            t.localScale = Vector3.Lerp(toScale, fromScale, time);
        }
        t.localScale = fromScale;

        if (alsoDisable)
            spriteRenderer.enabled = false;
    }

    void SpinCylinder(float angleAmount, bool activate, bool init = false) {
        if (spinCylinderCoroutine != null) {
            StopCoroutine(spinCylinderCoroutine);
            revolverCylinder.transform.localEulerAngles = Vector3.forward * currentTargetAngle;
        }
        if (activate) {
            float startAngle = revolverCylinder.transform.localEulerAngles.z;
            currentTargetAngle = startAngle - angleAmount;
            spinCylinderCoroutine = ieSpinCylinder(startAngle, init);
            StartCoroutine(spinCylinderCoroutine);
        }
    }
    IEnumerator ieSpinCylinder(float startAngle, bool init) {
        GameController.PlayRandomSound(init ? AudioController.revolverSpin : AudioController.revolverNext);

        float time  = 0;
        float speed = init ? 0.65f : 3f;
        while (time < 1) {
            revolverCylinder.transform.localEulerAngles = Vector3.forward * Mathf.Lerp(startAngle, currentTargetAngle, spinCurve.Evaluate(time));

            time += Time.deltaTime * speed;
            yield return null;
        }
        revolverCylinder.transform.localEulerAngles = Vector3.forward * currentTargetAngle;

        initBullets = false;
        spinCylinderCoroutine = null;
    }

    void Shoot() {
        if (--bullets <= 0)
            gunArmRb.simulated = true;

        GameController.PlayRandomSound(AudioController.shoot);
        revolverBullets[currentBullet].enabled = false;
        currentBullet = currentBullet + 1 >= revolverBullets.Length ? 0 : currentBullet + 1;
        SpinCylinder(60, true);
       
        rb.AddForce(-shootDirection.normalized * SHOOT_FORCE, ForceMode2D.Impulse);
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, MAX_SPEED);
        gunParticles.Play();
    }

    void ExtendLasso(bool extend) {
        if (extend && !resetLasso && !snapLasso && !dead) {
            if (currentAsteroid != null) {
                lassoDirection = currentAsteroid.transform.position - lassoArmPivot.transform.position;
                SetLassoLengthToTarget(currentAsteroid.transform.position);
                UpdateLasso();
                return;
            }
            if (currentCow != null) {
                lassoDirection = currentCow.transform.position - lassoArmPivot.transform.position;
                SetLassoLengthToTarget(currentCow.transform.position);
                UpdateLasso();
                return;
            }

            if (!extendingLasso) {
                if (lassoTime < 0)
                    lassoTime = 0;

                GameController.PlayRandomSound(AudioController.lassoExtend);

                lassoArm.transform.localPosition = lassoArmPosition;
                lassoArm.transform.localRotation = Quaternion.Euler(Vector3.zero);
                lassoArmRb.simulated = false;

                lassoLoop.transform.localPosition = lassoLoopPosition;
                lassoSpriteRenderer.enabled = lassoLoopSpriteRenderer.enabled = lassoLoopCollider.enabled = true;
                lassoDirection = shootDirection;
            }

            if (lassoTime < MAX_LASSO_LENGTH) {
                lassoTime += Time.deltaTime * LASSO_EXTEND_SPEED;
                lassoLength = (int)lassoTime;

                lassoSpriteRenderer.size = new Vector2(1, lassoLength);
                lassoLoop.transform.localPosition = new Vector2(lassoLoopPosition.x, lassoLength);
            }
            else
                resetLasso = true;

            UpdateLasso();

            void UpdateLasso() {
                GameController.LookDirection(lassoArmPivot, lassoDirection);
                RaycastForCactus();
            }
        }
        else {
            if (lassoTime > 0) {
                lassoLoopCollider.enabled = false;
                lassoTime -= Time.deltaTime * LASSO_EXTEND_SPEED;
                lassoLength = (int)lassoTime;

                lassoSpriteRenderer.size = new Vector2(1, lassoLength);
                GameController.LookDirection(lassoArmPivot, lassoDirection);
                RaycastForCactus();
                if (!snapLasso)
                    lassoLoop.transform.localPosition = new Vector2(lassoLoopPosition.x, lassoLength);
            }
            else
                ResetLasso();
        }

        extendingLasso = extend;
    }
    void ResetLasso() {
        lassoTime = 0;
        resetLasso = false;
        lassoArmRb.simulated = true;
        lassoSpriteRenderer.size = new Vector2(1, 0);
        lassoSpriteRenderer.enabled = false;
        if (!snapLasso)
            lassoLoopSpriteRenderer.enabled = false;
    }
    void ResetLassoLoop(bool showLoop) {
        lassoLoop.transform.SetParent(lasso.transform);
        lassoLoop.transform.localRotation = Quaternion.Euler(Vector3.forward * 180);
        lassoLoopSpriteRenderer.enabled = showLoop;
    }

    void RaycastForCactus() {
        if (snapLasso)
            return;
        
        Vector2 direction = lassoLoop.transform.position - lasso.transform.position;
        RaycastHit2D hit = Physics2D.Raycast(lasso.transform.position, direction, direction.magnitude, cactusMask);
        if (hit.collider != null) {
            snapLasso = true;
            asteroidJoint.enabled = false;
            GameController.ActivateCactus(hit.collider.gameObject);
            StartCoroutine(SnapLasso(hit.point));
        }
    }
    IEnumerator SnapLasso(Vector2 snapPoint) {
        GameController.PlayRandomSound(AudioController.lassoSnap);

        if (currentCow != null)
            LassoCow(null);

        SetLassoLengthToTarget(snapPoint);
        lassoTime = lassoSpriteRenderer.size.y;
        lassoLoop.transform.SetParent(null);
        lassoLoopCollider.enabled = false;
        lassoLimitCenter.SetActive(false);
        reticle.transform.SetParent(null);
        
        lassoSnapSpriteRenderer.transform.position = lassoLoop.transform.position;
        lassoSnapSpriteRenderer.transform.SetParent(null);
        float size = Vector2.Distance(snapPoint, lassoLoop.transform.position);
        lassoSnapSpriteRenderer.size = new Vector2(1, size);
        
        float length = lassoSnapSpriteRenderer.size.y;
        float time = length;
        while (length > 0) {
            time -= Time.deltaTime * LASSO_EXTEND_SPEED;
            length = (int)time;

            lassoSnapSpriteRenderer.size = new Vector2(1, length);
            yield return null;
        }
        lassoSnapSpriteRenderer.enabled = false;

        if (currentAsteroid == null && currentCow == null) {
            lassoLoopRb.bodyType = RigidbodyType2D.Dynamic;
            lassoLoopRb.AddForce(lassoDirection, ForceMode2D.Impulse);
            lassoLoopRb.angularVelocity = lassoDirection.sqrMagnitude * 0.1f;
        }
    }

    public void LassoAsteroid(Rigidbody2D asteroidRb) {
        if (asteroidRb != null) {
            GameController.PlayRandomSound(AudioController.lassoAttach);
            currentAsteroid = asteroidRb;
            lassoLoop.transform.SetParent(currentAsteroid.transform);
            lassoLoop.transform.position = currentAsteroid.transform.position;
            lassoLoopSpriteRenderer.enabled = lassoLoopCollider.enabled = false;
            SetLassoLengthToTarget(currentAsteroid.transform.position);
            GameController.asteroids[asteroidRb.gameObject].spriteRenderer.sprite = GameController.Game.asteroidSprites[1];

            asteroidJoint.connectedBody = asteroidRb;
            asteroidJoint.distance = Vector2.Distance(transform.position, currentAsteroid.transform.position);
            SetPlayerUIRadius(asteroidJoint.distance);
            asteroidJoint.enabled = true;
        }
        else {
            if (currentAsteroid != null) {
                if (!snapLasso) {
                    ResetLassoLoop(true);
                    GameController.asteroids[currentAsteroid.gameObject].spriteRenderer.sprite = GameController.Game.asteroidSprites[0];
                }
                
                lassoArm.transform.localPosition = lassoArmPosition;
                SetPlayerUIRadius(MAX_LASSO_LENGTH);
                asteroidJoint.enabled = false;
                currentAsteroid = null;
            }
        }
    }
    public void LassoCow(Rigidbody2D cowRb) {
        if (cowRb != null) {
            GameController.PlayRandomSound(AudioController.lassoAttach);
            currentCow = cowRb;
            lassoLoop.transform.SetParent(currentCow.transform);
            lassoLoop.transform.position = currentCow.transform.position;
            lassoLoopSpriteRenderer.enabled = lassoLoopCollider.enabled = false;
            GameController.cows[currentCow.gameObject].spriteRenderer.sprite = GameController.Game.cowSprites[1];

            cowJoint.connectedBody = cowRb;
            cowJoint.enabled = true;
        }
        else {
            if (currentCow != null) {
                if (!snapLasso) {
                    ResetLassoLoop(true);
                    GameController.cows[currentCow.gameObject].spriteRenderer.sprite = GameController.Game.cowSprites[0];
                }

                lassoArm.transform.localPosition = lassoArmPosition;
                cowJoint.enabled = false;
                currentCow = null;
            }
        }
    }

    void SetLassoLengthToTarget(Vector2 target) {
        float size = Vector2.Distance(target, lasso.transform.position);
        lassoSpriteRenderer.size = new Vector2(1, size);
    }

    public void CollectCow(GameObject cow) {
        GameController.PlayRandomSound(AudioController.cowMoo);
        GameController.PlayRandomSound(AudioController.cowCollect);
        cow.SetActive(false);

        if (currentCow != null) {
            ResetLasso();
            ResetLassoLoop(false);
        }

        GameController.UpdateCowCount();
    }

    void Puncture(GameObject cactus) {
        if (GameController.winLevel)
            return;

        if (punctureCount < 2) {
            puncturePitches[punctureCount] = AudioController.GetRandomPitch();
            GameController.PlayPitched(AudioController.puncture0, puncturePitches[punctureCount]);
            StartCoroutine(LoopPuncture(punctureCount));

            ParticleSystem ps = punctureParticles[punctureCount];
            Vector2 direction = (transform.position - cactus.transform.position).normalized;
            ps.transform.position = (Vector2)cactus.transform.position + direction * cactus.transform.localScale.y * GameController.CACTUS_RADIUS;
            ps.transform.SetParent(transform);
            GameController.LookDirection(ps.gameObject, direction);
            ps.gameObject.SetActive(true);

            oxygenTank.SetActive(true);
            GameController.ActivateCactus(cactus);
            punctureCount++;
        }
        else {
            GameController.ActivateCactus(cactus, true);
            Die(cactus);
        }

        IEnumerator LoopPuncture(int index) {
            yield return new WaitForSeconds(0.2f);
            GameController.PlayPitched(AudioController.puncture1, puncturePitches[index], true);
        }
    }

    void UpdateOxygen() {
        if (GameController.winLevel)
            return;

        if (punctureCount > 0) {
            if (oxygenAmount > 0) {
                oxygenAmount -= Time.deltaTime * OXYGEN_LOSS_SPEED * punctureCount;
                oxygenMeterSpriteRenderer.size = new Vector2(oxygenAmount, 3);
            }
            else {
                GameController.Audio.StopLooped(AudioController.puncture1);
                for (int i = 0; i < punctureCount; i++)
                    GameController.PlayPitched(AudioController.puncture2, puncturePitches[i]);

                punctureCount = 0;
                oxygenMeterSpriteRenderer.size = new Vector2(0, 3);
                foreach (ParticleSystem ps in punctureParticles) {
                    var emission = ps.emission;
                    emission.enabled = false;
                }

                if (!dead)
                    Die();
            }
        }
    }

    void SetPlayerUIRadius(float radius, bool restart = false) {
        if (setPlayerUIRadiusCoroutine != null) StopCoroutine(setPlayerUIRadiusCoroutine);
            setPlayerUIRadiusCoroutine  = ieSetPlayerUIRadius(radius, restart);
        StartCoroutine(setPlayerUIRadiusCoroutine);
    }
    IEnumerator ieSetPlayerUIRadius(float radius, bool restart) {
        Vector2[] targets = new Vector2[lassoLimitPoints.Length];
        for (int i = 0; i < targets.Length; i++)
            targets[i] = lassoLimitPoints[i].transform.right * radius;

        if (!restart) {
            while (Mathf.Abs(lassoLimitPoints[0].transform.localPosition.x - radius) > 0.01f) {
                for (int i = 0; i < targets.Length; i++)
                    lassoLimitPoints[i].transform.localPosition = Vector2.MoveTowards(lassoLimitPoints[i].transform.localPosition, targets[i], Time.deltaTime * 400);
                lassoLimitRadius = lassoLimitPoints[0].transform.localPosition.magnitude;
                yield return null;
            }
        }

        for (int i = 0; i < targets.Length; i++)
            lassoLimitPoints[i].transform.localPosition = targets[i];
        lassoLimitRadius = lassoLimitPoints[0].transform.localPosition.magnitude;
    }
    
    void Die(GameObject cactus = null) {
        dead = true;
        GameController.ShowRetry(true);
        GameController.PlayRandomSound(AudioController.death);
        gunArmRb.simulated = lassoArmRb.simulated = true;
        gunArmRb.velocity  = lassoArmRb.velocity  = rb.velocity;
        lassoLimitCenter.SetActive(false);

        for (int i = 0; i < limbRbs.Length; i++) {
            limbRbs[i].transform.SetParent(null);
            Vector2 direction = limbRbs[i].transform.position - transform.position;
            limbRbs[i].AddForce(direction.normalized * 0.005f, ForceMode2D.Impulse);
            limbRbs[i].AddTorque(1, ForceMode2D.Impulse);
            limbHinges[i].enabled = false;
        }

        if (cactus != null) {
            rb.simulated = false;
            transform.SetParent(cactus.transform);
        }
    }
}
