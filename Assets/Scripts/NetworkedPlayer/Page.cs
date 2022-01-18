using System;
using System.Collections;
using UnityEngine;

namespace NetworkedPlayer
{
    public class Page: MonoBehaviour
    {

        #region Private Fields

        private RectTransform rectTransform;
        private MeshRenderer meshRenderer;
        private bool isActive;
        private Coroutine rotation;

        #endregion

        #region Public API

        public bool IsActive => isActive;

        public void TurnOn()
        {
            if (isActive)
            {
                return;
            }
            
            isActive = true;
            meshRenderer.enabled = true;
            rotation = StartCoroutine(Rotate());
        }

        public void TurnOff()
        {
            if (!isActive)
            {
                return;
            }

            StopCoroutine(rotation);
            rotation = null;
            isActive = false;
            meshRenderer.enabled = false;
        }

        #endregion

        #region Unity API

        private void Start()
        {
            isActive = false;
            rectTransform = GetComponent<RectTransform>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
        }

        #endregion

        #region Coroutines

        private IEnumerator Rotate()
        {
            while (isActive)
            {
                rectTransform.Rotate(0f, 3f, 0f);
                yield return new WaitForSeconds(0.01f);
            }
        }

        #endregion

    }
}