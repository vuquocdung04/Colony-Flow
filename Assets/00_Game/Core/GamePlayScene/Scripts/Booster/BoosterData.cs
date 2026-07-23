[System.Serializable]
public class BoosterRecord
{
    public int Amount = 3;
    public bool TutorialDone;
}

public class BoosterData : MultiSaveData<BoosterData, BoosterType, BoosterRecord>
{
    public override string Key => "boosters";
}
