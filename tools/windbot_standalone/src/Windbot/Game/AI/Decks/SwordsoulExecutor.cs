using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Swordsoul", "AI_Swordsoul", "Normal")]
public class SwordsoulExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int NibiruThePrimalBeing = 27204311;

		public const int TenyiSpirit_Ashuna = 87052196;

		public const int TenyiSpirit_Vishuda = 23431858;

		public const int SwordsoulStrategistLongyuan = 93490856;

		public const int SwordsoulOfTaia = 56495147;

		public const int SwordsoulOfMoYe = 20001443;

		public const int IncredibleEcclesiaTheVirtuous = 55273560;

		public const int TenyiSpirit_Adhara = 98159737;

		public const int SwordsoulEmergence = 56465981;

		public const int SwordsoulSacredSummit = 93850690;

		public const int CrossoutDesignator = 65681983;

		public const int SwordsoulBlackout = 14821890;

		public const int GeomathmechFinalSigma = 42632209;

		public const int PsychicEndPunisher = 60465049;

		public const int SwordsoulSupremeSovereign_Chengying = 96633955;

		public const int BaronneDeFleur = 84815190;

		public const int SwordsoulSinisterSovereign_QixingLongyuan = 47710198;

		public const int AdamancipatorRisen_Dragite = 9464441;

		public const int DracoBerserkerOfTheTenyi = 5041348;

		public const int SwordsoulGrandmaster_Chixiao = 69248256;

		public const int BaxiaBrightnessOfTheYangZing = 83755611;

		public const int YaziEvilOfTheYangZing = 43202238;

		public const int ShamanOfTheTenyi = 78917791;

		public const int MonkOfTheTenyi = 32519092;

		public const int SwordsoulToken = 20001444;

		public const int NaturalExterio = 99916754;

		public const int NaturalBeast = 33198837;

		public const int ImperialOrder = 61740673;

		public const int SwordsmanLV7 = 37267041;

		public const int RoyalDecree = 51452091;

		public const int Number41BagooskatheTerriblyTiredTapir = 90590303;

		public const int InspectorBoarder = 15397015;
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodeYangZing = 158;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeTenyi = 300;

	private const int SetcodeSwordsoul = 363;

	private const int SetcodeFloowandereeze = 365;

	private List<int> normalCounterList = new List<int> { 14558127, 84815190, 27548199, 4280258, 53262004 };

	private List<int> notToNegateIdList = new List<int> { 58699500 };

	private const int hintTimingMainEnd = 4;

	private const int hintReplaceDestroy = 96;

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 93490856, 56495147, 20001443, 55273560, 14558127, 23434538, 97268402, 56465981, 10045474 }
		},
		{
			2,
			new List<int> { 87052196, 35261759, 24224830, 14821890 }
		},
		{
			1,
			new List<int> { 27204311, 23431858, 98159737, 93850690, 65681983 }
		}
	};

	private List<int> currentNegatingIdList = new List<int>();

	private bool enemyActivateMaxxC;

	private bool enemyActivateLockBird;

	private bool enemyActivateInfiniteImpermanenceFromHand;

	private List<int> infiniteImpermanenceList = new List<int>();

	private bool summoned;

	private bool onlyWyrmSpSummon;

	private List<int> activatedCardIdList = new List<int>();

	private List<int> canSpSummonLevel10IdList = new List<int>();

	private List<ClientCard> effectUsedBaronneDeFleurList = new List<ClientCard>();

	private List<ClientCard> currentNegateMonsterList = new List<ClientCard>();

	public SwordsoulExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 83755611, BaxiaBrightnessOfTheYangZingActivate);
		AddExecutor(ExecutorType.Activate, 43202238, YaziEvilOfTheYangZingActivate);
		AddExecutor(ExecutorType.Activate, 55273560, IncredibleEcclesiaTheVirtuousActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 69248256, SwordsoulGrandmaster_ChixiaoActivate);
		AddExecutor(ExecutorType.Activate, 97268402, EffectVeilerActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 47710198, SwordsoulSinisterSovereign_QixingLongyuanActivate);
		AddExecutor(ExecutorType.Activate, 5041348, DracoBerserkerOfTheTenyiActivate);
		AddExecutor(ExecutorType.Activate, 9464441, AdamancipatorRisen_DragiteActivate);
		AddExecutor(ExecutorType.Activate, 84815190, BaronneDeFleurActivate);
		AddExecutor(ExecutorType.Activate, 96633955, SwordsoulSupremeSovereign_ChengyingActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 27204311, NibiruThePrimalBeingActivate);
		AddExecutor(ExecutorType.Activate, 56465981, SwordsoulEmergenceActivate);
		AddExecutor(ExecutorType.Activate, 14821890, SwordsoulBlackoutActivate);
		AddExecutor(ExecutorType.SpSummon, 43202238, YaziEvilOfTheYangZingSpSummon);
		AddExecutor(ExecutorType.SpSummon, 83755611, BaxiaBrightnessOfTheYangZingSpSummon);
		AddExecutor(ExecutorType.SpSummon, 69248256, SwordsoulGrandmaster_ChixiaoSpSummon);
		AddExecutor(ExecutorType.SpSummon, 9464441, AdamancipatorRisen_DragiteSpSummon);
		AddExecutor(ExecutorType.SpSummon, 5041348, DracoBerserkerOfTheTenyiSpSummon);
		AddExecutor(ExecutorType.SpSummon, Level10SpSummonCheckInit);
		AddExecutor(ExecutorType.SpSummon, Level10SpSummonCheckCount);
		AddExecutor(ExecutorType.SpSummon, Level10SpSummonCheckDecide);
		AddExecutor(ExecutorType.SpSummon, Level10SpSummonCheckFinal);
		AddExecutor(ExecutorType.Activate, 20001443, SwordsoulOfMoYeActivate);
		AddExecutor(ExecutorType.Activate, 56495147, SwordsoulOfTaiaActivate);
		AddExecutor(ExecutorType.Activate, TenyiForShamanSpSummon);
		AddExecutor(ExecutorType.SpSummon, 55273560, IncredibleEcclesiaTheVirtuousSpSummon);
		AddExecutor(ExecutorType.Summon, 20001443, SwordsoulOfMoYeSummon);
		AddExecutor(ExecutorType.Summon, 55273560, IncredibleEcclesiaTheVirtuousSummon);
		AddExecutor(ExecutorType.Summon, 56495147, SwordsoulOfTaiaSummon);
		AddExecutor(ExecutorType.Activate, 93490856, SwordsoulStrategistLongyuanActivate);
		AddExecutor(ExecutorType.Activate, 35261759, PotOfDesiresActivate);
		AddExecutor(ExecutorType.Activate, 78917791, ShamanOfTheTenyiActivate);
		AddExecutor(ExecutorType.Activate, 93850690, SwordsoulSacredSummitActivate);
		AddExecutor(ExecutorType.Activate, 23431858, TenyiSpirit_VishudaActivate);
		AddExecutor(ExecutorType.Activate, 87052196, TenyiSpirit_AshunaActivate);
		AddExecutor(ExecutorType.Activate, 98159737, TenyiSpirit_AdharaActivate);
		AddExecutor(ExecutorType.Activate, TenyiForBlackoutSpSummon);
		AddExecutor(ExecutorType.SpSummon, 42632209, GeomathmechFinalSigmaSpSummon);
		AddExecutor(ExecutorType.SpSummon, 60465049, PsychicEndPunisherSpSummon);
		AddExecutor(ExecutorType.Summon, TunerForSynchroSummon);
		AddExecutor(ExecutorType.Summon, WyrmForBlackoutSummon);
		AddExecutor(ExecutorType.SpSummon, 78917791, ShamanOfTheTenyiSpSummon);
		AddExecutor(ExecutorType.SpSummon, 32519092, MonkOfTheTenyiSpSummon);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.Activate, 60465049, PsychicEndPunisherActivate);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
	}

	public List<ClientCard> ShuffleCardList(List<ClientCard> list)
	{
		int count = list.Count;
		while (count-- > 1)
		{
			int num = Program.Rand.Next(list.Count);
			int index = (num + Program.Rand.Next(list.Count - 1)) % list.Count;
			ClientCard value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
		return list;
	}

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false)
	{
		List<ClientCard> list = (from c in base.Enemy.GetMonsters()
			where c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())
			select c).ToList();
		if (list.Count() > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			return list[0];
		}
		List<ClientCard> list2 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterDangerous() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list2.Count() > 0)
		{
			list2.Sort(CardContainer.CompareCardAttack);
			list2.Reverse();
			return list2[0];
		}
		List<ClientCard> list3 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterInvincible() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list3.Count() > 0)
		{
			list3.Sort(CardContainer.CompareCardAttack);
			list3.Reverse();
			return list3[0];
		}
		if (attack == 0)
		{
			attack = base.Util.GetBestAttack(base.Bot);
		}
		List<ClientCard> list4 = (from card in base.Enemy.MonsterZone.GetMonsters()
			where card.GetDefensePower() >= attack && card.IsAttack() && (!canBeTarget || !card.IsShouldNotBeTarget())
			select card).ToList();
		if (list4.Count() > 0)
		{
			list4.Sort(CardContainer.CompareCardAttack);
			list4.Reverse();
			return list4[0];
		}
		return null;
	}

	public List<ClientCard> GetProblematicEnemyCardList(bool canBeTarget = false, bool ignoreNormalSpell = false)
	{
		List<ClientCard> resultList = new List<ClientCard>();
		List<ClientCard> list = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list.Count() > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			resultList.AddRange(list);
		}
		List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list2.Count() > 0)
		{
			resultList.AddRange(ShuffleCardList(list2));
		}
		List<ClientCard> list3 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && c.IsMonsterDangerous() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list3.Count() > 0 && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			list3.Sort(CardContainer.CompareCardAttack);
			list3.Reverse();
			resultList.AddRange(list3);
		}
		List<ClientCard> list4 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && c.IsMonsterInvincible() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list4.Count() > 0)
		{
			list4.Sort(CardContainer.CompareCardAttack);
			list4.Reverse();
			resultList.AddRange(list4);
		}
		List<ClientCard> list5 = base.Enemy.GetMonsters().ToList();
		if (list5.Count() > 0)
		{
			list5.Sort(CardContainer.CompareCardAttack);
			list5.Reverse();
			foreach (ClientCard item in list5)
			{
				if ((item.HasType((CardType)8396992) || (item.HasType(CardType.Link) && item.LinkCount >= 2)) && (!canBeTarget || (!item.IsShouldNotBeTarget() && !item.IsShouldNotBeMonsterTarget())) && !resultList.Contains(item))
				{
					resultList.Add(item);
				}
			}
		}
		List<ClientCard> list6 = (from c in base.Enemy.GetSpells()
			where c.IsFaceup() && c.HasType((CardType)17694720)
			select c).ToList();
		if (list6.Count() > 0 && !ignoreNormalSpell)
		{
			resultList.AddRange(ShuffleCardList(list6));
		}
		return resultList;
	}

	public ClientCard GetBestEnemyMonster(bool onlyFaceup = false, bool canBeTarget = false)
	{
		ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget);
		if (problematicEnemyMonster != null)
		{
			return problematicEnemyMonster;
		}
		problematicEnemyMonster = base.Enemy.MonsterZone.GetHighestAttackMonster(canBeTarget);
		if (problematicEnemyMonster != null)
		{
			return problematicEnemyMonster;
		}
		List<ClientCard> monsters = base.Enemy.GetMonsters();
		if (monsters.Count() > 0 && !onlyFaceup)
		{
			return ShuffleCardList(monsters)[0];
		}
		return null;
	}

	public ClientCard GetBestEnemySpell(bool onlyFaceup = false, bool canBeTarget = false)
	{
		List<ClientCard> list = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list.Count() > 0)
		{
			return ShuffleCardList(list)[0];
		}
		List<ClientCard> list2 = (from card in base.Enemy.GetSpells()
			where !card.IsFaceup() || !card.IsCode(15693423)
			select card).ToList();
		List<ClientCard> list3 = list2.Where((ClientCard ecard) => ecard.IsFaceup() && ecard.HasType((CardType)17694720)).ToList();
		if (list3.Count() > 0)
		{
			return ShuffleCardList(list3)[0];
		}
		if (list2.Count() > 0 && !onlyFaceup)
		{
			return ShuffleCardList(list2)[0];
		}
		return null;
	}

	public ClientCard GetBestEnemyCard(bool onlyFaceup = false, bool canBeTarget = false, bool checkGrave = false)
	{
		ClientCard bestEnemyMonster = GetBestEnemyMonster(onlyFaceup, canBeTarget);
		if (bestEnemyMonster != null)
		{
			return bestEnemyMonster;
		}
		bestEnemyMonster = GetBestEnemySpell(onlyFaceup, canBeTarget);
		if (bestEnemyMonster != null)
		{
			return bestEnemyMonster;
		}
		if (checkGrave && base.Enemy.Graveyard.Count() > 0)
		{
			List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard c) => c.IsMonster()).ToList();
			if (list.Count() > 0)
			{
				list.Sort(CardContainer.CompareCardAttack);
				list.Reverse();
				return list[0];
			}
			return ShuffleCardList(base.Enemy.Graveyard.ToList())[0];
		}
		return null;
	}

	public List<ClientCard> GetNormalEnemyTargetList(bool canBeTarget = true)
	{
		List<ClientCard> targetList = GetProblematicEnemyCardList(canBeTarget);
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup() && !targetList.Contains(card)
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		targetList.AddRange(list);
		targetList.AddRange(ShuffleCardList(base.Enemy.GetSpells()));
		targetList.AddRange(ShuffleCardList((from card in base.Enemy.GetMonsters()
			where card.IsFacedown()
			select card).ToList()));
		return targetList;
	}

	public List<ClientCard> GetMonsterListForTargetNegate(bool canBeMonsterTarget = false, bool canBeTrapTarget = false)
	{
		List<ClientCard> list = new List<ClientCard>();
		if (CheckWhetherNegated())
		{
			return list;
		}
		ClientCard clientCard = base.Enemy.MonsterZone.FirstOrDefault((ClientCard card) => card?.Data != null && card.IsMonsterShouldBeDisabledBeforeItUseEffect() && card.IsFaceup() && !card.IsShouldNotBeTarget() && (!canBeMonsterTarget || !card.IsShouldNotBeMonsterTarget()) && (!canBeTrapTarget || !card.IsShouldNotBeSpellTrapTarget()) && !currentNegateMonsterList.Contains(card));
		if (clientCard != null)
		{
			list.Add(clientCard);
		}
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Location == CardLocation.MonsterZone && item.Controller == 1 && !item.IsDisabled() && (!canBeMonsterTarget || !item.IsShouldNotBeMonsterTarget()) && (!canBeTrapTarget || !item.IsShouldNotBeSpellTrapTarget()) && !item.IsShouldNotBeTarget() && !currentNegateMonsterList.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public void CheckDeactiveFlag()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && base.Duel.LastChainPlayer == 1 && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone)
		{
			currentNegateMonsterList.Add(lastChainCard);
		}
	}

	public int CheckCalledbytheGrave(int id)
	{
		if (currentNegatingIdList.Contains(id))
		{
			return 1;
		}
		if (DefaultCheckWhetherCardIdIsNegated(id))
		{
			return 1;
		}
		return 0;
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

	public bool CheckWhetherNegated()
	{
		if ((base.Card.IsSpell() || base.Card.IsTrap()) && CheckSpellWillBeNegate())
		{
			return true;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return true;
		}
		if (base.Card.IsMonster() && base.Card.Location == CardLocation.MonsterZone && base.Card.IsDefense() && (base.Enemy.MonsterZone.Any((ClientCard card) => CheckNumber41(card)) || base.Bot.MonsterZone.Any((ClientCard card) => CheckNumber41(card))))
		{
			return true;
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

	public bool CheckAtAdvantage()
	{
		if (GetProblematicEnemyMonster() == null && (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup()) || (base.Duel.Player == 0 && base.Duel.Turn == 1)))
		{
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
		if (DefaultCheckWhetherCardIsNegated(lastChainCard))
		{
			return false;
		}
		if (base.Duel.CurrentChain.Count >= 2)
		{
			ClientCard clientCard = base.Duel.CurrentChain[base.Duel.CurrentChain.Count - 2];
			ClientCard clientCard2 = base.Duel.CurrentChain[base.Duel.CurrentChain.Count - 1];
			if (clientCard != null && clientCard.Controller == 0 && clientCard2 != null && clientCard2.Controller == 1 && clientCard2.IsCode(normalCounterList) && ((clientCard.Location == CardLocation.Grave && (clientCard.IsCode(56495147) || clientCard.IsCode(20001443) || clientCard.IsCode(93490856))) | (clientCard.IsCode(60465049) && base.Bot.LifePoints < base.Enemy.LifePoints)))
			{
				return false;
			}
		}
		return true;
	}

	public List<ClientCard> CheckDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> result = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219))).ToList();
		new List<int> { 99937011, 63542003, 98159737, 87052196, 23431858, 9411399, 28954097, 30680659 };
		return result;
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (cardId == 42632209 && location == CardLocation.MonsterZone)
		{
			if ((0x20 & available) > 0)
			{
				return 32;
			}
			if ((0x40 & available) > 0)
			{
				return 64;
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (namedCard != null)
		{
			if (namedCard.Id == 60465049)
			{
				return CardPosition.FaceUpAttack;
			}
			if (base.Util.IsTurn1OrMain2())
			{
				bool flag = false;
				if (!namedCard.HasType(CardType.Synchro) || namedCard.Attack <= namedCard.Defense)
				{
					flag = true;
				}
				if (flag)
				{
					return CardPosition.FaceUpDefence;
				}
			}
			if (base.Duel.Player == 1)
			{
				if (!namedCard.HasType(CardType.Synchro) || namedCard.Defense >= namedCard.Attack || base.Util.IsOneEnemyBetterThanValue(namedCard.Attack, onlyATK: true))
				{
					return CardPosition.FaceUpDefence;
				}
			}
			else if (namedCard.HasType(CardType.Synchro))
			{
				return CardPosition.FaceUpAttack;
			}
			int value = Math.Max(base.Util.GetBestAttack(base.Bot), namedCard.Attack);
			if (base.Util.IsAllEnemyBetterThanValue(value, onlyATK: true))
			{
				return CardPosition.FaceUpDefence;
			}
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		if (base.Util.ChainContainPlayer(1) && hint == 503 && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
		{
			int num = base.Bot.GetMonsterCount() + base.Bot.GetSpellCount();
			int num2 = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount();
			if (num - num2 == min && min == max)
			{
				Logger.DebugWriteLine("=== Evenly Matched activated.");
				List<ClientCard> banishList = new List<ClientCard>();
				List<ClientCard> source = (from card in base.Bot.GetMonsters()
					where !card.HasType(CardType.Token)
					select card).ToList();
				List<ClientCard> collection = source.Where((ClientCard card) => card.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list = source.Where((ClientCard card) => !card.HasType(CardType.Synchro) && !banishList.Contains(card)).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list);
				List<ClientCard> spells = base.Bot.GetSpells();
				banishList.AddRange(ShuffleCardList(spells));
				List<ClientCard> list2 = source.Where((ClientCard card) => card.HasType(CardType.Synchro) && !banishList.Contains(card)).ToList();
				list2.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list2);
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override void OnNewTurn()
	{
		enemyActivateMaxxC = false;
		enemyActivateLockBird = false;
		infiniteImpermanenceList.Clear();
		summoned = false;
		onlyWyrmSpSummon = false;
		enemyActivateInfiniteImpermanenceFromHand = false;
		activatedCardIdList.Clear();
		currentNegateMonsterList.Clear();
		currentNegatingIdList.Clear();
		base.OnNewTurn();
	}

	public override void OnChainSolved(int chainIndex)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo == null || base.Duel.IsCurrentSolvingChainNegated() || currentSolvingChainInfo.ActivatePlayer != 1)
		{
			return;
		}
		if (currentSolvingChainInfo.IsCode(23434538))
		{
			enemyActivateMaxxC = true;
		}
		if (currentSolvingChainInfo.IsCode(94145021))
		{
			enemyActivateLockBird = true;
		}
		if (!currentSolvingChainInfo.IsCode(10045474) || enemyActivateInfiniteImpermanenceFromHand)
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			if (base.Enemy.SpellZone[i] == currentSolvingChainInfo.RelatedCard)
			{
				infiniteImpermanenceList.Add(4 - i);
				break;
			}
		}
	}

	public override void OnChainEnd()
	{
		currentNegateMonsterList.Clear();
		currentNegatingIdList.Clear();
		for (int num = effectUsedBaronneDeFleurList.Count() - 1; num >= 0; num--)
		{
			ClientCard clientCard = effectUsedBaronneDeFleurList[num];
			if (clientCard == null || clientCard.IsFacedown() || clientCard.Location != CardLocation.MonsterZone)
			{
				effectUsedBaronneDeFleurList.RemoveAt(num);
			}
		}
		enemyActivateInfiniteImpermanenceFromHand = false;
		base.OnChainEnd();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (previousControler == 1 && card != null && card.IsCode(10045474) && previousLocation == 2 && currentLocation == 8)
		{
			enemyActivateInfiniteImpermanenceFromHand = true;
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
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

	public bool NibiruThePrimalBeingActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.Player == 0 || base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasType(CardType.Synchro)))
		{
			return false;
		}
		if (base.Util.GetBestAttack(base.Enemy) > base.Util.GetBestAttack(base.Bot))
		{
			if ((base.CurrentTiming & 4) != 0)
			{
				SelectNibiruPosition();
				return true;
			}
			List<ClientCard> list = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && card.IsTuner() && !card.HasType((CardType)75497472)
				select card).ToList();
			List<ClientCard> list2 = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !card.IsTuner() && !card.HasType((CardType)75497472)
				select card).ToList();
			foreach (ClientCard item in list)
			{
				foreach (ClientCard item2 in list2)
				{
					if (item.Level + item2.Level == 10)
					{
						SelectNibiruPosition();
						return true;
					}
				}
			}
		}
		return false;
	}

	public void SelectNibiruPosition()
	{
		int valueOrDefault = (from card in base.Bot.GetMonsters()
			where card.IsFaceup()
			select card).Sum((Func<ClientCard, int?>)((ClientCard m) => m.Attack)).GetValueOrDefault();
		valueOrDefault += (from card in base.Enemy.GetMonsters()
			where card.IsFaceup()
			select card).Sum((Func<ClientCard, int?>)((ClientCard m) => m.Attack)).GetValueOrDefault();
		Logger.DebugWriteLine("Nibiru token attack: " + valueOrDefault);
		if (valueOrDefault >= 3000)
		{
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
		}
		else
		{
			base.AI.SelectPosition(CardPosition.FaceUpAttack);
			base.AI.SelectPosition(CardPosition.FaceUpAttack);
		}
	}

	public bool TenyiSpirit_AshunaActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(87052196, 0))
		{
			if (TenyiSpSummonForTaiaCheck() || Level7TenyiSpSummonCheck())
			{
				return true;
			}
		}
		else if (base.ActivateDescription == base.Util.GetStringId(87052196, 1) && base.Card.Location == CardLocation.Grave && CheckCalledbytheGrave(base.Card.Id) == 0)
		{
			if (base.Bot.HasInHandOrInSpellZone(14821890) && (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType(CardType.Synchro) && card.HasRace(CardRace.Wyrm)
				select card).ToList().Count() == 0)
			{
				base.AI.SelectCard(98159737, 23431858);
				onlyWyrmSpSummon = true;
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			List<int> tunerIdList = new List<int> { 14558127, 97268402, 98159737 };
			if ((base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.IsCode(tunerIdList)) | (!summoned && base.Bot.HasInHand(tunerIdList))) && CheckRemainInDeck(23431858) > 0)
			{
				base.AI.SelectCard(87052196);
				onlyWyrmSpSummon = true;
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (base.Bot.HasInMonstersZone(87052196, notDisabled: false, hasXyzMaterial: false, faceUp: true) && CheckRemainInDeck(98159737) > 0)
			{
				base.AI.SelectCard(98159737);
				onlyWyrmSpSummon = true;
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool TenyiSpirit_VishudaActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(23431858, 0))
		{
			if (TenyiSpSummonForTaiaCheck() || Level7TenyiSpSummonCheck())
			{
				return true;
			}
		}
		else if (base.ActivateDescription == base.Util.GetStringId(23431858, 1) && base.Card.Location == CardLocation.Grave && CheckCalledbytheGrave(base.Card.Id) == 0)
		{
			List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreNormalSpell: true);
			if (problematicEnemyCardList.Count() > 0)
			{
				base.AI.SelectCard(problematicEnemyCardList);
				return true;
			}
		}
		return false;
	}

	public bool Level7TenyiSpSummonCheck()
	{
		List<int> cardId = new List<int> { 20001443, 56495147, 55273560 };
		List<int> cardId2 = new List<int> { 98159737, 97268402, 14558127 };
		if (!summoned && !base.Bot.HasInHand(cardId) && base.Bot.HasInHand(cardId2))
		{
			return true;
		}
		if (base.Bot.HasInExtra(60465049) && base.Bot.HasInMonstersZone(20001444) && !onlyWyrmSpSummon)
		{
			return true;
		}
		return false;
	}

	public bool SwordsoulStrategistLongyuanActivate()
	{
		if (base.Card.Location != CardLocation.Hand)
		{
			return true;
		}
		if (CheckWhetherNegated() || (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2()))
		{
			return false;
		}
		List<int> list = new List<int>();
		if (CheckAtAdvantage() && base.Bot.HasInHand(93850690) && base.Bot.HasInHand(56495147) && !activatedCardIdList.Contains(56495147) && !activatedCardIdList.Contains(93850690))
		{
			list.Add(56495147);
		}
		if (list.Count() == 0)
		{
			foreach (int item in new List<int> { 23431858, 87052196, 98159737 })
			{
				if (base.Bot.HasInHand(item))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count() == 0)
		{
			List<int> checkIdList = new List<int> { 56495147, 20001443, 14821890, 93490856, 56465981 };
			foreach (int item2 in checkIdList)
			{
				if (base.Bot.Hand.Count((ClientCard card) => card != base.Card && card.IsCode(checkIdList)) > 1)
				{
					list.Add(item2);
				}
			}
		}
		if (list.Count() == 0)
		{
			List<int> checkIdList2 = new List<int> { 56495147, 20001443, 14821890, 93490856, 93850690, 56465981 };
			foreach (int item3 in checkIdList2)
			{
				if (base.Bot.Hand.Count((ClientCard card) => card != base.Card && card.IsCode(checkIdList2)) >= 1)
				{
					list.Add(item3);
				}
			}
		}
		if (list.Count() > 0)
		{
			base.AI.SelectCard(list);
			base.AI.SelectPosition(CardPosition.FaceUpAttack);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool SwordsoulOfTaiaActivate()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			List<int> list = new List<int>();
			if (!base.Bot.HasInGraveyard(20001443) && CheckRemainInDeck(20001443) > 0)
			{
				bool flag = false;
				if (base.Bot.HasInMonstersZone(83755611, notDisabled: true, hasXyzMaterial: false, !activatedCardIdList.Contains(83755611)))
				{
					flag = true;
				}
				if (base.Bot.HasInHand(93850690) && !activatedCardIdList.Contains(93850690) && base.Bot.Hand.Any((ClientCard card) => card.Id != 93850690 && (card.HasSetcode(363) || card.HasRace(CardRace.Wyrm))))
				{
					flag = true;
				}
				if (flag)
				{
					list.Add(20001443);
				}
			}
			foreach (int item in new List<int> { 98159737, 23431858, 87052196 })
			{
				if (CheckRemainInDeck(item) > 0)
				{
					list.Add(item);
				}
			}
			if (list.Count() > 0)
			{
				base.AI.SelectCard(list);
				return true;
			}
			return false;
		}
		if (base.Bot.HasInGraveyard(14821890) && !activatedCardIdList.Contains(14821890))
		{
			base.AI.SelectCard(14821890);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>
		{
			93490856, 56465981, 56495147, 20001443, 32519092, 78917791, 93850690, 69248256, 87052196, 23431858,
			47710198, 96633955, 5041348, 98159737
		};
		foreach (int checkId in list3)
		{
			if (base.Bot.Graveyard.Count((ClientCard card) => card.IsCode(checkId)) > 1)
			{
				list2.Add(checkId);
			}
		}
		if (list2.Count() == 0)
		{
			foreach (int item2 in list3)
			{
				if (base.Bot.HasInGraveyard(item2))
				{
					list2.Add(item2);
				}
			}
		}
		if (list2.Count() > 0)
		{
			base.AI.SelectCard(list2);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool SwordsoulOfTaiaSummon()
	{
		if (base.Bot.HasInGraveyard(14821890) && !activatedCardIdList.Contains(56495147) && !activatedCardIdList.Contains(14821890))
		{
			summoned = true;
			return true;
		}
		if (SummonLevel4ForSynchro())
		{
			summoned = true;
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (SwordsoulOfTaiaEffectCheck() && !activatedCardIdList.Contains(56495147))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool SwordsoulOfTaiaEffectCheck(ClientCard exceptTarget = null)
	{
		if (exceptTarget == null)
		{
			exceptTarget = base.Card;
		}
		return base.Bot.Graveyard.Count((ClientCard card) => card != exceptTarget && (card.HasSetcode(363) || card.HasRace(CardRace.Wyrm))) > 0;
	}

	public bool SwordsoulOfMoYeActivate()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> list = base.Bot.Hand.Where((ClientCard card) => card.HasSetcode(363) || card.HasRace(CardRace.Wyrm)).ToList();
		if (list.Count() > 0)
		{
			list = ShuffleCardList(list);
			base.AI.SelectCard(list);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool SwordsoulOfMoYeSummon()
	{
		if (SummonLevel4ForSynchro())
		{
			summoned = true;
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (SwordsoulOfMoYeEffectCheck() && !activatedCardIdList.Contains(20001443))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool SwordsoulOfMoYeEffectCheck(List<ClientCard> exceptList = null)
	{
		if (exceptList == null)
		{
			exceptList = new List<ClientCard> { base.Card };
		}
		return base.Bot.Hand.Count((ClientCard card) => !exceptList.Contains(card) && (card.HasSetcode(363) || card.HasRace(CardRace.Wyrm))) > 0;
	}

	public bool SummonLevel4ForSynchro()
	{
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.HasType((CardType)75497472) && !card.IsTuner()))
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.HasType((CardType)75497472) && card.IsTuner()
			select card).ToList();
		if (list.Count() > 0)
		{
			foreach (ClientCard item in list)
			{
				int checkLevel = item.Level + 4;
				if (base.Bot.ExtraDeck.Any((ClientCard card) => card.HasType(CardType.Synchro) && card.Level == checkLevel))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IncredibleEcclesiaTheVirtuousActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return true;
		}
		if (base.Duel.Player == 0 && !CheckWhetherNegated())
		{
			bool flag = !activatedCardIdList.Contains(20001443) && CheckRemainInDeck(20001443) > 0 && CheckCalledbytheGrave(20001443) == 0 && SwordsoulOfMoYeEffectCheck();
			bool flag2 = !activatedCardIdList.Contains(56495147) && CheckRemainInDeck(56495147) > 0 && CheckCalledbytheGrave(56495147) == 0 && SwordsoulOfTaiaEffectCheck();
			if (flag && !summoned && !base.Bot.HasInHand(20001443))
			{
				base.AI.SelectCard(20001443);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (flag2 && !summoned && !base.Bot.HasInHand(56495147))
			{
				base.AI.SelectCard(56495147);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (flag)
			{
				base.AI.SelectCard(20001443);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (flag2)
			{
				base.AI.SelectCard(56495147);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool IncredibleEcclesiaTheVirtuousSummon()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (SwordsoulOfMoYeSummon() && CheckRemainInDeck(20001443) > 0)
		{
			summoned = true;
			return true;
		}
		if (SwordsoulOfTaiaSummon() && CheckRemainInDeck(56495147) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool IncredibleEcclesiaTheVirtuousSpSummon()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
		{
			return false;
		}
		return true;
	}

	public bool AshBlossomActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (CheckAtAdvantage() && base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard().IsCode(23434538))
		{
			return false;
		}
		if (DefaultAshBlossomAndJoyousSpring())
		{
			CheckDeactiveFlag();
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

	public bool EffectVeilerActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeMonsterTarget: true);
		if (monsterListForTargetNegate.Count() > 0)
		{
			ClientCard clientCard = monsterListForTargetNegate[0];
			currentNegateMonsterList.Add(clientCard);
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
	}

	public bool TunerForSynchroSummon()
	{
		if (!base.Card.IsCode(14558127) && !base.Card.IsCode(98159737) && !base.Card.IsCode(97268402))
		{
			return false;
		}
		if (base.Bot.HasInExtra(32519092) && base.Bot.HasInHand(56495147) && !activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0)
		{
			return false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.IsTuner()))
		{
			return false;
		}
		List<int> checkOnField = new List<int> { 23431858, 87052196 };
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && checkOnField.Contains(card.Id) && !card.IsTuner()))
		{
			int totalLevel = base.Card.Level + 7;
			if (base.Bot.ExtraDeck.Any((ClientCard card) => card.HasType(CardType.Synchro) && card.Level == totalLevel && (!onlyWyrmSpSummon || card.HasRace(CardRace.Wyrm))))
			{
				summoned = true;
				return true;
			}
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.IsTuner()
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		if (base.Bot.HasInExtra(43202238) && GetProblematicEnemyCardList(canBeTarget: true, ignoreNormalSpell: true).Count() > 0)
		{
			foreach (ClientCard item in list)
			{
				if (base.Card.Level + item.Level == 7)
				{
					summoned = true;
					return true;
				}
			}
		}
		if (base.Bot.HasInExtra(60465049))
		{
			foreach (ClientCard item2 in list)
			{
				if ((item2.IsDisabled() || !item2.HasType(CardType.Synchro)) && base.Card.Level + item2.Level == 11)
				{
					summoned = true;
					return true;
				}
			}
		}
		return false;
	}

	public bool WyrmForBlackoutSummon()
	{
		if (base.Card.Level > 4 || !base.Card.HasRace(CardRace.Wyrm))
		{
			return false;
		}
		if (base.Bot.HasInHandOrInSpellZone(14821890) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasRace(CardRace.Wyrm)))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool TenyiSpirit_AdharaActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(98159737, 0))
		{
			if (TenyiSpSummonForTaiaCheck())
			{
				return true;
			}
		}
		else if (base.ActivateDescription == base.Util.GetStringId(98159737, 1) && base.Card.Location == CardLocation.Grave && CheckCalledbytheGrave(base.Card.Id) == 0)
		{
			if (!activatedCardIdList.Contains(93490856) && SwordsoulOfMoYeEffectCheck() && base.Bot.HasInBanished(93490856))
			{
				base.AI.SelectCard(93490856);
				return true;
			}
			if (!summoned)
			{
				if (!activatedCardIdList.Contains(20001443) && SwordsoulOfMoYeEffectCheck() && base.Bot.HasInBanished(20001443))
				{
					base.AI.SelectCard(20001443);
					return true;
				}
				if (!activatedCardIdList.Contains(56495147) && SwordsoulOfTaiaEffectCheck() && base.Bot.HasInBanished(56495147))
				{
					base.AI.SelectCard(56495147);
					return true;
				}
			}
			foreach (int item in new List<int> { 23431858, 87052196 })
			{
				if (base.Bot.HasInBanished(item))
				{
					base.AI.SelectCard(item);
					return true;
				}
			}
		}
		return false;
	}

	public bool TenyiSpSummonForTaiaCheck()
	{
		if (!activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0)
		{
			bool num = (!summoned && base.Bot.HasInHand(56495147)) || base.Bot.HasInMonstersZone(56495147);
			bool flag = !base.Bot.Graveyard.Any((ClientCard card) => card.HasRace(CardRace.Wyrm) || card.HasSetcode(363));
			bool flag2 = base.Bot.HasInExtra(32519092);
			bool flag3 = activatedCardIdList.Contains(93490856) || !base.Bot.HasInHand(93490856);
			if (num && flag && flag2 && flag3)
			{
				return true;
			}
		}
		return false;
	}

	public bool TenyiForShamanSpSummon()
	{
		if (!new List<int>
		{
			base.Util.GetStringId(98159737, 0),
			base.Util.GetStringId(23431858, 0),
			base.Util.GetStringId(87052196, 0)
		}.Contains(base.ActivateDescription) || summoned || !base.Bot.HasInExtra(78917791) || (CheckAtAdvantage() && enemyActivateMaxxC))
		{
			return false;
		}
		ClientCard clientCard = base.Bot.Hand.FirstOrDefault((ClientCard card) => card.IsCode(20001443));
		if (clientCard == null)
		{
			return false;
		}
		List<ClientCard> exceptList = new List<ClientCard> { base.Card, clientCard };
		if (!SwordsoulOfMoYeEffectCheck(exceptList) || activatedCardIdList.Contains(20001443))
		{
			return false;
		}
		if (activatedCardIdList.Contains(56495147) || !base.Bot.HasInHandOrInGraveyard(56495147))
		{
			return false;
		}
		return true;
	}

	public bool TenyiForBlackoutSpSummon()
	{
		if (!new List<int>
		{
			base.Util.GetStringId(98159737, 0),
			base.Util.GetStringId(23431858, 0),
			base.Util.GetStringId(87052196, 0)
		}.Contains(base.ActivateDescription))
		{
			return false;
		}
		if (CheckAtAdvantage() && enemyActivateMaxxC)
		{
			return false;
		}
		if (base.Bot.HasInHandOrInSpellZone(14821890) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasRace(CardRace.Wyrm)))
		{
			return true;
		}
		return false;
	}

	public bool PotOfDesiresActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (CheckAtAdvantage())
		{
			bool num = base.Bot.Deck.Count() >= 15;
			if (num)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			return num;
		}
		SelectSTPlace(null, avoidImpermanence: true);
		return true;
	}

	public bool SwordsoulEmergenceActivate()
	{
		if (base.Card.Location == CardLocation.Removed)
		{
			return SwordsoulSpellBanishedEffect();
		}
		if (!base.Bot.HasInHand(20001443) && !activatedCardIdList.Contains(20001443) && CheckRemainInDeck(20001443) > 0 && SwordsoulOfMoYeEffectCheck())
		{
			base.AI.SelectCard(20001443);
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		if (!base.Bot.HasInHand(56495147) && !activatedCardIdList.Contains(56495147) && CheckRemainInDeck(56495147) > 0 && SwordsoulOfTaiaEffectCheck())
		{
			base.AI.SelectCard(56495147);
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		if (!base.Bot.HasInHand(93490856) && !activatedCardIdList.Contains(93490856) && CheckRemainInDeck(93490856) > 0 && SwordsoulOfMoYeEffectCheck())
		{
			base.AI.SelectCard(93490856);
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		if (!base.Bot.HasInHand(20001443) && CheckRemainInDeck(20001443) > 0 && SwordsoulOfMoYeEffectCheck())
		{
			base.AI.SelectCard(20001443);
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		foreach (int item in new List<int> { 56495147, 20001443, 93490856 })
		{
			if (CheckRemainInDeck(item) > 0)
			{
				base.AI.SelectCard(item);
				activatedCardIdList.Add(base.Card.Id);
				SelectSTPlace(null, avoidImpermanence: true);
				return true;
			}
		}
		return false;
	}

	public bool SwordsoulSacredSummitActivate()
	{
		if (base.Card.Location == CardLocation.Removed)
		{
			return SwordsoulSpellBanishedEffect();
		}
		if (CheckAtAdvantage())
		{
			if (enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
			{
				return false;
			}
			if (!activatedCardIdList.Contains(20001443) && base.Bot.HasInGraveyard(20001443) && CheckCalledbytheGrave(20001443) == 0 && SwordsoulOfMoYeEffectCheck())
			{
				base.AI.SelectCard(20001443);
				activatedCardIdList.Add(base.Card.Id);
				SelectSTPlace(null, avoidImpermanence: true);
				return true;
			}
			if (!activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0)
			{
				ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(56495147));
				if (clientCard != null && SwordsoulOfTaiaEffectCheck(clientCard))
				{
					base.AI.SelectCard(56495147);
					activatedCardIdList.Add(base.Card.Id);
					SelectSTPlace(null, avoidImpermanence: true);
					return true;
				}
			}
		}
		bool controlSynchro = base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasType(CardType.Synchro));
		List<ClientCard> list = base.Bot.Graveyard.Where((ClientCard card) => card.IsMonster() && (card.HasSetcode(363) || (controlSynchro && card.HasRace(CardRace.Wyrm)))).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		if (list.Count() > 0)
		{
			ClientCard clientCard2 = list[0];
			if (clientCard2.IsCode(20001443) && (activatedCardIdList.Contains(20001443) || !SwordsoulOfMoYeEffectCheck()))
			{
				return false;
			}
			if (clientCard2.IsCode(56495147) && activatedCardIdList.Contains(56495147))
			{
				return false;
			}
			base.AI.SelectCard(list);
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool SwordsoulSpellBanishedEffect()
	{
		return false;
	}

	public bool CalledbytheGraveActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (CheckAtAdvantage() && base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard().IsCode(23434538))
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			if (base.Util.GetLastChainCard().IsMonster())
			{
				int code = base.Util.GetLastChainCard().GetOriginCode();
				if (code == 0)
				{
					return false;
				}
				if (CheckCalledbytheGrave(code) > 0)
				{
					return false;
				}
				if (base.Util.GetLastChainCard().IsCode(23434538) && CheckAtAdvantage())
				{
					return false;
				}
				ClientCard firstMatchingCard = base.Enemy.Graveyard.GetFirstMatchingCard((ClientCard card) => card.IsMonster() && card.GetOriginCode() == code);
				if (firstMatchingCard != null)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					base.AI.SelectCard(firstMatchingCard);
					currentNegatingIdList.Add(code);
					CheckDeactiveFlag();
					return true;
				}
			}
			foreach (ClientCard item in base.Enemy.Graveyard)
			{
				if (base.Duel.ChainTargets.Contains(item) && item.IsMonster())
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					int id = item.Id;
					base.AI.SelectCard(item);
					currentNegatingIdList.Add(id);
					return true;
				}
			}
			if (base.Duel.ChainTargets.Contains(base.Card))
			{
				List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => card.IsMonster()).ToList();
				if (list.Count() > 0)
				{
					list.Sort(CardContainer.CompareCardAttack);
					list.Reverse();
					int id2 = list[0].Id;
					base.AI.SelectCard(id2);
					currentNegatingIdList.Add(id2);
					return true;
				}
			}
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			return false;
		}
		List<ClientCard> list2 = CheckDangerousCardinEnemyGrave(onlyMonster: true);
		if (list2.Count() > 0)
		{
			int id3 = list2[0].Id;
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(id3);
			currentNegatingIdList.Add(id3);
			return true;
		}
		return false;
	}

	public bool CrossoutDesignatorActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard() != null)
		{
			int num = base.Util.GetLastChainCard().Id;
			int alias = base.Util.GetLastChainCard().Alias;
			if (alias != 0 && alias - num < 10)
			{
				num = alias;
			}
			if (num == 0)
			{
				return false;
			}
			if (DefaultCheckWhetherCardIdIsNegated(num))
			{
				return false;
			}
			if (CheckRemainInDeck(num) > 0)
			{
				if (base.Card.Location != CardLocation.SpellZone)
				{
					SelectSTPlace(null, avoidImpermanence: true);
				}
				base.AI.SelectAnnounceID(num);
				currentNegatingIdList.Add(num);
				CheckDeactiveFlag();
				return true;
			}
		}
		return false;
	}

	public bool InfiniteImpermanenceActivate()
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
				CheckDeactiveFlag();
				ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true);
				if (problematicEnemyMonster != null)
				{
					base.AI.SelectCard(problematicEnemyMonster);
				}
				else
				{
					base.AI.SelectCard(base.Enemy.GetMonsters());
				}
				infiniteImpermanenceList.Add(num);
				return true;
			}
		}
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeMonsterTarget: false, canBeTrapTarget: true);
		if (monsterListForTargetNegate.Count() > 0)
		{
			ClientCard clientCard = monsterListForTargetNegate[0];
			currentNegateMonsterList.Add(clientCard);
			if (base.Card.Location == CardLocation.SpellZone)
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
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
	}

	public bool SwordsoulBlackoutActivate()
	{
		if (base.Card.Location == CardLocation.Removed)
		{
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.HasRace(CardRace.Wyrm)
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		ClientCard clientCard = list[0];
		bool flag = clientCard.HasType(CardType.Synchro);
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card.IsCode(96633955) && card.IsFaceup() && !card.IsDisabled()
			select card).ToList();
		if (list2.Count() > 0 && base.Bot.Graveyard.Count() > 0)
		{
			clientCard = list2[0];
			flag = false;
		}
		foreach (ClientCard item in list)
		{
			if (base.Duel.LastChainTargets.Contains(item))
			{
				clientCard = item;
				flag = false;
			}
		}
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true);
		if (problematicEnemyCardList.Count() >= 2 && base.Duel.Player == 1)
		{
			base.AI.SelectCard(clientCard);
			base.AI.SelectNextCard(problematicEnemyCardList);
			return true;
		}
		List<ClientCard> list3 = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup()
			select card).ToList();
		list3.Sort(CardContainer.CompareCardAttack);
		list3.Reverse();
		if (!flag && base.Duel.Player == 1)
		{
			if (list3.Count() >= 2)
			{
				base.AI.SelectCard(clientCard);
				base.AI.SelectNextCard(GetNormalEnemyTargetList());
				return true;
			}
			if (base.Duel.Phase == DuelPhase.End)
			{
				base.AI.SelectCard(clientCard);
				base.AI.SelectNextCard(GetNormalEnemyTargetList());
				return true;
			}
		}
		if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2 && list3.Count() > 0)
		{
			int bestAttack = base.Util.GetBestAttack(base.Bot);
			if (list3[0].GetDefensePower() >= bestAttack)
			{
				base.AI.SelectCard(clientCard);
				base.AI.SelectNextCard(GetNormalEnemyTargetList());
				return true;
			}
		}
		return false;
	}

	public bool GeomathmechFinalSigmaSpSummon()
	{
		if (base.Bot.GetMonstersExtraZoneCount() > 0)
		{
			return false;
		}
		if (base.Enemy.GetMonsters().Any((ClientCard card) => card.HasSetcode(365)) | base.Enemy.GetSpells().Any((ClientCard card) => card.HasSetcode(365)) | base.Enemy.Graveyard.Any((ClientCard card) => card.HasSetcode(365)) | base.Enemy.Banished.Any((ClientCard card) => card.HasSetcode(365)))
		{
			base.AI.SelectMaterials(GetSynchroMaterial(12));
			base.AI.SelectPosition(CardPosition.FaceUpAttack);
			return true;
		}
		return false;
	}

	public bool PsychicEndPunisherSpSummon()
	{
		List<ClientCard> synchroMaterial = GetSynchroMaterial(11);
		if (synchroMaterial.Count() > 1)
		{
			base.AI.SelectMaterials(synchroMaterial);
			base.AI.SelectPosition(CardPosition.FaceUpAttack);
			return true;
		}
		return false;
	}

	public bool Level10SpSummonCheckInit()
	{
		canSpSummonLevel10IdList.Clear();
		return false;
	}

	public bool Level10SpSummonCheckCount()
	{
		foreach (int item in new List<int> { 96633955, 47710198, 84815190 })
		{
			if (base.Card.IsCode(item))
			{
				canSpSummonLevel10IdList.Add(item);
			}
		}
		return false;
	}

	public bool Level10SpSummonCheckDecide()
	{
		if (canSpSummonLevel10IdList.Count <= 1)
		{
			return false;
		}
		List<int> list = new List<int>();
		if (canSpSummonLevel10IdList.Contains(84815190))
		{
			if (base.Bot.HasInHand(23434538))
			{
				canSpSummonLevel10IdList.Clear();
				canSpSummonLevel10IdList.Add(84815190);
				return false;
			}
			ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(56495147));
			if (clientCard != null && SwordsoulOfTaiaEffectCheck(clientCard) && base.Bot.HasInHand(93850690))
			{
				canSpSummonLevel10IdList.Clear();
				canSpSummonLevel10IdList.Add(84815190);
				return false;
			}
			list.Add(84815190);
		}
		if (canSpSummonLevel10IdList.Contains(47710198) && CheckAtAdvantage())
		{
			list.Add(47710198);
		}
		if (canSpSummonLevel10IdList.Contains(96633955))
		{
			int num = base.Bot.Banished.Count() + base.Enemy.Banished.Count();
			bool flag = base.Bot.HasInHandOrInSpellZone(14821890) || base.Bot.HasInMonstersZone(69248256, notDisabled: true, hasXyzMaterial: false, faceUp: true);
			if (CheckAtAdvantage())
			{
				if (3000 + num * 100 >= base.Enemy.LifePoints)
				{
					flag = true;
				}
			}
			else
			{
				ClientCard bestEnemyMonster = GetBestEnemyMonster(onlyFaceup: true);
				if (bestEnemyMonster != null && list.Count() == 0 && 3000 + num * 200 >= bestEnemyMonster.GetDefensePower())
				{
					flag = true;
				}
			}
			if (flag)
			{
				list.Add(96633955);
			}
		}
		if (list.Count() > 0)
		{
			canSpSummonLevel10IdList.Clear();
			int index = Program.Rand.Next(list.Count());
			int item = list[index];
			canSpSummonLevel10IdList.Add(item);
		}
		return false;
	}

	public bool Level10SpSummonCheckFinal()
	{
		if (canSpSummonLevel10IdList.Count() == 1)
		{
			int id = canSpSummonLevel10IdList[0];
			if (base.Card.IsCode(id))
			{
				List<ClientCard> synchroMaterial = GetSynchroMaterial(10, base.Card.IsCode(47710198));
				if (synchroMaterial.Count() > 1)
				{
					base.AI.SelectMaterials(synchroMaterial);
					return true;
				}
				return true;
			}
		}
		return false;
	}

	public bool AdamancipatorRisen_DragiteSpSummon()
	{
		if (!base.Bot.HasInMonstersZone(69248256, notDisabled: true))
		{
			return false;
		}
		bool flag = base.Bot.Graveyard.Any((ClientCard card) => card.IsMonster() && card.HasAttribute(CardAttribute.Water));
		if (!(flag | base.Bot.GetMonsters().Any((ClientCard card) => card.HasAttribute(CardAttribute.Water) && card.IsFaceup())))
		{
			return false;
		}
		SelectLevel8SynchroMaterial(needWyrmNonTuner: false, !flag);
		return true;
	}

	public bool DracoBerserkerOfTheTenyiSpSummon()
	{
		if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
		{
			return false;
		}
		SelectLevel8SynchroMaterial(needWyrmNonTuner: true);
		return true;
	}

	public bool SwordsoulGrandmaster_ChixiaoSpSummon()
	{
		if (CheckAtAdvantage() && enemyActivateLockBird)
		{
			return false;
		}
		if (!activatedCardIdList.Contains(69248256))
		{
			SelectLevel8SynchroMaterial(needWyrmNonTuner: true);
			return true;
		}
		return false;
	}

	public bool BaxiaBrightnessOfTheYangZingSpSummon()
	{
		if (CheckAtAdvantage())
		{
			return false;
		}
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true);
		if (problematicEnemyCardList.Count() > 1 && !activatedCardIdList.Contains(83755612))
		{
			SelectLevel8SynchroMaterial(needWyrmNonTuner: true);
			return true;
		}
		if (problematicEnemyCardList.Count() == 1 && base.Bot.GetSpellCount() > 0 && !activatedCardIdList.Contains(83755613))
		{
			bool flag = false;
			if (!activatedCardIdList.Contains(20001443) && SwordsoulOfMoYeEffectCheck() && base.Bot.HasInGraveyard(20001443))
			{
				flag = true;
			}
			if (!activatedCardIdList.Contains(56495147) && base.Bot.HasInGraveyard(56495147))
			{
				flag = true;
			}
			if (flag)
			{
				SelectLevel8SynchroMaterial(needWyrmNonTuner: true);
				return true;
			}
		}
		return false;
	}

	public void SelectLevel8SynchroMaterial(bool needWyrmNonTuner = false, bool needWaterNonTuner = false)
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.IsTuner() && card.Level < 8
			select card).ToList();
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.IsTuner() && card.Level < 8 && (!needWyrmNonTuner || (card.HasRace(CardRace.Wyrm) && (!needWaterNonTuner || card.HasAttribute(CardAttribute.Water))))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list2.Sort(CardContainer.CompareCardAttack);
		List<ClientCard> list3 = new List<ClientCard>();
		foreach (ClientCard item in list)
		{
			list3.Clear();
			list3.Add(item);
			if (item.Level == 4)
			{
				if (activatedCardIdList.Contains(20001443))
				{
					ClientCard firstMatchingCard = list2.GetFirstMatchingCard((ClientCard card) => card.IsCode(20001443));
					if (firstMatchingCard != null)
					{
						list3.Add(firstMatchingCard);
						base.AI.SelectMaterials(list3);
						break;
					}
				}
				if (activatedCardIdList.Contains(56495147) && !needWaterNonTuner)
				{
					ClientCard firstMatchingCard2 = list2.GetFirstMatchingCard((ClientCard card) => card.IsCode(56495147));
					if (firstMatchingCard2 != null)
					{
						list3.Add(firstMatchingCard2);
						base.AI.SelectMaterials(list3);
						break;
					}
				}
			}
			foreach (ClientCard item2 in list2)
			{
				if (item.Level + item2.Level == 8)
				{
					list3.Add(item2);
					base.AI.SelectMaterials(list3);
					return;
				}
			}
		}
	}

	public bool YaziEvilOfTheYangZingSpSummon()
	{
		if (base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() == 0)
		{
			return false;
		}
		if ((GetProblematicEnemyCardList(canBeTarget: true, ignoreNormalSpell: true).Count() > 0) | (!activatedCardIdList.Contains(20001443) && CheckCalledbytheGrave(20001443) == 0 && CheckRemainInDeck(20001443) > 0 && SwordsoulOfMoYeEffectCheck()) | (!activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0 && CheckRemainInDeck(56495147) > 0))
		{
			List<ClientCard> synchroMaterial = GetSynchroMaterial(7);
			if (synchroMaterial.Count() > 1)
			{
				base.AI.SelectMaterials(synchroMaterial);
				return true;
			}
		}
		return false;
	}

	public List<ClientCard> GetSynchroMaterial(int level, bool needWyrmNonTuner = false)
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.IsTuner() && !card.HasType((CardType)75497472)
			select card).ToList();
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.IsTuner() && !card.HasType((CardType)75497472) && (!needWyrmNonTuner || card.HasRace(CardRace.Wyrm))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list2.Sort(CardContainer.CompareCardAttack);
		List<ClientCard> list3 = new List<ClientCard>();
		foreach (ClientCard item in list)
		{
			list3.Clear();
			list3.Add(item);
			foreach (ClientCard item2 in list2)
			{
				if (item.Level + item2.Level == level && (item2.IsDisabled() || !item2.HasType(CardType.Synchro)))
				{
					list3.Add(item2);
					return list3;
				}
			}
		}
		list3.Clear();
		return list3;
	}

	public bool ShamanOfTheTenyiSpSummon()
	{
		if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
		{
			Logger.DebugWriteLine("[Shaman] advantage & maxxc, skip");
			return false;
		}
		List<ClientCard> monstersInExtraZone = base.Bot.GetMonstersInExtraZone();
		if (monstersInExtraZone.Count() > 0 && monstersInExtraZone.Any((ClientCard card) => card.IsFacedown() || !card.HasType(CardType.Link) || !card.HasRace(CardRace.Wyrm)))
		{
			Logger.DebugWriteLine("[Shaman] extra zone occupied, skip");
			return false;
		}
		if (!((!activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0 && base.Bot.HasInHandOrInGraveyard(56495147)) | (!activatedCardIdList.Contains(20001443) && CheckCalledbytheGrave(20001443) == 0 && base.Bot.HasInGraveyard(20001443) && SwordsoulOfMoYeEffectCheck()) | base.Bot.GetGraveyardMonsters().Any((ClientCard card) => card.HasType(CardType.Synchro) && card.IsCanRevive() && card.HasRace(CardRace.Wyrm))))
		{
			Logger.DebugWriteLine("[Shaman] no target, skip");
			return false;
		}
		List<ClientCard> list = new List<ClientCard>(monstersInExtraZone);
		List<ClientCard> list2 = (from card in base.Bot.GetMonstersInMainZone()
			where card.IsFaceup() && !card.HasType(CardType.Synchro) && card.HasRace(CardRace.Wyrm)
			select card).ToList();
		list2.Sort(CardContainer.CompareCardAttack);
		list.AddRange(list2);
		if (list.Count() >= 2)
		{
			base.AI.SelectMaterials(list.GetRange(0, 2));
			return true;
		}
		return false;
	}

	public bool MonkOfTheTenyiSpSummon()
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.HasType((CardType)67117056) && card.HasSetcode(300)
			select card).ToList();
		if (list.Count() > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			base.AI.SelectMaterials(list);
			return true;
		}
		return false;
	}

	public bool PsychicEndPunisherActivate()
	{
		if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
		{
			return true;
		}
		if (base.Bot.LifePoints <= 1500 || CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card != base.Card && (card.IsFacedown() || card.GetDefensePower() <= 1000)
			select card).ToList();
		if (list.Count() == 0)
		{
			return false;
		}
		list.Sort(CardContainer.CompareCardAttack);
		base.AI.SelectCard(list);
		base.AI.SelectNextCard(GetNormalEnemyTargetList());
		return true;
	}

	public bool SwordsoulSupremeSovereign_ChengyingActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(96633955, 0) || base.ActivateDescription == -1)
		{
			activatedCardIdList.Add(base.Card.Id);
			List<ClientCard> list = base.Duel.CurrentChain.Where((ClientCard card) => card.Controller == 1 && card.Location == CardLocation.Grave).ToList();
			list.AddRange(CheckDangerousCardinEnemyGrave());
			if (list.Count() > 0)
			{
				Logger.DebugWriteLine("Chengying banish grave: " + list[0]?.Name);
			}
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList();
			if (normalEnemyTargetList.Count() > 0)
			{
				Logger.DebugWriteLine("Chengying banish field: " + normalEnemyTargetList[0]?.Name);
			}
			list.AddRange(normalEnemyTargetList);
			base.AI.SelectCard(list);
		}
		else if (base.ActivateDescription == 96)
		{
			List<int> ids = new List<int>
			{
				24224830, 65681983, 10045474, 14558127, 23434538, 97268402, 32519092, 78917791, 69248256, 56495147,
				93490856, 20001443
			};
			base.AI.SelectCard(ids);
		}
		else
		{
			Logger.DebugWriteLine("Chengying desc: " + base.ActivateDescription);
		}
		return true;
	}

	public bool BaronneDeFleurActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(84815190, 1))
		{
			if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
			{
				return false;
			}
			if (base.Duel.LastChainPlayer == 1)
			{
				ClientCard lastChainCard = base.Util.GetLastChainCard();
				if (CheckAtAdvantage() && lastChainCard.IsCode(23434538))
				{
					return false;
				}
				if (base.Duel.LastChainTargets.Contains(base.Card) && lastChainCard.IsCode(97268402, 10045474, 78474168))
				{
					return false;
				}
			}
			CheckDeactiveFlag();
			effectUsedBaronneDeFleurList.Add(base.Card);
			return true;
		}
		if (base.Duel.Phase == DuelPhase.Standby)
		{
			if (effectUsedBaronneDeFleurList.Contains(base.Card) && !CheckWhetherNegated())
			{
				if (base.Duel.Player == 1)
				{
					if (!base.Bot.HasInMonstersZone(69248256) && base.Bot.HasInGraveyard(69248256))
					{
						base.AI.SelectCard(69248256);
						return true;
					}
				}
				else
				{
					if (GetProblematicEnemyCardList(canBeTarget: true, ignoreNormalSpell: true).Count() > 0)
					{
						return false;
					}
					if (CheckAtAdvantage() && base.Bot.ExtraDeck.Any((ClientCard card) => card.IsFacedown() && card.HasType(CardType.Synchro) && card.Level == 8))
					{
						if (base.Bot.HasInGraveyard(20001443) && SwordsoulOfMoYeEffectCheck() && CheckCalledbytheGrave(20001443) == 0)
						{
							base.AI.SelectCard(20001443);
							return true;
						}
						if (CheckCalledbytheGrave(56495147) == 0)
						{
							ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(56495147));
							if (clientCard != null && SwordsoulOfTaiaEffectCheck(clientCard))
							{
								base.AI.SelectCard(56495147);
								return true;
							}
						}
					}
				}
			}
		}
		else
		{
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList();
			if (normalEnemyTargetList.Count() > 0)
			{
				base.AI.SelectCard(normalEnemyTargetList);
				return true;
			}
		}
		return false;
	}

	public bool SwordsoulSinisterSovereign_QixingLongyuanActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(47710198, 0))
		{
			return true;
		}
		if (base.ActivateDescription == -1 || base.ActivateDescription == base.Util.GetStringId(47710198, 1))
		{
			return true;
		}
		if (base.ActivateDescription == base.Util.GetStringId(47710198, 2))
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && (DefaultOnBecomeTarget() | (base.Enemy.LifePoints <= 1200) | lastChainCard.HasType((CardType)17694720)))
			{
				return true;
			}
		}
		else
		{
			Logger.DebugWriteLine("qixinglongyuan desc: " + base.ActivateDescription);
		}
		return false;
	}

	public bool AdamancipatorRisen_DragiteActivate()
	{
		if (base.ActivateDescription == -1 || base.ActivateDescription == base.Util.GetStringId(9464441, 0))
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			if (CheckRemainInDeck(27204311) > 0 && base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() > 0)
			{
				base.AI.SelectCard(GetNormalEnemyTargetList(canBeTarget: false));
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		return true;
	}

	public bool DracoBerserkerOfTheTenyiActivate()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.IsCode(27204311) && lastChainCard.Controller == 1)
		{
			return false;
		}
		return true;
	}

	public bool SwordsoulGrandmaster_ChixiaoActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(69248256, 1))
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			List<ClientCard> list = new List<ClientCard>();
			List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeMonsterTarget: true);
			if (monsterListForTargetNegate.Count() > 0)
			{
				ClientCard item = monsterListForTargetNegate[0];
				currentNegateMonsterList.Add(item);
				list.AddRange(monsterListForTargetNegate);
			}
			if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
			{
				bool flag = base.Bot.GetMonsters().Any((ClientCard card) => card.IsAttack());
				if (base.Duel.Player == 0 && flag)
				{
					list.AddRange((from card in base.Enemy.GetMonsters()
						where card.IsFaceup() && card.IsMonsterDangerous()
						select card).ToList());
				}
				if (base.Duel.Player == 1)
				{
					ClientCard battlingMonster = base.Enemy.BattlingMonster;
					if (battlingMonster != null && battlingMonster.IsMonsterInvincible())
					{
						list.Add(battlingMonster);
					}
				}
			}
			if (base.Bot.HasInMonstersZone(96633955, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !activatedCardIdList.Contains(96633955) && base.Enemy.Graveyard.Count() > 0 && (GetProblematicEnemyMonster() != null || (base.Duel.Phase == DuelPhase.End && base.Duel.Player == 1)))
			{
				bool flag2 = true;
				List<ClientCard> list2 = (from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && card.HasType(CardType.Effect) && !card.IsShouldNotBeMonsterTarget() && card.IsShouldNotBeTarget()
					select card).ToList();
				if (list2.Count() == 0)
				{
					List<ClientCard> list3 = (from card in base.Bot.GetMonsters()
						where card.IsFaceup() && card.HasType(CardType.Effect) && !card.IsDisabled() && card != base.Card && !card.IsCode(96633955)
						select card).ToList();
					if (list3.Count() == 0)
					{
						flag2 = false;
					}
					else
					{
						list3.Sort(CardContainer.CompareCardAttack);
						list2.AddRange(list3);
					}
				}
				else
				{
					list2.Sort(CardContainer.CompareCardAttack);
					list2.Reverse();
				}
				if (flag2)
				{
					list.AddRange(list2);
				}
			}
			if (list.Count() > 0)
			{
				List<ClientCard> list4 = base.Bot.Graveyard.Where((ClientCard card) => card.HasSetcode(363) || card.HasRace(CardRace.Wyrm)).ToList();
				if (list4.Count() > 0)
				{
					bool flag3 = false;
					ClientCard clientCard = list4.FirstOrDefault((ClientCard card) => card.IsCode(14821890));
					if (base.Duel.Player == 0 && !activatedCardIdList.Contains(14821890) && clientCard != null)
					{
						base.AI.SelectCard(clientCard);
						flag3 = true;
					}
					if (!flag3)
					{
						foreach (int checkId in new List<int> { 56465981, 56495147, 20001443, 93490856, 32519092, 98159737, 23431858, 87052196 })
						{
							List<ClientCard> list5 = list4.Where((ClientCard card) => card.IsCode(checkId)).ToList();
							if (list5.Count() > 1)
							{
								base.AI.SelectCard(list5);
								flag3 = true;
								break;
							}
						}
					}
					if (!flag3)
					{
						foreach (int checkId2 in new List<int> { 56465981, 32519092, 78917791, 56495147, 93490856, 20001443, 98159737, 23431858, 87052196 })
						{
							List<ClientCard> list6 = list4.Where((ClientCard card) => card.IsCode(checkId2)).ToList();
							if (list6.Count() > 0)
							{
								base.AI.SelectCard(list6);
								flag3 = true;
								break;
							}
						}
					}
					if (!flag3)
					{
						base.AI.SelectCard(ShuffleCardList(list4));
					}
				}
				base.AI.SelectNextCard(list);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		else
		{
			if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
			{
				if (CheckRemainInDeck(14821890) > 0)
				{
					base.AI.SelectCard(14821890);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
				foreach (int item2 in new List<int> { 14821890, 20001443, 56495147, 56465981, 93490856 })
				{
					if (CheckRemainInDeck(item2) > 0 && !base.Bot.HasInHand(item2))
					{
						base.AI.SelectCard(item2);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
			if (CheckAtAdvantage())
			{
				if (!activatedCardIdList.Contains(93490856) && !base.Bot.HasInHand(93490856) && SwordsoulOfMoYeEffectCheck() && CheckRemainInDeck(93490856) > 0)
				{
					base.AI.SelectCard(93490856);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
				if (!activatedCardIdList.Contains(93490856) && base.Bot.HasInHand(93490856) && !activatedCardIdList.Contains(56495147) && !activatedCardIdList.Contains(93850690))
				{
					if (base.Bot.HasInHandOrInGraveyard(56495147) && !base.Bot.HasInHand(93850690) && CheckRemainInDeck(93850690) > 0)
					{
						base.AI.SelectCard(93850690);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
					if (!base.Bot.HasInHandOrInGraveyard(56495147) && base.Bot.HasInHand(93850690) && CheckRemainInDeck(56495147) > 0)
					{
						base.AI.SelectCard(56495147);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
			if (!base.Bot.HasInMonstersZone(20001444) && base.Bot.HasInMonstersZone(93490856) && base.Bot.HasInMonstersZone(93490856) && CheckRemainInDeck(14821890) > 0 && !activatedCardIdList.Contains(14821890))
			{
				Logger.DebugWriteLine("Chixiao banish blackout");
				base.AI.SelectCard(14821890);
				base.AI.SelectOption(1);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (CheckAtAdvantage())
			{
				foreach (int item3 in new List<int> { 14821890, 20001443, 56495147, 56465981, 93490856 })
				{
					if (CheckRemainInDeck(item3) > 0 && !base.Bot.HasInHand(item3))
					{
						base.AI.SelectCard(item3);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
			foreach (int item4 in new List<int> { 14821890, 20001443, 56495147, 56465981, 93490856 })
			{
				if (CheckRemainInDeck(item4) > 0 && !base.Bot.HasInHand(item4))
				{
					base.AI.SelectCard(item4);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
		}
		return false;
	}

	public bool BaxiaBrightnessOfTheYangZingActivate()
	{
		Logger.DebugWriteLine("Baxia desc: " + base.ActivateDescription);
		if (base.ActivateDescription == base.Util.GetStringId(83755611, 0))
		{
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList();
			if (normalEnemyTargetList.Count() > 0)
			{
				base.AI.SelectCard(normalEnemyTargetList);
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
		}
		else
		{
			List<ClientCard> spells = base.Bot.GetSpells();
			spells.AddRange((from card in base.Bot.GetMonsters()
				where card.IsFacedown() || card.Attack <= 1000
				select card).ToList());
			if (spells.Count() == 0)
			{
				return false;
			}
			bool flag = !activatedCardIdList.Contains(20001443) && CheckCalledbytheGrave(20001443) == 0 && SwordsoulOfMoYeEffectCheck();
			bool flag2 = !activatedCardIdList.Contains(56495147) && CheckCalledbytheGrave(56495147) == 0 && SwordsoulOfTaiaEffectCheck();
			if (flag && base.Bot.HasInGraveyard(20001443))
			{
				base.AI.SelectCard(spells);
				base.AI.SelectNextCard(20001443);
				activatedCardIdList.Add(base.Card.Id + 2);
				return true;
			}
			if (flag2 && base.Bot.HasInGraveyard(56495147))
			{
				base.AI.SelectCard(spells);
				base.AI.SelectNextCard(56495147);
				activatedCardIdList.Add(base.Card.Id + 2);
				return true;
			}
			if (base.Bot.HasInGraveyard(55273560))
			{
				if (!activatedCardIdList.Contains(55273560) && ((flag && CheckRemainInDeck(20001443) > 0) || (flag2 && CheckRemainInDeck(56495147) > 0)))
				{
					base.AI.SelectCard(spells);
					base.AI.SelectNextCard(55273560);
					activatedCardIdList.Add(base.Card.Id + 2);
					return true;
				}
				if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.IsTuner() && card.Level == 4))
				{
					base.AI.SelectCard(spells);
					base.AI.SelectNextCard(55273560);
					activatedCardIdList.Add(base.Card.Id + 2);
					return true;
				}
			}
		}
		return false;
	}

	public bool YaziEvilOfTheYangZingActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (!activatedCardIdList.Contains(20001443) && CheckRemainInDeck(20001443) > 0 && CheckCalledbytheGrave(20001443) == 0 && SwordsoulOfMoYeEffectCheck())
			{
				base.AI.SelectCard(20001443);
				return true;
			}
			if (!activatedCardIdList.Contains(56495147) && CheckRemainInDeck(56495147) > 0 && CheckCalledbytheGrave(56495147) == 0)
			{
				base.AI.SelectCard(56495147);
				return true;
			}
			if (base.Bot.HasInMonstersZone(20001444))
			{
				foreach (int item in new List<int> { 93490856, 20001443, 56495147 })
				{
					if (CheckRemainInDeck(item) > 0)
					{
						base.AI.SelectCard(item);
						return true;
					}
				}
			}
			foreach (int item2 in new List<int> { 87052196, 23431858, 98159737, 93490856, 20001443, 56495147 })
			{
				if (CheckRemainInDeck(item2) > 0)
				{
					base.AI.SelectCard(item2);
					return true;
				}
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		bool flag = false;
		if (!activatedCardIdList.Contains(20001443) && CheckRemainInDeck(20001443) > 0 && CheckCalledbytheGrave(20001443) == 0 && SwordsoulOfMoYeEffectCheck())
		{
			flag = true;
		}
		if (!activatedCardIdList.Contains(56495147) && CheckRemainInDeck(56495147) > 0 && CheckCalledbytheGrave(56495147) == 0)
		{
			flag = true;
		}
		if (flag)
		{
			base.AI.SelectCard(base.Card);
		}
		else
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.HasSetcode(158)
				select card).ToList();
			list.Sort(CardContainer.CompareCardAttack);
			base.AI.SelectCard(list);
		}
		base.AI.SelectNextCard(GetNormalEnemyTargetList());
		return true;
	}

	public bool ShamanOfTheTenyiActivate()
	{
		if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
		{
			base.AI.SelectCard(GetNormalEnemyTargetList());
			return true;
		}
		if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
		{
			return false;
		}
		bool flag = base.Bot.HasInGraveyard(20001443) && CheckCalledbytheGrave(20001443) == 0 && !activatedCardIdList.Contains(20001443);
		bool flag2 = base.Bot.HasInHandOrInGraveyard(56495147) && CheckCalledbytheGrave(56495147) == 0 && !activatedCardIdList.Contains(56495147);
		bool useTaia = !base.Bot.HasInGraveyard(56495147) && base.Bot.HasInHand(56495147);
		List<ClientCard> list = (from card in base.Bot.GetGraveyardMonsters()
			where card.IsCanRevive() && card.HasRace(CardRace.Wyrm)
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		if (CheckAtAdvantage())
		{
			if (base.Duel.Turn > 1 && base.Enemy.GetMonsterCount() == 0)
			{
				int currentAttack = base.Util.GetTotalAttackingMonsterAttack(0);
				if (currentAttack < base.Enemy.LifePoints)
				{
					List<ClientCard> list2 = list.Where((ClientCard card) => card.Attack + currentAttack >= base.Enemy.LifePoints).ToList();
					if (list2.Count() > 0)
					{
						SelectDiscardForShamanOfTheTenyi(useTaia);
						base.AI.SelectNextCard(list2);
						return true;
					}
				}
			}
			if (flag)
			{
				SelectDiscardForShamanOfTheTenyi();
				base.AI.SelectNextCard(20001443);
				return true;
			}
			if (flag2)
			{
				SelectDiscardForShamanOfTheTenyi(useTaia);
				base.AI.SelectNextCard(56495147);
				return true;
			}
			SelectDiscardForShamanOfTheTenyi();
			base.AI.SelectNextCard(list);
			return true;
		}
		List<ClientCard> list3 = list.Where((ClientCard card) => card.HasType(CardType.Synchro)).ToList();
		if (list3.Count() > 0)
		{
			SelectDiscardForShamanOfTheTenyi();
			base.AI.SelectNextCard(list3);
			return true;
		}
		if (flag)
		{
			SelectDiscardForShamanOfTheTenyi();
			base.AI.SelectNextCard(20001443);
			return true;
		}
		if (flag2)
		{
			SelectDiscardForShamanOfTheTenyi(useTaia);
			base.AI.SelectNextCard(56495147);
			return true;
		}
		return false;
	}

	public void SelectDiscardForShamanOfTheTenyi(bool useTaia = false)
	{
		if (useTaia)
		{
			base.AI.SelectCard(56495147);
			return;
		}
		foreach (int item in new List<int> { 23431858, 87052196, 98159737 })
		{
			if (base.Bot.HasInHand(item))
			{
				base.AI.SelectCard(item);
				return;
			}
		}
		foreach (ClientCard hand in base.Bot.Hand)
		{
			if (base.Bot.Hand.Where((ClientCard card) => card.IsCode(hand.Id)).Count() > 1)
			{
				base.AI.SelectCard(hand);
				return;
			}
		}
		foreach (int item2 in new List<int>
		{
			65681983, 35261759, 87052196, 23431858, 98159737, 27204311, 93850690, 55273560, 10045474, 24224830,
			56495147, 20001443, 93490856, 14558127, 23434538, 97268402, 56465981, 14821890
		})
		{
			if (base.Bot.HasInHand(item2))
			{
				base.AI.SelectCard(item2);
				break;
			}
		}
	}

	public bool SpellSetCheck()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (new List<int> { 14821890 }.Contains(base.Card.Id) && base.Bot.HasInSpellZone(base.Card.Id))
		{
			return false;
		}
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay))
		{
			List<int> list = new List<int>();
			int num = 0;
			for (int i = 0; i < 5; i++)
			{
				if (base.Enemy.SpellZone[i] != null && base.Enemy.SpellZone[i].IsFaceup() && base.Bot.SpellZone[4 - i] == null)
				{
					list.Add(4 - i);
					num += (int)Math.Pow(2.0, 4 - i);
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

	protected override bool DefaultSetForDiabellze()
	{
		if (base.DefaultSetForDiabellze())
		{
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}
}
