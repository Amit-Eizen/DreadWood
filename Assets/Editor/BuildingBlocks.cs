using UnityEngine;
using UnityEditor;
using System.IO;

// Shared pieces for the level-building tools (the cabin, the chase corridor). They all make
// the same things over and over — a cube with a collider, a plain coloured material saved as
// an asset — so those live here once instead of once per tool.
public static class BuildingBlocks
{
    const string MaterialFolder = "Assets/DreadWood/Materials";

    // Any primitive, parented and sized. It keeps its collider, so a wall or a pillar built
    // with this blocks the player for free.
    public static GameObject Shape(GameObject parent, string name, PrimitiveType type, Vector3 localCenter,
                                   Vector3 size, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localCenter;
        go.transform.localRotation = localRotation;
        go.transform.localScale = size;
        if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.isStatic = true;
        return go;
    }

    public static GameObject Box(GameObject parent, string name, Vector3 localCenter, Vector3 size,
                                 Quaternion localRotation, Material material)
    {
        return Shape(parent, name, PrimitiveType.Cube, localCenter, size, localRotation, material);
    }

    public static GameObject Box(GameObject parent, string name, Vector3 localCenter, Vector3 size, Material material)
    {
        return Box(parent, name, localCenter, size, Quaternion.identity, material);
    }

    // The same box, given two opposite corners instead of a centre and a size. Much easier
    // to read when building slabs around a hole.
    public static GameObject BoxBetween(GameObject parent, string name, Vector3 min, Vector3 max, Material material)
    {
        return Box(parent, name, (min + max) * 0.5f, max - min, Quaternion.identity, material);
    }

    // Loads the material asset if it is already there, otherwise creates and saves it.
    public static Material GetMaterial(string name, Color colour)
    {
        if (!Directory.Exists(MaterialFolder)) Directory.CreateDirectory(MaterialFolder);
        string path = MaterialFolder + "/" + name + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.color = colour; return existing; }

        Material created = new Material(Shader.Find("Standard"));
        created.color = colour;
        created.SetFloat("_Glossiness", 0.1f);   // matte, not shiny
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    // The same, plus a tiling texture — if that texture is actually in the project.
    public static Material GetTexturedMaterial(string name, string texturePath, Vector2 tiling, Color tint)
    {
        Material material = GetMaterial(name, tint);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture != null)
        {
            material.mainTexture = texture;
            material.color = tint;
            material.mainTextureScale = tiling;
        }
        return material;
    }

    // An empty GameObject used as a marker — a spawn point, a teleport exit.
    public static GameObject Marker(GameObject parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        return go;
    }

    // Sets the layer on an object and everything under it. Floors have to end up on Ground
    // or neither the player nor the enemies will believe they are standing on anything.
    public static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return;

        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
