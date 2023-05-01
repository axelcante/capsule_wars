using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Player")]
    private Color m_TeamColor;
    private string m_teamName;
    private string m_enemyTeamName;

    // Unit parameters
    [Header("Parameters")]
    [SerializeField] private int m_maxNbOfSoldiers = 12;
    [SerializeField] private int m_maxNbOfRows = 3;
    // Let's not go too crazy here...
    //[SerializeField] private UnitType m_UnitType = UnitType.Melee;

    // Unit data
    [Header("Data")]
    private Collider m_EnemyTarget;
    [SerializeField] private int m_nbOfRows;
    // TODO: MAKE PRIVATE
    public UnitState m_State = UnitState.Idle;
    private int m_nbOfIdle = 0;

    // Unit movement information
    [Header("Movement")]
    private List<Vector3> m_TargetSolPositions = new List<Vector3>();
    private Vector3 m_TargetPosition = new Vector3();

    // System variables
    [Header("System")]
    [SerializeField] private GameObject m_SoldierPrefab;
    [SerializeField] private GameObject m_TargetCirclePrefab;
    [SerializeField] private LineRenderer m_LRenderer;
    private List<GameObject> m_TargetCircles = new List<GameObject>();
    [SerializeField] private float m_rowSpacing = 2f;
    [SerializeField] private float m_colSpacing = 2f;
    [SerializeField] private float m_colliderPadding = 2f;
    private List<GameObject> m_Soldiers = new List<GameObject>();
    private bool m_isSelected = false;



    // Awake is called when this script is loaded (even if deactivated)
    private void Start ()
    {
        SpawnSoldiers();
        CalculateUnitCollider();
    }



    // FixedUpdate is called every fixed framerate frame
    private void FixedUpdate ()
    {
        if (GameManager.Instance.GetGameState() == GameFlow.Play) {
            // Change unit stage based on what soldiers are doing
            if (m_State != UnitState.Idle && m_nbOfIdle >= m_Soldiers.Count)
                ChangeState(UnitState.Idle);
            if (m_State == UnitState.Attacking && !m_EnemyTarget)
                ChangeState(UnitState.Moving);

            // State machine deciding a unit's behaviours, which are in turn passed down to soldiers
            switch (m_State) {
                case UnitState.Idle:
                    break;

                case UnitState.Moving:
                    CalculateUnitCollider();
                    break;

                case UnitState.Attacking:
                    CalculateUnitCollider();
                    Attack();
                    break;

                case UnitState.Seeking:
                    CalculateUnitCollider();
                    Seek();
                    break;

                default:
                    break;
            }
        }
    }

    #region MOUSE DETETCION

    // When hovering over unit with mouse
    private void OnMouseEnter ()
    {
        if (!m_isSelected)
            ToggleAllSelectors(true);
    }

    // When exiting hover unit with mouse
    private void OnMouseExit ()
    {
        if (!m_isSelected)
            ToggleAllSelectors(false);
    }

    // When unit is clicked on
    private void OnMouseDown ()
    {
        m_isSelected = true;
        GameManager.Instance.AssignSelectedUnit(this);
        ToggleOnSelect();
    }

    #endregion MOUSE DETECTION

    #region SPAWNING, MOVEMENT & STATE MACHINE

    // When the unit is created, spawn the units based on start data
    private void SpawnSoldiers ()
    {
        // Calculate the number of columns needed based on the number of rows and soldiers
        int numCols = Mathf.CeilToInt((float)m_maxNbOfSoldiers / m_maxNbOfRows);

        for (int i = 0; i < m_maxNbOfSoldiers; i++) {
            // Calculate the row and column of the current soldier
            int row = i / numCols;
            int col = i % numCols;
            // Calculate the position of the soldier
            float xPos = col * m_colSpacing - ((numCols - 1) * m_colSpacing) / 2f;
            float zPos = row * m_rowSpacing - ((m_maxNbOfRows - 1) * m_colSpacing) / 2f;
            Vector3 soldierPos = new Vector3(xPos + transform.position.x, 1f, zPos + transform.position.z);

            // Create a new object at the calculated position as a child of this unit
            GameObject soldier = Instantiate(m_SoldierPrefab, soldierPos, Quaternion.identity, transform);
            soldier.GetComponent<Soldier>().SetUnit(this);
            soldier.GetComponent<Soldier>().SetRow(row);
            soldier.GetComponent<Soldier>().SetColor(m_TeamColor);
            m_Soldiers.Add(soldier);
        }

        m_nbOfRows = m_maxNbOfRows;
    }



    // Calculate and draw a box collider surrounding all soldiers in this unit
    // WARNING: could be intensive to run every fixed frame. TODO: OPTIMIZE?
    private void CalculateUnitCollider ()
    {
        // Calculate the bounds of the capsules
        Bounds bounds = new Bounds(m_Soldiers[0].transform.position, Vector3.zero);
        for (int i = 0; i < m_Soldiers.Count; i++)
            bounds.Encapsulate(m_Soldiers[i].transform.position);

        // Select the box collider or create one if there are none
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        if (!collider) {
            collider = gameObject.AddComponent<BoxCollider>();
            collider.tag = m_teamName;
            collider.isTrigger = true;
        }

        Vector3 newCenter = new Vector3(bounds.center.x - transform.position.x, 1f, bounds.center.z - transform.position.z);

        //// Check if collider center has changed since last fixed frame; if not, mark unit as idle
        //if (collider.center == newCenter)
            //ChangeState(UnitState.Idle);

        collider.center = newCenter;
        collider.size = new Vector3(bounds.size.x + m_colliderPadding, 2.2f, bounds.size.z + m_colliderPadding);
    }



    // Ccalculate desired soldier positions and pass position to each soldier
    private List<Vector3> CalculateSoldierPositions (Vector3 unitPos)
    {
        List<Vector3> positions = new List<Vector3>();

        // Calculate the number of columns needed based on the number of rows and soldiers
        int numCols = m_nbOfRows > 0 ? Mathf.CeilToInt((float)m_Soldiers.Count / m_nbOfRows) : 1;

        for (int i = 0; i < m_Soldiers.Count; i++) {
            // Calculate the row and column of the current soldier
            int row = i / numCols;
            int col = i % numCols;

            // Calculate the position of the soldier
            float xPos = col * m_colSpacing - ((numCols - 1) * m_colSpacing) / 2f;
            float zPos = row * m_rowSpacing - ((m_maxNbOfRows - 1) * m_colSpacing) / 2f;
            Vector3 newPos = new Vector3(xPos + unitPos.x, 0f, zPos + unitPos.z);
            positions.Add(newPos);
            if (m_Soldiers[i].GetComponent<Soldier>())
                m_Soldiers[i].GetComponent<Soldier>().SetTargetPosition(newPos);
        }

        return positions;
    }



    // Constantly move towards target enemy position and update drawn target line
    private void Attack ()
    {
        if (m_EnemyTarget) {
            Unit enemyUnit = m_EnemyTarget.gameObject.GetComponent<Unit>();
            if (enemyUnit) {
                SetTargetPosition(enemyUnit.GetBoundsCentre());
                m_TargetSolPositions = CalculateSoldierPositions(m_TargetPosition);
                DrawAttackingLine(enemyUnit);
            }
        }
    }



    // Seek enemy targets
    private void Seek ()
    {

    }



    // Change this unit's state
    private void ChangeState (UnitState ut)
    {
        m_State = ut;

        switch (ut) {
            case UnitState.Idle:
                if (m_TargetCircles.Count > 0) {
                    foreach (GameObject g in m_TargetCircles)
                        Destroy(g);
                }
                break;

            case UnitState.Moving:
                m_nbOfIdle = 0;
                m_TargetSolPositions = CalculateSoldierPositions(m_TargetPosition);
                DisplayTargetPositions();
                break;

            case UnitState.Seeking:
                m_nbOfIdle = 0;
                break;

            case UnitState.Attacking:
                m_nbOfIdle = 0;
                DisplayTargetPositions();
                break;
        }
    }

    #endregion SPAWNING, MOVEMENT & STATE MACHINE

    #region PUBLIC METHODS

    #region UI ELEMENTS

    // Activate or deactivate all circular selectors below individual soldiers
    public void ToggleAllSelectors (bool turnOn)
    {
        foreach (GameObject s in m_Soldiers) {
            Soldier soldier = s.GetComponent<Soldier>();
            if (soldier) {
                soldier.ToggleSelector(turnOn);
            }
        }
    }



    // Tell each soldier in this unit to activate or deactivate their selector
    public void ToggleOnSelect ()
    {
        foreach (GameObject s in m_Soldiers) {
            Soldier soldier = s.GetComponent<Soldier>();
            if (soldier) {
                soldier.OnSelect(m_isSelected);
            }
        }
        DisplayTargetPositions();
    }



    // Display or hide the target positions for each soldier in this unit
    public void DisplayTargetPositions ()
    {
        foreach (GameObject g in m_TargetCircles)
            Destroy(g);

        m_LRenderer.enabled = m_isSelected && m_EnemyTarget;

        if (m_isSelected) {
            // Moving
            if (m_EnemyTarget == null) {
                foreach (Vector3 tp in m_TargetSolPositions)
                    m_TargetCircles.Add(Instantiate(m_TargetCirclePrefab, new Vector3(tp.x, 0.1f, tp.z), Quaternion.Euler(90f, 0f, 0f), transform));
            }
            // Attacking
            else {
                DrawAttackingLine();
            }

        }
    }



    // Draw an attacking line from the current position to the target position
    public void DrawAttackingLine (Unit unit = null)
    {
        if (m_isSelected && unit) {
            m_LRenderer.SetPosition(0, GetBoundsCentre());
            m_LRenderer.SetPosition(1, unit.GetBoundsCentre());
        }
    }



    // Used for UI, displays selector circles below units if selected (or hides if not)
    public void MarkUnselected ()
    {
        m_isSelected = false;
        ToggleOnSelect();
    }

    #endregion UI ELEMENTS

    // This returns the centre point of this unit based on the calculated collider
    public Vector3 GetBoundsCentre () => gameObject.GetComponent<Collider>() ?
            gameObject.GetComponent<Collider>().bounds.center :
            Vector3.zero;
    


    // Give a command to a unit and its soldiers
    public void GiveCommand (UnitState st, Vector3 pos, Collider col = null)
    {
        // Reset idle counter
        m_nbOfIdle = 0;

        // Set target position, target (if any) and change Unit state
        SetTargetPosition(pos);
        SetTarget(col);
        ChangeState(st);
    }



    // Inform the Unit of what each soldier is doing (moving or stopped)
    public void UpdateNbOfIdle (int nb) => m_nbOfIdle += nb;



    // Inform the Unit that one of its soldiers is fighting
    public void TellIsFighting (Unit enemyUnit)
    {
        m_EnemyTarget = enemyUnit.GetComponent<Collider>();
        if (m_EnemyTarget)
            ChangeState(UnitState.Attacking);
    }



    // Inform the Unit that one of its soldiers has perished
    public void UpdateOnDeath (GameObject g)
    {
        m_Soldiers.Remove(g);
        if (m_Soldiers.Count == 0) {
            GameManager.Instance.RemoveUnitReference(gameObject);
            Destroy(gameObject);
        }
    }



    // Soldiers can ask the Unit for a new position if they cannot find an enemy in range
    public void AskForNewPosition ()
    {
        m_TargetSolPositions = CalculateSoldierPositions(m_TargetPosition);
    }



    public string GetTeamName () => m_teamName;
    public void SetTeamName (string name) => m_teamName = name;
    public string GetEnemyTeamName () => m_enemyTeamName;
    public void SetEnemyTeamName (string name) => m_enemyTeamName = name;
    public Color GetTeamColor () => m_TeamColor;
    public void SetTeamColor (Color col) => m_TeamColor = col;
    private void SetTargetPosition (Vector3 pos) => m_TargetPosition = pos;
    private void SetTarget (Collider col) => m_EnemyTarget = col;

    #endregion PUBLIC METHODS
}
