public static class SelectedLevel
{
    public static LevelConfig Current { get; private set; }

    public static void Set(LevelConfig levelConfig)
    {
        Current = levelConfig;
    }

    public static void Clear()
    {
        Current = null;
        SelectedCardLoadout.Clear();
    }
}
