using UnityEditor;
using UnityEngine;

public static class CosmaProgrammingSmokeCheck
{
    [MenuItem("COSMA/Run Programming Smoke Check")]
    public static void RunProgrammingSmokeCheck()
    {
        GameObject root = new("CosmaProgrammingSmokeCheck");

        try
        {
            ProgramModel model = root.AddComponent<ProgramModel>();
            SatelliteStateController stateController = root.AddComponent<SatelliteStateController>();
            ProgramExecutor executor = root.AddComponent<ProgramExecutor>();

            model.EnsureLineCount(5);
            model.SetCommand(0, ProgramCommand.FromDefinition(CreateCommand(CommandType.PowerToggle, "Power")));
            model.SetCommand(1, ProgramCommand.FromDefinition(CreateCommand(CommandType.ReadMagnetometer, "Read Magnetometer")));
            model.SetCommand(2, ProgramCommand.FromDefinition(CreateCommand(CommandType.RotateToEarth, "Rotate To Earth")));
            model.SetCommand(3, ProgramCommand.FromDefinition(CreateCommand(CommandType.TakeEarthPhoto, "Photo")));

            executor.Configure(model, null, stateController, null, null, null, null, null);
            executor.StepProgram();
            executor.StepProgram();
            executor.StepProgram();
            executor.StepProgram();

            SatelliteState state = stateController.State;
            Require(state.powerOn, "Smoke check failed: power should be ON after step 1.");
            Require(state.hasEarthData, "Smoke check failed: magnetometer should populate Earth data.");
            Require(state.FacingEarth, "Smoke check failed: satellite should face Earth after rotation.");
            Require(state.photoTaken, "Smoke check failed: photo should be taken after the photo command.");

            model.ClearAll();
            model.SetCommand(0, ProgramCommand.FromDefinition(CreateCommand(CommandType.PowerToggle, "Power")));
            model.SetCommand(1, ProgramCommand.FromDefinition(CreateCommand(CommandType.ReadMagnetometer, "Read Magnetometer")));
            ProgramCommand jump = ProgramCommand.FromDefinition(CreateCommand(CommandType.JumpTo, "Jump"));
            jump.TargetLineNumber = 5;
            model.SetCommand(2, jump);
            model.SetCommand(3, ProgramCommand.FromDefinition(CreateCommand(CommandType.RotateToSun, "Rotate To Sun")));
            model.SetCommand(4, ProgramCommand.FromDefinition(CreateCommand(CommandType.RotateToEarth, "Rotate To Earth")));

            executor.ResetExecution();
            executor.StepProgram();
            executor.StepProgram();
            executor.StepProgram();
            executor.StepProgram();

            Require(state.FacingEarth, "Smoke check failed: JumpTo should land on line 5 and execute RotateToEarth.");
            Debug.Log("COSMA programming smoke check passed.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static CommandDefinition CreateCommand(CommandType type, string displayName)
    {
        CommandDefinition definition = ScriptableObject.CreateInstance<CommandDefinition>();
        definition.Type = type;
        definition.DisplayName = displayName;
        definition.Description = displayName;
        definition.DefaultTargetLine = 1;
        return definition;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
