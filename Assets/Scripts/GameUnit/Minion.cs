using System;
using System.Collections;
using System.Collections.Generic;
using BezierSolution;
using Character;
using GameManagement;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Utils;

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
        [field: SerializeField] public GameData.Team Team { get; set; }
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
        }

        public void Init(int networkID, GameData.Team team, GameData.Team targetTeam)
        {
            //Components
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            obstacle = GetComponent<NavMeshObstacle>();
            
            //Values
            this.NetworkID = networkID;
            this.targetTeam = targetTeam;
            this.Team = team;
            Health = Values.MinionHealth;
            MaxHealth = Values.MinionHealth;
            MoveSpeed = Values.MinionMoveSpeed;
            AttackSpeed = Values.MinionAttackSpeed;
            AttackDamage = Values.MinionAttackDamage;
            AttackRange = Values.MinionAttackRange;

            //Pathfinding
            pathHolder = Splines.transform.Find(team.ToString());
            currentPath = pathHolder.Find(targetTeam.ToString()).GetComponent<BezierSpline>();

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
            // if (!(updateTimer >= Values.UpdateRateInS)) return;
            // AILogic();
            updateTimer = 0;
        }
        
        public bool IsDestroyed()
        {
            return !gameObject;
        }
        
        private void OnDrawGizmos()
        {
            if (this.IsDestroyed())
                return;
            if (!ShowTarget) return;

            Vector3 target = agent.destination;
            
            if (CurrentChaseTarget != null && !CurrentChaseTarget.IsDestroyed())
            {
                target = CurrentChaseTarget.Position;
            }
            
            if (CurrentAttackTarget != null && !CurrentAttackTarget.IsDestroyed())
            {
                target = CurrentAttackTarget.Position;
            }
            GizmoUtils.DrawLine(Position, target, 1 , ColorUtils.GetColor(this.Team.ToString()));
            GizmoUtils.DrawPoint(target, 0.5f, ColorUtils.GetColor(this.Team.ToString()));
        }

        void AILogic()
        {
            switch (currentMinionState)
            {
                case MinionState.Idle:
                    agent.isStopped = true;
                    obstacle.enabled = true;
                    break;
                case MinionState.Walking:
                    CheckPath();
                    break;
                case MinionState.LookingForPath:
                    DetermineDestination();
                    break;
                case MinionState.Attacking:
                    if (CurrentAttackTarget is not null)
                    {
                        FaceTarget(CurrentAttackTarget.Position);

                        if (attackCycle is null)
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
            if (Vector3.Distance(Position, nextWayPoint) <= agent.stoppingDistance )
            {
                if (NormalizedT > 1)
                {
                    NormalizedT = 1;
                    onPathCompleted.Invoke();
                    currentMinionState = MinionState.Idle;
                }
                nextWayPoint = currentPath.MoveAlongSpline( ref mNormalizedT, MoveSpeed  );
                agent.SetDestination(nextWayPoint);
            }
            else
            {
                obstacle.enabled = false;
                agent.destination = nextWayPoint;
                agent.isStopped = false;
                currentMinionState = MinionState.Walking;
            }
        }

        void CheckPath()
        {
            //Find potential targets only if currently none is set. Max of 20 targets atm... should be enough? Increase/Decrease as needed. This might cause an issue in the future... oh well
            var results = new Collider[20];
            Physics.OverlapSphereNonAlloc(Position, Values.MinionAgroRadius, results, LayerMask.GetMask("GameObject"));

            IGameUnit closest = null;
            float closestDistance = Mathf.Infinity;
            
            //Find viable targets
            foreach (Collider res in results.NotNull())
            {
                IGameUnit unit = res.GetComponent<IGameUnit>();

                //Ignore units without GameUnit component
                if (unit is null)
                {
                    continue;
                }

                //Ignore own units... obviously
                if(unit.Team == this.Team)
                    continue;

                //Get distance between the units.
                //TODO Maybe use the NavMesh to find the distance since now a unit over a wall could in theory be the closest
                float distance = Vector3.Distance(Position, res.ClosestPoint(Position));

                if (closest is not null && !(distance < closestDistance)) continue;
                closest = unit;
                closestDistance = distance;
            }


            if (closest is not null)
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
                    agent.destination = closest.Position;
                    CurrentChaseTarget = closest;
                    currentMinionState = MinionState.ChasingTarget;
                }

                return;
            }
            
            //Check if interim destination reached. If not, dont recalculate the next position
            float dist = Vector3.Distance(Position, agent.destination);
            if (float.IsPositiveInfinity(dist) || agent.pathStatus != NavMeshPathStatus.PathComplete || dist > agent.stoppingDistance) return;
            
            currentMinionState = MinionState.LookingForPath;
            obstacle.enabled = true;
            agent.isStopped = true;
        }

        void CheckReturnPath()
        {
            if (Vector3.Distance(Position, nextWayPoint) <= agent.stoppingDistance)
            {
                currentMinionState = MinionState.LookingForPath;
            }
        }

        void ChaseTarget()
        {
            if (CurrentChaseTarget == null || CurrentChaseTarget.IsDestroyed())
            {
                currentMinionState = MinionState.ReturningToPath;
                return;
            }
            float distanceToTarget = Vector3.Distance(Position, CurrentChaseTarget.Position);
            Vector3 nearestPointOnPath = currentPath.FindNearestPointTo(Position);
            float distanceToPath = Vector3.Distance(Position, nearestPointOnPath);

            if (distanceToPath > Values.MinionLeashRadius)
            {
                currentMinionState = MinionState.ReturningToPath;
                agent.destination = nearestPointOnPath;
                nextWayPoint = nearestPointOnPath;
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
            if (Vector3.Distance(Position, CurrentAttackTarget.Position) > Values.MinionAttackRange)
            {
                Debug.Log("Minion out of range");
                CurrentAttackTarget.RemoveAttacker(this);
                CurrentChaseTarget = CurrentAttackTarget;
                currentMinionState = MinionState.ChasingTarget;
                CurrentAttackTarget = null;
                attackCycle = null;
                yield break;
            }
            
            agent.isStopped = true;
            obstacle.enabled = true;
            
            anim.SetBool(AnimAutoAttack, true);
            CurrentAttackTarget.AddAttacker(this);
            yield return new WaitForSeconds(1 / Values.MinionAttackSpeed);
            //AttackTarget();
            attackCycle = null;
        }

        void AttackTarget()
        {
            if (CurrentAttackTarget is null)
            {
                currentMinionState = MinionState.LookingForPath;
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

        public void Damage(IGameUnit unit, float damageTaken)
        {
            //Because this is a hashset, duplicates will not be added
            // CurrentlyAttackedBy.Add(unit);
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
