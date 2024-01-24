
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioProfileEditorSelect : UdonSharpBehaviour
    {
        public Image buttonImage;
        public Text profileNameText;

        AudioProfileEditor editor;
        AudioOverrideSettings profile;
        int index = 0;

        Color activeColor = new Color(.9f, .9f, .9f);
        Color inactiveColor = new Color(.6f, .6f, .6f);

        float defaultVoiceGain;
        float defaultVoiceFar;
        float defaultVoiceNear;
        float defaultVoiceVolumetric;
        bool defaultVoiceLowpass;

        float defaultAvatarGain;
        float defaultAvatarFar;
        float defaultAvatarNear;
        float defaultAvatarVolumetric;

        bool profileChanged = false;

        void Start()
        {

        }

        public void _Bind(AudioProfileEditor editor, AudioOverrideSettings profile, int index)
        {
            this.editor = editor;
            this.profile = profile;
            this.index = index;

            if (!profile)
                return;

            defaultVoiceGain = profile.voiceGain;
            defaultVoiceFar = profile.voiceFar;
            defaultVoiceNear = profile.voiceNear;
            defaultVoiceVolumetric = profile.voiceVolumetric;
            defaultVoiceLowpass = profile.voiceLowpass;

            defaultAvatarGain = profile.avatarGain;
            defaultAvatarFar = profile.avatarFar;
            defaultAvatarNear = profile.avatarNear;
            defaultAvatarVolumetric = profile.avatarVolumetric;
        }

        public void _RestoreVoiceDefault()
        {
            if (!profile)
                return;

            _SetVoiceGain(defaultVoiceGain);
            _SetVoiceFar(defaultVoiceFar);
            _SetVoiceNear(defaultVoiceNear);
            _SetVoiceVolumetric(defaultVoiceVolumetric);
            _SetVoiceLowPass(defaultVoiceLowpass);
        }

        public void _RestoreVoiceVRC()
        {
            if (!profile)
                return;

            _SetVoiceGain(15);
            _SetVoiceFar(25);
            _SetVoiceNear(0);
            _SetVoiceVolumetric(0);
            _SetVoiceLowPass(true);
        }

        public void _RestoreAvatarDefault()
        {
            if (!profile)
                return;

            _SetAvatarGain(defaultAvatarGain);
            _SetAvatarFar(defaultAvatarFar);
            _SetAvatarNear(defaultAvatarNear);
            _SetAvatarVolumetric(defaultAvatarVolumetric);
        }

        public void _RestoreAvatarVRC()
        {
            if (!profile)
                return;

            _SetAvatarGain(10);
            _SetAvatarFar(40);
            _SetAvatarNear(0);
            _SetAvatarVolumetric(0);
        }

        public void _SetVoiceGain(float val)
        {
            val = Mathf.Clamp(val, 0, 24);
            if (_CheckDiff(profile.voiceGain, val))
                profile.voiceGain = val;
        }

        public void _SetVoiceFar(float val)
        {
            val = Mathf.Clamp(val, 0, 1000000);
            if (_CheckDiff(profile.voiceFar, val))
            {
                profile.voiceFar = val;

                _SetVoiceNear(profile.voiceNear);
                _SetVoiceVolumetric(profile.voiceVolumetric);
            }
        }

        public void _SetVoiceNear(float val)
        {
            val = Mathf.Clamp(val, 0, Mathf.Min(1000000, profile.voiceFar));
            if (_CheckDiff(profile.voiceNear, val))
                profile.voiceNear = val;
        }

        public void _SetVoiceVolumetric(float val)
        {
            val = Mathf.Clamp(val, 0, Mathf.Min(1000, profile.voiceFar));
            if (_CheckDiff(profile.voiceVolumetric, val))
                profile.voiceVolumetric = val;
        }

        public void _SetVoiceLowPass(bool val)
        {
            if (_CheckDiff(profile.voiceLowpass ? 1 : 0, val ? 1 : 0))
                profile.voiceLowpass = val;
        }

        public void _SetAvatarGain(float val)
        {
            val = Mathf.Clamp(val, 0, 10);
            if (_CheckDiff(profile.avatarGain, val))
                profile.avatarGain = val;
        }

        public void _SetAvatarFar(float val)
        {
            val = Mathf.Clamp(val, 0, 1000000);
            if (_CheckDiff(profile.avatarFar, val))
            {
                profile.avatarFar = val;

                _SetAvatarNear(profile.avatarNear);
                _SetAvatarVolumetric(profile.avatarVolumetric);
            }
        }

        public void _SetAvatarNear(float val)
        {
            val = Mathf.Clamp(val, 0, Mathf.Min(1000000, profile.avatarFar));
            if (_CheckDiff(profile.avatarNear, val))
                profile.avatarNear = val;
        }

        public void _SetAvatarVolumetric(float val)
        {
            val = Mathf.Clamp(val, 0, Mathf.Min(1000000, profile.avatarFar));
            if (_CheckDiff(profile.avatarVolumetric, val))
                profile.avatarVolumetric = val;
        }

        bool _CheckDiff(float val1, float val2)
        {
            bool diff = val1 != val2;
            if (diff && !profileChanged)
            {
                profileChanged = true;
                SendCustomEventDelayedSeconds(nameof(_InternalProfileChanged), 1);
            }

            return diff;
        }

        public void _InternalProfileChanged()
        {
            if (editor)
                editor._UpdateManager();

            profileChanged = false;
        }

        public void _SetName(string name)
        {
            if (profileNameText)
                profileNameText.text = name;
        }

        public void _EditorDeselect()
        {
            if (buttonImage)
                buttonImage.color = inactiveColor;
        }

        public void _EditorSelect()
        {
            if (buttonImage)
                buttonImage.color = activeColor;
        }

        public void _Select()
        {
            if (editor)
                editor._SelectProfile(index);
        }
    }
}
