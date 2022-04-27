using BepInEx;
using BepInEx.IL2CPP;
using UnityEngine;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using HarmonyLib;

namespace DebugMenu
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        static string layout;
        static string path;
        public static Rigidbody playerBody;
        public static System.Collections.Generic.Dictionary<string,System.Func<string>> DebugDataCallbacks;

        public static void registerDataCallback(string s, System.Func<string> f){
            DebugDataCallbacks.Add(s,f);
        }
        public static void registerDataCallbacks(System.Collections.Generic.Dictionary<string,System.Func<string>> dict){
            foreach (System.Collections.Generic.KeyValuePair<string,System.Func<string>> pair in dict){
                DebugDataCallbacks.Add(pair.Key,pair.Value);
            }
        }
        public static void checkFileExists(){
            if (!System.IO.File.Exists(path)){
                System.IO.File.WriteAllText(path,"Speed: [SPEED] u/s\nPosition: [POSITION]",System.Text.Encoding.UTF8);
            }
        }
        public static void loadLayout(){
            layout = System.IO.File.ReadAllText(path,System.Text.Encoding.UTF8);
        }
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<DebugMenu>();
            DebugDataCallbacks = new System.Collections.Generic.Dictionary<string, System.Func<string>>();
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            path = System.IO.Directory.GetParent(Application.dataPath)+"\\DebugLayout.txt";
            Harmony.CreateAndPatchAll(typeof(Plugin));
            checkFileExists();
            loadLayout();
            registerDefaultCallbacks();
        }
        public static void registerDefaultCallbacks(){
            registerDataCallbacks(new System.Collections.Generic.Dictionary<string,System.Func<string>>(){
                {"SPEED",getPlayerSpeed},
                {"POSITION",getPlayerPos}
            });
        }
        public static Rigidbody getPlayerBody(){
            GameObject obj = GameObject.Find("/Player");
            return obj==null?null:obj.GetComponent<Rigidbody>();
        }
        public static Rigidbody getPlayerBodySafe(){
            if (playerBody==null){
                playerBody=getPlayerBody();
            }
            return playerBody;
        }
        public static string getPlayerSpeed(){
            Rigidbody rb = getPlayerBodySafe();
            return rb==null?"":rb.velocity.magnitude.ToString("0.00");
        }
        public static string getPlayerPos(){
            Rigidbody rb = getPlayerBodySafe();
            return rb==null?"":rb.transform.position.ToString();
        }
        public static string formatLayout(){
            string formatted = layout;
            foreach (System.Collections.Generic.KeyValuePair<string,System.Func<string>> pair in DebugDataCallbacks){
                formatted = formatted.Replace("["+pair.Key+"]",pair.Value());
            }
            return formatted;
        }
        public class DebugMenu : MonoBehaviour {
            public Text text;
            bool MenuEnabled = false;
            void Update(){
                text.text = MenuEnabled ? formatLayout() : ""; 
                if(Input.GetKeyDown("f3")){
                    MenuEnabled = !MenuEnabled;
                }
            }
        }
        [HarmonyPatch(typeof(MonoBehaviourPublicGaroloGaObInCacachGaUnique),"Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(MonoBehaviourPublicGaroloGaObInCacachGaUnique __instance){
            GameObject menuObject = new GameObject();
            Text text = menuObject.AddComponent<Text>();
            text.font = (Font)Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;
            DebugMenu menu = menuObject.AddComponent<DebugMenu>();
            menu.text = text;
            Plugin.playerBody = null;
            menuObject.transform.SetParent(__instance.transform);
            menuObject.transform.localPosition = new Vector3(menuObject.transform.localPosition.x,-menuObject.transform.localPosition.y,menuObject.transform.localPosition.z);
            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0,1);
            rt.sizeDelta = new Vector2(1000,1000);
        }
    }
}
