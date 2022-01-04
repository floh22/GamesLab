using System;
using System.Collections;
using System.Collections.Generic;
using BezierSolution;
using Character;
using GameManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Minion
{
    public class MinionBehavior : MonoBehaviour, IGameUnit
    {
        #region StaticValues
        
        public static GameObject Splines;
        
        public static MinionValues Values;

        //Determines how fine the navagent follows the spline. Higher values require less updates to destination but follow the exact spline less accurately 
        private const float NavAgentSplineDistanceModifier = 5;
        
        #endregion

        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        public float MaxHealth { get; set; }
        public float Health { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public float AttackDamage { get; set; }

        private Animator anim;
        
        #region MinionAI

        private NavMeshAgent agent;
        private BezierSpline Spline { get; set; }
        
        private GameData.Team target;
        
        private Transform pathHolder;

        [SerializeField]
        [Range( 0f, 1f )]
        private float mNormalizedT = 0f;
        
        public float NormalizedT
        {
            get => mNormalizedT;
            set => mNormalizedT = value;
        }

        public float RotationSpeed { get; set; } = 10f;
        public float MoveSpeed { get; set; } = 5f;

        public Transform CurrentTarget { get; set; }

        public UnityEvent onPathCompleted = new UnityEvent();
        private bool onPathCompletedCalled = false;



        private bool isAttacking = false;
        private static readonly int AutoAttack = Animator.StringToHash("Auto Attack");

        #endregion

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
        }

        public void Init(int networkID, GameData.Team team, GameData.Team targetTeam)
        {
            this.NetworkID = networkID;
            this.target = targetTeam;
            this.Team = team;
            Health = Values.MinionHealth;
            MaxHealth = Values.MinionHealth;
            MoveSpeed = Values.MinionMoveSpeed;
            AttackSpeed = Values.MinionAttackSpeed;
            AttackDamage = Values.MinionAttackDamage;
            AttackRange = Values.MinionAttackRange;

            pathHolder = Splines.transform.Find(team.ToString());
            Spline = pathHolder.Find(targetTeam.ToString()).GetComponent<BezierSpline>();
        }

        // Update is called once per frame
        void Update()
        {
            //GameObject not init yet somehow
            if (NetworkID is 0) return;
            
            Move(Time.deltaTime);
            
        }

        public void SetTarget(GameData.Team targetTeam)
        {
            this.target = targetTeam;
        }
        
        private void Move( float deltaTime )
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = currentPos;
            
            #region TargetedMovement
            if (CurrentTarget is not null)
            {
                if (Vector3.Distance(currentPos, CurrentTarget.transform.position) <= Values.MinionAttackRange)
                {
                    FaceTarget(CurrentTarget);
                    agent.isStopped = true;
                    if (!isAttacking)
                    {
                        StartCoroutine(AttackTarget());
                    }
                }
                else
                {
                    agent.destination = CurrentTarget.transform.position;
                    
                    //Deprecated. Let navagent figure out how to get closest
                    //targetPos = Vector3.Lerp( currentPos, CurrentTarget.position, MoveSpeed * deltaTime );
                }
                
                float distanceFromPath = Vector3.Distance(targetPos, Spline.FindNearestPointTo(currentPos));
                
                //Return if we can keep following the target away from the path
                if (distanceFromPath < Values.MinionLeashRadius) return;
                
                //Reset target
                CurrentTarget = null;
                agent.isStopped = true;

                return;
            }
            
            #endregion
            
            //Find potential targets only if currently none is set. Max of 20 targets atm... should be enough? Increase/Decrease as needed. This might cause an issue in the future... oh well
            var results = new Collider[20];
            int size = Physics.OverlapSphereNonAlloc(currentPos, Values.MinionAgroRadius, results, 7);

            IGameUnit closest = null;
            Transform closestT = null;
            float closestDistance = Mathf.Infinity;

            //Find viable targets
            foreach (Collider res in results)
            {
                IGameUnit unit = res.GetComponent<IGameUnit>();
                
                //Ignore own units... obviously
                if(unit.Team == this.Team)
                    continue;

                //Get distance between the units.
                //TODO Maybe use the NavMesh to find the distance since now a unit over a wall could in theory be the closest
                float distance = Vector3.Distance(currentPos, res.ClosestPoint(currentPos));
                
                if (closest is null ||  distance < closestDistance)
                {
                    closest = unit;
                    closestT = res.transform;
                    closestDistance = distance;
                }
            }

            //Target found
            if (closest is not null)
            {
                CurrentTarget = closestT;
                agent.destination = CurrentTarget.position;
                agent.isStopped = false;

                return;
            }
            
            
            #region NonTargetedMovement
            
            //Check if interim destination reached. If not, dont recalculate the next position
            float dist = agent.remainingDistance;
            if (float.IsPositiveInfinity(dist) || agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance != 0) return;
            
            //Final destination reached
            if( mNormalizedT >= 1f )
            {
                mNormalizedT = 1f;

                if (onPathCompletedCalled) return;
                onPathCompletedCalled = true;
#if UNITY_EDITOR
                if( UnityEditor.EditorApplication.isPlaying )
#endif
                    onPathCompleted.Invoke();
            }
            else
            {
                onPathCompletedCalled = false;
            }
                
            //Get next position. Make sure the navagent is active
            targetPos = Spline.MoveAlongSpline( ref mNormalizedT, MoveSpeed * NavAgentSplineDistanceModifier * deltaTime );
            agent.destination = targetPos;
            agent.isStopped = false;
            
            #endregion
        }

        private IEnumerator AttackTarget()
        {
            isAttacking = true;
            anim.SetBool(AutoAttack, true);

            yield return new WaitForSeconds(Values.MinionAttackSpeed);

            if (CurrentTarget is null)
            {
                anim.SetBool(AutoAttack, false);
                isAttacking = false;
            }
        }

        private void FaceTarget(Transform t)
        {
            Vector3 lookPos = t.position - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, RotationSpeed);  
        }

        private void OnAttack()
        {
            if (CurrentTarget is null) return;
            
            IGameUnit unit = CurrentTarget.GetComponent<IGameUnit>();
            if (unit.Team != this.Team)
            {
                unit.Health = Mathf.Max(0, unit.Health - this.AttackDamage);
            }

            isAttacking = false;
        }

        public void OnPlayerAttackInRadius(IGameUnit unit, Transform unitT)
        {
            //Ignore attacks from own player
            if (unit.Team == this.Team)
                return;
            
            //If we are currently in range of the player and are attacking a non player, attack the player. Maybe switch agro when a new player attacks?
            if (CurrentTarget is not null && !CurrentTarget.CompareTag("Player") && Vector3.Distance(transform.position, unitT.position) < Values.MinionAgroRadius)
            {
                CurrentTarget = unitT;
            }
        }
    }
}
