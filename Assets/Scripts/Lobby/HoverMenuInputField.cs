using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Lobby
{
    public class HoverMenuInputField : HoverMenuButton
    {
        public TMP_Text warningSymbol;

        private bool showWarning;

        public bool ShowWarning
        {
            get => showWarning;
            set
            {
                if (value == showWarning)
                    return;
                showWarning = value;

                if (!showWarning) return;
                
                if(warningRoutine != null)
                    StopCoroutine(warningRoutine);
                warningRoutine = StartCoroutine(FlashWarning());
            }
        }

        public float flashSpeed;

        private Coroutine warningRoutine;

        private IEnumerator FlashWarning()
        {
            float startTime = Time.time;
            while (showWarning || warningSymbol.alpha > 0.1)
            {
                warningSymbol.alpha = (Mathf.Sin((Time.time - startTime) * flashSpeed) + 1) / 2;
                yield return null;
            }

            warningSymbol.alpha = 0;
        }
    }
}
