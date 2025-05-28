using UnityEngine;


public enum GameMode { None, MeVsMe, MeVsAI }

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;
    public GameMode currentMode = GameMode.None;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetMeVsMe() => currentMode = GameMode.MeVsMe;
    public void SetMeVsAI() => currentMode = GameMode.MeVsAI;
}
