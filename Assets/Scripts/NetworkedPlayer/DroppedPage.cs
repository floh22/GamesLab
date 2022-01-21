using System;
using System.Collections;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

namespace NetworkedPlayer
{
    public class DroppedPage: MonoBehaviour
    {

        #region Public Fields

        public int secondsAlive = 30;
        public int NetworkID { get; set; }

        #endregion
        
        #region Private Fields

        private RectTransform rectTransform;
        private MeshRenderer meshRenderer;
        private Coroutine rotation;
        private Coroutine aliveTimer;

        #endregion

        #region Public API

        public void TurnOn()
        {
            meshRenderer.enabled = true;
            rotation = StartCoroutine(Rotate());
        }
        

        public void TurnOff()
        {
            StopCoroutine(aliveTimer);
            StopCoroutine(rotation);
            rotation = null;
            aliveTimer = null;
            meshRenderer.enabled = false;
            PhotonNetwork.Destroy(this.gameObject);
        }

        #endregion

        #region Unity API

        void Start()
        {
            NetworkID = gameObject.GetInstanceID();
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            TurnOn();
            aliveTimer = StartCoroutine(DespawnAfterTimeAlive());
        }
        
        void OnTriggerEnter(Collider collider)
        {
            GameObject colliderObject = collider.gameObject;
            if (colliderObject.tag == "Player")
            {
                PlayerController player = colliderObject.GetComponent<PlayerController>();
                if (!player.HasPage)
                {
                    player.PickUpPage();
                    TurnOff();
                }
            }
        }

        #endregion

        #region Coroutines

        IEnumerator DespawnAfterTimeAlive()
        {
            yield return new WaitForSeconds(secondsAlive);
            TurnOff();
        }

        IEnumerator Rotate()
        {
            while (true)
            {
                rectTransform.Rotate(0f, 3f, 0f);
                yield return new WaitForSeconds(0.01f);
            }
        }
        
        #endregion 




    }
}