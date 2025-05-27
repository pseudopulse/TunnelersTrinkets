using System;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using WormAPI;
using WormAPI.Utils;

namespace TunnelersTrinkets {
    public static class RockShoes {
        public static GameObject RockShoesEquipment;
        public static Dictionary<Player, bool> SilenceMap = new();

        public static void Initialize() {
            RockShoesEquipment = Load<GameObject>("PardnerEquipment_RockShoes.prefab");

            PrefabAPI.MarkNetworkPrefab(RockShoesEquipment);

            EquipmentAPI.AddEquipment(
                new EquipmentInfo()
                {
                    Prefab = RockShoesEquipment,
                    Name = "TT_EQUIP_ROCKSHOES"
                }
            );

            LanguageAPI.AddString("TT_EQUIP_ROCKSHOES", "Silent Shoes");

            On.GroundDetectable.ResetVisability += RockShoesSilence;
        }

        private static void RockShoesSilence(On.GroundDetectable.orig_ResetVisability orig, GroundDetectable self, float _fadeTimeBuffer, bool _IgnoreWormMidAirCheck, bool _forcePingVisability)
        {
            if (self.isPlayer && self.thisPlayerScript && SilenceMap.ContainsKey(self.thisPlayerScript) && !_forcePingVisability) {
                return;
            }
            
            orig(self, _fadeTimeBuffer, _IgnoreWormMidAirCheck, _forcePingVisability);
        }
    }

    public class PardnerEquipment_RockShoes : PardnerEquipment
    {
        public float duration = 12f;
        public GameObject singleShoe;
        private float stopwatch = 0f;
        internal bool isActive = false;
        private GameObject shoe1;
        private GameObject shoe2;

        public override void OnUse()
        {
            if (stopwatch > 0f)
            {
                return;
            }

            stopwatch = duration;
            isActive = true;

            HandleText();

            ToggleSilentServerRpc(true);

            RespawnShoes(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleSilentServerRpc(bool silence) {
            if (!grabberScriptOfWhoHasUsEquipped) return;
            
            if (silence) {
                if (!RockShoes.SilenceMap.ContainsKey(grabberScriptOfWhoHasUsEquipped.playerScript))
                {
                    RockShoes.SilenceMap.Add(grabberScriptOfWhoHasUsEquipped.playerScript, true);
                }

                isActive = true;
            }
            else {
                RockShoes.SilenceMap.Remove(grabberScriptOfWhoHasUsEquipped.playerScript);
                isActive = false;
            }

            HandleText();

            ToggleSilentClientRpc(silence);
        }

        [ClientRpc()]
        public void ToggleSilentClientRpc(bool silence) {
            if (!grabberScriptOfWhoHasUsEquipped) return;

            if (silence) {
                if (!RockShoes.SilenceMap.ContainsKey(grabberScriptOfWhoHasUsEquipped.playerScript))
                {
                    RockShoes.SilenceMap.Add(grabberScriptOfWhoHasUsEquipped.playerScript, true);
                }

                isActive = true;
            }
            else {
                RockShoes.SilenceMap.Remove(grabberScriptOfWhoHasUsEquipped.playerScript);
                isActive = false;
                stopwatch = 0f;
            }

            HandleText();
        }

        public void FixedUpdate()
        {
            if (grabberScriptOfWhoHasUsEquipped && grabberScriptOfWhoHasUsEquipped.GetSpawnedEquipmentOnOurBodyDummy()) {
                if (grabberScriptOfWhoHasUsEquipped.GetSpawnedEquipmentOnOurBodyDummy().gameObject.activeSelf) {
                    RespawnShoes(false);
                }

                grabberScriptOfWhoHasUsEquipped.GetSpawnedEquipmentOnOurBodyDummy().gameObject.SetActive(false);
            }

            if (stopwatch > 0f)
            {
                stopwatch -= Time.fixedDeltaTime;

                if (stopwatch <= 0f)
                {
                    ToggleSilentServerRpc(false);
                    DeductStock();
                }
            }
        }

        public void DeductStock()
        {
            numOfUses--;
            UpdateRemainingUses_ServerRPC(numOfUses);
            HudManager.Singleton.JustUsedEquipment_UpdateHud(this);
            isActive = false;

            stopwatch = 0f;

            HandleText();

            RespawnShoes(false);

            if (numOfUses <= 0)
            {
                OnEquipmentDepleted();
            }

            ToggleSilentServerRpc(false);
        }

        public override void OnUnequip()
        {
            HandleText();

            if (isActive) {
                DeductStock();
            }

            if (shoe1) GameObject.Destroy(shoe1);
            if (shoe2) GameObject.Destroy(shoe2);

            base.OnUnequip();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (shoe1) GameObject.Destroy(shoe1);
            if (shoe2) GameObject.Destroy(shoe2);

            if (!grabberScriptOfWhoHasUsEquipped) return;

            ToggleSilentServerRpc(false);
        }

        public override void OnEquip()
        {
            base.OnEquip();

            HandleText();
        }

        public void RespawnShoes(bool active) {
            if (shoe1) GameObject.Destroy(shoe1);
            if (shoe2) GameObject.Destroy(shoe2);

            if (!grabberScriptOfWhoHasUsEquipped) {
                return;
            }

            Transform root = grabberScriptOfWhoHasUsEquipped.transform.Find("Art");

            if (active) {
                root.transform.localPosition = new(0f, 0.125f, 0f);

                root = root.Find("Clayman_01_rigged").Find("mixamorig:Hips");

                shoe1 = GameObject.Instantiate(singleShoe, root.Find("mixamorig:LeftUpLeg").Find("mixamorig:LeftLeg").Find("mixamorig:LeftFoot"));
                shoe1.transform.localPosition = new(0, 0.2f, -0.1f);
                shoe1.transform.localRotation = Quaternion.Euler(0, 90, 130);
                shoe1.transform.localScale = new(0.3f, 0.3f, 0.3f);
                shoe2 = GameObject.Instantiate(singleShoe, root.Find("mixamorig:RightUpLeg").Find("mixamorig:RightLeg").Find("mixamorig:RightFoot"));
                shoe2.transform.localPosition = new(0, 0.2f, -0.1f);
                shoe2.transform.localRotation = Quaternion.Euler(0, 90, 130);
                shoe2.transform.localScale = new(0.3f, 0.3f, 0.3f);
            }
            else {
                root.transform.localPosition = new(0f, 0f, 0f);

                root = root.Find("Clayman_01_rigged").Find("mixamorig:Hips").Find("mixamorig:Spine").Find("mixamorig:Spine1").Find("mixamorig:Spine2");

                shoe1 = GameObject.Instantiate(singleShoe, root.Find("mixamorig:LeftShoulder").Find("mixamorig:LeftArm").Find("mixamorig:LeftForeArm").Find("mixamorig:LeftHand"));
                shoe1.transform.localScale = new(0.3f, 0.3f, 0.3f);
                shoe1.transform.localRotation = Quaternion.Euler(0, 0, 230);
                shoe1.transform.localPosition = new(-0.2f, 0.3f, 0f);
                shoe2 = GameObject.Instantiate(singleShoe, root.Find("mixamorig:RightShoulder").Find("mixamorig:RightArm").Find("mixamorig:RightForeArm").Find("mixamorig:RightHand"));
                shoe2.transform.localScale = new(0.3f, 0.3f, 0.3f);
                shoe2.transform.localRotation = Quaternion.Euler(0, 0, -230);
                shoe2.transform.localPosition = new(0.2f, 0.3f, 0f);
            }
        }

        public void HandleText()
        {
            if (!isActive && numOfUses > 0)
            {
                string prefix = "";

                if (InputManager.Singleton.lastUsedControllerType == InputManager.ControllerType.keyboard)
                {
                    prefix = InputManager.Singleton.inputActions.PardnerActionMap.UseHeldObject.GetBindingDisplayString(0);
                }
                else if (InputManager.Singleton.lastUsedControllerType == InputManager.ControllerType.controller)
                {
                    prefix = InputManager.Singleton.inputActions.PardnerActionMap.UseHeldObject.GetBindingDisplayString(1);
                }

                HudManager.Singleton.ClearEquipmentUseHintText();

                string text = $"Wear the Silent Shoes by pressing {prefix}.";

                HudManager.Singleton.DisplayNewEquipmentUseHintText(text, 38f);
            }
            else if (isActive && numOfUses > 0)
            {
                HudManager.Singleton.ClearEquipmentUseHintText();

                string text = $"You are silent to the worm.";

                HudManager.Singleton.DisplayNewEquipmentUseHintText(text, 38f);
            }
            else
            {
                HudManager.Singleton.ClearEquipmentUseHintText();
            }
        }
    }
}