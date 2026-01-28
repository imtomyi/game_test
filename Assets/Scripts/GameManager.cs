using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private float countdownTime = 3f;

    [Header("References")]
    [SerializeField] private KartController playerKart;
    [SerializeField] private Transform startPosition;
    [SerializeField] private CheckpointManager checkpointManager;

    // Race state
    public enum RaceState { Waiting, Countdown, Racing, Finished }
    private RaceState currentState = RaceState.Waiting;

    // Timing
    private float raceStartTime;
    private float currentLapStartTime;
    private float totalRaceTime;
    private float currentLapTime;
    private float bestLapTime = float.MaxValue;
    private List<float> lapTimes = new List<float>();

    // Lap tracking
    private int currentLap = 0;
    private int checkpointsHit = 0;
    private int totalCheckpoints;

    // Events
    public event Action<RaceState> OnRaceStateChanged;
    public event Action<int> OnCountdownTick;
    public event Action<int, int> OnLapCompleted; // currentLap, totalLaps
    public event Action<float> OnLapTimeUpdated;
    public event Action<float> OnBestLapTimeUpdated;
    public event Action<float> OnTotalTimeUpdated;
    public event Action<float> OnRaceFinished; // total time

    // Properties
    public RaceState CurrentState => currentState;
    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public float CurrentLapTime => currentLapTime;
    public float BestLapTime => bestLapTime;
    public float TotalRaceTime => totalRaceTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (checkpointManager != null)
        {
            totalCheckpoints = checkpointManager.TotalCheckpoints;
            checkpointManager.OnCheckpointHit += HandleCheckpointHit;
            checkpointManager.OnFinishLineCrossed += HandleFinishLineCrossed;
        }

        // Auto-start race after a short delay
        StartCoroutine(AutoStartRace());
    }

    private IEnumerator AutoStartRace()
    {
        yield return new WaitForSeconds(1f);
        StartRace();
    }

    private void Update()
    {
        if (currentState == RaceState.Racing)
        {
            UpdateTiming();
        }

        // Debug restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartRace();
        }
    }

    private void UpdateTiming()
    {
        totalRaceTime = Time.time - raceStartTime;
        currentLapTime = Time.time - currentLapStartTime;

        OnLapTimeUpdated?.Invoke(currentLapTime);
        OnTotalTimeUpdated?.Invoke(totalRaceTime);
    }

    public void StartRace()
    {
        if (currentState != RaceState.Waiting) return;

        // Reset kart position
        if (playerKart != null && startPosition != null)
        {
            playerKart.ResetKart(startPosition.position, startPosition.rotation);
        }

        StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        currentState = RaceState.Countdown;
        OnRaceStateChanged?.Invoke(currentState);

        int count = Mathf.CeilToInt(countdownTime);

        for (int i = count; i > 0; i--)
        {
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }

        // GO!
        OnCountdownTick?.Invoke(0);

        // Start the race
        currentState = RaceState.Racing;
        OnRaceStateChanged?.Invoke(currentState);

        raceStartTime = Time.time;
        currentLapStartTime = Time.time;
        currentLap = 1;

        if (playerKart != null)
        {
            playerKart.EnableControl(true);
        }

        OnLapCompleted?.Invoke(currentLap, totalLaps);
    }

    private void HandleCheckpointHit(int checkpointIndex)
    {
        if (currentState != RaceState.Racing) return;

        checkpointsHit++;
        Debug.Log($"Checkpoint {checkpointIndex + 1}/{totalCheckpoints} hit!");
    }

    private void HandleFinishLineCrossed()
    {
        if (currentState != RaceState.Racing) return;

        // Check if all checkpoints were hit
        if (checkpointsHit >= totalCheckpoints)
        {
            CompleteLap();
        }
        else
        {
            Debug.Log($"Must pass all checkpoints! ({checkpointsHit}/{totalCheckpoints})");
        }
    }

    private void CompleteLap()
    {
        float lapTime = currentLapTime;
        lapTimes.Add(lapTime);

        // Check for best lap
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;
            OnBestLapTimeUpdated?.Invoke(bestLapTime);
        }

        Debug.Log($"Lap {currentLap} completed in {FormatTime(lapTime)}");

        // Reset for next lap
        checkpointsHit = 0;
        if (checkpointManager != null)
        {
            checkpointManager.ResetCheckpoints();
        }

        if (currentLap >= totalLaps)
        {
            // Race finished
            FinishRace();
        }
        else
        {
            // Next lap
            currentLap++;
            currentLapStartTime = Time.time;
            OnLapCompleted?.Invoke(currentLap, totalLaps);
        }
    }

    private void FinishRace()
    {
        currentState = RaceState.Finished;
        OnRaceStateChanged?.Invoke(currentState);

        if (playerKart != null)
        {
            playerKart.EnableControl(false);
        }

        OnRaceFinished?.Invoke(totalRaceTime);
        Debug.Log($"Race Finished! Total time: {FormatTime(totalRaceTime)}");
    }

    public void RestartRace()
    {
        StopAllCoroutines();

        currentState = RaceState.Waiting;
        currentLap = 0;
        checkpointsHit = 0;
        totalRaceTime = 0;
        currentLapTime = 0;
        bestLapTime = float.MaxValue;
        lapTimes.Clear();

        if (checkpointManager != null)
        {
            checkpointManager.ResetCheckpoints();
        }

        if (playerKart != null)
        {
            playerKart.EnableControl(false);
            if (startPosition != null)
            {
                playerKart.ResetKart(startPosition.position, startPosition.rotation);
            }
        }

        OnRaceStateChanged?.Invoke(currentState);
        StartRace();
    }

    public static string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    private void OnDestroy()
    {
        if (checkpointManager != null)
        {
            checkpointManager.OnCheckpointHit -= HandleCheckpointHit;
            checkpointManager.OnFinishLineCrossed -= HandleFinishLineCrossed;
        }
    }
}
