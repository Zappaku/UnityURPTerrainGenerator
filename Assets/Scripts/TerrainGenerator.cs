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

    public int numberOfLakes = 3;
    public int lakeRadius = 50;

    public TerrainLayer grasslandsTerrainLayer;
    public TerrainLayer desertTerrainLayer;
    public TerrainLayer mountainTerrainLayer;
    public TerrainLayer lakeTerrainLayer;
    public TerrainLayer canyonsTerrainLayer;
    public TerrainLayer waterTerrainLayer;

    private Terrain terrain;

    public enum TerrainPreset { Grasslands, Desert, Mountainous, Lake, Canyons }
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

        PaintWaterBodies();
    }

    private Terrain GenerateTerrain()
    {
        TerrainData terrainData = new()
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
        TerrainData terrainData = new()
        {
            heightmapResolution = width + 1,
            size = new Vector3(width, depth, height)
        };
        terrainData.SetHeights(0, 0, GenerateHeights());

        switch (terrainPreset)
        {
            case TerrainPreset.Grasslands:
                terrainData.terrainLayers = new TerrainLayer[1] { grasslandsTerrainLayer };
                break;
            case TerrainPreset.Desert:
                terrainData.terrainLayers = new TerrainLayer[1] { desertTerrainLayer };
                break;
            case TerrainPreset.Mountainous:
                terrainData.terrainLayers = new TerrainLayer[1] { mountainTerrainLayer };
                break;
            case TerrainPreset.Lake:
                terrainData.terrainLayers = new TerrainLayer[1] { lakeTerrainLayer };
                break;
            case TerrainPreset.Canyons:
                terrainData.terrainLayers = new TerrainLayer[1] { canyonsTerrainLayer };
                break;
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

    private float[,] GenerateHeightsForLake()
    {
        float[,] heights = GenerateHeightsForGrasslands();

        List<Vector2Int> lakeCenters = new();
        for (int i = 0; i < numberOfLakes; i++)
        {
            int centerX = Random.Range(lakeRadius, width - lakeRadius);
            int centerY = Random.Range(lakeRadius, height - lakeRadius);
            lakeCenters.Add(new Vector2Int(centerX, centerY));
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                foreach (Vector2Int center in lakeCenters)
                {
                    float distance = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.y));
                    if (distance < lakeRadius)
                    {
                        float factor = (lakeRadius - distance) / lakeRadius;
                        heights[x, z] *= (1 - factor * 0.8f);
                    }
                }
            }
        }

        heights = SmoothTerrain(heights);
        return heights;
    }

    private float[,] GenerateHeightsForCanyons()
    {
        float[,] heights = new float[width, height];
        float baseHeight = 0.4f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xCoord = (float)x / width * scale * 2;
                float zCoord = (float)z / height * scale * 2;

                float terrainNoise = Mathf.PerlinNoise(xCoord * 0.05f, zCoord * 0.05f) * 0.5f;
                float canyonNoise = Mathf.PerlinNoise(xCoord, zCoord);

                if (canyonNoise < 0.4f)
                {
                    float canyonDepth = (0.4f - canyonNoise) * depth;
                    heights[x, z] = Mathf.Max(0, baseHeight + terrainNoise - canyonDepth);
                }
                else
                {
                    heights[x, z] = baseHeight + terrainNoise;
                }
            }
        }

        heights = SmoothTerrain(heights);
        return heights;
    }

    private float[,] GenerateHeights()
    {
        return terrainPreset switch
        {
            TerrainPreset.Grasslands => GenerateHeightsForGrasslands(),
            TerrainPreset.Desert => GenerateHeightsForDesert(),
            TerrainPreset.Mountainous => GenerateHeightsForMountainous(),
            TerrainPreset.Lake => GenerateHeightsForLake(),
            TerrainPreset.Canyons => GenerateHeightsForCanyons(),
            _ => new float[width, height],
        };
    }

    private float[,] CarveWaterBodies(float[,] heights)
    {
        if (terrainPreset != TerrainPreset.Lake && terrainPreset != TerrainPreset.Canyons)
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
        }
        return heights;
    }

    private float[,] SmoothTerrain(float[,] heights)
    {
        float smoothnessThreshold = 1f;
        float[,] smoothedHeights = (float[,])heights.Clone();

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                float steepness = Mathf.Abs(heights[x, z] - heights[x + 1, z]) + Mathf.Abs(heights[x, z] - heights[x, z + 1]);

                if (steepness < smoothnessThreshold)
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

    private void PaintWaterBodies()
    {
        TerrainData terrainData = terrain.terrainData;
        var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        TerrainLayer[] newLayers = terrainPreset switch
        {
            TerrainPreset.Grasslands => new TerrainLayer[] { grasslandsTerrainLayer, waterTerrainLayer},
            TerrainPreset.Desert => new TerrainLayer[] { desertTerrainLayer, waterTerrainLayer},
            TerrainPreset.Mountainous => new TerrainLayer[] { mountainTerrainLayer, waterTerrainLayer},
            TerrainPreset.Lake => new TerrainLayer[] { lakeTerrainLayer, waterTerrainLayer },
            TerrainPreset.Canyons => new TerrainLayer[] { canyonsTerrainLayer, waterTerrainLayer },
            _ => new TerrainLayer[] { grasslandsTerrainLayer, waterTerrainLayer}
        };
        terrainData.terrainLayers = newLayers;

        float[,,] alphaMap = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, newLayers.Length];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                _ = (float)x / terrainData.alphamapWidth;
                _ = (float)y / terrainData.alphamapHeight;
                float heightValue = heights[x * terrainData.heightmapResolution / terrainData.alphamapWidth, y * terrainData.heightmapResolution / terrainData.alphamapHeight];

                for (int layer = 0; layer < newLayers.Length; layer++)
                {
                    alphaMap[x, y, layer] = 0f;
                }

                bool isAboveWater = heightValue > waterLevel;
                if (isAboveWater)
                {
                    alphaMap[x, y, 0] = 1;
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
                scale = 15; depth = 50; waterLevel = 0.2f;
                break;
            case TerrainPreset.Lake:
                scale = 15; depth = 20; waterLevel = 0.2f; numberOfLakes = 1; lakeRadius = 200;
                break;
            case TerrainPreset.Canyons:
                scale = 5; depth = 20; waterLevel = 0.00f;
                break;
        }
    }
}
