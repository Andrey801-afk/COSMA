using UnityEngine;

public sealed class CommandPaletteRuntimeSyncRunner : MonoBehaviour
{
    private const string RunnerName = "__CommandPaletteRuntimeSync";
    private int framesRemaining;

    public static void Schedule()
    {
        GameObject runnerObject = GameObject.Find(RunnerName);
        if (runnerObject == null)
        {
            runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
        }

        CommandPaletteRuntimeSyncRunner runner = runnerObject.GetComponent<CommandPaletteRuntimeSyncRunner>();
        if (runner == null)
        {
            runner = runnerObject.AddComponent<CommandPaletteRuntimeSyncRunner>();
        }

        runner.framesRemaining = 10;
        runner.enabled = true;
    }

    private void LateUpdate()
    {
        if (framesRemaining <= 0)
        {
            enabled = false;
            return;
        }

        CommandPaletteRuntimeSync.SyncOpenPalettes();
        framesRemaining--;
    }
}
