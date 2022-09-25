#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelEditor : EditorWindow {
    GameObject[] items;
    static readonly GUILayoutOption[] BUTTON_SIZE = new GUILayoutOption[] { GUILayout.Height(60) };

    [MenuItem("Level Editor/Editor Window")]
    public static void EditorWindow() {
        LevelEditor popUp = CreateInstance<LevelEditor>();
        popUp.position = new Rect(Screen.width / 2, Screen.height / 2, 220, 360);
        popUp.ShowPopup();
    }

    void OnEnable() {
        var window = GetWindow(typeof(LevelEditor));
        string[] itemNames = new string[] { "Asteroid", "Cactus", "Cow" };
        items = new GameObject[itemNames.Length];
        for (int i = 0; i < itemNames.Length; i++)
            items[i] = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Game/" + itemNames[i] + ".prefab", typeof(GameObject)) as GameObject;
    }
    void OnGUI() {
        if (GUILayout.Button("Asteroid", BUTTON_SIZE)) CreateGameObject(0, Vector3.zero);
        if (GUILayout.Button("Cactus"  , BUTTON_SIZE)) CreateGameObject(1, Vector3.zero);
        if (GUILayout.Button("Cow"     , BUTTON_SIZE)) CreateGameObject(2, Vector3.zero);

        GUILayout.Space(20);

        Vector3 sizeStep = new Vector3(0.05f, 0.05f, 0);
        if (GUILayout.Button("Scale +")) Selection.activeGameObject.transform.localScale += sizeStep;
        if (GUILayout.Button("Scale -")) Selection.activeGameObject.transform.localScale -= sizeStep;

        GUILayout.Space(20);

        if (GUILayout.Button("Randomize Scaling")) {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(Selection.activeGameObject.tag);
            foreach (GameObject go in objects)
                go.transform.localScale = Vector3.one + (sizeStep * Random.Range(0, 11));
        }
        if (GUILayout.Button("Randomize Shading")) {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(Selection.activeGameObject.tag);
            foreach (GameObject go in objects)
                go.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.white, Random.Range(0.55f, 1.25f));
        }
        if (GUILayout.Button("Snap Positions")) {
            foreach (GameObject go in items) {
                GameObject[] objects = GameObject.FindGameObjectsWithTag(go.tag);
                foreach (GameObject g in objects)
                    SnapPosition(g);
            }
            SnapPosition(GameObject.FindGameObjectWithTag("Player"));

            void SnapPosition(GameObject go) {
                Vector2 position = go.transform.position;
                go.transform.position = new Vector2(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
            }
        }
        if (GUILayout.Button("Generate BG"))
            GenerateBG();
    }

    void CreateGameObject(int index, Vector2 position) {
        if (position == Vector2.zero)
            position = GetCurrentPosition();
        Selection.activeGameObject = PrefabUtility.InstantiatePrefab(items[index]) as GameObject;
        SetPosition(position);

        GameObject level = GameObject.FindGameObjectWithTag("Level");
        if (level != null)
            Selection.activeGameObject.transform.SetParent(level.GetComponent<LevelData>().holders[index].transform);
    }
    Vector2 GetCurrentPosition() {
        return Selection.activeGameObject != null ? (Vector2)Selection.activeGameObject.transform.position : Vector2.zero;
    }
    void SetPosition(Vector2 position) {
        Selection.activeGameObject.transform.position = position;
    }

    void GenerateBG() {
        LevelData levelData = GameObject.FindGameObjectWithTag("Level").GetComponent<LevelData>();
        Transform[] children = levelData.holders[3].GetComponentsInChildren<Transform>();
        foreach (Transform t in children) {
            if (t.name != "BG")
                DestroyImmediate(t.gameObject);
        }

        int bgRadius   = 1000;
        int bgDistance = 60;
        float bgOffset = (float)bgDistance / 3;

        for (int y = -bgRadius - bgDistance; y < bgRadius + bgDistance; y += bgDistance) {
            for (int x = -bgRadius - bgDistance; x < bgRadius + bgDistance; x += bgDistance) {
                Vector2 bgPos = new Vector2(x, y);
                float distance = Vector2.Distance(bgPos, Vector2.zero);
                if (distance > bgRadius || distance < bgDistance * 3)
                    continue;

                bgPos += new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * bgOffset;

                int asteroidChance = Random.Range(0, 100);
                GameObject bgObject = PrefabUtility.InstantiatePrefab(items[asteroidChance < 2 ? 0 : 1]) as GameObject;
                bgObject.transform.position = bgPos;
                bgObject.transform.SetParent(levelData.holders[3].transform);
            }
        }
    }
}
#endif