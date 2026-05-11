using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("References")]
    public HeroFigure hero;
    public PointOfInterest startingPOI;

    [Header("Settings")]
    public int regenerateAfterVisits = 5;

    private int visitCount = 0;
    private List<PointOfInterest> allPOIs = new List<PointOfInterest>();
    private bool inputBlocked = false;

    void Awake() => Instance = this;

    void Start()
    {
        allPOIs.AddRange(FindObjectsByType<PointOfInterest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

        if (GameState.Instance != null && GameState.Instance.mapStateByPOI.Count > 0)
        {
            RestoreFromGameState();
        }
        else
        {
            foreach (var poi in allPOIs)
                poi.Regenerate();

            if (startingPOI != null)
            {
                hero.currentPOI = startingPOI;
                hero.transform.position = startingPOI.transform.position + Vector3.up * 0.5f;
                startingPOI.SetState(POIState.Visited);
                UnlockNeighbors(startingPOI);
            }
        }
    }

    public void TryMoveTo(PointOfInterest target)
    {
        if (hero.currentPOI == null) return;
        if (!hero.currentPOI.neighbors.Contains(target)) return;
        if (target.currentState != POIState.Available && target.currentState != POIState.Visited) return;

        bool wasAvailable = target.currentState == POIState.Available;
        SetInputBlocked(true);

        hero.MoveTo(target, () =>
        {
            if (wasAvailable)
            {
                target.OnPlayerArrived();
                UnlockNeighbors(target);

                if (target.currentType != POIType.Empty)
                {
                    visitCount++;
                    Debug.Log($"Events passed: {visitCount}/{regenerateAfterVisits}");

                    if (visitCount >= regenerateAfterVisits)
                    {
                        visitCount = 0;
                        RegenerateRandomPOIs();
                    }
                }
            }
            else
            {
                hero.currentPOI = target;
            }

            SetInputBlocked(false);
        });
    }

    public void OnPOIVisited(PointOfInterest poi)
    {
        Debug.Log($"MapManager: POI {poi.poiID} visited. Type: {poi.currentType}");
    }

    void UnlockNeighbors(PointOfInterest poi)
    {
        foreach (var neighbor in poi.neighbors)
        {
            if (neighbor.currentState == POIState.Locked)
                neighbor.SetState(POIState.Available);
        }
    }

    void RegenerateRandomPOIs()
    {
        Debug.Log("=== Experiment iteration: map regenerating ===");
        foreach (var poi in allPOIs)
        {
            if (poi.isFixed) continue;
            if (poi == hero.currentPOI) continue;
            if (poi.currentState != POIState.Visited) continue;

            poi.Regenerate();
        }
    }

    public void SetInputBlocked(bool block) => inputBlocked = block;
    public bool IsInputBlocked() => inputBlocked;

    // ===== Save/Restore =====

    void RestoreFromGameState()
    {
        var gs = GameState.Instance;

        foreach (var poi in allPOIs)
        {
            if (gs.mapStateByPOI.TryGetValue(poi.poiID, out var saved))
            {
                poi.currentType = saved.type;
                poi.SetState(saved.state);
            }
        }

        var returnPOI = allPOIs.Find(p => p.poiID == gs.pendingPOI_ID);
        if (returnPOI != null)
        {
            hero.currentPOI = returnPOI;
            hero.transform.position = returnPOI.transform.position + Vector3.up * 0.5f;
            UnlockNeighbors(returnPOI);
        }

        visitCount = gs.visitCount;
    }

    public void SaveToGameState()
    {
        var gs = GameState.Instance;
        gs.mapStateByPOI.Clear();

        foreach (var poi in allPOIs)
        {
            gs.mapStateByPOI[poi.poiID] = new POISavedData
            {
                type = poi.currentType,
                state = poi.currentState
            };
        }

        gs.visitCount = visitCount;
    }
}