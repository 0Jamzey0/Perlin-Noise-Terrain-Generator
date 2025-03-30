using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class PerlinTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int heightmapResolution = 513;
    public float heightMultiplier = 25f;
    public int seed = 42;

    [Header("Noise Settings")]
    public float baseScale = 0.01f;
    public int octaves = 5;
    public float lacunarity = 2.0f;      // Frequency multiplier per octave
    public float persistence = 0.5f;     // Amplitude multiplier per octave

    private Terrain terrain;
    private float[,] heightMap;
    private int[] perm;
    private Vector2[] gradients;

    private void Start()
    {
        terrain = GetComponent<Terrain>();
        RegenerateTerrain();
    }

    [ContextMenu("Regenerate Perlin Terrain")]
    public void RegenerateTerrain()
    {
        GeneratePermutationTable(seed);
        GenerateHeightMap();
        ApplyHeightMap();
    }

    private void GeneratePermutationTable(int seed)
    {
        perm = new int[512];
        gradients = new Vector2[256];

        System.Random rand = new System.Random(seed);
        int[] p = new int[256];

        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
            float angle = (float)(rand.NextDouble() * Mathf.PI * 2);
            gradients[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        for (int i = 255; i > 0; i--)
        {
            int swap = rand.Next(i + 1);
            int temp = p[i];
            p[i] = p[swap];
            p[swap] = temp;
        }

        for (int i = 0; i < 512; i++)
            perm[i] = p[i % 256];
    }

    private void GenerateHeightMap()
    {
        heightMap = new float[heightmapResolution, heightmapResolution];

        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = x * baseScale * frequency;
                    float sampleY = y * baseScale * frequency;

                    float perlinValue = Perlin(sampleX, sampleY) * 2 - 1; // [-1,1]
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Normalize from [-1,1] to [0,1]
                noiseHeight = (noiseHeight + 1f) / 2f;

                heightMap[y, x] = noiseHeight * heightMultiplier / terrain.terrainData.size.y;
            }
        }
    }

    private void ApplyHeightMap()
    {
        terrain.terrainData.heightmapResolution = heightmapResolution;
        terrain.terrainData.SetHeights(0, 0, heightMap);
    }

    // === PERLIN NOISE ===

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    private float Grad(int hash, float x, float y)
    {
        Vector2 g = gradients[hash % 256];
        return g.x * x + g.y * y;
    }

    private float Perlin(float x, float y)
    {
        int xi = Mathf.FloorToInt(x) & 255;
        int yi = Mathf.FloorToInt(y) & 255;

        float xf = x - Mathf.Floor(x);
        float yf = y - Mathf.Floor(y);

        float u = Fade(xf);
        float v = Fade(yf);

        int aa = perm[perm[xi] + yi];
        int ab = perm[perm[xi] + yi + 1];
        int ba = perm[perm[xi + 1] + yi];
        int bb = perm[perm[xi + 1] + yi + 1];

        float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

        float value = Lerp(x1, x2, v);
        return Mathf.InverseLerp(-1f, 1f, value);
    }
}
