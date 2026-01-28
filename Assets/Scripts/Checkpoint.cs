using UnityEngine;
using System;

[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool isFinishLine = false;

    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer visual;
    [SerializeField] private Color activeColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color passedColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color finishLineColor = new Color(1f, 1f, 1f, 0.5f);

    private bool hasPassed = false;
    private BoxCollider triggerCollider;
    private Material checkpointMaterial;

    public int CheckpointIndex => checkpointIndex;
    public bool IsFinishLine => isFinishLine;
    public bool HasPassed => hasPassed;

    public event Action<Checkpoint> OnCheckpointTriggered;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Setup visual material
        if (visual != null)
        {
            checkpointMaterial = new Material(visual.material);
            visual.material = checkpointMaterial;
            UpdateVisual();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isFinishLine || !hasPassed)
            {
                hasPassed = true;
                OnCheckpointTriggered?.Invoke(this);
                UpdateVisual();
            }
        }
    }

    public void ResetCheckpoint()
    {
        hasPassed = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (checkpointMaterial == null) return;

        if (isFinishLine)
        {
            checkpointMaterial.color = finishLineColor;
        }
        else
        {
            checkpointMaterial.color = hasPassed ? passedColor : activeColor;
        }
    }

    private void OnDestroy()
    {
        if (checkpointMaterial != null)
        {
            Destroy(checkpointMaterial);
        }
    }

    // Editor helper to visualize checkpoint
    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = isFinishLine ? Color.white : (hasPassed ? Color.green : Color.yellow);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);

        // Draw checkpoint number
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            isFinishLine ? "FINISH" : $"CP {checkpointIndex}");
        #endif
    }
}
