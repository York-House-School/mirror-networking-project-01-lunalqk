using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI {
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour {
        [System.Serializable]
        public struct RendererIndexData {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index) {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;


        [Header("Weapons Parameters")]
        [Tooltip("Allow weapon swapping for this enemy")]
        public bool SwapToNextWeapon = false;

        [Tooltip("Time delay between a weapon swap and the next attack")]
        public float DelayAfterWeaponSwap = 0f;

        [Tooltip("The chance the object has to drop")]
        [Range(0, 1)]
        public float DropRate = 1f;

        public UnityAction onAttack;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;

        float m_LastTimeDamaged = float.NegativeInfinity;

        public PatrolPath PatrolPath { get; set; }
        public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }

        int m_PathDestinationNodeIndex;
        EnemyManager m_EnemyManager;
        ActorsManager m_ActorsManager;
        Health m_Health;
        Actor m_Actor;
        Collider[] m_SelfColliders;
        GameFlowManager m_GameFlowManager;
        bool m_WasDamagedThisFrame;
        float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
        int m_CurrentWeaponIndex;
        WeaponController m_CurrentWeapon;
        WeaponController[] m_Weapons;
        NavigationModule m_NavigationModule;

        void Start() {
            m_EnemyManager = FindObjectOfType<EnemyManager>();

            m_ActorsManager = FindObjectOfType<ActorsManager>();

            m_EnemyManager.RegisterEnemy(this);

            m_Health = GetComponent<Health>();

            m_Actor = GetComponent<Actor>();

            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_SelfColliders = GetComponentsInChildren<Collider>();

            m_GameFlowManager = FindObjectOfType<GameFlowManager>();

            // Subscribe to damage & death actions
            m_Health.OnDie += OnDie;
            m_Health.OnDamaged += OnDamaged;

            // Find and initialize all weapons
            FindAndInitializeAllWeapons();
            var weapon = GetCurrentWeapon();
            weapon.ShowWeapon(true);

            var detectionModules = GetComponentsInChildren<DetectionModule>();
            // Initialize detection module
            DetectionModule = detectionModules[0];
            DetectionModule.onDetectedTarget += OnDetectedTarget;
            DetectionModule.onLostTarget += OnLostTarget;
            onAttack += DetectionModule.OnAttack;

            var navigationModules = GetComponentsInChildren<NavigationModule>();
            // Override navmesh agent data
            if (navigationModules.Length > 0) {
                m_NavigationModule = navigationModules[0];
                NavMeshAgent.speed = m_NavigationModule.MoveSpeed;
                NavMeshAgent.angularSpeed = m_NavigationModule.AngularSpeed;
                NavMeshAgent.acceleration = m_NavigationModule.Acceleration;
            }
        }

        void Update() {
            EnsureIsWithinLevelBounds();

            DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

            m_WasDamagedThisFrame = false;
        }

        void EnsureIsWithinLevelBounds() {
            // at every frame, this tests for conditions to kill the enemy
            if (transform.position.y < SelfDestructYHeight) {
                Destroy(gameObject);
                return;
            }
        }

        void OnLostTarget() {
            onLostTarget.Invoke();
        }

        void OnDetectedTarget() {
            onDetectedTarget.Invoke();
        }

        public void OrientTowards(Vector3 lookPosition) {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f) {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        bool IsPathValid() {
            return PatrolPath && PatrolPath.PathNodes.Count > 0;
        }

        public void ResetPathDestination() {
            m_PathDestinationNodeIndex = 0;
        }

        public void SetPathDestinationToClosestNode() {
            if (IsPathValid()) {
                int closestPathNodeIndex = 0;
                for (int i = 0; i < PatrolPath.PathNodes.Count; i++) {
                    float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                    if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex)) {
                        closestPathNodeIndex = i;
                    }
                }

                m_PathDestinationNodeIndex = closestPathNodeIndex;
            } else {
                m_PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath() {
            if (IsPathValid()) {
                return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            } else {
                return transform.position;
            }
        }

        public void SetNavDestination(Vector3 destination) {
            if (NavMeshAgent) {
                NavMeshAgent.SetDestination(destination);
            }
        }

        public void UpdatePathDestination(bool inverseOrder = false) {
            if (IsPathValid()) {
                // Check if reached the path destination
                if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius) {
                    // increment path destination index
                    m_PathDestinationNodeIndex =
                        inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                    if (m_PathDestinationNodeIndex < 0) {
                        m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                    }

                    if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count) {
                        m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                    }
                }
            }
        }

        void OnDamaged(float damage, GameObject damageSource) {
            // test if the damage source is the player
            if (damageSource && !damageSource.GetComponent<EnemyController>()) {
                // pursue the player
                DetectionModule.OnDamaged(damageSource);

                onDamaged?.Invoke();
                m_LastTimeDamaged = Time.time;

                m_WasDamagedThisFrame = true;
            }
        }

        void OnDie() {
            // tells the game flow manager to handle the enemy destuction
            m_EnemyManager.UnregisterEnemy(this);

            // this will call the OnDestroy function
            Destroy(gameObject, DeathDuration);
        }

        public void OrientWeaponsTowards(Vector3 lookPosition) {
            for (int i = 0; i < m_Weapons.Length; i++) {
                // orient weapon towards player
                Vector3 weaponForward = (lookPosition - m_Weapons[i].WeaponRoot.transform.position).normalized;
                m_Weapons[i].transform.forward = weaponForward;
            }
        }

        public bool TryAtack(Vector3 enemyPosition) {
            if (m_GameFlowManager.GameIsEnding)
                return false;

            OrientWeaponsTowards(enemyPosition);

            if ((m_LastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
                return false;

            // Shoot the weapon
            bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

            if (didFire && onAttack != null) {
                onAttack.Invoke();

                if (SwapToNextWeapon && m_Weapons.Length > 1) {
                    int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_Weapons.Length;
                    SetCurrentWeapon(nextWeaponIndex);
                }
            }

            return didFire;
        }

        void FindAndInitializeAllWeapons() {
            // Check if we already found and initialized the weapons
            if (m_Weapons == null) {
                m_Weapons = GetComponentsInChildren<WeaponController>();

                for (int i = 0; i < m_Weapons.Length; i++) {
                    m_Weapons[i].Owner = gameObject;
                }
            }
        }

        public WeaponController GetCurrentWeapon() {
            FindAndInitializeAllWeapons();
            // Check if no weapon is currently selected
            if (m_CurrentWeapon == null) {
                // Set the first weapon of the weapons list as the current weapon
                SetCurrentWeapon(0);
            }


            return m_CurrentWeapon;
        }

        void SetCurrentWeapon(int index) {
            m_CurrentWeaponIndex = index;
            m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex];
            if (SwapToNextWeapon) {
                m_LastTimeWeaponSwapped = Time.time;
            } else {
                m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
            }
        }
    }
}