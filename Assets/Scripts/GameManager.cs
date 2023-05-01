using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameFlow m_State = GameFlow.Paused;
    [SerializeField] private GameObject m_UnitPrefab;
    public string[] m_Teams;
    public Color[] m_TeamColours;
    [SerializeField] private int m_unitsPerTeam = 10;
    private int[] m_UnitsLeftToPlace = new int[2];
    private Unit m_SelectedUnit = null;
    private List<GameObject> m_UnitsInGame = new List<GameObject>();

    #region SINGLETON DECLARATION

    // Singleton implementation pattern
    private static GameManager m_instance;
    public static GameManager Instance
    {
        get { return m_instance; }
    }

    #endregion SINGLETON DECLARATION

    // Awake is called when this script is loaded
    private void Awake ()
    {
        // Singleton declaration
        if (m_instance != null && m_instance != this)
            Destroy(gameObject);
        else
            m_instance = this;

        m_UnitsLeftToPlace[0] = m_unitsPerTeam;
        m_UnitsLeftToPlace[1] = m_unitsPerTeam;
    }



    // Update is called once per frame
    void Update()
    {
        if (m_State == GameFlow.Play) {
            // Reset game on R keypress and go back to title menu
            if (Input.GetKeyDown(KeyCode.R)) {
                ResetGame();
            }

            // Unselect any selected unit if clicking in empty world space
            if (Input.GetKeyDown(KeyCode.Mouse0)) {
                if (CameraScript.Instance.GetHoveredCollider() == null && m_SelectedUnit != null) {
                    m_SelectedUnit.MarkUnselected();
                    m_SelectedUnit = null;
                }
            }

            // Move selected to unit to mouse position
            if (Input.GetKeyDown(KeyCode.Mouse1) && m_SelectedUnit != null) {
                Collider t_col = CameraScript.Instance.GetHoveredCollider();
                if (t_col == null || t_col.tag == m_SelectedUnit.tag)
                    m_SelectedUnit.GiveCommand(UnitState.Moving, CameraScript.Instance.GetMouseWorldPos());
                else if (t_col.tag != m_SelectedUnit.tag) {
                    m_SelectedUnit.GiveCommand(UnitState.Attacking, CameraScript.Instance.GetMouseWorldPos(), t_col);
                }
            }
        }
    }



    // Reset game and go back to title menu
    private void ResetGame ()
    {
        foreach (GameObject unit in m_UnitsInGame) {
            Destroy(unit);
        }

        m_UnitsLeftToPlace[0] = m_unitsPerTeam;
        m_UnitsLeftToPlace[1] = m_unitsPerTeam;

        m_State = GameFlow.Paused;

        UIManager.Instance.ResetUI();
    }

    #region PUBLIC METHODS

    // Spawn a unit with given data (team name, enemy team, color...)
    public void SpawnUnit (Vector3 pos, string teamName, string enemyTeamName, Color col)
    {
        GameObject unit = Instantiate(m_UnitPrefab, pos, Quaternion.identity);

        if (unit) {
            m_UnitsInGame.Add(unit);

            unit.GetComponent<Unit>().SetTeamName(teamName);
            unit.GetComponent<Unit>().SetEnemyTeamName(enemyTeamName);
            unit.GetComponent<Unit>().SetTeamColor(col);

            if (teamName == "Player1")
                m_UnitsLeftToPlace[0]--;
            else if (teamName == "Player2")
                m_UnitsLeftToPlace[1]--;
            else
                Debug.LogWarning("Could not find team " + teamName);
        }
    }



    // Keep track of the last selected unit
    public void AssignSelectedUnit (Unit unit)
    {
        if (m_SelectedUnit == unit) return;
        if (m_SelectedUnit != null) m_SelectedUnit.MarkUnselected();
        m_SelectedUnit = unit;
    }



    // Remove the reference to a Unit, as it has been destroyed
    public void RemoveUnitReference (GameObject unit)
    {
        m_UnitsInGame.Remove(unit);
    }



    // Play the game
    public void Play ()
    {
        m_State = GameFlow.Play;
    }

    public GameFlow GetGameState () => m_State;
    public int GetUnitsPerTeam () => m_unitsPerTeam;
    public int GetRemaningUnitsToPlace (int teamId) => m_UnitsLeftToPlace[teamId];

    #endregion PUBLIC METHODS
}
