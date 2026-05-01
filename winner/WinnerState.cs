public static class WinnerState
{
    public static string WinnerName { get; private set; }

    public static void SetWinner(string winnerName)
    {
        WinnerName = string.IsNullOrEmpty(winnerName) ? "Winner" : winnerName;
    }

    public static void Clear()
    {
        WinnerName = string.Empty;
    }
}
