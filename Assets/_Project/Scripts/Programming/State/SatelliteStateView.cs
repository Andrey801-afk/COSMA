using TMPro;
using UnityEngine;

public sealed class SatelliteStateView : MonoBehaviour
{
    private const float SidePadding = 18f;
    private const float TelemetryBottom = 20f;
    private const float TelemetryHeight = 94f;

    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text messageText;

    private const string Good = "#8CFF9B";
    private const string Warn = "#FFD36A";
    private const string Bad = "#FF6858";
    private const string Info = "#7FD9FF";
    private const string Muted = "#9AA6B2";
    private const string Text = "#E8EDF2";

    public void Configure(TMP_Text stateLabel, TMP_Text messageLabel)
    {
        stateText = stateLabel;
        messageText = messageLabel;
        ConfigureStateTextStyle();
    }

    public void Render(SatelliteState state, string message)
    {
        if (stateText != null)
        {
            ConfigureStateTextStyle();
            stateText.text = BuildStateSummary(state);
        }

        string displayMessage = string.IsNullOrWhiteSpace(message) ? state.lastCommandMessage : message;
        if (messageText != null && !string.IsNullOrWhiteSpace(displayMessage))
        {
            messageText.text = displayMessage;
        }
    }

    private void ConfigureStateTextStyle()
    {
        if (stateText == null)
        {
            return;
        }

        stateText.richText = true;
        stateText.fontSize = 13f;
        stateText.lineSpacing = -7f;
        stateText.characterSpacing = 0f;
        stateText.textWrappingMode = TextWrappingModes.NoWrap;
        stateText.overflowMode = TextOverflowModes.Truncate;
        stateText.alignment = TextAlignmentOptions.TopLeft;
        stateText.margin = Vector4.zero;
        PlaceTelemetryBand();
    }

    private void PlaceTelemetryBand()
    {
        if (stateText == null || stateText.rectTransform == null)
        {
            return;
        }

        RectTransform rect = stateText.rectTransform;
        RectTransform parent = rect.parent as RectTransform;
        float panelHeight = parent != null && parent.rect.height > 1f ? parent.rect.height : 360f;
        float telemetryTop = Mathf.Max(0f, panelHeight - TelemetryBottom - TelemetryHeight);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(SidePadding, TelemetryBottom);
        rect.offsetMax = new Vector2(-SidePadding, -telemetryTop);
    }

    private static string BuildStateSummary(SatelliteState state)
    {
        if (state == null)
        {
            return $"<color={Muted}>ТЕЛЕМЕТРИЯ</color>\n<color={Bad}>Нет данных спутника</color>";
        }

        string power = Tag(state.powerOn ? Good : Muted, state.powerOn ? "ВКЛ" : "ВЫКЛ");
        string battery = Tag(BatteryColor(state), $"{state.batteryCharge:0}%{(state.BatteryLow ? " LOW" : string.Empty)}");
        string sun = SensorLabel(state.hasSunData, state.sunDetected);
        string earth = SensorLabel(state.hasEarthData, state.earthDetected);
        string gyro = Tag(state.gyrosCalibrated ? Good : Muted, state.gyrosCalibrated ? "OK" : "нет");
        string stable = Tag(state.isStabilized ? Good : Muted, state.isStabilized ? "да" : "нет");
        string cover = Tag(state.cameraCoverOpen ? Good : Muted, state.cameraCoverOpen ? "откр" : "закр");
        string photo = Tag(state.photoTaken ? Good : Muted, state.photoTaken ? "есть" : "нет");
        string frame = Tag(state.earthInFrame ? Good : Muted, state.earthInFrame ? "в кадре" : "нет");
        string link = Tag(state.communicationLinkAvailable ? Good : Muted, state.communicationLinkAvailable ? "есть" : "нет");
        string data = DataLabel(state);
        string last = !state.hasLastCommandResult
            ? Tag(Muted, "ожид.")
            : Tag(state.lastCommandSucceeded ? Good : Warn, state.lastCommandSucceeded ? "OK" : "ошиб.");

        return
            $"<color={Info}><b>ТЕЛЕМЕТРИЯ</b></color>\n" +
            $"{Dot(state.powerOn)} Питание {power}   Батарея {battery}\n" +
            $"{Dot(state.hasSunData || state.hasEarthData)} Датчики: Солнце {sun} / Земля {earth}\n" +
            $"{Dot(state.FacingEarth || state.FacingSun)} Ориент: {Tag(Info, FacingLabel(state))}   Гиро {gyro} / Стаб {stable}\n" +
            $"{Dot(state.cameraCoverOpen || state.photoTaken)} Камера {cover}   Фото {photo} / Земля {frame}\n" +
            $"{Dot(state.communicationLinkAvailable || state.dataSent)} Связь {link}   Данные {data} / Посл. {last}";
    }

    private static string FacingLabel(SatelliteState state)
    {
        if (state == null)
        {
            return "НЕИЗВЕСТНО";
        }

        switch (state.currentOrientation)
        {
            case SatelliteOrientation.TowardEarth:
                return state.earthFacingSide == EarthFacingSide.Antenna
                    ? "Земля/ант."
                    : "Земля/кам.";
            case SatelliteOrientation.TowardSun:
                return "Солнце";
            default:
                return "неизв.";
        }
    }

    private static string SensorLabel(bool hasData, bool detected)
    {
        if (!hasData)
        {
            return Tag(Muted, "нет");
        }

        return Tag(detected ? Good : Warn, detected ? "найд." : "нет цели");
    }

    private static string DataLabel(SatelliteState state)
    {
        if (state.dataSent)
        {
            return Tag(Good, "отпр.");
        }

        if (state.dataCompressed)
        {
            return Tag(Info, "сжаты");
        }

        if (state.photoTaken)
        {
            return Tag(Warn, "сыр.");
        }

        return Tag(Muted, "нет");
    }

    private static string BatteryColor(SatelliteState state)
    {
        if (state.BatteryLow)
        {
            return Bad;
        }

        return state.batteryCharge <= 30f ? Warn : Good;
    }

    private static string Dot(bool active)
    {
        return Tag(active ? Good : Muted, "●");
    }

    private static string Tag(string color, string value)
    {
        return $"<color={color}>{value}</color>";
    }
}
