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

        private IEnumerator Channel(PlayerController channeler)
        {
            hasBeenChanneledOnce = true;
            float progress = 0;
            float maxProgress = 100;
            float secondsToChannel = PlayerValues.SecondsToChannelSlenderman;
            while (progress < maxProgress)
            {
                if (!channeler.IsChannelingObjective ||
                    Vector3.Distance(transform.position, channeler.Position) > PlayerValues.SlendermanChannelRange)
                {
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
                channeler.InterruptChanneling();
                yield break;
            }
            
            channeler.SacrifisePage();
            channeler.OnChannelingFinishedAndReceiveSlendermanBuff(NetworkID);
            OnChanneled();
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