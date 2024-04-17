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
    public TerrainLayer waterTerrainLayer;

    public Material waterMaterial;

    private Terrain terrain;
    private GameObject waterPlane;

    public enum TerrainPreset { Grasslands, Desert, Mountainous, Lake }
    public TerrainPreset terrainPreset = TerrainPreset.Grasslands;
    private TerrainPreset lastPreset = TerrainPreset.Grasslands;

    public void Generate()
    {
        if (terrain != null)
        {
            DestroyImmediate(terrain.gameObject);
        }

        GameObject generatedTerrain = GameObject.Find("GeneratedTerrain") ?? new GameObject("GeneratedTerrain");

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
        terrain.transform.parent = generatedTerrain.transform;

        PaintWaterBodies();
        GenerateWaterPlane();
        waterPlane.transform.parent = generatedTerrain.transform;
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
            size = new Vector3(width, depth, height),
            alphamapResolution = 1024
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

    private float[,] GenerateHeights()
    {
        return terrainPreset switch
        {
            TerrainPreset.Grasslands => GenerateHeightsForGrasslands(),
            TerrainPreset.Desert => GenerateHeightsForDesert(),
            TerrainPreset.Mountainous => GenerateHeightsForMountainous(),
            TerrainPreset.Lake => GenerateHeightsForLake(),
            _ => new float[width, height],
        };
    }

    private float[,] CarveWaterBodies(float[,] heights)
    {
        if (terrainPreset != TerrainPreset.Lake)
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
        float[,] smoothedHeights = (float[,])heights.Clone();

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                float totalHeight = 0f;
                int count = 0;
                for (int nx = -1; nx <= 1; nx++)
                {
                    for (int nz = -1; nz <= 1; nz++)
                    {
                        totalHeight += heights[x + nx, z + nz];
                        count++;
                    }
                }
                smoothedHeights[x, z] = totalHeight / count;

                float currentHeight = heights[x, z];
                if (currentHeight <= waterLevel + 0.02 && currentHeight >= waterLevel - 0.02)
                {
                    smoothedHeights[x, z] = Mathf.Lerp(smoothedHeights[x, z], waterLevel, 0.5f);
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
            TerrainPreset.Grasslands => new TerrainLayer[] { grasslandsTerrainLayer, waterTerrainLayer },
            TerrainPreset.Desert => new TerrainLayer[] { desertTerrainLayer, waterTerrainLayer },
            TerrainPreset.Mountainous => new TerrainLayer[] { mountainTerrainLayer, waterTerrainLayer },
            TerrainPreset.Lake => new TerrainLayer[] { lakeTerrainLayer, waterTerrainLayer },
            _ => new TerrainLayer[] { grasslandsTerrainLayer, waterTerrainLayer }
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

                float blendRange = 0.01f;
                float lowerBound = waterLevel - blendRange;
                float upperBound = waterLevel + blendRange;
                float blendFactor = (heightValue - lowerBound) / (upperBound - lowerBound);
                blendFactor = Mathf.Clamp01(blendFactor);

                alphaMap[x, y, 0] = blendFactor;
                alphaMap[x, y, 1] = 1 - blendFactor;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    private void GenerateWaterPlane()
    {
        if (waterPlane != null)
        {
            DestroyImmediate(waterPlane);
        }

        waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "Terrain_WaterPlane";

        float waterPlaneHeight = waterLevel * depth - 0.1f;
        waterPlane.transform.position = new Vector3(width / 2, waterPlaneHeight, height / 2);
        waterPlane.transform.localScale = new Vector3(width / 10.0f, 1, height / 10.0f);
        waterPlane.GetComponent<Renderer>().material = waterMaterial;
        DestroyImmediate(waterPlane.GetComponent<Collider>());
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
        }
    }
}
