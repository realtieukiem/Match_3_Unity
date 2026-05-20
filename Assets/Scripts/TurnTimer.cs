using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TurnTimer : MonoBehaviour
{
    public float TurnDuration = 15f;
    public TMP_Text TimerText;
    public UnityEvent TimeExpired = new UnityEvent();

    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }

    private bool expired;

    private void OnValidate()
    {
        TurnDuration = Mathf.Max(0f, TurnDuration);
    }

    private void Awake()
    {
        RemainingTime = Mathf.Max(0f, TurnDuration);
        RefreshText();
    }

    private void Update()
    {
        if (!IsRunning || expired)
            return;

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        RefreshText();

        if (RemainingTime <= 0f)
        {
            expired = true;
            IsRunning = false;

            if (TimeExpired != null)
                TimeExpired.Invoke();
        }
    }

    public void Restart()
    {
        RemainingTime = Mathf.Max(0f, TurnDuration);
        expired = false;
        IsRunning = TurnDuration > 0f;
        RefreshText();
    }

    public void Pause()
    {
        IsRunning = false;
    }

    public void Resume()
    {
        if (!expired && RemainingTime > 0f)
            IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
        expired = true;
        RemainingTime = 0f;
        RefreshText();
    }

    private void RefreshText()
    {
        if (TimerText != null)
            TimerText.text = Mathf.CeilToInt(RemainingTime).ToString();
    }
}
