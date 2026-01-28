using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generates a complex KartRider-style track with curves, straights, and elevation changes.
/// Use this in the Unity Editor to create track geometry.
/// </summary>
public class TrackGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    [SerializeField] private float trackWidth = 15f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private Material trackMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material finishLineMaterial;

    [Header("Track Points")]
    [SerializeField] private List<TrackPoint> trackPoints = new List<TrackPoint>();

    [System.Serializable]
    public class TrackPoint
    {
        public Vector3 position;
        public float width = 15f;
        public float bankAngle = 0f; // For banked turns
        public bool isCheckpoint = false;
    }

    [ContextMenu("Generate Default KartRider Track")]
    public void GenerateDefaultTrack()
    {
        trackPoints.Clear();

        // Complex KartRider-style track layout
        // Start/Finish straight
        trackPoints.Add(new TrackPoint { position = new Vector3(0, 0, 0), width = 18f });
        trackPoints.Add(new TrackPoint { position = new Vector3(0, 0, 50), width = 18f });

        // First turn (right hairpin)
        trackPoints.Add(new TrackPoint { position = new Vector3(15, 0, 70), width = 16f, bankAngle = 15f });
        trackPoints.Add(new TrackPoint { position = new Vector3(40, 0, 80), width = 14f, bankAngle = 20f, isCheckpoint = true });
        trackPoints.Add(new TrackPoint { position = new Vector3(60, 0, 70), width = 14f, bankAngle = 15f });

        // Back straight with slight curve
        trackPoints.Add(new TrackPoint { position = new Vector3(80, 0, 50), width = 15f });
        trackPoints.Add(new TrackPoint { position = new Vector3(90, 2, 20), width = 15f }); // Slight elevation
        trackPoints.Add(new TrackPoint { position = new Vector3(85, 3, -10), width = 15f, isCheckpoint = true });

        // S-Curve section
        trackPoints.Add(new TrackPoint { position = new Vector3(70, 2, -30), width = 13f, bankAngle = -10f });
        trackPoints.Add(new TrackPoint { position = new Vector3(50, 1, -40), width = 12f, bankAngle = 10f });
        trackPoints.Add(new TrackPoint { position = new Vector3(30, 0, -30), width = 13f, bankAngle = -10f, isCheckpoint = true });

        // Long sweeping turn
        trackPoints.Add(new TrackPoint { position = new Vector3(10, 0, -20), width = 14f, bankAngle = 15f });
        trackPoints.Add(new TrackPoint { position = new Vector3(-10, 0, 0), width = 15f, bankAngle = 10f });

        // Tunnel section (narrow)
        trackPoints.Add(new TrackPoint { position = new Vector3(-20, 0, 30), width = 10f, isCheckpoint = true });
        trackPoints.Add(new TrackPoint { position = new Vector3(-25, 0, 50), width = 10f });

        // Final turn back to start
        trackPoints.Add(new TrackPoint { position = new Vector3(-15, 0, 70), width = 14f, bankAngle = 20f });
        trackPoints.Add(new TrackPoint { position = new Vector3(0, 0, 80), width = 16f, bankAngle = 15f });

        // Connect back to start
        trackPoints.Add(new TrackPoint { position = new Vector3(0, 0, 60), width = 18f });

        Debug.Log("Default track generated with " + trackPoints.Count + " points");
    }

    [ContextMenu("Build Track Mesh")]
    public void BuildTrackMesh()
    {
        if (trackPoints.Count < 2)
        {
            Debug.LogError("Need at least 2 track points!");
            return;
        }

        // Clear existing track objects
        ClearTrack();

        // Create track road
        GameObject trackRoad = CreateTrackRoad();
        trackRoad.transform.parent = transform;

        // Create walls
        GameObject leftWall = CreateWall(true);
        leftWall.transform.parent = transform;

        GameObject rightWall = CreateWall(false);
        rightWall.transform.parent = transform;

        // Create checkpoints
        CreateCheckpoints();

        // Create finish line
        CreateFinishLine();

        Debug.Log("Track built successfully!");
    }

    private void ClearTrack()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    private GameObject CreateTrackRoad()
    {
        GameObject road = new GameObject("TrackRoad");
        MeshFilter meshFilter = road.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = road.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = road.AddComponent<MeshCollider>();

        Mesh mesh = GenerateRoadMesh();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        if (trackMaterial != null)
            meshRenderer.material = trackMaterial;

        road.layer = LayerMask.NameToLayer("Default");

        return road;
    }

    private Mesh GenerateRoadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "TrackRoadMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < trackPoints.Count; i++)
        {
            TrackPoint current = trackPoints[i];
            TrackPoint next = trackPoints[(i + 1) % trackPoints.Count];

            Vector3 direction = (next.position - current.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            // Apply bank angle
            Quaternion bankRotation = Quaternion.AngleAxis(current.bankAngle, direction);
            right = bankRotation * right;

            float halfWidth = current.width / 2f;

            Vector3 leftPoint = current.position - right * halfWidth;
            Vector3 rightPoint = current.position + right * halfWidth;

            vertices.Add(leftPoint);
            vertices.Add(rightPoint);

            float uvY = i / (float)trackPoints.Count;
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
        }

        // Create triangles
        for (int i = 0; i < trackPoints.Count; i++)
        {
            int next = (i + 1) % trackPoints.Count;

            int bl = i * 2;
            int br = i * 2 + 1;
            int tl = next * 2;
            int tr = next * 2 + 1;

            // Two triangles per segment
            triangles.Add(bl);
            triangles.Add(tl);
            triangles.Add(br);

            triangles.Add(br);
            triangles.Add(tl);
            triangles.Add(tr);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private GameObject CreateWall(bool isLeft)
    {
        GameObject wall = new GameObject(isLeft ? "LeftWall" : "RightWall");
        MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wall.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = wall.AddComponent<MeshCollider>();

        Mesh mesh = GenerateWallMesh(isLeft);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        if (wallMaterial != null)
            meshRenderer.material = wallMaterial;

        wall.tag = "Wall";

        return wall;
    }

    private Mesh GenerateWallMesh(bool isLeft)
    {
        Mesh mesh = new Mesh();
        mesh.name = isLeft ? "LeftWallMesh" : "RightWallMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < trackPoints.Count; i++)
        {
            TrackPoint current = trackPoints[i];
            TrackPoint next = trackPoints[(i + 1) % trackPoints.Count];

            Vector3 direction = (next.position - current.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            Quaternion bankRotation = Quaternion.AngleAxis(current.bankAngle, direction);
            right = bankRotation * right;

            float halfWidth = current.width / 2f;
            float side = isLeft ? -1f : 1f;

            Vector3 basePoint = current.position + right * halfWidth * side;
            Vector3 topPoint = basePoint + Vector3.up * wallHeight;

            vertices.Add(basePoint);
            vertices.Add(topPoint);

            float uvY = i / (float)trackPoints.Count;
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
        }

        // Create triangles (flip winding for inside faces)
        for (int i = 0; i < trackPoints.Count; i++)
        {
            int next = (i + 1) % trackPoints.Count;

            int bl = i * 2;
            int tl = i * 2 + 1;
            int br = next * 2;
            int tr = next * 2 + 1;

            if (isLeft)
            {
                triangles.Add(bl);
                triangles.Add(br);
                triangles.Add(tl);

                triangles.Add(tl);
                triangles.Add(br);
                triangles.Add(tr);
            }
            else
            {
                triangles.Add(bl);
                triangles.Add(tl);
                triangles.Add(br);

                triangles.Add(br);
                triangles.Add(tl);
                triangles.Add(tr);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void CreateCheckpoints()
    {
        GameObject checkpointsParent = new GameObject("Checkpoints");
        checkpointsParent.transform.parent = transform;

        CheckpointManager manager = checkpointsParent.AddComponent<CheckpointManager>();

        int checkpointIndex = 0;

        for (int i = 0; i < trackPoints.Count; i++)
        {
            if (trackPoints[i].isCheckpoint)
            {
                TrackPoint current = trackPoints[i];
                TrackPoint next = trackPoints[(i + 1) % trackPoints.Count];

                Vector3 direction = (next.position - current.position).normalized;

                GameObject checkpoint = new GameObject($"Checkpoint_{checkpointIndex}");
                checkpoint.transform.parent = checkpointsParent.transform;
                checkpoint.transform.position = current.position + Vector3.up * 1.5f;
                checkpoint.transform.rotation = Quaternion.LookRotation(direction);

                BoxCollider collider = checkpoint.AddComponent<BoxCollider>();
                collider.size = new Vector3(current.width, 4f, 2f);
                collider.isTrigger = true;

                Checkpoint cp = checkpoint.AddComponent<Checkpoint>();
                // Set checkpoint index via reflection or serialized field
                var indexField = typeof(Checkpoint).GetField("checkpointIndex",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (indexField != null)
                    indexField.SetValue(cp, checkpointIndex);

                checkpointIndex++;
            }
        }
    }

    private void CreateFinishLine()
    {
        TrackPoint start = trackPoints[0];
        TrackPoint next = trackPoints[1];

        Vector3 direction = (next.position - start.position).normalized;

        GameObject finishLine = new GameObject("FinishLine");
        finishLine.transform.parent = transform;
        finishLine.transform.position = start.position + Vector3.up * 0.01f;
        finishLine.transform.rotation = Quaternion.LookRotation(direction);

        // Trigger collider
        BoxCollider collider = finishLine.AddComponent<BoxCollider>();
        collider.size = new Vector3(start.width, 4f, 2f);
        collider.center = new Vector3(0, 2f, 0);
        collider.isTrigger = true;

        Checkpoint cp = finishLine.AddComponent<Checkpoint>();
        var finishField = typeof(Checkpoint).GetField("isFinishLine",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (finishField != null)
            finishField.SetValue(cp, true);

        // Visual finish line
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "FinishLineVisual";
        visual.transform.parent = finishLine.transform;
        visual.transform.localPosition = new Vector3(0, 0.05f, 0);
        visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        visual.transform.localScale = new Vector3(start.width, 3f, 1f);

        DestroyImmediate(visual.GetComponent<Collider>());

        if (finishLineMaterial != null)
            visual.GetComponent<MeshRenderer>().material = finishLineMaterial;
    }

    private void OnDrawGizmos()
    {
        if (trackPoints.Count < 2) return;

        // Draw track path
        Gizmos.color = Color.yellow;
        for (int i = 0; i < trackPoints.Count; i++)
        {
            TrackPoint current = trackPoints[i];
            TrackPoint next = trackPoints[(i + 1) % trackPoints.Count];

            Gizmos.DrawLine(current.position, next.position);
            Gizmos.DrawWireSphere(current.position, 1f);

            // Draw width
            Vector3 direction = (next.position - current.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            Gizmos.color = current.isCheckpoint ? Color.green : Color.yellow;
            Gizmos.DrawLine(
                current.position - right * current.width / 2f,
                current.position + right * current.width / 2f
            );
        }
    }
}
