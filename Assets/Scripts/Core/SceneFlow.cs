using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance;

    [Tooltip("Имя сцены боя")]
    public string battleSceneName = "BattleScene";

    [Tooltip("Имя сцены мира (район/проходимая изо-сцена). При смерти/после боя возвращаемся сюда.")]
    public string mapSceneName = "MapScene";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Запуск боя. encounterID — для будущего (тип энкаунтера / референс на врага),
    /// пока не используется, оставлен для совместимости с EnemyEncounter / EventOverlay.
    /// </summary>
    public void GoToBattle(int encounterID = 0)
    {
        SceneManager.LoadScene(battleSceneName);
    }

    public void GoToStory(string storySceneName)
    {
        SceneManager.LoadScene(storySceneName);
    }

    public void ReturnToMap()
    {
        SceneManager.LoadScene(mapSceneName);
    }
}
