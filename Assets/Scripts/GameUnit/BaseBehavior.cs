using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using Controls.Channeling;
using ExitGames.Client.Photon;
using GameManagement;
using Network;
using NetworkedPlayer;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace GameUnit
{
    public class BaseBehavior : MonoBehaviourPunCallbacks, IGameUnit
    {
        public int NetworkID { get; set; }
        public int OwnerID => GameStateController.Instance.Players[Team].OwnerID;
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

        // Start is called before the first frame update
        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            foreach (var material in meshRenderer.materials)
            {
                material.color = Color.white;
            }
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
            CurrentlyAttackedBy = new HashSet<IGameUnit>();
            StartCoroutine(Glow());
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

            channeler.OnChannelObjective(transform.position, NetworkID);
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

        public void OnStartChannel(PlayerController channeler)
        {
            // Enable channeling effects when channeling Slenderman on the channeler.
                // The channeling effect on Slenderman will be activated in the OnCollision.cs script
                // when the particles from the channeler hit Slenderman.
                channeler.transform.Find("InnerChannelingParticleSystem").gameObject.SetActive(true);
                ParticleSystem ps = channeler.transform.Find("InnerChannelingParticleSystem").gameObject.GetComponent<ParticleSystem>();

                // Set particles color
                float hSliderValueR = 0.0f;
                float hSliderValueG = 0.0f;
                float hSliderValueB = 0.0f;
                float hSliderValueA = 0.0f;


                if(Team.ToString() == "RED")
                {
                    // Set particles color
                    hSliderValueR = 174.0F / 255;
                    hSliderValueG = 6.0F / 255;
                    hSliderValueB = 6.0F / 255;
                    hSliderValueA = 255.0F / 255;
                }
                else if (Team.ToString() == "YELLOW")
                {
                    // Set particles color
                    hSliderValueR = 171.0F / 255;
                    hSliderValueG = 173.0F / 255;
                    hSliderValueB = 6.0F / 255;
                    hSliderValueA = 255.0F / 255;
                }
                else if (Team.ToString() == "GREEN")
                {
                    // Set particles color
                    hSliderValueR = 7.0F / 255;
                    hSliderValueG = 173.0F / 255;
                    hSliderValueB = 16.0F / 255;
                    hSliderValueA = 255.0F / 255;
                }
                else if (Team.ToString() == "BLUE")
                {
                    // Set particles color
                    hSliderValueR = 7.0F / 255;
                    hSliderValueG = 58.0F / 255;
                    hSliderValueB = 173.0F / 255;
                    hSliderValueA = 255.0F / 255;
                }

                ps.startColor = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);

                // Set the force that will change the particles direcion
                var fo = ps.forceOverLifetime;
                fo.enabled = true;

                fo.x = new ParticleSystem.MinMaxCurve(innerChannelingParticleSystem.transform.position.x - channeler.transform.position.x);
                fo.y = new ParticleSystem.MinMaxCurve(-innerChannelingParticleSystem.transform.position.y + channeler.transform.position.y);
                fo.z = new ParticleSystem.MinMaxCurve(innerChannelingParticleSystem.transform.position.z - channeler.transform.position.z);
        }

        private IEnumerator Channel(PlayerController channeler)
        {
            hasBeenChanneledOnce = true;
            float progress = 0;
            float maxProgress = 100;
            float secondsToChannel = SecondsToChannelPage;
            
            OnStartChannel(channeler);

            while (progress <= maxProgress)
            {
                if (!channeler.IsChannelingObjective ||
                    Vector3.Distance(transform.position, channeler.Position) > PlayerValues.SlendermanChannelRange)
                {
                    // Disable channeling effects if player moves
                    innerChannelingParticleSystem.SetActive(false);
                    channeler.InterruptChanneling();
                    yield break;
                }

                progress += maxProgress / secondsToChannel;
                Debug.Log($"{Team} Base being channeled, {progress} / {maxProgress}");
                yield return new WaitForSeconds(1);
            }

            if (!channeler.IsChannelingObjective)
            {
                yield break;
            }

            if (Vector3.Distance(transform.position, channeler.Position) > PlayerValues.SlendermanChannelRange)
            {
                // Disable channeling effects if player moves
                innerChannelingParticleSystem.SetActive(false);
                channeler.InterruptChanneling();
                yield break;
            }

            if (channeler.Team != Team) // Different team
            {
                if (channeler.HasPage) // No page if already has a page
                {
                    innerChannelingParticleSystem.SetActive(false);
                    channeler.InterruptChanneling();
                    yield break;
                }

                if (Pages > 0)
                {
                    --Pages;
                    channeler.OnChannelingFinishedAndPickUpPage(NetworkID, Pages);
                    innerChannelingParticleSystem.SetActive(false);
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
                    innerChannelingParticleSystem.SetActive(false);
                }
            }

            innerChannelingParticleSystem.SetActive(false);
            channeler.InterruptChanneling();
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
            int stepsCount = 10;
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

        private Color Copy(Color color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }
    }
}