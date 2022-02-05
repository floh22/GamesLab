using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LoadingScreenMovement : MonoBehaviour
{
    [SerializeField] private int moveDurationInFrames;
    [SerializeField] private int moveOffset;
    
    [SerializeField] private GameObject LeftMaskOne;
    [SerializeField] private GameObject LeftMaskTwo;

    [SerializeField] private GameObject RightMaskOne;
    [SerializeField] private GameObject RightMaskTwo;


    private Vector3 leftPosStart;
    private Vector3 leftPosEnd;
    private Vector3 rightPosStart;
    private Vector3 rightPosEnd;

    private Coroutine movementRoutine;
    
    // Start is called before the first frame update
    void Start()
    {
        float width = (Camera.main.scaledPixelWidth / 160) * 103;
        
        leftPosStart = LeftMaskOne.transform.position;
        leftPosEnd = leftPosStart + new Vector3(width, 0, 0);

        rightPosStart = RightMaskOne.transform.position;
        rightPosEnd = rightPosStart - new Vector3(width, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Close()
    {
        if(movementRoutine != null)
            StopCoroutine(movementRoutine);
        movementRoutine = StartCoroutine(Slide(true));
    }

    public void Open()
    {
        if(movementRoutine != null)
            StopCoroutine(movementRoutine);
        movementRoutine = StartCoroutine(Slide(false));
    }

    private IEnumerator Slide(bool close)
    {
        int max = moveDurationInFrames + moveOffset;

        Vector3 distLeft = ((close? leftPosEnd : leftPosStart) - LeftMaskOne.transform.position) / moveDurationInFrames;
        Vector3 distRight = ((close? rightPosEnd : rightPosStart) - RightMaskOne.transform.position) / moveDurationInFrames;

        //Really ugly way to differenciate this but I dont care
        if (close)
        {
            for (int i = 0; i < max; i++)
            {
                if (i < moveDurationInFrames)
                {
                    LeftMaskOne.transform.position += distLeft;
                    RightMaskOne.transform.position += distRight;
                }

                if (i >= moveOffset)
                {
                    LeftMaskTwo.transform.position += distLeft;
                    RightMaskTwo.transform.position += distRight;
                }
            
                yield return null;
            }
        }
        else
        {
            for (int i = 0; i < max; i++)
            {
                if (i < moveDurationInFrames)
                {
                    LeftMaskTwo.transform.position += distLeft;
                    RightMaskTwo.transform.position += distRight;
                }

                if (i >= moveOffset)
                {
                    LeftMaskOne.transform.position += distLeft;
                    RightMaskOne.transform.position += distRight;
                }
            
                yield return null;
            }
        }
        
    }
}
