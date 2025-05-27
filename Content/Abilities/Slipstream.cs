using System;
using UnityEngine;
using WormAPI;

namespace TunnelersTrinkets {
    public class Slipstream {
        public static GameObject SlipstreamObject;

        public static void Initialize() {
            SlipstreamObject = Load<GameObject>("Slipstream.prefab");
            PrefabAPI.MarkNetworkPrefab(SlipstreamObject);

            WormAbilityAPI.AddWormAbility(new WormAbilityInfo()
            {
                WormAbilityObject = SlipstreamObject,
                AbilityNotes = new() {
                    "This is a passive ability and cannot be activated."
                }
            });

            LanguageAPI.AddString("TT_ABILITY_SLIPSTREAM", "Slipstream");
            LanguageAPI.AddString("TT_ABILITY_SLIPSTREAM_DESC", "Increase movement speed underground by 50%. Boost is lowered to 25% when below max health.");
        }
    }

    public class WormAbility_Slipstream : WormAbility {
        public WormMovement movement;
        public float surfaceSpeed;
        public float damagedSpeed;
        public float speed;
        public bool equipped = false;
        public override void Update()
        {
            base.Update();

            if (!movement) {
                if (ourWormAbilityManager) {
                    movement = ourWormAbilityManager.GetComponent<WormMovement>();
                    surfaceSpeed = movement.burrowSpeed;
                    damagedSpeed = surfaceSpeed * 1.25f;
                    speed = surfaceSpeed * 1.50f;

                    equipped = ourWormAbilityManager.abilitySlot2 == this || ourWormAbilityManager.abilitySlot3 == this;

                    Transform root = isActiveUIFillImage.transform.parent;
                    if (ourWormAbilityManager.abilitySlot2 == this) {
                        root.Find("Cooldown Indicator").gameObject.SetActive(false);
                        root.Find("Cooldown Time Text 2").gameObject.SetActive(false);
                        root.Find("Charge Counter Group (2)").gameObject.SetActive(false);
                        root.Find("Ability Keybind Label").gameObject.SetActive(false);
                        root.Find("Border Image On Cooldown (1)").gameObject.SetActive(false);
                        root.Find("Is Off Cooldown background X").gameObject.SetActive(false);
                    }
                    else {
                        root.Find("Cooldown Indicator").gameObject.SetActive(false);
                        root.Find("Cooldown Time Text 3").gameObject.SetActive(false);
                        root.Find("Charge Counter Group (3)").gameObject.SetActive(false);
                        root.Find("Ability Keybind Label").gameObject.SetActive(false);
                        root.Find("Border Image On Cooldown (2)").gameObject.SetActive(false);
                        root.Find("Is Off Cooldown background X").gameObject.SetActive(false);
                    }
                }
            }

            if (!equipped) return;

            if (movement && movement.wormMovementState == WormPlayer.WormMovementState.Burrowed) {
                if (movement.wormPlayer.wormHealth < movement.wormPlayer.wormHealth_Max) {
                    movement.burrowSpeed = damagedSpeed;
                }
                else {
                    movement.burrowSpeed = speed;
                }
            }
        }
    }
}