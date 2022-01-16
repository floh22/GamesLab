using System;
using UnityEngine;
using NetworkedPlayer;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Abilities.Ability currentAbility;
    public float deadZone = 0.2f;

    [SerializeField] private Image _cirularMeterImage;
    private Abilities _abilities;
    private Image _abilityJoystickImage;
    private Joystick _abilityJoystick;
    private bool _isInDeadZone = true;
    private Vector3 _lastPosition;


    void Start()
    {
        if (PlayerController.LocalPlayerInstance == null || PlayerController.LocalPlayerInstance.Equals(null))
            return;
        _abilities = PlayerController.LocalPlayerInstance.GetComponent<Abilities>();
        _abilityJoystick = GameObject.Find("Ability_Joystick_" + (int) currentAbility).GetComponent<Joystick>();
        _abilityJoystickImage = GameObject.Find("Ability_Joystick_" + (int) currentAbility).GetComponent<Image>();
        _cirularMeterImage = GameObject.Find("Circular_Meter_" + ((int) currentAbility - 1)).GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (PlayerController.LocalPlayerInstance == null || PlayerController.LocalPlayerInstance.Equals(null))
        {
            RefreshReferences();
        }
            
        if (!_abilities.isCooldown(currentAbility))
        {
            GetComponentInParent<FixedJoystick>().SendMessage("OnDrag", eventData);
            _abilities.move(this.currentAbility);
            _lastPosition = new Vector3(
                _abilityJoystick.Vertical,
                _abilityJoystick.Horizontal, 0);
            if (InDeadZone() && !_isInDeadZone)
            {
                _isInDeadZone = true;
                this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 1f);
            }
            else if (!InDeadZone() && _isInDeadZone)
            {
                _isInDeadZone = false;
                this.gameObject.GetComponent<Image>().color = new Color(0, 0.1104961f, 0.9716981f, 1f);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (PlayerController.LocalPlayerInstance == null || PlayerController.LocalPlayerInstance.Equals(null))
        {
            RefreshReferences();
        }
        
        if (!_abilities.isCooldown(currentAbility))
        {
            GetComponentInParent<FixedJoystick>().SendMessage("OnPointerDown", eventData);
            _abilities.ShowAbilityInterface(this.currentAbility);
            _abilityJoystickImage.enabled = true;
            _cirularMeterImage.enabled = false;
            _abilities.move(this.currentAbility);
            this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 1f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GetComponentInParent<FixedJoystick>().SendMessage("OnPointerUp", eventData);

        if (InDeadZone())
        {
            _abilities.DontCastAbility(this.currentAbility);
        }
        else
        {
            _abilities.CastAbility(this.currentAbility, _lastPosition);
        }

        _isInDeadZone = true;
        this.gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        _abilityJoystickImage.enabled = false;
        _cirularMeterImage.enabled = true;
    }

    public void RefreshReferences()
    {
        _abilities = PlayerController.LocalPlayerInstance.GetComponent<Abilities>();
        _abilityJoystick = GameObject.Find("Ability_Joystick_" + (int) currentAbility).GetComponent<Joystick>();
        _abilityJoystickImage = GameObject.Find("Ability_Joystick_" + (int) currentAbility).GetComponent<Image>();
        _cirularMeterImage = GameObject.Find("Circular_Meter_" + ((int) currentAbility - 1)).GetComponent<Image>();
    }

    private bool InDeadZone()
    {
        float verticalJoystick = _lastPosition.x;
        float horizontalJoystick = _lastPosition.y;

        return verticalJoystick < deadZone && verticalJoystick > -deadZone && horizontalJoystick < deadZone &&
               horizontalJoystick > -deadZone;
    }
}