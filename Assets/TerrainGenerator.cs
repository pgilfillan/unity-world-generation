using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int chunkSize = 100;
    [Range(2, 100)]
    public int chunkRingSize = 2;
    public float frequency = 8.0f;
    public int amplitude = 10;
    public const int viewDistance = 200;

    Transform chunkRoot;
    Transform transformComponent;
    Vector3 currPosition;
    int numChunksWidth;
    Dictionary<Vector2, TerrainChunk> loadedChunks = new Dictionary<Vector2, TerrainChunk>();
    Dictionary<Vector2, TerrainChunk> unloadedChunks = new Dictionary<Vector2, TerrainChunk>();
    NoiseGenerator noiseGenerator;

    void Start()
    {
        chunkRoot = new GameObject("Chunk Root").transform;
        transformComponent = GetComponent<Transform>();
        currPosition = transformComponent.position;
        numChunksWidth = chunkRingSize * 2 - 1;
        noiseGenerator = new NoiseGenerator(frequency, amplitude);
        UpdateTerrainChunks();
    }

    void Update()
    {
        currPosition = transformComponent.position;
        foreach (var pair in loadedChunks)
        {
            pair.Value.UpdateTerrainChunk(currPosition, viewDistance);
        }
        foreach (var pair in unloadedChunks)
        {
            pair.Value.UpdateTerrainChunk(currPosition, viewDistance);
        }
        UpdateTerrainChunks();
    }

    void UpdateTerrainChunks()
    {
        int currChunkX = Mathf.RoundToInt(currPosition.x / chunkSize);
        int currChunkZ = Mathf.RoundToInt(currPosition.z / chunkSize);

        for (int i = -numChunksWidth/2; i <= numChunksWidth/2; ++i)
        {
            for (int j = -numChunksWidth/2; j <= numChunksWidth/2; ++j)
            {
                Vector2 chunkCoords = new Vector2(currChunkX + i, currChunkZ + j);
                if (unloadedChunks.ContainsKey(chunkCoords))
                {
                    if (unloadedChunks[chunkCoords].IsActive())
                    {
                        loadedChunks.Add(chunkCoords, unloadedChunks[chunkCoords]);
                        unloadedChunks.Remove(chunkCoords);
                    }
                }
                else if (loadedChunks.ContainsKey(chunkCoords))
                {
                    if (!loadedChunks[chunkCoords].IsActive())
                    {
                        unloadedChunks.Add(chunkCoords, loadedChunks[chunkCoords]);
                        loadedChunks.Remove(chunkCoords);
                    }
                }
                else
                {
                    Vector2 chunkCentrePos = new Vector2(chunkCoords.x * chunkSize, chunkCoords.y * chunkSize);
                    TerrainChunk newChunk = new TerrainChunk(chunkCentrePos, chunkSize, chunkRoot, noiseGenerator);
                    newChunk.UpdateTerrainChunk(currPosition, viewDistance);
                    if (newChunk.IsActive())
                    {
                        loadedChunks.Add(chunkCoords, newChunk);
                    }
                    else
                    {
                        unloadedChunks.Add(chunkCoords, newChunk);
                    }
                }
            }
        }
    }

    public class NoiseGenerator
    {
        float frequency;
        int amplitude;

        public NoiseGenerator(float frequency, int amplitude)
        {
            this.frequency = frequency;
            this.amplitude = amplitude;
        }

        public float Noise(float x, float y)
        {
            return Mathf.PerlinNoise(x / frequency, y / frequency) * amplitude;
        }
    }

    public class TerrainChunk
    {
        GameObject chunkObject;
        Bounds chunkBounds;

        public TerrainChunk(Vector2 centrePos, int size, Transform root, NoiseGenerator noiseGenerator)
        {
            GenerateMesh(centrePos, size, root, noiseGenerator);
        }

        void GenerateMesh(Vector2 centrePos, int size, Transform root, NoiseGenerator noiseGenerator)
        {
            // Create vertices
            Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];
            for (int i = -size / 2, k = 0 ; i <= size/2; ++i)
            {
                for (int j = -size/2; j <= size/2; ++j)
                {
                    vertices[k] = new Vector3(j, noiseGenerator.Noise(centrePos.x + j, centrePos.y + i), i);
                    ++k;
                }
            }

            // Create triangles
            int[] triangles = new int[size * size * 2 * 3];
            for (int i = 0, bottomLeft = 0, k = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    // Top left triangle
                    triangles[k] = bottomLeft;
                    triangles[k + 1] = bottomLeft + size + 1;
                    triangles[k + 2] = bottomLeft + size + 2;
                    // Bottom right triangle
                    triangles[k + 3] = bottomLeft;
                    triangles[k + 4] = bottomLeft + size + 2;
                    triangles[k + 5] = bottomLeft + 1;
                    k += 6;
                    ++bottomLeft;
                }
                ++bottomLeft;
            }

            // Create chunk
            chunkObject = new GameObject("Chunk");
            chunkObject.SetActive(false);
            chunkObject.GetComponent<Transform>().SetParent(root);
            chunkObject.AddComponent<MeshFilter>();
            chunkObject.AddComponent<MeshRenderer>();
            chunkObject.AddComponent<MeshCollider>();

            chunkBounds = new Bounds(new Vector3(centrePos.x, 0, centrePos.y), Vector3.one * size);

            // Set transform and mesh
            chunkObject.transform.position = new Vector3(centrePos.x, 0, centrePos.y);
            Mesh mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            chunkObject.GetComponent<MeshFilter>().mesh = mesh;
            chunkObject.GetComponent<MeshCollider>().sharedMesh = mesh;

            // Set material
            chunkObject.GetComponent<MeshRenderer>().materials[0] = Resources.Load<Material>("Default-Material");
        }

        public void UpdateTerrainChunk(Vector3 currPos, int viewDistance)
        {
            //Debug.Log(Mathf.Sqrt(chunkBounds.SqrDistance(currPos)));
            SetActive(Mathf.Sqrt(chunkBounds.SqrDistance(currPos)) <= viewDistance);
        }

        public bool IsActive()
        {
            return chunkObject.activeSelf;
        }

        void SetActive(bool value)
        {
            chunkObject.SetActive(value);
        }
    }
}