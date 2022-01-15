using System.Collections;
using System.Collections.Generic;
using Controls;
using GameManagement;
using Network;
using NetworkedPlayer;
using UnityEngine;
using UnityEngine.UI;

public class Abilities : MonoBehaviour
{
    // Start is called before the first frame update
    public float maxAbilityDistance1 = 5;
    public float maxAbilityDistance2 = 7;
    public float cooldownAbility1 = 5;
    public float cooldownAbility2 = 7;
    public bool isCooldown1 = false;
    public bool isCooldown2 = false;

    public Image ability1Image;
    public Image ability2Image;
    public Image targetCircle;
    public Image rangeIndicatorCircle1;
    public Image rangeIndicatorCircle2;
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
        arrowIndicatorPivot.GetComponentInChildren<Image>().enabled = false;
        rangeIndicatorCircle1.GetComponent<Image>().enabled = false;
        rangeIndicatorCircle2.GetComponent<Image>().enabled = false;


        ability1Image = GameObject.FindWithTag("Ability1Handle").GetComponent<Image>();
        ability2Image = GameObject.FindWithTag("Ability2Handle").GetComponent<Image>();
        ability1Image.fillAmount = 1;
        ability2Image.fillAmount = 1;

        GameObject.FindWithTag("Ability1Handle").GetComponent<AbilityJoystick>().RefreshReferences();
        GameObject.FindWithTag("Ability2Handle").GetComponent<AbilityJoystick>().RefreshReferences();
    }

    void Update()
    {
        if (isCooldown1)
        {
            ability1Image.fillAmount += 1 / cooldownAbility1 * Time.deltaTime;
            if (ability1Image.fillAmount >= 1)
            {
                ability1Image.fillAmount = 1;
                isCooldown1 = false;
            }
        }

        if (isCooldown2)
        {
            ability2Image.fillAmount += 1 / cooldownAbility2 * Time.deltaTime;
            if (ability2Image.fillAmount >= 1)
            {
                ability2Image.fillAmount = 1;
                isCooldown2 = false;
            }
        }
    }

    public void move(Ability ability)
    {
        Joystick joystick = GameObject.FindWithTag("Ability" + (int) ability).GetComponent<Joystick>();

        switch (ability)
        {
            case Ability.RANGE:
                targetCircle.transform.localPosition = new Vector3(-joystick.Vertical * maxAbilityDistance1,
                    joystick.Horizontal * maxAbilityDistance1, 0);
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
        switch (ability)
        {
            case Ability.RANGE:
                rangeIndicatorCircle1.GetComponent<Image>().enabled = true;
                targetCircle.GetComponent<Image>().enabled = true;
                break;
            case Ability.LINE:
                rangeIndicatorCircle2.GetComponent<Image>().enabled = true;
                arrowIndicatorPivot.GetComponentInChildren<Image>().enabled = true;
                break;
        }
    }

    public void HideAbilityInterface(Ability ability)
    {
        switch (ability)
        {
            case Ability.RANGE:
                rangeIndicatorCircle1.GetComponent<Image>().enabled = false;
                targetCircle.GetComponent<Image>().enabled = false;
                break;
            case Ability.LINE:
                rangeIndicatorCircle2.GetComponent<Image>().enabled = false;
                arrowIndicatorPivot.GetComponentInChildren<Image>().enabled = false;
                break;
        }
    }

    public void CastAbility(Ability ability, Vector3 lastPosition)
    {
        Vector3 startingPosition;

        switch (ability)
        {
            case Ability.RANGE:
                if (isCooldown1)
                {
                    return;
                }

                startingPosition = transform.position + new Vector3(0, 2, 0);

                GameObject ability1ActiveObject = Instantiate(ability1ProjectilePrefab, startingPosition,
                    Quaternion.identity) as GameObject;
                ability1ActiveObject.GetComponent<AbilityProjectile1>().Activate(targetCircle.transform.position,
                    gameObject.GetComponent<PlayerController>(),
                    gameObject.GetComponent<PlayerController>().DamageMultiplierAbility1);
                isCooldown1 = true;
                ability1Image.fillAmount = 0;
                break;

            case Ability.LINE:
                if (isCooldown2)
                {
                    return;
                }

                Vector3 direction = new Vector3(lastPosition.x, 0, lastPosition.y);
                direction *= maxAbilityDistance2;

                float angle = Vector3.Angle(direction, new Vector3(1, 0, 0));
                if (direction.z <= 0)
                {
                    angle *= -1;
                }

                direction = Quaternion.Euler(0, angle, 0) * new Vector3(0, 1, maxAbilityDistance2);
                startingPosition = transform.position + new Vector3(direction.x * 0.05f, 2, direction.z * 0.05f);
                direction = transform.TransformPoint(direction);


                GameObject ability2ActiveObject = Instantiate(ability2ProjectilePrefab, startingPosition,
                    Quaternion.Euler(0, angle, 0));
                ability2ActiveObject.transform.LookAt(direction);
                ability2ActiveObject.GetComponent<AbilityProjectile2>()
                    .Activate(direction, gameObject.GetComponent<PlayerController>(),
                        0f); //Damage Multiplier not needed here but only below
                ability2ActiveObject.GetComponent<DamageObject>()
                    .Activate(gameObject.GetComponent<PlayerController>(), false,
                        gameObject.GetComponent<PlayerController>().DamageMultiplierAbility2);

                isCooldown2 = true;
                ability2Image.fillAmount = 0;
                break;
        }

        HideAbilityInterface(ability);
    }

    public void DontCastAbility(Ability ability)
    {
        HideAbilityInterface(ability);
    }

    public bool isCooldown(Ability ability)
    {
        return ability == Ability.RANGE ? isCooldown1 : isCooldown2;
    }
}