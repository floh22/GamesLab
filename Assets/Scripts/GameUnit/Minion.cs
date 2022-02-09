using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using Character;
using ExitGames.Client.Photon;
using GameManagement;
using JetBrains.Annotations;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Utils;
using Network;
using NetworkedPlayer;

namespace GameUnit
{
    public class Minion : MonoBehaviourPunCallbacks, IGameUnit
    {
        #region Enums

        public enum MinionState
        {
            Idle,
            Walking,
            LookingForPath,
            Attacking,
            ChasingTarget,
            ReturningToPath
        }

        #endregion

        #region StaticValues

        public static GameObject Splines;


        public static MinionValues Values;


        //Determines how fine the navagent follows the spline. Higher values require less updates to destination but follow the exact spline less accurately 
        private const float NavAgentSplineDistanceModifier = 10;

        #endregion

        #region GameUnit

        public int NetworkID { get; set; }

        public int OwnerID
        {
            get
            {
                if (!photonView.IsDestroyed())
                {
                    return photonView.OwnerActorNr;
                }

                return -1;
            }
        }

        [field: SerializeField] public GameData.Team Team { get; set; }
        public GameUnitType Type => GameUnitType.Minion;
        public GameObject AttachtedObjectInstance { get; set; }

        public Vector3 Position
        {
            get => transform.position.XZPlane();
            set => transform.position = value;
        }

        public float MaxHealth { get; set; }
        [field: SerializeField] public float Health { get; set; }
        public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; } = 10;
        public float AttackDamage { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool IsVisible { get; set; }

        #endregion

        #region Animator

        [SerializeField] private Animator anim;
        private static readonly int AnimAutoAttack = Animator.StringToHash("AutoAttack");
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MovementSpeed");
        private static readonly int AnimHealth = Animator.StringToHash("Health");
        private static readonly int AnimAttackSpeed = Animator.StringToHash("AttackSpeedMult");
        private static readonly int AnimDoDeath = Animator.StringToHash("DoDie");

        #endregion

        #region MinionAI

        public MinionState currentMinionState = MinionState.Idle;

        [Header("Pathfinding")] private NavMeshAgent agent;

        private NavMeshObstacle obstacle;
        [SerializeField] [Range(0f, 1f)] private float mNormalizedT = 0f;

        public float NormalizedT
        {
            get => mNormalizedT;
            set => mNormalizedT = value;
        }

        public BezierSpline currentPath;
        public Vector3 nextWayPoint;
        public UnityEvent onPathCompleted = new UnityEvent();
        public GameData.Team targetTeam;
        private Transform pathHolder;

        [field: Header("Attack Logic")] public IGameUnit CurrentAttackTarget { get; set; }
        public IGameUnit CurrentChaseTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; } = new HashSet<IGameUnit>();

        public int attackingID;
        public IEnumerator attackCycle;

        #endregion

        #region Private Fields

        [SerializeField] private GameObject minionUiPrefab;
        [SerializeField] private GameObject fogOfWarMesh;
        [SerializeField] private GameObject damageText;

        private float updateTimer;
        private MinionUI minionUI;

        #endregion

        public Material[] minion_materials;


        public bool ShowTarget = false;

        void Awake()
        {
            this.NetworkID = gameObject.GetPhotonView().InstantiationId;

            Vector3 minionSpawnLocation = transform.position;
            Vector3 baseSpawnLocation;

            foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
            {
                baseSpawnLocation = GameStateController.Instance.minionSpawnPointHolder.transform.Find(team.ToString())
                    .transform.position;

                if (baseSpawnLocation == minionSpawnLocation)
                {
                    this.Team = team;
                }
            }

            // Debug.Log("Minion of team " + Team.ToString() + " spawned locally with NetworkID = " + NetworkID.ToString());

            // Add to local instance of GameStateController
            GameStateController.Instance.GameUnits.Add(this);
            GameStateController.Instance.Minions[Team].Add(this);

            if (UIManager.Instance.isGameOver)
            {
                GetComponent<UnitVisibilityScript>().enabled = false;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // If the minion's team is the same as the local player's team
            // enable the visibility mesh for the minion

            if (this.Team.Equals(PersistentData.Team))
            {
                fogOfWarMesh.gameObject.SetActive(true);
            }

            SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>();

            switch (this.Team.ToString())
            {
                case "RED":
                    this.gameObject.layer = LayerMask.NameToLayer("REDUnits");
                    break;
                case "BLUE":
                    this.gameObject.layer = LayerMask.NameToLayer("BLUEUnits");
                    renderer.material = minion_materials[1];
                    break;
                case "GREEN":
                    this.gameObject.layer = LayerMask.NameToLayer("GREENUnits");
                    renderer.material = minion_materials[2];
                    break;
                case "YELLOW":
                    this.gameObject.layer = LayerMask.NameToLayer("YELLOWUnits");
                    renderer.material = minion_materials[3];
                    break;
                default:
                    break;
            }


            // Create UI
            if (minionUiPrefab != null)
            {
                GameObject uiGo = Instantiate(minionUiPrefab);
                minionUI = uiGo.GetComponent<MinionUI>();
                minionUI.SetTarget(this);
                switch (Team)
                {
                    case GameData.Team.RED: 
                        minionUI.SetColor(Color.red);
                        break;
                    case GameData.Team.BLUE: 
                        minionUI.SetColor(Color.blue);
                        break;
                    case GameData.Team.GREEN:
                        minionUI.SetColor(Color.green);
                        break;
                    case GameData.Team.YELLOW: 
                        minionUI.SetColor(Color.yellow);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("<Color=Red><b>Missing</b></Color> MinionUiPrefab reference on minion Prefab.", this);
            }
        }

        public void Init(int networkID, GameData.Team team, GameData.Team target)
        {
            //Components
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            obstacle = GetComponent<NavMeshObstacle>();

            //Values
            // this.NetworkID = networkID; // moved to Awake()
            this.targetTeam = target;
            // this.Team = team;  // moved to Awake()
            Health = Values.MinionHealth;
            MaxHealth = Values.MinionHealth;
            MoveSpeed = Values.MinionMoveSpeed;
            AttackSpeed = Values.MinionAttackSpeed;
            AttackDamage = Values.MinionAttackDamage;
            AttackRange = Values.MinionAttackRange;

            //Pathfinding
            pathHolder = Splines.transform.Find(team.ToString());
            currentPath = pathHolder.Find(target.ToString()).GetComponent<BezierSpline>();

            agent.speed = MoveSpeed;
            agent.stoppingDistance = Values.MinionAttackRange - 1f;

            nextWayPoint = Position;

            currentMinionState = MinionState.LookingForPath;

            // Moved to Awake()
            // GameStateController.Instance.GameUnits.Add(this);
            // GameStateController.Instance.Minions[Team].Add(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            //Only do logic for the minion on the server
            if (!photonView.IsMine)
                return;

            attackingID = CurrentAttackTarget?.NetworkID ?? -1;
            updateTimer += Time.deltaTime;
            if (!(updateTimer >= Values.UpdateRateInS)) return;
            AILogic();
            updateTimer = 0;
        }

        private void OnDestroy()
        {
            GameStateController.Instance.Minions[Team].Remove(this);
            GameStateController.Instance.GameUnits.Remove(this);
        }

        public bool IsDestroyed()
        {
            return !gameObject;
        }

        private void OnDrawGizmos()
        {
            if (this.IsDestroyed() || this == null || this.Equals(null))
                return;
            if (!ShowTarget) return;

            Vector3 target = agent.destination;

            if (CurrentChaseTarget != null && !CurrentChaseTarget.Equals(null) && !CurrentChaseTarget.IsDestroyed())
            {
                target = CurrentChaseTarget.Position;
            }

            if (CurrentAttackTarget != null && !CurrentAttackTarget.Equals(null) && !CurrentAttackTarget.IsDestroyed())
            {
                target = CurrentAttackTarget.Position;
            }

            if (double.IsPositiveInfinity(Math.Abs(target.magnitude)))
            {
                target = Position;
            }

            target = target.XZPlane();
            GizmoUtils.DrawLine(Position, target, 1, ColorUtils.GetColor(this.Team.ToString()));
            GizmoUtils.DrawPoint(target, 0.5f, ColorUtils.GetColor(this.Team.ToString()));
        }

        void AILogic()
        {
            switch (currentMinionState)
            {
                case MinionState.Idle:
                    break;
                case MinionState.Walking:
                    CheckPath();
                    break;
                case MinionState.LookingForPath:
                    DetermineDestination();
                    break;
                case MinionState.Attacking:
                    if (CurrentAttackTarget != null && !CurrentAttackTarget.Equals(null))
                    {
                        if (CurrentAttackTarget == null || CurrentAttackTarget.Equals(null))
                        {
                            currentMinionState = MinionState.LookingForPath;
                            break;
                        }

                        if ( GetDistanceToTarget(CurrentAttackTarget) >
                             Values.MinionAttackRange)
                        {
                            CurrentAttackTarget.RemoveAttacker(this);
                            CurrentChaseTarget = CurrentAttackTarget;
                            currentMinionState = MinionState.ChasingTarget;
                            CurrentAttackTarget = null;
                            attackCycle = null;
                            break;
                        }

                        FaceTarget(CurrentAttackTarget.Position);

                        if (attackCycle == null || attackCycle.Equals(null))
                        {
                            attackCycle = AttackLogic();
                            StartCoroutine(attackCycle);
                        }

                        break;
                    }

                    currentMinionState = MinionState.LookingForPath;
                    break;
                case MinionState.ChasingTarget:
                    ChaseTarget();
                    break;
                case MinionState.ReturningToPath:
                    CheckReturnPath();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateAnimator();

            if (Health <= 0)
            {
                Die();
            }
        }

        void DetermineDestination()
        {
            if (NormalizedT > 1)
            {
                NormalizedT = 1;
                onPathCompleted.Invoke();
                currentMinionState = MinionState.Idle;
            }

            //only get a next waypoint if we are close to our current one
            //should reduce the number of instances where minions walk past each other
            if (Vector3.Distance(transform.position, nextWayPoint) < 2)
                nextWayPoint = currentPath.MoveAlongSpline(ref mNormalizedT, MoveSpeed);

            if (!agent.enabled)
            {
                StartCoroutine(EnableAgent(
                    () =>
                    {
                        currentMinionState = MinionState.Walking;
                        agent.speed = Values.MinionMoveSpeed;
                        agent.SetDestination(nextWayPoint);
                    })
                );
            }
            else
            {
                currentMinionState = MinionState.Walking;
                agent.speed = Values.MinionMoveSpeed;
                agent.SetDestination(nextWayPoint);
            }
        }

        void CheckPath()
        {
            var closeUnits = FindUnits();
            (IGameUnit closest, float closestDistance) = (from unit in closeUnits orderby unit.Value select unit)
                .Where(kvp => kvp.Key.Team != this.Team).FirstOrDefault();

            if (closest != null && !closest.Equals(null))
            {
                //Target found

                //Attack if in range
                if (closestDistance < Values.MinionAttackRange)
                {
                    CurrentAttackTarget = closest;
                    currentMinionState = MinionState.Attacking;
                }
                //Move towards target if not in range
                else
                {
                    StartCoroutine(EnableAgent(
                        () =>
                        {
                            currentMinionState = MinionState.ChasingTarget;
                            agent.destination = closest.Position;
                            CurrentChaseTarget = closest;
                        })
                    );
                }

                return;
            }

            //Check if interim destination reached. If not, dont recalculate the next position
            float dist = Vector3.Distance(Position, agent.destination);
            if (dist > agent.stoppingDistance + 0.5) return;


            if (dist < 0.05)
                StartCoroutine(DisableAgent(() => currentMinionState = MinionState.LookingForPath));
            else
                currentMinionState = MinionState.LookingForPath;
        }


        Dictionary<IGameUnit, float> FindUnits()
        {
            string[] layers = { "REDUnits", "BLUEUnits", "GREENUnits", "YELLOWUnits"};
            //Only look for units of other teams
            layers = layers.Where(layer => !layer.StartsWith(Team.ToString(), StringComparison.InvariantCultureIgnoreCase)).ToArray();

            // Find potential targets only if currently none is set. Max of 20 targets atm... should be enough? Increase/Decrease as needed. This might cause an issue in the future... oh well
            var results = new Collider[20];
            var foundUnits = new Dictionary<IGameUnit, float>();

            //Find viable targets. Just use all layers in the layer mask instead of doing the same work 4 times Aymane...
            Physics.OverlapSphereNonAlloc(Position, Values.MinionAgroRadius, results, LayerMask.GetMask(layers));

            //Find viable targets
            foreach (Collider res in results.NotNull())
            {
                IGameUnit unit = res.GetComponent<IGameUnit>();

                //Ignore units without GameUnit component
                if (unit == null || unit.Equals(null) || unit.IsAlive == false)
                {
                    continue;
                }

                //Get distance between the units.
                //TODO Maybe use the NavMesh to find the distance since now a unit over a wall could in theory be the closest
                float distance = Vector3.Distance(Position, res.ClosestPoint(Position));

                foundUnits.TryAdd(unit, distance);
            }

            return foundUnits;
        }
        
        float GetDistanceToTarget(IGameUnit target)
        {
            if (target == null || target.Equals(null))
                return Mathf.Infinity;
            
            if(target.Type != GameUnitType.Structure)
                return Vector3.Distance(Position, target.Position.XZPlane());
                
            //Only get physical distance to edge when its a base since other game units are far smaller
                
            string[] layers = { "REDUnits", "BLUEUnits", "GREENUnits", "YELLOWUnits"};
            //Only look for units of other teams
            layers = layers.Where(layer => !layer.StartsWith(Team.ToString(), StringComparison.InvariantCultureIgnoreCase)).ToArray();

            // Find potential targets only if currently none is set. Max of 20 targets atm... should be enough? Increase/Decrease as needed. This might cause an issue in the future... oh well
            var results = new RaycastHit[5];

            Physics.RaycastNonAlloc(new Ray(Position, target.Position.XZPlane()), results, Values.MinionLeashRadius,
                LayerMask.GetMask(layers));
            RaycastHit targetHit = default;
            
            foreach (RaycastHit hit in results)
            {
                try
                {
                    IGameUnit u = hit.collider.gameObject.GetComponent<IGameUnit>();
                    if (u.NetworkID != target.NetworkID) continue;
                    targetHit = hit;
                }
                catch
                {
                    //only get gameunits
                }
            }

            return EqualityComparer<RaycastHit>.Default.Equals(targetHit, default) ? Mathf.Infinity : targetHit.distance;
        }

        void CheckReturnPath()
        {
            if (!agent.enabled)
            {
                StartCoroutine(EnableAgent(CheckReturnPathActiveAgent));
                return;
            }

            CheckReturnPathActiveAgent();
        }

        void CheckReturnPathActiveAgent()
        {
            float dist = Vector3.Distance(Position.XZPlane(), nextWayPoint.XZPlane());
            if (dist <= agent.stoppingDistance)
            {
                currentMinionState = MinionState.LookingForPath;
                return;
            }

            if (dist <= Values.MinionAttackRange)
            {
                var results = FindUnits();

                bool foundAlly = false;
                bool foundEnemy = false;

                foreach (IGameUnit unit in results.Keys.NotNull())
                {
                    //Ignore units without GameUnit component
                    if (unit == null || unit.Equals(null) || unit.IsAlive == false)
                    {
                        continue;
                    }

                    if (unit.Team == Team)
                    {
                        foundAlly = true;
                    }
                    else
                    {
                        foundEnemy = true;
                    }
                }

                //If an enemy is on the path we wish to return to, attack it
                if (foundEnemy)
                {
                    currentMinionState = MinionState.Walking;
                    return;
                }

                //If an ally is blocking out path we wish to return to, ignore them and find the next point to travel to
                if (foundAlly)
                {
                    currentMinionState = MinionState.LookingForPath;
                    return;
                }

                currentMinionState = MinionState.LookingForPath;
            }

            if (agent.destination != nextWayPoint)
                agent.destination = nextWayPoint;
        }

        void ChaseTarget()
        {
            Vector3 nearestPointOnPath = currentPath.FindNearestPointTo(Position);
            //If our target does not exist anymore, break off and return to path
            if (CurrentChaseTarget == null || CurrentChaseTarget.Equals(null) || CurrentChaseTarget.IsDestroyed())
            {
                currentMinionState = MinionState.Walking;
                if (agent.enabled)
                    agent.destination = nearestPointOnPath;
                nextWayPoint = nearestPointOnPath;
                return;
            }

            float distanceToTarget = GetDistanceToTarget(CurrentChaseTarget);
            float distanceToPath = Vector3.Distance(Position, nearestPointOnPath);

            //Check if target is outside of leash range
            if (distanceToPath > Values.MinionLeashRadius || distanceToTarget > Values.MinionLeashRadius)
            {
                CurrentAttackTarget = null;
                CurrentChaseTarget = null;

                if (!agent.enabled)
                {
                    StartCoroutine(EnableAgent(() =>
                    {
                        currentMinionState = MinionState.Walking;
                        agent.destination = nearestPointOnPath;
                        nextWayPoint = nearestPointOnPath;
                    }));
                    return;
                }

                currentMinionState = MinionState.Walking;
                agent.destination = nearestPointOnPath;
                nextWayPoint = nearestPointOnPath;


                return;
            }

            //If we are targeting a minion, see if there is a closer minion we can attack
            if (CurrentChaseTarget.Type == GameUnitType.Minion)
            {
                (IGameUnit closest, float closestDistance) = (from unit in FindUnits() orderby unit.Value select unit).FirstOrDefault(unit => unit.Key.Type == GameUnitType.Minion && unit.Key.Team != this.Team);

                //If there are not minions found nearby, we have no target. This will make minions de agro other minions as soon as they are out of agro range... might break stuff?
                if (!(closest == null || closest.Equals(null) || closest.IsDestroyed()) &&
                    closest != CurrentChaseTarget)
                {
                    if (!agent.enabled)
                    {
                        StartCoroutine(EnableAgent(() =>
                        {
                            agent.destination = closest.Position;
                            CurrentChaseTarget = closest;
                            distanceToTarget = closestDistance;
                        }));
                    }
                    else
                    {
                        agent.destination = closest.Position;
                        CurrentChaseTarget = closest;
                        distanceToTarget = closestDistance;
                    }
                }
            }

            if (distanceToTarget < Values.MinionAttackRange)
            {
                CurrentAttackTarget = CurrentChaseTarget;
                CurrentChaseTarget = null;
                currentMinionState = MinionState.Attacking;
                return;
            }


            if (!agent.enabled)
            {
                StartCoroutine(EnableAgent(() => { agent.destination = CurrentChaseTarget.Position.XZPlane(); }));
            }
            else
            {
                agent.destination = CurrentChaseTarget.Position.XZPlane();
            }
        }

        IEnumerator AttackLogic()
        {
            yield return DisableAgent();

            //quick n dirty

            if (CurrentAttackTarget == null || CurrentAttackTarget.Equals(null))
            {
                currentMinionState = MinionState.LookingForPath;
                yield break;
            }

            if (GetDistanceToTarget(CurrentAttackTarget) > Values.MinionAttackRange)
            {
                CurrentAttackTarget.RemoveAttacker(this);
                CurrentChaseTarget = CurrentAttackTarget;
                currentMinionState = MinionState.ChasingTarget;
                CurrentAttackTarget = null;
                attackCycle = null;
                yield break;
            }

            //pls ignore this :)

            anim.SetBool(AnimAutoAttack, true);
            CurrentAttackTarget.AddAttacker(this);
            yield return new WaitForSeconds(1 / Values.MinionAttackSpeed);
            attackCycle = null;
        }

        void AttackTarget()
        {
            if (!photonView.IsMine)
                return;
            if (CurrentAttackTarget == null || CurrentAttackTarget.Equals(null) || CurrentAttackTarget.IsAlive == false)
            {
                currentMinionState = MinionState.Walking;
                anim.SetBool(AnimAutoAttack, false);
                return;
            }

            if (CurrentAttackTarget.Team != this.Team)
            {
                IGameUnit.SendDealDamageEvent(this, CurrentAttackTarget, Values.MinionAttackDamage);
            }
        }

        void UpdateAnimator()
        {
            anim.SetFloat(AnimMoveSpeed, agent.velocity.magnitude);
            anim.SetFloat(AnimHealth, Health);
            anim.SetFloat(AnimAttackSpeed, AttackSpeed);
        }

        void FaceTarget(Vector3 pos)
        {
            Vector3 lookPos = pos - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, RotationSpeed);
        }

        void Die()
        {
            IsAlive = false;
            GameObject minionUiGo = minionUI.gameObject;
            minionUiGo.SetActive(false);
            Destroy(minionUiGo);
            foreach (IGameUnit gameUnit in CurrentlyAttackedBy)
            {
                if (gameUnit.Type == GameUnitType.Player && Vector3.Distance(gameUnit.Position, Position) <
                    IGameUnit.DistanceForExperienceOnDeath)
                {
                    var instance = gameUnit.AttachtedObjectInstance;
                    if (instance == null)
                    {
                        Debug.LogError("Attached object instance is missing when Minion dies");
                        continue;
                    }

                    var controller = instance.GetComponent<PlayerController>();
                    if (controller == null)
                    {
                        Debug.LogError("Player Controller is missing when Minion dies");
                        continue;
                    }

                    controller.AddExperienceBySource(true);
                }

                gameUnit.TargetDied(this);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                MasterController.Instance.RemoveMinion(this);
            }

            //PhotonNetwork.Destroy(gameObject);
            anim.SetBool(AnimDoDeath, true);
        }


        private void DestroyObject()
        {
            PhotonNetwork.Destroy(gameObject);
        }


        IEnumerator EnableAgent([CanBeNull] Action nextFunc = null)
        {
            if (agent.enabled)
            {
                nextFunc?.Invoke();
                yield break;
            }


            MinionState prevState = currentMinionState;
            currentMinionState = MinionState.Idle;

            obstacle.enabled = false;
            yield return null;
            agent.enabled = true;
            agent.isStopped = false;

            currentMinionState = prevState;
            nextFunc?.Invoke();
        }

        IEnumerator DisableAgent([CanBeNull] Action nextFunc = null)
        {
            if (!agent.enabled)
            {
                nextFunc?.Invoke();
                yield break;
            }

            MinionState prevState = currentMinionState;
            currentMinionState = MinionState.Idle;

            agent.isStopped = true;
            agent.enabled = false;
            yield return null;
            obstacle.enabled = true;

            currentMinionState = prevState;
            nextFunc?.Invoke();
        }

        public void DoDamageVisual(IGameUnit unit, float damageTaken)
        {
            //Cant take damage from a null object
            if (unit == null || unit.Equals(null) || this == null || this.Equals(null))
                return;


            //Because this is a hashset, duplicates will not be added
            CurrentlyAttackedBy.Add(unit);

            //this.Health -= damageTaken;

            Vector3 spawnPosition = transform.position;
            spawnPosition.y = 1;

            DamageIndicator indicator = Instantiate(damageText, spawnPosition, Quaternion.identity)
                .GetComponent<DamageIndicator>();

            indicator.SetDamageText(damageTaken);
        }

        public void SetTargetTeam(GameData.Team team)
        {
            targetTeam = team;
            currentPath = pathHolder.Find(targetTeam.ToString()).GetComponent<BezierSpline>();


            //Don't think any of this is needed honestly
            return;
            // Vector3 dest = currentPath.FindNearestPointTo(Position);
            // nextWayPoint = dest;

            // agent.SetDestination(dest);
            // currentMinionState = MinionState.Walking;
        }


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local player, send data
                stream.SendNext(this.NetworkID);
                stream.SendNext(this.Team);
                stream.SendNext(this.Health);
                stream.SendNext(this.MaxHealth);
                stream.SendNext(this.MoveSpeed);
                stream.SendNext(this.AttackDamage);
                stream.SendNext(this.AttackSpeed);
            }
            else
            {
                // Network player, receive data
                this.NetworkID = (int) stream.ReceiveNext();
                this.Team = (GameData.Team) stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
                this.MaxHealth = (float) stream.ReceiveNext();
                this.MoveSpeed = (float) stream.ReceiveNext();
                this.AttackDamage = (float) stream.ReceiveNext();
                this.AttackSpeed = (float) stream.ReceiveNext();
            }
        }
    }
}