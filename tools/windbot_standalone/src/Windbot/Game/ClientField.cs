using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game;

public class ClientField
{
	public int LifePoints;

	public ClientCard BattlingMonster;

	public bool UnderAttack;

	public IList<ClientCard> Hand { get; private set; }

	public ClientCard[] MonsterZone { get; private set; }

	public ClientCard[] SpellZone { get; private set; }

	public IList<ClientCard> Graveyard { get; private set; }

	public IList<ClientCard> Banished { get; private set; }

	public IList<ClientCard> Deck { get; private set; }

	public IList<ClientCard> ExtraDeck { get; private set; }

	public HashSet<int> HintDescriptions { get; private set; }

	public void Init(int deck, int extra)
	{
		Hand = new List<ClientCard>();
		MonsterZone = new ClientCard[7];
		SpellZone = new ClientCard[8];
		Graveyard = new List<ClientCard>();
		Banished = new List<ClientCard>();
		Deck = new List<ClientCard>();
		ExtraDeck = new List<ClientCard>();
		HintDescriptions = new HashSet<int>();
		for (int i = 0; i < deck; i++)
		{
			Deck.Add(new ClientCard(0, CardLocation.Deck, -1));
		}
		for (int j = 0; j < extra; j++)
		{
			ExtraDeck.Add(new ClientCard(0, CardLocation.Extra, -1));
		}
	}

	public int GetMonstersExtraZoneCount()
	{
		int num = 0;
		if (MonsterZone[5] != null)
		{
			num++;
		}
		if (MonsterZone[6] != null)
		{
			num++;
		}
		return num;
	}

	public int GetMonsterCount()
	{
		return GetCount(MonsterZone);
	}

	public int GetSpellCount()
	{
		return GetCount(SpellZone);
	}

	public int GetHandCount()
	{
		return GetCount(Hand);
	}

	public int GetSpellCountWithoutField()
	{
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			if (SpellZone[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetColumnCount(int zone, bool IncludeExtraMonsterZone = true)
	{
		int num = 0;
		if (SpellZone[zone] != null)
		{
			num++;
		}
		if (MonsterZone[zone] != null)
		{
			num++;
		}
		if (zone == 1 && IncludeExtraMonsterZone && MonsterZone[5] != null)
		{
			num++;
		}
		if (zone == 3 && IncludeExtraMonsterZone && MonsterZone[6] != null)
		{
			num++;
		}
		return num;
	}

	public int GetFieldCount()
	{
		return GetSpellCount() + GetMonsterCount();
	}

	public int GetFieldHandCount()
	{
		return GetSpellCount() + GetMonsterCount() + GetHandCount();
	}

	public bool IsFieldEmpty()
	{
		if (GetMonsters().Count == 0)
		{
			return GetSpells().Count == 0;
		}
		return false;
	}

	public int GetLinkedZones()
	{
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			num |= MonsterZone[i]?.GetLinkedZones() ?? 0;
		}
		return num;
	}

	public List<ClientCard> GetMonsters()
	{
		return GetCards(MonsterZone);
	}

	public List<ClientCard> GetGraveyardMonsters()
	{
		return GetCards(Graveyard, CardType.Monster);
	}

	public List<ClientCard> GetGraveyardSpells()
	{
		return GetCards(Graveyard, CardType.Spell);
	}

	public List<ClientCard> GetGraveyardTraps()
	{
		return GetCards(Graveyard, CardType.Trap);
	}

	public List<ClientCard> GetSpells()
	{
		return GetCards(SpellZone);
	}

	public List<ClientCard> GetMonstersInExtraZone()
	{
		return (from card in GetMonsters()
			where card.Sequence >= 5
			select card).ToList();
	}

	public List<ClientCard> GetMonstersInMainZone()
	{
		return (from card in GetMonsters()
			where card.Sequence < 5
			select card).ToList();
	}

	public ClientCard GetFieldSpellCard()
	{
		return SpellZone[5];
	}

	public bool HasInHand(int cardId)
	{
		return HasInCards(Hand, cardId);
	}

	public bool HasInHand(IList<int> cardId)
	{
		return HasInCards(Hand, cardId);
	}

	public bool HasInGraveyard(int cardId)
	{
		return HasInCards(Graveyard, cardId);
	}

	public bool HasInGraveyard(IList<int> cardId)
	{
		return HasInCards(Graveyard, cardId);
	}

	public bool HasInBanished(int cardId)
	{
		return HasInCards(Banished, cardId);
	}

	public bool HasInBanished(IList<int> cardId)
	{
		return HasInCards(Banished, cardId);
	}

	public bool HasInExtra(int cardId)
	{
		return HasInCards(ExtraDeck, cardId);
	}

	public bool HasInExtra(IList<int> cardId)
	{
		return HasInCards(ExtraDeck, cardId);
	}

	public bool HasAttackingMonster()
	{
		return GetMonsters().Any((ClientCard card) => card.IsAttack());
	}

	public bool HasDefendingMonster()
	{
		return GetMonsters().Any((ClientCard card) => card.IsDefense());
	}

	public bool HasInMonstersZone(int cardId, bool notDisabled = false, bool hasXyzMaterial = false, bool faceUp = false)
	{
		return HasInCards(MonsterZone, cardId, notDisabled, hasXyzMaterial, faceUp);
	}

	public bool HasInMonstersZone(IList<int> cardId, bool notDisabled = false, bool hasXyzMaterial = false, bool faceUp = false)
	{
		return HasInCards(MonsterZone, cardId, notDisabled, hasXyzMaterial, faceUp);
	}

	public bool HasInSpellZone(int cardId, bool notDisabled = false, bool faceUp = false)
	{
		return HasInCards(SpellZone, cardId, notDisabled, hasXyzMaterial: false, faceUp);
	}

	public bool HasInSpellZone(IList<int> cardId, bool notDisabled = false, bool faceUp = false)
	{
		return HasInCards(SpellZone, cardId, notDisabled, hasXyzMaterial: false, faceUp);
	}

	public bool HasInHandOrInSpellZone(int cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInSpellZone(cardId);
		}
		return true;
	}

	public bool HasInHandOrHasInMonstersZone(int cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInMonstersZone(cardId);
		}
		return true;
	}

	public bool HasInHandOrInGraveyard(int cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInGraveyardOrInBanished(int cardId)
	{
		if (!HasInBanished(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInMonstersZoneOrInGraveyard(int cardId)
	{
		if (!HasInMonstersZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInSpellZoneOrInGraveyard(int cardId)
	{
		if (!HasInSpellZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInHandOrInMonstersZoneOrInGraveyard(int cardId)
	{
		if (!HasInHand(cardId) && !HasInMonstersZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInHandOrInSpellZoneOrInGraveyard(int cardId)
	{
		if (!HasInHand(cardId) && !HasInSpellZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInHandOrInSpellZone(IList<int> cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInSpellZone(cardId);
		}
		return true;
	}

	public bool HasInHandOrHasInMonstersZone(IList<int> cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInMonstersZone(cardId);
		}
		return true;
	}

	public bool HasInHandOrInGraveyard(IList<int> cardId)
	{
		if (!HasInHand(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInMonstersZoneOrInGraveyard(IList<int> cardId)
	{
		if (!HasInMonstersZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInSpellZoneOrInGraveyard(IList<int> cardId)
	{
		if (!HasInSpellZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInHandOrInMonstersZoneOrInGraveyard(IList<int> cardId)
	{
		if (!HasInHand(cardId) && !HasInMonstersZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public bool HasInHandOrInSpellZoneOrInGraveyard(IList<int> cardId)
	{
		if (!HasInHand(cardId) && !HasInSpellZone(cardId))
		{
			return HasInGraveyard(cardId);
		}
		return true;
	}

	public int GetRemainingCount(int cardId, int initialCount)
	{
		int num = initialCount;
		num -= Hand.Count((ClientCard card) => card?.IsOriginalCode(cardId) ?? false);
		num -= SpellZone.Count((ClientCard card) => card?.IsOriginalCode(cardId) ?? false);
		num -= MonsterZone.Count((ClientCard card) => card?.IsOriginalCode(cardId) ?? false);
		num -= Graveyard.Count((ClientCard card) => card?.IsOriginalCode(cardId) ?? false);
		num -= Banished.Count((ClientCard card) => card?.IsOriginalCode(cardId) ?? false);
		if (num >= 0)
		{
			return num;
		}
		return 0;
	}

	private static int GetCount(IEnumerable<ClientCard> cards)
	{
		return cards.Count((ClientCard card) => card != null);
	}

	public int GetCountCardInZone(IEnumerable<ClientCard> cards, int cardId)
	{
		return cards.Count((ClientCard card) => card?.IsCode(cardId) ?? false);
	}

	public int GetCountCardInZone(IEnumerable<ClientCard> cards, List<int> cardId)
	{
		return cards.Count((ClientCard card) => card?.IsCode(cardId) ?? false);
	}

	private static List<ClientCard> GetCards(IEnumerable<ClientCard> cards, CardType type)
	{
		return cards.Where((ClientCard card) => card?.HasType(type) ?? false).ToList();
	}

	private static List<ClientCard> GetCards(IEnumerable<ClientCard> cards)
	{
		return cards.Where((ClientCard card) => card != null).ToList();
	}

	private static bool HasInCards(IEnumerable<ClientCard> cards, int cardId, bool notDisabled = false, bool hasXyzMaterial = false, bool faceUp = false)
	{
		return cards.Any((ClientCard card) => card != null && card.IsCode(cardId) && (!notDisabled || !card.IsDisabled()) && (!hasXyzMaterial || card.HasXyzMaterial()) && (!faceUp || !card.IsFacedown()));
	}

	private static bool HasInCards(IEnumerable<ClientCard> cards, IList<int> cardId, bool notDisabled = false, bool hasXyzMaterial = false, bool faceUp = false)
	{
		return cards.Any((ClientCard card) => card != null && card.IsCode(cardId) && (!notDisabled || !card.IsDisabled()) && (!hasXyzMaterial || card.HasXyzMaterial()) && (!faceUp || !card.IsFacedown()));
	}
}
