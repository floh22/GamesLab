using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using ExitGames.Client.Photon;
using Network;
using NetworkedPlayer;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Controls.Abilities
{
    public class Abilities : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static Abilities Instance;
        
        private const byte CastAbilityEventCode = 2;

        [SerializeField] private GameObject abilityCanvas;

        public static void SendCastAbilityEvent(int gameUnitID, Ability ability, Vector3 start, Vector3 target, Vector3 direction)
        {
            object[] content = { gameUnitID, ability, start, target, direction }; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
            PhotonNetwork.RaiseEvent(CastAbilityEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }
        
        // Start is called before the first frame update
        public float maxAbilityDistance1 = 5;
        public float maxAbilityDistance2 = 7;
        public float cooldownAbility1 = 5;
        public float cooldownAbility2 = 7;
        public bool isCooldown1 = false;
        public bool isCooldown2 = false;

        public Image ability1Image;
        public Image ability2Image;
        public Image ability3Image;

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
            if (!photonView.IsMine)
                return;
            if (Instance == null)
            {
                Instance = this;
            }

            abilityCanvas.SetActive(true);
            targetCircle.GetComponent<Image>().enabled = false;
            arrowIndicatorPivot.GetComponentInChildren<Image>().enabled = false;
            rangeIndicatorCircle1.GetComponent<Image>().enabled = false;
            rangeIndicatorCircle2.GetComponent<Image>().enabled = false;


            ability1Image = GameObject.FindWithTag("Ability1Handle").GetComponent<Image>();
            ability2Image = GameObject.FindWithTag("Ability2Handle").GetComponent<Image>();
            ability3Image = GameObject.FindWithTag("Ability3Handle").GetComponent<Image>();

            ability1Image.fillAmount = 1;
            ability2Image.fillAmount = 1;
            ability3Image.fillAmount = 1;

            GameObject.FindWithTag("Ability1Handle").GetComponent<AbilityJoystick>().RefreshReferences();
            GameObject.FindWithTag("Ability2Handle").GetComponent<AbilityJoystick>().RefreshReferences();
            GameObject.FindWithTag("Ability3Handle").GetComponent<AbilityJoystick>().RefreshReferences();
        }

        void Update()
        {
            if (!photonView.IsMine)
                return;
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
        
        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void Move(Ability ability)
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
                case Ability.NORMAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, null);
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
                case Ability.NORMAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, null);
            }
        }

        private void HideAbilityInterface(Ability ability)
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
                case Ability.NORMAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, null);
            }
        }

        public void CastAbility(Ability ability, Vector3 joystickPosition)
        {
            Vector3 startingPosition = new Vector3();
            Vector3 targetPosition = new Vector3();
            Vector3 direction = new Vector3();
            /* Start of Ellen's  Attack Animation stuff */
            PlayerInput ellenPlayerInput = PlayerController.LocalPlayerController.gameObject.GetComponent<PlayerInput>();
            /* Start of Ellen's Attack Animation stuff */

            switch (ability)
            {
                case Ability.NORMAL:

                /* Start of Ellen's  Attack Animation stuff */

                ellenPlayerInput.DoAttack();

                /* Start of Ellen's Attack Animation stuff */
                break;
                case Ability.RANGE:

                    // We need to keep track of the cooldown of the caster.
                    // It's not important for others to keep track of it,
                    // in other words it's not necessary in the networked
                    // version of this function.                
                    if (isCooldown1)
                    {
                        return;
                    }

                    startingPosition = transform.position + new Vector3(0, 2, 0);
                    targetPosition = targetCircle.transform.position;

                    /* Start of the Energy Explosion stuff */

                    GameObject energyExplosionGameObject = Instantiate(ability1ProjectilePrefab, startingPosition, Quaternion.identity);
                    EnergyExplosionAbilityScript energyExplosionAbilityScript = energyExplosionGameObject.GetComponent<EnergyExplosionAbilityScript>();

                    energyExplosionAbilityScript.setTargetPosition(targetCircle.transform.position);
                    energyExplosionAbilityScript.activateDamage(); // Activate damage for the caster (he main client).

                    /* End of the Energy Explosion stuff*/
                    
                    isCooldown1 = true;
                    ability1Image.fillAmount = 0;

                    /* Start of Ellen's  Attack Animation stuff */

                    ellenPlayerInput.DoAttack();

                    /* Start of Ellen's Attack Animation stuff */

                    break;

                case Ability.LINE:

                    // We need to keep track of the cooldown of the caster.
                    // It's not important for others to keep track of it,
                    // in other words it's not necessary in the networked
                    // version of this function.
                    if (isCooldown2)
                    {
                        return;
                    }                    

                    direction = new Vector3(joystickPosition.x, 0, joystickPosition.y);
                    direction *= maxAbilityDistance2;
                    float angle = Vector3.Angle(direction, new Vector3(1, 0, 0));
                    if (direction.z <= 0)
                    {
                        angle *= -1;
                    }
                    direction = Quaternion.Euler(0, angle, 0) * new Vector3(0, 1, maxAbilityDistance2);

                    startingPosition = transform.position + new Vector3(direction.x * 0.1f, 1, direction.z * 0.1f);

                    targetPosition = transform.TransformPoint(direction);

                    /* Start of the Ice Lance stuff */

                    GameObject iceLanceGameObject = Instantiate(ability2ProjectilePrefab, startingPosition, Quaternion.identity);
                    IceLanceAbilityScript iceLanceAbilityScript = iceLanceGameObject.GetComponent<IceLanceAbilityScript>();
                    
                    iceLanceGameObject.transform.LookAt(targetPosition);
                    iceLanceAbilityScript.setMaxRadius(maxAbilityDistance2*2);
                    iceLanceAbilityScript.determineNumberOfShots();
                    iceLanceAbilityScript.determineUnitsToAvoid();
                    iceLanceAbilityScript.activateDamage();  // Activate damage for the caster (he main client).
                    // iceLanceAbilityScript.setTargetPosition(direction, startingPosition); // Used for debugging.

                    /* End of the Ice Lance stuff*/

                    isCooldown2 = true;
                    ability2Image.fillAmount = 0;

                    /* Start of Ellen's  Attack Animation stuff */

                    ellenPlayerInput.DoAttack();

                    /* Start of Ellen's Attack Animation stuff */

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, null);
            }

            //Send ability cast to other players
            SendCastAbilityEvent(PlayerController.LocalPlayerController.NetworkID, ability, startingPosition, targetPosition, direction);
            
            HideAbilityInterface(ability);
        }


        private void CastAbility(IGameUnit caster, Vector3 start, Vector3 target, Ability ability, Vector3 direction)
        {
            /* Start of Ellen's  Attack Animation stuff */
            PlayerInput ellenPlayerInput = PlayerController.LocalPlayerController.gameObject.GetComponent<PlayerInput>();
            /* Start of Ellen's Attack Animation stuff */

            switch (ability)
            {
                case Ability.NORMAL:

                /* Start of Ellen's  Attack Animation stuff */

                ellenPlayerInput.DoAttack();

                /* Start of Ellen's Attack Animation stuff */

                break;
                case Ability.RANGE:

                    /* Start of the Energy Explosion stuff */

                    GameObject energyExplosionGameObject = Instantiate(ability1ProjectilePrefab, start, Quaternion.identity);
                    EnergyExplosionAbilityScript energyExplosionAbilityScript = energyExplosionGameObject.GetComponent<EnergyExplosionAbilityScript>();

                    energyExplosionAbilityScript.setTargetPosition(target);

                    /* End of the Energy Explosion stuff*/

                    /* Start of Ellen's  Attack Animation stuff */

                    ellenPlayerInput.DoAttack();

                    /* Start of Ellen's Attack Animation stuff */

                    break;
                case Ability.LINE:

                    /* Start of the Ice Lance stuff */

                    GameObject iceLanceGameObject = Instantiate(ability2ProjectilePrefab, start, Quaternion.identity);
                    IceLanceAbilityScript iceLanceAbilityScript = iceLanceGameObject.GetComponent<IceLanceAbilityScript>();
                    
                    iceLanceGameObject.transform.LookAt(target);
                    iceLanceAbilityScript.setMaxRadius(maxAbilityDistance2);
                    iceLanceAbilityScript.determineNumberOfShots();
                    iceLanceAbilityScript.determineUnitsToAvoid();
                    // iceLanceAbilityScript.setTargetPosition(direction, start); // Used for debugging.
                    
                    /* End of the Ice Lance stuff*/

                    /* Start of Ellen's  Attack Animation stuff */

                    ellenPlayerInput.DoAttack();

                    /* Start of Ellen's Attack Animation stuff */
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, null);
            }
        }

        public void DontCastAbility(Ability ability)
        {
            HideAbilityInterface(ability);
        }

        public bool IsCooldown(Ability ability)
        {
            return ability == Ability.RANGE ? isCooldown1 : isCooldown2;
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            if (eventCode == CastAbilityEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;

                int casterID = (int)data[0];
                Ability ability = (Ability)data[1];
                Vector3 start = (Vector3)data[2];
                Vector3 target = (Vector3)data[3];
                Vector3 direction = (Vector3)data[4];
                
                CastAbility(GameStateController.Instance.Players.Values.SingleOrDefault(p => p.NetworkID == casterID), start, target, ability, direction);
            }
        }

    }
}