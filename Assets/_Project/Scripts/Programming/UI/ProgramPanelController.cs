using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ProgramPanelController : MonoBehaviour
{
    [SerializeField] private ProgramModel programModel;
    [SerializeField] private ProgramCommandView programCommandPrefab;
    [SerializeField] private UIAnimationDriver animationDriver;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private List<ProgramLineView> lines = new();

    public ProgramModel Model => programModel;
    public int LineCount => programModel != null ? programModel.LineCount : 0;
    public bool HasAnyCommand => programModel != null && programModel.HasAnyCommand;

    public void Configure(
        ProgramModel model,
        ProgramCommandView commandPrefab,
        UIAnimationDriver driver,
        TMP_Text messageLabel,
        List<ProgramLineView> programLines)
    {
        programModel = model;
        programCommandPrefab = commandPrefab;
        animationDriver = driver;
        messageText = messageLabel;
        lines = programLines ?? new List<ProgramLineView>();
        BindLines();
    }

    private void Awake()
    {
        if (programModel == null)
        {
            programModel = GetComponent<ProgramModel>();
        }

        if (lines == null || lines.Count == 0)
        {
            lines = new List<ProgramLineView>(GetComponentsInChildren<ProgramLineView>(true));
        }

        BindLines();
    }

    public void AssignCommandToLine(ProgramLineView lineView, CommandDefinition definition)
    {
        if (programModel == null || lineView == null || definition == null)
        {
            return;
        }

        int lineIndex = lines.IndexOf(lineView);
        if (lineIndex < 0)
        {
            return;
        }

        ProgramCommand command = ProgramCommand.FromDefinition(definition);
        programModel.SetCommand(lineIndex, command);
        RefreshLine(lineIndex);
        SetMessage($"Строка {lineIndex + 1:00}: команда «{command.DisplayName}» добавлена.");
    }

    public void MoveCommand(ProgramLineView sourceLineView, ProgramLineView targetLineView)
    {
        if (programModel == null || sourceLineView == null || targetLineView == null)
        {
            return;
        }

        int sourceIndex = lines.IndexOf(sourceLineView);
        int targetIndex = lines.IndexOf(targetLineView);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }

        if (sourceIndex == targetIndex)
        {
            RefreshLine(sourceIndex);
            return;
        }

        ProgramCommand sourceCommand = programModel.GetCommand(sourceIndex);
        if (sourceCommand == null)
        {
            return;
        }

        ProgramCommand targetCommand = programModel.GetCommand(targetIndex);
        programModel.SetCommand(targetIndex, sourceCommand);
        programModel.SetCommand(sourceIndex, targetCommand);

        RefreshLine(sourceIndex);
        RefreshLine(targetIndex);

        string action = targetCommand == null ? "перемещена" : "поменяна местами";
        SetMessage($"Строка {sourceIndex + 1:00} -> {targetIndex + 1:00}: команда «{sourceCommand.DisplayName}» {action}.");
    }

    public void RemoveCommandFromLine(ProgramLineView lineView)
    {
        if (programModel == null || lineView == null)
        {
            return;
        }

        int lineIndex = lines.IndexOf(lineView);
        if (lineIndex < 0)
        {
            return;
        }

        ProgramCommand command = programModel.GetCommand(lineIndex);
        if (command == null)
        {
            RefreshLine(lineIndex);
            return;
        }

        programModel.SetCommand(lineIndex, null);
        RefreshLine(lineIndex);
        SetMessage($"Строка {lineIndex + 1:00}: команда «{command.DisplayName}» удалена.");
    }

    public ProgramCommand GetCommandAtLine(int lineIndex)
    {
        return programModel != null ? programModel.GetCommand(lineIndex) : null;
    }

    public void ClearProgram()
    {
        if (programModel == null)
        {
            return;
        }

        programModel.ClearAll();
        RefreshAllLines();
        SetActiveLine(-1);
        SetMessage("Программа очищена.");
    }

    public void UndoLastCommand()
    {
        if (programModel == null || lines == null || lines.Count == 0)
        {
            return;
        }

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            ProgramLineData line = programModel.GetLine(i);
            if (line == null || !line.HasCommand)
            {
                continue;
            }

            line.Clear();
            RefreshLine(i);
            SetActiveLine(-1);
            SetMessage($"Строка {i + 1:00}: последняя команда удалена.");
            return;
        }

        SetMessage("Отменять нечего.");
    }

    public void SetActiveLine(int activeLineIndex)
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            lines[i].SetExecuting(i == activeLineIndex);
        }
    }

    public void SetMessage(string message)
    {
        if (messageText != null && !string.IsNullOrWhiteSpace(message))
        {
            messageText.text = message;
        }
    }

    private void BindLines()
    {
        if (programModel == null || lines == null)
        {
            return;
        }

        programModel.EnsureLineCount(lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            ProgramLineView lineView = lines[i];
            if (lineView == null)
            {
                continue;
            }

            lineView.SetProgramPanel(this);
            lineView.BindLine(programModel.GetLine(i));
            lineView.RefreshFromModel(programCommandPrefab, LineCount, animationDriver);
        }
    }

    private void RefreshAllLines()
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            RefreshLine(i);
        }
    }

    private void RefreshLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= lines.Count)
        {
            return;
        }

        ProgramLineView lineView = lines[lineIndex];
        if (lineView == null)
        {
            return;
        }

        lineView.BindLine(programModel.GetLine(lineIndex));
        lineView.RefreshFromModel(programCommandPrefab, LineCount, animationDriver);
    }
}
