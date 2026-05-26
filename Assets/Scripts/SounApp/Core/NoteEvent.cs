public enum NoteName
{
    C3,
    D3,
    E3,
    F3,
    G3,
    A3,
    B3,
    C4
}

public enum NoteLength
{
    Quarter,
    Half,
    Whole,
    Eighth
}

[System.Serializable]
public class NoteEvent
{
    public bool isRest;

    public NoteName note;

    public NoteLength duration;
}