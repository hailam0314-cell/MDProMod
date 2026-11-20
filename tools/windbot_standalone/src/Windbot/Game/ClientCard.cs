using System.Collections.Generic;
using System.IO;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game;

public class ClientCard
{
	public ClientCard EquipTarget;

	public int Id { get; private set; }

	public NamedCard Data { get; private set; }

	public string Name { get; private set; }

	public int Position { get; set; }

	public int Sequence { get; set; }

	public CardLocation Location { get; set; }

	public CardLocation LastLocation { get; set; }

	public int Alias { get; private set; }

	public int Level { get; private set; }

	public int Rank { get; private set; }

	public int Type { get; private set; }

	public int Attribute { get; private set; }

	public int Race { get; private set; }

	public int Attack { get; private set; }

	public int Defense { get; private set; }

	public int LScale { get; private set; }

	public int RScale { get; private set; }

	public int LinkCount { get; private set; }

	public int LinkMarker { get; private set; }

	public int BaseAttack { get; private set; }

	public int BaseDefense { get; private set; }

	public int RealPower { get; set; }

	public List<int> Overlays { get; private set; }

	public int Owner { get; private set; }

	public int Controller { get; set; }

	public int Disabled { get; private set; }

	public int ProcCompleted { get; private set; }

	public int SelectSeq { get; set; }

	public int OpParam1 { get; set; }

	public int OpParam2 { get; set; }

	public List<ClientCard> EquipCards { get; set; }

	public List<ClientCard> OwnTargets { get; set; }

	public List<ClientCard> TargetCards { get; set; }

	public bool CanDirectAttack { get; set; }

	public bool ShouldDirectAttack { get; set; }

	public bool Attacked { get; set; }

	public bool IsLastAttacker { get; set; }

	public bool IsSpecialSummoned { get; set; }

	public int[] ActionIndex { get; set; }

	public IDictionary<int, int> ActionActivateIndex { get; private set; }

	public ClientCard(int id, CardLocation loc, int sequence)
		: this(id, loc, -1, 0)
	{
	}

	public ClientCard(int id, CardLocation loc, int sequence, int position)
	{
		SetId(id);
		Sequence = sequence;
		Position = position;
		Overlays = new List<int>();
		EquipCards = new List<ClientCard>();
		OwnTargets = new List<ClientCard>();
		TargetCards = new List<ClientCard>();
		ActionIndex = new int[16];
		ActionActivateIndex = new Dictionary<int, int>();
		Location = loc;
		LastLocation = (CardLocation)0;
	}

	public void SetId(int id)
	{
		if (Id != id)
		{
			Id = id;
			Data = NamedCard.Get(Id);
			if (Data != null)
			{
				Name = Data.Name;
				Alias = Data.Alias;
			}
			else
			{
				Name = null;
				Alias = 0;
			}
		}
	}

	public void Update(BinaryReader packet, Duel duel)
	{
		int num = packet.ReadInt32();
		if ((num & 1) != 0)
		{
			SetId(packet.ReadInt32());
		}
		if ((num & 2) != 0)
		{
			Controller = duel.GetLocalPlayer(packet.ReadByte());
			Location = (CardLocation)packet.ReadByte();
			Sequence = packet.ReadByte();
			Position = packet.ReadByte();
		}
		if ((num & 4) != 0)
		{
			Alias = packet.ReadInt32();
		}
		if ((num & 8) != 0)
		{
			Type = packet.ReadInt32();
		}
		if ((num & 0x10) != 0)
		{
			Level = packet.ReadInt32();
		}
		if ((num & 0x20) != 0)
		{
			Rank = packet.ReadInt32();
		}
		if ((num & 0x40) != 0)
		{
			Attribute = packet.ReadInt32();
		}
		if ((num & 0x80) != 0)
		{
			Race = packet.ReadInt32();
		}
		if ((num & 0x100) != 0)
		{
			Attack = packet.ReadInt32();
		}
		if ((num & 0x200) != 0)
		{
			Defense = packet.ReadInt32();
		}
		if ((num & 0x400) != 0)
		{
			BaseAttack = packet.ReadInt32();
		}
		if ((num & 0x800) != 0)
		{
			BaseDefense = packet.ReadInt32();
		}
		if ((num & 0x1000) != 0)
		{
			packet.ReadInt32();
		}
		if ((num & 0x2000) != 0)
		{
			packet.ReadInt32();
		}
		if ((num & 0x4000) != 0)
		{
			packet.ReadInt32();
		}
		if ((num & 0x8000) != 0)
		{
			int num2 = packet.ReadInt32();
			for (int i = 0; i < num2; i++)
			{
				packet.ReadInt32();
			}
		}
		if ((num & 0x10000) != 0)
		{
			Overlays.Clear();
			int num3 = packet.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				Overlays.Add(packet.ReadInt32());
			}
		}
		if ((num & 0x20000) != 0)
		{
			int num4 = packet.ReadInt32();
			for (int k = 0; k < num4; k++)
			{
				packet.ReadInt32();
			}
		}
		if ((num & 0x40000) != 0)
		{
			Owner = duel.GetLocalPlayer(packet.ReadInt32());
		}
		if ((num & 0x80000) != 0)
		{
			int num5 = packet.ReadInt32();
			Disabled = num5 & 1;
			ProcCompleted = num5 & 8;
		}
		if ((num & 0x200000) != 0)
		{
			LScale = packet.ReadInt32();
		}
		if ((num & 0x400000) != 0)
		{
			RScale = packet.ReadInt32();
		}
		if ((num & 0x800000) != 0)
		{
			LinkCount = packet.ReadInt32();
			LinkMarker = packet.ReadInt32();
		}
	}

	public void ClearCardTargets()
	{
		foreach (ClientCard targetCard in TargetCards)
		{
			targetCard.OwnTargets.Remove(this);
		}
		foreach (ClientCard ownTarget in OwnTargets)
		{
			ownTarget.TargetCards.Remove(this);
		}
		OwnTargets.Clear();
		TargetCards.Clear();
	}

	public bool HasLinkMarker(int dir)
	{
		return (LinkMarker & dir) != 0;
	}

	public bool HasLinkMarker(CardLinkMarker dir)
	{
		return ((uint)LinkMarker & (uint)dir) != 0;
	}

	public int GetLinkedZones()
	{
		if (!HasType(CardType.Link) || Location != CardLocation.MonsterZone)
		{
			return 0;
		}
		int num = 0;
		if (Sequence > 0 && Sequence <= 4 && HasLinkMarker(CardLinkMarker.Left))
		{
			num |= 1 << Sequence - 1;
		}
		if (Sequence <= 3 && HasLinkMarker(CardLinkMarker.Right))
		{
			num |= 1 << Sequence + 1;
		}
		if ((Sequence == 0 && HasLinkMarker(CardLinkMarker.TopRight)) || (Sequence == 1 && HasLinkMarker(CardLinkMarker.Top)) || (Sequence == 2 && HasLinkMarker(CardLinkMarker.TopLeft)))
		{
			num |= 0x400020;
		}
		if ((Sequence == 2 && HasLinkMarker(CardLinkMarker.TopRight)) || (Sequence == 3 && HasLinkMarker(CardLinkMarker.Top)) || (Sequence == 4 && HasLinkMarker(CardLinkMarker.TopLeft)))
		{
			num |= 0x200040;
		}
		if (Sequence == 5)
		{
			if (HasLinkMarker(CardLinkMarker.BottomLeft))
			{
				num |= 1;
			}
			if (HasLinkMarker(CardLinkMarker.Bottom))
			{
				num |= 2;
			}
			if (HasLinkMarker(CardLinkMarker.BottomRight))
			{
				num |= 4;
			}
			if (HasLinkMarker(CardLinkMarker.TopLeft))
			{
				num |= 0x100000;
			}
			if (HasLinkMarker(CardLinkMarker.Top))
			{
				num |= 0x80000;
			}
			if (HasLinkMarker(CardLinkMarker.TopRight))
			{
				num |= 0x40000;
			}
		}
		if (Sequence == 6)
		{
			if (HasLinkMarker(CardLinkMarker.BottomLeft))
			{
				num |= 4;
			}
			if (HasLinkMarker(CardLinkMarker.Bottom))
			{
				num |= 8;
			}
			if (HasLinkMarker(CardLinkMarker.BottomRight))
			{
				num |= 0x10;
			}
			if (HasLinkMarker(CardLinkMarker.TopLeft))
			{
				num |= 0x40000;
			}
			if (HasLinkMarker(CardLinkMarker.Top))
			{
				num |= 0x20000;
			}
			if (HasLinkMarker(CardLinkMarker.TopRight))
			{
				num |= 0x10000;
			}
		}
		return num;
	}

	public bool HasType(CardType type)
	{
		return ((uint)Type & (uint)type) != 0;
	}

	public bool HasPosition(CardPosition position)
	{
		return ((uint)Position & (uint)position) != 0;
	}

	public bool HasAttribute(CardAttribute attribute)
	{
		return ((uint)Attribute & (uint)attribute) != 0;
	}

	public bool HasRace(CardRace race)
	{
		return ((uint)Race & (uint)race) != 0;
	}

	public bool HasSetcode(int setcode)
	{
		if (Data == null)
		{
			return false;
		}
		long num = Data.Setcode;
		int num2 = setcode & 0xFFF;
		int num3 = setcode & 0xF000;
		while (num > 0)
		{
			long num4 = num & 0xFFFF;
			num >>= 16;
			if ((num4 & 0xFFF) == num2 && (num4 & 0xF000 & num3) == num3)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsMonster()
	{
		return HasType(CardType.Monster);
	}

	public bool IsTuner()
	{
		return HasType(CardType.Tuner);
	}

	public bool IsSpell()
	{
		return HasType(CardType.Spell);
	}

	public bool IsTrap()
	{
		return HasType(CardType.Trap);
	}

	public bool IsExtraCard()
	{
		if (!HasType(CardType.Fusion) && !HasType(CardType.Synchro) && !HasType(CardType.Xyz))
		{
			return HasType(CardType.Link);
		}
		return true;
	}

	public bool IsFaceup()
	{
		return HasPosition(CardPosition.FaceUp);
	}

	public bool IsFacedown()
	{
		return HasPosition(CardPosition.FaceDown);
	}

	public bool IsAttack()
	{
		return HasPosition(CardPosition.Attack);
	}

	public bool IsDefense()
	{
		return HasPosition(CardPosition.Defence);
	}

	public bool IsDisabled()
	{
		return Disabled != 0;
	}

	public bool IsCanRevive()
	{
		if (ProcCompleted == 0)
		{
			if (!IsExtraCard() && !HasType(CardType.Ritual))
			{
				return !HasType(CardType.SpSummon);
			}
			return false;
		}
		return true;
	}

	public bool IsCode(int id)
	{
		if (Id != id)
		{
			if (Alias != 0)
			{
				return Alias == id;
			}
			return false;
		}
		return true;
	}

	public bool IsCode(IList<int> ids)
	{
		if (!ids.Contains(Id))
		{
			if (Alias != 0)
			{
				return ids.Contains(Alias);
			}
			return false;
		}
		return true;
	}

	public bool IsCode(params int[] ids)
	{
		if (!ids.Contains(Id))
		{
			if (Alias != 0)
			{
				return ids.Contains(Alias);
			}
			return false;
		}
		return true;
	}

	public bool IsOriginalCode(int id)
	{
		if (Id != id)
		{
			if (Alias - Id < 20)
			{
				return Alias == id;
			}
			return false;
		}
		return true;
	}

	public bool IsOnField()
	{
		if (Location != CardLocation.MonsterZone && Location != CardLocation.SpellZone && Location != CardLocation.PendulumZone)
		{
			return Location == CardLocation.FieldZone;
		}
		return true;
	}

	public bool HasXyzMaterial()
	{
		return Overlays.Count > 0;
	}

	public bool HasXyzMaterial(int count)
	{
		return Overlays.Count >= count;
	}

	public bool HasXyzMaterial(int count, int cardid)
	{
		if (Overlays.Count >= count)
		{
			return Overlays.Contains(cardid);
		}
		return false;
	}

	public int GetDefensePower()
	{
		if (!IsAttack())
		{
			return Defense;
		}
		return Attack;
	}

	public int GetOriginCode()
	{
		int result = Id;
		if (Data != null)
		{
			result = ((Data.Alias <= 0) ? Data.Id : Data.Alias);
		}
		return result;
	}

	public bool Equals(ClientCard card)
	{
		return this == card;
	}
}
