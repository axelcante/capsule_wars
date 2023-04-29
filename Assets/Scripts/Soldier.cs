using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Soldier : MonoBehaviour
{
    private Unit m_Unit; // A reference to the unit this soldier belongs to


    [Header("Graphics")]
    public Image m_SelectorImage;
    public Color m_SelectorColor;
    public float m_hoveredAlpha;

    [Header("Physics")]
    [SerializeField] private Collider m_Collider;
    [SerializeField] private Rigidbody m_Rigidbody;
    [SerializeField] private LayerMask m_LayerMask;
    [SerializeField] private float m_distanceToStopThresh = 0.05f;
    [SerializeField] private float m_distanceToMoveThresh = 0.7f;
    [SerializeField] private float m_distanceToSlowDown = 5f;
    [SerializeField] private float m_maxVelocityPerFrame = 0.07f;
    [SerializeField] private float m_avoidanceDistance = 5f; 
    [SerializeField] private float m_avoidanceForce = 10f;

    [Header("Movement")]
    [SerializeField] private AnimationCurve m_SpeedCurve;
    [SerializeField] private float m_maxSpeed = 1f;
    private Vector3 m_TargetPosition = new Vector3();
    private Vector3 m_Direction = new Vector3();
    // TODO: MAKE PRIVATE
    public float m_distance = 0f;
    private float m_maxDistance = 0f;

    [Header("Data")]
    // TODO: SET TO PRIVATE
    public SoldierState m_State = SoldierState.Idle;
    private bool m_isAlive = true;
    private int m_row;

    #region PRIVATE METHODS

    // Start is called before the first frame update
    private void Start()
    {
        m_TargetPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        m_Collider.tag = m_Unit.GetTeamName();
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
    private void CalculateDistanceToTarget (bool forceMaxDistance = false)
    {
        m_Direction = m_TargetPosition - new Vector3(transform.position.x, 0f, transform.position.z);
        m_distance = m_Direction.magnitude;

        if (m_distance > m_maxDistance || forceMaxDistance)
            m_maxDistance = m_distance;
    }



    // Move a soldier to it's intended target position, and control its acceleration by limiting increase in velocity per frame
    private void Move (bool noSteer = false)
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

        Vector3 newDirection = m_Direction.normalized;
        if (!noSteer) {
            Vector3 y0Position = new Vector3(transform.position.x, 0f, transform.position.z);
            // STEERING
            // Check for any colliders in the path we are looking to take
            Collider[] colliders = Physics.OverlapSphere(y0Position, m_avoidanceDistance, m_LayerMask);
            Vector3 avoidanceVec = Vector3.zero;
            foreach (Collider collider in colliders) {
                if (collider.gameObject.CompareTag("Obstacle")||
                    collider.gameObject.CompareTag(m_Unit.GetTeamName())) {
                    // calculate repulsion force to avoid the obstacle/enemy
                    Vector3 colY0Position = new Vector3(collider.transform.position.x, 0f, collider.transform.position.z);
                    Vector3 repulsionDirection = (y0Position - colY0Position).normalized;
                    avoidanceVec += repulsionDirection * m_avoidanceForce;
                }
            }
            newDirection += avoidanceVec;
        }


        // Calculate the target speed based on the distance to the target and a reversed evaluation curve (goes from 1 to 0 here)
        // Couldn't quite create the slowdown I wanted without having abrubt stops when asking to move too close by
        float speed;
        if (m_distance <= m_distanceToSlowDown)
            speed = m_SpeedCurve.Evaluate(m_distance / m_maxDistance) * m_maxSpeed / 2;
        else
            speed = m_maxSpeed;

        // Calculate the target velocity based on the move speed
        Vector3 targetVelocity = newDirection * speed;

        // Calculate the velocity change required to reach the target velocity if accelerating
        Vector3 velocityChange = targetVelocity - m_Rigidbody.velocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, m_maxVelocityPerFrame);

        // Move the soldier's rigidbody by adding a force
        m_Rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }



    // Maintain a soldier's position and look to get back to it if moved by another force (when Idle)
    private void MaintainPosition ()
    {
        if (m_distance > m_distanceToStopThresh) {
            ChangeState(SoldierState.Moving);
        }
    }



    // Change this soldier's state
    private void ChangeState (SoldierState st)
    {
        if (m_State != st) {
            m_State = st;

            // Could be switch case if this becomes a more complicated if/else
            if (st == SoldierState.Idle)
                m_Unit.UpdateNbOfMoving(-1);
            else if (st == SoldierState.Moving)
                m_Unit.UpdateNbOfMoving(1);
        }
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
    // ******* RANT *******
    // Sorry for color, colour. I was born in the US and the English language as a whole needs to get its s*it together
    // Same thing with -ise and -ize. I know we threw your tea in the ocean, but you had it coming.
    public void SetColor (Color color)
    {
        gameObject.GetComponent<MeshRenderer>().material.color = color;
    }



    public Unit GetUnit () => m_Unit;
    public void SetUnit (Unit unit) => m_Unit = unit;
    public int GetRow () => m_row;
    public void SetRow (int row) => m_row = row;
    public Vector3 GetTargetPosition () => m_TargetPosition;
    public void SetTargetPosition (Vector3 tp)
    {
        // TODO: For now, only move when moving or idle states
        // Force new maxDistance calculation
        if (m_State == SoldierState.Moving || m_State == SoldierState.Idle) {
            m_TargetPosition = tp;
            bool forceMaxDistance = true;
            CalculateDistanceToTarget(forceMaxDistance);
            ChangeState(SoldierState.Moving);
        }
    }
    public string GetTeamName () => m_Unit.GetTeamName();
    public string GetEnemyTeamName () => m_Unit.GetEnemyTeamName();

    #endregion PUBLIC METHODS
}
