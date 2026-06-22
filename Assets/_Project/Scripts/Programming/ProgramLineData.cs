using System;
using UnityEngine;

[Serializable]
public class ProgramLineData
{
    [SerializeField, Min(1)] private int lineNumber = 1;
    [SerializeField] private bool hasCommand;
    [SerializeField] private ProgramCommand command;

    public int LineNumber
    {
        get => Mathf.Max(1, lineNumber);
        set => lineNumber = Mathf.Max(1, value);
    }

    public ProgramCommand Command => HasCommand ? command : null;
    public bool HasCommand => hasCommand && command != null && command.Definition != null;

    public void SetCommand(ProgramCommand value)
    {
        hasCommand = value != null && value.Definition != null;
        command = value;
    }

    public void Clear()
    {
        hasCommand = false;
        command = null;
    }
}
