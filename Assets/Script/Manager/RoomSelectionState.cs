using UnityEngine;

public static class RoomSelectionState
{
    private const string UnlockKey = "ReelOfDungeon.HighestUnlockedRoom";
    public static int SelectedRoom { get; private set; } = 1;
    public static bool IsUnlocked(int room) => room > 0 && room <= PlayerPrefs.GetInt(UnlockKey, 1);
    public static void Select(int room) => SelectedRoom = Mathf.Max(1, room);
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockKey);
        PlayerPrefs.Save();
        SelectedRoom = 1;
    }
    public static void Unlock(int room)
    {
        if (room <= PlayerPrefs.GetInt(UnlockKey, 1)) return;
        PlayerPrefs.SetInt(UnlockKey, room);
        PlayerPrefs.Save();
    }
}
