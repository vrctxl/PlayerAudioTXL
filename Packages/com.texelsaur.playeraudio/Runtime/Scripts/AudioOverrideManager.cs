
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Audio/Audio Override Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioOverrideManager : UdonSharpBehaviour
    {
        public AudioOverrideZone defaultZone;
        public AudioOverrideZone[] overrideZones;

        [Header("Debug")]
        public AudioOverrideDebug debugState;

        bool waitForInit = true;
        int zoneCount = 0;
        VRCPlayerApi[] playerBuffer = new VRCPlayerApi[100];

        AudioOverrideZone[] cachedLocalZoneStack;
        int cachedLocalZoneCount = 0;
        bool rebuildLocalQueued = false;

        void Start()
        {
            if (debugState)
                debugState._SetManager(this);

            if (Utilities.IsValid(overrideZones))
                zoneCount = overrideZones.Length;

            cachedLocalZoneStack = new AudioOverrideZone[zoneCount];
            for (int i = 0; i < overrideZones.Length; i++)
            {
                if (Utilities.IsValid(overrideZones[i]))
                    overrideZones[i]._Register(this, i);
            }

            if (Utilities.IsValid(defaultZone))
                defaultZone._Register(this, -1);

            SendCustomEventDelayedSeconds("_RebuildLocal", 1f);

            _UpdateZoneData();
        }

        public void _SetDebugState(AudioOverrideDebug state)
        {
            debugState = state;
            if (debugState)
                debugState._SetManager(this);

            _UpdateZoneData();
        }

        public void _UpdateZoneData()
        {
            if (debugState)
                debugState._UpdateZoneData();
        }

        public void _PlayerEnterZone(AudioOverrideZone zone, VRCPlayerApi player)
        {
            if (waitForInit)
                return;

            //Debug.Log($"Player enter zone {zone._ZoneId()}");

            if (player.isLocal)
                _RebuildLocal();
            else
                _RebuildPlayer(player, cachedLocalZoneStack, cachedLocalZoneCount);
        }

        public void _PlayerLeaveZone(AudioOverrideZone zone, VRCPlayerApi player)
        {
            if (waitForInit)
                return;

            //Debug.Log($"Player leave zone {zone._ZoneId()}");

            if (player.isLocal)
                _RebuildLocal();
            else
                _RebuildPlayer(player, cachedLocalZoneStack, cachedLocalZoneCount);
        }

        public void _RebuildLocal()
        {
            waitForInit = false;

            if (!rebuildLocalQueued)
            {
                rebuildLocalQueued = true;
                SendCustomEventDelayedFrames("_CommitRebuildLocal", 1);
            }
        }

        public void _CommitRebuildLocal()
        {
            rebuildLocalQueued = false;

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return;

            _RebuildZoneCache(player);

            if (debugState)
                debugState._UpdateLocal(cachedLocalZoneCount > 0 ? cachedLocalZoneStack[0] : defaultZone);

            _RebuildAll(player, cachedLocalZoneStack, cachedLocalZoneCount);
        }

        void _RebuildAll(VRCPlayerApi localPlayer, AudioOverrideZone[] localZoneStack, int stackCount)
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            playerBuffer = VRCPlayerApi.GetPlayers(playerBuffer);

            for (int i = 0; i < playerCount; i++)
            {
                VRCPlayerApi player = playerBuffer[i];
                if (player == localPlayer)
                    continue;

                _RebuildPlayer(player, localZoneStack, stackCount);
            }
        }

        void _RebuildPlayer(VRCPlayerApi player, AudioOverrideZone[] localZoneStack, int stackCount)
        {
            for (int i = 0; i < stackCount; i++)
            {
                //Debug.Log($"Try Rebuild player {player.displayName} from {localZone.name}");
                if (localZoneStack[i]._Apply(player))
                    return;
            }

            //Debug.Log($"Try Rebuild player {player.displayName} from {defaultZone.name}");
            if (defaultZone && defaultZone._Apply(player))
                return;

            _ResetSettings(player);
        }

        public AudioOverrideZone _FindActiveZone(VRCPlayerApi player)
        {
            if (zoneCount <= 0)
                return null;

            for (int i = 0; i < overrideZones.Length; i++)
            {
                AudioOverrideZone zone = overrideZones[i];
                if (zone.zoneEnabled && zone.membership._ContainsPlayer(player))
                    return zone;
            }

            return null;
        }

        void _RebuildZoneCache(VRCPlayerApi player)
        {
            cachedLocalZoneCount = 0;

            for (int i = 0; i < overrideZones.Length; i++)
            {
                AudioOverrideZone zone = overrideZones[i];
                if (zone.zoneEnabled && zone.membership._ContainsPlayer(player))
                {
                    cachedLocalZoneStack[cachedLocalZoneCount] = zone;
                    cachedLocalZoneCount += 1;
                }
            }
        }

        void _ResetSettings(VRCPlayerApi player)
        {
            player.SetVoiceGain(15);
            player.SetVoiceDistanceNear(0);
            player.SetVoiceDistanceFar(25);
            player.SetVoiceLowpass(true);

            if (debugState)
                debugState._UpdatePlayer(player, "[reset]", "[reset]");
        }
    }
}
