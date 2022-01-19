using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using NetworkedPlayer;
using Photon.Pun;
using UnityEngine;

namespace Controls.Channeling
{
    public class Slenderman : MonoBehaviourPunCallbacks, IPunObservable
    {
        public GameObject innerChannelingParticleSystem;

        #region Private Fields

        private const float RecoveryTime = 10f;
        
        private bool hasBeenAcquired;

        private bool isVisible;

        private bool hasBeenChanneledOnce;

        private SkinnedMeshRenderer skinnedMeshRenderer;

        #endregion

        #region Public Fields

        public int NetworkID { get; set; }

        #endregion

        public void Start()
        {
            GameObject o = gameObject;
            NetworkID = o.GetInstanceID();
            hasBeenAcquired = false;
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            foreach (var material in skinnedMeshRenderer.materials)
            {
                switch (material.name)
                {
                    case "Body":
                        material.color = Color.white;
                        break;
                    case "Shoes":
                        material.color = Color.black;
                        break;
                    case "Suit":
                        material.color = Color.black;
                        break;
                }
            }
            isVisible = true;
            StartCoroutine(Glow());
        }

        public void Update()
        {
            if (isVisible == !hasBeenAcquired)
            {
                return;
            }

            if (hasBeenAcquired)
            {
                skinnedMeshRenderer.enabled = false;
                isVisible = false;
            }
            else
            {
                skinnedMeshRenderer.enabled = true;
                isVisible = true;
            }
        }
        
        public void OnMouseDown()
        {
            if (hasBeenAcquired)
            {
                return;
            }

            PlayerController channeler = PlayerController.LocalPlayerController;

            if (!channeler.HasPage)
            {
                return;
            }

            Debug.Log("Slenderman has been clicked by player from team " + channeler.Team);
            
            if (Vector3.Distance(transform.position, channeler.Position) > PlayerValues.SlendermanChannelRange)
            {
                return;
            }

            if (channeler.IsChannelingObjective)
            {
                return;
            }

            channeler.OnChannelObjective(transform.position, NetworkID);
            StartCoroutine(Channel(channeler));
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local player, send data
                stream.SendNext(this.NetworkID);
                stream.SendNext(this.hasBeenAcquired);
            }
            else
            {
                // Network player, receive data
                this.NetworkID = (int)stream.ReceiveNext();
                this.hasBeenAcquired = (bool)stream.ReceiveNext();
            }
        }

        public void OnStartChannel(PlayerController channeler)
        {
            // Enable channeling effects when channeling Slenderman on the channeler.
            // The channeling effect on Slenderman will be activated in the OnCollision.cs script
            // when the particles from the channeler hit Slenderman.
            
            
            
            //Set this in the player themselves
            //channeler.ChannelParticleSystem.SetActive(true);
            ParticleSystem ps = channeler.ChannelParticleSystem.GetComponent<ParticleSystem>();


            // Set particles color
            // float hSliderValueR = 209.0F / 255;
            // float hSliderValueG = 25.0F / 255;
            // float hSliderValueB = 191.0F / 255;
            // float hSliderValueA = 255.0F / 255;      

            float hSliderValueR = 255.0F / 255;
            float hSliderValueG = 0.0F / 255;
            float hSliderValueB = 221.0F / 255;
            float hSliderValueA = 255.0F / 255;   

            Color color = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);

            ps.startColor = color;

            /* Start of Rings Channeling Effect Stuff */
            channeler.RingsParticleSystem.SetActive(true);

            ParticleSystem ringsParticleSystem = channeler.RingsParticleSystem.GetComponent<ParticleSystem>();
            ringsParticleSystem.Play(true);       

            ParticleSystem embers = channeler.RingsParticleSystem.transform.Find("Embers").gameObject.GetComponent<ParticleSystem>();
            ParticleSystem smoke = channeler.RingsParticleSystem.transform.Find("Smoke").gameObject.GetComponent<ParticleSystem>();
            embers.startColor = color;
            smoke.startColor = color;
            
            Gradient gradient;
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;
            gradient = new Gradient();
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = color;
            colorKey[0].time = 0.0f;
            colorKey[1].color = color;
            colorKey[1].time = 1.0f;
            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 0.0f;
            alphaKey[1].time = 1.0f;
            gradient.SetKeys(colorKey, alphaKey);       

            ParticleSystem.MainModule ringsParticleSystemMain = ringsParticleSystem.main;
            ringsParticleSystemMain.startColor = new ParticleSystem.MinMaxGradient(gradient);
            /* End of Rings Channeling Effect Stuff */            

            //var main = ps.main;
            //main.startColor = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);

            // Set the force that will change the particles direcion
            var fo = ps.forceOverLifetime;
            fo.enabled = true;

            fo.x = new ParticleSystem.MinMaxCurve(transform.position.x - channeler.transform.position.x);
            fo.y = new ParticleSystem.MinMaxCurve(-transform.position.y + channeler.transform.position.y + (GetComponent<BoxCollider>().bounds.size.y / 2));
            fo.z = new ParticleSystem.MinMaxCurve(transform.position.z - channeler.transform.position.z);
        }

        private IEnumerator Channel(PlayerController channeler)
        {
            hasBeenChanneledOnce = true;
            float progress = 0;
            float maxProgress = 100;
            float secondsToChannel = PlayerValues.SecondsToChannelSlenderman;
            
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
                Debug.Log($"Slenderman being channeled, {progress} / {maxProgress}");
                yield return new WaitForSeconds(1);
            }

            if (!channeler.IsChannelingObjective)
            {
                yield break;
            }

            if (hasBeenAcquired || Vector3.Distance(transform.position, channeler.Position) > PlayerValues.SlendermanChannelRange)
            {
                // Disable channeling effects if player moves
                innerChannelingParticleSystem.SetActive(false);
                channeler.InterruptChanneling();                
                yield break;
            }
            
            channeler.SacrifisePage();

            // Disable channeling effects after hiring Slenderman
            innerChannelingParticleSystem.SetActive(false);
            channeler.transform.Find("InnerChannelingParticleSystem").gameObject.SetActive(false);
            channeler.transform.Find("Rings").gameObject.SetActive(false);

            channeler.OnChannelingFinishedAndReceiveSlendermanBuff(NetworkID);
            OnChanneled();
        }

        public void DisableChannelEffects()
        {
            innerChannelingParticleSystem.SetActive(false);
        }


        public void OnChanneled()
        {
            hasBeenAcquired = true;
            StartCoroutine(Recover());
        }

        private IEnumerator Recover()
        {
            Debug.Log($"Slenderman recovering");           
            yield return new WaitForSeconds(RecoveryTime);
            hasBeenAcquired = false;
            Debug.Log($"Slenderman has recovered");
        }

        private IEnumerator Glow()
        {
            List<Material> materials = new List<Material>();
            skinnedMeshRenderer.GetSharedMaterials(materials);
            Dictionary<Material, Color> normalColors = new Dictionary<Material, Color>();
            foreach (var material in materials)
            {
                normalColors[material] = Copy(material.color);
            }
            Color glowColor = Color.yellow;
            float minutesToGlow = 1;
            float step = 0.1f;
            int stepsCount = 4;
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
        }

        private Color Copy(Color color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }

    }
}