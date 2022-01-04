using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion;
using Photon.Pun;
using UnityEngine;

namespace GameManagement
{
    public class MasterController : MonoBehaviourPunCallbacks
    {

        private System.Timers.Timer waveTimer;
        private HashSet<System.Timers.Timer> minionDelayTimers;

        private Dictionary<GameData.Team, HashSet<MinionBehavior>> minions;
        private Dictionary<GameData.Team, GameData.Team> targets;

        private MinionValues minionValues;
        private GameObject minionPrefab;
        private GameObject spawnPointHolder;
        
        public MasterController()
        {
            minions = new Dictionary<GameData.Team,  HashSet<MinionBehavior>>();
            targets = new Dictionary<GameData.Team, GameData.Team>();

            foreach (GameData.Team team in (GameData.Team[])Enum.GetValues(typeof(GameData.Team)))
            {
                minions.Add(team, new HashSet<MinionBehavior>());
                
                //Set default target to opposing team
                targets.Add(team, (GameData.Team)(((int)team + 2) % 4));
            }
        }

        public void Init(MinionValues minionValues, GameObject minionPrefab, GameObject spawnPointHolder, GameObject minionPaths)
        {
            this.minionValues = minionValues;
            this.minionPrefab = minionPrefab;
            this.spawnPointHolder = spawnPointHolder;

            MinionBehavior.Values = minionValues;
            MinionBehavior.Splines = minionPaths;
        }

        public async Task StartMinionSpawning(int startDelayInMs = 0)
        {
            if (startDelayInMs != 0)
            {
                await Task.Delay(startDelayInMs);
            }
            OnWaveSpawn();
            waveTimer = new System.Timers.Timer() {Interval = minionValues.WaveDelayInMs};
            waveTimer.Elapsed += (s, e) => OnWaveSpawn();
            waveTimer.Start();
        }

        public void StopMinionSpawning()
        {
            waveTimer.Dispose();
        }

        // Update is called once per frame
        void Update()
        {
            
        }


        void OnWaveSpawn()
        {
            for (int waves = 1; waves < minionValues.WaveSize; waves++)
            {
                System.Timers.Timer t = new System.Timers.Timer()
                    { Interval = waves * minionValues.MinionOffsetInMs, Enabled = true, AutoReset = false, };
                t.Elapsed += SpawnMinions;
                t.Start();
                minionDelayTimers.Add(t);
            }
            SpawnMinions(this, EventArgs.Empty);
        }

        void SpawnMinions(object o , EventArgs e)
        {
            foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
            {
                Vector3 spawnPoint = spawnPointHolder.transform.Find(team.ToString()).transform.position;

                GameObject go = PhotonNetwork.Instantiate(minionPrefab.name, spawnPoint,
                    Quaternion.LookRotation((Vector3.zero - transform.position).normalized));
                MinionBehavior behavior = go.GetComponent<MinionBehavior>();
                behavior.Init(go.GetInstanceID(), team, targets[team]);
                minions[team].Add(behavior);
            }
            
            System.Timers.Timer t = o as System.Timers.Timer;
            minionDelayTimers.Remove(t);
            t?.Dispose();
        }


        [PunRPC]
        void SetMinionTarget(GameData.Team team, GameData.Team target)
        {

            targets[team] = target;
            //For now, have all minions instantly switch agro. Maybe change this over so only future minions switch agro?
            foreach (MinionBehavior minionBehavior in minions[team])
            {
                minionBehavior.SetTarget(target);
            }
        }
    }
}
