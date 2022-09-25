using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour {
    public class Asteroid {
        public GameObject asteroidObject;
        public SpriteRenderer spriteRenderer;

        public Asteroid(GameObject asteroidObject) {
            this.asteroidObject = asteroidObject;
            spriteRenderer      = asteroidObject.GetComponent<SpriteRenderer>();
        }
    }
    public static Dictionary<GameObject, Asteroid> asteroids;
    public Sprite[] asteroidSprites;

    public class Cactus {
        public GameObject cactusObject;
        public SpriteRenderer spriteRenderer;
        public float initSpeed;

        public Cactus(GameObject cactusObject) {
            this.cactusObject = cactusObject;
            spriteRenderer    = cactusObject.GetComponent<SpriteRenderer>();
            initSpeed         = Random.Range(10.0f, 20.0f) * (Random.Range(0, 2) == 0 ? 1 : -1);
            RandomizeRotation(cactusObject);
        }
    }
    public static Dictionary<GameObject, Cactus> cacti;
    public Sprite[] cactusSprites;
    const float CACTUS_SIZE_MULTIPLIER = 1.2f;
    public const float CACTUS_RADIUS = 5;

    public class Cow {
        public GameObject cowObject;
        public SpriteRenderer spriteRenderer;

        public Cow(GameObject cowObject) {
            this.cowObject = cowObject;
            spriteRenderer = cowObject.GetComponent<SpriteRenderer>();
        }
    }
    public static Dictionary<GameObject, Cow> cows;
    public Sprite[] cowSprites;

    public static GameController Game;
    public static AudioController Audio;
    public static LevelData levelData;
    public static int currentLevel;
    public static int unlockedLevels = 0;
    const int TOTAL_LEVELS = 20;
    public static Vector2 mousePos;
    public Texture2D cursor;
    public bool fullTest = false; // For testing and easily restarting levels
    public static bool winLevel;

    public GameObject pauseUI;
    public static bool paused;
    public static bool canPause = true;

    public GameObject gameUI;
    public GameObject oxygenTank;
    public SpriteRenderer oxygenMeterSpriteRenderer;
    public GameObject revolverCylinder;
    public SpriteRenderer[] revolverBullets;
    public Text cowCountText;
    public static int cowsCollected;
    public static int cowsNeeded;
    public string[] links;

    public GameObject endUI;

    public GameObject gameCamera;
    public BoxCollider2D gameCameraCollider;
    public AnimationCurve cameraCurve;
    const int CAMERA_OFFSET = -10;
    const int CAMERA_SIZE_MIN = 20;
    const int CAMERA_SIZE_MAX = 150;
    const int PLAYER_CAMERA_SIZE = 110;
    public static bool playingCameraIntro;

    public GameObject retry;
    public GameObject dialogueBox;
    public Text dialoguetext;
    public static bool showingDialogue;

    public Material pixelizeMaterial;
    const float PIXEL_MIN = 0.001f;
    const float PIXEL_MAX = 0.15f;
    
    void Awake() {
        if (Game != null) {
            Destroy(gameObject);
            return;
        }

        UnityEngine.Cursor.SetCursor(cursor, Vector2.zero, CursorMode.ForceSoftware);
        UnityEngine.Cursor.lockState = CursorLockMode.Confined;
        UnityEngine.Cursor.visible   = false;

        Game  = this;
        Audio = GetComponent<AudioController>();
        Audio.Init();
        DontDestroyOnLoad(gameObject);
        pixelizeMaterial.SetFloat("_PixelSize", PIXEL_MIN);
        unlockedLevels = LoadGame();
        
        if (GameObject.FindGameObjectWithTag("Level") != null)
            LoadLevel(SceneManager.GetActiveScene().name, !fullTest);
    }
    void Update() {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public static void LoadLevel(string level, bool restart = false) {
        Game.StopAllCoroutines();
        Game.StartCoroutine(Game.ieLoadLevel(level, restart));
    }
    IEnumerator ieLoadLevel(string level, bool restart) {
        bool mainMenu = level == "_MainMenu";
        bool ending   = level == "_End";
        float time  = 0;
        float speed = 1.2f;

        while (time < 1) {
            time += Time.deltaTime * speed;
            pixelizeMaterial.SetFloat("_PixelSize", Mathf.Lerp(PIXEL_MIN, PIXEL_MAX, time));
            yield return null;
        }

        Game.gameCameraCollider.enabled = false;
        AsyncOperation ao = SceneManager.LoadSceneAsync(level);
        yield return new WaitUntil(() => ao.isDone);
        
        gameUI.SetActive(!mainMenu && !ending);
        if (!ending) InitLevel(mainMenu);

        Camera cam = Camera.main;
        List<GameObject> cowList = null;
        Vector3 camPos = Vector3.zero;
        if (!mainMenu && !ending) {
            if (!restart) {
                playingCameraIntro = true;
                cowList = new List<GameObject>();
                foreach (Cow c in cows.Values)
                    cowList.Add(c.cowObject);

                camPos = cowList[0].transform.position;
                cam.orthographicSize = CAMERA_SIZE_MIN;
            }
            else {
                camPos = PlayerController.Player.transform.position;
                cam.orthographicSize = PLAYER_CAMERA_SIZE;
            }
        }
        camPos.z = CAMERA_OFFSET;
        cam.transform.position = camPos;

        time = 1;
        while (time > 0) {
            time -= Time.deltaTime * speed;
            pixelizeMaterial.SetFloat("_PixelSize", Mathf.Lerp(PIXEL_MIN, PIXEL_MAX, time));
            yield return null;
        }

        if (!mainMenu) {
            canPause = true;
            if (!ending) {
                if (restart) PlayerController.Player.StartCoroutine(PlayerController.Player.InitPlayerUI(restart));
                else         PlayCameraIntro(cam, cowList);
            }
            else
                InitEnding();
        }
    }

    public static void InitLevel(bool mainMenu) {
        winLevel = false;

        Vector3 camPos = Vector3.zero; camPos.z = CAMERA_OFFSET;
        Camera.main.orthographicSize = PLAYER_CAMERA_SIZE;
        Game.gameCamera.transform.localPosition = camPos;

        cacti = new Dictionary<GameObject, Cactus>();
        GameObject[] cactusObjects = GameObject.FindGameObjectsWithTag("Cactus");
        foreach (GameObject go in cactusObjects) {
            Cactus c = new Cactus(go);
            cacti.Add(go, c);
            c.spriteRenderer.enabled = mainMenu;
        }
        Game.StartCoroutine(Game.RotateCacti());

        if (mainMenu) {
            currentLevel = -1;
            return;
        }

        Game.gameCameraCollider.enabled = true;
        ResetUI();

        GameObject level = GameObject.FindGameObjectWithTag("Level");
        if (level != null)
            levelData = level.GetComponent<LevelData>();
        levelData.levelCamera.SetActive(false);

        asteroids = new Dictionary<GameObject, Asteroid>();
        GameObject[] asteroidObjects = GameObject.FindGameObjectsWithTag("Asteroid");
        foreach (GameObject go in asteroidObjects)
            asteroids.Add(go, new Asteroid(go));

        cows = new Dictionary<GameObject, Cow>();
        GameObject[] cowObjects = GameObject.FindGameObjectsWithTag("Cow");
        foreach (GameObject go in cowObjects)
            cows.Add(go, new Cow(go));

        cowsNeeded = cowObjects.Length;
        cowsCollected = 0;
        UpdateCowCount(false);
    }

    public static void PlayCameraIntro(Camera cam, List<GameObject> cowList) {
        playingCameraIntro = true;
        Game.StartCoroutine(Game.iePlayCameraIntro(cam, cowList));
    }
    IEnumerator iePlayCameraIntro(Camera cam, List<GameObject> cowList) {
        Vector3 camPos      = Vector3.zero;
        Vector3 startCamPos = cam.transform.position;

        float time          = 0;
        float zoomSpeed     = 1;
        float moveSpeed     = 0;
        float baseMoveSpeed = 200;
        float waitTime      = 0.1f;

        // Zoom out
        while (time < 1) {
            time += Time.deltaTime * zoomSpeed;
            cam.orthographicSize = Mathf.Lerp(CAMERA_SIZE_MIN, CAMERA_SIZE_MAX, cameraCurve.Evaluate(time));
            yield return null;
        }
        cam.orthographicSize = CAMERA_SIZE_MAX;

        // Pan to each cow
        for (int i = 1; i < cowList.Count; i++) {
            yield return new WaitForSeconds(waitTime);

            startCamPos = cam.transform.position;
            camPos = cowList[i].transform.position; camPos.z = CAMERA_OFFSET;
            moveSpeed = baseMoveSpeed / Vector2.Distance(startCamPos, camPos);
            time = 0;
            while (time < 1) {
                time += Time.deltaTime * moveSpeed;
                cam.transform.position = Vector3.Lerp(startCamPos, camPos, cameraCurve.Evaluate(time));
                yield return null;
            }
            cam.transform.position = camPos;
        }

        yield return new WaitForSeconds(waitTime);

        // Pan to player
        startCamPos = cam.transform.position;
        camPos = PlayerController.Player.transform.position; camPos.z = CAMERA_OFFSET;
        moveSpeed = baseMoveSpeed / Vector2.Distance(startCamPos, camPos);
        time = 0;
        while (time < 1) {
            time += Time.deltaTime * moveSpeed;
            cam.transform.position = Vector3.Lerp(startCamPos, camPos, cameraCurve.Evaluate(time));
            yield return null;
        }
        cam.transform.position = camPos;

        yield return new WaitForSeconds(waitTime);

        PlayerController.Player.StartCoroutine(PlayerController.Player.InitPlayerUI());

        // Zoom in
        time = 0;
        while (time < 1) {
            time += Time.deltaTime * zoomSpeed;
            cam.orthographicSize = Mathf.Lerp(CAMERA_SIZE_MAX, PLAYER_CAMERA_SIZE, cameraCurve.Evaluate(time));
            yield return null;
        }
        cam.orthographicSize = PLAYER_CAMERA_SIZE;

        if (levelData.dialogue != null && levelData.dialogue.Length > 0) {
            ShowDialogueBox(true);
            StartDialogue(levelData);
        }

        playingCameraIntro = false;
    }
    
    public static void PlaySound(AudioClip clip) {
        Audio.audioSound.PlayOneShot(clip);
    }
    public static void PlayRandomSound(AudioClip clip) {
        Audio.PlayRandom(clip);
    }
    public static void PlayPitched(AudioClip clip, float pitch, bool looped = false) {
        if (looped) Audio.PlayLooped (clip, pitch);
        else        Audio.PlayPitched(clip, pitch);
    }

    public static void RandomizeRotation(GameObject go) {
        go.transform.rotation = Quaternion.Euler(Vector3.forward * Random.Range(0.0f, 359.0f));
    }
    public static void RandomizeTorque(Rigidbody2D rb, float min, float max) {
        rb.AddTorque(Random.Range(min, max) * (Random.Range(0, 2) == 0 ? 1 : -1), ForceMode2D.Impulse);
    }

    public static void LookAt(GameObject go, Vector2 position) {
        LookDirection(go, position - (Vector2)go.transform.position);
    }
    public static void LookDirection(GameObject go, Vector2 direction) {
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);
        go.transform.rotation = Quaternion.Euler(0, 0, rotation.eulerAngles.z + 90);
    }

    public static void ActivateCactus(GameObject cactus, bool death = false) {
        Game.StartCoroutine(Game.ieActivateCactus(cacti[cactus], death));
    }
    IEnumerator ieActivateCactus(Cactus cactus, bool death) {
        PlayRandomSound(AudioController.cactusSpike);
        Transform t = cactus.cactusObject.transform;
        Vector3 startScale = t.localScale;
        Vector3 endScale   = startScale * CACTUS_SIZE_MULTIPLIER;
        float speed = 10;

        cactus.spriteRenderer.sprite = cactusSprites[1];
        float time = 0;
        while (time < 1) {
            time += Time.deltaTime * speed;
            t.localScale = Vector3.Lerp(startScale, endScale, time);
        }
        t.localScale = endScale;

        // If cactus causes death keep spikes out
        if (death) yield break;
        yield return new WaitForSeconds(0.2f);

        cactus.spriteRenderer.sprite = cactusSprites[0];
        time = 1;
        while (time > 0) {
            time -= Time.deltaTime * speed;
            t.localScale = Vector3.Lerp(endScale, startScale, time);
        }
        t.localScale = startScale;
    }
    IEnumerator RotateCacti() {
        while (true) {
            foreach (var kvp in cacti) {
                Cactus c = kvp.Value;
                if (!c.spriteRenderer.enabled)
                    continue;

                float angle = c.cactusObject.transform.localEulerAngles.z + Time.deltaTime * c.initSpeed;
                c.cactusObject.transform.localRotation = Quaternion.Euler(Vector3.forward * angle);
            }
            yield return null;
        }
    }

    public static void Pause() {
        if (!canPause)
            return;

        paused = !Game.pauseUI.activeSelf;
        Game.pauseUI.SetActive(paused);
        Time.timeScale = paused ? 0 : 1;
    }

    public static void ResetUI() {
        Game.oxygenMeterSpriteRenderer.size = new Vector2(20, 3);
        Game.oxygenTank.SetActive(false);

        foreach (SpriteRenderer sr in Game.revolverBullets) {
            sr.enabled = false;
            sr.transform.localScale = Vector3.one;
        }
        Game.revolverCylinder.transform.eulerAngles = Vector3.zero;

        Game.dialoguetext.text = "";
        ShowDialogueBox(false, true);
    }

    public static void RestartLevel() {
        PlayerController.Player.gameObject.SetActive(false);
        Audio.StopLooped(AudioController.puncture1);
        LoadLevel(SceneManager.GetActiveScene().name, (unlockedLevels + 1) > -1);
    }

    public static void ShowRetry(bool show) {
        Game.retry.SetActive(show);
    }

    public static void UpdateCowCount(bool increment = true) {
        if (increment)
            cowsCollected++;

        Game.cowCountText.text = cowsCollected + "/" + cowsNeeded;

        if (cowsCollected == cowsNeeded) {
            canPause = false;
            Audio.StopLooped(AudioController.puncture1);
            Game.StartCoroutine(Game.WinLevel());
        }
    }
    IEnumerator WinLevel() {
        winLevel = true;

        if (levelData.levelNumber > unlockedLevels) {
            if (unlockedLevels < TOTAL_LEVELS)
                SaveGame(++unlockedLevels);
        }

        // Go into slow mo
        while (Time.timeScale > 0.1f) {
            Time.timeScale -= Time.unscaledDeltaTime * 0.5f;
            Audio.audioMusic.pitch = Time.timeScale;
            yield return null;
        }
        Audio.audioMusic.pitch = Time.timeScale = 1;

        LoadLevel(levelData.levelNumber < TOTAL_LEVELS ? "Level" + (levelData.levelNumber + 1) : "_End");
    }

    public static void ShowDialogueBox(bool show, bool instant = false) {
        if (!instant)
            PlayRandomSound(show ? AudioController.dialogueShow : AudioController.dialogueHide);
        showingDialogue = show;
        Game.StartCoroutine(Game.ieShowDialogueBox(show, instant));
    }
    IEnumerator ieShowDialogueBox(bool show, bool instant) {
        Vector3 showPos = new Vector3(-53, -234);
        Vector3 hidePos = new Vector3(-53, -368);
        float time  = 0;
        float speed = 10;
        
        if (show) {
            if (!instant) {
                while (time < 1) {
                    dialogueBox.transform.localPosition = Vector3.Lerp(hidePos, showPos, cameraCurve.Evaluate(time));

                    time += Time.deltaTime * speed;
                    yield return null;
                }
            }
            dialogueBox.transform.localPosition = showPos;
        }
        else {
            if (!instant) {
                while (time < 1) {
                    dialogueBox.transform.localPosition = Vector3.Lerp(showPos, hidePos, cameraCurve.Evaluate(time));

                    time += Time.deltaTime * speed;
                    yield return null;
                }
            }
            dialogueBox.transform.localPosition = hidePos;
        }
    }
    public static void StartDialogue(LevelData levelData) {
        Game.StartCoroutine(Game.ieStartDialogue(levelData));
    }
    IEnumerator ieStartDialogue(LevelData levelData) {
        string[] dialogue = new string[levelData.dialogue.Length + 2];
        dialogue[0] = dialogue[dialogue.Length - 1] = "-bzzzt-";
        for (int i = 0; i < levelData.dialogue.Length; i++)
            dialogue[i + 1] = levelData.dialogue[i];

        bool sound = false;
        foreach (string s in dialogue) {
            dialoguetext.text = "";
            foreach (char c in s) {
                if (sound) PlayRandomSound(AudioController.dialogueText);
                sound = !sound;
                dialoguetext.text += c;
                yield return new WaitForSeconds(0.02f);
            }
            
            // Wait for player to click to continue
            while (true) {
                if (Input.GetMouseButtonDown(0) && !paused)
                    break;

                yield return null;
            }
            yield return null;
        }
        dialoguetext.text = "";

        yield return new WaitForSeconds(0.3f);

        ShowDialogueBox(false);
    }

    // Cows jump around
    void InitEnding() {
        endUI.SetActive(true);
        StartCoroutine(Ending());
    }
    IEnumerator Ending() {
        GameObject[] cowsObjects = GameObject.FindGameObjectsWithTag("Cow");
        GameObject[][] cows = new GameObject[cowsObjects.Length][];
        for (int i = 0; i < cowsObjects.Length; i++) {
            cows[i] = new GameObject[] { cowsObjects[i], null, null };
            Transform[] children = cows[i][0].GetComponentsInChildren<Transform>();
            foreach (Transform t in children) {
                switch (t.name) {
                    case "EyeAnchor": cows[i][1] = t.gameObject; break;
                    case "Eye"      : cows[i][2] = t.gameObject; break;
                }
            }
        }

        int lastCow = -1;
        float jumpTime = 1;
        while (true) {
            if (Input.GetMouseButtonDown(0)) {
                LoadLevel("_MainMenu");
                endUI.SetActive(false);
                yield break;
            }

            if (jumpTime <= 0) {
                jumpTime = 0.5f;

                // Randomly pick new cow to jump
                int cowJump = lastCow;
                while (cowJump == lastCow)
                    cowJump = Random.Range(0, cowsObjects.Length);

                lastCow = cowJump;
                StartCoroutine(CowJump(cowsObjects[cowJump]));
            }
            else
                jumpTime -= Time.deltaTime;

            // All eyes follow mouse
            foreach (GameObject[] c in cows)
                c[2].transform.position = (Vector2)c[1].transform.position + (mousePos - (Vector2)c[1].transform.position).normalized * 3.25f;

            yield return null;
        }
    }
    IEnumerator CowJump(GameObject cow) {
        Vector3 startPos = new Vector3(cow.transform.localPosition.x, -12.23f);
        Vector3 endPos   = new Vector3(cow.transform.localPosition.x,  -9.23f);
        float speed = 5;
        
        if (Random.Range(0, 2) == 0)
            PlayRandomSound(AudioController.cowMoo);

        float time = 0;
        while (time < 1) {
            time += Time.deltaTime * speed;
            cow.transform.localPosition = Vector3.Lerp(startPos, endPos, cameraCurve.Evaluate(time));
            yield return null;
        }

        yield return null;

        time = 1;
        while (time > 0) {
            time -= Time.deltaTime * speed;
            cow.transform.localPosition = Vector3.Lerp(startPos, endPos, cameraCurve.Evaluate(time));
            yield return null;
        }
    }

    int LoadGame() {
        return PlayerPrefs.GetInt("Levels");
    }
    void SaveGame(int unlockedlevels) {
        PlayerPrefs.SetInt("Levels", unlockedlevels);
        PlayerPrefs.Save();
    }
}
