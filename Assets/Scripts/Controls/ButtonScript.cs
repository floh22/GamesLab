using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public GameObject Character;
    public float deadZone = 0.2f;
    private bool IsInDeadZone = true;
    public Abilities.Ability CurrentAbility;

    private Vector3 lastPosition;

    public void OnDrag(PointerEventData eventData)
    {
        GetComponentInParent<FixedJoystick>().SendMessage("OnDrag", eventData);
        Character.GetComponent<Abilities>().move(this.CurrentAbility);
        lastPosition = new Vector3(
            GameObject.FindWithTag("Ability" + (int) this.CurrentAbility).GetComponent<Joystick>().Vertical,
            GameObject.FindWithTag("Ability" + (int) this.CurrentAbility).GetComponent<Joystick>().Horizontal, 0);
        if (InDeadZone() && !IsInDeadZone)
        {
            IsInDeadZone = true;
            this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 0.6f);
        }
        else if (!InDeadZone() && IsInDeadZone)
        {
            IsInDeadZone = false;
            this.gameObject.GetComponent<Image>().color = new Color(0, 0.1104961f, 0.9716981f, 0.6f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        GetComponentInParent<FixedJoystick>().SendMessage("OnPointerDown", eventData);
        Character.GetComponent<Abilities>().ShowAbilityInterface(this.CurrentAbility);
        GameObject.FindWithTag("Ability" + (int) this.CurrentAbility).GetComponent<Image>().enabled = true;
        Character.GetComponent<Abilities>().move(this.CurrentAbility);
        this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 0.6f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GetComponentInParent<FixedJoystick>().SendMessage("OnPointerUp", eventData);

        if (InDeadZone())
        {
            Character.GetComponent<Abilities>().DontCastAbility(this.CurrentAbility);
        }
        else
        {
            Character.GetComponent<Abilities>().CastAbility(this.CurrentAbility, lastPosition);
        }

        IsInDeadZone = true;
        this.gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        GameObject.FindWithTag("Ability" + (int) this.CurrentAbility).GetComponent<Image>().enabled = false;
    }

    private bool InDeadZone()
    {
        float verticalJoystick = lastPosition.x;
        float horizontalJoystick = lastPosition.y;

        return verticalJoystick < deadZone && verticalJoystick > -deadZone && horizontalJoystick < deadZone &&
               horizontalJoystick > -deadZone;
    }
}