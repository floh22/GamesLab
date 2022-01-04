using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Abilities : MonoBehaviour
{
    // Start is called before the first frame update
    public Canvas abilityCanvas;
    public Image arrowIndicator;
    public Image targetCircle;
    public Image rangeIndicatorCircle;
    public float maxAbilityDistance = 5;
    public GameObject Character;
    public GameObject arrowIndicatorPivot;
    public GameObject ability1ProjectilePrefab;
    public GameObject ability2ProjectilePrefab;

    public enum Ability
    {
        NORMAL = 1,
        RANGE = 2,
        LINE = 3,
    }

    void Start()
    {
        targetCircle.GetComponent<Image>().enabled = false;
        arrowIndicator.GetComponent<Image>().enabled = false;
        rangeIndicatorCircle.GetComponent<Image>().enabled = false;
    }

    public void move(Ability ability)
    {
        Joystick joystick = GameObject.FindWithTag("Ability" + (int) ability).GetComponent<Joystick>();

        switch (ability)
        {
            case Ability.RANGE:
                targetCircle.transform.localPosition = new Vector3(-joystick.Vertical * maxAbilityDistance,
                    joystick.Horizontal * maxAbilityDistance, 0);
                break;
            case Ability.LINE:
                Vector3 direction = new Vector3(joystick.Vertical, 0, joystick.Horizontal);
                Quaternion transRot = Quaternion.LookRotation(direction);
                transRot.eulerAngles = new Vector3(0, 0, transRot.eulerAngles.y);
                arrowIndicatorPivot.transform.localRotation =
                    Quaternion.Lerp(transRot, arrowIndicatorPivot.transform.localRotation, 0f);
                break;
        }
    }

    public void ShowAbilityInterface(Ability ability)
    {
        rangeIndicatorCircle.GetComponent<Image>().enabled = true;
        switch (ability)
        {
            case Ability.RANGE:
                targetCircle.GetComponent<Image>().enabled = true;
                break;
            case Ability.LINE:
                arrowIndicator.GetComponent<Image>().enabled = true;
                break;
        }
    }

    public void HideAbilityInterface(Ability ability)
    {
        rangeIndicatorCircle.GetComponent<Image>().enabled = false;
        switch (ability)
        {
            case Ability.RANGE:
                targetCircle.GetComponent<Image>().enabled = false;
                break;
            case Ability.LINE:
                arrowIndicator.GetComponent<Image>().enabled = false;
                break;
        }
    }

    public void CastAbility(Ability ability, Vector3 lastPosition)
    {
        Vector3 startingPosition = transform.position + new Vector3(0, 2f, 0);

        switch (ability)
        {
            case Ability.RANGE:
                GameObject ability1ActiveObject = Instantiate(ability1ProjectilePrefab, startingPosition,
                    Quaternion.identity) as GameObject;
                ability1ActiveObject.GetComponent<AbilityProjectile1>().Activate(targetCircle.transform.position);
                break;

            case Ability.LINE:
                Vector3 direction = new Vector3(lastPosition.x, 0, lastPosition.y);
                Debug.Log("----------------");
                Debug.Log(direction);
                Quaternion transRot = Quaternion.LookRotation(direction);
                // Debug.Log(transRot);
                transRot.eulerAngles = new Vector3(0, 0, transRot.eulerAngles.y);
                // Debug.Log(transRot);
                // Debug.Log(Vector3.forward);
                // Debug.Log(transRot * Vector3.forward);
                // Debug.Log(".......");
                // Debug.Log(transRot.eulerAngles);

                GameObject ability2ActiveObject = Instantiate(ability2ProjectilePrefab, startingPosition,
                    transRot) as GameObject;
                ability2ActiveObject.GetComponent<AbilityProjectile2>().Activate(transRot * Vector3.forward);

                break;
        }

        HideAbilityInterface(ability);
    }

    public void DontCastAbility(Ability ability)
    {
        Debug.Log("DontCast!!!!!");
        HideAbilityInterface(ability);
    }
}