using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Color m_TeamColor;
    [SerializeField] private int m_teamNb;

    // Unit parameters
    [Header("Parameters")]
    [SerializeField] private int m_maxNbOfSoldiers = 12;
    [SerializeField] private int m_maxNbOfRows = 3;
    // Let's not go too crazy here...
    //[SerializeField] private UnitType m_UnitType = UnitType.Melee;

    // Unit data
    [Header("Data")]
    [SerializeField] private int m_nbOfSoliders;
    [SerializeField] private int m_nbOfRows;
    // TODO: MAKE PRIVATE
    public UnitState m_State = UnitState.Idle;
    public int m_nbOfMoving = 0;

    // Unit movement information
    [Header("Movement")]
    private List<Vector3> m_TargetSolPositions = new List<Vector3>();

    // System variables
    [Header("System")]
    [SerializeField] private GameObject m_SoldierPrefab;
    [SerializeField] private GameObject m_TargetCirclePrefab;
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
        // Change unit stage based on what soldiers are doing
        if (m_State == UnitState.Moving && m_nbOfMoving == 0)
            m_State = UnitState.Idle;

        if (m_State == UnitState.Idle && m_nbOfMoving == m_nbOfSoliders)
            m_State = UnitState.Moving;

        // State machine deciding a unit's behaviours, which are in turn passed down to soldiers
        switch (m_State) {
            case UnitState.Idle:
                break;

            case UnitState.Moving:
                CalculateUnitCollider();
                break;

            case UnitState.Attacking:
                CalculateUnitCollider();
                break;

            case UnitState.Engaged:
                CalculateUnitCollider();
                break;

            case UnitState.Retreating:
                CalculateUnitCollider();
                break;
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

    #region SPAWNING & MOVEMENT

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
        m_nbOfSoliders = m_maxNbOfSoldiers;


        /*
         * TODO: DELETE WHEN NO LONGER NEEDED
         */

        //// Determine the number of columns based on the number of soldiers and rows
        //int numCols = Mathf.CeilToInt((float)m_maxNbOfSoldiers / m_maxNbOfRows);

        //// Loop over each soldier and calculate its position
        //for (int i = 0; i < m_maxNbOfSoldiers; i++) {
        //    // Calculate the row and column of this soldier
        //    int row = i / numCols;
        //    int col = i % numCols;

        //    // Calculate the position of this soldier based on its row and column
        //    Vector3 position = new Vector3(col * m_colSpacing, 1f, row * m_rowSpacing);

        //    // Spawn the soldier at the calculated position and save it in the soldier list
        //    GameObject soldier = Instantiate(m_SoldierPrefab, position, Quaternion.identity, transform);
        //    soldier.GetComponent<Soldier>().SetUnit(this);
        //    soldier.GetComponent<Soldier>().SetRow(row);
        //    m_Soldiers.Add(soldier);
        //}

        //m_nbOfRows = m_maxNbOfRows;
        //m_nbOfSoliders = m_maxNbOfSoldiers;
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
            collider.isTrigger = true;
        }

        Vector3 newCenter = new Vector3(bounds.center.x - transform.position.x, 0f, bounds.center.z - transform.position.z);

        // Check if collider center has changed since last fixed frame; if not, mark unit as idle
        if (collider.center == newCenter)
            m_State = UnitState.Idle;

        collider.center = newCenter;
        collider.size = new Vector3(bounds.size.x + m_colliderPadding, 0.01f, bounds.size.z + m_colliderPadding);
    }



    // Ccalculate desired soldier positions and pass position to each soldier
    private List<Vector3> CalculateSoldierPositions (Vector3 unitPos)
    {
        List<Vector3> positions = new List<Vector3>();

        // Calculate the number of columns needed based on the number of rows and soldiers
        int numCols = Mathf.CeilToInt((float)m_nbOfSoliders / m_nbOfRows);

        for (int i = 0; i < m_nbOfSoliders; i++) {
            // Calculate the row and column of the current soldier
            int row = i / numCols;
            int col = i % numCols;

            // Calculate the position of the soldier
            float xPos = col * m_colSpacing - ((numCols - 1) * m_colSpacing) / 2f;
            float zPos = row * m_rowSpacing - ((m_maxNbOfRows - 1) * m_colSpacing) / 2f;
            Vector3 newPos = new Vector3(xPos + unitPos.x, 1f, zPos + unitPos.z);
            positions.Add(newPos);
            if (m_Soldiers[i].GetComponent<Soldier>())
                m_Soldiers[i].GetComponent<Soldier>().SetTargetPosition(newPos);
        }

        return positions;

            /*
             * TODO: DELETE WHEN NO LONGER NEEDED
             */
            //List<Vector3> positions = new List<Vector3>();

            //foreach(GameObject s in m_Soldiers) {
            //    Vector3 newPos = s.transform.position + unitPos;
            //    positions.Add(newPos);
            //    Soldier soldier = s.GetComponent<Soldier>();
            //    if (soldier) {
            //        soldier.SetTargetPosition(newPos);
            //    }
            //}

            //return positions;
        }

    #endregion SPAWNING & MOVEMENT

    #region UI ELEMENTS

    // Activate or deactivate all circular selectors below individual soldiers
    private void ToggleAllSelectors (bool turnOn)
    {
        foreach (GameObject s in m_Soldiers) {
            Soldier soldier = s.GetComponent<Soldier>();
            if (soldier) {
                soldier.ToggleSelector(turnOn);
            }
        }
    }



    // Tell each soldier in this unit to activate or deactivate their selector
    private void ToggleOnSelect ()
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
    private void DisplayTargetPositions ()
    {
        foreach (GameObject g in m_TargetCircles)
            Destroy(g);

        if (m_isSelected) {
            foreach (Vector3 tp in m_TargetSolPositions)
                m_TargetCircles.Add(Instantiate(m_TargetCirclePrefab, new Vector3(tp.x, 0.1f, tp.z), Quaternion.Euler(90f, 0f, 0f), transform));
        }
    }

    #endregion UI ELEMENTS

    #region PUBLIC METHODS

    // Used for UI, displays selector circles below units if selected (or hides if not)
    public void MarkUnselected ()
        {
            m_isSelected = false;
            ToggleOnSelect();
        }



    // This returns the centre point of this unit based on the calculated collider, transforming it into local space
    public Vector3 GetBoundsCentreLocal () => gameObject.GetComponent<Collider>() ?
            transform.InverseTransformPoint(gameObject.GetComponent<Collider>().bounds.center) :
            Vector3.zero;



    // Set target position to move towards when not retreating
    public void SetTargetPosition (Vector3 pos)
    {
        if (m_State != UnitState.Retreating) {
            m_State = UnitState.Moving;
            // Calculate soldier target positions
            m_TargetSolPositions = CalculateSoldierPositions(pos);
            DisplayTargetPositions();
        }
    }



    // Inform the Unit of what each soldier is doing
    public void UpdateNbOfMoving (int nb) => m_nbOfMoving += nb;



    public int GetTeamNb () => m_teamNb;
    public void SetTeamNb (int nb) => m_teamNb = nb;
    public Color GetTeamColor () => m_TeamColor;
    public void SetTeamColor (Color col) => m_TeamColor = col;

    #endregion PUBLIC METHODS
}
