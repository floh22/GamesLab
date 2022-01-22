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
        
        private float updateTimer;

        public MasterController()
        {
            if (Instance == null)
                Instance = this;
            
            
        }

        public void SpawnSlenderman()
        {
            Transform spawnT = GameStateController.Instance.slendermanSpawnPosition.transform;
            GameObject slenderGo = PhotonNetwork.Instantiate("Slender", spawnT.position, spawnT.rotation);
            slenderGo.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        }

        public void SpawnBases()
        {
            Transform spawnT = GameStateController.Instance.baseSpawnPosition.transform;
            GameObject baseGo = PhotonNetwork.Instantiate("Bases", spawnT.position, spawnT.rotation);
        }

        public void StartMinionSpawning(int startDelayInMs = 0)
        {
            Debug.Log($"Spawning Minions in {startDelayInMs}ms");
            StartCoroutine(SpawnMinions(startDelayInMs));
        }

        private IEnumerator SpawnMinions(int startDelayInMs = 0)
        {
            yield return new WaitForSeconds(startDelayInMs / 1000);
            
            while (!GameStateController.Instance.IsPaused)
            {
                StartCoroutine(OnWaveSpawn());
                
                yield return new WaitForSeconds(Minion.Values.WaveDelayInMs / 1000);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //Update 20 times a second, no reason to do more
            updateTimer += Time.deltaTime;
            if (!(updateTimer >= 0.05f)) return;
            GameStateController.Instance.GameTime += updateTimer;
            updateTimer = 0;
            
            GameStateController.SendGameTimeEvent(GameStateController.Instance.GameTime);
            
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
            while (wavesSpawned++ < Minion.Values.WaveSize)
            {
                SpawnMinions(this, EventArgs.Empty);
                yield return new WaitForSeconds(Minion.Values.MinionOffsetInMs / 1000);
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
                Vector3 spawnPoint = GameStateController.Instance.minionSpawnPointHolder.transform.Find(team.ToString()).transform.position;
                
                Debug.Log($"Spawning Minion at {spawnPoint}");

                GameObject go = PhotonNetwork.Instantiate(GameStateController.Instance.minionPrefab.name, spawnPoint,
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
            PhotonNetwork.Destroy(minion.gameObject);
        }
    }
}
