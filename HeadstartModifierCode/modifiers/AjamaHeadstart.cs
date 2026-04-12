using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace HeadstartModifier.HeadstartModifierCode.modifiers;

public class Headstart : ModifierModel
{
    public override bool ClearsPlayerDeck => true;

    private static int _cardsToChoose = 9;
    
    private static int _numberOfCharacterCardOptions = 29;
    private static int _guaranteedRareCharacterCards = 10;
    private static int _numberOfColorlessCardOptions = 5;
    
    private static int _numberOfStrikes = 2;
    private static int _numberOfDefends = 2;

    private static async Task ChooseCards(Player player)
    {
        IEnumerable<CardModel> cardsToAdd = new List<CardModel>();
        
        #region Basics
        CardModel strike = GetStrikeForCharacter(player.Character);
        CardModel defend = GetDefendForCharacter(player.Character);

        IEnumerable<CardModel> basicCards = new List<CardModel>();

        for (int i = 0; i < _numberOfStrikes; i++)
        {
            basicCards = basicCards.Append(strike);
        }
        
        for (int i = 0; i < _numberOfDefends; i++)
        {
            basicCards = basicCards.Append(defend);
        }

        basicCards = basicCards.Concat(GetUniqueStartersForCharacter(player.Character));
        #endregion
        
        #region Character Cards for Selection Screen
        CardCreationOptions characterCardOptions = new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter).WithFlags(CardCreationFlags.ForceRarityOddsChange);
        
        IEnumerable<CardCreationResult> characterCards = CardFactory.CreateForReward(player, _numberOfCharacterCardOptions, characterCardOptions);
        
        CardCreationOptions characterRareCardOptions = new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.Uniform, c => c.Rarity == CardRarity.Rare).WithFlags(CardCreationFlags.NoUpgradeRoll);

        IEnumerable<CardCreationResult> characterRareCards = CardFactory.CreateForReward(player, _guaranteedRareCharacterCards, characterRareCardOptions).ToList();

        characterCards = characterCards.Concat(characterRareCards)
            .ToList()
            .OrderBy(r => r.Card.Rarity)
            .ThenBy((Func<CardCreationResult, string>) (r => r.Card.Title));;
        #endregion

        #region Selection Screen
        IEnumerable<CardCreationResult> cardsToChooseFrom = new List<CardCreationResult>();
        cardsToChooseFrom = cardsToChooseFrom.Concat(characterCards);

        CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("modifiers", "HEADSTARTMODIFIER-HEADSTART.selectionPrompt"), _cardsToChoose)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        
        IEnumerable<CardModel> chosenCards = (await CardSelectCmd.FromSimpleGridForRewards(new BlockingPlayerChoiceContext(), cardsToChooseFrom.ToList(), player, prefs));
        #endregion

        cardsToAdd = cardsToAdd.Concat(basicCards);
        cardsToAdd = cardsToAdd.Concat(chosenCards);
        
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardsToAdd, PileType.Deck), 1.2f, CardPreviewStyle.GridLayout);
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