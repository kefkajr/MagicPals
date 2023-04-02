using System;

[Serializable]
public class Stat
{
    public StatTypes type;
    public int value = 0;

    public Stat(StatTypes type) {
        this.type = type;
    }
}
