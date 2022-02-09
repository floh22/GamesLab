using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using GameManagement;
using Network;
using NetworkedPlayer;
using Photon.Pun;
using UnityEngine;

namespace GameUnit
{
    public class BaseBehavior : MonoBehaviourPunCallbacks, IGameUnit
    {
        public int NetworkID { get; set; }

        public int OwnerID
        {
            get
            {
                try
                {
                    return GameStateController.Instance.Players[Team].OwnerID;
                }
                catch
                {
                    return -1;
                }
            }
        }
        public GameData.Team Team { get; set; }

        public GameObject AttachtedObjectInstance { get; set; }


        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public float MaxHealth { get; set; } = 1000;
        public GameUnitType Type => GameUnitType.Structure;
        [SerializeField] private float _health;
        public float Health
        {

            get => _health;
            set
            {
                _health = value;
                CheckHealth();
            }
        }
        public float MoveSpeed { get; set; } = 0;
        public float RotationSpeed { get; set; } = 0;
        public float AttackDamage { get; set; } = 0;
        public float AttackSpeed { get; set; } = 0;
        public float AttackRange { get; set; } = 0;
        public bool IsAlive { get; set; } = true;
        public bool IsVisible { get; set; }
        public int SecondsToChannelPage { get; set; } = PlayerValues.SecondsToChannelPage;
        public IGameUnit CurrentAttackTarget { get; set; } = null;
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }
        public GameObject innerChannelingParticleSystem;

        public IEnumerator IsAttackedCoroutine;
        public int TimeUntilIsAttackedSoundIsPlayedAgainst = 8;

        public AudioSource AudioSource;

        private int pages;
        
        public int Pages
        {
            get => pages;
            set
            {
                pages = value;
                OnPageUpdate();
            }
        }

        private bool hasBeenChanneledOnce;
        private MeshRenderer meshRenderer;
        private bool isBeingChanneled;

        // Start is called before the first frame update
        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            foreach (var material in meshRenderer.materials)
            {
                material.color = Color.white;
            }
            
            CurrentlyAttackedBy = new HashSet<IGameUnit>();
            StartCoroutine(Glow());

            if (!photonView.IsMine)
                return;
            
            //Init these by owner only since these values get synced
            pages = PlayerValues.PagesAmount;
            GameObject o = gameObject;
            NetworkID = o.GetInstanceID();
            bool res = Enum.TryParse(o.name, out GameData.Team parsedTeam);
            if (res)
            {
                Team = parsedTeam;
            }
            else
            {
                Debug.LogError($"Could not init base {o.name}");
            }

            Health = MaxHealth;
            
        }
        

        public void OnMouseDown()
        {
            if (Pages <= 0)
            {
                Debug.Log($"{Team} Base has {Pages} pages and cannot be channeled");
                return;
            }

            PlayerController channeler = PlayerController.LocalPlayerController;
            Debug.Log($"{Team} Base has been clicked by player from team {channeler.Team}");

            if (channeler.Team != Team && channeler.HasPage)
            {
                Debug.Log($"player from team {channeler.Team} has a page already");
                return;
            }

            if (Vector3.Distance(transform.position, channeler.Position) > PlayerValues.BaseChannelRange)
            {
                Debug.Log($"Player from team {channeler.Team} is too far away " +
                          $"distance = {Vector3.Distance(transform.position, channeler.Position)}" +
                          $"ChannelRange = {PlayerValues.BaseChannelRange}");
                return;
            }

            if (channeler.IsChannelingObjective)
            {
                Debug.Log($"player from team {channeler.Team} is channeling objective already");
                return;
            }

            channeler.OnChannelObjective(innerChannelingParticleSystem.transform.position, NetworkID);
            channeler.OnStartBaseChannel();
            StartCoroutine(Channel(channeler));
        }
        
        void OnPageUpdate()
        {
            if (Team != PersistentData.Team)
                return;

            if (Pages <= 0)
            {
                IsAlive = false;
                if (PlayerController.LocalPlayerInstance != null && !PlayerController.LocalPlayerInstance.Equals(null))
                    GameStateController.Instance.OnLose();
            }

            UIManager.Instance.SetPages(Pages);
        }

        public bool IsDestroyed()
        {
            return !gameObject;
        }

        public void DoDamageVisual(IGameUnit unit, float damage)
        {
            this.CurrentlyAttackedBy.Add(unit);
            this.IsAttackedCoroutine = this.IsAttackedTimer();
            Debug.Log("Base getting attacked");
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
                stream.SendNext(this.Pages);
                stream.SendNext(this.IsAlive);
            }
            else
            {
                // Network player, receive data
                this.NetworkID = (int)stream.ReceiveNext();
                this.Team = (GameData.Team)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
                this.MaxHealth = (float)stream.ReceiveNext();
                this.Pages = (int)stream.ReceiveNext();
                this.IsAlive = (bool)stream.ReceiveNext();
            }
        }

        private void CheckHealth()
        {
            if (_health == 0)
                --Pages;
        }

        public void DisableChannelEffects()
        {
            innerChannelingParticleSystem.SetActive(false);
        }

        private IEnumerator Channel(PlayerController channeler)
        {
            hasBeenChanneledOnce = true;
            float progress = 0;
            const float MAX_CHANNELING_PROGRESS = 100;
            float secondsToChannel = SecondsToChannelPage;

            while (progress < MAX_CHANNELING_PROGRESS)
            {
                /* Assumption: Stopping is not the same as interrupting. */

                // Stop channeling effects if player is not channeling.
                // Could be due to external reasons like another player
                // interrupting the the channeling player.
                if (!channeler.IsChannelingObjective)
                {                    
                    DisableChannelEffects();
                    channeler.DisableChannelEffects();
                    channeler.DisableChannelEffectsNetworked(NetworkID);
                    yield break;
                }

                // Interrupt channeling effects if player is out of range with a base.
                if (Vector3.Distance(transform.position, channeler.Position) > PlayerValues.BaseChannelRange)
                {                    
                    DisableChannelEffects();
                    channeler.InterruptChanneling();
                    channeler.DisableChannelEffectsNetworked(NetworkID);
                    yield break;
                }

                progress += MAX_CHANNELING_PROGRESS / secondsToChannel;
                Debug.Log($"{Team} Base being channeled, {progress} / {MAX_CHANNELING_PROGRESS}");

                if(progress >= MAX_CHANNELING_PROGRESS)
                {
                    // Stop channeling effects after successfuly channeling a base
                    // It's not an interruption but rather a stop to the channeling effects
                    DisableChannelEffects();
                    channeler.DisableChannelEffects();
                    channeler.DisableChannelEffectsNetworked(NetworkID);

                    /* Start of page stuff */

                    if (channeler.Team != Team) // Different team
                    {
                        if (channeler.HasPage) // No page if already has a page
                        {
                            yield break;
                        }

                        if (Pages > 0)
                        {
                            --Pages;
                            channeler.OnChannelingFinishedAndPickUpPage(NetworkID, Pages);
                        }
                    }
                    else // Same team
                    {
                        if (channeler.HasPage) // Return page back
                        {
                            ++Pages;
                            channeler.OnChannelingFinishedAndDropPage(NetworkID, Pages);
                        }
                        else if (Pages > 0) // Take a page
                        {
                            --Pages;
                            channeler.OnChannelingFinishedAndPickUpPage(NetworkID, Pages);
                        }
                    }

                    /* End of page stuff */

                    yield break;
                }                

                yield return new WaitForSeconds(1);
            }
        }
        
        private IEnumerator Glow()
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetSharedMaterials(materials);
            Dictionary<Material, Color> normalColors = new Dictionary<Material, Color>();
            foreach (var material in materials)
            {
                normalColors[material] = Copy(material.color);
            }
            Color glowColor = Color.green;
            float minutesToGlow = 1;
            float step = 0.1f;
            int stepsCount = 6;
            float pause = 3f;
            float totalRepetitions = (minutesToGlow * 60) / pause;
            int localRepetitions = 2;
            while (!hasBeenChanneledOnce && totalRepetitions-- > 0)
            {
                for (int i = 0; i < localRepetitions; i++)
                {
                    for (int j = 0; j < stepsCount; j++)
                    {
                        foreach (var material in materials)
                        {
                            material.color = Color.Lerp(material.color, glowColor, step);
                            yield return new WaitForSeconds(0.01f);
                        }
                    }

                    for (int j = 0; j < stepsCount; j++)
                    {
                        foreach (var material in materials)
                        {
                            material.color = Color.Lerp(material.color, normalColors[material], step);
                            yield return new WaitForSeconds(0.01f);
                        }
                    }

                    foreach (var material in materials)
                    {
                        material.color = normalColors[material];
                    }
                }

                yield return new WaitForSeconds(pause);
            }
            foreach (var baseBehavior in FindObjectsOfType<BaseBehavior>())
            {
                baseBehavior.hasBeenChanneledOnce = true;
            }
        }

        private IEnumerator IsAttackedTimer()
        {
            Debug.Log("here");
            if (this.IsAttackedCoroutine == null && this.IsAttackedCoroutine.Equals(null))
            {
            Debug.Log("?????");
                AudioSource.enabled = true;
                AudioSource.Play();
            }
            
            yield return new WaitForSeconds(this.TimeUntilIsAttackedSoundIsPlayedAgainst);

            this.IsAttackedCoroutine = null;
        }

        private Color Copy(Color color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }
    }
}