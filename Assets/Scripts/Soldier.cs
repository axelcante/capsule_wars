using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Soldier : MonoBehaviour
{
    private Unit m_Unit; // A reference to the unit this soldier belongs to

    [Header("Physics")]
    [SerializeField] private Collider m_Collider;
    [SerializeField] private Rigidbody m_Rigidbody;
    [SerializeField] private float m_walkSpeed;
    [SerializeField] private float m_runSpeed;

    [Header("Graphics")]
    public Image m_SelectorImage;
    public Color m_SelectorColor;
    public float m_hoveredAlpha;

    [Header("Movement")]
    [SerializeField] private float m_speed = 1f;
    [SerializeField] private float m_slowDownSpeed = .5f;
    [SerializeField] private float m_distanceToStopThresh = 0.1f;
    [SerializeField] private float m_distanceToMoveThresh = 0.7f;
    [SerializeField] private float m_maxVelocityPerFrame = 2f;
    private Vector3 m_TargetPosition = new Vector3();
    private Vector3 m_Direction = new Vector3();
    private float m_distance = 0f;

    [Header("Data")]
    // TODO: SET TO PRIVATE
    public SoldierState m_State = SoldierState.Idle;
    private bool m_isAlive = true;

    // Data
    private int m_row;

    #region PRIVATE METHODS

    // Start is called before the first frame update
    private void Start()
    {
        m_TargetPosition = transform.position;
    }



    // FixedUpdate is called every fixed framerate frame
    private void FixedUpdate ()
    {
        CalculateDistanceToTarget();

        // Keep soldier upward when alive
        if (m_isAlive)
            transform.rotation = Quaternion.Euler(transform.rotation.x, 0f, transform.rotation.z);

        // State machine deciding each soldier's behaviour
        switch (m_State) {
            case SoldierState.Idle:
                MaintainPosition();
                break;

            case SoldierState.Moving:
                Move();
                break;

            case SoldierState.Charging:
                break;

            case SoldierState.Fighting:
                break;
        }
    }



    // Calculate the distance between the target position and the current position
    // This may be expensive to do every frame but I don't know if there's another way to do it
    private void CalculateDistanceToTarget ()
    {
        m_Direction = m_TargetPosition - transform.position;
        m_distance = m_Direction.magnitude;
    }



    // Move a soldier to it's intended target position, and control its acceleration by limiting increase in velocity per frame
    private void Move ()
    {
        // Check if the character has reached the target position
        if (m_distance <= m_distanceToStopThresh) {
            // Stop moving - using Sleep() to stop all forces applied feels hacky,
            // but it's the only way I found to essentially stop a rigidbody in a single frame
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.Sleep();
            ChangeState(SoldierState.Idle);
            return;
        }

        // Calculate the target speed based on the distance to the target
        float speed = Mathf.Lerp(m_slowDownSpeed, m_speed, m_distance / (m_distanceToStopThresh * 100));

        // Calculate the target velocity based on the move speed
        Vector3 targetVelocity = m_Direction.normalized * speed;

        // Calculate the velocity change required to reach the target velocity
        Vector3 velocityChange = targetVelocity - m_Rigidbody.velocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, m_maxVelocityPerFrame);

        // Move the soldier's rigidbody by adding a force
        m_Rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }



    // Maintain a soldier's position and look to get back to it if moved by another force (when Idle)
    private void MaintainPosition ()
    {
        if (m_distance > m_distanceToMoveThresh) {
            ChangeState(SoldierState.Moving);
            Move();
        }
    }



    // Change this soldier's state
    private void ChangeState (SoldierState st)
    {
        m_State = st;
        if (st == SoldierState.Idle)
            m_Unit.UpdateNbOfMoving(-1);
        else if (st == SoldierState.Moving)
            m_Unit.UpdateNbOfMoving(1);

        //switch (st) {
        //    case SoldierState.Idle:
        //        m_Unit.UpdateNbOfMoving(-1);
        //        break;

        //    case SoldierState.Moving:
        //        m_Unit.UpdateNbOfMoving(1);
        //        break;

            //case SoldierState.Charging:
            //    break;

            //case SoldierState.Fighting:
            //    break;
        //}
    }


    #endregion PRIVATE METHODS

    #region PUBLIC METHODS

    // Turn the selector sprite on below this soldier
    public void ToggleSelector (bool toggle) => m_SelectorImage.enabled = toggle;



    // Turn the selector sprite to brite if unit is selected, otherwise revert to default alpha
    public void OnSelect (bool selected)
    {
        Color newCol = m_SelectorColor;
        newCol.a = selected ? 1f : m_hoveredAlpha;
        m_SelectorImage.color = newCol;

        if (!selected)
            ToggleSelector(false);
    }



    // Change this unit's color based on the team it belongs to
    // PS: Sorry for color, colour. I was born in the US and the English language as a whole needs to get its s*it together
    // PS2: Same thing with -ise and -ize. I'm sorry we threw your tea in the ocean, but you had it coming.
    public void SetColor (Color color)
    {
        gameObject.GetComponent<MeshRenderer>().material.color = color;
    }



    // Declare this soldier as moving and inform unit
    public void SetMovingState ()
    {
        m_State = SoldierState.Moving;

    }



    public Unit GetUnit () => m_Unit;
    public void SetUnit (Unit unit) => m_Unit = unit;
    public int GetRow () => m_row;
    public void SetRow (int row) => m_row = row;
    public Vector3 GetTargetPosition () => m_TargetPosition;
    public void SetTargetPosition (Vector3 tp) => m_TargetPosition = tp;
    //{
    //    // TODO: For now, only move when moving or idle states
    //    if (m_State == SoldierState.Moving || m_State == SoldierState.Idle) {
    //        m_TargetPosition = tp;
    //        m_State = SoldierState.Moving;
    //    }
    //}
    public void GetTeamNb () => m_Unit.GetTeamNb();

    #endregion PUBLIC METHODS
}
