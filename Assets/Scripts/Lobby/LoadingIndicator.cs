using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace Lobby
{
    public class LoadingIndicator : MonoBehaviour
    {
        private bool loadingFinished;
        public bool LoadingFinished
        {
            get => loadingFinished;
            set
            {
                if (value)
                {
                    loadingImage.color = FinishedColor;
                    loadingImage.transform.localScale = startSize;
                }
                loadingFinished = value;
            }
        }

        public string PlayerName
        {
            get => playerNameText.text;
            set => playerNameText.text = value;
        }

        public bool HasPlayer;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image loadingImage;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private  AnimationCurve loadingCurve;
        [SerializeField] private  float loadingExpandAmount;
        [SerializeField] private  float loadingExpandSpeed;
        [SerializeField] private  Color LoadingColor;
        [SerializeField] private  Color FinishedColor;
    
        private float scrollAmount;
        private Vector3 startSize;
        private Vector3 targetSize;

        private Coroutine transitionRoutine;
        // Start is called before the first frame update
        void Start()
        {
            startSize = loadingImage.transform.localScale;
            targetSize = startSize * loadingExpandAmount;
            loadingImage.color = LoadingColor;
        
        
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g,
                backgroundImage.color.b, 0);
            
            loadingImage.color = new Color(loadingImage.color.r, loadingImage.color.g,
                loadingImage.color.b, 0);
            
            playerNameText.color = new Color(playerNameText.color.r, playerNameText.color.g,
                playerNameText.color.b, 0);
        }

        // Update is called once per frame
        void Update()
        {
            return;
            if (!loadingFinished)
            {
                Pulse();
            }
        }

        private void Pulse()
        {
            scrollAmount += Time.deltaTime * loadingExpandSpeed;

            float percent = loadingCurve.Evaluate(scrollAmount);

            loadingImage.transform.localScale = Vector3.Lerp(startSize, targetSize, percent);
        }


        public void Show()
        {
            if(transitionRoutine != null)
                StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(SwitchVisibility(true));
        }

        public void Hide()
        {
            if(transitionRoutine != null)
                StopCoroutine(transitionRoutine);
            StartCoroutine(SwitchVisibility(false));
        }


        private IEnumerator SwitchVisibility(bool visibility)
        {
            //Wait for background to close
            if (visibility)
                yield return new WaitForSeconds(1f);
        
        
        
            bool isRunning = true;
            int vis = (visibility ? 1: -1);
            while (isRunning)
            {
                float visibilityToAdd = 0.1f * vis;

                backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g,
                    backgroundImage.color.b, Mathf.Clamp(backgroundImage.color.a + visibilityToAdd, 0, 1));
            
                loadingImage.color = new Color(loadingImage.color.r, loadingImage.color.g,
                    loadingImage.color.b, Mathf.Clamp(loadingImage.color.a + visibilityToAdd, 0, 1));
            
                playerNameText.color = new Color(playerNameText.color.r, playerNameText.color.g,
                    playerNameText.color.b, Mathf.Clamp(playerNameText.color.a + visibilityToAdd, 0, 1));


                isRunning = backgroundImage.color.a is not (0 or 1);

                yield return null;
            }

            if (!visibility)
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}
