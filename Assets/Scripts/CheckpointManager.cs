using UnityEngine;
using System;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();
    [SerializeField] private Checkpoint finishLine;

    private HashSet<int> passedCheckpoints = new HashSet<int>();

    public int TotalCheckpoints => checkpoints.Count;
    public int PassedCheckpointsCount => passedCheckpoints.Count;

    public event Action<int> OnCheckpointHit; // checkpoint index
    public event Action OnFinishLineCrossed;
    public event Action OnAllCheckpointsPassed;

    private void Awake()
    {
        // Auto-find checkpoints if not assigned
        if (checkpoints.Count == 0)
        {
            FindCheckpointsInChildren();
        }

        // Subscribe to checkpoint events
        foreach (var checkpoint in checkpoints)
        {
            if (checkpoint != null)
            {
                checkpoint.OnCheckpointTriggered += HandleCheckpointTriggered;
            }
        }

        if (finishLine != null)
        {
            finishLine.OnCheckpointTriggered += HandleFinishLineTriggered;
        }
    }

    private void FindCheckpointsInChildren()
    {
        Checkpoint[] foundCheckpoints = GetComponentsInChildren<Checkpoint>();

        foreach (var cp in foundCheckpoints)
        {
            if (cp.IsFinishLine)
            {
                finishLine = cp;
            }
            else
            {
                checkpoints.Add(cp);
            }
        }

        // Sort checkpoints by index
        checkpoints.Sort((a, b) => a.CheckpointIndex.CompareTo(b.CheckpointIndex));
    }

    private void HandleCheckpointTriggered(Checkpoint checkpoint)
    {
        if (!checkpoint.IsFinishLine && !passedCheckpoints.Contains(checkpoint.CheckpointIndex))
        {
            passedCheckpoints.Add(checkpoint.CheckpointIndex);
            OnCheckpointHit?.Invoke(checkpoint.CheckpointIndex);

            if (passedCheckpoints.Count >= checkpoints.Count)
            {
                OnAllCheckpointsPassed?.Invoke();
            }
        }
    }

    private void HandleFinishLineTriggered(Checkpoint checkpoint)
    {
        OnFinishLineCrossed?.Invoke();
    }

    public void ResetCheckpoints()
    {
        passedCheckpoints.Clear();

        foreach (var checkpoint in checkpoints)
        {
            if (checkpoint != null)
            {
                checkpoint.ResetCheckpoint();
            }
        }
    }

    public bool HasPassedAllCheckpoints()
    {
        return passedCheckpoints.Count >= checkpoints.Count;
    }

    private void OnDestroy()
    {
        foreach (var checkpoint in checkpoints)
        {
            if (checkpoint != null)
            {
                checkpoint.OnCheckpointTriggered -= HandleCheckpointTriggered;
            }
        }

        if (finishLine != null)
        {
            finishLine.OnCheckpointTriggered -= HandleFinishLineTriggered;
        }
    }
}
