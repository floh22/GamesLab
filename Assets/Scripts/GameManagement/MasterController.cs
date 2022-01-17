using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using GameUnit;
using Network;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;
using UnityEngine;

namespace GameManagement
{
    public class MasterController : MonoBehaviourPunCallbacks
    {

        public static MasterController Instance;
        

        private MinionValues minionValues;
        private GameObject minionPrefab;
        private GameObject spawnPointHolder;

        public bool IsPaused = false;
        
        [field: SerializeField] public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
        
        public MasterController()
        {
            if (Instance == null)
                Instance = this;
            
            
        }

        public void Init(MinionValues minionValues, GameObject minionPrefab, GameObject spawnPointHolder, GameObject minionPaths)
        {
            this.minionValues = minionValues;
            this.minionPrefab = minionPrefab;
            this.spawnPointHolder = spawnPointHolder;

            Minion.Values = minionValues;
            Minion.Splines = minionPaths;
            
            Debug.Log("Starting Master Client Controller");
        }

        public void StartMinionSpawning(int startDelayInMs = 0)
        {
            Debug.Log($"Spawning Minions in {startDelayInMs}ms");
            StartCoroutine(SpawnMinions());
        }

        private IEnumerator SpawnMinions(int startDelayInMs = 0)
        {
            yield return new WaitForSeconds(minionValues.InitWaveDelayInMs / 1000);
            
            while (!IsPaused)
            {
                StartCoroutine(OnWaveSpawn());
                
                yield return new WaitForSeconds(minionValues.WaveDelayInMs / 1000);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            if (UIManager.Instance.GameTimer == null)
            {
                return;
            }

            if (UIManager.Instance.GameTimer.timeRemainingComponent == null)
            {
                return;
            }
            if (UIManager.Instance.GameTimer.timeRemainingInSeconds == 0)
            {
                foreach (var kvp in GameStateController.Instance.Bases)
                {
                    kvp.Value.Pages--;
                }

                UIManager.Instance.GameTimer.timeRemainingInSeconds = GameData.SecondsPerRound;
            }
        }

        private IEnumerator OnWaveSpawn()
        {
            Debug.Log("Spawning Minion Wave");

            int wavesSpawned = 0;
            while (wavesSpawned++ < minionValues.WaveSize)
            {
                SpawnMinions(this, EventArgs.Empty);
                yield return new WaitForSeconds(minionValues.MinionOffsetInMs / 1000);
            }
        }

        void SpawnMinions(object o , EventArgs e)
        {
            //Don't actually spawn the minions unless we are the master client
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
            {
                Vector3 spawnPoint = spawnPointHolder.transform.Find(team.ToString()).transform.position;
                
                Debug.Log($"Spawning Minion at {spawnPoint}");

                GameObject go = PhotonNetwork.Instantiate(minionPrefab.name, spawnPoint,
                    Quaternion.LookRotation((Vector3.zero - transform.position).normalized));

                Minion behavior = go.GetComponent<Minion>();
                behavior.Init(go.GetInstanceID(), team, GameStateController.Instance.Targets[team]);
                GameStateController.Instance.Minions[team].Add(behavior);
                

                //Debug
                /*
                if (team == GameData.Team.BLUE)
                {
                    behavior.showDestination = true;
                }
                */
                
                behavior.ShowTarget = true;
            }
        }

        public void RemoveMinion(Minion minion)
        {
            GameStateController.Instance.Minions[minion.Team].Remove(minion);
        }
        
    }
}
