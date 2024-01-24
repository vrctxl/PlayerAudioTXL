
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
        public AudioOverrideSettings[] audioProfiles;
        public bool autoScanProfiles = true;

        public AccessControl accessControl;

        [Header("UI")]
        public GameObject entryTemplate;
        public GameObject profileListRoot;

        public Toggle syncSettingsToggle;
        public Slider voiceGainSlider;
        public InputField voiceGainInput;
        public Slider voiceFarSlider;
        public InputField voiceFarInput;
        public Slider voiceNearSlider;
        public InputField voiceNearInput;
        public Slider voiceVolumetricSlider;
        public InputField voiceVolumetricInput;
        public Toggle voiceLowpassToggle;
        public Slider avatarGainSlider;
        public InputField avatarGainInput;
        public Slider avatarFarSlider;
        public InputField avatarFarInput;
        public Slider avatarNearSlider;
        public InputField avatarNearInput;
        public Slider avatarVolumetricSlider;
        public InputField avatarVolumetricInput;

        AudioOverrideSettings[] profiles;
        AudioProfileEditorSelect[] uiEntries;
        int nextProfileId = 0;
        bool ignoreInput = false;

        AudioOverrideSettings selectedProfile;
        AudioProfileEditorSelect selectedEditor;

        [UdonSynced, FieldChangeCallback(nameof(SyncSettings))]
        bool syncSyncSettings = false;
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
        [UdonSynced]
        float[] syncAvatarGain;
        [UdonSynced]
        float[] syncAvatarFar;
        [UdonSynced]
        float[] syncAvatarNear;
        [UdonSynced]
        float[] syncAvatarVolumetric;

        void Start()
        {
            profiles = new AudioOverrideSettings[0];
            uiEntries = new AudioProfileEditorSelect[0];

            if (autoScanProfiles)
            {
                _ScanZone(manager.defaultZone);
                for (int i = 0; i < manager.overrideZones.Length; i++)
                    _ScanZone(manager.overrideZones[i]);
            }

            foreach (AudioOverrideSettings p in audioProfiles)
                _TryAddSettings(p);

            int count = profiles.Length;
            syncVoiceGain = new float[count];
            syncVoiceFar = new float[count];
            syncVoiceNear = new float[count];
            syncVoiceVolumetric = new float[count];
            syncVoiceLowpass = new bool[count];
            syncAvatarGain = new float[count];
            syncAvatarFar = new float[count];
            syncAvatarNear = new float[count];
            syncAvatarVolumetric = new float[count];

            _RefreshList();
            _RefreshSyncSettingsControl();
        }

        public bool SyncSettings
        {
            get { return syncSyncSettings; }
            set
            {
                syncSyncSettings = value;

                _RefreshSyncSettingsControl();
            }
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

            val = selectedProfile.avatarGain;
            avatarGainSlider.SetValueWithoutNotify(val);
            avatarGainInput.text = val.ToString();

            val = selectedProfile.avatarFar;
            avatarFarSlider.SetValueWithoutNotify(val);
            avatarFarInput.text = val.ToString();

            val = selectedProfile.avatarNear;
            avatarNearSlider.SetValueWithoutNotify(val);
            avatarNearInput.text = val.ToString();

            val = selectedProfile.avatarVolumetric;
            avatarVolumetricSlider.SetValueWithoutNotify(val);
            avatarVolumetricInput.text = val.ToString();

            ignoreInput = false;
        }

        void _RefreshSyncSettingsControl()
        {
            ignoreInput = true;
            syncSettingsToggle.isOn = SyncSettings;
            ignoreInput = false;
        }

        public void _UpdateSyncSettings()
        {
            if (accessControl && !accessControl._LocalHasAccess())
            {
                ignoreInput = true;
                syncSettingsToggle.isOn = SyncSettings;
                ignoreInput = false;
                return;
            }

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            SyncSettings = syncSettingsToggle.isOn;

            if (SyncSettings)
                _SyncProfiles();
        }

        void _UpdateCommon()
        {
            if (SyncSettings)
            {
                if (accessControl && !accessControl._LocalHasAccess())
                    return;
            }

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

            float avatarGain = 0;
            if (float.TryParse(avatarGainInput.text, out avatarGain))
                selectedEditor._SetAvatarGain(avatarGain);

            float avatarFar = 0;
            if (float.TryParse(avatarFarInput.text, out avatarFar))
                selectedEditor._SetAvatarFar(avatarFar);

            float avatarNear = 0;
            if (float.TryParse(avatarNearInput.text, out avatarNear))
                selectedEditor._SetAvatarNear(avatarNear);

            float avatarVolumetric = 0;
            if (float.TryParse(avatarVolumetricInput.text, out avatarVolumetric))
                selectedEditor._SetAvatarVolumetric(avatarVolumetric);

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

        public void _UpdateSliderAvatarGain()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetAvatarGain(avatarGainSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderAvatarFar()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetAvatarFar(avatarFarSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderAvatarNear()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetAvatarNear(avatarNearSlider.value);
                _RefreshControls();
            }
        }

        public void _UpdateSliderAvatarVolumetric()
        {
            if (selectedProfile && !ignoreInput)
            {
                _UpdateCommon();
                selectedEditor._SetAvatarVolumetric(avatarVolumetricSlider.value);
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

        public void _ResetAoiceDefault()
        {
            if (selectedProfile)
            {
                _UpdateCommon();
                selectedEditor._RestoreAvatarDefault();
                _RefreshControls();
            }
        }

        public void _ResetAvatarVRC()
        {
            if (selectedProfile)
            {
                _UpdateCommon();
                selectedEditor._RestoreAvatarVRC();
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

            if (SyncSettings)
            {
                if (!accessControl || accessControl._LocalHasAccess())
                {
                    // Ownership set on physical interaction, so will be false if triggered form deserialize
                    if (Networking.IsOwner(gameObject))
                        _SyncProfiles();
                }
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            base.OnDeserialization(result);

            if (syncSyncSettings)
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
                syncAvatarGain[i] = profiles[i].avatarGain;
                syncAvatarFar[i] = profiles[i].avatarFar;
                syncAvatarNear[i] = profiles[i].avatarNear;
                syncAvatarVolumetric[i] = profiles[i].avatarVolumetric;
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
                profiles[i].avatarGain = syncAvatarGain[i];
                profiles[i].avatarFar = syncAvatarFar[i];
                profiles[i].avatarNear = syncAvatarNear[i];
                profiles[i].avatarVolumetric = syncAvatarVolumetric[i];
            }

            manager._RebuildLocal();
        }
    }
}
