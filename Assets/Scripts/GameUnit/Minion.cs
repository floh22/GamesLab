using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using Character;
using GameManagement;
using JetBrains.Annotations;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Utils;
using Network;

namespace GameUnit
{
    public class Minion : MonoBehaviour, IGameUnit, IDamagable
    {
        #region Enums
        public enum MinionState {Idle, Walking, LookingForPath, Attacking, ChasingTarget, ReturningToPath }
        
        #endregion
        
        #region StaticValues
        
        public static GameObject Splines;
        
        public static MinionValues Values;

        //Determines how fine the navagent follows the spline. Higher values require less updates to destination but follow the exact spline less accurately 
        private const float NavAgentSplineDistanceModifier = 10;
        
        #endregion
        
        #region GameUnit
        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        public GameUnitType Type => GameUnitType.Minion;
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
        #endregion
        
        #region Animator
        [SerializeField] private Animator anim;
        private static readonly int AnimAutoAttack = Animator.StringToHash("AutoAttack");
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MovementSpeed");
        private static readonly int AnimHealth = Animator.StringToHash("Health");
        private static readonly int AnimAttackSpeed = Animator.StringToHash("AttackSpeedMult");
        #endregion
        
        #region MinionAI
        
        public MinionState currentMinionState = MinionState.Idle;

        [Header("Pathfinding")] 
        private NavMeshAgent agent;

        private NavMeshObstacle obstacle;
        [SerializeField] [Range( 0f, 1f )] private float mNormalizedT = 0f;
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

        [field:Header("Attack Logic")] 
        public IGameUnit CurrentAttackTarget { get; set; }
        public IGameUnit CurrentChaseTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }

        public int attackingID;
        public IEnumerator attackCycle;
        #endregion

        private float updateTimer;

        public bool ShowTarget = false;

        // Start is called before the first frame update
        void Start()
        {
            // If the minion's team is the same as the local player's team
            // enable the visibility mesh for the minion

            Debug.Log($"PersistentData.Team.ToString() = {PersistentData.Team.ToString()}");
            Debug.Log($"this.Team.ToString() = {this.Team.ToString()}");

            if (this.Team.ToString().Equals(PersistentData.Team.ToString()))
            {
                this.gameObject.transform.Find("FogOfWarVisibleRangeMesh").gameObject.SetActive(true);
            }
        }

        public void Init(int networkID, GameData.Team team, GameData.Team target)
        {
            Debug.Log($"In Init");
            //Components
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            obstacle = GetComponent<NavMeshObstacle>();
            
            //Values
            this.NetworkID = networkID;
            this.targetTeam = target;
            this.Team = team;
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
            
            //Attacking
            CurrentlyAttackedBy = new HashSet<IGameUnit>();

            currentMinionState = MinionState.LookingForPath;
        }

        // Update is called once per frame
        void Update()
        {
            attackingID = CurrentAttackTarget?.NetworkID??-1;
            updateTimer += Time.deltaTime;
            if (!(updateTimer >= Values.UpdateRateInS)) return;
            AILogic();
            updateTimer = 0;
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
            GizmoUtils.DrawLine(Position, target, 1 , ColorUtils.GetColor(this.Team.ToString()));
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
            
            nextWayPoint = currentPath.MoveAlongSpline(ref mNormalizedT, MoveSpeed);

            if (!agent.enabled)
            {
                StartCoroutine(EnableAgent(
                    () =>
                    {
                        currentMinionState = MinionState.Walking;
                        agent.SetDestination(nextWayPoint);
                    })
                );
            }
            else
            {
                currentMinionState = MinionState.Walking;
                agent.SetDestination(nextWayPoint);
            }
        }

        void CheckPath()
        {
            var closeUnits = FindUnits();
            (IGameUnit closest, float closestDistance) = (from unit in closeUnits orderby unit.Value select unit).Where(kvp => kvp.Key.Team != this.Team).FirstOrDefault();

            if ( closest != null && !closest.Equals(null))
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
                            currentMinionState = MinionState.Walking;
                            agent.destination = closest.Position;
                            CurrentChaseTarget = closest;
                        })
                    );
                }

                return;
            }
            
            //Check if interim destination reached. If not, dont recalculate the next position
            float dist = Vector3.Distance(Position, agent.destination);
            if (dist > agent.stoppingDistance + 0.5 ) return;
            
            
            if(dist < 0.05)
                StartCoroutine(DisableAgent(() => currentMinionState = MinionState.LookingForPath));
            else
                currentMinionState = MinionState.LookingForPath;
        }


        Dictionary<IGameUnit, float> FindUnits()
        {
            //Find potential targets only if currently none is set. Max of 20 targets atm... should be enough? Increase/Decrease as needed. This might cause an issue in the future... oh well
            var results = new Collider[20];
            Physics.OverlapSphereNonAlloc(Position, Values.MinionAgroRadius, results, LayerMask.GetMask("GameObject"));

            var foundUnits = new Dictionary<IGameUnit, float>();
            
            
            //Find viable targets
            foreach (Collider res in results.NotNull())
            {
                IGameUnit unit = res.GetComponent<IGameUnit>();

                //Ignore units without GameUnit component
                if (unit == null || unit.Equals(null) )
                {
                    continue;
                }

                //Get distance between the units.
                //TODO Maybe use the NavMesh to find the distance since now a unit over a wall could in theory be the closest
                float distance = Vector3.Distance(Position, res.ClosestPoint(Position));
                
                
                foundUnits.Add(unit, distance);
                
            }
            
            return foundUnits;
        }

        void CheckReturnPath()
        {
            StartCoroutine(EnableAgent(() =>
            {
                float dist = Vector3.Distance(Position, nextWayPoint);
                if ( dist <= agent.stoppingDistance)
                {
                    currentMinionState = MinionState.LookingForPath;
                    return;
                }

                if (dist <= Values.MinionAttackRange)
                {
                    var results = new Collider[20];
                    Physics.OverlapSphereNonAlloc(Position, Values.MinionAttackRange, results, LayerMask.GetMask("GameObject"));

                    bool foundAlly = false;
                    bool foundEnemy = false;

                    foreach (Collider res in results.NotNull())
                    {
                        IGameUnit unit = res.GetComponent<IGameUnit>();

                        //Ignore units without GameUnit component
                        if (unit == null || unit.Equals(null) )
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
            }));
            
        }

        void ChaseTarget()
        {
            Vector3 nearestPointOnPath = currentPath.FindNearestPointTo(Position);
            //If our target does not exist anymore, break off and return to path
            if (CurrentChaseTarget == null|| CurrentChaseTarget.Equals(null) || CurrentChaseTarget.IsDestroyed() )
            {
                currentMinionState = MinionState.Walking;
                agent.destination = nearestPointOnPath;
                nextWayPoint = nearestPointOnPath;
                return;
            }
            
            float distanceToTarget = Vector3.Distance(Position, CurrentChaseTarget.Position.XZPlane());
            float distanceToPath = Vector3.Distance(Position, nearestPointOnPath);

            //Check if target is outside of leash range
            if (distanceToPath > Values.MinionLeashRadius)
            {
                currentMinionState = MinionState.ReturningToPath;
                agent.destination = nearestPointOnPath;
                nextWayPoint = nearestPointOnPath;
                return;
            }
            
            //If we are targeting a minion, see if there is a closer minion we can attack
            if (CurrentChaseTarget.Type == GameUnitType.Minion)
            {
                (IGameUnit closest, float closestDistance)  = (from unit in FindUnits() orderby unit.Value select unit).Where(unit => unit.Key.Type == GameUnitType.Minion && unit.Key.Team != this.Team).FirstOrDefault();
                
                //If there are not minions found nearby, we have no target. This will make minions de agro other minions as soon as they are out of agro range... might break stuff?
                if (!(closest == null || closest.Equals(null) || closest.IsDestroyed()) && closest != CurrentChaseTarget)
                {
                    agent.destination = closest.Position;
                    CurrentChaseTarget = closest;
                    distanceToTarget = closestDistance;
                    
                }
            }
            
            if (distanceToTarget < Values.MinionAttackRange)
            {
                CurrentAttackTarget = CurrentChaseTarget;
                CurrentChaseTarget = null;
                currentMinionState = MinionState.Attacking;
                
            }
        }

        IEnumerator AttackLogic()
        {
            if (CurrentAttackTarget == null || CurrentAttackTarget.Equals(null))
            {
                currentMinionState = MinionState.LookingForPath;
                yield break;
            }
            
            if (Vector3.Distance(Position, CurrentAttackTarget.Position.XZPlane()) > Values.MinionAttackRange)
            {
                CurrentAttackTarget.RemoveAttacker(this);
                CurrentChaseTarget = CurrentAttackTarget;
                currentMinionState = MinionState.ChasingTarget;
                CurrentAttackTarget = null;
                attackCycle = null;
                yield break;
            }
            
            yield return DisableAgent();
            
            anim.SetBool(AnimAutoAttack, true);
            CurrentAttackTarget.AddAttacker(this);
            yield return new WaitForSeconds(1 / Values.MinionAttackSpeed);
            attackCycle = null;
        }

        void AttackTarget()
        {
            if (CurrentAttackTarget == null || CurrentAttackTarget.Equals(null))
            {
                currentMinionState = MinionState.ReturningToPath;
                anim.SetBool(AnimAutoAttack, false);
                return;
            }
            
            if (CurrentAttackTarget.Team != this.Team)
            {
                CurrentAttackTarget.Damage(this, Values.MinionAttackDamage);
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
            foreach (IGameUnit gameUnit in CurrentlyAttackedBy)
            {
                gameUnit.TargetDied(this);
            }
            
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

        public void Damage(IGameUnit unit, float damageTaken)
        {
            //Because this is a hashset, duplicates will not be added
            CurrentlyAttackedBy.Add(unit);
            this.Health -= damageTaken;
        }

        public void SetTargetTeam(GameData.Team team)
        {
            targetTeam = team;
            currentPath = pathHolder.Find(targetTeam.ToString()).GetComponent<BezierSpline>();
            
            Vector3 dest = currentPath.FindNearestPointTo(Position);
            nextWayPoint = dest;

            agent.SetDestination(dest);
            currentMinionState = MinionState.Walking;
        }
    }
}
