
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioProfileEditor : UdonSharpBehaviour
    {
        public AudioOverrideManager manager;

        public GameObject entryTemplate;
        public GameObject profileListRoot;

        public Slider voiceGainSlider;
        public InputField voiceGainInput;
        public Slider voiceFarSlider;
        public InputField voiceFarInput;
        public Slider voiceNearSlider;
        public InputField voiceNearInput;
        public Slider voiceVolumetricSlider;
        public InputField voiceVolumetricInput;
        public Toggle voiceLowpassToggle;

        AudioOverrideSettings[] profiles;
        AudioProfileEditorSelect[] uiEntries;
        int nextProfileId = 0;
        bool ignoreInput = false;

        AudioOverrideSettings selectedProfile;
        AudioProfileEditorSelect selectedEditor;

        [UdonSynced]
        float[] syncVoiceGain;
        [UdonSynced]
        float[] syncVoiceFar;
        [UdonSynced]
        float[] syncVoiceNear;
        [UdonSynced]
        float[] syncVoiceVolumetric;
        [UdonSynced]
        bool[] syncVoiceLowpass;

        void Start()
        {
            profiles = new AudioOverrideSettings[0];
            uiEntries = new AudioProfileEditorSelect[0];
 
            _ScanZone(manager.defaultZone);
            for (int i = 0; i < manager.overrideZones.Length; i++)
                _ScanZone(manager.overrideZones[i]);

            int count = profiles.Length;
            syncVoiceGain = new float[count];
            syncVoiceFar = new float[count];
            syncVoiceNear = new float[count];
            syncVoiceVolumetric = new float[count];
            syncVoiceLowpass = new bool[count];

            _RefreshList();
        }

        void _ScanZone(AudioOverrideZone zone)
        {
            if (!zone)
                return;

            _TryAddSettings(zone._GetDefaultSettings());
            _TryAddSettings(zone._GetLocalSettings());

            int count = zone.linkedZoneSettings.Length;
            for (int i = 0; i < count; i++)
                _TryAddSettings(zone.linkedZoneSettings[i]);
        }

        void _TryAddSettings(AudioOverrideSettings profile)
        {
            if (!profile)
                return;

            foreach (var other in profiles)
            {
                if (profile == other)
                    return;
            }

            profiles = (AudioOverrideSettings[])UtilityTxl.ArrayAddElement(profiles, profile, profile.GetType());
        }

        void _RefreshList()
        {
            for (int i = 0; i < uiEntries.Length; i++)
            {
                AudioProfileEditorSelect entry = uiEntries[i];
                GameObject.DestroyImmediate(entry.gameObject);
            }

            uiEntries = new AudioProfileEditorSelect[0];

            for (int i = 0; i < profiles.Length; i++)
                _AddUIEntry(profiles[i]);

            _SelectProfile(0);
        }

        void _AddUIEntry(AudioOverrideSettings profile)
        {
            GameObject entry = Instantiate(entryTemplate);
            AudioProfileEditorSelect script = entry.GetComponent<AudioProfileEditorSelect>();

            script._SetName(profile.name);
            script._Bind(this, profile, nextProfileId);

            uiEntries = (AudioProfileEditorSelect[])UtilityTxl.ArrayAddElement(uiEntries, script, script.GetType());
            nextProfileId += 1;

            entry.transform.SetParent(profileListRoot.transform);

            RectTransform rt = (RectTransform)entry.GetComponent(typeof(RectTransform));
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.localPosition = Vector3.zero;
        }

        public void _SelectProfile(int id)
        {
            if (id < 0 || id >= uiEntries.Length)
                return;

            selectedProfile = profiles[id];
            selectedEditor = uiEntries[id];
            uiEntries[id]._EditorSelect();

            for (int i = 0; i < uiEntries.Length; i++)
            {
                if (i != id)
                    uiEntries[i]._EditorDeselect();
            }

            _RefreshControls();
        }

        void _RefreshControls()
        {
            if (!selectedProfile)
                return;

            ignoreInput = true;

            float val = selectedProfile.voiceGain;
            voiceGainSlider.SetValueWithoutNotify(val);
            voiceGainInput.text = val.ToString();

            val = selectedProfile.voiceFar;
            voiceFarSlider.SetValueWithoutNotify(val);
            voiceFarInput.text = val.ToString();

            val = selectedProfile.voiceNear;
            voiceNearSlider.SetValueWithoutNotify(val);
            voiceNearInput.text = val.ToString();

            val = selectedProfile.voiceVolumetric;
            voiceVolumetricSlider.SetValueWithoutNotify(val);
            voiceVolumetricInput.text = val.ToString();

            bool lowPass = selectedProfile.voiceLowpass;
            voiceLowpassToggle.isOn = lowPass;

            ignoreInput = false;
        }

        void _UpdateCommon()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _UpdateInput()
        {
            if (!selectedProfile)
                return;

            if (ignoreInput)
                return;

            _UpdateCommon();

            float voiceGain = 0;
            if (float.TryParse(voiceGainInput.text, out voiceGain))
                selectedEditor._SetVoiceGain(voiceGain);

            float voiceFar = 0;
            if (float.TryParse(voiceFarInput.text, out voiceFar))
                selectedEditor._SetVoiceFar(voiceFar);

            float voiceNear = 0;
            if (float.TryParse(voiceNearInput.text, out voiceNear))
                selectedEditor._SetVoiceNear(voiceNear);

            float voiceVolumetric = 0;
            if (float.TryParse(voiceVolumetricInput.text, out voiceVolumetric))
                selectedEditor._SetVoiceVolumetric(voiceVolumetric);

            selectedEditor._SetVoiceLowPass(voiceLowpassToggle.isOn);

            _RefreshControls();
        }

        public void _UpdateSliderVoiceGain()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetVoiceGain(voiceGainSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderVoiceFar()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetVoiceFar(voiceFarSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderVoiceNear()
        {
            if (selectedProfile && !ignoreInput) {
                _UpdateCommon();
                selectedEditor._SetVoiceNear(voiceNearSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderVoiceVolumetric()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetVoiceVolumetric(voiceVolumetricSlider.value);
                _RefreshControls();
            }
        }

        public void _ResetVoiceDefault()
        {
            if (selectedProfile)
            {
                _UpdateCommon();
                selectedEditor._RestoreVoiceDefault();
                _RefreshControls();
            }
        }

        public void _ResetVoiceVRC()
        {
            if (selectedProfile)
            {
                _UpdateCommon();
                selectedEditor._RestoreVoiceVRC();
                _RefreshControls();
            }
        }

        public void _UpdateControl()
        {
            if (ignoreInput)
                return;

            _RefreshControls();
        }

        public void _UpdateManager()
        {
            manager._RebuildLocal();

            if (Networking.IsOwner(gameObject))
                _SyncProfiles();
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            base.OnDeserialization(result);

            _CopyProfilesFromSync();
            _RefreshControls();
        }

        void _SyncProfiles()
        {
            int c = Mathf.Min(profiles.Length, syncVoiceGain.Length);
            for (int i = 0; i < c; i++)
            {
                syncVoiceGain[i] = profiles[i].voiceGain;
                syncVoiceFar[i] = profiles[i].voiceFar;
                syncVoiceNear[i] = profiles[i].voiceNear;
                syncVoiceVolumetric[i] = profiles[i].voiceVolumetric;
                syncVoiceLowpass[i] = profiles[i].voiceLowpass;
            }

            RequestSerialization();
        }

        void _CopyProfilesFromSync()
        {
            int c = Mathf.Min(profiles.Length, syncVoiceGain.Length);
            for (int i = 0; i < c; i++)
            {
                profiles[i].voiceGain = syncVoiceGain[i];
                profiles[i].voiceFar = syncVoiceFar[i];
                profiles[i].voiceNear = syncVoiceNear[i];
                profiles[i].voiceVolumetric = syncVoiceVolumetric[i];
                profiles[i].voiceLowpass = syncVoiceLowpass[i];
            }

            manager._RebuildLocal();
        }
    }
}
