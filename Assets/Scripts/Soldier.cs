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
    [SerializeField] private LayerMask m_SoldierLayer;
    [SerializeField] private float m_distanceToStopThresh = 0.05f;
    [SerializeField] private float m_distanceToSlowDown = 5f;
    [SerializeField] private float m_maxVelocityPerFrame = 0.07f;
    [SerializeField] private float m_avoidanceDistance = 5f; 
    [SerializeField] private float m_avoidanceForce = 10f;
    [SerializeField] private float m_avoidanceWeight = 1.2f;

    [Header("Movement")]
    [SerializeField] private AnimationCurve m_SpeedCurve;
    [SerializeField] private float m_maxSpeed = 1f;
    [SerializeField] private float m_chargeSpeed = 2f;
    private Vector3 m_TargetPosition = new Vector3();
    private Vector3 m_Direction = new Vector3();
    private float m_distance = 0f;
    private float m_maxDistance = 0f;

    [Header("Combat")]
    [SerializeField] private float m_detectEnemyDistance = 3f;
    [SerializeField] private float m_DPS = 10f;
    [SerializeField] private float m_distanceToDamange = 2f;
    private float m_damageRandomiser;

    [Header("Data")]
    private SoldierState m_State = SoldierState.Idle;
    private Soldier m_EnemySoldier;
    [SerializeField] private float m_maxHealth = 100f;
    [SerializeField] private float m_health;
    private bool m_isAlive = true;
    private int m_row;

    #region PRIVATE METHODS

    // Start is called before the first frame update
    private void Start()
    {
        m_TargetPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        m_Collider.tag = m_Unit.GetTeamName();
        m_health = m_maxHealth;

        m_damageRandomiser = Random.Range(0.7f, 1.3f);
    }



    // FixedUpdate is called every fixed framerate frame
    private void FixedUpdate ()
    {
        if (GameManager.Instance.GetGameState() == GameFlow.Play) {
            CalculateDistanceToTarget();

            // Keep soldier upward when alive
            if (m_isAlive)
                transform.rotation = Quaternion.Euler(transform.rotation.x, 0f, transform.rotation.z);

            // State machine deciding each soldier's behaviour
            switch (m_State) {
                case SoldierState.Idle:
                    MaintainPosition();
                    LookForEnemy(m_detectEnemyDistance);
                    break;

                case SoldierState.Moving:
                    Move();
                    LookForEnemy(m_detectEnemyDistance);
                    break;

                case SoldierState.Attacking:
                    GetFightPosition();
                    Charge();
                    break;
            }
        }
    }



    // Detect collisions -- FIGHT TO THE DEATH
    private void OnCollisionStay (Collision collision)
    {
        if (IsEnemy(collision.collider)) {
            if (m_State == SoldierState.Attacking && m_distance <= m_distanceToDamange) {
                DealDamage(collision.collider);
            }
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

        // Decide if soldier should steer around obstacles and allies
        Vector3 newDirection = noSteer ? m_Direction.normalized : Steer();

        // Calculate the target speed based on the distance to the target and a reversed evaluation curve (goes from 1 to 0 here)
        float speed;
        if (m_distance <= m_distanceToSlowDown) {
            speed = m_SpeedCurve.Evaluate(m_distance / m_maxDistance) * m_maxSpeed / 2;
        }
        else
            speed = m_maxSpeed;

        // Calculate the target velocity based on the move speed
        Vector3 targetVelocity = newDirection * speed;

        // Calculate the velocity change required to reach the target velocity as if accelerating
        Vector3 velocityChange = targetVelocity - m_Rigidbody.velocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, m_maxVelocityPerFrame);

        // Move the soldier's rigidbody by adding a force
        m_Rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }



    // Charge towards an enemy soldier - simplified version of the above Move() method
    private void Charge ()
    {
        // Avoid obstacles and allies
        Vector3 newDirection = Steer();

        // Calculate the target velocity based on the charge speed
        Vector3 targetVelocity = newDirection * m_chargeSpeed;

        // Calculate the velocity change required to reach the target velocity as if accelerating
        Vector3 velocityChange = targetVelocity - m_Rigidbody.velocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, m_maxVelocityPerFrame);

        // Move the soldier's rigidbody by adding a force
        m_Rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }



    // Move while avoiding obstacles and ally soldiers
    private Vector3 Steer ()
    {
        Vector3 normDirection = m_Direction.normalized;
        Vector3 y0Position = new Vector3(transform.position.x, 0f, transform.position.z);

        // Check for any colliders in the path we are looking to take
        Collider[] colliders = Physics.OverlapSphere(y0Position, m_avoidanceDistance, m_SoldierLayer);
        Vector3 avoidanceVec = Vector3.zero;
        foreach (Collider collider in colliders) {
            if (IsObstacle(collider) || IsAlly(collider)) {
                // Calculate repulsion force to avoid the obstacle/ally
                Vector3 colY0Position = new Vector3(collider.transform.position.x, 0f, collider.transform.position.z);
                Vector3 repulsionDirection = (y0Position - colY0Position).normalized;
                avoidanceVec += repulsionDirection * m_avoidanceForce * m_avoidanceWeight;
            }
        }
        return normDirection + avoidanceVec;
    }



    // Maintain a soldier's position and look to get back to it if moved by another force (when Idle)
    private void MaintainPosition ()
    {
        if (m_distance > m_distanceToStopThresh)
            ChangeState(SoldierState.Moving);
    }



    // Look for enemies in range
    private void LookForEnemy (float dist)
    {
        Vector3 y0Position = new Vector3(transform.position.x, 0f, transform.position.z);
        Collider[] colliders = Physics.OverlapSphere(y0Position, dist, m_SoldierLayer);

        foreach (Collider collider in colliders) {
            if (IsEnemy(collider)) {
                m_EnemySoldier = collider.gameObject.GetComponent<Soldier>();
                if (m_EnemySoldier != null) {
                    ChangeState(SoldierState.Attacking);
                    break;
                }
            }
        }
    }


    
    // Deal damage to an enemy soldier based on charge speed
    private void DealDamage (Collider collider)
    {
        Soldier targetSoldier = collider.gameObject.GetComponent<Soldier>();

        if (targetSoldier)
            targetSoldier.TakeDamage(m_DPS * m_damageRandomiser * Time.deltaTime);
    }


    // TODO - COOLS ANIMATIONS? I wish
    // What happens when the soldier is killed (health < 0)
    private void OnDeath ()
    {
        m_Unit.UpdateOnDeath(gameObject);
        Destroy(gameObject);
    }



    // Get a new target position based on the target enemy, or set as idle and look for other enemies around
    private void GetFightPosition ()
    {
        if (m_EnemySoldier)
            m_TargetPosition = m_EnemySoldier.gameObject.transform.position;
        else {
            ChangeState(SoldierState.Moving);
            m_Unit.AskForNewPosition();
        }
    }



    // Change this soldier's state and take action if necessary
    private void ChangeState (SoldierState st)
    {
        if (m_State != st) {
            m_State = st;

            switch (st) {
                case SoldierState.Idle:
                    m_Unit.UpdateNbOfIdle(1);
                    break;

                case SoldierState.Moving:
                    break;

                case SoldierState.Attacking:
                    if (m_EnemySoldier)
                        m_Unit.TellIsFighting(m_EnemySoldier.GetUnit());
                    break;

                default:
                    break;
            }
        }
    }


    #endregion PRIVATE METHODS

    #region PUBLIC METHODS

    // Take damage (called by other enemy soldier scripts)
    public void TakeDamage (float amount)
    {
        m_health -= amount;
        if (m_health <= 0) {
            OnDeath();
        }
    }



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
    // Don't even get me started with "centre" and "center"...
    public void SetColor (Color color)
    {
        gameObject.GetComponent<MeshRenderer>().material.color = color;
    }



    // Collider tag tests
    public bool IsEnemy (Collider collider) => !collider.CompareTag(m_Collider.tag) && !collider.CompareTag("Obstacle");
    public bool IsAlly (Collider collider) => collider.CompareTag(m_Collider.tag);
    public bool IsObstacle (Collider collider) => collider.CompareTag("Obstacle");



    public Unit GetUnit () => m_Unit;
    public void SetUnit (Unit unit) => m_Unit = unit;
    public int GetRow () => m_row;
    public void SetRow (int row) => m_row = row;
    public Vector3 GetTargetPosition () => m_TargetPosition;
    public void SetTargetPosition (Vector3 tp)
    {
        // Only move when moving or idle states
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
