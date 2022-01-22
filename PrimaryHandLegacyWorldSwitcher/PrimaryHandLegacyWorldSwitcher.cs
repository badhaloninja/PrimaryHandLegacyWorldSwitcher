using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using BaseX;
namespace PrimaryHandLegacyWorldSwitcher
{
    public class PrimaryHandLegacyWorldSwitcher : NeosMod
    {
        public override string Name => "PrimaryHandLegacyWorldSwitcher";
        public override string Author => "badhaloninja";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/badhaloninja/PrimaryHandLegacyWorldSwitcher";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.badhaloninja.PrimaryHandLegacyWorldSwitcher");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Userspace), "Bootstrap")]
        class Userspace_Bootstrap_Patch
        {
            public static void Postfix(Userspace __instance)
            {
                UserRoot localUserRoot = __instance.LocalUser.Root;

                CommonTool leftTool = GetTool(localUserRoot, Chirality.Left);
                CommonTool rightTool = GetTool(localUserRoot, Chirality.Right);

                Traverse.Create(leftTool).Field<Action<CommonTool>>("_userspaceToggle").Value = userspaceToggleAction;
                Traverse.Create(rightTool).Field<Action<CommonTool>>("_userspaceToggle").Value = userspaceToggleAction;
            }
            private static CommonTool GetTool(UserRoot targetUserRoot, Chirality side)
            {
                return targetUserRoot.GetRegisteredComponent<CommonTool>((CommonTool c) => c.Side.Value == side);
            }
            private static void userspaceToggleAction(CommonTool tool)
            {
                if (tool.IsRemoved)
                    return;


                var neosDash = tool.World.GetGloballyRegisteredComponent<UserspaceRadiantDash>();
                if (tool.Inputs.Grab.Held)
                {
                    if (tool.Side == Chirality.Left)
                    {
                        neosDash.ToggleLegacyInventory();
                        return;
                    }
                    neosDash.ToggleSessionControl();
                    return;
                }

                bool isPrimary = Settings.ReadValue<Chirality>("Input.User.PrimaryHand", Chirality.Right) == tool.Side.Value;
                bool worldSwitcherEnabled = Settings.ReadValue<bool>("Userspace.WorldSwitcher.Enabled", false);
                if (isPrimary && worldSwitcherEnabled)
                {
                    WorldSwitcher switcher = tool.Slot.GetComponentInChildren<WorldSwitcher>();
                    if (switcher == null)
                    { // Handle switching
                        switcher = GetTool(tool.LocalUserRoot, tool.OtherSide).Slot.GetComponentInChildren<WorldSwitcher>();
                        if (switcher == null) return;
                        Slot switcherRoot = switcher.Slot.Parent;
                        
                        rotateVoiceSwitcher(switcher.Slot, tool.Side.Value); // mirror voice control

                        if (switcher.Show.Value)
                        { // Animate if already open bc why not
                            switcherRoot.SetParent(tool.GrabIgnore);
                            switcherRoot.Position_Field.TweenTo(float3.Zero, 0.25f);
                            switcherRoot.Rotation_Field.TweenTo(floatQ.Identity, 0.25f);
                            switcherRoot.Scale_Field.TweenTo(float3.One, 0.25f);
                            return; 
                        }
                        switcherRoot.SetParent(tool.GrabIgnore, false);
                    }
                    switcher.Show.Value = !switcher.Show.Value;
                    return;
                }
                neosDash.Open = !neosDash.Open;
            }

            private static void rotateVoiceSwitcher(Slot slot, Chirality value)
            {
                var voiceModeSwitcher = slot.GetComponentInChildren<VoiceModeSwitcher>();
                
                var voiceModeSwitcherMenu = (voiceModeSwitcher.GetSyncMember(3) as SyncRef<NeosLogoMenuController>).Target;
                if (voiceModeSwitcherMenu == null) return;

                // Get each voice mode item
                var mute = voiceModeSwitcherMenu.GetIndependentItem(0);
                var whisper = voiceModeSwitcherMenu.GetIndependentItem(1);
                var normal = voiceModeSwitcherMenu.GetIndependentItem(2);// Center
                var shout = voiceModeSwitcherMenu.GetIndependentItem(3);
                var broadcast = voiceModeSwitcherMenu.GetIndependentItem(4);

                normal.AngleStart.Value = (value == Chirality.Left) ? 180f : 0;

                voiceModeSwitcherMenu.RemoveItemsArcAt(0);
                NeosLogoMenuController.ItemsArc itemsArc = voiceModeSwitcherMenu.AddItemsArc(normal);
                if(value == Chirality.Left)
                {
                    itemsArc.AddRight(whisper);
                    itemsArc.AddRight(mute);
                    itemsArc.AddLeft(shout);
                    itemsArc.AddLeft(broadcast);
                } else
                { // default
                    itemsArc.AddLeft(whisper);
                    itemsArc.AddLeft(mute);
                    itemsArc.AddRight(shout);
                    itemsArc.AddRight(broadcast);
                }
            }
        }
    }
}