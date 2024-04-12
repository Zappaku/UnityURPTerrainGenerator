using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        EditorGUI.BeginChangeCheck();

        GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
        TerrainGenerator.TerrainPreset newPreset = (TerrainGenerator.TerrainPreset)EditorGUILayout.EnumPopup("Terrain Preset", terrainGenerator.terrainPreset);

        switch (terrainGenerator.terrainPreset)
        {
            case TerrainGenerator.TerrainPreset.Grasslands:
                terrainGenerator.grasslandsTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Grasslands Terrain Layer",
                    terrainGenerator.grasslandsTerrainLayer, typeof(TerrainLayer), false);
                break;
            case TerrainGenerator.TerrainPreset.Desert:
                terrainGenerator.desertTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Desert Terrain Layer",
                    terrainGenerator.desertTerrainLayer, typeof(TerrainLayer), false);
                break;
            case TerrainGenerator.TerrainPreset.Mountainous:
                terrainGenerator.mountainTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Mountainous Terrain Layer",
                    terrainGenerator.mountainTerrainLayer, typeof(TerrainLayer), false);
                break;
            case TerrainGenerator.TerrainPreset.Lake:
                terrainGenerator.lakeTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Lake Terrain Layer",
                    terrainGenerator.lakeTerrainLayer, typeof(TerrainLayer), false);
                break;
            case TerrainGenerator.TerrainPreset.Canyons:
                terrainGenerator.canyonsTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Canyons Terrain Layer",
                    terrainGenerator.canyonsTerrainLayer, typeof(TerrainLayer), false);
                break;
        }

        terrainGenerator.waterTerrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Water Layer",
            terrainGenerator.waterTerrainLayer, typeof(TerrainLayer), false);

        bool presetChanged = false;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(terrainGenerator, "Change Terrain Preset");
            terrainGenerator.terrainPreset = newPreset;
            presetChanged = true;
        }

        if (presetChanged)
        {
            terrainGenerator.ResetValuesForPreset();
        }

        GUILayout.Space(10);
        GUILayout.Label("Size/Resolution", EditorStyles.boldLabel);
        terrainGenerator.width = EditorGUILayout.IntField("Width", terrainGenerator.width);
        terrainGenerator.height = EditorGUILayout.IntField("Height", terrainGenerator.height);
        terrainGenerator.depth = EditorGUILayout.IntField("Depth", terrainGenerator.depth);
        terrainGenerator.scale = EditorGUILayout.FloatField("Scale", terrainGenerator.scale);

        GUILayout.Space(10);
        GUILayout.Label("Water Bodies", EditorStyles.boldLabel);
        terrainGenerator.waterLevel = EditorGUILayout.Slider("Water Level Height", terrainGenerator.waterLevel, 0f, 0.5f);

        if (terrainGenerator.terrainPreset == TerrainGenerator.TerrainPreset.Lake)
        {
            terrainGenerator.numberOfLakes = EditorGUILayout.IntField("Number of Lakes", terrainGenerator.numberOfLakes);
            terrainGenerator.lakeRadius = EditorGUILayout.IntField("Lake Radius", terrainGenerator.lakeRadius);
        }

        GUILayout.Space(10);
        GUILayout.Label("Seed", EditorStyles.boldLabel);
        terrainGenerator.useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", terrainGenerator.useRandomSeed);
        if (!terrainGenerator.useRandomSeed)
        {
            terrainGenerator.seed = EditorGUILayout.IntField("Seed", terrainGenerator.seed);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(terrainGenerator);
        }

        if (GUILayout.Button("Generate"))
        {
            terrainGenerator.Generate();
        }
    }
}
