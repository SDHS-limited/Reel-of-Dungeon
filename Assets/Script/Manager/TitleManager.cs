using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Selete";
    [SerializeField, Min(0.01f)] private float closeDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.7f;

    public void GameStart()
    {
        CircularSceneTransition.Begin(nextSceneName, closeDuration, revealDuration);
    }

    public void Setting()
    {
    }

    public void Quit()
    {
        Application.Quit();
    }
}
