using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [Header("Config")]
    public int poiID;
    public bool isFixed = false;
    public POIType fixedType;

    [Header("Random Chances")]
    [Range(0f, 1f)] public float spawnChance = 0.7f;
    public float chanceEmpty    = 0.20f;
    public float chanceCombat   = 0.40f;
    public float chanceEvent    = 0.20f;
    public float chanceMerchant = 0.10f;
    public float chanceFriend   = 0.10f;

    [Header("Connections")]
    public List<PointOfInterest> neighbors = new List<PointOfInterest>();

    [Header("Icons (optional)")]
    public GameObject iconQuad;
    public Material iconCombat;
    public Material iconStory;
    public Material iconEvent;
    public Material iconMerchant;
    public Material iconBoss;
    public Material iconFriend;
    public Material iconEmpty;

    [HideInInspector] public POIType currentType;
    [HideInInspector] public POIState currentState = POIState.Locked;

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Regenerate()
    {
        if (isFixed)
        {
            currentType  = fixedType;
            currentState = POIState.Available;
        }
        else
        {
            float roll = Random.value;
            if (roll > spawnChance)
            {
                currentState = POIState.Hidden;
                currentType  = POIType.Empty;
            }
            else
            {
                currentState = POIState.Locked;
                currentType  = RollType();
            }
        }

        RefreshVisual();
        UpdateIcon();
    }

    POIType RollType()
    {
        float roll = Random.value;
        float cumulative = 0f;

        cumulative += chanceEmpty;
        if (roll < cumulative) return POIType.Empty;

        cumulative += chanceCombat;
        if (roll < cumulative) return POIType.Combat;

        cumulative += chanceEvent;
        if (roll < cumulative) return POIType.Event;

        cumulative += chanceMerchant;
        if (roll < cumulative) return POIType.Merchant;

        return POIType.Friend;
    }

    public void SetState(POIState state)
    {
        currentState = state;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (rend == null) return;

        Material mat = rend.material;

        switch (currentState)
        {
            case POIState.Available:
                mat.color = Color.green;
                gameObject.SetActive(true);
                break;
            case POIState.Visited:
                mat.color = Color.gray;
                gameObject.SetActive(true);
                break;
            case POIState.Locked:
                mat.color = Color.red;
                gameObject.SetActive(true);
                break;
            case POIState.Hidden:
                gameObject.SetActive(false);
                break;
        }
    }

    void UpdateIcon()
    {
        if (iconQuad == null) return;
        var iconRend = iconQuad.GetComponent<Renderer>();
        if (iconRend == null) return;

        Material chosen = null;
        switch (currentType)
        {
            case POIType.Combat:   chosen = iconCombat; break;
            case POIType.Story:    chosen = iconStory; break;
            case POIType.Event:    chosen = iconEvent; break;
            case POIType.Merchant: chosen = iconMerchant; break;
            case POIType.Boss:     chosen = iconBoss; break;
            case POIType.Friend:   chosen = iconFriend; break;
            default:               chosen = iconEmpty; break;
        }

        if (chosen != null) iconRend.material = chosen;
    }

    public void OnPlayerArrived()
    {
        SetState(POIState.Visited);

        switch (currentType)
        {
            case POIType.Empty:
                Debug.Log($"POI {poiID}: Empty.");
                break;

            case POIType.Combat:
                if (CanGoToScene())
                {
                    MapManager.Instance.SaveToGameState();
                    SceneFlow.Instance.GoToBattle(poiID);
                }
                break;

            case POIType.Story:
                if (CanGoToScene())
                {
                    MapManager.Instance.SaveToGameState();
                    SceneFlow.Instance.GoToStory(poiID, "StoryScene");
                }
                break;

            case POIType.Boss:
                if (CanGoToScene())
                {
                    MapManager.Instance.SaveToGameState();
                    SceneFlow.Instance.GoToBattle(poiID);
                }
                break;

            case POIType.Event:
                if (EventOverlay.Instance != null)
                    EventOverlay.Instance.OpenRandom(poiID);
                else
                    Debug.LogWarning("EventOverlay not found in scene.");
                break;

            case POIType.Merchant:
                if (EventOverlay.Instance != null)
                    EventOverlay.Instance.OpenMerchant(poiID);
                else
                    Debug.LogWarning("EventOverlay not found in scene.");
                break;

            case POIType.Friend:
                if (EventOverlay.Instance != null)
                    EventOverlay.Instance.OpenFriend(poiID);
                else
                    Debug.LogWarning("EventOverlay not found in scene.");
                break;
        }

        MapManager.Instance.OnPOIVisited(this);
    }

    bool CanGoToScene()
    {
        if (GameState.Instance == null)
        {
            Debug.LogWarning("GameState not found! Add GameState GameObject to scene.");
            return false;
        }
        if (SceneFlow.Instance == null)
        {
            Debug.LogWarning("SceneFlow not found! Add SceneFlow GameObject to scene.");
            return false;
        }
        return true;
    }

    void OnMouseDown()
    {
        if (MapManager.Instance.IsInputBlocked()) return;
        if (currentState != POIState.Available && currentState != POIState.Visited) return;
        MapManager.Instance.TryMoveTo(this);
    }
}