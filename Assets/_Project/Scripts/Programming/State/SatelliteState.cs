using System;

[Serializable]
public sealed class SatelliteState
{
    public const float BatteryLowThreshold = 10f;

    public bool powerOn;
    public float batteryCharge = 55f;
    public bool hasSunData;
    public bool hasEarthData;
    public bool sunDetected;
    public bool earthDetected;
    public bool gyrosCalibrated;
    public SatelliteOrientation currentOrientation = SatelliteOrientation.Unknown;
    public EarthFacingSide earthFacingSide = EarthFacingSide.Camera;
    public bool antennaFacingEarth;
    public bool facingEarth;
    public bool facingSun;
    public bool isStabilized;
    public bool cameraCoverOpen;
    public bool photoTaken;
    public bool earthInFrame;
    public bool dataCompressed;
    public bool communicationLinkAvailable;
    public bool dataSent;
    public bool planetDestroyed;
    public bool hasLastCommandResult;
    public bool lastCommandSuccess;
    public bool lastCommandSucceeded;
    public string lastCommandMessage = "Команды еще не выполнялись.";

    public bool BatteryLow => batteryCharge <= BatteryLowThreshold;
    public bool FacingEarth => facingEarth || currentOrientation == SatelliteOrientation.TowardEarth;
    public bool FacingSun => facingSun || currentOrientation == SatelliteOrientation.TowardSun;
    public bool CanRotateToEarth => powerOn && hasEarthData && earthDetected;
    public bool CanRotateToSun => powerOn && hasSunData && sunDetected;
    public bool CanTakeEarthPhoto => powerOn;
    public bool CanSendMessage => powerOn && communicationLinkAvailable && (!photoTaken || dataCompressed);
    public bool CanSendMessageToEarth => CanSendMessage;
    public bool CanDestroyPlanet => powerOn && batteryCharge >= 100f;

    public void SetOrientation(SatelliteOrientation orientation)
    {
        currentOrientation = orientation;
        facingEarth = orientation == SatelliteOrientation.TowardEarth;
        facingSun = orientation == SatelliteOrientation.TowardSun;
    }

    public void SetEarthFacingSide(EarthFacingSide side)
    {
        earthFacingSide = side;
    }

    public void SetLastCommandResult(bool succeeded, string message)
    {
        hasLastCommandResult = true;
        lastCommandSuccess = succeeded;
        lastCommandSucceeded = succeeded;
        lastCommandMessage = string.IsNullOrWhiteSpace(message) ? "Нет сообщения от команды." : message;
    }

    public void Reset()
    {
        powerOn = false;
        batteryCharge = 55f;
        hasSunData = false;
        hasEarthData = false;
        sunDetected = false;
        earthDetected = false;
        gyrosCalibrated = false;
        currentOrientation = SatelliteOrientation.Unknown;
        earthFacingSide = EarthFacingSide.Camera;
        antennaFacingEarth = false;
        facingEarth = false;
        facingSun = false;
        isStabilized = false;
        cameraCoverOpen = false;
        photoTaken = false;
        earthInFrame = false;
        dataCompressed = false;
        communicationLinkAvailable = false;
        dataSent = false;
        planetDestroyed = false;
        hasLastCommandResult = false;
        lastCommandSuccess = false;
        lastCommandSucceeded = false;
        lastCommandMessage = "Команды еще не выполнялись.";
    }
}
