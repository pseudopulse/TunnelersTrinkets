using System;
using System.Linq;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using WormAPI;
using WormAPI.Utils;

namespace TunnelersTrinkets {
    public static class FlareGun {
        public static GameObject FlareGunEquipment;
        public static GameObject Flare;
        public static void Initialize() {
            FlareGunEquipment = Load<GameObject>("PardnerEquipment_FlareGun.prefab");
            Flare = Load<GameObject>("Flare.prefab");

            PrefabAPI.MarkNetworkPrefab(FlareGunEquipment);
            PrefabAPI.MarkNetworkPrefab(Flare);

            EquipmentAPI.AddEquipment(
                new EquipmentInfo()
                {
                    Prefab = FlareGunEquipment,
                    Name = "TT_EQUIP_FLAREGUN"
                }
            );

            LanguageAPI.AddString("TT_EQUIP_FLAREGUN", "Flare Gun");
        }
    }

    // TODO:
    // - fix not being destroyed on ammo empty

    public class PardnerEquipment_FlareGun : PardnerEquipment {
        public GameObject flareGunModel;
        public float shotSpeed;
        public GameObject flare;
        public GameObject muzzleFlash;
        public AudioSource shotSound;
        private Transform muzzle;

        public override void OnUse()
        {
            if (!GetIsCooldownReady()) {
                return;
            }

            Vector3 direction = (aimSpotOnUse - muzzle.transform.position).normalized;

            ShootTheFuckingShot_ServerRPC(muzzle.transform.position, muzzle.forward, direction);

            base.OnUse();
        }

        public override void Update()
        {
            base.Update();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShootTheFuckingShot_ServerRPC(Vector3 pos, Vector3 rot, Vector3 dir) {
            ShootTheFuckingShot(pos, rot, dir);
        }

        public void ShootTheFuckingShot(Vector3 pos, Vector3 rot, Vector3 dir) {
            SpawnEffects_ServerRPC(pos, rot);
            SpawnEffects(pos, rot);

            Vector3 direction = dir;
            GameObject flareShot = GameObject.Instantiate(flare, pos, Quaternion.LookRotation(dir));
            flareShot.GetComponent<Rigidbody>().velocity = direction * shotSpeed;
            flareShot.GetComponent<NetworkObject>().Spawn(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnEffects_ServerRPC(Vector3 pos, Vector3 dir) {
            SpawnEffects_ClientRPC(pos, dir);
        }
        
        [ClientRpc]
        public void SpawnEffects_ClientRPC(Vector3 pos, Vector3 dir) {
            SpawnEffects(pos, dir);
        }

        public void SpawnEffects(Vector3 pos, Vector3 dir) {
            GameObject.Instantiate(muzzleFlash, pos, Quaternion.LookRotation(dir));
            AudioSource.PlayClipAtPoint(shotSound.clip, pos);
        }

        public override void OnEquip()
        {
            base.OnEquip();
            spawnedAimDummyObject = GameObject.Instantiate(flareGunModel);
            spawnedAimDummyObject.transform.parent = grabberScriptOfWhoHasUsEquipped.GetRifleAimDummyParent();
            spawnedAimDummyObject.transform.localPosition = new(-0.2f, 0, -0.1f);
            spawnedAimDummyObject.transform.localRotation = Quaternion.Euler(0, 270, 0);
            muzzle = spawnedAimDummyObject.transform.Find("Muzzle");
            spawnedAimDummyObject.SetActive(false);
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            GameObject.Destroy(spawnedAimDummyObject);
        }

        public override void OnIsAimingUpdate()
        {
            base.OnIsAimingUpdate();
            spawnedAimDummyObject.SetActive(true);
            grabberScriptOfWhoHasUsEquipped.GetSpawnedEquipmentOnOurBodyDummy().SetActive(false);
        }

        public override void OnNotAimingUpdate()
        {
            base.OnNotAimingUpdate();
            spawnedAimDummyObject.SetActive(false);
            grabberScriptOfWhoHasUsEquipped.GetSpawnedEquipmentOnOurBodyDummy().SetActive(true);
        }
    }

    public class Flare : NetworkBehaviour {
        public Rigidbody rb;
        public bool stuck = false;
        private GroundDetectable detectable;

        public void Start() {
            rb = GetComponent<Rigidbody>();
            detectable = GetComponentInChildren<GroundDetectable>();
        }

        public void Update() {
            if (rb.velocity != Vector3.zero) {
                base.transform.forward = rb.velocity.normalized;
            }
        }

        public void LateUpdate() {
            if (detectable && detectable.spawnedAudioPingParticles && detectable.spawnedHudAudioPing) {
                if (GameManager.Singleton && GameManager.Singleton.gameState == GameManager.GameState.playing && !detectable.isOurPlayerObject && !(GameManager.Singleton.playerType != GameManager.PlayerType.worm || !GroundDetectableManager.Singleton.localWormTransform)) {
                    Camera cam = GameManager.Singleton.activeCamera;

                    Vector3 vec = cam.WorldToViewportPoint(base.transform.position);

                    if (vec.x > 0f && vec.x < 1f && vec.y > 0f && vec.y < 1f && vec.z > 0f) {
                        detectable.spawnedAudioPingParticles.SetActive(true);
                        detectable.spawnedHudAudioPing.SetActive(false);
                        detectable.spawnedAudioPingParticles.transform.position = base.transform.position;
                    }
                    else {
                        detectable.spawnedAudioPingParticles.SetActive(false);
                        detectable.spawnedHudAudioPing.SetActive(true);
                        Quaternion rotation = Quaternion.LookRotation(cam.transform.position - base.transform.position);
                        rotation.z = 0f - rotation.y;
                        rotation.x = (rotation.y = 0f);
                        Vector3 euler = new(0f, 0f, cam.transform.eulerAngles.y);
                        detectable.spawnedHudAudioPing.transform.localRotation = rotation * Quaternion.Euler(euler);
                    }
                }
            }
        }

        public void OnCollisionEnter(Collision col) {
            if (stuck) {
                return;
            }

            var contact = col.contacts[0];

            if (col.collider && Deflect(col.collider)) {
                rb.velocity += contact.normal * 5f;
                return;
            }

            // base.transform.localRotation = Quaternion.LookRotation(-contact.normal);
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            
            stuck = true;

            GetComponentInChildren<Collider>().enabled = false;
        }

        public bool Deflect(Collider col) {
            var tumbleweed = col.GetComponentInParent<Tumbleweed>();
            if (tumbleweed) {
                tumbleweed.LightOnFire_ServerRPC(tumbleweed.ourPickUpScript.lastPlayerWhoHeldUs);
                return true;
            }

            var dynamite = col.GetComponentInParent<Dynamite>();
            if (dynamite) {
                dynamite.DynamiteWasShotByRifle(dynamite.ourPickUpScript.lastPlayerWhoHeldUs);
                GameObject.Destroy(base.gameObject);
                return true;
            }

            if (col.GetComponentInParent<WormPlayer>() || col.GetComponentInParent<Player>() || col.GetComponentInParent<Rigidbody>()) {
                return true;
            }
        
            return false;
        }
    }
}