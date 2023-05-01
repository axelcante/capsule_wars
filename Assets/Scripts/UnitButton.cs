using UnityEngine;

public class UnitButton : MonoBehaviour
{
    // Lazy coding
    [Range(0, 1)]
    public int teamId;
    [Range(0, 1)]
    public int enemyTeamId;
    private bool m_isDragging = false;

    public void OnBeginDrag ()
    {
        HandleMouseInput(ButtonAction.OnDrag); 
    }

    public void OnRelease ()
    {
        HandleMouseInput(ButtonAction.OnRelease);
    }

    private void HandleMouseInput (ButtonAction act)
    {
        switch (act) {
            case ButtonAction.OnDrag:
                m_isDragging = true;
                break;

            case ButtonAction.OnRelease:
                if (m_isDragging) {
                    m_isDragging = false;
                    // Get all the data needed to spawn a unit in the game world
                    Vector3 pos = CameraScript.Instance.GetMouseWorldPos();
                    string team = GameManager.Instance.m_Teams[teamId];
                    string enemyTeam = GameManager.Instance.m_Teams[enemyTeamId];
                    Color col = GameManager.Instance.m_TeamColours[teamId];
                    // Spawn unit
                    GameManager.Instance.SpawnUnit(pos, team, enemyTeam, col);

                    // Refresh UI
                    UIManager.Instance.RefreshUI();
                }
                break;

            default:
                Debug.LogWarning("No mouse event linked");
                break;
        }
    }
}
