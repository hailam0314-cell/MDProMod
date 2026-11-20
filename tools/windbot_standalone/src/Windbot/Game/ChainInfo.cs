using System;
using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game;

public class ChainInfo
{
	public ClientCard RelatedCard { get; private set; }

	public int ActivatePlayer { get; private set; }

	public int ActivateId { get; private set; }

	public int ActivateAlias { get; private set; }

	public int ActivateController { get; private set; }

	public int ActivatePosition { get; private set; }

	public int ActivateSequence { get; private set; }

	public CardLocation ActivateLocation { get; private set; }

	public int ActivateLevel { get; private set; }

	public int ActivateRank { get; private set; }

	public int ActivateType { get; private set; }

	public int ActivateRace { get; private set; }

	public int ActivateAttack { get; private set; }

	public int ActivateDefense { get; private set; }

	public bool IsSpecialSummoned { get; private set; }

	public int ActivateDescription { get; private set; }

	public ChainInfo(ClientCard card)
		: this(card, card.Controller, 0)
	{
	}

	public ChainInfo(ClientCard card, int player, int desc)
	{
		RelatedCard = card;
		ActivatePlayer = player;
		ActivateId = card.Id;
		ActivateAlias = card.Alias;
		ActivateController = card.Controller;
		ActivatePosition = card.Position;
		ActivateSequence = card.Sequence;
		ActivateLocation = card.Location;
		ActivateLevel = card.Level;
		ActivateRank = card.Rank;
		ActivateType = card.Type;
		ActivateRace = card.Race;
		ActivateAttack = card.Attack;
		ActivateDefense = card.Defense;
		ActivateAttack = card.Attack;
		ActivateDefense = card.Defense;
		IsSpecialSummoned = card.IsSpecialSummoned;
		ActivateDescription = desc;
	}

	public bool HasPosition(CardPosition position)
	{
		return ((uint)ActivatePosition & (uint)position) != 0;
	}

	public bool HasLocation(CardLocation location)
	{
		return (ActivateLocation & location) != 0;
	}

	public bool IsActivateCode(int id)
	{
		if (ActivateId != id)
		{
			if (Math.Abs(ActivateAlias - ActivateId) <= 20)
			{
				return ActivateAlias == id;
			}
			return false;
		}
		return true;
	}

	public bool IsCode(int id)
	{
		if (RelatedCard != null)
		{
			return RelatedCard.IsCode(id);
		}
		return false;
	}

	public bool IsCode(IList<int> ids)
	{
		if (RelatedCard != null)
		{
			return RelatedCard.IsCode(ids);
		}
		return false;
	}

	public bool IsCode(params int[] ids)
	{
		if (RelatedCard != null)
		{
			return RelatedCard.IsCode(ids);
		}
		return false;
	}

	public bool HasType(CardType type)
	{
		if (RelatedCard != null)
		{
			return ((uint)RelatedCard.Type & (uint)type) != 0;
		}
		return false;
	}

	public bool IsSpell()
	{
		return HasType(CardType.Spell);
	}

	public bool IsTrap()
	{
		return HasType(CardType.Trap);
	}
}
