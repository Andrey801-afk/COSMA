using System;

[Serializable]
public class MissionConditionData
{
    public MissionConditionType conditionType;
    public int value;

    public MissionConditionData()
    {
        conditionType = MissionConditionType.PowerEnabled;
        value = 50;
    }

    public MissionConditionData(MissionConditionType type, int numericValue = 0)
    {
        conditionType = type;
        value = numericValue;
    }

    public MissionConditionData Clone()
    {
        return new MissionConditionData(conditionType, value);
    }
}
