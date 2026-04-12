using HarmonyLib;
using HeadstartModifier.HeadstartModifierCode.modifiers;
using MegaCrit.Sts2.Core.Models;
// ReSharper disable InconsistentNaming

namespace HeadstartModifier.HeadstartModifierCode.patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
public static class AddHeadstartModifierPatch
{
    public static IReadOnlyList<ModifierModel> Postfix(IReadOnlyList<ModifierModel> __result)
    {
        List<ModifierModel> goodModifiers = [..__result, ModelDb.GetById<ModifierModel>(ModelDb.GetId<AjamaHeadstart>())];

        return goodModifiers.AsReadOnly();
    }
}