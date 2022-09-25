using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
    class LevelSelection {
        public GameObject levelObject;
        public Text levelNumber;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer lockedSpriteRenderer;
        public BoxCollider2D trigger;
        public BoxCollider2D collider;
        public float initSpeed;

        public LevelSelection(GameObject levelObject) {
            this.levelObject     = levelObject;
            levelNumber          = levelObject.transform.parent.GetComponent<Text>();
            spriteRenderer       = levelObject.GetComponent<SpriteRenderer>();
            lockedSpriteRenderer = levelObject.transform.GetChild(0).GetComponent<SpriteRenderer>();

            BoxCollider2D[] colliders = levelObject.GetComponents<BoxCollider2D>();
            if (colliders[0].isTrigger) {
                trigger  = colliders[0];
                collider = colliders[1];
            }
            else {
                trigger  = colliders[1];
                collider = colliders[0];
            }

            GameController.RandomizeRotation(levelObject);
            initSpeed = Random.Range(10.0f, 20.0f) * (Random.Range(0, 2) == 0 ? 1 : -1);
            
            if (int.Parse(levelNumber.text) > GameController.unlockedLevels + 1)
                levelNumber.enabled = trigger.enabled = false;
            else
                lockedSpriteRenderer.enabled = false;
        }
    }
    
    public static MenuController Menu;
    public bool selectingLevel;
    public AnimationCurve menuMovementCurve;
    public Color32[] levelColors = new Color32[2];
    public Color32[] hightlightColors = new Color32[2];
    public Sprite[] levelSprites;
    Dictionary<GameObject, LevelSelection> levelSelections;

    public GameObject title;
    public GameObject menuAnchor;
    public Text playText;

    public Rigidbody2D playerRb;
    public Rigidbody2D cowRb; 

    void Awake() {
        Menu = this;
        InitLevels();
    }
    void Start() {
        GetComponent<Canvas>().worldCamera = Camera.main;
        GameController.RandomizeRotation(playerRb.gameObject);
        GameController.RandomizeRotation(cowRb   .gameObject);
        GameController.RandomizeTorque(playerRb, 2, 4);
        GameController.RandomizeTorque(cowRb   , 4, 8);
        GameController.InitLevel(true);
        StartCoroutine(BlinkPlayText());
        StartCoroutine(RotateAsteroids());
    }
    void Update() {
        title.transform.localPosition = Vector3.up * Mathf.Sin(Time.time * 2) * 2;

        if (Input.GetMouseButtonDown(0) && playText.gameObject.activeSelf) {
            playText.gameObject.SetActive(false);
            StartCoroutine(MoveToLevelSelect(false));
        }
    }

    IEnumerator BlinkPlayText() {
        while (playText.gameObject.activeSelf) {
            foreach (Color32 c in hightlightColors) {
                playText.color = c;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    IEnumerator MoveToLevelSelect(bool instant) {
        float moveHeight = 800;

        if (!instant) {
            float time = 0;
            while (time < 1) {
                time += Time.deltaTime * 2;
                menuAnchor.transform.localPosition = Vector2.up * Mathf.Lerp(0, moveHeight, menuMovementCurve.Evaluate(time));
                yield return null;
            }
        }
        menuAnchor.transform.localPosition = Vector2.up * moveHeight;
    }

    void InitLevels() {
        levelSelections = new Dictionary<GameObject, LevelSelection>();
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Asteroid");
        foreach (GameObject go in objects)
            levelSelections.Add(go, new LevelSelection(go));
    }

    public void SelectObject(GameObject go) {
        if (go == null)
            return;

        if (go.CompareTag("Asteroid")) {
            GameController.PlayRandomSound(AudioController.levelSelect);
            StartCoroutine(SelectLevel(levelSelections[go]));
        }
    }
    IEnumerator SelectLevel(LevelSelection levelSelection) {
        selectingLevel = true;
        for (int blink = 0; blink < 7; blink++) {
            levelSelection.spriteRenderer.sprite = levelSprites[2]; yield return new WaitForSeconds(0.075f);
            levelSelection.spriteRenderer.sprite = levelSprites[1]; yield return new WaitForSeconds(0.075f);
        }
        levelSelection.spriteRenderer.sprite = levelSprites[2];

        yield return new WaitForSeconds(0.5f);

        GameController.LoadLevel("Level" + levelSelection.levelNumber.text);
    }

    public void HighlightAsteroid(GameObject asteroid, bool highlight) {
        LevelSelection ls = levelSelections[asteroid];
        if (ls.collider.enabled)
            return;

        ls.spriteRenderer.sprite = highlight ? levelSprites[1] : levelSprites[0];
        ls.collider.enabled = false;
    }
    public void SelectAsteroid(GameObject asteroid) {
        levelSelections[asteroid].collider.enabled = true;
    }
    IEnumerator RotateAsteroids() {
        while (true) {
            foreach (var kvp in levelSelections) {
                LevelSelection ls = kvp.Value;
                float angle = ls.levelObject.transform.eulerAngles.z + Time.deltaTime * ls.initSpeed;
                ls.levelObject.transform.rotation = Quaternion.Euler(Vector3.forward * angle);
            }
            yield return null;
        }
    }

    public static void HightlightLink(Text text, bool highlight) {
        text.color = Menu.hightlightColors[highlight ? 1 : 0];
    }
    public static void SelectLink(GameObject link) {
        switch (link.name) {
            case "Music"  : OpenNewTab(GameController.Game.links[0]); break;
            case "License": OpenNewTab(GameController.Game.links[1]); break;
        }
    }
    [DllImport("__Internal")]
    static extern void OpenNewTab(string url);
}