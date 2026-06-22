using System;
using TMPro;
using UnityEngine;

public sealed class SatelliteStateController : MonoBehaviour
{
    [SerializeField] private SatelliteState state = new();
    [SerializeField] private SatelliteStateView stateView;
    [SerializeField] private TMP_Text messageText;

    public SatelliteState State => state;
    public event Action<SatelliteState, string> StateChanged;

    public void Configure(SatelliteStateView view, TMP_Text messageLabel)
    {
        stateView = view;
        messageText = messageLabel;
    }

    private void Awake()
    {
        state.Reset();
        RefreshView("Program link ready.");
    }

    public void ResetState()
    {
        state.Reset();
        RefreshView("Satellite state reset.");
    }

    public void RefreshView(string message)
    {
        string displayMessage = string.IsNullOrWhiteSpace(message) ? state.lastCommandMessage : message;

        if (stateView != null)
        {
            stateView.Render(state, displayMessage);
        }

        if (messageText != null && !string.IsNullOrWhiteSpace(displayMessage))
        {
            messageText.text = displayMessage;
        }

        StateChanged?.Invoke(state, displayMessage);
    }
}
