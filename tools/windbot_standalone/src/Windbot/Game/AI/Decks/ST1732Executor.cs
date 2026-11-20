using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("ST1732", "AI_ST1732", "Normal")]
public class ST1732Executor : DefaultExecutor
{
	public class CardId
	{
		public const int Digitron = 32295838;

		public const int Bitron = 36211150;

		public const int DualAssembloom = 7445307;

		public const int BootStagguard = 70950698;

		public const int Linkslayer = 35595518;

		public const int RAMClouder = 9190563;

		public const int ROMCloudia = 44956694;

		public const int BalancerLord = 8567955;

		public const int Backlinker = 71172240;

		public const int Kleinant = 45778242;

		public const int Draconnet = 62706865;

		public const int DotScaper = 18789533;

		public const int MindControl = 37520316;

		public const int DarkHole = 53129443;

		public const int MonsterReborn = 83764718;

		public const int MysticalSpaceTyphoon = 5318639;

		public const int CosmicCyclone = 8267140;

		public const int BookOfMoon = 14087893;

		public const int CynetBackdoor = 43839002;

		public const int MoonMirrorShield = 19508728;

		public const int CynetUniverse = 61583217;

		public const int BottomlessTrapHole = 29401950;

		public const int MirrorForce = 44095762;

		public const int TorrentialTribute = 53582587;

		public const int RecodedAlive = 70238111;

		public const int DimensionalBarrier = 83326048;

		public const int CompulsoryEvacuationDevice = 94192409;

		public const int SolemnStrike = 40605147;

		public const int DecodeTalker = 1861629;

		public const int EncodeTalker = 6622715;

		public const int TriGateWizard = 32617464;

		public const int Honeybot = 34472920;

		public const int BinarySorceress = 79016563;

		public const int LinkSpider = 98978921;

		public const int StagToken = 70950699;
	}

	private bool BalancerLordUsed;

	public ST1732Executor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 8267140, base.DefaultCosmicCyclone);
		AddExecutor(ExecutorType.Activate, 5318639, base.DefaultMysticalSpaceTyphoon);
		AddExecutor(ExecutorType.Activate, 53129443, base.DefaultDarkHole);
		AddExecutor(ExecutorType.Activate, 14087893, base.DefaultBookOfMoon);
		AddExecutor(ExecutorType.Activate, 61583217, CynetUniverseEffect);
		AddExecutor(ExecutorType.SpSummon, 35595518);
		AddExecutor(ExecutorType.Activate, 35595518, LinkslayerEffect);
		AddExecutor(ExecutorType.SpSummon, 98978921);
		AddExecutor(ExecutorType.Activate, 98978921);
		AddExecutor(ExecutorType.Activate, 37520316, MindControlEffect);
		AddExecutor(ExecutorType.SpSummon, 71172240);
		AddExecutor(ExecutorType.Activate, 71172240, BacklinkerEffect);
		AddExecutor(ExecutorType.Activate, 70950698, BootStagguardEffect);
		AddExecutor(ExecutorType.Activate, 83764718, MonsterRebornEffect);
		AddExecutor(ExecutorType.Activate, 19508728, MoonMirrorShieldEffect);
		AddExecutor(ExecutorType.Activate, 43839002, CynetBackdoorEffect);
		AddExecutor(ExecutorType.Activate, 70238111);
		AddExecutor(ExecutorType.Summon, 8567955, BalancerLordSummon);
		AddExecutor(ExecutorType.Summon, 44956694, ROMCloudiaSummon);
		AddExecutor(ExecutorType.Activate, 44956694, ROMCloudiaEffect);
		AddExecutor(ExecutorType.Summon, 62706865, DraconnetSummon);
		AddExecutor(ExecutorType.Activate, 62706865, DraconnetEffect);
		AddExecutor(ExecutorType.Summon, 45778242);
		AddExecutor(ExecutorType.Activate, 45778242, KleinantEffect);
		AddExecutor(ExecutorType.Summon, 9190563);
		AddExecutor(ExecutorType.Activate, 9190563, RAMClouderEffect);
		AddExecutor(ExecutorType.SummonOrSet, 18789533);
		AddExecutor(ExecutorType.Activate, 18789533, DotScaperEffect);
		AddExecutor(ExecutorType.Summon, 8567955);
		AddExecutor(ExecutorType.Summon, 44956694);
		AddExecutor(ExecutorType.Summon, 62706865);
		AddExecutor(ExecutorType.SummonOrSet, 71172240);
		AddExecutor(ExecutorType.SummonOrSet, 32295838);
		AddExecutor(ExecutorType.SummonOrSet, 36211150);
		AddExecutor(ExecutorType.Activate, 8567955, BalancerLordEffect);
		AddExecutor(ExecutorType.SpSummon, 1861629, LinkSummon);
		AddExecutor(ExecutorType.Activate, 1861629);
		AddExecutor(ExecutorType.SpSummon, 32617464, LinkSummon);
		AddExecutor(ExecutorType.Activate, 32617464);
		AddExecutor(ExecutorType.SpSummon, 6622715, LinkSummon);
		AddExecutor(ExecutorType.Activate, 6622715);
		AddExecutor(ExecutorType.SpSummon, 34472920, LinkSummon);
		AddExecutor(ExecutorType.SpSummon, 79016563, LinkSummon);
		AddExecutor(ExecutorType.Activate, 79016563);
		AddExecutor(ExecutorType.SpellSet, 43839002, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 70238111, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 40605147, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 94192409, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 83326048, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 53582587, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 44095762, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 29401950, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 14087893, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 8267140, base.DefaultSpellSet);
		AddExecutor(ExecutorType.SpellSet, 5318639, base.DefaultSpellSet);
		AddExecutor(ExecutorType.Activate, 40605147, base.DefaultSolemnStrike);
		AddExecutor(ExecutorType.Activate, 94192409, base.DefaultCompulsoryEvacuationDevice);
		AddExecutor(ExecutorType.Activate, 83326048, base.DefaultDimensionalBarrier);
		AddExecutor(ExecutorType.Activate, 53582587, base.DefaultTorrentialTribute);
		AddExecutor(ExecutorType.Activate, 44095762, base.DefaultUniqueTrap);
		AddExecutor(ExecutorType.Activate, 29401950, base.DefaultUniqueTrap);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
	}

	public override bool OnSelectHand()
	{
		return false;
	}

	public override void OnNewTurn()
	{
		BalancerLordUsed = false;
		base.OnNewTurn();
	}

	public override int OnSelectOption(IList<int> options)
	{
		if (options.Count != 2)
		{
			return 0;
		}
		return 1;
	}

	public override bool OnSelectYesNo(int desc)
	{
		return desc switch
		{
			210 => false, 
			31 => true, 
			_ => base.OnSelectYesNo(desc), 
		};
	}

	private bool LinkslayerEffect()
	{
		IList<ClientCard> spells = base.Enemy.GetSpells();
		if (spells.Count > 0)
		{
			base.AI.SelectCard(7445307, 36211150, 32295838, 70238111);
			base.AI.SelectNextCard(spells);
			return true;
		}
		return false;
	}

	private bool MindControlEffect()
	{
		ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster();
		if (bestEnemyMonster != null)
		{
			base.AI.SelectCard(bestEnemyMonster);
			return true;
		}
		return false;
	}

	private bool BacklinkerEffect()
	{
		if (base.Bot.MonsterZone[5] == null)
		{
			return base.Bot.MonsterZone[6] == null;
		}
		return false;
	}

	private bool BootStagguardEffect()
	{
		if (base.Card.Location != CardLocation.Hand)
		{
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
		}
		return true;
	}

	private bool MonsterRebornEffect()
	{
		IList<int> list = new int[13]
		{
			1861629, 6622715, 32617464, 79016563, 34472920, 7445307, 70950698, 8567955, 44956694, 35595518,
			9190563, 71172240, 45778242
		};
		if (!base.Bot.HasInGraveyard(list))
		{
			return false;
		}
		base.AI.SelectCard(list);
		return true;
	}

	private bool MoonMirrorShieldEffect()
	{
		using (List<ClientCard>.Enumerator enumerator = base.Bot.GetMonsters().GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				ClientCard current = enumerator.Current;
				base.AI.SelectCard(current);
				return true;
			}
		}
		return false;
	}

	private bool CynetUniverseEffect()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			return DefaultField();
		}
		foreach (ClientCard item in base.Enemy.Graveyard)
		{
			if (item.IsMonster())
			{
				base.AI.SelectCard(item);
				return true;
			}
		}
		return false;
	}

	private bool CynetBackdoorEffect()
	{
		if ((base.Duel.Player != 0 || base.Duel.Phase != DuelPhase.Main2) && (base.Duel.Player != 1 || (base.Duel.Phase != DuelPhase.BattleStart && base.Duel.Phase != DuelPhase.End)))
		{
			return false;
		}
		if (!UniqueFaceupSpell())
		{
			return false;
		}
		bool flag = false;
		foreach (ClientCard item in base.Bot.GetMonstersInExtraZone())
		{
			if (item.Attack > 1000)
			{
				base.AI.SelectCard(item);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			List<ClientCard> monsters = base.Bot.GetMonsters();
			foreach (ClientCard item2 in monsters)
			{
				if (item2.IsCode(8567955))
				{
					base.AI.SelectCard(item2);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (ClientCard item3 in monsters)
				{
					if (item3.Attack >= 1700)
					{
						base.AI.SelectCard(item3);
						flag = true;
						break;
					}
				}
			}
		}
		if (flag)
		{
			base.AI.SelectNextCard(44956694, 8567955, 45778242, 62706865, 71172240);
			return true;
		}
		return false;
	}

	private bool BalancerLordSummon()
	{
		return !BalancerLordUsed;
	}

	private bool BalancerLordEffect()
	{
		if (base.Card.Location == CardLocation.Removed)
		{
			return true;
		}
		if (base.Bot.HasInHand(new int[6] { 62706865, 45778242, 8567955, 44956694, 9190563, 18789533 }) && !BalancerLordUsed)
		{
			BalancerLordUsed = true;
			return true;
		}
		return false;
	}

	private bool ROMCloudiaSummon()
	{
		return base.Bot.HasInGraveyard(new int[6] { 70950698, 8567955, 45778242, 35595518, 62706865, 9190563 });
	}

	private bool ROMCloudiaEffect()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			base.AI.SelectCard(70950698, 8567955, 45778242, 35595518, 62706865, 9190563);
			return true;
		}
		base.AI.SelectCard(8567955, 45778242, 9190563, 18789533);
		return true;
	}

	private bool DraconnetSummon()
	{
		if (base.Bot.GetRemainingCount(32295838, 1) <= 0)
		{
			return base.Bot.GetRemainingCount(36211150, 1) > 0;
		}
		return true;
	}

	private bool DraconnetEffect()
	{
		base.AI.SelectCard(36211150);
		return true;
	}

	private bool KleinantEffect()
	{
		IList<int> ids = new int[4] { 7445307, 36211150, 32295838, 18789533 };
		foreach (ClientCard item in base.Bot.Hand)
		{
			if (item.IsCode(ids))
			{
				base.AI.SelectCard(ids);
				return true;
			}
		}
		IList<int> ids2 = new int[4] { 70950699, 36211150, 32295838, 18789533 };
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (monster.IsCode(ids2))
			{
				base.AI.SelectCard(ids2);
				return true;
			}
		}
		return false;
	}

	private bool RAMClouderEffect()
	{
		base.AI.SelectCard(70950699, 36211150, 32295838, 18789533, 62706865, 71172240, 9190563);
		base.AI.SelectNextCard(1861629, 6622715, 32617464, 79016563, 34472920, 7445307, 70950698, 8567955, 44956694, 35595518, 9190563);
		return true;
	}

	private bool DotScaperEffect()
	{
		base.AI.SelectPosition(CardPosition.FaceUpDefence);
		return true;
	}

	private bool LinkSummon()
	{
		if (base.Util.IsTurn1OrMain2() || base.Util.IsOneEnemyBetter())
		{
			return base.Util.GetBestAttack(base.Bot) < base.Card.Attack;
		}
		return false;
	}
}
