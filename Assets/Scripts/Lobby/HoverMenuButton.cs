using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lobby
{
    public class HoverMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        public GameObject foreground;

        public GameObject background;


        public Vector3 hoverOffsetForeground;
        public Vector3 hoverOffsetBackground;
        public Vector3 clickOffsetForeground;
        public Vector3 clickOffsetBackground;

        public int hoverMoveDurationInFrames;
        public int clickMoveDurationInFrames;

        private bool clickable = true;

        private Coroutine moveRoutine;
        private readonly Dictionary<GameObject, Vector3> startingPositions = new();
        private readonly Dictionary<GameObject, Vector3> hoverPositions = new();
        private readonly Dictionary<GameObject, Vector3> clickPositions = new();

        void Start()
        {
            int clickOffset = Camera.main.scaledPixelWidth / 2;
            clickOffsetBackground = new Vector3(clickOffset, 0, 0);
            clickOffsetForeground = new Vector3(clickOffset, 0, 0);
            Vector3 foregroundT = foreground.transform.position;
            Vector3 backgroundT = background.transform.position;
            startingPositions.Add(foreground, foregroundT);
            hoverPositions.Add(foreground, foregroundT + hoverOffsetForeground);
            clickPositions.Add(foreground, foregroundT + clickOffsetForeground);
        
            startingPositions.Add(background, backgroundT);
            hoverPositions.Add(background, backgroundT + hoverOffsetBackground);
            clickPositions.Add(background, backgroundT + clickOffsetBackground);


            foreground.transform.position = foregroundT + clickOffsetForeground;
            background.transform.position = backgroundT + clickOffsetBackground;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!clickable)
                return;
            if(moveRoutine != null)
                StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(Hover(true));
        }
 
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!clickable)
                return;
            if(moveRoutine != null)
                StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(Hover(false));
        }


        private IEnumerator Hover(bool isHover)
        {
            Vector3 distForeground = ((isHover? hoverPositions[foreground] : startingPositions[foreground]) - foreground.transform.position) / hoverMoveDurationInFrames;
            Vector3 distBackground = ((isHover ? hoverPositions[background] : startingPositions[background]) - background.transform.position) / hoverMoveDurationInFrames;

            for (int i = 0; i < hoverMoveDurationInFrames; i++)
            {
                foreground.transform.position += distForeground;
                background.transform.position += distBackground;
                yield return null;
            }
        }

        private IEnumerator ClickRoutine(bool isClick)
        {
            Vector3 distForeground = ((isClick? clickPositions[foreground] : startingPositions[foreground]) - foreground.transform.position) / clickMoveDurationInFrames;
            Vector3 distBackground = ((isClick ? clickPositions[background] : startingPositions[background]) - background.transform.position) / clickMoveDurationInFrames;

            int max = clickMoveDurationInFrames + 3;
            for (int i = 0; i < max; i++)
            {
                if(i < clickMoveDurationInFrames)
                    foreground.transform.position += distForeground;
                if(i >= 3)
                    background.transform.position += distBackground;
                yield return null;
            }
        }


        public void Click()
        {
            clickable = false;
            if(moveRoutine != null)
                StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(ClickRoutine(true));
        }
        

        public void Show()
        {
            clickable = true;
            if(moveRoutine != null)
                StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(ClickRoutine(false));
        }
    }
}
