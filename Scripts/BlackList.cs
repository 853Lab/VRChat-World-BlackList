
using System.Text;
using Sonic853.Texture2String;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace Sonic853.BlackList
{
    public class BlackList : UdonSharpBehaviour
    {
        public TextAsset textAsset;
        [Multiline]
        public string stringData = "";
        public TextAsset f2b;
        public TextAsset bb;
        bool found = false;
        string whyBlock = "";
        public Texture2D texture = null;
        public GameObject[] gameObjects;
        VRCPlayerApi localPlayer;
        Vector3 originalPosition;
        public Transform vRCWorld;
        public Transform moveVRCWorld;
        public TMP_Text tMP_Text;
        bool[] actives = new bool[0];
        public float maxDistance = 10f;
        bool isBlocked = false;
        void Start()
        {
            Load();
            if (vRCWorld == null)
            {
                var vRCWorldObj = GameObject.Find("VRCWorld");
                if (vRCWorldObj != null) vRCWorld = vRCWorldObj.transform;
            }
            if (moveVRCWorld == null) moveVRCWorld = transform;
            originalPosition = vRCWorld.position;
            localPlayer = Networking.LocalPlayer;
            LoadTextAsset();
        }
        void Update()
        {
            if (isBlocked)
            {
                var pos = localPlayer.GetBonePosition(HumanBodyBones.Head);
                var distance = Vector3.Distance(pos, moveVRCWorld.position);
                if (distance > maxDistance) localPlayer.Respawn();
            }
        }
        void Load()
        {
            if (actives.Length != 0) { return; }
            actives = new bool[gameObjects.Length];
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var _gameObject = gameObjects[i];
                if (_gameObject == null) { continue; }
                actives[i] = _gameObject.activeSelf;
            }
        }
        public void LoadTextAsset() => LoadTextAsset(textAsset);
        public void LoadTextAsset(TextAsset _textAsset)
        {
            if (_textAsset != null) stringData = Encoding.UTF8.GetString(System.Convert.FromBase64String(_textAsset.text));
            LoadData();
        }
        public void LoadTexture() => LoadTexture(texture);
        public void LoadTexture(Texture2D _texture)
        {
            if (_texture == null)
            {
                LoadData();
                return;
            }
            stringData = _texture.RGBAToText(true, true);
            LoadData();
        }
        public void LoadData() => LoadData(stringData);
        public void LoadData(string _stringData)
        {
            var displayName = localPlayer.displayName;
            var _found = false;
            var _whyBlock = whyBlock;
            // Debug.Log(_stringData);
            if (!VRCJson.TryDeserializeFromJson(_stringData, out var result)) { return; }
            if (result.TokenType != TokenType.DataList) { return; }
            for (int i = 0; i < result.DataList.Count; i++)
            {
                if (!result.DataList.TryGetValue(i, out var value)) { break; }
                if (value.TokenType != TokenType.DataDictionary
                || !value.DataDictionary.TryGetValue("n", out var names)
                || names.TokenType != TokenType.DataList) { continue; }
                for (int j = 0; j < names.DataList.Count; j++)
                {
                    if (!names.DataList.TryGetValue(j, out var name)) { break; }
                    if (name.TokenType != TokenType.String) { continue; }
                    var nameStr = name.String;
                    _found = displayName == nameStr;
                    if (_found) { break; }
                }
                if (!value.DataDictionary.TryGetValue("w", out var whyBlockt)
                    || whyBlockt.TokenType != TokenType.String)
                {
                    if (_found) { break; }
                    continue;
                }
                if (_found)
                {
                    _whyBlock = GetBanMessage(whyBlockt.String);
                    break;
                }
            }
            if (found || _found)
            {
                whyBlock = _whyBlock;
                Block(displayName, whyBlock);
            }
        }
        void Block(string displayName, string whyBlock)
        {
            Debug.Log($"[Sonic853.BlackList] {displayName} is blocked: {whyBlock}");
            if (tMP_Text != null) tMP_Text.text = $"You are BLOCK\nInfo: {whyBlock}";
            isBlocked = true;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var _gameObject = gameObjects[i];
                if (_gameObject == null) { continue; }
                _gameObject.SetActive(!actives[i]);
            }
            if (vRCWorld == null)
            {
                var vRCWorldObj = GameObject.Find("VRCWorld");
                if (vRCWorldObj == null) { return; }
                vRCWorld = vRCWorldObj.transform;
            }
            vRCWorld.position = moveVRCWorld.position;
            localPlayer.Respawn();
        }
        void UnBlock()
        {
            isBlocked = false;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var _gameObject = gameObjects[i];
                if (_gameObject == null) { continue; }
                _gameObject.SetActive(actives[i]);
            }
            vRCWorld.position = originalPosition;
            localPlayer.Respawn();
        }
        string GetBanMessage(string text)
        {
            switch (text)
            {
                case "f2b":
                    return Encoding.UTF8.GetString(System.Convert.FromBase64String(f2b.text));
                case "bb":
                    return Encoding.UTF8.GetString(System.Convert.FromBase64String(bb.text));
                default:
                    return text;
            }
        }
        public void SendFunction() => LoadTexture(texture);
    }
}
