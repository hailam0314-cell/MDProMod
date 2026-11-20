using System;
using System.Linq;
using WindBot.Game.AI.Enums;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI;

public static class CardExtension
{
	public static bool IsMonsterInvincible(this ClientCard card)
	{
		if (!card.IsDisabled())
		{
			if (card.Controller != 0 || !Enum.IsDefined(typeof(InvincibleBotMonster), card.Id))
			{
				if (card.Controller == 1)
				{
					return Enum.IsDefined(typeof(InvincibleEnemyMonster), card.Id);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool IsMonsterDangerous(this ClientCard card)
	{
		if (!card.IsDisabled())
		{
			if (!Enum.IsDefined(typeof(DangerousMonster), card.Id))
			{
				if (card.HasSetcode(397))
				{
					if (!card.HasType(CardType.Ritual))
					{
						return card.EquipCards.Count > 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool IsMonsterHasPreventActivationEffectInBattle(this ClientCard card)
	{
		if (!card.IsDisabled())
		{
			return Enum.IsDefined(typeof(PreventActivationEffectInBattle), card.Id);
		}
		return false;
	}

	public static bool IsShouldNotBeTarget(this ClientCard card)
	{
		if (!card.IsDisabled() && !card.HasType(CardType.Normal))
		{
			if (!Enum.IsDefined(typeof(ShouldNotBeTarget), card.Id))
			{
				return card.Overlays.Any((int code) => code == 91025875);
			}
			return true;
		}
		return false;
	}

	public static bool IsShouldNotBeMonsterTarget(this ClientCard card)
	{
		if (card.IsDisabled() || !Enum.IsDefined(typeof(ShouldNotBeMonsterTarget), card.Id))
		{
			return card.EquipCards.Any((ClientCard c) => c.IsCode(89812483) && !c.IsDisabled());
		}
		return true;
	}

	public static bool IsShouldNotBeSpellTrapTarget(this ClientCard card)
	{
		if (card.IsDisabled() || !Enum.IsDefined(typeof(ShouldNotBeSpellTrapTarget), card.Id))
		{
			return card.EquipCards.Any((ClientCard c) => c.IsCode(89812483) && !c.IsDisabled());
		}
		return true;
	}

	public static bool IsMonsterShouldBeDisabledBeforeItUseEffect(this ClientCard card)
	{
		if (!card.IsDisabled())
		{
			return Enum.IsDefined(typeof(ShouldBeDisabledBeforeItUseEffectMonster), card.Id);
		}
		return false;
	}

	public static bool IsFloodgate(this ClientCard card)
	{
		return Enum.IsDefined(typeof(Floodgate), card.Id);
	}

	public static bool IsOneForXyz(this ClientCard card)
	{
		return Enum.IsDefined(typeof(OneForXyz), card.Id);
	}

	public static bool IsFusionSpell(this ClientCard card)
	{
		return Enum.IsDefined(typeof(FusionSpell), card.Id);
	}

	public static bool IsMonsterNotBeSynchroMaterial(this ClientCard card)
	{
		return Enum.IsDefined(typeof(NotBeSynchroMaterialMonster), card.Id);
	}

	public static bool IsMonsterNotBeXyzMaterial(this ClientCard card)
	{
		return Enum.IsDefined(typeof(NotBeXyzMaterialMonster), card.Id);
	}
}
