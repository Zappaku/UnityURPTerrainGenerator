using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public int depth = 20;
    public float scale = 20f;

    public bool useRandomSeed = false;
    public int seed;

    public float waterLevel = 0.1f;

    public TerrainLayer grasslandsTerrainLayer;
    public TerrainLayer desertTerrainLayer;
    public TerrainLayer mountainTerrainLayer;
    public TerrainLayer waterTerrainLayer;
    public TerrainLayer cliffTerrainLayer;

    private Terrain terrain;

    public enum TerrainPreset { Grasslands, Desert, Mountainous }
    public TerrainPreset terrainPreset = TerrainPreset.Grasslands;
    private TerrainPreset lastPreset = TerrainPreset.Grasslands;

    public void Generate()
    {
        if (terrain != null)
        {
            DestroyImmediate(terrain.gameObject);
        }

        if (terrainPreset != lastPreset)
        {
            ResetValuesForPreset();
            lastPreset = terrainPreset;
        }

        if (useRandomSeed)
        {
            seed = Random.Range(0, 100000);
        }

        Random.InitState(seed);
        terrain = GenerateTerrain();
        terrain.terrainData = GenerateTerrainData();

        PaintWaterAndCliffAreas();
    }

    private int GetBaseLayerIndex()
    {
        switch (terrainPreset)
        {
            case TerrainPreset.Grasslands:
                return 0;
            case TerrainPreset.Desert:
                return 1;
            case TerrainPreset.Mountainous:
                return 2;
            default:
                return 0;
        }
    }

        private Terrain GenerateTerrain()
    {
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = width + 1,
            size = new Vector3(width, depth, height)
        };
        terrainData.SetHeights(0, 0, GenerateHeights());

        Terrain terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        return terrain;
    }

    private TerrainData GenerateTerrainData()
    {
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = width + 1,
            size = new Vector3(width, depth, height)
        };
        terrainData.SetHeights(0, 0, GenerateHeights());

        if (terrainPreset == TerrainPreset.Grasslands && desertTerrainLayer != null)
        {
            terrainData.terrainLayers = new TerrainLayer[1] { grasslandsTerrainLayer };
        }

        if (terrainPreset == TerrainPreset.Desert && desertTerrainLayer != null)
        {
            terrainData.terrainLayers = new TerrainLayer[1] { desertTerrainLayer };
        }

        if (terrainPreset == TerrainPreset.Mountainous && mountainTerrainLayer != null)
        {
            terrainData.terrainLayers = new TerrainLayer[1] { mountainTerrainLayer };
        }

        return terrainData;
    }

    private float[,] GenerateHeightsForGrasslands()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xCoord = ((float)x / width) * scale + seed;
                float zCoord = ((float)z / height) * scale + seed;

                float baseHeight = Mathf.PerlinNoise(xCoord, zCoord);

                float detailScale = scale * 0.5f;
                float detailHeight = Mathf.PerlinNoise(xCoord * detailScale + seed, zCoord * detailScale + seed) * 0.1f;

                heights[x, z] = Mathf.Lerp(baseHeight, detailHeight + baseHeight * 0.1f, 0.5f);
            }
        }

        heights = CarveWaterBodies(heights);
        heights = SmoothTerrain(heights);
        return heights;
    }

    private float[,] GenerateHeightsForDesert()
    {
        float[,] heights = new float[width, height];
        float duneScale = scale * 0.1f;
        float maxHeight = depth * 0.1f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xCoord = (x + seed) / duneScale;
                float zCoord = (z + seed) / duneScale;
                float duneHeight = Mathf.PerlinNoise(xCoord, zCoord) * maxHeight;

                float noiseDetail = Mathf.PerlinNoise(xCoord * 2f, zCoord * 2f) * (maxHeight / 3f);
                duneHeight += noiseDetail;

                heights[x, z] = duneHeight / depth;
            }
        }

        heights = SmoothTerrain(heights);
        return heights;
    }

    private float[,] GenerateHeightsForMountainous()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xCoord = ((float)x / width * scale * 0.3f) + seed;
                float zCoord = ((float)z / height * scale * 0.3f) + seed;

                float baseHeight = Mathf.PerlinNoise(xCoord, zCoord);

                float detailScale = 50f;
                float detailHeight = Mathf.PerlinNoise(xCoord * detailScale, zCoord * detailScale) * 0.1f;

                heights[x, z] = Mathf.Lerp(baseHeight, detailHeight, 0.1f);
            }
        }

        heights = CarveWaterBodies(heights);
        heights = SmoothTerrain(heights);
        return heights;
    }

    private float[,] CarveWaterBodies(float[,] heights)
    {
        float waterThreshold = 0.1f;
        float waterDepth = 0.05f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (heights[x, z] < waterThreshold)
                {
                    heights[x, z] *= waterDepth;
                }
            }
        }

        return heights;
    }

    private float[,] GenerateHeights()
    {
        switch (terrainPreset)
        {
            case TerrainPreset.Grasslands:
                return GenerateHeightsForGrasslands();
            case TerrainPreset.Desert:
                return GenerateHeightsForDesert();
            case TerrainPreset.Mountainous:
                return GenerateHeightsForMountainous();
            default:
                return new float[width, height];
        }
    }

    private float[,] SmoothTerrain(float[,] heights)
    {
        float steepnessThreshold = 0.5f;

        float[,] smoothedHeights = (float[,])heights.Clone();

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                float steepness = Mathf.Abs(heights[x, z] - heights[x + 1, z]) + Mathf.Abs(heights[x, z] - heights[x, z + 1]);

                if (steepness < steepnessThreshold)
                {
                    float totalHeight = 0f;
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int nz = -1; nz <= 1; nz++)
                        {
                            totalHeight += heights[x + nx, z + nz];
                        }
                    }
                    smoothedHeights[x, z] = totalHeight / 9f;
                }
            }
        }

        return smoothedHeights;
    }

    private void PaintWaterAndCliffAreas()
    {
        TerrainData terrainData = terrain.terrainData;
        var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        TerrainLayer[] newLayers = { grasslandsTerrainLayer, waterTerrainLayer, cliffTerrainLayer };
        terrainData.terrainLayers = newLayers;

        float[,,] alphaMap = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, newLayers.Length];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float normalizedX = (float)x / terrainData.alphamapWidth;
                float normalizedY = (float)y / terrainData.alphamapHeight;

                float steepness = terrainData.GetSteepness(normalizedX, normalizedY) * Mathf.Deg2Rad;
                float heightValue = heights[x * terrainData.heightmapResolution / terrainData.alphamapWidth, y * terrainData.heightmapResolution / terrainData.alphamapHeight];

                for (int layer = 0; layer < newLayers.Length; layer++)
                {
                    alphaMap[x, y, layer] = 0f;
                }

                int baseLayerIndex = GetBaseLayerIndex();

                bool isAboveWater = heightValue > waterLevel;
                if (isAboveWater)
                {
                    float angle = Mathf.Rad2Deg * Mathf.Asin(steepness);
                    if (angle >= 35 && angle <= 90)
                    {
                        alphaMap[x, y, 2] = 1;
                    }
                    else
                    {
                        alphaMap[x, y, baseLayerIndex] = 1;
                    }
                }
                else
                {
                    alphaMap[x, y, 1] = 1;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    public void ResetValuesForPreset()
    {
        switch (terrainPreset)
        {
            case TerrainPreset.Grasslands:
                scale = 15; depth = 10; waterLevel = 0.1f;
                break;
            case TerrainPreset.Desert:
                scale = 150; depth = 25; waterLevel = 0.00f;
                break;
            case TerrainPreset.Mountainous:
                scale = 25; depth = 50; waterLevel = 0.3f;
                break;
        }
    }
}
