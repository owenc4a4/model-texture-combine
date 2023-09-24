using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

public class ModelEditor : EditorWindow
{
    [MenuItem("Window/UI Toolkit/ModelEditor")]
    public static void ShowExample()
    {
        ModelEditor wnd = GetWindow<ModelEditor>();
        wnd.titleContent = new GUIContent("ModelEditor");
        //wnd.Show();
    }

    [SerializeField]
    private VisualTreeAsset m_UXMLTree;


    private int m_ClickCount = 0;
    private const string m_ButtonPrefix = "button";

    private Texture2D _tex;
    private Mesh _mesh;

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        Label label = new Label("These controls were created using C# code.");
        root.Add(label);

        ObjectField objectField = new ObjectField();
        objectField.name = "mesh";
        objectField.objectType = typeof(Mesh);
        root.Add(objectField);

        if (_mesh != null) {
            objectField.SetValueWithoutNotify(_mesh);
        }
        objectField.RegisterValueChangedCallback((ChangeEvent<Object> evt) => {
            Debug.Log("changed");
            Debug.Log(evt.newValue);
            _mesh = evt.newValue as Mesh;
        });


        ObjectField textureField = new ObjectField();
        textureField.name = "texture";
        textureField.objectType = typeof(Texture2D);
        root.Add(textureField);

        if (_tex != null) {
            textureField.SetValueWithoutNotify(_tex);
        }
        textureField.RegisterValueChangedCallback((ChangeEvent<Object> evt) => {
            Debug.Log("changed");
            Debug.Log(evt.newValue);
            _tex = evt.newValue as Texture2D;
        });


        root.Add(m_UXMLTree.Instantiate());

        //Call the event handler
        SetupButtonHandler();
    }


    private void SetupButtonHandler()
    {
        VisualElement root = rootVisualElement;

        var btn = root.Q<Button>("convert-btn");
        btn.RegisterCallback<ClickEvent>(Convert);
    }

    void Convert(ClickEvent evt)
    {
        VisualElement root = rootVisualElement;

        Debug.Log("Convert ");
        Debug.Log("c " + m_ClickCount++);
        // Button button = evt.currentTarget as Button;
        // Debug.Log(button);


        // var t = root.Q<ObjectField>("texture");
        // Debug.Log(t);

        Debug.Log(_tex);
        Debug.Log(_mesh);

        var w = _tex.width;
        var h = _tex.height;

        SetTexReadable(_tex, true);

        var vs =  _mesh.vertices;
        var uvs = _mesh.uv;
        var tris = _mesh.triangles;

        Dictionary<Color, Vector2Int> list = new Dictionary<Color, Vector2Int>();

        foreach (var uv in uvs) {
            var x = (int)(uv.x * w);
            var y = (int)(uv.y * h);

            Color color = _tex.GetPixel(x, y);
            if (!list.ContainsKey(color)) {
                list[color] = Vector2Int.zero;
            }
        }


        Debug.Log("colors " + list.Count);
        var newSize = 8;
        if (list.Count <= 4) {
            newSize = 16;
        } else if (list.Count <= 16) {
            newSize = 32;
        }

        {
            var i = 0;
            var j = 0;
            var keys = list.Keys.ToArray();
            foreach (var key in keys) {
                list[key] = new Vector2Int(i, j);
                i++;

                if (newSize / 8 <= i) {
                    i = 0;
                    j++;
                }
            }
        }

        foreach (var keyPair in list) {
            Debug.Log($"{keyPair.Key}  {keyPair.Value}");
        }

        var newMesh = new Mesh();

        var newUvs = new Vector2[uvs.Length];
        for (var i = 0; i < uvs.Length; i++)
        {
            var uv = uvs[i];
            var x = (int)(uv.x * w);
            var y = (int)(uv.y * h);

            Color color = _tex.GetPixel(x, y);

            // TODO: color to uv
            var newUv = list[color];

            newUvs[i] = new Vector2((newUv.x * 8f + 3f) / newSize, (newUv.y * 8f + 3f) / newSize);
        }

        // Copy existing data (UVs, vertices, and triangles) to the new mesh
        newMesh.vertices = vs;
        newMesh.triangles = tris;
        newMesh.uv = newUvs;

        var newMeshName = "newMesh";

        // Save the new mesh as an asset
        string assetPath = "Assets/" + newMeshName + ".asset";
        //AssetDatabase.CreateAsset(newMesh, assetPath);


        var newTexture = new  Texture2D(newSize, newSize);
        Color32[] pixels = new Color32[newSize * newSize];
        {
            foreach (var keyPair in list) {
                Debug.Log($"#### num: {keyPair.Value}");
                var i = keyPair.Value.x;
                var j = keyPair.Value.y;
                var pi = i * 8;
                var pj = j * 8;
                for (var x = pi; x < pi + 8; x++) {
                    for (var y = pj; y < pj + 8; y++) {
                        Debug.Log(x + ", " + y);
                        pixels[x + y * newSize] =  keyPair.Key;
                    }
                }
            }
        }
        newTexture.SetPixels32(pixels);


        newTexture.Apply(true);

        // Encode the texture as a PNG or other desired format
        byte[] textureBytes = newTexture.EncodeToPNG();

        var textureName = "newTEx";
        // Save the texture as an asset
        assetPath = "Assets/" + textureName + ".png";
        System.IO.File.WriteAllBytes(assetPath, textureBytes);

        DestroyImmediate(newTexture);



        SetTexReadable(_tex, false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void SetTexReadable(Texture2D tex, bool isread)
    {
        var assetPath = AssetDatabase.GetAssetPath(tex);
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;


        // Modify the Is Readable property
        textureImporter.isReadable = isread; // Set to true or false as needed

        // Apply the changes
        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.Refresh();
    }

}
