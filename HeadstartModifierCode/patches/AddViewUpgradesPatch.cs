using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
// ReSharper disable InconsistentNaming
// ReSharper disable MoveLocalFunctionAfterJumpStatement

namespace HeadstartModifier.HeadstartModifierCode.patches;

// Credit to KHJGames and their work on Yu-Gi-Oh! Duelist for this patch

[HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
public class AddViewUpgradesPatch
{
    [HarmonyPriority(Priority.Low)]
    private static void Postfix(NSimpleCardSelectScreen __instance)
    {
        if (!Config.AddViewUpgradesButton) return;
        
        NCardGrid? cardGrid = __instance.GetNodeOrNull<NCardGrid>("%CardGrid");
        if (cardGrid == null) return;

        const string YgoViewUpgradesName = "YgoViewUpgradesRow";
        const string ViewUpgradesName = "ViewUpgradesRow";

        if (__instance.GetNodeOrNull(YgoViewUpgradesName) != null) return;
        if (__instance.GetNodeOrNull(ViewUpgradesName) != null) return;
        
        PackedScene? scene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/simple_cards_view_screen"));
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (scene == null) return;

        Control sacrifice = scene.Instantiate<Control>();
        Control viewUpgrades = sacrifice.GetNodeOrNull<Control>("ViewUpgrades");
        if (viewUpgrades == null)
        {
            sacrifice.QueueFree();
            return;
        }
        
        sacrifice.RemoveChild(viewUpgrades);
        viewUpgrades.Name = ViewUpgradesName;
        sacrifice.QueueFree();
        
        __instance.AddChild(viewUpgrades);

        NTickbox? tickbox = viewUpgrades.GetNodeOrNull<NTickbox>("MarginContainer/Upgrades");
        if (tickbox == null)
        {
            viewUpgrades.QueueFree();
            return;
        }
        
        MegaLabel? label = viewUpgrades.GetNodeOrNull<MegaLabel>("MarginContainer/Upgrades/ViewUpgradesLabel");
        label?.SetTextAutoSize(new LocString("gameplay_ui", "VIEW_UPGRADES").GetFormattedText());

        tickbox.IsTicked = false;
        cardGrid.IsShowingUpgrades = false;

        void Apply(NTickbox tb)
        {
            cardGrid.IsShowingUpgrades = tb.IsTicked;
        }

        tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(Apply));
        
        OnControllerUpdated();

        void OnControllerUpdated()
        {
            if (NControllerManager.Instance == null) return;
            if (!tickbox.IsValid()) return;
            
            tickbox.Visible = !NControllerManager.Instance.IsUsingController;

            // ReSharper disable once InvertIf
            if (NControllerManager.Instance.IsUsingController)
            {
                tickbox.IsTicked = false;
                Apply(tickbox);
            }
        }
        
        __instance._peekButton.Connect(NPeekButton.SignalName.Toggled, Callable.From<NPeekButton>(_ =>
        {
            if (NControllerManager.Instance == null) return;
            if (!tickbox.IsValid()) return;
            
            Control? colorRect = viewUpgrades.GetNodeOrNull<Control>("ColorRect");
            Control? marginContainer = viewUpgrades.GetNodeOrNull<Control>("MarginContainer");
            
            if (__instance._peekButton.IsPeeking)
            {
                tickbox.Visible = false;
                viewUpgrades.MouseFilter = Control.MouseFilterEnum.Ignore;
                if (colorRect != null) colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
                if (marginContainer != null) marginContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
                tickbox.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
            else
            {
                tickbox.Visible = !NControllerManager.Instance.IsUsingController;
                if (!NControllerManager.Instance.IsUsingController)
                {
                    viewUpgrades.MouseFilter = Control.MouseFilterEnum.Pass;
                    if (colorRect != null) colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
                    if (marginContainer != null) marginContainer.MouseFilter = Control.MouseFilterEnum.Pass;
                    tickbox.MouseFilter = Control.MouseFilterEnum.Pass;
                }
            }
        }));

        void OnTickboxDeleted()
        {
            if (NControllerManager.Instance == null) return;

            if (NControllerManager.Instance.IsConnected(NControllerManager.SignalName.MouseDetected, Callable.From(OnControllerUpdated)))
            {
                NControllerManager.Instance.Disconnect(NControllerManager.SignalName.MouseDetected, Callable.From(OnControllerUpdated));
            }
            
            if (NControllerManager.Instance.IsConnected(NControllerManager.SignalName.ControllerDetected, Callable.From(OnControllerUpdated)))
            {
                NControllerManager.Instance.Disconnect(NControllerManager.SignalName.ControllerDetected, Callable.From(OnControllerUpdated));
            }
        }

        // ReSharper disable once InvertIf
        if (NControllerManager.Instance != null)
        {
            NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(OnControllerUpdated));
            NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(OnControllerUpdated));
            tickbox.Connect(Node.SignalName.TreeExited, Callable.From(OnTickboxDeleted));
        }
    }
}