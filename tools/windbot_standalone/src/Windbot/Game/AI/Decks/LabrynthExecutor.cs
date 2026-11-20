using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Labrynth", "AI_Labrynth", "Normal")]
public class LabrynthExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int LadyLabrynthOfTheSilverCastle = 81497285;

		public const int LovelyLabrynthOfTheSilverCastle = 2347656;

		public const int UnchainedSoulOfSharvara = 41165831;

		public const int AriasTheLabrynthButler = 73602965;

		public const int ArianeTheLabrynthServant = 75730490;

		public const int AriannaTheLabrynthServant = 1225009;

		public const int LabrynthChandraglier = 37629703;

		public const int LabrynthStovieTorbie = 74018812;

		public const int LabrynthCooclock = 2511;

		public const int PotOfExtravagance = 49238328;

		public const int WelcomeLabrynth = 5380979;

		public const int TransactionRollback = 6351147;

		public const int DestructiveDarumaKarmaCannon = 30748475;

		public const int EscapeOfTheUnchained = 53417695;

		public const int BigWelcomeLabrynth = 92714517;

		public const int ChaosAngel = 22850702;

		public const int SuperStarslayerTYPHON = 93039339;

		public const int UnchainedAbomination = 29479265;

		public const int UnchainedSoulOfAnguish = 93084621;

		public const int UnchainedSoulLordOfYama = 24269961;

		public const int UnchainedSoulOfRage = 67680512;

		public const int SPLittleKnight = 29301450;

		public const int MuckrakerFromTheUnderworld = 71607202;

		public const int RelinquishedAnima = 94259633;

		public const int NaturalExterio = 99916754;

		public const int NaturalBeast = 33198837;

		public const int ImperialOrder = 61740673;

		public const int SwordsmanLV7 = 37267041;

		public const int RoyalDecree = 51452091;

		public const int Number41BagooskatheTerriblyTiredTapir = 90590303;

		public const int InspectorBoarder = 15397015;

		public const int SkillDrain = 82732705;

		public const int DimensionShifter = 91800273;

		public const int MacroCosmos = 30241314;

		public const int DimensionalFissure = 81674782;

		public const int BanisheroftheRadiance = 94853057;

		public const int BanisheroftheLight = 61528025;

		public const int KashtiraAriseHeart = 48626373;

		public const int AccesscodeTalker = 86066372;

		public const int GhostMournerMoonlitChill = 52038441;
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeUnchained = 304;

	private const int SetcodeLabrynth = 382;

	private const int SetcodeHorus = 413;

	private const int hintTimingMainEnd = 4;

	private const int hintBattleStart = 8;

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 1225009, 37629703, 14558127, 23434538, 74018812, 2511, 10045474, 92714517 }
		},
		{
			2,
			new List<int> { 81497285, 73602965, 49238328, 5380979, 6351147 }
		},
		{
			1,
			new List<int> { 2347656, 41165831, 75730490, 30748475, 53417695, 83326048 }
		}
	};

	private List<int> notToNegateIdList = new List<int> { 58699500, 20343502 };

	private List<int> notToBeTrapTargetList = new List<int>
	{
		72144675, 86188410, 41589166, 11443677, 72566043, 1688285, 59071624, 6511113, 48183890, 952523,
		22423493, 73639099
	};

	private List<int> targetNegateIdList = new List<int>
	{
		97268402, 10045474, 52038441, 78474168, 74003290, 67037924, 9753964, 66192538, 23204029, 73445448,
		35103106, 30286474, 45002991, 5795980, 38511382, 53742162, 30430448
	};

	private List<int> notToDestroySpellTrap = new List<int> { 50005218, 6767771 };

	private bool enemyActivateMaxxC;

	private List<int> infiniteImpermanenceList = new List<int>();

	private bool summoned;

	private List<int> activatedCardIdList = new List<int>();

	private List<ClientCard> currentNegateMonsterList = new List<ClientCard>();

	private List<ClientCard> currentDestroyCardList = new List<ClientCard>();

	private List<ClientCard> setTrapThisTurn = new List<ClientCard>();

	private List<ClientCard> summonThisTurn = new List<ClientCard>();

	private List<ClientCard> enemySetThisTurn = new List<ClientCard>();

	private List<ClientCard> escapeTargetList = new List<ClientCard>();

	private List<ClientCard> summonInChainList = new List<ClientCard>();

	private bool cooclockAffected;

	private bool cooclockActivating;

	private bool furnitureActivating;

	private bool dimensionBarrierAnnouncing;

	private int banSpSummonExceptFiendCount;

	private int dimensionShifterCount;

	private int enemySpSummonFromExLastTurn;

	private int enemySpSummonFromExThisTurn;

	private bool enemyActivateInfiniteImpermanenceFromHand;

	private int rollbackCopyCardId;

	private List<int> dimensionalBarrierAnnouced = new List<int>();

	private List<int> chainSummoningIdList = new List<int>(3);

	private ClientCard bigwelcomeEscaseTarget;

	public LabrynthExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 49238328, PotOfExtravaganceActivate);
		AddExecutor(ExecutorType.Repos, 2347656, ReposForLabrynth);
		AddExecutor(ExecutorType.Activate, 22850702, ChaosAngelActivate);
		AddExecutor(ExecutorType.Activate, 2347656, LovelyLabrynthOfTheSilverCastleActivate);
		AddExecutor(ExecutorType.Activate, 94259633, RelinquishedAnimaActivate);
		AddExecutor(ExecutorType.Activate, 1225009, AriannaTheLabrynthServantActivate);
		AddExecutor(ExecutorType.Activate, 75730490, ArianeTheLabrynthServantActivate);
		AddExecutor(ExecutorType.Activate, 37629703, RecycleActivate);
		AddExecutor(ExecutorType.Activate, 74018812, RecycleActivate);
		AddExecutor(ExecutorType.Activate, 2511, RecycleActivate);
		AddExecutor(ExecutorType.Activate, 24269961, UnchainedSoulLordOfYamaActivate);
		AddExecutor(ExecutorType.Activate, 5380979, RecycleActivate);
		AddExecutor(ExecutorType.Activate, 93039339, SuperStarslayerTYPHONActivate);
		AddExecutor(ExecutorType.Activate, 29479265, UnchainedAbominationActivate);
		AddExecutor(ExecutorType.Repos, 75730490, ReposForLabrynth);
		AddExecutor(ExecutorType.Repos, 1225009, ReposForLabrynth);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 81497285, LadyLabrynthOfTheSilverCastleFieldActivate);
		AddExecutor(ExecutorType.Activate, 73602965, RecycleActivate);
		AddExecutor(ExecutorType.Activate, 29301450, SPLittleKnightActivate);
		AddExecutor(ExecutorType.Activate, 83326048, DimensionalBarrierActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 71607202, MuckrakerFromTheUnderworldActivate);
		AddExecutor(ExecutorType.Activate, 67680512, UnchainedSoulOfRageActivate);
		AddExecutor(ExecutorType.Activate, 6351147, TransactionRollbackActivate);
		AddExecutor(ExecutorType.Activate, 30748475, DestructiveDarumaKarmaCannonActivate);
		AddExecutor(ExecutorType.Activate, 53417695, EscapeOfTheUnchainedActivate);
		AddExecutor(ExecutorType.Activate, 81497285, LadyLabrynthOfTheSilverCastleHandActivate);
		AddExecutor(ExecutorType.Activate, 92714517, BigWelcomeLabrynthBecomeTargetActivate);
		AddExecutor(ExecutorType.Activate, 5380979, WelcomeLabrynthActivate);
		AddExecutor(ExecutorType.Activate, 92714517, BigWelcomeLabrynthActivate);
		AddExecutor(ExecutorType.Activate, 73602965, AriasTheLabrynthButlerActivate);
		AddExecutor(ExecutorType.Activate, 2511, LabrynthCooclockActivate);
		AddExecutor(ExecutorType.Activate, 92714517, BigWelcomeLabrynthGraveActivate);
		AddExecutor(ExecutorType.Activate, 93084621, UnchainedSoulOfAnguishActivate);
		AddExecutor(ExecutorType.SpellSet, SpellSetForCooClockCheck);
		AddExecutor(ExecutorType.Summon, 75730490, ArianeTheLabrynthServantForRollbackSummon);
		AddExecutor(ExecutorType.Summon, 1225009, AriannaTheLabrynthServantSummon);
		AddExecutor(ExecutorType.Summon, 75730490, ArianeTheLabrynthServantSummon);
		AddExecutor(ExecutorType.Summon, LabrynthForCooClockSummon);
		AddExecutor(ExecutorType.Summon, ForLinkSummon);
		AddExecutor(ExecutorType.Summon, ForSynchroSummon);
		AddExecutor(ExecutorType.Summon, 2511, ForAnimaSummon);
		AddExecutor(ExecutorType.Activate, 37629703, FurnitureSetWelcomeActivate);
		AddExecutor(ExecutorType.Activate, 74018812, FurnitureSetWelcomeActivate);
		AddExecutor(ExecutorType.SpSummon, 22850702, ChaosAngelSpSummonWith2Monster);
		AddExecutor(ExecutorType.SpSummon, 94259633, RelinquishedAnimaSpSummon);
		AddExecutor(ExecutorType.SpSummon, 24269961, UnchainedSoulLordOfYamaSpSummon);
		AddExecutor(ExecutorType.SpSummon, 93084621, UnchainedSoulOfAnguishSpSummon);
		AddExecutor(ExecutorType.SpSummon, 67680512, UnchainedSoulOfRageSpSummon);
		AddExecutor(ExecutorType.SpSummon, 29479265, UnchainedAbominationSpSummon);
		AddExecutor(ExecutorType.SpSummon, 29301450, SPLittleKnightSpSummon);
		AddExecutor(ExecutorType.SpSummon, 22850702, ChaosAngelSpSummonWith3Monster);
		AddExecutor(ExecutorType.SpSummon, 71607202, MuckrakerFromTheUnderworldSpSummon);
		AddExecutor(ExecutorType.Activate, 41165831, UnchainedSoulOfSharvaraActivate);
		AddExecutor(ExecutorType.SpSummon, 93039339, SuperStarslayerTYPHONSpSummon);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
		AddExecutor(ExecutorType.Summon, SummonForTYPHONCheck);
		AddExecutor(ExecutorType.SummonOrSet, ForBigWelcomeSummon);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
	}

	public List<T> ShuffleList<T>(List<T> list)
	{
		int count = list.Count;
		while (count-- > 1)
		{
			int num = Program.Rand.Next(list.Count);
			int index = (num + Program.Rand.Next(list.Count - 1)) % list.Count;
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		return list;
	}

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false, bool ignoreCurrentDestroy = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card?.Data != null && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			orderby card.Attack descending
			select card).ToList();
		if (list.Count() > 0)
		{
			return list[0];
		}
		List<ClientCard> list2 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			orderby card.Attack descending
			select card).ToList();
		if (list2.Count() > 0)
		{
			return list2[0];
		}
		List<ClientCard> list3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			orderby card.Attack descending
			select card).ToList();
		if (list3.Count() > 0)
		{
			return list3[0];
		}
		List<ClientCard> list4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && card.EquipCards.Count() > 0 && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			orderby card.Attack descending
			select card).ToList();
		if (list4.Count() > 0)
		{
			return list4[0];
		}
		List<ClientCard> list5 = (from card in base.Enemy.GetMonsters()
			orderby card.Attack descending
			select card).ToList();
		if (list5.Count() > 0)
		{
			foreach (ClientCard item in list5)
			{
				if ((item.HasType((CardType)8396992) || (item.HasType(CardType.Link) && item.LinkCount >= 2)) && CheckCanBeTargeted(item, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(item)))
				{
					return item;
				}
			}
		}
		if (attack >= 0)
		{
			if (attack == 0)
			{
				attack = base.Util.GetBestAttack(base.Bot);
			}
			List<ClientCard> list6 = (from card in base.Enemy.MonsterZone.GetMonsters()
				where card.GetDefensePower() >= attack && card.GetDefensePower() > 0 && card.IsAttack() && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
				orderby card.Attack descending
				select card).ToList();
			if (list6.Count() > 0)
			{
				return list6[0];
			}
		}
		return null;
	}

	public List<ClientCard> GetProblematicEnemyCardList(bool canBeTarget = false, bool ignoreSpells = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> resultList = new List<ClientCard>();
		List<ClientCard> list = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !currentDestroyCardList.Contains(card) && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list.Count() > 0)
		{
			resultList.AddRange(list);
		}
		List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && !currentDestroyCardList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && CheckCanBeTargeted(c, canBeTarget, selfType)).ToList();
		if (list2.Count() > 0)
		{
			resultList.AddRange(ShuffleList(list2));
		}
		List<ClientCard> list3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentDestroyCardList.Contains(card) && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list3.Count() > 0 && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			resultList.AddRange(list3);
		}
		List<ClientCard> list4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentDestroyCardList.Contains(card) && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list4.Count() > 0)
		{
			resultList.AddRange(list4);
		}
		List<ClientCard> list5 = (from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card)
			orderby card.Attack descending
			select card).ToList();
		if (list5.Count() > 0)
		{
			foreach (ClientCard item in list5)
			{
				if ((item.HasType((CardType)8396992) || (item.HasType(CardType.Link) && item.LinkCount >= 2)) && !resultList.Contains(item) && CheckCanBeTargeted(item, canBeTarget, selfType))
				{
					resultList.Add(item);
				}
			}
		}
		List<ClientCard> list6 = (from c in base.Enemy.GetSpells()
			where c.IsFaceup() && !currentDestroyCardList.Contains(c) && c.HasType((CardType)17694720) && CheckCanBeTargeted(c, canBeTarget, selfType) && !notToDestroySpellTrap.Contains(c.Id)
			select c).ToList();
		if (list6.Count() > 0 && !ignoreSpells)
		{
			resultList.AddRange(ShuffleList(list6));
		}
		return resultList;
	}

	public ClientCard GetBestEnemyMonster(bool onlyFaceup = false, bool canBeTarget = false, bool ignoreCurrentDestroy = false, CardType selfType = (CardType)0)
	{
		ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget, ignoreCurrentDestroy, selfType);
		if (problematicEnemyMonster != null)
		{
			return problematicEnemyMonster;
		}
		problematicEnemyMonster = (from c in base.Enemy.MonsterZone
			where c?.Data != null && c.HasType(CardType.Monster) && c.IsFaceup() && CheckCanBeTargeted(c, canBeTarget, selfType) && (!ignoreCurrentDestroy || currentDestroyCardList.Contains(c))
			orderby c.Attack descending
			select c).FirstOrDefault();
		if (problematicEnemyMonster != null)
		{
			return problematicEnemyMonster;
		}
		List<ClientCard> list = (from c in base.Enemy.GetMonsters()
			where !ignoreCurrentDestroy || currentDestroyCardList.Contains(c)
			select c).ToList();
		if (list.Count() > 0 && !onlyFaceup)
		{
			return ShuffleList(list)[0];
		}
		return null;
	}

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219) || card.HasSetcode(413))).ToList();
		List<int> dangerMonsterIdList = new List<int> { 99937011, 63542003, 9411399, 28954097, 30680659 };
		list.AddRange(base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => dangerMonsterIdList.Contains(card.Id)));
		return list;
	}

	public int GetEmptyMainMonsterZoneCount()
	{
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.MonsterZone[i] == null)
			{
				num++;
			}
		}
		return num;
	}

	public List<ClientCard> GetNormalEnemyTargetList(bool canBeTarget = true, bool ignoreCurrentDestroy = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> targetList = GetProblematicEnemyCardList(canBeTarget, ignoreSpells: false, selfType);
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup() && !targetList.Contains(card) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		targetList.AddRange(list);
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && enemySetThisTurn.Contains(card)
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && !enemySetThisTurn.Contains(card)
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetMonsters()
			where card.IsFacedown() && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			select card).ToList()));
		return targetList;
	}

	public List<ClientCard> GetMonsterListForTargetNegate(bool canBeTarget = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> list = new List<ClientCard>();
		if (CheckWhetherNegated())
		{
			return list;
		}
		ClientCard clientCard = base.Enemy.MonsterZone.FirstOrDefault((ClientCard card) => card?.Data != null && card.IsMonsterShouldBeDisabledBeforeItUseEffect() && card.IsFaceup() && !card.IsShouldNotBeTarget() && CheckCanBeTargeted(card, canBeTarget, selfType) && !currentNegateMonsterList.Contains(card));
		if (clientCard != null)
		{
			list.Add(clientCard);
		}
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Location == CardLocation.MonsterZone && item.Controller == 1 && !item.IsDisabled() && CheckCanBeTargeted(item, canBeTarget, selfType) && !currentNegateMonsterList.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public int GetMaterialAttack(List<ClientCard> materials)
	{
		if (base.Util.IsTurn1OrMain2())
		{
			return 0;
		}
		int num = 0;
		foreach (ClientCard material in materials)
		{
			if (material.IsAttack() || !summonThisTurn.Contains(material))
			{
				num += material.Attack;
			}
		}
		return num;
	}

	public int GetBotCurrentTotalAttack(List<ClientCard> exceptList = null)
	{
		if (base.Util.IsTurn1OrMain2())
		{
			return 0;
		}
		int num = 0;
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if ((exceptList == null || !exceptList.Contains(monster)) && (monster.IsAttack() || !summonThisTurn.Contains(monster)))
			{
				num += monster.Attack;
			}
		}
		return num;
	}

	public List<ClientCard> GetCanBeUsedForLinkMaterial(bool useAdvancedMonster = false, Func<ClientCard, bool> exceptRule = null)
	{
		List<ClientCard> list = base.Bot.GetMonsters().Where(delegate(ClientCard card)
		{
			if (card.IsFacedown() || (exceptRule != null && exceptRule(card)))
			{
				return false;
			}
			if (card.IsCode(71607202) && summonThisTurn.Contains(card))
			{
				return false;
			}
			if (card.IsCode(2347656) && !card.IsDisabled() && base.Bot.HasInSpellZoneOrInGraveyard(92714517))
			{
				return false;
			}
			return ((!card.IsCode(22850702) && !card.IsCode(81497285)) || useAdvancedMonster || (!card.IsAttack() && summonThisTurn.Contains(card))) ? true : false;
		}).ToList();
		list.Sort(CompareUsableAttack);
		return list;
	}

	public bool CheckCanDirectAttack()
	{
		if (base.Enemy.GetMonsterCount() == 0 && !activatedCardIdList.Contains(29301450) && base.Duel.Turn > 1 && base.Duel.Player == 0)
		{
			return base.Duel.Phase < DuelPhase.Main2;
		}
		return false;
	}

	public int CheckCalledbytheGrave(int id)
	{
		if (DefaultCheckWhetherCardIdIsNegated(id))
		{
			return 1;
		}
		return 0;
	}

	public bool CheckCanBeTargeted(ClientCard card, bool canBeTarget, CardType selfType)
	{
		if (card == null)
		{
			return true;
		}
		if (canBeTarget)
		{
			if (card.IsShouldNotBeTarget())
			{
				return false;
			}
			if ((selfType & CardType.Monster) > (CardType)0 && card.IsShouldNotBeMonsterTarget())
			{
				return false;
			}
			if ((selfType & CardType.Spell) > (CardType)0 && card.IsShouldNotBeSpellTrapTarget())
			{
				return false;
			}
			if ((selfType & CardType.Trap) > (CardType)0 && (card.IsShouldNotBeSpellTrapTarget() || (!card.IsDisabled() && notToBeTrapTargetList.Contains(card.Id))))
			{
				return false;
			}
		}
		return true;
	}

	public int CheckRemainInDeck(int id)
	{
		for (int i = 1; i < 4; i++)
		{
			if (DeckCountTable[i].Contains(id))
			{
				return base.Bot.GetRemainingCount(id, i);
			}
		}
		return 0;
	}

	public int CheckRemainInDeck(params int[] ids)
	{
		int num = 0;
		foreach (int id in ids)
		{
			num += CheckRemainInDeck(id);
		}
		return num;
	}

	public bool CheckSpellWillBeNegate(bool isCounter = false, ClientCard target = null)
	{
		if (target == null)
		{
			target = base.Card;
		}
		if (target.Location != CardLocation.SpellZone && target.Location != CardLocation.Hand)
		{
			return false;
		}
		if (base.Enemy.HasInMonstersZone(99916754, notDisabled: true) && !isCounter)
		{
			return true;
		}
		if (target.IsSpell())
		{
			if (base.Enemy.HasInMonstersZone(33198837, notDisabled: true))
			{
				return true;
			}
			if (base.Enemy.HasInSpellZone(61740673, notDisabled: true) || base.Bot.HasInSpellZone(61740673, notDisabled: true))
			{
				return true;
			}
			if (base.Enemy.HasInMonstersZone(37267041, notDisabled: true) || base.Bot.HasInMonstersZone(37267041, notDisabled: true))
			{
				return true;
			}
		}
		if (target.IsTrap() && (base.Enemy.HasInSpellZone(51452091, notDisabled: true) || base.Bot.HasInSpellZone(51452091, notDisabled: true)))
		{
			return true;
		}
		if (target.Location == CardLocation.SpellZone && (target.IsSpell() || target.IsTrap()))
		{
			int item = -1;
			for (int i = 0; i < 5; i++)
			{
				if (base.Bot.SpellZone[i] == base.Card)
				{
					item = i;
				}
			}
			if (infiniteImpermanenceList.Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckWhetherNegated(bool disablecheck = true, bool toFieldCheck = false, CardType type = (CardType)0)
	{
		if ((base.Card.IsSpell() || base.Card.IsTrap() || (type & CardType.Spell) == 0 || (type & CardType.Trap) == 0) && CheckSpellWillBeNegate())
		{
			return true;
		}
		if (CheckCalledbytheGrave(base.Card.Id) > 0)
		{
			return true;
		}
		if ((base.Card.IsMonster() || (type & CardType.Monster) == 0) && (toFieldCheck || base.Card.Location == CardLocation.MonsterZone))
		{
			if (((toFieldCheck && (type & CardType.Link) == 0) || base.Card.IsDefense()) && (base.Enemy.MonsterZone.Any((ClientCard card) => CheckNumber41(card)) || base.Bot.MonsterZone.Any((ClientCard card) => CheckNumber41(card))))
			{
				return true;
			}
			if (base.Enemy.HasInSpellZone(82732705, notDisabled: true, faceUp: true))
			{
				return true;
			}
		}
		if (disablecheck)
		{
			return base.Card.IsDisabled();
		}
		return false;
	}

	public bool CheckNumber41(ClientCard card)
	{
		if (card != null && card.IsFaceup() && card.IsCode(90590303) && card.IsDefense())
		{
			return !card.IsDisabled();
		}
		return false;
	}

	public bool CheckWhetherWillbeRemoved()
	{
		if (dimensionShifterCount > 0)
		{
			return true;
		}
		foreach (int item in new List<int> { 94853057, 61528025, 30241314, 81674782, 48626373, 58481572 })
		{
			foreach (ClientField item2 in new List<ClientField> { base.Bot, base.Enemy })
			{
				if (item2.HasInMonstersZone(item, notDisabled: true, hasXyzMaterial: false, faceUp: true) || item2.HasInSpellZone(item, notDisabled: true, faceUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CheckAtAdvantage()
	{
		if (GetProblematicEnemyMonster() == null && (base.Duel.Player == 0 || base.Bot.GetMonsterCount() > 0))
		{
			return true;
		}
		return false;
	}

	public bool CheckShouldNoMoreSpSummon(bool isLabrynth = true)
	{
		if (CheckAtAdvantage() && enemyActivateMaxxC && (base.Duel.Turn == 1 || base.Duel.Phase >= DuelPhase.Main2))
		{
			if (!isLabrynth)
			{
				return true;
			}
			if (cooclockAffected)
			{
				if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)))
				{
					return true;
				}
				if (base.Duel.Player == 0 && !summoned)
				{
					return true;
				}
				if (setTrapThisTurn.Count() == 0)
				{
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool CheckLastChainShouldNegated()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard == null || lastChainCard.Controller != 1)
		{
			return false;
		}
		if (lastChainCard.IsMonster() && lastChainCard.HasSetcode(74) && base.Duel.Phase == DuelPhase.Standby)
		{
			return false;
		}
		if (notToNegateIdList.Contains(lastChainCard.Id))
		{
			return false;
		}
		return true;
	}

	public bool CheckChainContainEnemyMaxxC()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Controller == 1 && item.IsCode(23434538))
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckBigWelcomeCanSpSummon(int cardId)
	{
		if (!base.Bot.HasInHandOrInGraveyard(cardId))
		{
			return CheckRemainInDeck(cardId) > 0;
		}
		return true;
	}

	public int CompareUsableAttack(ClientCard cardA, ClientCard cardB)
	{
		if (cardA == null && cardB == null)
		{
			return 0;
		}
		if (cardA == null)
		{
			return -1;
		}
		if (cardB == null)
		{
			return 1;
		}
		int num = ((!cardA.IsDefense() || !summonThisTurn.Contains(cardA)) ? cardA.Attack : 0);
		int num2 = ((!cardB.IsDefense() || !summonThisTurn.Contains(cardB)) ? cardB.Attack : 0);
		if (num < num2)
		{
			return -1;
		}
		if (num == num2)
		{
			return CardContainer.CompareCardLevel(cardA, cardB);
		}
		return 1;
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (currentSolvingChainCard != null)
		{
			if (currentSolvingChainCard.Controller == 1 && currentSolvingChainCard.IsCode(15693423))
			{
				Logger.DebugWriteLine("=== Evenly Matched activated.");
				List<ClientCard> banishList = new List<ClientCard>();
				List<ClientCard> source = (from card in base.Bot.GetMonsters()
					where !card.HasType(CardType.Token)
					select card).ToList();
				List<ClientCard> collection = source.Where((ClientCard card) => card.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list = source.Where((ClientCard card) => !banishList.Contains(card) && ((card.HasType((CardType)75505728) && base.Bot.HasInExtra(card.Id)) || CheckRemainInDeck(card.Id) > 0)).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list);
				List<ClientCard> list2 = (from c in base.Bot.GetSpells()
					where c.IsFaceup()
					select c).ToList();
				banishList.AddRange(ShuffleList(list2));
				List<ClientCard> list3 = (from c in base.Bot.GetSpells()
					where c.IsFacedown()
					select c).ToList();
				banishList.AddRange(ShuffleList(list3));
				List<ClientCard> list4 = source.Where((ClientCard card) => !banishList.Contains(card) && !card.IsCode(2347656) && ((card.HasType((CardType)75505728) && !base.Bot.HasInExtra(card.Id)) || CheckRemainInDeck(card.Id) == 0)).ToList();
				list4.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list4);
				List<ClientCard> list5 = source.Where((ClientCard card) => !banishList.Contains(card) && card.IsCode(2347656)).ToList();
				list5.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list5);
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
			if (currentSolvingChainCard.IsCode(81497285) && min == 1 && max == 1 && hint == 510)
			{
				foreach (KeyValuePair<int, Func<bool>> pair in new SortedDictionary<int, Func<bool>>
				{
					{ 83326048, DimensionalBarrierActivate },
					{ 30748475, DestructiveDarumaKarmaCannonSetCheck },
					{ 10045474, InfiniteImpermanenceSetCheck }
				})
				{
					ClientCard clientCard = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
					if (clientCard != null && pair.Value())
					{
						SelectSTPlace(null, avoidImpermanence: true);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard }, cards, min, max);
					}
				}
				ClientCard clientCard2 = cards.FirstOrDefault((ClientCard card) => card.IsCode(6351147));
				if (clientCard2 != null)
				{
					bool flag = false;
					if (!activatedCardIdList.Contains(41165831))
					{
						flag |= base.Bot.HasInHand(41165831);
						flag |= base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2 && base.Bot.HasInExtra(24269961) && !activatedCardIdList.Contains(24269961) && (CheckRemainInDeck(41165831) > 0 || base.Bot.HasInGraveyard(41165831)) && (from card in base.Bot.GetMonsters()
							where card.IsFaceup() && card.HasRace(CardRace.Fiend) && card.Level <= 4
							select card).Count() >= 2;
					}
					bool flag2 = false;
					if (!activatedCardIdList.Contains(75730490) && base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2)
					{
						flag2 |= base.Bot.HasInMonstersZone(75730490);
						flag2 |= base.Bot.HasInHand(75730490) && !summoned;
						flag2 |= base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && ((card.IsCode(5380979) && !activatedCardIdList.Contains(5380979) && (cooclockAffected || !setTrapThisTurn.Contains(card))) || (card.IsCode(92714517) && !activatedCardIdList.Contains(92714517) && (cooclockAffected || !setTrapThisTurn.Contains(card)))));
					}
					if (flag || flag2)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard2 }, cards, min, max);
					}
				}
				SortedDictionary<int, ClientCard> sortedDictionary = new SortedDictionary<int, ClientCard>
				{
					{
						92714517,
						cards.FirstOrDefault((ClientCard card) => card.IsCode(92714517))
					},
					{
						5380979,
						cards.FirstOrDefault((ClientCard card) => card.IsCode(5380979))
					}
				};
				new List<int> { 92714517, 5380979 };
				foreach (KeyValuePair<int, ClientCard> checkPair in sortedDictionary)
				{
					if (checkPair.Value != null && !base.Bot.HasInHand(checkPair.Key) && !base.Bot.HasInGraveyard(checkPair.Key) && !base.Bot.GetSpells().Any((ClientCard card) => card.IsCode(checkPair.Key) && card.IsFacedown()))
					{
						SelectSTPlace(null, avoidImpermanence: true);
						return base.Util.CheckSelectCount(new List<ClientCard> { checkPair.Value }, cards, min, max);
					}
				}
				if (sortedDictionary[92714517] != null && !base.Bot.HasInHand(92714517) && !base.Bot.GetSpells().Any((ClientCard card) => card.IsCode(92714517) && card.IsFacedown()))
				{
					SelectSTPlace(null, avoidImpermanence: true);
					return base.Util.CheckSelectCount(new List<ClientCard> { sortedDictionary[92714517] }, cards, min, max);
				}
				foreach (int checkId in new List<int> { 10045474, 83326048, 30748475, 92714517, 6351147, 5380979 })
				{
					ClientCard clientCard3 = cards.FirstOrDefault((ClientCard card) => card.IsCode(checkId));
					if (clientCard3 != null)
					{
						SelectSTPlace(null, avoidImpermanence: true);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard3 }, cards, min, max);
					}
				}
			}
			if (currentSolvingChainCard.IsCode(5380979))
			{
				banSpSummonExceptFiendCount = 2;
			}
			if (currentSolvingChainCard.IsCode(5380979) || (currentSolvingChainCard.IsCode(6351147) && rollbackCopyCardId == 5380979))
			{
				Logger.DebugWriteLine("rewrite welcome's select.");
				List<ClientCard> list6 = new List<ClientCard>();
				ClientCard welcomeOrBigWelcomeTarget = GetWelcomeOrBigWelcomeTarget(cards, 75730490);
				if (welcomeOrBigWelcomeTarget != null && !summonInChainList.Any((ClientCard card) => card.IsCode(75730490)) && ((base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2) || (base.Duel.Player == 1 && base.Duel.Phase >= DuelPhase.Main2)) && base.Bot.HasInHandOrInSpellZone(6351147))
				{
					list6.Add(welcomeOrBigWelcomeTarget);
				}
				ClientCard welcomeOrBigWelcomeTarget2 = GetWelcomeOrBigWelcomeTarget(cards, 1225009);
				if (welcomeOrBigWelcomeTarget2 != null && !summonInChainList.Any((ClientCard card) => card.IsCode(1225009)) && !activatedCardIdList.Contains(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && ((activatedCardIdList.Contains(92714517) || (!base.Bot.HasInGraveyard(92714517) && !base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517)))) | (!activatedCardIdList.Contains(92714517) && (CheckBigWelcomeCanSpSummon(2347656) || base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true)) && base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517) && (!setTrapThisTurn.Contains(card) || cooclockAffected))) | (!base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !CheckBigWelcomeCanSpSummon(2347656))))
				{
					list6.Add(welcomeOrBigWelcomeTarget2);
				}
				ClientCard welcomeOrBigWelcomeTarget3 = GetWelcomeOrBigWelcomeTarget(cards, 73602965);
				if (welcomeOrBigWelcomeTarget3 != null && !summonInChainList.Any((ClientCard card) => card.IsCode(73602965)) && !base.Bot.HasInHandOrHasInMonstersZone(73602965) && !activatedCardIdList.Contains(73602965) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Bot.HasInHand(2347656))
				{
					list6.Add(welcomeOrBigWelcomeTarget3);
				}
				ClientCard welcomeOrBigWelcomeTarget4 = GetWelcomeOrBigWelcomeTarget(cards, 2347656);
				if (welcomeOrBigWelcomeTarget4 != null && !summonInChainList.Any((ClientCard card) => card.IsCode(2347656)) && base.Bot.HasInSpellZoneOrInGraveyard(92714517) && !activatedCardIdList.Contains(92714517))
				{
					list6.Add(welcomeOrBigWelcomeTarget4);
				}
				ClientCard welcomeOrBigWelcomeTarget5 = GetWelcomeOrBigWelcomeTarget(cards, 81497285);
				if (welcomeOrBigWelcomeTarget5 != null && base.Bot.HasInSpellZoneOrInGraveyard(92714517) && !activatedCardIdList.Contains(92714517) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasRace(CardRace.Fiend) && card.Level >= 8 && !card.HasType((CardType)75497472)))
				{
					list6.Add(welcomeOrBigWelcomeTarget5);
				}
				bool flag3 = CheckCanDirectAttack();
				bool flag4 = base.Bot.UnderAttack && base.Bot.GetMonsterCount() == 0;
				if (flag3 || flag4)
				{
					ClientCard clientCard4 = null;
					int num = -1;
					foreach (ClientCard card in cards)
					{
						NamedCard namedCard = NamedCard.Get(card.Id);
						if (namedCard != null)
						{
							int num2 = (flag3 ? namedCard.Attack : Math.Max(namedCard.Attack, namedCard.Defense));
							if (clientCard4 == null || num2 > num)
							{
								clientCard4 = card;
								num = num2;
							}
						}
					}
					if (flag4 || (GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack() + num >= base.Enemy.LifePoints))
					{
						ClientCard welcomeOrBigWelcomeTarget6 = GetWelcomeOrBigWelcomeTarget(cards, clientCard4.Id);
						if (welcomeOrBigWelcomeTarget6 != null)
						{
							list6.Add(welcomeOrBigWelcomeTarget6);
						}
					}
				}
				foreach (int item in new List<int> { 74018812, 37629703, 2511, 73602965 })
				{
					if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item))
					{
						ClientCard welcomeOrBigWelcomeTarget7 = GetWelcomeOrBigWelcomeTarget(cards, item);
						if (welcomeOrBigWelcomeTarget7 != null)
						{
							list6.Add(welcomeOrBigWelcomeTarget7);
						}
					}
				}
				foreach (int item2 in new List<int> { 81497285, 74018812, 37629703, 2511, 73602965, 75730490, 1225009 })
				{
					ClientCard welcomeOrBigWelcomeTarget8 = GetWelcomeOrBigWelcomeTarget(cards, item2);
					if (welcomeOrBigWelcomeTarget8 != null && !list6.Contains(welcomeOrBigWelcomeTarget8))
					{
						list6.Add(welcomeOrBigWelcomeTarget8);
					}
				}
				if (list6.Count() > 0)
				{
					return base.Util.CheckSelectCount(list6, cards, min, max);
				}
			}
			bool num3 = currentSolvingChainCard.IsCode(1225009) && hint == 506;
			bool flag5 = currentSolvingChainCard.IsCode(92714517) || (currentSolvingChainCard.IsCode(6351147) && rollbackCopyCardId == 92714517);
			if (num3 | (flag5 && hint == 509 && base.Bot.GetMonsterCount() == 0))
			{
				Logger.DebugWriteLine("rewrite search.");
				new List<ClientCard>();
				List<int> list7 = new List<int> { 74018812, 2511, 37629703 };
				ClientCard welcomeOrBigWelcomeTarget9 = GetWelcomeOrBigWelcomeTarget(cards, 92714517);
				ClientCard welcomeOrBigWelcomeTarget10 = GetWelcomeOrBigWelcomeTarget(cards, 5380979);
				ClientCard welcomeOrBigWelcomeTarget11 = GetWelcomeOrBigWelcomeTarget(cards, 1225009);
				if (base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2)
				{
					if (!summoned && !activatedCardIdList.Contains(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && CheckCalledbytheGrave(1225009) == 0 && welcomeOrBigWelcomeTarget11 != null && !base.Bot.HasInHand(1225009))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget11 }, cards, min, max);
					}
					if (!CheckShouldNoMoreSpSummon())
					{
						if (welcomeOrBigWelcomeTarget9 != null && !activatedCardIdList.Contains(73602965) && base.Bot.HasInHandOrHasInMonstersZone(73602965))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget9 }, cards, min, max);
						}
						if ((cooclockAffected || base.Bot.HasInHand(2511)) && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)) && !base.Bot.HasInHandOrInSpellZone(92714517) && !activatedCardIdList.Contains(92714517))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { AriannaSearchWelcomeTrap(cards, 92714517) }, cards, min, max);
						}
					}
				}
				ClientCard clientCard5 = null;
				ClientCard clientCard6 = null;
				if (!activatedCardIdList.Contains(73602965) && CheckRemainInDeck(73602965) > 0 && !base.Bot.HasInHand(73602965))
				{
					clientCard5 = GetWelcomeOrBigWelcomeTarget(cards, 73602965);
				}
				if (!activatedCardIdList.Contains(2511) && CheckRemainInDeck(2511) > 0 && !base.Bot.HasInHand(2511))
				{
					clientCard6 = GetWelcomeOrBigWelcomeTarget(cards, 2511);
				}
				if (clientCard5 != null || clientCard6 != null)
				{
					foreach (KeyValuePair<int, Func<bool>> checkPair2 in new SortedDictionary<int, Func<bool>>
					{
						{ 92714517, BigWelcomeLabrynthSetCheck },
						{ 83326048, DimensionalBarrierActivate },
						{ 30748475, DestructiveDarumaKarmaCannonSetCheck },
						{ 5380979, WelcomeLabrynthSetCheck }
					})
					{
						if (base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && !setTrapThisTurn.Contains(card) && card.IsCode(checkPair2.Key)) || activatedCardIdList.Contains(checkPair2.Key))
						{
							continue;
						}
						if (base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && setTrapThisTurn.Contains(card) && card.IsCode(checkPair2.Key)) && clientCard6 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
						}
						if (base.Bot.HasInHand(checkPair2.Key) && checkPair2.Value())
						{
							if (clientCard5 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard5 }, cards, min, max);
							}
							if (base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2 && clientCard6 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
							}
						}
					}
				}
				bool flag6 = !base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(5380979, 92714517));
				if (flag6)
				{
					List<ClientCard> source2 = base.Bot.Hand.Where((ClientCard card) => card != base.Card).ToList();
					flag6 &= source2.Count() <= 2 && !source2.Any((ClientCard card) => !card.IsCode(23434538, 14558127, 2511));
				}
				if (!flag6 && clientCard6 != null && welcomeOrBigWelcomeTarget9 != null)
				{
					foreach (int item3 in list7)
					{
						if (item3 != 2511 && CheckCalledbytheGrave(item3) == 0 && !activatedCardIdList.Contains(item3) && base.Bot.HasInHand(item3))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
						}
					}
				}
				if (base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2)
				{
					if (!flag6)
					{
						foreach (int item4 in list7)
						{
							ClientCard welcomeOrBigWelcomeTarget12 = GetWelcomeOrBigWelcomeTarget(cards, item4);
							if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item4) && welcomeOrBigWelcomeTarget12 != null && (item4 != 2511 || base.Enemy.GetMonsterCount() <= 0 || base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(37629703)))
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget12 }, cards, min, max);
							}
						}
					}
					if (welcomeOrBigWelcomeTarget9 != null && ((!base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInHandOrInSpellZone(92714517) && cards.Any((ClientCard c) => c.IsCode(2347656))) | (base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInHandOrInSpellZoneOrInGraveyard(92714517))))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { AriannaSearchWelcomeTrap(cards, 92714517) }, cards, min, max);
					}
					if (welcomeOrBigWelcomeTarget10 != null && base.Bot.HasInHandOrInSpellZone(92714517) && !base.Bot.HasInHandOrInSpellZone(5380979))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { AriannaSearchWelcomeTrap(cards, 5380979) }, cards, min, max);
					}
				}
				if (base.Duel.Player == 1 && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2) && !activatedCardIdList.Contains(92714517) && !activatedCardIdList.Contains(73602965) && !base.Bot.HasInSpellZone(92714517))
				{
					if (base.Bot.HasInHand(92714517) && !base.Bot.HasInHandOrHasInMonstersZone(73602965) && clientCard5 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard5 }, cards, min, max);
					}
					if (base.Bot.HasInHand(73602965) && !base.Bot.HasInHandOrHasInMonstersZone(92714517) && welcomeOrBigWelcomeTarget9 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget9 }, cards, min, max);
					}
				}
				ClientCard welcomeOrBigWelcomeTarget13 = GetWelcomeOrBigWelcomeTarget(cards, 81497285);
				bool flag7 = base.Duel.Player == 0 && base.Bot.Hand.Any((ClientCard card) => card.Type == 4) && base.Duel.Phase <= DuelPhase.Main2;
				flag7 |= base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.Type == 4);
				if (!base.Bot.HasInHandOrHasInMonstersZone(81497285) && !activatedCardIdList.Contains(81497285) && flag7 && welcomeOrBigWelcomeTarget13 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget13 }, cards, min, max);
				}
				if (!activatedCardIdList.Contains(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && CheckCalledbytheGrave(1225009) == 0 && welcomeOrBigWelcomeTarget11 != null && !base.Bot.HasInHand(1225009))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget11 }, cards, min, max);
				}
				if (!flag6)
				{
					foreach (int item5 in list7)
					{
						ClientCard welcomeOrBigWelcomeTarget14 = GetWelcomeOrBigWelcomeTarget(cards, item5);
						if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item5) && welcomeOrBigWelcomeTarget14 != null && (item5 != 2511 || base.Enemy.GetMonsterCount() <= 0 || base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(37629703)))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget14 }, cards, min, max);
						}
					}
				}
				List<int> list8 = new List<int> { 92714517, 74018812, 2511, 37629703, 81497285, 73602965, 75730490, 5380979 };
				foreach (int item6 in list8)
				{
					ClientCard welcomeOrBigWelcomeTarget15 = GetWelcomeOrBigWelcomeTarget(cards, item6);
					if (!base.Bot.HasInMonstersZone(item6) && !base.Bot.HasInHandOrInSpellZone(item6) && welcomeOrBigWelcomeTarget15 != null)
					{
						if (item6 == 92714517 || item6 == 5380979)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { AriannaSearchWelcomeTrap(cards, item6) }, cards, min, max);
						}
						return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget15 }, cards, min, max);
					}
				}
				foreach (int item7 in list8)
				{
					ClientCard welcomeOrBigWelcomeTarget16 = GetWelcomeOrBigWelcomeTarget(cards, item7);
					if (CheckRemainInDeck(item7) > 0)
					{
						if (item7 == 92714517 || item7 == 5380979)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { AriannaSearchWelcomeTrap(cards, item7) }, cards, min, max);
						}
						return base.Util.CheckSelectCount(new List<ClientCard> { welcomeOrBigWelcomeTarget16 }, cards, min, max);
					}
				}
			}
			if (flag5 && hint == 509)
			{
				bool flag8 = base.Duel.Phase > DuelPhase.Main2 || (base.Card.IsCode(73602965) && (base.CurrentTiming & 4) > 0);
				if (((GetProblematicEnemyCardList().Count() > 0) | (activatedCardIdList.Contains(1225009) && flag8) | (base.Bot.UnderAttack && (base.Bot.BattlingMonster?.GetDefensePower() ?? 0) <= (base.Enemy.BattlingMonster?.GetDefensePower() ?? 0) && base.Duel.LastChainPlayer != 0) | (base.Duel.Turn == 1 && base.Duel.Player == 0 && !activatedCardIdList.Contains(2347657)) | (base.Duel.Turn == 1 && base.Enemy.GetMonsterCount() == 0 && base.Enemy.GetSpellCount() == 0 && base.Enemy.Hand.Count > 0 && (base.CurrentTiming & 4) > 0)) && cards.Any((ClientCard c) => c.IsCode(2347656)) && !activatedCardIdList.Contains(2347657))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, 2347656) }, cards, min, max);
				}
				if (cards.Any((ClientCard c) => c.IsCode(1225009)) && !activatedCardIdList.Contains(1225009) && !base.Bot.HasInMonstersZone(1225009))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, 1225009) }, cards, min, max);
				}
				if (cards.Any((ClientCard c) => c.IsCode(2347656)) && !activatedCardIdList.Contains(2347657))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, 2347656) }, cards, min, max);
				}
				if (cards.Any((ClientCard c) => c.IsCode(1225009)) && !activatedCardIdList.Contains(1225009) && !chainSummoningIdList.Contains(1225009))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, 1225009) }, cards, min, max);
				}
				if (cards.Any((ClientCard c) => c.IsCode(81497285)) && base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2 && base.Duel.Player == 0 && base.Enemy.GetMonsterCount() == 0)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, 81497285) }, cards, min, max);
				}
				foreach (int furniture in new List<int> { 74018812, 2511, 37629703, 73602965 })
				{
					if (cards.Any((ClientCard c) => c.IsCode(furniture)) && !base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(furniture))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, furniture) }, cards, min, max);
					}
				}
				foreach (int checkId2 in new List<int> { 75730490, 81497285, 1225009, 74018812, 2511, 37629703, 73602965 })
				{
					if (cards.Any((ClientCard c) => c.IsCode(checkId2)))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { GetWelcomeOrBigWelcomeTarget(cards, checkId2) }, cards, min, max);
					}
				}
				Logger.DebugWriteLine("[warning] call BigWelcomeSpSummon with no select.");
			}
			if (flag5 && hint == 505)
			{
				if (bigwelcomeEscaseTarget != null && cards.Contains(bigwelcomeEscaseTarget))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { bigwelcomeEscaseTarget }, cards, min, max);
				}
				ClientCard clientCard7 = cards.FirstOrDefault((ClientCard c) => c.IsCode(2511));
				bool flag9 = CheckRemainInDeck(5380979, 92714517) > 0 && (base.Bot.HasInHandOrHasInMonstersZone(new List<int> { 37629703, 74018812 }) || summonInChainList.Any((ClientCard c) => c.IsCode(1225009)) || (base.Duel.Player == 0 && !summoned && !activatedCardIdList.Contains(1225009) && base.Bot.HasInHand(1225009)));
				if (clientCard7 != null && (setTrapThisTurn.Count() > 0 || (base.Duel.Turn == 1 && ((!activatedCardIdList.Contains(81497285) && base.Bot.HasInHandOrHasInMonstersZone(81497285)) || flag9)) || (base.Duel.Turn == 0 && flag9)))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard7 }, cards, min, max);
				}
				ClientCard clientCard8 = cards.FirstOrDefault((ClientCard c) => c.IsDefense() && c.IsCode(81497285));
				ClientCard clientCard9 = cards.FirstOrDefault((ClientCard c) => c.IsAttack() && c.IsCode(81497285));
				if (base.Bot.GetMonsters().Any((ClientCard card) => (base.Duel.Player == 1 || card.IsDefense()) && card.IsCode(81497285)) && (!activatedCardIdList.Contains(81497285) || activatedCardIdList.Contains(81497286)))
				{
					if (clientCard8 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard8 }, cards, min, max);
					}
					if (clientCard9 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard9 }, cards, min, max);
					}
				}
				if (summonInChainList.Any((ClientCard c) => c.IsCode(2347656)))
				{
					foreach (int checkId3 in new List<int> { 23434538, 1225009, 14558127, 2511, 81497285, 37629703, 74018812, 73602965, 41165831, 75730490 })
					{
						ClientCard clientCard10 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId3));
						if (clientCard10 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard10 }, cards, min, max);
						}
					}
					return base.Util.CheckSelectCount(cards.OrderBy((ClientCard card) => card.Attack).ToList(), cards, min, max);
				}
				if (cards.Count() == 1)
				{
					return base.Util.CheckSelectCount(cards.OrderBy((ClientCard card) => card.Attack).ToList(), cards, min, max);
				}
				ClientCard clientCard11 = cards.FirstOrDefault((ClientCard c) => c.IsCode(1225009) && !summonInChainList.Contains(c));
				if (clientCard11 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard11 }, cards, min, max);
				}
				ClientCard clientCard12 = (from card in base.Bot.GetMonsters()
					where !card.IsCode(2347656)
					orderby card.Attack
					select card).FirstOrDefault();
				if (clientCard12 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard12 }, cards, min, max);
				}
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public ClientCard GetWelcomeOrBigWelcomeTarget(IList<ClientCard> cards, int cardId)
	{
		ClientCard clientCard = cards.FirstOrDefault((ClientCard card) => card.IsCode(cardId) && card.Location == CardLocation.Grave);
		if (clientCard != null)
		{
			return clientCard;
		}
		ClientCard clientCard2 = cards.FirstOrDefault((ClientCard card) => card.IsCode(cardId) && card.Location == CardLocation.Deck);
		if (clientCard2 != null)
		{
			return clientCard2;
		}
		ClientCard clientCard3 = cards.FirstOrDefault((ClientCard card) => card.IsCode(cardId) && card.Location == CardLocation.Hand);
		if (clientCard3 != null)
		{
			return clientCard3;
		}
		return null;
	}

	public ClientCard AriannaSearchWelcomeTrap(IList<ClientCard> cards, int welcomeId)
	{
		if (base.Bot.HasInHand(new List<int> { 2347656, 6351147, 73602965 }))
		{
			List<int> list = new List<int> { 74018812, 37629703 };
			foreach (int item in list)
			{
				ClientCard welcomeOrBigWelcomeTarget = GetWelcomeOrBigWelcomeTarget(cards, item);
				if (welcomeOrBigWelcomeTarget != null && !base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item) && CheckCalledbytheGrave(item) == 0 && !activatedCardIdList.Contains(item))
				{
					return welcomeOrBigWelcomeTarget;
				}
			}
			foreach (int item2 in list)
			{
				ClientCard welcomeOrBigWelcomeTarget2 = GetWelcomeOrBigWelcomeTarget(cards, item2);
				if (welcomeOrBigWelcomeTarget2 != null && CheckCalledbytheGrave(item2) == 0 && !activatedCardIdList.Contains(item2))
				{
					return welcomeOrBigWelcomeTarget2;
				}
			}
		}
		return GetWelcomeOrBigWelcomeTarget(cards, welcomeId);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override bool OnSelectMonsterSummonOrSet(ClientCard card)
	{
		if (card.Attack > 0 && CheckCanDirectAttack())
		{
			return false;
		}
		if (card.Attack <= 1000)
		{
			return true;
		}
		return base.OnSelectMonsterSummonOrSet(card);
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone)
		{
			switch (cardId)
			{
			case 94259633:
				return base.OnSelectPlace(cardId, player, location, available);
			case 24269961:
			case 93084621:
				if (base.Bot.MonsterZone[0] != null && base.Bot.MonsterZone[2] != null && (0x40 & available) != 0)
				{
					return 64;
				}
				if (base.Bot.MonsterZone[2] != null && base.Bot.MonsterZone[4] != null && (0x20 & available) != 0)
				{
					return 32;
				}
				break;
			}
			if (cardId == 71607202 || cardId == 67680512)
			{
				if (base.Bot.MonsterZone[1] != null && (0x40 & available) != 0)
				{
					return 64;
				}
				if (base.Bot.MonsterZone[3] != null && (0x20 & available) != 0)
				{
					return 32;
				}
			}
			List<int> list = ShuffleList(new List<int> { 0, 2, 4 });
			list.AddRange(ShuffleList(new List<int> { 1, 3 }));
			list.AddRange(ShuffleList(new List<int> { 5, 6 }));
			foreach (int item in list)
			{
				int num = (int)Math.Pow(2.0, item);
				if ((available & num) != 0 && base.Bot.MonsterZone[item] == null)
				{
					return num;
				}
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (namedCard != null)
		{
			if (base.Duel.Turn == 1 || base.Duel.Phase >= DuelPhase.Main2)
			{
				bool flag = false;
				if (namedCard.Attack <= namedCard.Defense)
				{
					flag = true;
				}
				if (flag)
				{
					return CardPosition.FaceUpDefence;
				}
			}
			if (base.Duel.Player == 1 && (namedCard.Defense >= namedCard.Attack || base.Util.IsOneEnemyBetterThanValue(namedCard.Attack, onlyATK: true)))
			{
				return CardPosition.FaceUpDefence;
			}
			int attack = namedCard.Attack;
			int value = Math.Max(base.Util.GetBestAttack(base.Bot), attack);
			if (base.Util.IsAllEnemyBetterThanValue(value, onlyATK: true))
			{
				return CardPosition.FaceUpDefence;
			}
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public override int OnSelectOption(IList<int> options)
	{
		if (options.Count() == 2 && options.Contains(1190) && options.Contains(1152))
		{
			if ((base.Duel.Player != 0 || base.Duel.Phase > DuelPhase.Main2) && !base.Bot.HasInHand(2511) && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)) && !activatedCardIdList.Contains(2511) && !CheckWhetherWillbeRemoved() && (activatedCardIdList.Contains(92714517) || base.Bot.GetSpells().All((ClientCard card) => setTrapThisTurn.Contains(card) || !card.IsCode(92714517))) && setTrapThisTurn.Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517, 83326048, 10045474, 30748475)))
			{
				return options.IndexOf(1190);
			}
			if (!enemyActivateMaxxC)
			{
				return options.IndexOf(1152);
			}
			if (activatedCardIdList.Contains(2511) && !CheckShouldNoMoreSpSummon())
			{
				return options.IndexOf(1152);
			}
			return options.IndexOf(1190);
		}
		if (options.Contains(base.Util.GetStringId(2347656, 3)) && options.Contains(base.Util.GetStringId(2347656, 4)))
		{
			int botWorstAttack = 0;
			ClientCard worstBotMonster = base.Util.GetWorstBotMonster(onlyATK: true);
			if (worstBotMonster != null)
			{
				botWorstAttack = worstBotMonster.Attack;
			}
			List<ClientCard> targetList = GetProblematicEnemyCardList();
			List<ClientCard> list = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card) && card.GetDefensePower() >= botWorstAttack && !currentDestroyCardList.Contains(card)
				select card).ToList();
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			targetList.AddRange(list);
			targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
				where !currentDestroyCardList.Contains(card) && card.IsFacedown()
				select card).ToList()));
			if (targetList.Count() > 0)
			{
				currentDestroyCardList.Add(targetList[0]);
				base.AI.SelectCard(targetList);
				return options.IndexOf(base.Util.GetStringId(2347656, 4));
			}
			return options.IndexOf(base.Util.GetStringId(2347656, 3));
		}
		if (options.IndexOf(1057) >= 0 || options.IndexOf(1056) >= 0 || options.IndexOf(1063) >= 0 || options.IndexOf(1073) >= 0 || options.IndexOf(1074) >= 0)
		{
			Dictionary<int, Func<bool>> obj = new Dictionary<int, Func<bool>>
			{
				{ 1057, DimensionalBarrierForRitual },
				{ 1056, DimensionalBarrierForFusion },
				{ 1063, DimensionalBarrierForSynchro },
				{ 1073, DimensionalBarrierForXyz },
				{ 1074, DimensionalBarrierForPendulum }
			};
			dimensionBarrierAnnouncing = true;
			foreach (KeyValuePair<int, Func<bool>> item in obj)
			{
				if (options.Contains(item.Key) && item.Value())
				{
					dimensionBarrierAnnouncing = false;
					dimensionalBarrierAnnouced.Add(item.Key);
					return options.IndexOf(item.Key);
				}
			}
			dimensionBarrierAnnouncing = false;
			List<ClientCard> list2 = new List<ClientCard>(base.Enemy.GetMonsters());
			list2.AddRange(base.Enemy.GetGraveyardMonsters());
			foreach (KeyValuePair<int, bool> item2 in new Dictionary<int, bool>
			{
				{
					1057,
					list2.Any((ClientCard card) => card.HasType(CardType.Ritual))
				},
				{
					1056,
					list2.Any((ClientCard card) => card.HasType(CardType.Fusion))
				},
				{
					1063,
					list2.Any((ClientCard card) => card.HasType(CardType.Synchro))
				},
				{
					1073,
					list2.Any((ClientCard card) => card.HasType(CardType.Xyz))
				},
				{
					1074,
					list2.Any((ClientCard card) => card.HasType(CardType.Pendulum))
				}
			})
			{
				if (options.Contains(item2.Key) && item2.Value)
				{
					dimensionBarrierAnnouncing = false;
					dimensionalBarrierAnnouced.Add(item2.Key);
					return options.IndexOf(item2.Key);
				}
			}
			foreach (int item3 in new List<int> { 1073, 1063, 1056, 1074, 1057 })
			{
				if (options.Contains(item3))
				{
					return options.IndexOf(item3);
				}
			}
		}
		if ((options.Contains(base.Util.GetStringId(1225009, 2)) || options.Contains(base.Util.GetStringId(75730490, 2))) && GetEmptyMainMonsterZoneCount() > chainSummoningIdList.Count())
		{
			bool flag = false;
			if (!activatedCardIdList.Contains(1225009) && base.Bot.HasInHand(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !chainSummoningIdList.Contains(1225009))
			{
				flag = true;
				base.AI.SelectCard(1225009);
			}
			if (!flag)
			{
				foreach (int item4 in new List<int> { 2347656, 81497285 })
				{
					if (base.Bot.HasInHand(item4))
					{
						flag = true;
						base.AI.SelectCard(item4);
						break;
					}
				}
			}
			if (!flag && base.Duel.Player == 0 && base.Duel.Phase < DuelPhase.End && GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !card.HasRace(CardRace.Fiend)).Count() + chainSummoningIdList.Count() == 2)
			{
				ClientCard clientCard = null;
				int num = 0;
				foreach (ClientCard item5 in base.Bot.Hand)
				{
					NamedCard namedCard = NamedCard.Get(item5.Id);
					if (namedCard != null && namedCard.Race == 8 && (clientCard == null || num > item5.Attack))
					{
						clientCard = item5;
						num = item5.Attack;
					}
				}
				if (clientCard != null)
				{
					flag = true;
					base.AI.SelectCard(clientCard);
				}
			}
			if (!flag && CheckCanDirectAttack())
			{
				ClientCard clientCard2 = null;
				int num2 = 0;
				foreach (ClientCard item6 in base.Bot.Hand)
				{
					NamedCard namedCard2 = NamedCard.Get(item6.Id);
					if (namedCard2 != null && namedCard2.Race == 8 && (clientCard2 == null || num2 < item6.Attack))
					{
						clientCard2 = item6;
						num2 = item6.Attack;
					}
				}
				if (clientCard2 != null)
				{
					flag = true;
					base.AI.SelectCard(clientCard2);
				}
			}
			if (flag)
			{
				if (options.Contains(base.Util.GetStringId(1225009, 2)))
				{
					return options.IndexOf(base.Util.GetStringId(1225009, 2));
				}
				if (options.Contains(base.Util.GetStringId(75730490, 2)))
				{
					return options.IndexOf(base.Util.GetStringId(75730490, 2));
				}
			}
		}
		if ((options.Contains(base.Util.GetStringId(1225009, 3)) || options.Contains(base.Util.GetStringId(75730490, 3))) && (!base.Util.ChainContainsCard(5380979) || base.Bot.GetSpellCountWithoutField() < 4))
		{
			foreach (int item7 in new List<int> { 92714517, 5380979, 10045474, 83326048, 30748475 })
			{
				if (base.Bot.HasInHand(item7) && (item7 == 10045474 || !base.Bot.HasInSpellZone(item7)))
				{
					base.AI.SelectCard(item7);
					if (options.Contains(base.Util.GetStringId(1225009, 3)))
					{
						return options.IndexOf(base.Util.GetStringId(1225009, 3));
					}
					if (options.Contains(base.Util.GetStringId(75730490, 3)))
					{
						return options.IndexOf(base.Util.GetStringId(75730490, 3));
					}
				}
			}
		}
		if (options.Contains(base.Util.GetStringId(1225009, 4)))
		{
			return options.IndexOf(base.Util.GetStringId(1225009, 4));
		}
		if (options.Contains(base.Util.GetStringId(75730490, 4)))
		{
			return options.IndexOf(base.Util.GetStringId(75730490, 4));
		}
		return base.OnSelectOption(options);
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == 96)
		{
			Logger.DebugWriteLine("*** muckraker replace.");
			base.AI.SelectCard((from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.HasRace(CardRace.Fiend)
				orderby card.Attack
				select card).ToList());
			return true;
		}
		return base.OnSelectYesNo(desc);
	}

	public override void OnNewTurn()
	{
		if (base.Duel.Turn <= 1)
		{
			dimensionShifterCount = 0;
			enemySpSummonFromExLastTurn = 0;
			enemySpSummonFromExThisTurn = 0;
			banSpSummonExceptFiendCount = 0;
		}
		enemyActivateMaxxC = false;
		enemySpSummonFromExLastTurn = enemySpSummonFromExThisTurn;
		enemySpSummonFromExThisTurn = 0;
		rollbackCopyCardId = 0;
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		if (banSpSummonExceptFiendCount > 0)
		{
			banSpSummonExceptFiendCount--;
		}
		infiniteImpermanenceList.Clear();
		summoned = false;
		cooclockAffected = false;
		activatedCardIdList.Clear();
		setTrapThisTurn.Clear();
		summonThisTurn.Clear();
		enemySetThisTurn.Clear();
		dimensionalBarrierAnnouced.Clear();
		summonInChainList.Clear();
		base.OnNewTurn();
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (card == null)
		{
			return;
		}
		if (chainSummoningIdList.Count() > 0)
		{
			Logger.DebugWriteLine("[Welcome] Summoning: " + string.Join(",", chainSummoningIdList) + "\n");
		}
		if (player == 1 && card.IsCode(10045474))
		{
			if (enemyActivateInfiniteImpermanenceFromHand)
			{
				enemyActivateInfiniteImpermanenceFromHand = false;
			}
			else
			{
				for (int i = 0; i < 5; i++)
				{
					if (base.Enemy.SpellZone[i] == card)
					{
						infiniteImpermanenceList.Add(4 - i);
						break;
					}
				}
			}
		}
		base.OnChaining(player, card);
	}

	public override void OnChainSolved(int chainIndex)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null && !base.Duel.IsCurrentSolvingChainNegated())
		{
			if (currentSolvingChainInfo.ActivatePlayer == 1)
			{
				if (currentSolvingChainInfo.IsCode(23434538))
				{
					enemyActivateMaxxC = true;
				}
				if (currentSolvingChainInfo.IsCode(91800273))
				{
					dimensionShifterCount = 2;
				}
			}
			if (currentSolvingChainInfo.ActivatePlayer == 0 && currentSolvingChainInfo.IsCode(2511))
			{
				cooclockAffected = true;
			}
		}
		base.OnChainSolved(chainIndex);
	}

	public override void OnChainEnd()
	{
		rollbackCopyCardId = 0;
		currentNegateMonsterList.Clear();
		currentDestroyCardList.Clear();
		escapeTargetList.Clear();
		chainSummoningIdList.Clear();
		summonInChainList.Clear();
		enemyActivateInfiniteImpermanenceFromHand = false;
		for (int num = enemySetThisTurn.Count() - 1; num >= 0; num--)
		{
			ClientCard clientCard = enemySetThisTurn[num];
			if (clientCard == null || clientCard.Location != CardLocation.SpellZone || clientCard.HasPosition(CardPosition.FaceUp))
			{
				enemySetThisTurn.RemoveAt(num);
			}
		}
		if (cooclockActivating)
		{
			cooclockActivating = false;
		}
		furnitureActivating = false;
		dimensionBarrierAnnouncing = false;
		bigwelcomeEscaseTarget = null;
		base.OnChainEnd();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (previousControler == 1)
		{
			if (previousLocation == 64 && currentLocation == 4)
			{
				enemySpSummonFromExThisTurn++;
			}
			if (card != null)
			{
				if (card.IsCode(10045474) && previousLocation == 2 && currentLocation == 8)
				{
					enemyActivateInfiniteImpermanenceFromHand = true;
				}
				if (card.Location == CardLocation.SpellZone && card.HasPosition(CardPosition.FaceDown))
				{
					enemySetThisTurn.Add(card);
				}
			}
		}
		if (card != null)
		{
			if (previousControler == 0)
			{
				if (previousLocation == 4 && currentLocation != 4)
				{
					if (summonThisTurn.Contains(card))
					{
						summonThisTurn.Remove(card);
					}
					if (summonInChainList.Contains(card))
					{
						summonInChainList.Remove(card);
					}
				}
				if (previousLocation == 8 && currentLocation != 8 && setTrapThisTurn.Contains(card))
				{
					setTrapThisTurn.Remove(card);
				}
			}
			if (currentControler == 0)
			{
				ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
				if (currentLocation == 8 && (currentSolvingChainCard == null || !currentSolvingChainCard.IsCode(73602965)) && (card.HasType(CardType.Trap) || card.IsCode(5380979, 92714517)))
				{
					Logger.DebugWriteLine("[setTrapThisTurn]set " + card.Name);
					setTrapThisTurn.Add(card);
				}
				if (currentLocation == 4)
				{
					summonThisTurn.Add(card);
					if (currentSolvingChainCard != null)
					{
						summonInChainList.Add(card);
					}
				}
			}
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	public override BattlePhaseAction OnBattle(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		if (attackers.Count() > 0 && defenders.Count() > 0)
		{
			List<ClientCard> list = attackers.OrderBy((ClientCard card) => card.Attack).ToList();
			for (int num = 0; num < list.Count; num++)
			{
				ClientCard clientCard = list[num];
				clientCard.IsLastAttacker = num == list.Count - 1;
				BattlePhaseAction battlePhaseAction = OnSelectAttackTarget(clientCard, defenders);
				if (battlePhaseAction != null)
				{
					return battlePhaseAction;
				}
			}
		}
		return base.OnBattle(attackers, defenders);
	}

	public void SelectSTPlace(ClientCard card = null, bool avoidImpermanence = false, List<int> avoidList = null)
	{
		if (card == null)
		{
			card = base.Card;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.SpellZone[i] == null && (!(card != null && card.Location == CardLocation.Hand && avoidImpermanence) || !infiniteImpermanenceList.Contains(i)) && (avoidList == null || !avoidList.Contains(i)))
			{
				list.Add(i);
			}
		}
		int count = list.Count;
		while (count-- > 1)
		{
			int num = Program.Rand.Next(list.Count);
			int index = (num + Program.Rand.Next(list.Count - 1)) % list.Count;
			int value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		if (avoidImpermanence && base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled()))
		{
			foreach (int item in list)
			{
				ClientCard clientCard = base.Enemy.SpellZone[4 - item];
				if (clientCard == null || !clientCard.IsFacedown())
				{
					int zones = (int)Math.Pow(2.0, item);
					base.AI.SelectPlace(zones);
					return;
				}
			}
		}
		using (List<int>.Enumerator enumerator = list.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				int current2 = enumerator.Current;
				int zones2 = (int)Math.Pow(2.0, current2);
				base.AI.SelectPlace(zones2);
				return;
			}
		}
		base.AI.SelectPlace(0);
	}

	public void ResetCooclockEffect(bool onlyCheck)
	{
		if (!onlyCheck && cooclockAffected && setTrapThisTurn.Contains(base.Card))
		{
			cooclockAffected = false;
			setTrapThisTurn.Remove(base.Card);
		}
	}

	public bool LadyLabrynthOfTheSilverCastleFieldActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone && (base.Util.GetLastChainCard() == null || !base.Util.GetLastChainCard().IsCode(15693423)) && (!CheckWhetherNegated() || base.Enemy.HasInMonstersZone(81497285)))
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool LadyLabrynthOfTheSilverCastleHandActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckShouldNoMoreSpSummon() || base.Util.ChainContainsCard(15693423))
			{
				return false;
			}
			bool flag = false;
			flag |= CheckChainContainEnemyMaxxC();
			if (!flag && GetEmptyMainMonsterZoneCount() + chainSummoningIdList.Count() <= 0)
			{
				return false;
			}
			flag |= cooclockAffected && setTrapThisTurn.Count() > 0 && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382));
			flag |= base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.HasType(CardType.Trap) && !setTrapThisTurn.Contains(card)) && !base.Bot.HasInMonstersZone(base.Card.Id, notDisabled: true, hasXyzMaterial: false, faceUp: true);
			flag |= base.Duel.Player == 1 && base.Duel.Phase >= DuelPhase.End;
			flag |= setTrapThisTurn.Count() > 0 && base.Duel.Phase >= DuelPhase.End;
			flag |= base.Bot.UnderAttack && base.Bot.GetMonsterCount() == 0 && !base.Util.ChainContainsCard(30748475);
			if (base.Bot.HasInExtra(24269961) && base.Duel.Player == 0 && base.Duel.Phase < DuelPhase.End)
			{
				List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !card.HasRace(CardRace.Fiend));
				int num = canBeUsedForLinkMaterial.Count();
				if (!activatedCardIdList.Contains(41165831) && base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown()) && (!activatedCardIdList.Contains(24269961) || base.Bot.HasInHand(41165831)))
				{
					num++;
				}
				if (((num == 2 || (num == 1 && canBeUsedForLinkMaterial.Any((ClientCard card) => card.IsCode(24269961)))) && base.Bot.HasInExtra(24269961)) || canBeUsedForLinkMaterial.Any((ClientCard card) => card.HasSetcode(304)))
				{
					flag |= base.Enemy.GetMonsterCount() > 0 && base.Bot.HasInExtra(93084621);
					flag |= base.Bot.HasInExtra(67680512);
				}
			}
			if (flag)
			{
				activatedCardIdList.Add(base.Card.Id);
				chainSummoningIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool LovelyLabrynthOfTheSilverCastleActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(2347656, 0))
		{
			List<int> list = new List<int> { 92714517, 10045474, 83326048, 30748475, 5380979 };
			foreach (int item in list)
			{
				if (base.Bot.HasInGraveyard(item) && !base.Bot.HasInHandOrInSpellZone(item))
				{
					base.AI.SelectCard(item);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			foreach (int item2 in list)
			{
				if (base.Bot.HasInGraveyard(item2))
				{
					base.AI.SelectCard(item2);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			if (GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !card.HasRace(CardRace.Fiend)).Count() == 2 && !activatedCardIdList.Contains(24269961) && (base.Bot.HasInGraveyard(41165831) || CheckRemainInDeck(41165831) > 0))
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			return false;
		}
		if (base.Enemy.GetHandCount() == 0)
		{
			int botWorstAttack = 0;
			ClientCard worstBotMonster = base.Util.GetWorstBotMonster(onlyATK: true);
			if (worstBotMonster != null)
			{
				botWorstAttack = worstBotMonster.Attack;
			}
			List<ClientCard> targetList = GetProblematicEnemyCardList();
			List<ClientCard> list2 = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card) && card.GetDefensePower() >= botWorstAttack && !currentDestroyCardList.Contains(card)
				select card).ToList();
			list2.Sort(CardContainer.CompareCardAttack);
			list2.Reverse();
			targetList.AddRange(list2);
			targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
				where !currentDestroyCardList.Contains(card)
				select card).ToList()));
			if (targetList.Count() > 0)
			{
				currentDestroyCardList.Add(targetList[0]);
				base.AI.SelectCard(targetList);
				base.AI.SelectOption(1);
			}
			else
			{
				base.AI.SelectOption(0);
			}
		}
		activatedCardIdList.Add(base.Card.Id + 1);
		return true;
	}

	public bool UnchainedSoulOfSharvaraActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			SelectSTPlace();
			return true;
		}
		if (base.Bot.HasInSpellZone(6351147) && GetEmptyMainMonsterZoneCount() > chainSummoningIdList.Count() && !CheckWhetherWillbeRemoved() && !CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			base.AI.SelectCard(6351147);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.IsCode(targetNegateIdList) && base.Duel.LastChainTargets.Any((ClientCard card) => card.Controller == 0 && card.IsFaceup() && card.HasRace(CardRace.Fiend) && base.Duel.CurrentChain.Any((ClientCard chain) => chain == card) && !card.IsCode(67680512, 93084621)))
			{
				escapeTargetList.AddRange(base.Duel.LastChainTargets);
				base.AI.SelectCard(base.Duel.LastChainTargets);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		bool flag = base.Duel.Player == 0 && GetEmptyMainMonsterZoneCount() > chainSummoningIdList.Count() && base.Bot.GetMonsterCount() > 0 && base.CurrentTiming <= 0;
		if (flag)
		{
			List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !card.HasRace(CardRace.Fiend));
			flag = CheckAtAdvantage() && !base.Bot.HasInMonstersZone(67680512) && base.Bot.HasInExtra(67680512) && canBeUsedForLinkMaterial.Count() == 1;
			if (base.Bot.HasInExtra(93084621) && !base.Bot.HasInMonstersZone(93084621) && !activatedCardIdList.Contains(93084621) && (from card in base.Enemy.GetMonsters()
				where card.IsFaceup()
				select card).Count() > 0)
			{
				flag |= canBeUsedForLinkMaterial.Count() == 2;
				flag |= canBeUsedForLinkMaterial.Count() == 1 && canBeUsedForLinkMaterial.Any((ClientCard card) => card.HasType(CardType.Link) && card.LinkCount == 2);
			}
		}
		flag |= CheckCanDirectAttack() && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack() + 2000 >= base.Enemy.LifePoints && GetEmptyMainMonsterZoneCount() > chainSummoningIdList.Count();
		if (flag | (base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.Main1 && base.Bot.GetMonsterCount() == 0 && (base.CurrentTiming & 4) != 0 && base.Util.GetTotalAttackingMonsterAttack(1) >= base.Bot.LifePoints))
		{
			foreach (int checkId in new List<int> { 10045474, 6351147, 5380979, 83326048, 30748475, 92714517 })
			{
				ClientCard clientCard = base.Bot.GetSpells().FirstOrDefault((ClientCard card) => card.IsFacedown() && card.IsCode(checkId));
				if (clientCard != null)
				{
					base.AI.SelectCard(clientCard);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
		}
		return false;
	}

	public bool AriasTheLabrynthButlerActivate()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			if (base.Util.ChainContainsCard(new int[3] { 90448279, 15693423, 46772449 }))
			{
				return false;
			}
			if (base.Duel.CurrentChain.Any((ClientCard card) => card.Controller == 0 && card.IsCode(1225009)))
			{
				return false;
			}
			foreach (KeyValuePair<int, Func<bool>> pair in new SortedList<int, Func<bool>>
			{
				{ 92714517, BigWelcomeLabrynthSetCheck },
				{ 5380979, WelcomeLabrynthSetCheck },
				{ 30748475, DestructiveDarumaKarmaCannonSetCheck },
				{ 83326048, DimensionalBarrierActivate }
			})
			{
				ClientCard clientCard = base.Bot.Hand.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
				if (clientCard != null && !activatedCardIdList.Contains(pair.Key) && pair.Value())
				{
					base.AI.SelectOption(1);
					base.AI.SelectCard(pair.Key);
					activatedCardIdList.Add(base.Card.Id);
					SelectSTPlace(clientCard, avoidImpermanence: true);
					return true;
				}
			}
			if (base.Bot.HasInHand(2347656) && (base.Duel.Player == 0 || (base.CurrentTiming & 4) != 0))
			{
				base.AI.SelectOption(0);
				base.AI.SelectCard(2347656);
				chainSummoningIdList.Add(2347656);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (base.Bot.HasInHand(1225009) && !activatedCardIdList.Contains(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true) && !chainSummoningIdList.Contains(1225009))
			{
				bool flag = false;
				if (base.Duel.Player == 1)
				{
					flag |= (base.CurrentTiming & 4) != 0;
					flag |= GetProblematicEnemyCardList().Count() > 0 && (base.Bot.HasInMonstersZoneOrInGraveyard(2347656) || CheckRemainInDeck(2347656) > 0) && !activatedCardIdList.Contains(2347657);
				}
				if (base.Duel.Player == 0)
				{
					flag |= summoned && !CheckShouldNoMoreSpSummon();
				}
				if (flag)
				{
					base.AI.SelectOption(0);
					base.AI.SelectCard(1225009);
					chainSummoningIdList.Add(1225009);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
		}
		return false;
	}

	public bool ArianeTheLabrynthServantSummon()
	{
		if (base.Duel.Turn > 1 && base.Enemy.GetMonsterCount() == 0)
		{
			summoned = true;
			return true;
		}
		if (!activatedCardIdList.Contains(base.Card.Id) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true) && !CheckWhetherWillbeRemoved() && (base.Bot.Hand.Any((ClientCard card) => card.Type == 4) || base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.Type == 4)) && !CheckShouldNoMoreSpSummon())
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ArianeTheLabrynthServantForRollbackSummon()
	{
		if (activatedCardIdList.Contains(base.Card.Id))
		{
			return false;
		}
		if (base.Bot.HasInHandOrInSpellZone(6351147) && !CheckWhetherWillbeRemoved())
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ArianeTheLabrynthServantActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(base.Card.Id, 0))
		{
			bool flag = base.Bot.HasInHandOrInSpellZone(6351147);
			if (CheckWhetherNegated() && !flag)
			{
				return false;
			}
			if (CheckShouldNoMoreSpSummon() && (!flag || !base.Bot.Graveyard.Any((ClientCard card) => card.IsCode(5380979, 92714517))))
			{
				return false;
			}
			int num = 0;
			if (!activatedCardIdList.Contains(1225009) && CheckRemainInDeck(1225009) > 0)
			{
				num = 1225009;
			}
			if (num == 0)
			{
				foreach (int item in new List<int> { 74018812, 37629703, 2511 })
				{
					if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item) && CheckRemainInDeck(item) > 0)
					{
						num = item;
						break;
					}
				}
			}
			if (num == 0)
			{
				List<int> list = new List<int>();
				if (base.Enemy.GetMonsterCount() == 0)
				{
					list.AddRange(new List<int> { 1225009, 37629703, 74018812, 2511 });
				}
				else
				{
					list.AddRange(new List<int> { 37629703, 74018812, 2511, 1225009 });
				}
				foreach (int item2 in list)
				{
					if (CheckRemainInDeck(item2) > 0)
					{
						num = item2;
						break;
					}
				}
			}
			if (num > 0)
			{
				bool flag2 = false;
				if (flag)
				{
					base.AI.SelectCard(6351147);
					flag2 = true;
				}
				if (!flag2)
				{
					ClientCard clientCard = base.Bot.GetSpells().FirstOrDefault((ClientCard card) => card.IsCode(5380979));
					if (clientCard != null)
					{
						base.AI.SelectCard(clientCard);
						flag2 = true;
					}
				}
				List<ClientCard> list2 = base.Bot.Hand.Where((ClientCard card) => card.IsFacedown() && card.Type == 4).ToList();
				list2.AddRange((from card in base.Bot.GetSpells()
					where card.IsFacedown() && card.Type == 4
					select card).ToList());
				if (!flag2)
				{
					foreach (int checkId in new List<int> { 10045474, 5380979, 92714517 })
					{
						ClientCard clientCard2 = list2.FirstOrDefault((ClientCard card) => card.IsCode(checkId));
						if (list2.Count((ClientCard card) => card.IsCode(checkId)) > 1 && clientCard2 != null)
						{
							base.AI.SelectCard(clientCard2);
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					foreach (int checkId2 in new List<int> { 10045474, 83326048, 30748475, 5380979, 92714517 })
					{
						ClientCard clientCard3 = list2.FirstOrDefault((ClientCard card) => card.IsCode(checkId2));
						if (clientCard3 != null)
						{
							base.AI.SelectCard(clientCard3);
							flag2 = true;
							break;
						}
					}
				}
			}
			return false;
		}
		activatedCardIdList.Add(base.Card.Id + 1);
		return true;
	}

	public bool AriannaTheLabrynthServantSummon()
	{
		if (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true) && !activatedCardIdList.Contains(base.Card.Id))
		{
			summoned = true;
			return true;
		}
		if (base.Duel.Turn > 1 && base.Duel.Player == 0 && base.Duel.Phase < DuelPhase.Main2 && base.Enemy.GetMonsterCount() == 0 && !base.Bot.HasInHand(75730490))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool AriannaTheLabrynthServantActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool AshBlossomActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (base.Util.GetLastChainCard().IsCode(23434538))
		{
			return false;
		}
		if (DefaultAshBlossomAndJoyousSpring())
		{
			if (base.Util.GetLastChainCard().Location == CardLocation.MonsterZone)
			{
				currentNegateMonsterList.Add(base.Util.GetLastChainCard());
			}
			return true;
		}
		return false;
	}

	public bool MaxxCActivate()
	{
		if (CheckWhetherNegated() || base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		return DefaultMaxxC();
	}

	public bool FurnitureSetWelcomeActivate()
	{
		if (furnitureActivating && (base.Card.Location == CardLocation.Hand || !DefaultOnBecomeTarget()))
		{
			return false;
		}
		if (base.Util.ChainContainsCard(new int[3] { 90448279, 15693423, 46772449 }))
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Grave)
		{
			bool flag = base.Card.Location == CardLocation.MonsterZone && DefaultOnBecomeTarget() && !escapeTargetList.Contains(base.Card);
			bool flag2 = base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(5380979, 92714517));
			if (flag2)
			{
				List<ClientCard> source = base.Bot.Hand.Where((ClientCard card) => card != base.Card).ToList();
				flag2 &= source.Count() <= 2 && source.All((ClientCard card) => card.IsCode(23434538, 14558127));
			}
			bool flag3 = flag;
			if (CheckRemainInDeck(92714517) > 0 && cooclockAffected && !activatedCardIdList.Contains(92714517) && (base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) || CheckBigWelcomeCanSpSummon(2347656)) && !base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517) && !setTrapThisTurn.Contains(card)) && (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)) || (base.Bot.HasInGraveyard(2511) && !activatedCardIdList.Contains(2512)) || (base.Bot.HasInHand(81497285) && !activatedCardIdList.Contains(81497285))) && ShouldSetBigWelcome())
			{
				bool force = flag | (GetProblematicEnemyCardList().Count() > 0);
				ClientCard clientCard = FurnitureGetCost(force);
				if (clientCard != null)
				{
					base.AI.SelectCard(clientCard);
					base.AI.SelectNextCard(92714517);
					activatedCardIdList.Add(base.Card.Id);
					furnitureActivating = true;
					SelectSTPlace(null, avoidImpermanence: true);
					return true;
				}
			}
			bool flag4 = (cooclockActivating || cooclockAffected) && activatedCardIdList.Contains(2512) && base.Card.Location == CardLocation.MonsterZone && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card != base.Card && card.HasSetcode(382)) && setTrapThisTurn.Count() > 0;
			flag3 |= base.Duel.Phase > DuelPhase.Main2 && !flag2 && !flag4;
			flag3 |= base.Bot.HasInGraveyard(new List<int> { 5380979, 92714517 }) && base.Bot.HasInHand(6351147) && !activatedCardIdList.Contains(6351147);
			if (base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && card.IsCode(92714517) && card.Location == CardLocation.SpellZone) && (base.Bot.GetMonsterCount() != 1 || base.Card.Location != CardLocation.MonsterZone))
			{
				flag3 |= !flag2 && base.Bot.GetMonsters().Any((ClientCard card) => card != base.Card) && !base.Bot.HasInGraveyard(base.Card.Id) && !activatedCardIdList.Contains(base.Card.Id + 1);
			}
			if (flag3 | (!base.Util.ChainContainPlayer(0) && base.Duel.Player == 1 && base.Bot.UnderAttack && base.Bot.GetMonsterCount() == 0 && base.Bot.HasInGraveyard(2511) && !activatedCardIdList.Contains(2512) && (!base.Bot.HasInHand(81497285) || activatedCardIdList.Contains(81497285))))
			{
				ClientCard clientCard2 = FurnitureGetCost(flag);
				if (clientCard2 != null)
				{
					base.AI.SelectCard(clientCard2);
					activatedCardIdList.Add(base.Card.Id);
					furnitureActivating = true;
					if (base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517)) | (base.Bot.GetMonsterCount() == 0 && !base.Bot.HasInHandOrInSpellZone(5380979) && (!base.Bot.HasInGraveyard(2511) || activatedCardIdList.Contains(2512)) && ((base.Duel.Player == 0 && base.Duel.Phase > DuelPhase.Main2) || !base.Bot.Hand.Any((ClientCard card) => card != base.Card && card.Level <= 4))))
					{
						base.AI.SelectNextCard(5380979);
					}
					else
					{
						base.AI.SelectNextCard(92714517);
					}
					SelectSTPlace(null, avoidImpermanence: true);
					return true;
				}
			}
		}
		return false;
	}

	public ClientCard FurnitureGetCost(bool force = false, List<ClientCard> ignoreList = null)
	{
		if (ignoreList == null)
		{
			ignoreList = new List<ClientCard>();
		}
		foreach (int checkId in new List<int> { 6351147, 2347656, 73602965, 37629703, 74018812, 5380979 })
		{
			ClientCard clientCard = base.Bot.Hand.FirstOrDefault((ClientCard card) => !ignoreList.Contains(card) && card.IsCode(checkId) && card != base.Card);
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		List<ClientCard> list = base.Bot.Hand.Where((ClientCard card) => !ignoreList.Contains(card)).ToList();
		List<int> list2 = new List<int>(list.Count());
		foreach (ClientCard item in list)
		{
			if (!base.Duel.CurrentChain.Contains(item))
			{
				if (list2.Contains(item.Id))
				{
					return item;
				}
				list2.Add(item.Id);
			}
		}
		List<int> list3 = new List<int> { 10045474, 83326048, 41165831, 53417695, 30748475, 2511, 75730490, 5380979, 49238328, 81497285 };
		if (force)
		{
			list3.AddRange(new List<int> { 1225009, 14558127, 92714517, 23434538 });
		}
		foreach (int checkId2 in list3)
		{
			ClientCard clientCard2 = list.FirstOrDefault((ClientCard card) => !base.Duel.CurrentChain.Contains(card) && card.IsCode(checkId2) && !base.Duel.CurrentChain.Contains(card));
			if (clientCard2 != null)
			{
				return clientCard2;
			}
		}
		foreach (int checkId3 in list3)
		{
			ClientCard clientCard3 = list.FirstOrDefault((ClientCard card) => card.IsCode(checkId3) && !base.Duel.CurrentChain.Contains(card));
			if (clientCard3 != null)
			{
				return clientCard3;
			}
		}
		return null;
	}

	public bool ShouldSetBigWelcome(bool checkArianna = true)
	{
		if (CheckWhetherWillbeRemoved())
		{
			return false;
		}
		bool flag = GetProblematicEnemyCardList().Count() > 0;
		flag |= base.Duel.Player == 1 && base.Duel.Phase > DuelPhase.Main2;
		flag |= base.Duel.Player == 1 && GetProblematicEnemyCardList().Count() == 0 && GetProblematicEnemyMonster(0, canBeTarget: false, ignoreCurrentDestroy: false, CardType.Monster) == null && base.Enemy.Hand.Count() == 1;
		if (checkArianna)
		{
			flag |= base.Duel.Player == 0 && !summoned && base.Bot.HasInHandOrHasInMonstersZone(1225009) && !activatedCardIdList.Contains(1225009);
		}
		return flag | (base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2);
	}

	public bool LabrynthCooclockActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			bool flag = false;
			if (base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) || CheckBigWelcomeCanSpSummon(2347656))
			{
				flag |= ShouldSetBigWelcome();
			}
			if ((flag & !activatedCardIdList.Contains(92714517)) && !base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && !setTrapThisTurn.Contains(card) && card.IsCode(92714517)))
			{
				bool flag2 = base.Duel.Player == 0 && base.Bot.HasInHand(92714517) && (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)) || (!summoned && base.Bot.Hand.Any((ClientCard card) => card != base.Card && card.HasType(CardType.Monster) && card.Level <= 4 && card.HasSetcode(382))));
				if (CheckRemainInDeck(92714517) > 0)
				{
					foreach (int checkId in new List<int> { 37629703, 74018812 })
					{
						if (!activatedCardIdList.Contains(checkId) && CheckCalledbytheGrave(checkId) <= 0 && ((base.Bot.HasInHand(checkId) && base.Bot.Hand.Count > 2) || (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.IsDisabled() && card.IsCode(checkId)) && base.Bot.Hand.Count > 1)))
						{
							flag2 = true;
							break;
						}
					}
				}
				if (flag2)
				{
					activatedCardIdList.Add(base.Card.Id);
					cooclockActivating = true;
					return true;
				}
			}
			bool flag3 = base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382));
			if ((base.Duel.Player == 1 && base.Duel.Phase <= DuelPhase.Main2 && setTrapThisTurn.Any((ClientCard card) => !activatedCardIdList.Contains(card.Id)) && flag3) | (base.Duel.Player == 1 && activatedCardIdList.Contains(81497286) && (base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.Type == 4) || base.Util.ChainContainsCard(81497285))) | (setTrapThisTurn.Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517) && !activatedCardIdList.Contains(92714517)) && flag3) | (setTrapThisTurn.Any((ClientCard card) => card.IsFacedown() && card.IsCode(5380979) && !activatedCardIdList.Contains(5380979)) && flag3))
			{
				activatedCardIdList.Add(base.Card.Id);
				cooclockActivating = true;
				return true;
			}
		}
		return false;
	}

	public bool RecycleActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (base.Card.IsCode(74018812, 73602965))
			{
				if (CheckShouldNoMoreSpSummon() || GetEmptyMainMonsterZoneCount() + chainSummoningIdList.Count() <= 0)
				{
					return false;
				}
				chainSummoningIdList.Add(base.Card.Id);
			}
			if (base.Card.IsCode(5380979))
			{
				SelectSTPlace(base.Card);
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool ForLinkSummon()
	{
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(304)))
		{
			return false;
		}
		if (base.Card.Level > 4)
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon())
		{
			return false;
		}
		if (!base.Bot.HasInExtra(24269961))
		{
			return false;
		}
		List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !card.HasRace(CardRace.Fiend));
		int num = canBeUsedForLinkMaterial.Count();
		if (!activatedCardIdList.Contains(41165831) && base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown()) && (!activatedCardIdList.Contains(24269961) || base.Bot.HasInHand(41165831)))
		{
			num++;
		}
		if (num != 2 && (num != 1 || !canBeUsedForLinkMaterial.Any((ClientCard card) => card.IsCode(24269961))))
		{
			return false;
		}
		if (!base.Bot.HasInExtra(24269961) && !canBeUsedForLinkMaterial.Any((ClientCard card) => card.HasSetcode(304)))
		{
			return false;
		}
		if ((0u | ((base.Enemy.GetMonsterCount() > 0 && base.Bot.HasInExtra(93084621)) ? 1u : 0u) | (base.Bot.HasInExtra(67680512) ? 1u : 0u)) != 0)
		{
			NamedCard namedCard = NamedCard.Get(base.Card.Id);
			if (namedCard == null)
			{
				return false;
			}
			if (namedCard.Race != 8)
			{
				return false;
			}
			foreach (ClientCard item in base.Bot.Hand)
			{
				NamedCard namedCard2 = NamedCard.Get(item.Id);
				if (namedCard2 != null && namedCard2.HasType(CardType.Monster) && namedCard2.Level <= 4 && namedCard2.Attack < namedCard.Attack)
				{
					return false;
				}
			}
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ForSynchroSummon()
	{
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(304)))
		{
			return false;
		}
		if (!base.Card.IsCode(new List<int> { 74018812, 75730490, 1225009 }))
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon())
		{
			return false;
		}
		if (!base.Bot.HasInExtra(22850702) || dimensionalBarrierAnnouced.Contains(1063))
		{
			return false;
		}
		if (GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
		{
			_ = !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		}
		else
			_ = 0;
		if (base.Card.IsCode(74018812))
		{
			if (!base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.HasType((CardType)75497472) && card.Level == 8 && card.HasAttribute((CardAttribute)48)))
			{
				return false;
			}
			summoned = true;
			return true;
		}
		if (!base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.HasType((CardType)75497472) && card.Level == 6 && card.HasAttribute((CardAttribute)48)))
		{
			return false;
		}
		summoned = true;
		return true;
	}

	public bool ForAnimaSummon()
	{
		if (banSpSummonExceptFiendCount > 0 || !base.Bot.HasInExtra(94259633))
		{
			return false;
		}
		if (CheckWhetherNegated() || base.Duel.Turn == 1)
		{
			return false;
		}
		bool flag = base.Bot.MonsterZone[1] == null && base.Enemy.MonsterZone[6] != null && base.Enemy.MonsterZone[6].HasType(CardType.Link) && base.Enemy.MonsterZone[6].HasLinkMarker(CardLinkMarker.Top);
		flag |= base.Bot.MonsterZone[3] == null && base.Enemy.MonsterZone[5] != null && base.Enemy.MonsterZone[5].HasType(CardType.Link) && base.Enemy.MonsterZone[5].HasLinkMarker(CardLinkMarker.Top);
		if (base.Bot.GetMonstersExtraZoneCount() == 0)
		{
			flag |= base.Enemy.MonsterZone[1] != null || base.Enemy.MonsterZone[3] != null;
		}
		return flag;
	}

	public bool LabrynthForCooClockSummon()
	{
		if (!cooclockAffected)
		{
			return false;
		}
		if (base.Card.Level > 4 || !base.Card.HasSetcode(382))
		{
			return false;
		}
		if ((base.Bot.Hand.Any((ClientCard card) => (card.IsCode(5380979) && !activatedCardIdList.Contains(5380979)) || (card.IsCode(92714517) && !activatedCardIdList.Contains(92714517))) | base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && setTrapThisTurn.Contains(card) && ((card.IsCode(5380979) && !activatedCardIdList.Contains(5380979)) || (card.IsCode(92714517) && !activatedCardIdList.Contains(92714517))))) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382)))
		{
			int num = 0;
			NamedCard namedCard = NamedCard.Get(base.Card.Id);
			if (namedCard != null)
			{
				num = namedCard.Attack;
			}
			foreach (ClientCard item in base.Bot.Hand.Where((ClientCard card) => card.IsMonster() && card.Level <= 4 && card.HasSetcode(382)).ToList())
			{
				namedCard = NamedCard.Get(item.Id);
				if (namedCard != null && namedCard.Attack < num)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public bool ForBigWelcomeSummon()
	{
		if (base.Bot.HasInSpellZone(92714517) && base.Bot.GetMonsterCount() == 0 && base.Card.Level <= 4)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool PotOfExtravaganceActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
		activatedCardIdList.Add(base.Card.Id);
		base.AI.SelectOption(1);
		return true;
	}

	public bool WelcomeLabrynthActivate()
	{
		return WelcomeLabrynthActivateCheck();
	}

	public bool WelcomeLabrynthActivateCopy()
	{
		return WelcomeLabrynthActivateCheck(onlyCheck: true);
	}

	public bool WelcomeLabrynthSetCheck()
	{
		if (!CheckShouldNoMoreSpSummon())
		{
			return WelcomeLabrynthActivateCheck(onlyCheck: true, noSelect: true);
		}
		return false;
	}

	public bool WelcomeLabrynthActivateCheck(bool onlyCheck = false, bool noSelect = false)
	{
		if (base.Card.Location == CardLocation.SpellZone || onlyCheck)
		{
			if (GetEmptyMainMonsterZoneCount() == 0)
			{
				return false;
			}
			if (CheckShouldNoMoreSpSummon())
			{
				return false;
			}
			bool flag = base.Duel.Phase > DuelPhase.Main2 || (base.Card.IsCode(73602965) && (base.CurrentTiming & 4) > 0);
			bool flag2 = base.Card.Location == CardLocation.SpellZone && DefaultOnBecomeTarget();
			if (((base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2) || (base.Duel.Player == 1 && flag)) && CheckRemainInDeck(75730490) > 0 && base.Bot.HasInHandOrInSpellZone(6351147) && !chainSummoningIdList.Contains(75730490))
			{
				if (!noSelect)
				{
					chainSummoningIdList.Add(75730490);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
			if (((!base.Bot.HasInSpellZoneOrInGraveyard(92714517) || (!base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !CheckBigWelcomeCanSpSummon(2347656))) | (base.Duel.Player == 1 && flag) | (base.Duel.Player == 0)) && CheckRemainInDeck(1225009) > 0 && !activatedCardIdList.Contains(1225009) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !chainSummoningIdList.Contains(1225009))
			{
				if (!noSelect)
				{
					chainSummoningIdList.Add(1225009);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
			if (base.Bot.HasInSpellZoneOrInGraveyard(92714517) && !activatedCardIdList.Contains(92714517) && base.Bot.HasInHand(2347656) && CheckRemainInDeck(73602965) > 0 && !chainSummoningIdList.Contains(73602965) && !base.Bot.HasInMonstersZone(73602965, notDisabled: true, hasXyzMaterial: false, faceUp: true))
			{
				if (!noSelect)
				{
					chainSummoningIdList.Add(73602965);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
			if (flag2 | (base.Bot.UnderAttack && base.Bot.GetMonsterCount() == 0) | ShouldSetBigWelcome(checkArianna: false))
			{
				if (!noSelect)
				{
					if (base.Bot.HasInSpellZoneOrInGraveyard(92714517) && !activatedCardIdList.Contains(92714517) && CheckRemainInDeck(2347656) > 0 && !chainSummoningIdList.Contains(2347656))
					{
						chainSummoningIdList.Add(2347656);
					}
					else if (!activatedCardIdList.Contains(1225009) && CheckRemainInDeck(1225009) > 0 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !chainSummoningIdList.Contains(1225009))
					{
						chainSummoningIdList.Add(1225009);
					}
					else if (base.Bot.HasInGraveyard(92714517) && !activatedCardIdList.Contains(92714517) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasRace(CardRace.Fiend) && card.Level >= 8) && CheckRemainInDeck(81497285) > 0 && !chainSummoningIdList.Contains(81497285))
					{
						chainSummoningIdList.Add(81497285);
					}
					else
					{
						int num = 0;
						foreach (int item in new List<int> { 74018812, 37629703, 2511 })
						{
							if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(item) && CheckRemainInDeck(item) > 0 && !chainSummoningIdList.Contains(item))
							{
								num = item;
								break;
							}
						}
						List<int> list = new List<int> { 81497285, 74018812, 37629703, 2511, 73602965, 75730490, 1225009 };
						if (num == 0)
						{
							foreach (int item2 in list)
							{
								if (CheckRemainInDeck(item2) > 0 && !chainSummoningIdList.Contains(item2))
								{
									num = item2;
									break;
								}
							}
						}
						if (num > 0)
						{
							chainSummoningIdList.Add(num);
						}
					}
					ResetCooclockEffect(onlyCheck);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
		}
		return false;
	}

	public bool TransactionRollbackActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			foreach (KeyValuePair<int, Func<bool>> item in new SortedList<int, Func<bool>>
			{
				{ 92714517, BigWelcomeLabrynthActivateCopy },
				{ 83326048, DimensionalBarrierActivate },
				{ 53417695, EscapeOfTheUnchainedActivateCopy },
				{ 10045474, InfiniteImpermanenceActivateCopy },
				{ 5380979, WelcomeLabrynthActivateCopy },
				{ 30748475, DestructiveDarumaKarmaCannonActivate }
			})
			{
				if (base.Bot.HasInGraveyard(item.Key) && item.Value())
				{
					rollbackCopyCardId = item.Key;
					base.AI.SelectCard(item.Key);
					return true;
				}
			}
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			foreach (KeyValuePair<int, Func<bool>> item2 in new SortedList<int, Func<bool>>
			{
				{ 5380979, WelcomeLabrynthActivateCopy },
				{ 94192409, base.DefaultCompulsoryEvacuationDevice },
				{ 30748475, DestructiveDarumaKarmaCannonActivate },
				{ 83326048, DimensionalBarrierActivate },
				{ 53417695, EscapeOfTheUnchainedActivateCopy },
				{ 10045474, InfiniteImpermanenceActivateCopy },
				{ 78474168, base.DefaultBreakthroughSkill },
				{ 92714517, BigWelcomeLabrynthActivateCopy }
			})
			{
				if (base.Enemy.HasInGraveyard(item2.Key) && item2.Value())
				{
					rollbackCopyCardId = item2.Key;
					base.AI.SelectCard(item2.Key);
					ResetCooclockEffect(onlyCheck: false);
					return true;
				}
			}
		}
		return false;
	}

	public bool InfiniteImpermanenceActivate()
	{
		return InfiniteImpermanenceActivateCheck();
	}

	public bool InfiniteImpermanenceActivateCopy()
	{
		return InfiniteImpermanenceActivateCheck(onlyCheck: true);
	}

	public bool InfiniteImpermanenceSetCheck()
	{
		return InfiniteImpermanenceActivateCheck(onlyCheck: true, noSelect: true);
	}

	public bool InfiniteImpermanenceActivateCheck(bool onlyCheck = false, bool noSelect = false)
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Card.Location == CardLocation.SpellZone)
		{
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < 5; i++)
			{
				if (base.Bot.SpellZone[i] == base.Card)
				{
					num = i;
				}
				if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.SpellZone && base.Enemy.SpellZone[i] == lastChainCard)
				{
					num2 = i;
				}
				else if (base.Duel.Player == 0 && base.Util.GetProblematicEnemySpell() != null && base.Enemy.SpellZone[i] != null && base.Enemy.SpellZone[i].IsFloodgate())
				{
					num2 = i;
				}
			}
			if ((num * num2 >= 0 && num + num2 == 4) || base.Util.IsChainTarget(base.Card) || (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.IsCode(18144506)))
			{
				ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Trap);
				if (!noSelect)
				{
					if (problematicEnemyMonster != null)
					{
						base.AI.SelectCard(problematicEnemyMonster);
					}
					else
					{
						base.AI.SelectCard(base.Enemy.GetMonsters());
					}
				}
				if (!onlyCheck)
				{
					infiniteImpermanenceList.Add(num2);
					if (cooclockAffected && setTrapThisTurn.Contains(base.Card))
					{
						cooclockAffected = false;
						setTrapThisTurn.Remove(base.Card);
					}
				}
				return true;
			}
		}
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeTarget: true, CardType.Trap);
		if (monsterListForTargetNegate.Count() > 0)
		{
			ClientCard clientCard = monsterListForTargetNegate[0];
			currentNegateMonsterList.Add(clientCard);
			if (base.Card.Location == CardLocation.SpellZone && !onlyCheck)
			{
				for (int j = 0; j < 5; j++)
				{
					if (base.Bot.SpellZone[j] == base.Card)
					{
						infiniteImpermanenceList.Add(j);
						break;
					}
				}
			}
			if (base.Card.Location == CardLocation.Hand)
			{
				SelectSTPlace(base.Card, avoidImpermanence: true);
			}
			if (!noSelect)
			{
				base.AI.SelectCard(clientCard);
			}
			currentDestroyCardList.Add(clientCard);
			ResetCooclockEffect(onlyCheck);
			return true;
		}
		return false;
	}

	public bool DestructiveDarumaKarmaCannonActivate()
	{
		return DestructiveDarumaKarmaCannonActivateCheck();
	}

	public bool DestructiveDarumaKarmaCannonSetCheck()
	{
		return DestructiveDarumaKarmaCannonActivateCheck(noSelect: true);
	}

	public bool DestructiveDarumaKarmaCannonActivateCheck(bool noSelect = false)
	{
		bool flag = base.Card.Location == CardLocation.SpellZone && DefaultOnBecomeTarget() && base.Util.IsOneEnemyBetter(onlyATK: true);
		bool flag2 = ((!activatedCardIdList.Contains(92714517) && base.Bot.GetSpells().Any((ClientCard card) => card.IsFacedown() && card.IsCode(92714517) && (!cooclockAffected || !setTrapThisTurn.Contains(card)))) || base.Util.ChainContainsCard(92714517)) && (base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) || (CheckBigWelcomeCanSpSummon(2347656) && base.Bot.GetMonsterCount() > 0)) && !activatedCardIdList.Contains(2347657);
		flag |= base.Bot.UnderAttack && (base.Bot.BattlingMonster?.GetDefensePower() ?? 0) <= (base.Enemy.BattlingMonster?.GetDefensePower() ?? 0) && !base.Util.ChainContainPlayer(0) && !flag2;
		flag |= base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2 && base.Bot.GetMonsterCount() == 0 && base.Enemy.GetMonsterCount() > 0;
		flag |= base.Enemy.HasInMonstersZone(86066372, notDisabled: true) && !base.Util.ChainContainPlayer(0);
		int num = 0;
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (!monster.IsFacedown())
			{
				num = (monster.HasType(CardType.Link) ? (num + monster.LinkCount) : (num + 1));
			}
		}
		if (flag | (num >= 6 && base.Util.IsOneEnemyBetter(onlyATK: true)))
		{
			if (!noSelect)
			{
				currentDestroyCardList.AddRange(base.Enemy.GetMonsters());
				escapeTargetList.AddRange(base.Bot.GetMonsters());
			}
			return true;
		}
		return false;
	}

	public bool EscapeOfTheUnchainedActivate()
	{
		return EscapeOfTheUnchainedActivateCheck();
	}

	public bool EscapeOfTheUnchainedActivateCopy()
	{
		return EscapeOfTheUnchainedActivateCheck(onlyCheck: true);
	}

	public bool EscapeOfTheUnchainedActivateCheck(bool onlyCheck = false, bool noSelect = false)
	{
		if (base.Card.Location == CardLocation.SpellZone || onlyCheck)
		{
			ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsFaceup() && card.HasSetcode(304) && base.Duel.ChainTargets.Contains(card) && !escapeTargetList.Contains(card));
			if (clientCard == null)
			{
				clientCard = (from card in base.Bot.GetMonsters()
					where card.IsFaceup() && card.HasSetcode(304)
					orderby card.Attack
					select card).FirstOrDefault();
			}
			if (clientCard == null)
			{
				return false;
			}
			List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap);
			if (problematicEnemyCardList.Count() > 0 && base.Duel.LastChainPlayer != 0)
			{
				if (!noSelect)
				{
					base.AI.SelectCard(clientCard);
					base.AI.SelectNextCard(problematicEnemyCardList);
					escapeTargetList.Add(clientCard);
					currentDestroyCardList.Add(problematicEnemyCardList[0]);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
			int botBestPower = base.Util.GetBestPower(base.Bot);
			if (base.Duel.Player == 1 && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
			{
				List<ClientCard> list = (from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && card.Attack >= botBestPower && !currentDestroyCardList.Contains(card) && !card.IsShouldNotBeTarget() && !card.IsShouldNotBeSpellTrapTarget()
					orderby card.Attack descending
					select card).ToList();
				if (list.Count() > 0)
				{
					if (!noSelect)
					{
						base.AI.SelectCard(clientCard);
						base.AI.SelectNextCard(list);
						escapeTargetList.Add(clientCard);
						currentDestroyCardList.Add(list[0]);
						activatedCardIdList.Add(base.Card.Id);
					}
					return true;
				}
			}
			if ((base.Duel.Player == 1 && base.Duel.Phase > DuelPhase.Main2 && ((base.Bot.HasInGraveyard(24269961) && !activatedCardIdList.Contains(24269962)) || (base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !activatedCardIdList.Contains(2347657)))) | (DefaultOnBecomeTarget() && base.Card.Location == CardLocation.SpellZone && !base.Util.ChainContainsCard(15693423)))
			{
				List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
				if (normalEnemyTargetList.Count() > 0)
				{
					if (!noSelect)
					{
						base.AI.SelectCard(clientCard);
						base.AI.SelectNextCard(normalEnemyTargetList);
						escapeTargetList.Add(clientCard);
						currentDestroyCardList.Add(normalEnemyTargetList[0]);
						activatedCardIdList.Add(base.Card.Id);
					}
					return true;
				}
			}
			return false;
		}
		if (!noSelect)
		{
			base.AI.SelectCard(41165831);
			activatedCardIdList.Add(base.Card.Id + 1);
		}
		return true;
	}

	public bool DimensionalBarrierActivate()
	{
		if (base.Duel.Player == 0 && base.Duel.Turn == 1)
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		foreach (KeyValuePair<int, Func<bool>> item in new Dictionary<int, Func<bool>>
		{
			{ 1057, DimensionalBarrierForRitual },
			{ 1056, DimensionalBarrierForFusion },
			{ 1063, DimensionalBarrierForSynchro },
			{ 1073, DimensionalBarrierForXyz },
			{ 1074, DimensionalBarrierForPendulum }
		})
		{
			if (!dimensionalBarrierAnnouced.Contains(item.Key) && item.Value())
			{
				ResetCooclockEffect(onlyCheck: false);
				return true;
			}
		}
		return DefaultOnBecomeTarget();
	}

	public bool DimensionalBarrierForRitual()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && item.Controller == 1 && !item.IsDisabled() && item.HasType(CardType.Ritual) && (item.HasType(CardType.Spell) || (item.Location == CardLocation.MonsterZone && !currentNegateMonsterList.Contains(item))))
			{
				if (dimensionBarrierAnnouncing)
				{
					currentNegateMonsterList.Add(item);
				}
				return true;
			}
		}
		return false;
	}

	public bool DimensionalBarrierForFusion()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && item.Controller == 1 && !item.IsDisabled() && (item.IsFusionSpell() || (item.HasType(CardType.Fusion) && item.Location == CardLocation.MonsterZone && !currentNegateMonsterList.Contains(item))))
			{
				if (dimensionBarrierAnnouncing)
				{
					currentNegateMonsterList.Add(item);
				}
				return true;
			}
		}
		return false;
	}

	public bool DimensionalBarrierForSynchro()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && item.Controller == 1 && !item.IsDisabled() && item.HasType(CardType.Synchro) && item.Location == CardLocation.MonsterZone && !currentNegateMonsterList.Contains(item))
			{
				if (dimensionBarrierAnnouncing)
				{
					currentNegateMonsterList.Add(item);
				}
				return true;
			}
		}
		if (base.Duel.Player == 1 && !base.Util.ChainContainsCard(30748475) && base.Enemy.ExtraDeck.Count() > 0)
		{
			bool flag = false;
			bool flag2 = false;
			foreach (ClientCard monster in base.Enemy.GetMonsters())
			{
				if (!monster.IsFacedown() && !monster.HasType((CardType)75497472))
				{
					if (monster.HasType(CardType.Tuner))
					{
						flag = true;
					}
					else
					{
						flag2 = true;
					}
				}
			}
			if (flag && flag2)
			{
				return true;
			}
		}
		return false;
	}

	public bool DimensionalBarrierForXyz()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && item.Controller == 1 && !item.IsDisabled() && item.HasType(CardType.Xyz) && item.Location == CardLocation.MonsterZone && !currentNegateMonsterList.Contains(item))
			{
				if (dimensionBarrierAnnouncing)
				{
					currentNegateMonsterList.Add(item);
				}
				return true;
			}
		}
		if (base.Duel.Player == 1 && !base.Util.ChainContainsCard(30748475) && base.Enemy.ExtraDeck.Count() > 0)
		{
			List<int> list = new List<int>(6);
			foreach (ClientCard monster in base.Enemy.GetMonsters())
			{
				if (monster.IsFacedown())
				{
					continue;
				}
				if (monster.IsOneForXyz())
				{
					return true;
				}
				if (monster.HasType((CardType)8404992))
				{
					continue;
				}
				int level = monster.Level;
				if (level == 2 || !monster.HasType(CardType.Link))
				{
					if (list.Contains(level))
					{
						return true;
					}
					list.Add(level);
				}
			}
		}
		return false;
	}

	public bool DimensionalBarrierForPendulum()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && item.Controller == 1 && !item.IsDisabled() && item.HasType(CardType.Pendulum) && item.Location == CardLocation.MonsterZone && !currentNegateMonsterList.Contains(item))
			{
				if (dimensionBarrierAnnouncing)
				{
					currentNegateMonsterList.Add(item);
				}
				return true;
			}
		}
		ClientCard clientCard = base.Enemy.SpellZone[6];
		ClientCard clientCard2 = base.Enemy.SpellZone[7];
		if (clientCard != null && clientCard2 != null && clientCard.LScale != clientCard2.RScale)
		{
			return true;
		}
		return false;
	}

	public bool BigWelcomeLabrynthActivate()
	{
		return BigWelcomeLabrynthActivateCheck();
	}

	public bool BigWelcomeLabrynthBecomeTargetActivate()
	{
		if (DefaultOnBecomeTarget())
		{
			return BigWelcomeLabrynthActivateCheck();
		}
		return false;
	}

	public bool BigWelcomeLabrynthActivateCopy()
	{
		return BigWelcomeLabrynthActivateCheck(onlyCheck: true);
	}

	public bool BigWelcomeLabrynthSetCheck()
	{
		if (!CheckShouldNoMoreSpSummon())
		{
			return BigWelcomeLabrynthActivateCheck(onlyCheck: true, noSelect: true);
		}
		return false;
	}

	public bool BigWelcomeLabrynthActivateCheck(bool onlyCheck = false, bool noSelect = false)
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location != CardLocation.SpellZone && !onlyCheck)
		{
			return false;
		}
		if (GetEmptyMainMonsterZoneCount() == 0)
		{
			return false;
		}
		bool flag = base.Duel.Phase > DuelPhase.Main2 || (base.Card.IsCode(73602965) && (base.CurrentTiming & 4) > 0);
		bool flag2 = GetProblematicEnemyCardList().Count() > 0;
		flag2 |= activatedCardIdList.Contains(1225009) && flag;
		flag2 |= base.Bot.UnderAttack && (base.Bot.BattlingMonster?.GetDefensePower() ?? 0) <= (base.Enemy.BattlingMonster?.GetDefensePower() ?? 0) && base.Duel.LastChainPlayer != 0;
		flag2 |= base.Duel.Turn == 1 && base.Duel.Player == 0 && !activatedCardIdList.Contains(2347657);
		flag2 |= base.Duel.Turn == 1 && base.Enemy.GetMonsterCount() == 0 && base.Enemy.GetSpellCount() == 0 && base.Enemy.Hand.Count > 0 && (base.CurrentTiming & 4) > 0;
		bool flag3 = false;
		bool flag4 = false;
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null)
			{
				if (item.Controller == 1)
				{
					flag3 = true;
					break;
				}
				if (item.IsCode(5380979, 6351147, 81497285))
				{
					flag4 = true;
				}
			}
		}
		if (flag4 && !flag3)
		{
			return false;
		}
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (base.Duel.ChainTargets.Contains(monster) && !escapeTargetList.Contains(monster) && (!monster.IsCode(67680512, 93084621) || !base.Duel.CurrentChain.Contains(monster)))
			{
				Logger.DebugWriteLine("[BigWelcome]escape target");
				if (!noSelect)
				{
					bigwelcomeEscaseTarget = monster;
					escapeTargetList.Add(monster);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
		}
		if (base.Bot.GetMonsterCount() > 0)
		{
			bool flag5 = flag2 && !activatedCardIdList.Contains(2347657) && (base.Util.ChainContainPlayer(1) || base.Duel.LastChainPlayer != 0);
			bool flag6 = DefaultOnBecomeTarget();
			bool flag7 = base.Duel.Player == 1 && !activatedCardIdList.Contains(92714517) && flag;
			bool flag8 = base.Duel.Player == 0 && base.Duel.LastChainPlayer != 0 && !activatedCardIdList.Contains(92714517);
			Logger.DebugWriteLine("[BigWelcome count>0]flag: " + flag5 + " " + flag6 + " " + flag7 + " " + flag8);
			flag2 = flag2 || flag7;
			if (flag5 || flag6 || flag7 || flag8)
			{
				if (CheckBigWelcomeCanSpSummon(2347656))
				{
					_ = !activatedCardIdList.Contains(2347657);
				}
				else
					_ = 0;
				base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.IsCode(2347656));
				if (!noSelect)
				{
					activatedCardIdList.Add(base.Card.Id);
				}
				ResetCooclockEffect(onlyCheck);
				return true;
			}
		}
		else if ((DefaultOnBecomeTarget() | (base.Duel.Player == 1 && !activatedCardIdList.Contains(92714517) && flag) | (base.Duel.Player == 0 && !summoned && !base.Bot.HasInHand(1225009) && !activatedCardIdList.Contains(1225009) && (base.Duel.Phase >= DuelPhase.Main1 || !base.Bot.HasInHand(49238328) || base.Bot.ExtraDeck.Count() < 3) && !base.Duel.CurrentChain.Any((ClientCard card) => card.IsCode(49238328) && card.Controller == 0))) && !noSelect)
		{
			activatedCardIdList.Add(base.Card.Id);
			ResetCooclockEffect(onlyCheck);
			return true;
		}
		return false;
	}

	public bool BigWelcomeLabrynthGraveActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (base.Bot.GetMonsters().Any((ClientCard card) => card.Level >= 8 && card.IsFaceup() && card.HasRace(CardRace.Fiend) && !card.HasType((CardType)75497472)))
			{
				ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(-1, canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
				if (problematicEnemyMonster != null)
				{
					base.AI.SelectCard(problematicEnemyMonster);
					currentDestroyCardList.Add(problematicEnemyMonster);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
				if (!base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) || (activatedCardIdList.Contains(2347656) && activatedCardIdList.Contains(2347657)))
				{
					List<ClientCard> problemEnemySpellList = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && c.IsFaceup() && c.IsFloodgate() && !c.IsShouldNotBeTarget() && (c.HasType(CardType.Trap) || base.Duel.Player == 0)).ToList();
					problemEnemySpellList.AddRange(base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && c.IsFaceup() && !problemEnemySpellList.Contains(c) && c.HasType((CardType)17694720) && !c.IsShouldNotBeTarget() && (c.HasType(CardType.Trap) || base.Duel.Player == 0)).ToList());
					if (problemEnemySpellList.Count() > 0)
					{
						base.AI.SelectCard(problemEnemySpellList);
						currentDestroyCardList.Add(problemEnemySpellList[0]);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
				int botBestPower = base.Util.GetBestPower(base.Bot);
				if (base.Duel.Player == 1 && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
				{
					List<ClientCard> list = (from card in base.Enemy.GetMonsters()
						where card.IsFaceup() && card.Attack >= botBestPower && !currentDestroyCardList.Contains(card) && !card.IsShouldNotBeTarget() && !card.IsShouldNotBeSpellTrapTarget()
						orderby card.Attack descending
						select card).ToList();
					if (list.Count() > 0)
					{
						base.AI.SelectCard(list);
						currentDestroyCardList.Add(list[0]);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
				if (base.Duel.Phase > DuelPhase.Main2)
				{
					List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
					if (normalEnemyTargetList.Count() > 0)
					{
						base.AI.SelectCard(normalEnemyTargetList);
						currentDestroyCardList.Add(normalEnemyTargetList[0]);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
			List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.HasRace(CardRace.Fiend)
				select card).ToList();
			foreach (ClientCard item in list2)
			{
				if (base.Duel.ChainTargets.Contains(item) && !escapeTargetList.Contains(item) && (!item.IsCode(67680512, 93084621) || !base.Duel.CurrentChain.Contains(item)))
				{
					base.AI.SelectCard(item);
					escapeTargetList.Add(item);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			if (base.Duel.Player == 0 && base.Duel.Phase <= DuelPhase.Main2 && !summoned && !base.Bot.HasInHand(1225009) && !activatedCardIdList.Contains(1225009) && !chainSummoningIdList.Contains(1225009))
			{
				ClientCard clientCard = list2.FirstOrDefault((ClientCard card) => card.IsCode(1225009));
				if (clientCard != null)
				{
					base.AI.SelectCard(clientCard);
					escapeTargetList.Add(clientCard);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			List<ClientCard> list3 = new List<ClientCard>(base.Bot.Hand);
			list3.AddRange(base.Bot.GetMonsters());
			if ((CheckRemainInDeck(5380979, 92714517) == 0 || !list3.Any((ClientCard card) => card.IsCode(37629703, 74018812))) && base.Duel.LastChainPlayer < 0 && base.Duel.Player == 0 && !base.Bot.HasInMonstersZone(2347656, notDisabled: true, hasXyzMaterial: false, faceUp: true) && (!cooclockAffected || !base.Bot.HasInHandOrInSpellZone(92714517)))
			{
				int num = 0;
				foreach (int item2 in new List<int> { 37629703, 74018812, 5380979 })
				{
					if (base.Bot.HasInGraveyard(item2) && !activatedCardIdList.Contains(item2 + 1))
					{
						num++;
					}
				}
				if (num > 0)
				{
					ClientCard clientCard2 = list2.FirstOrDefault((ClientCard card) => card.IsFaceup() && card.HasRace(CardRace.Fiend) && ((card.Level <= 4 && !card.HasType((CardType)75505664)) || card.IsCode(81497285)));
					if (clientCard2 != null)
					{
						base.AI.SelectCard(clientCard2);
						escapeTargetList.Add(clientCard2);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool ChaosAngelSpSummonWith2Monster()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		List<ClientCard> list3 = new List<ClientCard>();
		List<ClientCard> list4 = new List<ClientCard>();
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (monster.IsFaceup() && !monster.HasType((CardType)75497472) && monster.HasAttribute((CardAttribute)48))
			{
				if (monster.Level == 2)
				{
					list.Add(monster);
				}
				if (monster.Level == 4)
				{
					list2.Add(monster);
				}
				if (monster.Level == 6)
				{
					list3.Add(monster);
				}
				if (monster.Level == 8)
				{
					list4.Add(monster);
				}
			}
		}
		list.Sort(CompareUsableAttack);
		list2.Sort(CompareUsableAttack);
		list3.Sort(CompareUsableAttack);
		list4.Sort(CompareUsableAttack);
		bool flag = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster();
		if (bestEnemyMonster != null && base.Util.GetBestPower(base.Bot, onlyATK: true) <= base.Util.GetBestPower(base.Enemy))
		{
			flag |= base.Util.GetBestPower(base.Enemy) <= 3500;
			flag |= !bestEnemyMonster.IsShouldNotBeTarget() && !bestEnemyMonster.IsShouldNotBeMonsterTarget();
		}
		if (list2.Count() > 0 && list3.Count() > 0)
		{
			List<ClientCard> list5 = new List<ClientCard>
			{
				list2[0],
				list3[0]
			};
			bool flag2 = flag;
			if (base.Enemy.GetMonsterCount() == 0 && base.Duel.Phase < DuelPhase.Main2)
			{
				flag2 |= GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list5) + 3500 >= base.Enemy.LifePoints;
			}
			if (flag2)
			{
				base.AI.SelectMaterials(list5);
				return true;
			}
		}
		if (list.Count() > 0 && list4.Count() > 0)
		{
			foreach (ClientCard item in list)
			{
				foreach (ClientCard item2 in list4)
				{
					List<ClientCard> list6 = new List<ClientCard> { item, item2 };
					if (flag && (!item2.IsCode(2347656) || item2.IsDisabled() || !base.Bot.HasInSpellZoneOrInGraveyard(92714517)))
					{
						base.AI.SelectMaterials(list6);
						return true;
					}
					if (base.Enemy.GetMonsterCount() == 0 && GetMaterialAttack(list6) < 3500 && base.Duel.Phase < DuelPhase.Main2 && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list6) + 3500 >= base.Enemy.LifePoints)
					{
						base.AI.SelectMaterials(list6);
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool ChaosAngelSpSummonWith3Monster()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (monster.IsFaceup() && !monster.HasType((CardType)75497472) && monster.HasAttribute((CardAttribute)48))
			{
				if (monster.Level == 2)
				{
					list.Add(monster);
				}
				if (monster.Level == 4)
				{
					list2.Add(monster);
				}
			}
		}
		list.Sort(CompareUsableAttack);
		list2.Sort(CompareUsableAttack);
		bool flag = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster();
		if (bestEnemyMonster != null && base.Util.GetBestPower(base.Bot, onlyATK: true) <= base.Util.GetBestPower(base.Enemy))
		{
			flag |= base.Util.GetBestPower(base.Enemy) <= 3500;
			flag |= !bestEnemyMonster.IsShouldNotBeTarget() && !bestEnemyMonster.IsShouldNotBeMonsterTarget();
		}
		if (list.Count() >= 1 && list2.Count() >= 2)
		{
			foreach (ClientCard item3 in list)
			{
				for (int i = 0; i < list2.Count() - 1; i++)
				{
					ClientCard item = list2[i];
					for (int j = i + 1; j < list2.Count(); j++)
					{
						ClientCard item2 = list2[j];
						List<ClientCard> list3 = new List<ClientCard> { item3, item, item2 };
						bool flag2 = flag;
						if (base.Enemy.GetMonsterCount() == 0 && base.Duel.Phase < DuelPhase.Main2)
						{
							flag2 |= GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list3) + 3500 >= base.Enemy.LifePoints;
						}
						if (flag2)
						{
							base.AI.SelectMaterials(list3);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public bool ChaosAngelActivate()
	{
		List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster);
		if (normalEnemyTargetList.Count() > 0)
		{
			base.AI.SelectCard(normalEnemyTargetList);
			currentDestroyCardList.Add(normalEnemyTargetList[0]);
			return true;
		}
		return false;
	}

	public bool SummonForTYPHONCheck()
	{
		if (!base.Bot.HasInExtra(93039339) || base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup()))
		{
			return false;
		}
		if (enemySpSummonFromExLastTurn < 2 && enemySpSummonFromExThisTurn < 2)
		{
			return false;
		}
		if (base.Card.Level > 4)
		{
			return false;
		}
		int num = 0;
		NamedCard namedCard = NamedCard.Get(base.Card.Id);
		if (namedCard != null)
		{
			num = namedCard.Attack;
		}
		foreach (ClientCard item in base.Bot.Hand.Where((ClientCard card) => card.IsMonster() && card.Level <= 4).ToList())
		{
			namedCard = NamedCard.Get(item.Id);
			if (namedCard != null && namedCard.Attack < num)
			{
				return false;
			}
		}
		return true;
	}

	public bool SuperStarslayerTYPHONSpSummon()
	{
		ClientCard clientCard = (from card in base.Bot.GetMonsters()
			where card.IsFaceup()
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard == null || (clientCard.Attack >= 2900 && clientCard.Owner == 0))
		{
			return false;
		}
		if (((GetProblematicEnemyMonster(clientCard.Attack) != null) | (clientCard.Level <= 4)) & (!clientCard.HasType(CardType.Link) || base.Duel.Phase < DuelPhase.Main2))
		{
			Logger.DebugWriteLine("*** TYPHON select: " + clientCard.Name);
			base.AI.SelectMaterials(clientCard);
			return true;
		}
		return false;
	}

	public bool SuperStarslayerTYPHONActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> targetList = new List<ClientCard>();
		targetList.AddRange(from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card) && card.IsFloodgate() && card.IsFaceup()
			orderby card.Attack descending
			select card);
		targetList.AddRange(from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card) && card.IsMonsterDangerous() && card.IsFaceup()
			orderby card.Attack descending
			select card);
		targetList.AddRange(from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card) && card.IsMonsterInvincible() && card.IsFaceup()
			orderby card.Attack descending
			select card);
		targetList.AddRange(from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card) && card.GetDefensePower() >= base.Util.GetBestAttack(base.Bot) && card.IsAttack()
			orderby card.Attack descending
			select card);
		if (base.Duel.Phase >= DuelPhase.Main2)
		{
			targetList.AddRange(from card in base.Enemy.GetMonsters()
				where !currentDestroyCardList.Contains(card) && card.HasType((CardType)109060160)
				orderby card.Attack descending
				select card);
		}
		if (targetList.Count() > 0)
		{
			targetList.AddRange(from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card)
				orderby card.Attack descending
				select card);
			targetList.AddRange(ShuffleList((from card in base.Enemy.GetMonsters()
				where card.IsFacedown() && !targetList.Contains(card)
				select card).ToList()));
			targetList.AddRange(ShuffleList((from card in base.Bot.GetMonsters()
				where card.IsFacedown() && !targetList.Contains(card)
				select card).ToList()));
			targetList.AddRange(from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card)
				orderby card.Attack
				select card);
			base.AI.SelectCard(base.Card.Overlays);
			Logger.DebugWriteLine("TYPHON first target: " + targetList[0]?.Name);
			base.AI.SelectNextCard(targetList);
			return true;
		}
		return false;
	}

	public bool UnchainedAbominationSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			return false;
		}
		if (base.Enemy.GetMonsterCount() > 0 && base.Bot.HasInMonstersZone(93084621) && !activatedCardIdList.Contains(93084621))
		{
			return false;
		}
		List<List<ClientCard>> list = new List<List<ClientCard>>();
		ClientCard anguish = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsCode(93084621));
		if (anguish != null)
		{
			List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => card == anguish);
			if (canBeUsedForLinkMaterial.Count() > 0)
			{
				list.Add(new List<ClientCard>
				{
					anguish,
					canBeUsedForLinkMaterial[0]
				});
			}
		}
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card.HasType(CardType.Link) && card.LinkCount == 2 && (!card.IsCode(71607202) || !summonThisTurn.Contains(card))
			orderby card.Attack
			select card).ToList();
		if (list2.Count() > 0)
		{
			ClientCard link2Material = null;
			ClientCard clientCard = list2.FirstOrDefault((ClientCard card) => card.Sequence >= 5 && card.IsCode(29301450));
			if (clientCard != null)
			{
				link2Material = clientCard;
			}
			else
			{
				link2Material = list2[0];
			}
			if (list2.Count() >= 2)
			{
				list.Add(new List<ClientCard>
				{
					link2Material,
					list2.FirstOrDefault((ClientCard card) => card != link2Material)
				});
			}
			List<ClientCard> canBeUsedForLinkMaterial2 = GetCanBeUsedForLinkMaterial(useAdvancedMonster: false, (ClientCard card) => card != link2Material && (!card.HasType(CardType.Link) || card.LinkMarker <= 2));
			if (canBeUsedForLinkMaterial2.Count() >= 2)
			{
				list.Add(new List<ClientCard>
				{
					link2Material,
					canBeUsedForLinkMaterial2[0],
					canBeUsedForLinkMaterial2[1]
				});
			}
		}
		foreach (List<ClientCard> item in list)
		{
			if ((CheckCanDirectAttack() && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(item) + 3000 >= base.Enemy.LifePoints) | (GetProblematicEnemyMonster() != null && GetProblematicEnemyMonster(3000) == null))
			{
				base.AI.SelectMaterials(item);
				return true;
			}
		}
		return false;
	}

	public bool UnchainedAbominationActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster);
		if (normalEnemyTargetList.Count() == 0)
		{
			return false;
		}
		int num = base.ActivateDescription;
		if (num >= base.Util.GetStringId(29479265, 0))
		{
			num = base.Util.GetStringId(29479265, 0) - 10;
		}
		Logger.DebugWriteLine("[UnchainedAbomination]desc: " + num + ", timing = " + base.CurrentTiming);
		if (base.ActivateDescription == base.Util.GetStringId(29479265, 0))
		{
			activatedCardIdList.Add(base.Card.Id);
		}
		if (base.ActivateDescription == base.Util.GetStringId(29479265, 1) || base.ActivateDescription == -1)
		{
			activatedCardIdList.Add(base.Card.Id + 1);
		}
		if (base.ActivateDescription == base.Util.GetStringId(29479265, 2))
		{
			activatedCardIdList.Add(base.Card.Id + 2);
		}
		base.AI.SelectCard(normalEnemyTargetList);
		return true;
	}

	public bool UnchainedSoulOfAnguishSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			return false;
		}
		ClientCard unchainedNonLink = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsFaceup() && card.HasSetcode(304) && !card.HasType(CardType.Link));
		ClientCard unchainedLink2 = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsFaceup() && card.HasSetcode(304) && card.HasType(CardType.Link) && card.LinkCount == 2);
		Logger.DebugWriteLine("[Anguish summon] unchainedNonLink = " + unchainedNonLink?.Name + ", unchainedLink2 = " + unchainedLink2?.Name);
		if (unchainedNonLink == null && unchainedLink2 == null)
		{
			return false;
		}
		int num = 2;
		if (unchainedLink2 != null)
		{
			num = 1;
		}
		if (num == 2 && base.Bot.HasInExtra(24269961))
		{
			return false;
		}
		bool flag = !base.Bot.HasInMonstersZone(93084621) && !activatedCardIdList.Contains(93084621) && base.Enemy.GetMonsters().Any((ClientCard card) => card.IsFaceup());
		if (flag)
		{
			flag = base.Bot.HasInExtra(67680512);
			flag |= base.Bot.HasInExtra(29479265);
			flag |= base.Bot.HasInExtra(29301450) && banSpSummonExceptFiendCount == 0;
		}
		Logger.DebugWriteLine("[Anguish summon] needAnguish = " + flag);
		if (num == 1)
		{
			List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(flag, (ClientCard card) => card == unchainedLink2);
			Logger.DebugWriteLine("[Anguish summon 1] material count = " + canBeUsedForLinkMaterial.Count());
			if (canBeUsedForLinkMaterial.Count() == 0)
			{
				return false;
			}
			List<ClientCard> list = new List<ClientCard>
			{
				unchainedLink2,
				canBeUsedForLinkMaterial[0]
			};
			bool flag2 = flag;
			flag2 |= CheckCanDirectAttack() && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list) + 2400 >= base.Enemy.LifePoints;
			Logger.DebugWriteLine("[Anguish summon 1] summon flag " + flag2);
			if (flag2)
			{
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		if (num == 2)
		{
			List<ClientCard> canBeUsedForLinkMaterial2 = GetCanBeUsedForLinkMaterial(flag, (ClientCard card) => card == unchainedNonLink);
			Logger.DebugWriteLine("[Anguish summon 2] material count = " + canBeUsedForLinkMaterial2.Count());
			if (canBeUsedForLinkMaterial2.Count() >= 2)
			{
				List<ClientCard> list2 = new List<ClientCard>
				{
					unchainedNonLink,
					canBeUsedForLinkMaterial2[0],
					canBeUsedForLinkMaterial2[1]
				};
				if (flag || GetMaterialAttack(list2) < 2400)
				{
					base.AI.SelectMaterials(list2);
					return true;
				}
			}
		}
		return false;
	}

	public bool UnchainedSoulOfAnguishActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			List<ClientCard> list = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !card.IsShouldNotBeTarget() && !card.IsShouldNotBeMonsterTarget()
				orderby card.Attack descending
				select card).ToList();
			if (list.Count() > 0)
			{
				currentDestroyCardList.Add(list[0]);
				int num = 0;
				if (base.Bot.HasInExtra(29479265) && GetProblematicEnemyMonster(3000, canBeTarget: false, ignoreCurrentDestroy: true) == null)
				{
					num = 29479265;
				}
				else if (banSpSummonExceptFiendCount == 0 && base.Bot.HasInExtra(29301450) && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
				{
					num = 29301450;
				}
				else if (base.Bot.HasInExtra(67680512))
				{
					num = 67680512;
				}
				if (num > 0)
				{
					List<ClientCard> cards = new List<ClientCard>(list) { base.Card };
					Logger.DebugWriteLine("*** Anguish select: " + num);
					base.AI.SelectCard(list);
					base.AI.SelectNextCard(num);
					base.AI.SelectMaterials(cards);
					activatedCardIdList.Add(base.Card.Id);
				}
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			return UnchainRecycleActivate();
		}
		return false;
	}

	public bool UnchainedSoulLordOfYamaSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false))
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(24269961) || activatedCardIdList.Contains(24269961))
		{
			return false;
		}
		bool flag = base.Bot.HasInExtra(93084621) && !base.Bot.HasInMonstersZone(93084621) && !activatedCardIdList.Contains(93084621) && GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster) != null;
		flag |= CheckAtAdvantage() && base.Duel.Phase == DuelPhase.Main2 && base.Bot.HasInExtra(67680512) && !base.Bot.HasInMonstersZone(67680512);
		bool flag2 = base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(304));
		if (flag)
		{
			flag = base.Bot.HasInExtra(67680512);
			flag |= base.Bot.HasInExtra(29479265);
			flag |= base.Bot.HasInExtra(29301450) && banSpSummonExceptFiendCount == 0;
		}
		List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(flag, (ClientCard card) => !card.HasRace(CardRace.Fiend) || (card.HasType(CardType.Link) && card.HasSetcode(304)));
		Logger.DebugWriteLine("[Yama Summon]need3Monster = " + flag + ", material count = " + canBeUsedForLinkMaterial.Count());
		for (int num = 0; num < canBeUsedForLinkMaterial.Count() - 1; num++)
		{
			ClientCard item = canBeUsedForLinkMaterial[num];
			for (int num2 = num + 1; num2 < canBeUsedForLinkMaterial.Count(); num2++)
			{
				ClientCard item2 = canBeUsedForLinkMaterial[num2];
				List<ClientCard> list = new List<ClientCard> { item, item2 };
				if (flag && canBeUsedForLinkMaterial.Count() == 2 && (activatedCardIdList.Contains(41165831) || base.Bot.GetSpells().Count() == 0) && (GetProblematicEnemyMonster() != null || !CheckCanDirectAttack() || GetMaterialAttack(list) >= 2000))
				{
					return false;
				}
				if (flag | (base.Enemy.GetMonsterCount() == 0 && GetMaterialAttack(list) < 2000) | (CheckAtAdvantage() && !flag2))
				{
					base.AI.SelectMaterials(list);
					return true;
				}
			}
		}
		return false;
	}

	public bool UnchainedSoulLordOfYamaActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone && (base.ActivateDescription == base.Util.GetStringId(24269961, 0) || base.ActivateDescription == -1))
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			base.AI.SelectCard(41165831, 29479265, 93084621, 67680512);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			ClientCard clientCard = null;
			ClientCard clientCard2 = null;
			ClientCard clientCard3 = null;
			ClientCard clientCard4 = null;
			ClientCard clientCard5 = null;
			ClientCard clientCard6 = null;
			ClientCard clientCard7 = null;
			foreach (ClientCard item in base.Bot.Graveyard)
			{
				if (item.IsCode(22850702) && item.ProcCompleted != 0 && !dimensionalBarrierAnnouced.Contains(1063))
				{
					clientCard = item;
				}
				if (item.IsCode(67680512) && item.ProcCompleted != 0)
				{
					clientCard7 = item;
				}
				if (item.IsCode(29479265) && item.ProcCompleted != 0)
				{
					clientCard2 = item;
				}
				if (item.IsCode(81497285))
				{
					clientCard3 = item;
				}
				if (item.IsCode(2347656))
				{
					clientCard4 = item;
				}
				if (item.IsCode(1225009))
				{
					clientCard5 = item;
				}
				if (base.Card != item && item.IsMonster() && item.HasRace(CardRace.Fiend) && item.IsCanRevive() && (clientCard6 == null || item.Attack > clientCard6.Attack))
				{
					clientCard6 = item;
				}
			}
			ClientCard clientCard8 = null;
			bool flag = false;
			if (clientCard != null && (GetProblematicEnemyCardList(canBeTarget: false, ignoreSpells: false, CardType.Monster).Count() > 0 || (base.Bot.GetMonsterCount() == 0 && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
			{
				clientCard8 = clientCard;
			}
			if (clientCard8 == null && clientCard2 != null && (GetProblematicEnemyCardList(canBeTarget: false, ignoreSpells: false, CardType.Monster).Count() > 0 || (base.Bot.GetMonsterCount() == 0 && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
			{
				clientCard8 = clientCard2;
				Logger.DebugWriteLine("[Yama] timing: " + base.CurrentTiming);
				if (base.Bot.HasInSpellZone(5380979) && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase >= DuelPhase.Main2) && !activatedCardIdList.Contains(29479265))
				{
					flag = true;
				}
			}
			if (clientCard8 == null && clientCard7 != null && (base.Duel.Player == 0 || (!activatedCardIdList.Contains(67680512) && (base.Duel.Phase == DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2))) && base.Bot.HasInExtra(new List<int> { 93084621, 29301450 }))
			{
				clientCard8 = clientCard7;
			}
			if (clientCard8 == null && clientCard5 != null && base.Duel.Player == 0 && !activatedCardIdList.Contains(1225009))
			{
				clientCard8 = clientCard5;
			}
			if (clientCard8 == null && clientCard4 != null && base.Duel.Player == 1 && base.Util.GetBestAttack(base.Enemy) < 2900)
			{
				clientCard8 = clientCard4;
			}
			if (clientCard8 == null && clientCard3 != null && base.Duel.Player == 1 && base.Util.GetBestAttack(base.Enemy) < 3000)
			{
				clientCard8 = clientCard3;
			}
			if (clientCard8 == null && clientCard5 != null && !activatedCardIdList.Contains(1225009))
			{
				clientCard8 = clientCard5;
			}
			if (clientCard8 == null && clientCard6 != null)
			{
				clientCard8 = clientCard6;
			}
			if (clientCard8 != null)
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				base.AI.SelectCard(clientCard8);
				if (flag)
				{
					base.AI.SelectYesNo(opt: true);
					base.AI.SelectNextCard(5380979);
				}
				else
				{
					base.AI.SelectYesNo(opt: false);
				}
				return true;
			}
		}
		return false;
	}

	public bool UnchainedSoulOfRageSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(isLabrynth: false) || CheckWhetherNegated(disablecheck: true, toFieldCheck: true, (CardType)67108865))
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(67680512))
		{
			return false;
		}
		ClientCard unchained = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsFaceup() && card.HasSetcode(304) && !card.IsCode(93084621, 29479265));
		if (unchained == null)
		{
			return false;
		}
		bool flag = CheckAtAdvantage() && base.Util.IsTurn1OrMain2();
		flag |= (!base.Bot.HasInExtra(93084621) || activatedCardIdList.Contains(93084621)) && base.Util.IsTurn1OrMain2();
		if (flag)
		{
			flag = base.Bot.HasInExtra(93084621);
			flag |= base.Bot.HasInExtra(29301450) && banSpSummonExceptFiendCount == 0;
		}
		List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(base.Util.IsTurn1OrMain2(), (ClientCard card) => !card.HasRace(CardRace.Fiend) || card == unchained);
		if (canBeUsedForLinkMaterial.Count() > 0)
		{
			List<ClientCard> list = new List<ClientCard>
			{
				unchained,
				canBeUsedForLinkMaterial[0]
			};
			if (flag | (base.Enemy.GetMonsterCount() == 0 && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list) + 1800 >= base.Enemy.LifePoints))
			{
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		return false;
	}

	public bool UnchainedSoulOfRageActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			bool num = DefaultOnBecomeTarget() && !base.Util.ChainContainsCard(53417695);
			ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(-1, canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster);
			List<ClientCard> list = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !card.IsShouldNotBeTarget() && !card.IsShouldNotBeTarget()
				orderby card.Attack
				select card).ToList();
			if (problematicEnemyMonster != null)
			{
				list.Insert(0, problematicEnemyMonster);
			}
			if (((num | ((base.CurrentTiming & 4) > 0 && base.Util.IsOneEnemyBetterThanValue(base.Card.Attack, onlyATK: true))) || problematicEnemyMonster != null) && list.Count() > 0)
			{
				ClientCard clientCard = list[0];
				int cardId = 0;
				if (base.Bot.HasInExtra(29479265) && GetProblematicEnemyMonster(3000) == null && clientCard.HasType(CardType.Link) && clientCard.LinkCount == 2)
				{
					cardId = 93084621;
				}
				else if (banSpSummonExceptFiendCount == 0 && base.Bot.HasInExtra(29301450))
				{
					cardId = 29301450;
				}
				else if (base.Bot.HasInExtra(93084621) && GetProblematicEnemyMonster(2400) == null)
				{
					cardId = 93084621;
				}
				List<ClientCard> cards = new List<ClientCard>(list) { base.Card };
				base.AI.SelectCard(list);
				base.AI.SelectNextCard(cardId);
				base.AI.SelectMaterials(cards);
				activatedCardIdList.Add(base.Card.Id);
				escapeTargetList.Add(base.Card);
				currentDestroyCardList.Add(clientCard);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			return UnchainRecycleActivate();
		}
		return false;
	}

	public bool UnchainRecycleActivate()
	{
		base.AI.SelectCard(41165831, 2347656, 1225009, 29479265, 74018812, 37629703, 2511, 73602965, 75730490);
		activatedCardIdList.Add(base.Card.Id + 1);
		return true;
	}

	public bool SPLittleKnightSpSummon()
	{
		if (CheckCanDirectAttack())
		{
			List<ClientCard> list = SPLittleKnightSelectMaterial();
			if (list.Count() >= 2 && GetMaterialAttack(list) < 1600)
			{
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		else if (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true, (CardType)67108865) && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
		{
			List<ClientCard> list2 = SPLittleKnightSelectMaterial(needToUseEffect: true);
			if (list2.Count() >= 2 && list2.Any((ClientCard card) => card.HasType((CardType)75505728)))
			{
				base.AI.SelectMaterials(list2);
				return true;
			}
		}
		return false;
	}

	public List<ClientCard> SPLittleKnightSelectMaterial(bool needToUseEffect = false)
	{
		List<ClientCard> list = new List<ClientCard>();
		if (base.Bot.GetMonstersExtraZoneCount() > 0)
		{
			ClientCard botMonsterExtraZome = base.Bot.GetMonstersInExtraZone()[0];
			if (botMonsterExtraZome.HasType((CardType)25174080) || botMonsterExtraZome.IsCode(94259633))
			{
				list.Add(botMonsterExtraZome);
				if (botMonsterExtraZome.HasType((CardType)75505728))
				{
					needToUseEffect = false;
				}
			}
			List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => card == botMonsterExtraZome);
			if (canBeUsedForLinkMaterial.Count() > 0)
			{
				foreach (ClientCard item in canBeUsedForLinkMaterial)
				{
					if (!needToUseEffect || item.HasType((CardType)8396864) || (item.HasType(CardType.Link) && item.LinkCount <= 2))
					{
						list.Add(item);
						if (item.HasType((CardType)75505728))
						{
							needToUseEffect = false;
						}
					}
					if (list.Count() >= 2)
					{
						break;
					}
				}
			}
			if (list.Count() < 2)
			{
				list.Clear();
			}
		}
		else
		{
			List<ClientCard> canBeUsedForLinkMaterial2 = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => !needToUseEffect || card.HasType((CardType)8396864) || (card.HasType(CardType.Link) && card.LinkCount <= 2));
			if (canBeUsedForLinkMaterial2.Count() >= 2)
			{
				for (int num = 0; num < canBeUsedForLinkMaterial2.Count() - 1; num++)
				{
					ClientCard clientCard = canBeUsedForLinkMaterial2[num];
					if (clientCard.HasType(CardType.Link) && clientCard.LinkCount >= 3)
					{
						continue;
					}
					bool flag = !needToUseEffect || clientCard.HasType((CardType)75505728);
					for (int num2 = 0; num2 < canBeUsedForLinkMaterial2.Count(); num2++)
					{
						ClientCard clientCard2 = canBeUsedForLinkMaterial2[num2];
						if (!clientCard2.HasType(CardType.Link) || clientCard2.LinkCount < 3)
						{
							bool flag2 = !needToUseEffect || clientCard2.HasType((CardType)75505728);
							if (flag || flag2)
							{
								return new List<ClientCard> { clientCard, clientCard2 };
							}
						}
					}
				}
			}
		}
		return list;
	}

	public bool SPLittleKnightActivate()
	{
		if (base.ActivateDescription == -1 || base.ActivateDescription == base.Util.GetStringId(29301450, 0))
		{
			List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster);
			problematicEnemyCardList.AddRange(GetDangerousCardinEnemyGrave());
			problematicEnemyCardList.AddRange(GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster));
			problematicEnemyCardList.AddRange(from card in base.Enemy.Graveyard
				where card.HasType(CardType.Monster)
				orderby card.Attack descending
				select card);
			problematicEnemyCardList.AddRange(base.Enemy.Graveyard.Where((ClientCard card) => !card.HasType(CardType.Monster)));
			if (problematicEnemyCardList.Count() > 0)
			{
				base.AI.SelectCard(problematicEnemyCardList);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		else if (base.ActivateDescription == base.Util.GetStringId(29301450, 1))
		{
			ClientCard selfMonster = null;
			foreach (ClientCard monster in base.Bot.GetMonsters())
			{
				if (base.Duel.ChainTargets.Contains(monster) && !escapeTargetList.Contains(monster))
				{
					selfMonster = monster;
					break;
				}
			}
			if (selfMonster == null && base.Duel.Player == 1)
			{
				selfMonster = (from card in base.Bot.GetMonsters()
					where card.IsAttack()
					orderby card.Attack
					select card).FirstOrDefault();
				if (!base.Util.IsOneEnemyBetterThanValue(selfMonster.Attack, onlyATK: true))
				{
					selfMonster = null;
				}
			}
			if (selfMonster != null)
			{
				ClientCard clientCard = null;
				List<ClientCard> list = (from card in base.Bot.GetMonsters()
					where card != selfMonster
					select card).ToList();
				if (base.Enemy.GetMonsterCount() == 0 && list.Count() > 0)
				{
					list.Sort(CompareUsableAttack);
					clientCard = list[0];
					escapeTargetList.Add(clientCard);
				}
				if (base.Enemy.GetMonsterCount() > 0)
				{
					clientCard = GetBestEnemyMonster(onlyFaceup: false, canBeTarget: true, ignoreCurrentDestroy: true);
					currentDestroyCardList.Add(clientCard);
				}
				if (clientCard != null)
				{
					base.AI.SelectCard(selfMonster);
					base.AI.SelectNextCard(clientCard);
					escapeTargetList.Add(selfMonster);
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
		}
		return false;
	}

	public bool MuckrakerFromTheUnderworldSpSummon()
	{
		List<ClientCard> canBeUsedForLinkMaterial = GetCanBeUsedForLinkMaterial(useAdvancedMonster: true, (ClientCard card) => card.HasType(CardType.Link));
		if (canBeUsedForLinkMaterial.Count() < 2)
		{
			return false;
		}
		bool flag = CheckWhetherNegated(disablecheck: true, toFieldCheck: true, (CardType)67108865) && base.Bot.Hand.Count() > 0;
		bool flag2 = base.Bot.Graveyard.Any((ClientCard card) => card.IsCanRevive() && card.IsCode(22850702)) && !flag;
		bool flag3 = base.Bot.Graveyard.Any((ClientCard card) => card.IsCode(2347656)) && !flag;
		int num = 0;
		bool flag4 = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		foreach (ClientCard item3 in base.Bot.Graveyard)
		{
			if (item3.IsMonster() && item3.HasRace(CardRace.Fiend) && item3.IsCanRevive() && item3.Attack > num)
			{
				num = item3.Attack;
			}
		}
		for (int num2 = 0; num2 < canBeUsedForLinkMaterial.Count() - 1; num2++)
		{
			ClientCard item = canBeUsedForLinkMaterial[num2];
			for (int num3 = num2 + 1; num3 < canBeUsedForLinkMaterial.Count(); num3++)
			{
				ClientCard item2 = canBeUsedForLinkMaterial[num2];
				List<ClientCard> list = new List<ClientCard> { item, item2 };
				bool flag5 = flag4 && (flag2 || (list.Any((ClientCard card) => card.IsCode(22850702)) && !flag));
				flag5 |= base.Enemy.GetMonsterCount() == 0 && flag3;
				flag5 |= !activatedCardIdList.Contains(2347656) && base.Bot.Graveyard.Any((ClientCard card) => card.Type == 4) && list.Any((ClientCard card) => card.IsDisabled() && card.IsCode(2347656));
				if (CheckCanDirectAttack())
				{
					flag5 |= GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack(list) + num >= base.Enemy.LifePoints;
					flag5 |= GetMaterialAttack(list) < 1000;
				}
				if (flag5)
				{
					base.AI.SelectMaterials(list);
					return true;
				}
			}
		}
		return false;
	}

	public bool MuckrakerFromTheUnderworldActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(71607202, 0))
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			ClientCard clientCard = null;
			ClientCard clientCard2 = null;
			ClientCard clientCard3 = null;
			ClientCard clientCard4 = null;
			foreach (ClientCard item in base.Bot.Graveyard)
			{
				if (item.IsCode(22850702) && item.ProcCompleted != 0 && !dimensionalBarrierAnnouced.Contains(1063))
				{
					clientCard = item;
				}
				if (item.IsCode(2347656))
				{
					clientCard2 = item;
				}
				if (item.IsCode(1225009))
				{
					clientCard3 = item;
				}
				if (base.Card != item && item.IsMonster() && item.HasRace(CardRace.Fiend) && item.IsCanRevive() && (clientCard4 == null || item.Attack > clientCard4.Attack))
				{
					clientCard4 = item;
				}
			}
			ClientCard clientCard5 = null;
			if (clientCard != null && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
			{
				clientCard5 = clientCard;
			}
			if (clientCard5 == null && clientCard2 != null && base.Util.GetBestAttack(base.Enemy) < 2900 && (!activatedCardIdList.Contains(2347656) || base.Bot.HasInSpellZoneOrInGraveyard(92714517)))
			{
				clientCard5 = clientCard2;
			}
			if (clientCard5 == null && clientCard4 != null && CheckCanDirectAttack() && GetBotCurrentTotalAttack() < base.Enemy.LifePoints && GetBotCurrentTotalAttack() + clientCard4.Attack >= base.Enemy.LifePoints)
			{
				clientCard5 = clientCard4;
			}
			if (clientCard5 == null && clientCard3 != null && base.Duel.Player == 0 && !activatedCardIdList.Contains(1225009))
			{
				clientCard5 = clientCard3;
			}
			if (clientCard5 == null && clientCard4 != null)
			{
				clientCard5 = clientCard4;
			}
			if (clientCard5 != null)
			{
				base.AI.SelectCard(clientCard5);
				base.AI.SelectNextCard(FurnitureGetCost());
				activatedCardIdList.Contains(base.Card.Id);
				banSpSummonExceptFiendCount = Math.Max(1, banSpSummonExceptFiendCount);
				return true;
			}
		}
		return false;
	}

	public bool RelinquishedAnimaSpSummon()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard clientCard = base.Enemy.MonsterZone[6];
		if (clientCard != null && clientCard.HasLinkMarker(128) && !clientCard.IsShouldNotBeTarget() && !clientCard.IsShouldNotBeMonsterTarget())
		{
			ClientCard clientCard2 = base.Bot.MonsterZone[1];
			if (clientCard2 == null)
			{
				base.AI.SelectMaterials(2511);
				base.AI.SelectPlace(2);
				return true;
			}
			if (!clientCard2.HasType((CardType)75513856) && clientCard2.Level == 1)
			{
				base.AI.SelectMaterials(clientCard2);
				base.AI.SelectPlace(2);
				return true;
			}
		}
		ClientCard clientCard3 = base.Enemy.MonsterZone[5];
		if (clientCard3 != null && clientCard3.HasLinkMarker(128) && !clientCard3.IsShouldNotBeTarget() && !clientCard3.IsShouldNotBeMonsterTarget())
		{
			ClientCard clientCard4 = base.Bot.MonsterZone[3];
			if (clientCard4 == null)
			{
				base.AI.SelectMaterials(2511);
				base.AI.SelectPlace(8);
				return true;
			}
			if (!clientCard4.HasType((CardType)75513856) && clientCard4.Level == 1)
			{
				base.AI.SelectMaterials(clientCard4);
				base.AI.SelectPlace(8);
				return true;
			}
		}
		if (base.Bot.MonsterZone[5] != null || base.Bot.MonsterZone[6] != null)
		{
			return false;
		}
		ClientCard clientCard5 = base.Enemy.MonsterZone[3];
		ClientCard clientCard6 = base.Enemy.MonsterZone[1];
		if (base.Enemy.MonsterZone[6] != null)
		{
			clientCard5 = null;
		}
		if (clientCard5 != null && clientCard5.IsFacedown())
		{
			clientCard5 = null;
		}
		if (clientCard5 != null && (clientCard5.IsShouldNotBeMonsterTarget() || clientCard5.IsShouldNotBeTarget()))
		{
			clientCard5 = null;
		}
		if (base.Enemy.MonsterZone[5] != null)
		{
			clientCard6 = null;
		}
		if (clientCard6 != null && (clientCard6.IsShouldNotBeMonsterTarget() || clientCard6.IsShouldNotBeTarget()))
		{
			clientCard6 = null;
		}
		if (clientCard6 != null && clientCard6.IsFacedown())
		{
			clientCard6 = null;
		}
		int num = -1;
		if (clientCard5 != null && clientCard6 == null)
		{
			num = 32;
		}
		if (clientCard5 == null && clientCard6 != null)
		{
			num = 64;
		}
		if (clientCard5 != null && clientCard6 != null)
		{
			num = ((clientCard5.IsFloodgate() && !clientCard6.IsFloodgate()) ? 32 : ((!clientCard5.IsFloodgate() && clientCard6.IsFloodgate()) ? 64 : ((clientCard5.GetDefensePower() < clientCard6.GetDefensePower()) ? 64 : 32)));
		}
		if (num >= 0)
		{
			base.AI.SelectMaterials((from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType((CardType)75513856) && card.Level == 1
				orderby card.Attack
				select card).ToList());
			base.AI.SelectPlace(num);
			return true;
		}
		if (base.Bot.HasInExtra(29301450) && base.Bot.GetMonsters().Count((ClientCard card) => card.IsFaceup()) >= 2 && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasType((CardType)75505728)) && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
		{
			base.AI.SelectMaterials((from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType((CardType)75513856) && card.Level == 1
				orderby card.Attack
				select card).ToList());
			return true;
		}
		return false;
	}

	public bool RelinquishedAnimaActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		foreach (KeyValuePair<int, int> item in new Dictionary<int, int>
		{
			{ 1, 6 },
			{ 3, 5 },
			{ 5, 3 },
			{ 6, 1 }
		})
		{
			if (base.Bot.MonsterZone[item.Key] == base.Card && base.Enemy.MonsterZone[item.Value] != null)
			{
				currentDestroyCardList.Add(base.Enemy.MonsterZone[item.Value]);
				break;
			}
		}
		return true;
	}

	public bool MonsterRepos()
	{
		if (base.Card.Attack + 1 <= 1)
		{
			return !base.Card.IsDefense();
		}
		int num = 0;
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			int attack = monster.Attack;
			if (attack >= num)
			{
				num = attack;
			}
		}
		bool flag = base.Util.IsAllEnemyBetterThanValue(num, onlyATK: true);
		if (base.Card.IsAttack() && flag)
		{
			return true;
		}
		if (base.Card.IsDefense() && !flag)
		{
			return true;
		}
		return false;
	}

	public bool ReposForLabrynth()
	{
		if (!activatedCardIdList.Contains(92714517) && base.Bot.HasInSpellZoneOrInGraveyard(92714517))
		{
			return base.Card.IsFacedown();
		}
		return false;
	}

	public bool SpellSetCheck()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (base.Card.IsCode(92714517) && base.Bot.HasInSpellZone(base.Card.Id))
		{
			return false;
		}
		if (base.Card.IsCode(6351147) && !base.Bot.HasInSpellZone(6351147))
		{
			bool flag = false;
			if (base.Enemy.Graveyard.Any((ClientCard card) => card.IsCode(5380979, 92714517, 10045474, 83326048, 30748475, 94192409, 78474168)))
			{
				flag = true;
			}
			if (!flag && !base.Bot.HasInHand(41165831))
			{
				return false;
			}
		}
		if (base.Card.IsCode(53417695) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(304)))
		{
			return false;
		}
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay))
		{
			List<int> list = new List<int>();
			int num = 0;
			for (int num2 = 0; num2 < 5; num2++)
			{
				if (base.Enemy.SpellZone[num2] != null && base.Enemy.SpellZone[num2].IsFaceup() && base.Bot.SpellZone[4 - num2] == null)
				{
					list.Add(4 - num2);
					num += (int)Math.Pow(2.0, 4 - num2);
				}
			}
			if (base.Bot.HasInHand(10045474))
			{
				if (base.Card.IsCode(10045474))
				{
					base.AI.SelectPlace(num);
					return true;
				}
				SelectSTPlace(base.Card, avoidImpermanence: false, list);
				return true;
			}
			SelectSTPlace();
			return true;
		}
		if ((base.Enemy.HasInSpellZone(58921041, notDisabled: true) || base.Bot.HasInSpellZone(58921041, notDisabled: true)) && base.Card.IsSpell() && !base.Bot.HasInSpellZone(base.Card.Id))
		{
			SelectSTPlace();
			return true;
		}
		return false;
	}

	public bool SpellSetForCooClockCheck()
	{
		if (base.Card.IsCode(49238328, 6351147, 5380979) && base.Bot.HasInHand(41165831) && !activatedCardIdList.Contains(41165831))
		{
			SelectSTPlace(base.Card);
			return true;
		}
		bool flag = base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(382));
		if (!cooclockAffected || (flag && base.Bot.HasInHand(2511) && !activatedCardIdList.Contains(2511)))
		{
			return false;
		}
		if (!base.Card.IsCode(92714517, 5380979, 10045474, 83326048))
		{
			return false;
		}
		if (flag)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		if (!base.Card.IsCode(92714517, 5380979))
		{
			return false;
		}
		if (!summoned && base.Bot.Hand.Any((ClientCard card) => card.IsMonster() && card.Level <= 4 && card.HasSetcode(382)))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}
}
