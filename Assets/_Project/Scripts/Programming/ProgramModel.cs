using System.Collections.Generic;
using UnityEngine;

public sealed class ProgramModel : MonoBehaviour
{
    [SerializeField] private List<ProgramLine> lines = new();

    public IReadOnlyList<ProgramLineData> Lines => lines;
    public int LineCount => lines != null ? lines.Count : 0;
    public bool HasAnyCommand
    {
        get
        {
            if (lines == null)
            {
                return false;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && lines[i].HasCommand)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void EnsureLineCount(int count)
    {
        count = Mathf.Max(0, count);
        if (lines == null)
        {
            lines = new List<ProgramLine>(count);
        }

        while (lines.Count < count)
        {
            lines.Add(new ProgramLine());
        }

        while (lines.Count > count)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] == null)
            {
                lines[i] = new ProgramLine();
            }

            lines[i].LineNumber = i + 1;
        }
    }

    public ProgramLineData GetLine(int index)
    {
        return index >= 0 && index < LineCount ? lines[index] : null;
    }

    public ProgramCommand GetCommand(int index)
    {
        ProgramLineData line = GetLine(index);
        return line != null ? line.Command : null;
    }

    public void SetCommand(int index, ProgramCommand command)
    {
        ProgramLineData line = GetLine(index);
        if (line == null)
        {
            return;
        }

        line.SetCommand(command);
    }

    public void ClearAll()
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            lines[i]?.Clear();
        }
    }
}
