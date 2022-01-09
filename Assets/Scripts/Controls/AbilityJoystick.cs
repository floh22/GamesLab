using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AbilityJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Abilities.Ability currentAbility;
    public float deadZone = 0.2f;

    private GameObject _character;
    private Image _abilityJoystickImage;
    private Joystick _abilityJoystick;
    private bool _isInDeadZone = true;
    private Vector3 _lastPosition;


    void Start()
    {
        _character = GameObject.FindWithTag("Player");
        _abilityJoystick = GameObject.FindWithTag("Ability" + (int) this.currentAbility).GetComponent<Joystick>();
        _abilityJoystickImage = GameObject.FindWithTag("Ability" + (int) this.currentAbility).GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_character.GetComponent<Abilities>().isCooldown(currentAbility))
        {
            GetComponentInParent<FixedJoystick>().SendMessage("OnDrag", eventData);
            _character.GetComponent<Abilities>().move(this.currentAbility);
            _lastPosition = new Vector3(
                _abilityJoystick.Vertical,
                _abilityJoystick.Horizontal, 0);
            if (InDeadZone() && !_isInDeadZone)
            {
                _isInDeadZone = true;
                this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 0.6f);
            }
            else if (!InDeadZone() && _isInDeadZone)
            {
                _isInDeadZone = false;
                this.gameObject.GetComponent<Image>().color = new Color(0, 0.1104961f, 0.9716981f, 0.6f);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_character.GetComponent<Abilities>().isCooldown(currentAbility))
        {
            GetComponentInParent<FixedJoystick>().SendMessage("OnPointerDown", eventData);
            _character.GetComponent<Abilities>().ShowAbilityInterface(this.currentAbility);
            _abilityJoystickImage.enabled = true;
            _character.GetComponent<Abilities>().move(this.currentAbility);
            this.gameObject.GetComponent<Image>().color = new Color(0.490566f, 0, 0, 0.6f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GetComponentInParent<FixedJoystick>().SendMessage("OnPointerUp", eventData);

        if (InDeadZone())
        {
            _character.GetComponent<Abilities>().DontCastAbility(this.currentAbility);
        }
        else
        {
            _character.GetComponent<Abilities>().CastAbility(this.currentAbility, _lastPosition);
        }

        _isInDeadZone = true;
        this.gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        _abilityJoystickImage.enabled = false;
    }

    private bool InDeadZone()
    {
        float verticalJoystick = _lastPosition.x;
        float horizontalJoystick = _lastPosition.y;

        return verticalJoystick < deadZone && verticalJoystick > -deadZone && horizontalJoystick < deadZone &&
               horizontalJoystick > -deadZone;
    }
}