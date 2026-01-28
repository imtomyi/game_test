using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private GameManager gameManager;

    [Header("Speed Display")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Image speedometerFill;

    [Header("Lap Display")]
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI bestLapText;
    [SerializeField] private TextMeshProUGUI totalTimeText;

    [Header("Booster Display")]
    [SerializeField] private Image driftChargeBar;
    [SerializeField] private Image[] boosterSlots; // 2 slots
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color filledSlotColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color boostingColor = new Color(1f, 0f, 0.5f);

    [Header("Countdown Display")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownPanel;

    [Header("Race Finish Display")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private TextMeshProUGUI finishTimeText;
    [SerializeField] private TextMeshProUGUI finishBestLapText;

    [Header("Message Display")]
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine messageCoroutine;

    private void Start()
    {
        // Subscribe to kart events
        if (kart != null)
        {
            kart.OnDriftChargeChanged += UpdateDriftCharge;
            kart.OnBoosterCountChanged += UpdateBoosterSlots;
            kart.OnBoostStateChanged += UpdateBoostState;
            kart.OnSpeedChanged += UpdateSpeed;
        }

        // Subscribe to game events
        if (gameManager != null)
        {
            gameManager.OnRaceStateChanged += HandleRaceStateChanged;
            gameManager.OnCountdownTick += UpdateCountdown;
            gameManager.OnLapCompleted += UpdateLapDisplay;
            gameManager.OnLapTimeUpdated += UpdateCurrentTime;
            gameManager.OnBestLapTimeUpdated += UpdateBestLap;
            gameManager.OnTotalTimeUpdated += UpdateTotalTime;
            gameManager.OnRaceFinished += ShowFinishScreen;
        }

        // Initialize UI
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Hide panels
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (finishPanel != null)
            finishPanel.SetActive(false);

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        // Initialize booster slots
        UpdateBoosterSlots(0);
        UpdateDriftCharge(0);

        // Initialize times
        if (currentTimeText != null)
            currentTimeText.text = "00:00.000";

        if (bestLapText != null)
            bestLapText.text = "--:--.---";

        if (totalTimeText != null)
            totalTimeText.text = "00:00.000";

        if (lapText != null)
            lapText.text = "Lap 0/3";
    }

    private void UpdateSpeed(float speed)
    {
        int displaySpeed = Mathf.RoundToInt(Mathf.Abs(speed) * 10f); // Convert to "km/h"

        if (speedText != null)
            speedText.text = $"{displaySpeed}";

        if (speedometerFill != null && kart != null)
        {
            float fillAmount = Mathf.Abs(speed) / (kart.MaxSpeed * 1.8f); // Account for boost
            speedometerFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    private void UpdateDriftCharge(float chargePercent)
    {
        if (driftChargeBar != null)
        {
            driftChargeBar.fillAmount = chargePercent / 100f;

            // Change color based on charge level
            if (chargePercent >= 80f)
                driftChargeBar.color = Color.cyan;
            else if (chargePercent >= 50f)
                driftChargeBar.color = Color.yellow;
            else
                driftChargeBar.color = Color.white;
        }
    }

    private void UpdateBoosterSlots(int filledCount)
    {
        if (boosterSlots == null) return;

        for (int i = 0; i < boosterSlots.Length; i++)
        {
            if (boosterSlots[i] != null)
            {
                boosterSlots[i].color = i < filledCount ? filledSlotColor : emptySlotColor;
            }
        }
    }

    private void UpdateBoostState(bool isBoosting)
    {
        // Animate booster slots when boosting
        if (isBoosting && boosterSlots != null)
        {
            StartCoroutine(BoostAnimation());
        }
    }

    private IEnumerator BoostAnimation()
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration && kart != null && kart.IsBoosting)
        {
            // Flash effect
            float flash = Mathf.PingPong(elapsed * 10f, 1f);
            Color currentColor = Color.Lerp(boostingColor, Color.white, flash);

            // Apply to speedometer or other UI elements
            if (speedometerFill != null)
                speedometerFill.color = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset color
        if (speedometerFill != null)
            speedometerFill.color = Color.white;
    }

    private void HandleRaceStateChanged(GameManager.RaceState state)
    {
        switch (state)
        {
            case GameManager.RaceState.Countdown:
                if (countdownPanel != null)
                    countdownPanel.SetActive(true);
                break;

            case GameManager.RaceState.Racing:
                StartCoroutine(HideCountdownAfterDelay());
                break;

            case GameManager.RaceState.Finished:
                // Finish screen is shown via OnRaceFinished event
                break;
        }
    }

    private IEnumerator HideCountdownAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    private void UpdateCountdown(int count)
    {
        if (countdownText == null) return;

        if (count > 0)
        {
            countdownText.text = count.ToString();
            countdownText.fontSize = 150;
        }
        else
        {
            countdownText.text = "GO!";
            countdownText.fontSize = 120;
        }

        // Animate scale
        StartCoroutine(CountdownPopAnimation());
    }

    private IEnumerator CountdownPopAnimation()
    {
        if (countdownText == null) yield break;

        RectTransform rect = countdownText.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 originalScale = Vector3.one;
        rect.localScale = Vector3.one * 1.5f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rect.localScale = Vector3.Lerp(Vector3.one * 1.5f, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = originalScale;
    }

    private void UpdateLapDisplay(int currentLap, int totalLaps)
    {
        if (lapText != null)
            lapText.text = $"Lap {currentLap}/{totalLaps}";

        // Show lap message
        if (currentLap > 1)
        {
            ShowMessage($"Lap {currentLap - 1} Complete!", 2f);
        }
    }

    private void UpdateCurrentTime(float time)
    {
        if (currentTimeText != null)
            currentTimeText.text = GameManager.FormatTime(time);
    }

    private void UpdateBestLap(float time)
    {
        if (bestLapText != null)
            bestLapText.text = GameManager.FormatTime(time);

        ShowMessage("Best Lap!", 1.5f);
    }

    private void UpdateTotalTime(float time)
    {
        if (totalTimeText != null)
            totalTimeText.text = GameManager.FormatTime(time);
    }

    private void ShowFinishScreen(float totalTime)
    {
        if (finishPanel != null)
            finishPanel.SetActive(true);

        if (finishTimeText != null)
            finishTimeText.text = $"Total Time: {GameManager.FormatTime(totalTime)}";

        if (finishBestLapText != null && gameManager != null)
            finishBestLapText.text = $"Best Lap: {GameManager.FormatTime(gameManager.BestLapTime)}";
    }

    private void ShowMessage(string message, float duration)
    {
        if (messageText == null) return;

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        messageText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (kart != null)
        {
            kart.OnDriftChargeChanged -= UpdateDriftCharge;
            kart.OnBoosterCountChanged -= UpdateBoosterSlots;
            kart.OnBoostStateChanged -= UpdateBoostState;
            kart.OnSpeedChanged -= UpdateSpeed;
        }

        if (gameManager != null)
        {
            gameManager.OnRaceStateChanged -= HandleRaceStateChanged;
            gameManager.OnCountdownTick -= UpdateCountdown;
            gameManager.OnLapCompleted -= UpdateLapDisplay;
            gameManager.OnLapTimeUpdated -= UpdateCurrentTime;
            gameManager.OnBestLapTimeUpdated -= UpdateBestLap;
            gameManager.OnTotalTimeUpdated -= UpdateTotalTime;
            gameManager.OnRaceFinished -= ShowFinishScreen;
        }
    }
}
