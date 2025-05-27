global using static TunnelersTrinkets.TunnelersTrinkets;

using System.Linq;
using BepInEx;
using UnityEngine;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using System.Text;
using BepInEx.Logging;
using HarmonyLib.Tools;
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;
using WormAPI;
using System;
using WormAPI.Utils;

namespace TunnelersTrinkets
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TunnelersTrinkets : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "WormTeam.TunnelersTrinkets";
        public const string PLUGIN_NAME = "TunnelersTrinkets";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static ManualLogSource Log;
        public static AssetBundle bundle;
        public void Awake() {
            Log = Logger;

            bundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("TunnelersTrinkets.dll", "wcmbundle"));

            FlareGun.Initialize();
            RockShoes.Initialize();
            Slipstream.Initialize();

            Material[] mat = bundle.LoadAllAssets<Material>();

            for (int i = 0; i < mat.Length; i++)
            {
                if (mat[i].shader.name == "Stubbed/Clay_WithDissolve")
                {
                    mat[i].shader = CommonAssets.Shaders.Clay_WithDissolve;
                }
            }

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        public static T Load<T>(string key) where T : UnityEngine.Object {
            return bundle.LoadAsset<T>(key);
        }
    }
}
