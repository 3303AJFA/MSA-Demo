using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToBattle(int poiID)
    {
        GameState.Instance.pendingPOI_ID = poiID;
        SceneManager.LoadScene("BattleScene");
    }

    public void GoToStory(int poiID, string storySceneName)
    {
        GameState.Instance.pendingPOI_ID = poiID;
        SceneManager.LoadScene(storySceneName);
    }

    public void ReturnToMap()
    {
        SceneManager.LoadScene("MapScene");
    }
}