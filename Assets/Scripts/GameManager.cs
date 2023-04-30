using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject m_UnitPrefab;
    [SerializeField] private string[] m_Teams;
    [SerializeField] private Color[] m_TeamColours;
    //[SerializeField] private int m_nbOfTeams;
    private Unit m_SelectedUnit = null;

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
    }



    // Update is called once per frame
    void Update()
    {
        // TODO: TEMP, REMOVE
        if (Input.GetKeyDown(KeyCode.R)) {
            SpawnUnit(CameraScript.Instance.GetMouseWorldPos(), m_Teams[0], m_Teams[1], m_TeamColours[0]);
        }

        // TODO: TEMP, REMOVE
        if (Input.GetKeyDown(KeyCode.T)) {
            SpawnUnit(CameraScript.Instance.GetMouseWorldPos(), m_Teams[1], m_Teams[0], m_TeamColours[1]);
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



    // Spawn a unit with a given team number
    private void SpawnUnit (Vector3 pos, string teamName, string enemyTeamName, Color col)
    {
        GameObject unit = Instantiate(m_UnitPrefab, pos, Quaternion.identity);
        unit.GetComponent<Unit>().SetTeamName(teamName);
        unit.GetComponent<Unit>().SetEnemyTeamName(enemyTeamName);
        unit.GetComponent<Unit>().SetTeamColor(col);
    }

    #region PUBLIC METHODS

    // Keep track of the last selected unit
    public void AssignSelectedUnit (Unit unit)
    {
        if (m_SelectedUnit == unit) return;
        if (m_SelectedUnit != null) m_SelectedUnit.MarkUnselected();
        m_SelectedUnit = unit;
    }

    #endregion PUBLIC METHODS
}
