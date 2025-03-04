using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonoMod.Utils;
using UnityEngine;

namespace CultOfQoL;

[HarmonyPatch]
public static class FastCollectingPatches
{
    [HarmonyPatch(typeof(Interaction_CollectResourceChest), "Update")]
    [HarmonyPrefix]
    public static void InteractionCollectResourceChestPrefix(ref Interaction_CollectResourceChest __instance)
    {
        var triggerExists = __instance.StructureInfo.Inventory.Exists(a => a.quantity >= Plugin.TriggerAmount.Value);
        __instance.AutomaticallyInteract = false;
        if (Plugin.EnableAutoInteract.Value && (__instance.StructureInfo.Inventory.Count >= Plugin.TriggerAmount.Value || triggerExists))
        {
            __instance.Activating = true;
            __instance.DistanceToTriggerDeposits = Plugin.IncreaseRange.Value ? 10f : 5f;
            __instance.AutomaticallyInteract = true;
        }
    }


    [HarmonyPatch(typeof(LumberjackStation), "Update")]
    [HarmonyPrefix]
    public static void LumberjackStationPrefix(ref LumberjackStation __instance)
    {
        var triggerExists = __instance.StructureInfo.Inventory.Exists(a => a.quantity >= Plugin.TriggerAmount.Value);
        __instance.AutomaticallyInteract = false;
        if (Plugin.EnableAutoInteract.Value && (__instance.StructureInfo.Inventory.Count >= Plugin.TriggerAmount.Value || triggerExists))
        {
            __instance.Activating = true;
            __instance.DistanceToTriggerDeposits = Plugin.IncreaseRange.Value ? 10f : 5f;
            __instance.AutomaticallyInteract = true;
        }
    }

    [HarmonyPatch(typeof(BuildingShrine), nameof(BuildingShrine.Update))]
    public static class BuildingShrineOnInteractPatches
    {
        [HarmonyPrefix]
        public static void Prefix(ref float ___ReduceDelay, ref float ___Delay)
        {
            if (!Plugin.FastCollecting.Value) return;
            ___ReduceDelay = 0.0f;
            ___Delay = 0.0f;
        }

        [HarmonyPostfix]
        public static void Postfix(ref float ___ReduceDelay, ref float ___Delay)
        {
            if (!Plugin.FastCollecting.Value) return;
            ___ReduceDelay = 0.0f;
            ___Delay = 0.0f;
        }
    }


    [HarmonyPatch(typeof(BuildingShrinePassive), nameof(BuildingShrinePassive.Update))]
    public static class BuildingShrinePassiveOnInteractPatches
    {
        [HarmonyPrefix]
        public static void Prefix(ref float ___Delay)
        {
            if (!Plugin.FastCollecting.Value) return;
            ___Delay = 0.0f;
        }

        [HarmonyPostfix]
        public static void Postfix(ref float ___Delay)
        {
            if (!Plugin.FastCollecting.Value) return;
            ___Delay = 0.0f;
        }
    }

    [HarmonyPatch]
    public static class LootDelayTranspilers
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Interaction_Bed), "GiveReward", MethodType.Enumerator)]
        [HarmonyPatch(typeof(Interaction_CollectedResources), "GiveResourcesRoutine", MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> TranspilerOne(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
        {
            if (!Plugin.FastCollecting.Value) return instructions;
    
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(WaitForSeconds), new[] {typeof(float)})))
                .SetOperandAndAdvance(originalMethod.GetRealDeclaringType().Name.Contains("Resources") ? 0.01f : 0f)
                .InstructionEnumeration();
        }
    
        //collection speed for Interaction_CollectResourceChest - default speed is 0.1f
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Interaction_CollectResourceChest), nameof(Interaction_CollectResourceChest.Update))]
        [HarmonyPatch(typeof(LumberjackStation), nameof(LumberjackStation.Update))]
        public static IEnumerable<CodeInstruction> InteractionCollectResourceChestTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
        {
            if (!Plugin.FastCollecting.Value) return instructions;
            var delayField = AccessTools.Field(originalMethod.GetRealDeclaringType(), "Delay");
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Stfld, delayField))
                .SetOperandAndAdvance(originalMethod.GetRealDeclaringType().Name.Contains("Lumber") ? 0.025f : 0.01f)
                .InstructionEnumeration();
        }
    
    
        //collection speed for Interaction_EntranceShrine (dungeon shrines) - default speed is 0.1f
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Interaction_RatauShrine), nameof(Interaction_RatauShrine.Update))]
        [HarmonyPatch(typeof(Interaction_EntranceShrine), nameof(Interaction_EntranceShrine.Update))]
        [HarmonyPatch(typeof(Interaction_Outhouse), nameof(Interaction_Outhouse.Update))]
        public static IEnumerable<CodeInstruction> InteractionEntranceShrineTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
        {
            if (!Plugin.FastCollecting.Value) return instructions;
            var delayField = AccessTools.Field(originalMethod.GetRealDeclaringType(), "Delay");
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Stfld, delayField))
                .SetOperandAndAdvance(originalMethod.GetRealDeclaringType().Name.Contains("Outhouse") ? 0.025f : 0f)
                .InstructionEnumeration();
        }
    }
}