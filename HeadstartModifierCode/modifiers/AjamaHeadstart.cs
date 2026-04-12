using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
// ReSharper disable LoopCanBeConvertedToQuery

namespace HeadstartModifier.HeadstartModifierCode.modifiers;

public class AjamaHeadstart : ModifierModel
{
    protected override string IconPath => "res://HeadstartModifier/images/modifiers/headstart.png";

    public override bool ClearsPlayerDeck => true;

    private static int _cardsToChoose = 9;
    
    // TODO: Add config for changing these
    private static int _numberOfCharacterCardOptions = 29;
    private static int _guaranteedRareCharacterCards = 1;
    private static int _numberOfColorlessCardOptions = 5;
    
    private static int _numberOfStrikes = 2;
    private static int _numberOfDefends = 2;

    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => ChooseCards(eventModel.Owner!);
    }

    private static async Task ChooseCards(Player player)
    {
        #region Basics (Added immediately)
        
        CardModel strike = GetStrikeForCharacter(player.Character);
        CardModel defend = GetDefendForCharacter(player.Character);

        IEnumerable<CardModel> basicCards = [];

        for (int i = 0; i < _numberOfStrikes; i++)
        {
            basicCards = [..basicCards, player.RunState.CreateCard(strike, player)];
        }
        
        for (int i = 0; i < _numberOfDefends; i++)
        {
            basicCards = [..basicCards, player.RunState.CreateCard(defend, player)];
        }
        
        foreach (CardModel starter in GetUniqueStartersForCharacter(player.Character))
        {
            basicCards = [..basicCards, player.RunState.CreateCard(starter, player)];
        }
        
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(basicCards, PileType.Deck), 0.6f);
        
        #endregion
        
        #region Character Cards for Selection Screen
        
        CardCreationOptions characterCardOptions = new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter).WithFlags(CardCreationFlags.ForceRarityOddsChange | CardCreationFlags.NoUpgradeRoll);
        
        IEnumerable<CardCreationResult> characterCards = CardFactory.CreateForReward(player, _numberOfCharacterCardOptions, characterCardOptions);
        
        // ReSharper disable once AccessToModifiedClosure
        CardCreationOptions characterRareCardOptions = new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.Uniform, 
            c => c.Rarity == CardRarity.Rare && 
                 IsNotRareAlreadyChosen(c, characterCards
                .Where(r => r.originalCard.Rarity == CardRarity.Rare)))
            .WithFlags(CardCreationFlags.NoUpgradeRoll);

        IEnumerable<CardCreationResult> characterRareCards = CardFactory.CreateForReward(player, _guaranteedRareCharacterCards, characterRareCardOptions).ToList();

        characterCards = characterCards.Concat(characterRareCards)
            .ToList()
            .OrderBy(r => r.Card.Rarity)
            .ThenBy((Func<CardCreationResult, string>) (r => r.Card.Title));
        
        #endregion

        #region Colorless Cards for Selection Screen

        CardCreationOptions colorlessCardOptions = CardCreationOptions.ForNonCombatWithUniformOdds([ModelDb.CardPool<ColorlessCardPool>()]).WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);
        
        IEnumerable<CardCreationResult> colorlessCards = CardFactory.CreateForReward(player, _numberOfColorlessCardOptions, colorlessCardOptions)
            .ToList()
            .OrderBy(r => r.Card.Rarity)
            .ThenBy((Func<CardCreationResult, string>) (r => r.Card.Title));

        #endregion
        
        #region Selection Screen
        
        IEnumerable<CardCreationResult> cardsToChooseFrom = [..characterCards, ..colorlessCards];

        CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("modifiers", "AJAMA_HEADSTART.selectionPrompt"), _cardsToChoose)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        
        IEnumerable<CardModel> chosenCards = (await CardSelectCmd.FromSimpleGridForRewards(new BlockingPlayerChoiceContext(), cardsToChooseFrom.ToList(), player, prefs));
        
        #endregion
        
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosenCards, PileType.Deck), 1.2f, CardPreviewStyle.GridLayout);
    }

    private static bool IsNotRareAlreadyChosen(CardModel card, IEnumerable<CardCreationResult> alreadyChosen)
    {
        foreach (CardCreationResult alreadyChosenResult in alreadyChosen)
        {
            if (card.Id == alreadyChosenResult.originalCard.Id)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static CardModel GetStrikeForCharacter(CharacterModel character)
    {
        return character.CardPool.AllCards.First(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike));
    }

    private static CardModel GetDefendForCharacter(CharacterModel character)
    {
        return character.CardPool.AllCards.First(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Defend));
    }

    private static IEnumerable<CardModel> GetUniqueStartersForCharacter(CharacterModel character)
    {
        return character.CardPool.AllCards.Where(c =>
            c.Rarity == CardRarity.Basic && 
            !(
                c.Tags.Contains(CardTag.Strike) || 
                c.Tags.Contains(CardTag.Defend)
            ));
    }
}