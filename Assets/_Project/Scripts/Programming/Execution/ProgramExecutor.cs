using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ProgramExecutor : MonoBehaviour
{
    public static event System.Action<ProgramExecutor> ExecutionStopped;

    private const float DefaultEmptyLineSkipDelay = 0.1f;
    private const float FullBatteryCharge = 100f;
    private const float SensorBatteryCost = 2f;
    private const float RotationBatteryCost = 8f;
    private const float StabilizeBatteryCost = 5f;
    private const float PhotoBatteryCost = 12f;
    private const float MessageBatteryCost = 10f;
    private const float UtilityBatteryCost = 3f;
    private const float DefaultAttitudeCommandDurationSeconds = 15f;
    private const float DefaultAttitudeCommandAdvanceDelaySeconds = 1f;

    [SerializeField] private ProgramModel programModel;
    [SerializeField] private ProgramPanelController programPanelView;
    [SerializeField] private SatelliteStateController satelliteStateController;
    [SerializeField] private MissionSystem missionSystem;
    [FormerlySerializedAs("satellite")]
    [SerializeField] private SatelliteController satelliteController;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button runButton;
    [SerializeField] private Button stepButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button restartButton;
    [SerializeField, Min(0.05f)] private float stepDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float emptyLineSkipDelay = DefaultEmptyLineSkipDelay;
    [SerializeField, Min(1f)] private float rotationSpeedDegreesPerSecond = 30f;
    [SerializeField, Min(0.1f)] private float attitudeCommandDurationSeconds = DefaultAttitudeCommandDurationSeconds;
    [SerializeField, Min(0.1f)] private float attitudeCommandAdvanceDelaySeconds = DefaultAttitudeCommandAdvanceDelaySeconds;
    [SerializeField, Min(0.1f)] private float earthPhotoPreviewDurationSeconds = 5f;
    [SerializeField, Min(0.1f)] private float messageTransmissionDurationSeconds = 1f;
    [SerializeField] private bool limitExecutionSteps = false;
    [SerializeField, Min(1)] private int maxExecutionSteps = 128;
    [SerializeField, Min(0)] private int currentLineIndex;
    [SerializeField] private bool pauseUnityEditorOnPauseInEditor = false;

    private Coroutine runRoutine;
    private Coroutine batteryChargeRoutine;
    private CommandExecutionResult lastCommandResult;
    private TMP_Text pauseButtonLabel;
    private int executedSteps;
    private bool hasExecutionStarted;
    private bool continueRunning;
    private bool commandInProgress;
    private bool lastStepWasEmptyLine;
    private bool skipDelayAfterStep;
    private bool runRuntimeBound;
    private bool stepRuntimeBound;
    private bool pauseRuntimeBound;
    private bool resetRuntimeBound;
    private bool restartRuntimeBound;
    private bool waitingForUnityEditorResume;

    public int CurrentLineIndex => currentLineIndex;
    public bool IsPaused => ExecutionPauseController.IsPaused;
    public bool HasExecutionStarted => hasExecutionStarted;
    public bool HasRemainingProgramLines => programModel != null &&
                                            currentLineIndex >= 0 &&
                                            currentLineIndex < programModel.LineCount;
    public bool IsProgramComplete => programModel != null &&
                                     programModel.LineCount > 0 &&
                                     currentLineIndex >= programModel.LineCount;

    public void Configure(
        ProgramModel model,
        ProgramPanelController panelView,
        SatelliteStateController stateController,
        TMP_Text statusLabel,
        Button run,
        Button step,
        Button pause,
        Button reset,
        MissionSystem activeMissionSystem = null)
    {
        programModel = model;
        programPanelView = panelView;
        satelliteStateController = stateController;
        missionSystem = activeMissionSystem;
        statusText = statusLabel;
        runButton = run;
        stepButton = step;
        pauseButton = pause;
        resetButton = reset;
    }

    public void ConfigureSceneBindings(SatelliteController controller, float rotationSpeed)
    {
        satelliteController = controller;
        rotationSpeedDegreesPerSecond = Mathf.Max(1f, rotationSpeed);
    }

    private void OnEnable()
    {
        EnsureReferences();
        ExecutionPauseController.PauseStateChanged += HandlePauseStateChanged;
#if UNITY_EDITOR
        EditorApplication.pauseStateChanged += HandleUnityEditorPauseStateChanged;
#endif
        EnsureButtonBinding(runButton, RunProgram, nameof(RunProgram), ref runRuntimeBound);
        EnsureButtonBinding(stepButton, StepProgram, nameof(StepProgram), ref stepRuntimeBound);
        EnsureButtonBinding(pauseButton, TogglePause, nameof(TogglePause), ref pauseRuntimeBound);
        EnsureButtonBinding(resetButton, ResetExecution, nameof(ResetExecution), ref resetRuntimeBound);
        EnsureButtonBinding(restartButton, RestartProgramFromBeginning, nameof(RestartProgramFromBeginning), ref restartRuntimeBound);
        UpdatePauseButtonVisual();
        WriteStatus("Программа готова.");
    }

    private void OnDisable()
    {
        CancelBatteryCharge();
        ExecutionPauseController.PauseStateChanged -= HandlePauseStateChanged;
#if UNITY_EDITOR
        EditorApplication.pauseStateChanged -= HandleUnityEditorPauseStateChanged;
#endif
        RemoveRuntimeBinding(runButton, RunProgram, ref runRuntimeBound);
        RemoveRuntimeBinding(stepButton, StepProgram, ref stepRuntimeBound);
        RemoveRuntimeBinding(pauseButton, TogglePause, ref pauseRuntimeBound);
        RemoveRuntimeBinding(resetButton, ResetExecution, ref resetRuntimeBound);
        RemoveRuntimeBinding(restartButton, RestartProgramFromBeginning, ref restartRuntimeBound);
        waitingForUnityEditorResume = false;
    }

    public void BindRestartButton(Button button)
    {
        if (restartButton != null && restartButton != button)
        {
            RemoveRuntimeBinding(restartButton, RestartProgramFromBeginning, ref restartRuntimeBound);
        }

        restartButton = button;
        EnsureButtonBinding(restartButton, RestartProgramFromBeginning, nameof(RestartProgramFromBeginning), ref restartRuntimeBound);
    }

    public void RunProgram()
    {
        if (!ValidateExecutionState())
        {
            return;
        }

        if (!hasExecutionStarted || currentLineIndex < 0 || currentLineIndex >= programModel.LineCount)
        {
            BeginFromStart();
        }

        continueRunning = true;
        if (ExecutionPauseController.IsPaused)
        {
            ResumeExecution();
            return;
        }

        if (commandInProgress)
        {
            WriteStatus("Текущая команда еще выполняется.");
            return;
        }

        if (runRoutine == null)
        {
            runRoutine = StartCoroutine(RunRoutine());
        }
    }

    public void StepProgram()
    {
        if (!ValidateExecutionState())
        {
            return;
        }

        if (!hasExecutionStarted || currentLineIndex < 0 || currentLineIndex >= programModel.LineCount)
        {
            BeginFromStart();
        }

        continueRunning = false;
        if (ExecutionPauseController.IsPaused)
        {
            ResumeExecution();
            return;
        }

        if (commandInProgress)
        {
            WriteStatus("Текущая команда еще выполняется.");
            return;
        }

        if (runRoutine == null)
        {
            runRoutine = StartCoroutine(RunRoutine());
        }
    }

    public void TogglePause()
    {
        if (ExecutionPauseController.IsPaused)
        {
            ResumeExecution();
            return;
        }

        PauseExecution();
    }

    public void PauseProgram()
    {
        TogglePause();
    }

    public void PauseExecution()
    {
        bool programWasRunning = CanPauseExecution();

        ExecutionPauseController.PauseExecution();
        string pauseMessage = programWasRunning ? "Программа поставлена на паузу." : "Симуляция поставлена на паузу.";
        WriteStatus(pauseMessage);
        satelliteStateController?.RefreshView(pauseMessage);
        PauseUnityEditorIfConfigured();
    }

    public void ResumeExecution()
    {
        bool canResumeProgram = CanResumeExecution();
        if (!ExecutionPauseController.IsPaused && !canResumeProgram)
        {
            WriteStatus("Симуляция не находится на паузе.");
            return;
        }

        ExecutionPauseController.ResumeExecution();
        bool shouldRestartProgramCoroutine = canResumeProgram && runRoutine == null && (continueRunning || commandInProgress);
        if (shouldRestartProgramCoroutine)
        {
            runRoutine = StartCoroutine(RunRoutine());
        }

        string resumeMessage = canResumeProgram ? "Выполнение программы продолжено." : "Симуляция продолжена.";
        WriteStatus(resumeMessage);
        satelliteStateController?.RefreshView(resumeMessage);
    }

    public void ResetExecution()
    {
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        CancelBatteryCharge();
        ExecutionPauseController.ResetExecutionPauseState();
        continueRunning = false;
        commandInProgress = false;
        hasExecutionStarted = false;
        currentLineIndex = 0;
        executedSteps = 0;
        waitingForUnityEditorResume = false;
        lastCommandResult = default;
        lastStepWasEmptyLine = false;
        skipDelayAfterStep = false;
        programPanelView?.SetActiveLine(-1);
        satelliteController?.CancelCommandRotation();
        satelliteController?.ResetSatellitePose();
        satelliteController?.ClearEarthPhotoPreview();
        satelliteStateController?.ResetState();
        missionSystem?.ResetProgress();

        string resetMessage = "Выполнение сброшено. Программа сохранена.";
        WriteStatus(resetMessage);
        satelliteStateController?.RefreshView(resetMessage);
        UpdatePauseButtonVisual();
    }

    public void RestartProgramFromBeginning()
    {
        if (!ValidateExecutionState())
        {
            return;
        }

        ResetExecution();
        WriteStatus("Запуск программы с первой строки.");
        RunProgram();
    }

    private IEnumerator RunRoutine()
    {
        while (programModel != null && programModel.LineCount > 0)
        {
            if (!HasValidExecutionLine(currentLineIndex))
            {
                break;
            }

            yield return WaitWhilePaused();
            yield return ExecuteOneStep();

            if (limitExecutionSteps && executedSteps >= maxExecutionSteps)
            {
                WriteStatus("Выполнение остановлено: достигнут лимит шагов. Проверь команду перехода.");
                satelliteStateController?.RefreshView("Выполнение остановлено: достигнут лимит шагов. Проверь команду перехода.");
                break;
            }

            if (!HasValidExecutionLine(currentLineIndex))
            {
                break;
            }

            if (!continueRunning)
            {
                break;
            }

            float delayAfterStep = lastStepWasEmptyLine
                ? (emptyLineSkipDelay > 0f ? emptyLineSkipDelay : DefaultEmptyLineSkipDelay)
                : stepDelay;
            if (skipDelayAfterStep)
            {
                delayAfterStep = 0f;
            }

            if (delayAfterStep > 0f)
            {
                yield return WaitForSecondsWithPause(delayAfterStep);
            }
        }

        if (programModel != null && currentLineIndex >= programModel.LineCount)
        {
            programPanelView?.SetActiveLine(-1);
            WriteStatus("Программа завершена.");
            satelliteStateController?.RefreshView("Программа завершена.");
        }

        runRoutine = null;
        commandInProgress = false;
        continueRunning = false;
        UpdatePauseButtonVisual();
        ExecutionStopped?.Invoke(this);
    }

    private IEnumerator ExecuteOneStep()
    {
        if (programModel == null || programModel.LineCount == 0)
        {
            WriteStatus("Модель программы пуста.");
            yield break;
        }

        if (!HasValidExecutionLine(currentLineIndex))
        {
            programPanelView?.SetActiveLine(-1);
            WriteStatus("Программа завершена.");
            yield break;
        }

        ProgramLineData line = programModel.GetLine(currentLineIndex);
        if (line == null)
        {
            lastStepWasEmptyLine = true;
            currentLineIndex = ResolveNextLineIndex(currentLineIndex, default);
            lastCommandResult = new CommandExecutionResult(true, "Пустая строка пропущена.");
            yield break;
        }

        lastStepWasEmptyLine = line.Command == null;
        skipDelayAfterStep = false;
        programPanelView?.SetActiveLine(currentLineIndex);
        int executingLineNumber = line.LineNumber;
        commandInProgress = true;

        yield return ExecuteCommand(line.Command, executingLineNumber);
        ApplyLastCommandResultToState();
        CheckMissionObjectives();

        commandInProgress = false;
        executedSteps++;

        if (!lastCommandResult.Success)
        {
            WriteStatus($"Строка {executingLineNumber:00}: ошибка. {lastCommandResult.Message}");
            satelliteStateController?.RefreshView(lastCommandResult.Message);
        }

        currentLineIndex = ResolveNextLineIndex(currentLineIndex, lastCommandResult);

        string statusPrefix = lastCommandResult.Success ? "Шаг" : "Ошибка";
        WriteStatus($"{statusPrefix} {executedSteps}, строка {executingLineNumber:00}: {lastCommandResult.Message}");
        satelliteStateController?.RefreshView(lastCommandResult.Message);
    }

    private bool HasValidExecutionLine(int lineIndex)
    {
        return programModel != null &&
               lineIndex >= 0 &&
               lineIndex < programModel.LineCount;
    }

    private int ResolveNextLineIndex(int executedLineIndex, CommandExecutionResult result)
    {
        if (programModel == null || programModel.LineCount <= 0)
        {
            return 0;
        }

        if (result.JumpTargetLineIndex.HasValue)
        {
            return Mathf.Clamp(result.JumpTargetLineIndex.Value, 0, programModel.LineCount - 1);
        }

        return executedLineIndex + 1;
    }

    private IEnumerator ExecuteCommand(ProgramCommand command, int executingLineNumber)
    {
        lastCommandResult = new CommandExecutionResult(false, "Выполнение команды не завершилось.");

        if (command == null)
        {
            lastCommandResult = new CommandExecutionResult(true, "Пустая строка пропущена.");
            yield break;
        }

        SatelliteState state = satelliteStateController != null ? satelliteStateController.State : null;
        if (state == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Состояние спутника не найдено.");
            yield break;
        }

        switch (command.CommandType)
        {
            case CommandType.PowerToggle:
                lastCommandResult = ExecutePowerToggle(state);
                yield break;

            case CommandType.ReadSunSensors:
                lastCommandResult = ExecuteReadSunSensors(state);
                yield break;

            case CommandType.ReadMagnetometer:
                lastCommandResult = ExecuteReadMagnetometer(state);
                yield break;

            case CommandType.RotateToEarth:
                yield return ExecuteRotateToEarth(state, command, executingLineNumber);
                yield break;

            case CommandType.RotateToSun:
                yield return ExecuteRotateToSun(state, executingLineNumber);
                yield break;

            case CommandType.JumpTo:
                int targetLine = Mathf.Clamp(command.TargetLineNumber, 1, programModel.LineCount);
                lastCommandResult = new CommandExecutionResult(true, $"Переход к строке {targetLine:00}.", targetLine - 1);
                yield break;

            case CommandType.ConditionalJump:
                lastCommandResult = ExecuteConditionalJump(state, command);
                yield break;

            case CommandType.CalibrateGyroscopes:
                lastCommandResult = ExecuteCalibrateGyroscopes(state);
                yield break;

            case CommandType.ChargeBattery:
                yield return ExecuteChargeBattery(state, executingLineNumber);
                yield break;

            case CommandType.OpenCameraCover:
                yield return ExecuteOpenCameraCover(state);
                yield break;

            case CommandType.CloseCameraCover:
                yield return ExecuteCloseCameraCover(state);
                yield break;

            case CommandType.CompressPhoto:
                lastCommandResult = ExecuteCompressPhoto(state);
                yield break;

            case CommandType.CheckCommunicationLink:
                lastCommandResult = ExecuteCheckCommunicationLink(state);
                yield break;

            case CommandType.TakeEarthPhoto:
                yield return ExecuteTakeEarthPhoto(state, executingLineNumber);
                yield break;

            case CommandType.StabilizeSatellite:
                yield return ExecuteStabilizeSatellite(state, executingLineNumber);
                yield break;

            case CommandType.CheckEarthInFrame:
                lastCommandResult = ExecuteCheckEarthInFrame(state);
                yield break;

            case CommandType.Wait:
                yield return ExecuteWait(command, executingLineNumber);
                yield break;

            case CommandType.RotateAntennaToEarth:
                yield return ExecuteRotateAntennaToEarth(state, executingLineNumber);
                yield break;

            case CommandType.SendMessageToEarth:
                yield return ExecuteSendMessage(state, executingLineNumber);
                yield break;

            case CommandType.DestroyPlanet:
                yield return ExecuteDestroyPlanet(state, executingLineNumber);
                yield break;

            default:
                lastCommandResult = new CommandExecutionResult(false, $"Неподдерживаемая команда: {command.CommandType}.");
                yield break;
        }
    }

    private CommandExecutionResult ExecutePowerToggle(SatelliteState state)
    {
        if (!state.powerOn && state.batteryCharge <= 0f)
        {
            return new CommandExecutionResult(false, "Нельзя включить питание: батарея разряжена.");
        }

        state.powerOn = !state.powerOn;
        if (!state.powerOn)
        {
            state.communicationLinkAvailable = false;
            state.earthInFrame = false;
        }

        return new CommandExecutionResult(true, state.powerOn ? "Питание включено." : "Питание выключено.");
    }

    private CommandExecutionResult ExecuteReadSunSensors(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя считать солнечные датчики при выключенном питании.");
        }

        if (!TrySpendBattery(state, SensorBatteryCost, "считывания солнечных датчиков", out CommandExecutionResult batteryFailure))
        {
            return batteryFailure;
        }

        if (satelliteController == null)
        {
            return new CommandExecutionResult(false, "Контроллер спутника не найден.");
        }

        if (!satelliteController.TryReadSunSensors(out bool sunDetected))
        {
            return new CommandExecutionResult(false, "Данные солнечных датчиков недоступны.");
        }

        state.hasSunData = true;
        state.sunDetected = sunDetected;
        return new CommandExecutionResult(
            true,
            sunDetected ? "Данные солнечных датчиков получены. Солнце обнаружено." : "Данные солнечных датчиков получены, но Солнце не обнаружено.");
    }

    private CommandExecutionResult ExecuteReadMagnetometer(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя считать магнитометр при выключенном питании.");
        }

        if (!TrySpendBattery(state, SensorBatteryCost, "считывания магнитометра", out CommandExecutionResult batteryFailure))
        {
            return batteryFailure;
        }

        if (satelliteController == null)
        {
            return new CommandExecutionResult(false, "Контроллер спутника не найден.");
        }

        if (!satelliteController.TryReadMagnetometer(out bool earthDetected))
        {
            return new CommandExecutionResult(false, "Данные магнитометра недоступны.");
        }

        state.hasEarthData = true;
        state.earthDetected = earthDetected;
        return new CommandExecutionResult(
            true,
            earthDetected ? "Данные магнитометра получены. Земля обнаружена." : "Данные магнитометра получены, но Земля не обнаружена.");
    }

    private IEnumerator ExecuteStabilizeSatellite(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя стабилизировать спутник при выключенном питании.");
            yield break;
        }

        if (!state.gyrosCalibrated)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для стабилизации нужно сначала откалибровать гироскопы.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanRotateToEarth())
        {
            lastCommandResult = new CommandExecutionResult(false, "Цель Земли недоступна для стабилизации.");
            yield break;
        }

        if (!TrySpendBattery(state, StabilizeBatteryCost, "стабилизации спутника", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        float durationSeconds = ResolveAttitudeCommandDuration();
        WriteStatus($"Строка {executingLineNumber:00}: удержание текущей ориентации относительно Земли {FormatSeconds(durationSeconds)}...");
        satelliteStateController?.RefreshView($"Удержание текущей ориентации относительно Земли {FormatSeconds(durationSeconds)}...");

        satelliteController.BeginStabilizeHold(durationSeconds, rotationSpeedDegreesPerSecond);
        yield return WaitForSecondsWithPause(ResolveAttitudeCommandAdvanceDelay());

        state.isStabilized = true;
        skipDelayAfterStep = true;
        lastCommandResult = new CommandExecutionResult(true, $"Спутник начал удерживать текущую ориентацию относительно Земли на {FormatSeconds(durationSeconds)}.");
    }

    private CommandExecutionResult ExecuteCheckEarthInFrame(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя проверить кадр при выключенном питании.");
        }

        if (satelliteController == null)
        {
            return new CommandExecutionResult(false, "Контроллер спутника не найден.");
        }

        if (!state.cameraCoverOpen)
        {
            state.earthInFrame = false;
            return new CommandExecutionResult(true, "Крышка камеры закрыта: кадр будет черным.");
        }

        bool earthInFrame = satelliteController.IsEarthInPhotoFrame();
        state.earthInFrame = earthInFrame;
        return new CommandExecutionResult(
            true,
            earthInFrame ? "Земля находится в кадре камеры." : "Земля не попадает в кадр камеры.");
    }

    private CommandExecutionResult ExecuteCalibrateGyroscopes(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя калибровать гироскопы при выключенном питании.");
        }

        if (!TrySpendBattery(state, UtilityBatteryCost, "калибровки гироскопов", out CommandExecutionResult batteryFailure))
        {
            return batteryFailure;
        }

        state.gyrosCalibrated = true;
        return new CommandExecutionResult(true, "Гироскопы откалиброваны. Точные маневры разрешены.");
    }

    private IEnumerator ExecuteChargeBattery(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя управлять зарядкой при выключенном питании.");
            yield break;
        }

        if (!state.FacingSun)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для зарядки нужно повернуть солнечные панели к Солнцу.");
            yield break;
        }

        float durationSeconds = ResolveAttitudeCommandDuration();
        WriteStatus($"Строка {executingLineNumber:00}: плавная зарядка батареи {FormatSeconds(durationSeconds)}...");
        satelliteStateController?.RefreshView($"Плавная зарядка батареи {FormatSeconds(durationSeconds)}...");
        StartBatteryCharge(state, durationSeconds);
        yield return WaitForSecondsWithPause(ResolveAttitudeCommandAdvanceDelay());

        skipDelayAfterStep = true;
        lastCommandResult = new CommandExecutionResult(true, $"Батарея начала плавную зарядку до 100% за {FormatSeconds(durationSeconds)}.");
    }

    private IEnumerator ExecuteOpenCameraCover(SatelliteState state)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя открыть крышку камеры при выключенном питании.");
            yield break;
        }

        if (state.cameraCoverOpen)
        {
            lastCommandResult = new CommandExecutionResult(true, "Крышка камеры уже открыта.");
            yield break;
        }

        if (!TrySpendBattery(state, UtilityBatteryCost, "открытия крышки камеры", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        state.cameraCoverOpen = true;
        if (satelliteController != null)
        {
            yield return satelliteController.OpenCameraCoverVisualRoutine();
        }

        lastCommandResult = new CommandExecutionResult(true, "Крышка камеры открыта.");
    }

    private IEnumerator ExecuteCloseCameraCover(SatelliteState state)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя закрыть крышку камеры при выключенном питании.");
            yield break;
        }

        if (!state.cameraCoverOpen)
        {
            lastCommandResult = new CommandExecutionResult(true, "Крышка камеры уже закрыта.");
            yield break;
        }

        state.cameraCoverOpen = false;
        state.earthInFrame = false;
        if (satelliteController != null)
        {
            yield return satelliteController.CloseCameraCoverVisualRoutine();
        }

        lastCommandResult = new CommandExecutionResult(true, "Крышка камеры закрыта.");
    }

    private void StartBatteryCharge(SatelliteState state, float durationSeconds)
    {
        CancelBatteryCharge();
        batteryChargeRoutine = StartCoroutine(ChargeBatteryOverTime(state, durationSeconds));
    }

    private void CancelBatteryCharge()
    {
        if (batteryChargeRoutine == null)
        {
            return;
        }

        StopCoroutine(batteryChargeRoutine);
        batteryChargeRoutine = null;
    }

    private IEnumerator ChargeBatteryOverTime(SatelliteState state, float durationSeconds)
    {
        if (state == null)
        {
            batteryChargeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        float startCharge = Mathf.Clamp(state.batteryCharge, 0f, FullBatteryCharge);
        float resolvedDuration = Mathf.Max(0.1f, durationSeconds);
        float refreshTimer = 0f;

        while (elapsed < resolvedDuration)
        {
            yield return WaitWhilePaused();

            float t = Mathf.Clamp01(elapsed / resolvedDuration);
            float scheduledCharge = Mathf.Lerp(startCharge, FullBatteryCharge, t);
            state.batteryCharge = Mathf.Clamp(Mathf.Max(state.batteryCharge, scheduledCharge), 0f, FullBatteryCharge);

            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer >= 0.25f)
            {
                satelliteStateController?.RefreshView($"Зарядка батареи: {state.batteryCharge:0}%");
                refreshTimer = 0f;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        state.batteryCharge = FullBatteryCharge;
        satelliteStateController?.RefreshView("Батарея заряжена до 100%.");
        batteryChargeRoutine = null;
    }

    private CommandExecutionResult ExecuteCompressPhoto(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя сжать снимок при выключенном питании.");
        }

        if (!state.photoTaken)
        {
            return new CommandExecutionResult(false, "Нет снимка для сжатия.");
        }

        if (state.dataCompressed)
        {
            return new CommandExecutionResult(true, "Снимок уже сжат и готов к передаче.");
        }

        if (!TrySpendBattery(state, UtilityBatteryCost, "сжатия снимка", out CommandExecutionResult batteryFailure))
        {
            return batteryFailure;
        }

        state.dataCompressed = true;
        return new CommandExecutionResult(true, "Снимок сжат и готов к передаче.");
    }

    private CommandExecutionResult ExecuteCheckCommunicationLink(SatelliteState state)
    {
        if (!state.powerOn)
        {
            return new CommandExecutionResult(false, "Нельзя проверить канал связи при выключенном питании.");
        }

        if (!TrySpendBattery(state, SensorBatteryCost, "проверки канала связи", out CommandExecutionResult batteryFailure))
        {
            return batteryFailure;
        }

        bool linkAvailable = state.antennaFacingEarth && (satelliteController == null || satelliteController.CanSendMessage());
        state.communicationLinkAvailable = linkAvailable;
        return new CommandExecutionResult(
            true,
            linkAvailable ? "Канал связи с Землей доступен." : "Канал связи не найден. Проверь наведение антенны.");
    }

    private CommandExecutionResult ExecuteConditionalJump(SatelliteState state, ProgramCommand command)
    {
        bool conditionPassed = EvaluateCondition(command.Condition, state);
        if (!conditionPassed)
        {
            return new CommandExecutionResult(true, $"IF {command.Condition}: false. Переход не выполнен.");
        }

        int targetLine = Mathf.Clamp(command.TargetLineNumber, 1, programModel.LineCount);
        return new CommandExecutionResult(true, $"IF {command.Condition}: true. Переход к строке {targetLine:00}.", targetLine - 1);
    }

    private IEnumerator ExecuteWait(ProgramCommand command, int executingLineNumber)
    {
        float seconds = Mathf.Max(0f, command != null ? command.WaitSeconds : 0f);
        WriteStatus($"Строка {executingLineNumber:00}: ожидание {seconds:0.#} сек...");
        satelliteStateController?.RefreshView($"Ожидание {seconds:0.#} сек...");
        yield return WaitForSecondsWithPause(seconds);
        lastCommandResult = new CommandExecutionResult(true, $"WAIT завершен: {seconds:0.#} сек.");
    }

    private IEnumerator ExecuteTakeEarthPhoto(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя сделать снимок при выключенном питании.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!TrySpendBattery(state, PhotoBatteryCost, "съемки", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        WriteStatus($"Строка {executingLineNumber:00}: выполняется съемка...");
        satelliteStateController?.RefreshView("Выполняется съемка...");

        if (!satelliteController.CaptureEarthPhoto(
                state.cameraCoverOpen,
                out RenderTexture capturedTexture,
                out string captureMessage,
                out bool earthInFrame))
        {
            lastCommandResult = new CommandExecutionResult(false, captureMessage);
            yield break;
        }

        state.earthInFrame = earthInFrame;
        state.photoTaken = true;
        state.dataCompressed = false;
        state.dataSent = false;
        satelliteStateController?.RefreshView(captureMessage);

        yield return satelliteController.ShowEarthPhotoPreview(capturedTexture, earthPhotoPreviewDurationSeconds);

        lastCommandResult = new CommandExecutionResult(true, captureMessage);
    }

    private IEnumerator ExecuteRotateToEarth(SatelliteState state, ProgramCommand command, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя повернуться к Земле при выключенном питании.");
            yield break;
        }

        if (!state.hasEarthData)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для поворота к Земле нужны актуальные данные магнитометра.");
            yield break;
        }

        if (!state.earthDetected)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для поворота к Земле нужно сначала обнаружить Землю.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanRotateToEarth())
        {
            lastCommandResult = new CommandExecutionResult(false, "Цель Земли недоступна для поворота.");
            yield break;
        }

        if (!TrySpendBattery(state, RotationBatteryCost, "поворота к Земле", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        EarthFacingSide facingSide = command != null ? command.EarthFacingSide : EarthFacingSide.Camera;
        string sideLabel = facingSide == EarthFacingSide.Antenna ? "антенной" : "камерой";

        float durationSeconds = ResolveAttitudeCommandDuration();
        WriteStatus($"Строка {executingLineNumber:00}: наведение на Землю {sideLabel} {FormatSeconds(durationSeconds)}...");
        satelliteStateController?.RefreshView($"Наведение на Землю {sideLabel} {FormatSeconds(durationSeconds)}...");
        satelliteController.BeginRotateToEarthHold(facingSide, rotationSpeedDegreesPerSecond, durationSeconds);
        yield return WaitForSecondsWithPause(ResolveAttitudeCommandAdvanceDelay());

        state.SetOrientation(SatelliteOrientation.TowardEarth);
        state.SetEarthFacingSide(facingSide);
        state.earthInFrame = false;
        state.antennaFacingEarth = facingSide == EarthFacingSide.Antenna;
        state.communicationLinkAvailable = false;
        skipDelayAfterStep = true;
        lastCommandResult = new CommandExecutionResult(
            true,
            facingSide == EarthFacingSide.Antenna
                ? $"Спутник начал удерживать наведение на Землю стороной антенны на {FormatSeconds(durationSeconds)}."
                : $"Спутник начал удерживать наведение на Землю стороной камеры на {FormatSeconds(durationSeconds)}.");
    }

    private IEnumerator ExecuteRotateToSun(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя повернуться к Солнцу при выключенном питании.");
            yield break;
        }

        if (!state.hasSunData)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для поворота к Солнцу нужны актуальные данные солнечных датчиков.");
            yield break;
        }

        if (!state.sunDetected)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для поворота к Солнцу нужно сначала обнаружить Солнце.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanRotateToSun())
        {
            lastCommandResult = new CommandExecutionResult(false, "Цель Солнца недоступна для поворота.");
            yield break;
        }

        if (!TrySpendBattery(state, RotationBatteryCost, "поворота к Солнцу", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        float durationSeconds = ResolveAttitudeCommandDuration();
        WriteStatus($"Строка {executingLineNumber:00}: наведение на Солнце {FormatSeconds(durationSeconds)}...");
        satelliteStateController?.RefreshView($"Наведение на Солнце {FormatSeconds(durationSeconds)}...");
        satelliteController.BeginRotateToSunHold(rotationSpeedDegreesPerSecond, durationSeconds);
        yield return WaitForSecondsWithPause(ResolveAttitudeCommandAdvanceDelay());

        state.SetOrientation(SatelliteOrientation.TowardSun);
        state.earthInFrame = false;
        state.antennaFacingEarth = false;
        state.communicationLinkAvailable = false;
        skipDelayAfterStep = true;
        lastCommandResult = new CommandExecutionResult(true, $"Спутник начал удерживать наведение на Солнце на {FormatSeconds(durationSeconds)}.");
    }

    private IEnumerator ExecuteRotateAntennaToEarth(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя повернуть антенну при выключенном питании.");
            yield break;
        }

        if (!state.hasEarthData)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для наведения антенны нужны данные магнитометра.");
            yield break;
        }

        if (!state.earthDetected)
        {
            lastCommandResult = new CommandExecutionResult(false, "Для наведения антенны нужно сначала обнаружить Землю.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanRotateAntennaToEarth())
        {
            lastCommandResult = new CommandExecutionResult(false, "Антенна или цель Земли недоступна.");
            yield break;
        }

        if (!TrySpendBattery(state, RotationBatteryCost, "наведения антенны", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        WriteStatus($"Строка {executingLineNumber:00}: наведение антенны на Землю...");
        satelliteStateController?.RefreshView("Наведение антенны на Землю...");
        yield return satelliteController.RotateAntennaToEarth(rotationSpeedDegreesPerSecond);

        state.antennaFacingEarth = true;
        state.communicationLinkAvailable = false;
        lastCommandResult = new CommandExecutionResult(true, "Антенна наведена на Землю.");
    }

    private IEnumerator ExecuteSendMessage(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя отправить сообщение при выключенном питании.");
            yield break;
        }

        if (!state.communicationLinkAvailable)
        {
            lastCommandResult = new CommandExecutionResult(false, "Канал связи не подтвержден. Сначала проверь канал связи.");
            yield break;
        }

        if (state.photoTaken && !state.dataCompressed)
        {
            lastCommandResult = new CommandExecutionResult(false, "Снимок не сжат. Сожми снимок перед передачей.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanSendMessage())
        {
            lastCommandResult = new CommandExecutionResult(false, "Нет доступного направления для передачи сигнала.");
            yield break;
        }

        if (!TrySpendBattery(state, MessageBatteryCost, "передачи данных", out CommandExecutionResult batteryFailure))
        {
            lastCommandResult = batteryFailure;
            yield break;
        }

        WriteStatus($"Строка {executingLineNumber:00}: отправка сообщения...");
        satelliteStateController?.RefreshView("Отправка сообщения...");
        yield return satelliteController.SendMessage(messageTransmissionDurationSeconds);

        state.dataSent = true;
        lastCommandResult = new CommandExecutionResult(true, "Сообщение отправлено.");
    }

    private IEnumerator ExecuteDestroyPlanet(SatelliteState state, int executingLineNumber)
    {
        if (!state.powerOn)
        {
            lastCommandResult = new CommandExecutionResult(false, "Нельзя уничтожить планету при выключенном питании.");
            yield break;
        }

        if (state.planetDestroyed)
        {
            lastCommandResult = new CommandExecutionResult(true, "Планета уже уничтожена.");
            yield break;
        }

        if (state.batteryCharge < FullBatteryCharge)
        {
            lastCommandResult = new CommandExecutionResult(false, $"Для уничтожения планеты нужен заряд батареи 100%. Сейчас {state.batteryCharge:0}%.");
            yield break;
        }

        if (satelliteController == null)
        {
            lastCommandResult = new CommandExecutionResult(false, "Контроллер спутника не найден.");
            yield break;
        }

        if (!satelliteController.CanDestroyPlanet())
        {
            lastCommandResult = new CommandExecutionResult(false, "Цель Земли недоступна для уничтожения.");
            yield break;
        }

        state.batteryCharge = 0f;
        state.communicationLinkAvailable = false;
        WriteStatus($"Строка {executingLineNumber:00}: наведение на планету, затем 2 секунды импульса...");
        satelliteStateController?.RefreshView("Наведение на планету и зарядка импульса...");
        yield return satelliteController.DestroyPlanet();

        state.planetDestroyed = true;
        lastCommandResult = new CommandExecutionResult(true, "Планета уничтожена. Заряд батареи израсходован полностью.");
    }

    private static bool TrySpendBattery(
        SatelliteState state,
        float amount,
        string actionName,
        out CommandExecutionResult failure)
    {
        failure = default;
        if (state == null)
        {
            failure = new CommandExecutionResult(false, "Состояние спутника не найдено.");
            return false;
        }

        amount = Mathf.Max(0f, amount);
        if (amount <= 0f)
        {
            return true;
        }

        if (state.batteryCharge < amount)
        {
            failure = new CommandExecutionResult(
                false,
                $"Недостаточно заряда для {actionName}: {state.batteryCharge:0}% из {amount:0}%.");
            return false;
        }

        state.batteryCharge = Mathf.Max(0f, state.batteryCharge - amount);
        return true;
    }

    private float ResolveAttitudeCommandDuration()
    {
        return Mathf.Max(0.1f, attitudeCommandDurationSeconds);
    }

    private float ResolveAttitudeCommandAdvanceDelay()
    {
        return Mathf.Max(0.1f, attitudeCommandAdvanceDelaySeconds);
    }

    private static string FormatSeconds(float seconds)
    {
        return $"{seconds:0.#} с";
    }

    private void ApplyLastCommandResultToState()
    {
        SatelliteState state = satelliteStateController != null ? satelliteStateController.State : null;
        if (state == null)
        {
            return;
        }

        state.SetLastCommandResult(lastCommandResult.Success, lastCommandResult.Message);
    }

    private void CheckMissionObjectives()
    {
        SatelliteState state = satelliteStateController != null ? satelliteStateController.State : null;
        missionSystem?.CheckObjectives(state);
    }

    private static bool EvaluateCondition(CommandConditionType condition, SatelliteState state)
    {
        if (state == null)
        {
            return false;
        }

        return condition switch
        {
            CommandConditionType.PowerOn => state.powerOn,
            CommandConditionType.SunDataReady => state.hasSunData,
            CommandConditionType.EarthDataReady => state.hasEarthData,
            CommandConditionType.FacingEarth => state.FacingEarth,
            CommandConditionType.FacingSun => state.FacingSun,
            CommandConditionType.PhotoTaken => state.photoTaken,
            CommandConditionType.EarthInFrame => state.earthInFrame,
            CommandConditionType.DataSent => state.dataSent,
            CommandConditionType.LastCommandSuccess => state.hasLastCommandResult && state.lastCommandSuccess,
            CommandConditionType.LastCommandFailed => state.hasLastCommandResult && !state.lastCommandSuccess,
            CommandConditionType.Stabilized => state.isStabilized,
            CommandConditionType.BatteryLow => state.BatteryLow,
            CommandConditionType.GyrosCalibrated => state.gyrosCalibrated,
            CommandConditionType.CameraCoverOpen => state.cameraCoverOpen,
            CommandConditionType.DataCompressed => state.dataCompressed,
            CommandConditionType.CommunicationLinkAvailable => state.communicationLinkAvailable,
            _ => false
        };
    }

    private void BeginFromStart()
    {
        hasExecutionStarted = true;
        currentLineIndex = 0;
        executedSteps = 0;
        continueRunning = false;
        commandInProgress = false;
        waitingForUnityEditorResume = false;
        lastCommandResult = default;
        lastStepWasEmptyLine = false;
        skipDelayAfterStep = false;
        CancelBatteryCharge();
        ExecutionPauseController.ResetExecutionPauseState();
        programPanelView?.SetActiveLine(-1);
        satelliteController?.CancelCommandRotation();
        satelliteController?.ResetSatellitePose();
        satelliteController?.ClearEarthPhotoPreview();
        satelliteStateController?.ResetState();
        missionSystem?.ResetProgress();
        UpdatePauseButtonVisual();
    }

    private bool ValidateExecutionState()
    {
        EnsureReferences();
        if (programModel == null)
        {
            WriteStatus("Модель программы не настроена.");
            return false;
        }

        if (satelliteStateController == null)
        {
            WriteStatus("Состояние спутника не настроено.");
            return false;
        }

        if (!programModel.HasAnyCommand)
        {
            WriteStatus("Программа пуста. Сначала перетащи команды в строки.");
            return false;
        }

        return true;
    }

    private bool CanPauseExecution()
    {
        return hasExecutionStarted &&
               currentLineIndex >= 0 &&
               currentLineIndex < (programModel != null ? programModel.LineCount : 0) &&
               (commandInProgress || runRoutine != null);
    }

    private bool CanResumeExecution()
    {
        return hasExecutionStarted &&
               currentLineIndex >= 0 &&
               currentLineIndex < (programModel != null ? programModel.LineCount : 0);
    }

    private void EnsureReferences()
    {
        if (programPanelView == null)
        {
            programPanelView = GetComponentInParent<ProgramPanelController>();
        }

        if (programModel == null && programPanelView != null)
        {
            programModel = programPanelView.Model;
        }

        if (satelliteStateController == null)
        {
            satelliteStateController = FindFirstObjectByType<SatelliteStateController>(FindObjectsInactive.Include);
        }

        if (missionSystem == null)
        {
            missionSystem = FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
        }

        if (satelliteController == null)
        {
            satelliteController = FindPrimarySatelliteController();
        }

        if (pauseButtonLabel == null && pauseButton != null)
        {
            pauseButtonLabel = pauseButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void WriteStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        programPanelView?.SetMessage(message);
    }

    private static SatelliteController FindPrimarySatelliteController()
    {
        GameObject namedSatellite = GameObject.Find("Satellite");
        if (namedSatellite != null && namedSatellite.TryGetComponent(out SatelliteController namedController))
        {
            return namedController;
        }

        SatelliteController[] candidates = FindObjectsByType<SatelliteController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SatelliteController bestCandidate = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            SatelliteController candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            int score = 0;
            if (candidate.name == "Satellite")
            {
                score += 8;
            }

            if (candidate.GetComponent<Rigidbody>() != null)
            {
                score += 4;
            }

            if (candidate.EarthTarget != null)
            {
                score += 2;
            }

            if (candidate.SunTarget != null)
            {
                score += 2;
            }

            if (bestCandidate == null || score > bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }
        }

        return bestCandidate;
    }

    private void HandlePauseStateChanged(bool paused)
    {
        UpdatePauseButtonVisual();
    }

    private bool PauseUnityEditorIfConfigured()
    {
#if UNITY_EDITOR
        if (!pauseUnityEditorOnPauseInEditor || !Application.isPlaying || EditorApplication.isPaused)
        {
            return false;
        }

        waitingForUnityEditorResume = true;
        Debug.Break();
        return true;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private void HandleUnityEditorPauseStateChanged(PauseState pauseState)
    {
        if (pauseState != PauseState.Unpaused || !waitingForUnityEditorResume)
        {
            return;
        }

        waitingForUnityEditorResume = false;
        if (!ExecutionPauseController.IsPaused || !CanResumeExecution())
        {
            return;
        }

        ResumeExecution();
    }
#endif

    private void UpdatePauseButtonVisual()
    {
        if (pauseButtonLabel == null && pauseButton != null)
        {
            pauseButtonLabel = pauseButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (pauseButtonLabel == null)
        {
            return;
        }

        pauseButtonLabel.text = ExecutionPauseController.IsPaused ? "RESUME" : "PAUSE";
    }

    private static IEnumerator WaitWhilePaused()
    {
        yield return ExecutionPauseController.WaitWhilePaused();
    }

    private static IEnumerator WaitForSecondsWithPause(float durationSeconds)
    {
        float elapsed = 0f;
        durationSeconds = Mathf.Max(0f, durationSeconds);

        while (elapsed < durationSeconds)
        {
            yield return ExecutionPauseController.WaitWhilePaused();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void EnsureButtonBinding(Button button, UnityAction action, string methodName, ref bool runtimeBound)
    {
        if (button == null)
        {
            return;
        }

        if (HasPersistentBinding(button, methodName))
        {
            runtimeBound = false;
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        runtimeBound = true;
    }

    private bool HasPersistentBinding(Button button, string methodName)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveRuntimeBinding(Button button, UnityAction action, ref bool runtimeBound)
    {
        if (button != null && runtimeBound)
        {
            button.onClick.RemoveListener(action);
        }

        runtimeBound = false;
    }
}
