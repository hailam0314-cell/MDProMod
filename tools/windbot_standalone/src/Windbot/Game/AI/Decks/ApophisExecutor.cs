using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Apophis", "AI_Apophis", "Normal")]
public class ApophisExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int LabradoriteDragon = 62514770;

		public const int AnubisTheLastJudge = 60411677;

		public const int PrimiteDragonEtherBeryl = 63198739;

		public const int TheManWithTheMark = 97522863;

		public const int Terraforming = 73628505;

		public const int PrimiteDrillbeam = 29095457;

		public const int PrimiteLordlyLode = 56506740;

		public const int TreasuresOfTheKings = 69299029;

		public const int DominusSpark = 6325660;

		public const int DominusImpulse = 40366667;

		public const int SongsOfTheDominators = 58053438;

		public const int DominusPurge = 97045737;

		public const int ApophisTheSwampDeity = 85888377;

		public const int ApophisTheSerpent = 95561146;

		public const int VerdictOfAnubis = 59576447;

		public const int SolemnReport = 78114463;

		public const int DivineSerpentApophis = 97800311;

		public const int SwordsoulSupremeSovereignChengying = 96633955;

		public const int BaronneDeFleur = 84815190;

		public const int SuperdreadnoughtRailCannonJuggernautLiebe = 26096328;

		public const int SuperdreadnoughtRailCannonFlyingLauncher = 38354018;

		public const int EvilswarmExcitonKnight = 46772449;

		public const int SPLittleKnight = 46772449;

		public const int SilhouhatteRabbit = 1528054;

		public const int LinkSpider = 98978921;
	}

	private List<int> NotToNegateIdList = new List<int> { 58699500, 20343502, 25451383, 19403423 };

	private List<int> solemnReportBanishIdList = new List<int> { 5318639, 63166095, 9726840, 5380979, 92714517, 6153210, 32548318, 30271097, 45171524, 81560239 };

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 97522863, 56506740, 69299029, 6325660, 10045474, 58053438, 97045737, 95561146 }
		},
		{
			2,
			new List<int> { 60411677, 63198739, 49238328, 40366667, 85888377, 78114463 }
		},
		{
			1,
			new List<int> { 62514770, 73628505, 29095457, 59576447 }
		}
	};

	private const int hintTimingMainEnd = 4;

	private const int hintToHand = 2097152;

	private int maxSummonCount = 1;

	private int summonCount = 1;

	private bool enemyActivateMonsterEffectFromHandGrave;

	private int dimensionShifterCount;

	private int songsOfTheDominatorsResolvedCount;

	private bool activatingLodeSpSummonEffect;

	private bool lodeSpSummonEffectResolved;

	private bool songsOfTheDominatorsActivatedFromHand;

	private List<ClientCard> currentNegateCardList = new List<ClientCard>();

	private List<int> activatedCardIdList = new List<int>();

	private List<ClientCard> enemyPlaceThisTurn = new List<ClientCard>();

	private List<ClientCard> summonThisTurn = new List<ClientCard>();

	private List<ClientCard> placedThisTurn = new List<ClientCard>();

	private List<ClientCard> activatedDivineSerpent1stList = new List<ClientCard>();

	private List<ClientCard> activatedDivineSerpent2ndList = new List<ClientCard>();

	private int anubisTheLastJudgeSpSummoningStep;

	private int SPLittleKnightRemoveStep;

	private int currentSummoningCount;

	public ApophisExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 78114463, SolemnReportBanishActivate);
		AddExecutor(ExecutorType.Activate, 85888377, ApophisTheSwampDeityActivate);
		AddExecutor(ExecutorType.Activate, 58053438, SongsOfTheDominatorsActivateFirst);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 58053438, DominusNegateTrapActivate);
		AddExecutor(ExecutorType.Activate, 97045737, DominusNegateTrapActivate);
		AddExecutor(ExecutorType.Activate, 40366667, DominusNegateTrapActivate);
		AddExecutor(ExecutorType.Activate, 84815190, BaronneDeFleurNegateEffect);
		AddExecutor(ExecutorType.Activate, 29095457, PrimiteDrillbeamActivate);
		AddExecutor(ExecutorType.Activate, 59576447, SpellNegateActivate);
		AddExecutor(ExecutorType.Activate, 78114463, SpellNegateActivate);
		AddExecutor(ExecutorType.Activate, 46772449, SPLittleKnightActivate);
		AddExecutor(ExecutorType.Activate, 6325660, DominusSparkActivate);
		AddExecutor(ExecutorType.Activate, 49238328, PotOfExtravaganceActivate);
		AddExecutor(ExecutorType.Repos, MonstetReposForImportantMonsters);
		AddExecutor(ExecutorType.Activate, 84815190, BaronneDeFleurActivate);
		AddExecutor(ExecutorType.Activate, 26096328, SuperdreadnoughtRailCannonJuggernautLiebeActivate);
		AddExecutor(ExecutorType.Activate, 38354018, SuperdreadnoughtRailCannonFlyingLauncherActivate);
		AddExecutor(ExecutorType.Activate, 46772449, base.DefaultEvilswarmExcitonKnightEffect);
		AddExecutor(ExecutorType.SpellSet, SpellSetFirst);
		AddExecutor(ExecutorType.Activate, 1528054, SilhouhatteRabbitActivate);
		AddExecutor(ExecutorType.Activate, 97800311, DivineSerpentApophisActivate);
		AddExecutor(ExecutorType.Activate, 96633955, SwordsoulSupremeSovereignChengyingActivate);
		AddExecutor(ExecutorType.Activate, 97522863, TheManWithTheMarkActivate);
		AddExecutor(ExecutorType.SpSummon, 46772449, EvilswarmExcitonKnightSummon);
		AddExecutor(ExecutorType.SpSummon, 60411677, AnubisTheLastJudgeSpSummon);
		AddExecutor(ExecutorType.SpSummon, 26096328, SuperdreadnoughtRailCannonJuggernautLiebeSpSummon);
		AddExecutor(ExecutorType.SpSummon, 38354018, SuperdreadnoughtRailCannonFlyingLauncherSpSummon);
		AddExecutor(ExecutorType.SpSummon, Level10SynchroSummon);
		AddExecutor(ExecutorType.SpSummon, 97800311, DivineSerpentApophisSpSummon);
		AddExecutor(ExecutorType.SpSummon, 98978921, LinkSpiderSummon);
		AddExecutor(ExecutorType.SpSummon, 46772449, SPLittleKnightSummon);
		AddExecutor(ExecutorType.SpSummon, 1528054, SilhouhatteRabbitSummon);
		AddExecutor(ExecutorType.Activate, 95561146, ApophisTheSerpentActivate);
		AddExecutor(ExecutorType.Activate, 73628505, TerraformingActivate);
		AddExecutor(ExecutorType.Activate, 69299029, TreasuresOfTheKingsActivate);
		AddExecutor(ExecutorType.Activate, 56506740, PrimiteLordlyLodeActivate);
		AddExecutor(ExecutorType.Summon, Level4MonsterSummon);
		AddExecutor(ExecutorType.Activate, 60411677, AnubisTheLastJudgeActivate);
		AddExecutor(ExecutorType.Activate, 63198739, PrimiteDragonEtherBerylActivate);
		AddExecutor(ExecutorType.Activate, 56506740, PrimiteLordlyLodeSpSummon);
		AddExecutor(ExecutorType.Activate, 85888377, ApophisTheSwampDeityActivateForAttack);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
		AddExecutor(ExecutorType.SpellSet, SpellSet);
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
			if ((selfType & CardType.Trap) > (CardType)0 && card.IsShouldNotBeSpellTrapTarget() && !card.IsDisabled())
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

	public bool CheckWhetherNegated(bool disablecheck = true, bool toFieldCheck = false, CardType type = (CardType)0, bool ignore41 = false)
	{
		bool flag = type == (CardType)0 && base.Card.IsMonster();
		flag = flag || (type & CardType.Monster) != 0;
		bool num = (type == (CardType)0 && (base.Card.IsSpell() || base.Card.IsTrap())) || (type & (CardType)6) != 0;
		bool isCounter = (type & CardType.Counter) != 0;
		if (num && toFieldCheck)
		{
			if (CheckSpellWillBeNegate(isCounter))
			{
				return true;
			}
			if (DefaultCheckWhetherSpellActivateWillBeNegated(base.Card))
			{
				return true;
			}
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return true;
		}
		if (flag && (toFieldCheck || base.Card.Location == CardLocation.MonsterZone))
		{
			if (((toFieldCheck && (type & CardType.Link) != 0) || base.Card.IsDefense()) && (base.Enemy.MonsterZone.Any((ClientCard card) => CheckNumber41(card, ignore41)) || base.Bot.MonsterZone.Any((ClientCard card) => CheckNumber41(card, ignore41))))
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
			if ((base.Card.Location == CardLocation.MonsterZone || base.Card.Location == CardLocation.SpellZone) && base.Card.IsDisabled())
			{
				return base.Card.IsFaceup();
			}
			return false;
		}
		return false;
	}

	public bool CheckNumber41(ClientCard card, bool ignoreSelf41 = false)
	{
		if (card != null && card.IsFaceup() && card.IsCode(90590303) && card.IsDefense() && !card.IsDisabled())
		{
			if (ignoreSelf41)
			{
				return card.Controller == 0;
			}
			return true;
		}
		return false;
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
			if (infiniteImpermanenceNegatedColumns.Contains(item))
			{
				return true;
			}
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
		return CheckCardShouldNegate(lastChainCard);
	}

	public bool CheckCardShouldNegate(ClientCard card)
	{
		if (card == null)
		{
			return false;
		}
		if (card.IsMonster() && card.HasSetcode(74) && base.Duel.Phase == DuelPhase.Standby)
		{
			return false;
		}
		if (NotToNegateIdList.Contains(card.Id))
		{
			return false;
		}
		if (card.HasSetcode(286) && card.Location == CardLocation.Hand)
		{
			return false;
		}
		if (card.IsMonster() && card.Location == CardLocation.MonsterZone && card.HasPosition(CardPosition.Defence) && (base.Enemy.MonsterZone.Any((ClientCard c) => CheckNumber41(c)) || base.Bot.MonsterZone.Any((ClientCard c) => CheckNumber41(c))))
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(card))
		{
			return false;
		}
		if (card.Location == CardLocation.SpellZone)
		{
			int num = card.Sequence;
			if (card.Controller == 1)
			{
				num = 4 - num;
			}
			if (infiniteImpermanenceNegatedColumns.Contains(num))
			{
				return false;
			}
		}
		if (card.IsCode(84192580, 42141493, 87126721, 23434538))
		{
			return false;
		}
		if (card.IsDisabled())
		{
			return false;
		}
		return true;
	}

	public bool CheckCardShouldNegate(ChainInfo chainInfo)
	{
		if (chainInfo == null)
		{
			return false;
		}
		ClientCard relatedCard = chainInfo.RelatedCard;
		if (relatedCard == null)
		{
			return false;
		}
		if (relatedCard.IsMonster() && relatedCard.HasSetcode(74) && base.Duel.Phase == DuelPhase.Standby)
		{
			return false;
		}
		if (NotToNegateIdList.Contains(relatedCard.Id))
		{
			return false;
		}
		if (relatedCard.HasSetcode(286) && relatedCard.Location == CardLocation.Hand)
		{
			return false;
		}
		if (relatedCard.IsMonster() && chainInfo.HasLocation(CardLocation.MonsterZone) && chainInfo.HasPosition(CardPosition.Defence) && (base.Enemy.MonsterZone.Any((ClientCard c) => CheckNumber41(c)) || base.Bot.MonsterZone.Any((ClientCard c) => CheckNumber41(c))))
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(relatedCard))
		{
			return false;
		}
		if (base.Duel.Player == 1 && relatedCard.IsCode(84192580, 42141493, 87126721))
		{
			return false;
		}
		if (relatedCard.IsDisabled())
		{
			return false;
		}
		return true;
	}

	public bool CheckAtAdvantage()
	{
		if (GetProblematicEnemyMonster() == null && (base.Duel.Player == 0 || base.Bot.GetMonsterCount() > 0))
		{
			return true;
		}
		return false;
	}

	public bool CheckShouldNoMoreSpSummon()
	{
		if (CheckAtAdvantage() && enemyResolvedEffectIdList.Contains(23434538) && DefaultCheckWhetherEnemyCanDraw() && (base.Duel.Turn == 1 || base.Duel.Phase >= DuelPhase.Main2))
		{
			return true;
		}
		return false;
	}

	public bool CheckShouldNoMoreSpSummon(CardLocation loc)
	{
		if (CheckShouldNoMoreSpSummon())
		{
			return true;
		}
		if (!DefaultCheckWhetherEnemyCanDraw() || (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2))
		{
			return false;
		}
		if (enemyResolvedEffectIdList.Contains(84192580) && (loc & CardLocation.Hand) != 0)
		{
			return true;
		}
		if (enemyResolvedEffectIdList.Contains(42141493) && (loc & (CardLocation)65) != 0)
		{
			return true;
		}
		if (enemyResolvedEffectIdList.Contains(87126721) && (loc & (CardLocation)48) != 0)
		{
			return true;
		}
		return false;
	}

	public bool CheckWhetherCanSummon()
	{
		if (base.Duel.Player == 0 && base.Duel.Phase < DuelPhase.End)
		{
			return summonCount > 0;
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

	public bool CheckWhetherCanActivateMonsterEffect(CardAttribute attribute)
	{
		if (base.Bot.HintDescriptions.Contains(base.Util.GetStringId(6325660, 3)) && (attribute & (CardAttribute)15) != 0)
		{
			return false;
		}
		if (base.Bot.HintDescriptions.Contains(base.Util.GetStringId(40366667, 2)) && (attribute & (CardAttribute)25) != 0)
		{
			return false;
		}
		if (base.Bot.HintDescriptions.Contains(base.Util.GetStringId(97045737, 2)) && (attribute & (CardAttribute)38) != 0)
		{
			return false;
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

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false, bool ignoreCurrentDestroy = true, CardType selfType = (CardType)0)
	{
		ClientCard clientCard = (from card in base.Enemy.GetMonsters()
			where card?.Data != null && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard != null)
		{
			return clientCard;
		}
		ClientCard clientCard2 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard2 != null)
		{
			return clientCard2;
		}
		ClientCard clientCard3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard3 != null)
		{
			return clientCard3;
		}
		ClientCard clientCard4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && card.EquipCards.Count > 0 && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard4 != null)
		{
			return clientCard4;
		}
		ClientCard clientCard5 = (from card in base.Enemy.MonsterZone
			where card != null && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && (card.HasType((CardType)8396992) || (card.HasType(CardType.Link) && card.LinkCount >= 2)) && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard5 != null)
		{
			return clientCard5;
		}
		ClientCard clientCard6 = base.Enemy.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(68468459) && !c.IsDisabled() && !currentNegateCardList.Contains(c) && base.Duel.CurrentChain.Contains(c));
		if (clientCard6 != null)
		{
			return clientCard6;
		}
		if (attack >= 0)
		{
			if (attack == 0)
			{
				attack = GetBotBestPower();
			}
			ClientCard clientCard7 = (from card in base.Enemy.MonsterZone
				where card != null && card.GetDefensePower() >= attack && card.GetDefensePower() > 0 && card.IsAttack() && CheckCanBeTargeted(card, canBeTarget, selfType) && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card))
				orderby card.Attack descending
				select card).FirstOrDefault();
			if (clientCard7 != null)
			{
				return clientCard7;
			}
		}
		return null;
	}

	public bool CheckShouldNotIgnore(ClientCard cards, bool ignore = false)
	{
		if (ignore)
		{
			if (!currentNegateCardList.Contains(cards))
			{
				return !currentNegateCardList.Contains(cards);
			}
			return false;
		}
		return true;
	}

	public List<ClientCard> GetProblematicEnemyCardList(bool canBeTarget = false, bool ignoreSpells = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> resultList = new List<ClientCard>();
		List<ClientCard> list = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !currentNegateCardList.Contains(card) && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list.Count > 0)
		{
			resultList.AddRange(list);
		}
		List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && !currentNegateCardList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && CheckCanBeTargeted(c, canBeTarget, selfType)).ToList();
		if (list2.Count > 0)
		{
			resultList.AddRange(ShuffleList(list2));
		}
		List<ClientCard> list3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentNegateCardList.Contains(card) && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list3.Count > 0 && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			resultList.AddRange(list3);
		}
		List<ClientCard> list4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentNegateCardList.Contains(card) && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list4.Count > 0)
		{
			resultList.AddRange(list4);
		}
		List<ClientCard> list5 = (from card in base.Enemy.GetMonsters()
			where !currentNegateCardList.Contains(card)
			orderby card.Attack descending
			select card).ToList();
		if (list5.Count > 0)
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
			where c.IsFaceup() && !currentNegateCardList.Contains(c) && c.HasType((CardType)17694720) && CheckCanBeTargeted(c, canBeTarget, selfType)
			select c).ToList();
		if (list6.Count > 0 && !ignoreSpells)
		{
			resultList.AddRange(ShuffleList(list6));
		}
		return resultList;
	}

	public List<ClientCard> GetNormalEnemyTargetList(bool canBeTarget = true, bool ignoreCurrentDestroy = true, CardType selfType = (CardType)0, bool forNegate = false)
	{
		List<ClientCard> targetList = GetProblematicEnemyCardList(canBeTarget, ignoreSpells: false, selfType);
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup() && !targetList.Contains(card) && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && (!forNegate || (!card.IsDisabled() && card.HasType(CardType.Effect)))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		targetList.AddRange(list);
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && enemyPlaceThisTurn.Contains(card) && card.IsFacedown()
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && !enemyPlaceThisTurn.Contains(card) && card.IsFacedown()
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetMonsters()
			where card.IsFacedown() && (!ignoreCurrentDestroy || !currentNegateCardList.Contains(card)) && (!forNegate || (!card.IsDisabled() && card.HasType(CardType.Effect) && card.IsFaceup()))
			select card).ToList()));
		return targetList;
	}

	public List<ClientCard> GetNormalEnemySpellTargetList(bool canBeTarget = true, bool ignoreCurrentDestroy = true, CardType selfType = (CardType)0, bool forNegate = false)
	{
		return (from card in GetNormalEnemyTargetList(canBeTarget, ignoreCurrentDestroy, selfType, forNegate)
			where card.HasType((CardType)6) || card.Location == CardLocation.SpellZone
			select card).ToList();
	}

	public List<ClientCard> GetMonsterListForTargetNegate(bool canBeTarget = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> list = new List<ClientCard>();
		if (CheckWhetherNegated())
		{
			return list;
		}
		ClientCard clientCard = base.Enemy.MonsterZone.FirstOrDefault((ClientCard card) => card?.Data != null && card.IsMonsterShouldBeDisabledBeforeItUseEffect() && card.IsFaceup() && !card.IsShouldNotBeTarget() && CheckCanBeTargeted(card, canBeTarget, selfType) && !currentNegateCardList.Contains(card));
		if (clientCard != null)
		{
			list.Add(clientCard);
		}
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Location != CardLocation.MonsterZone || item.Controller != 1 || item.IsDisabled() || !CheckCanBeTargeted(item, canBeTarget, selfType) || currentNegateCardList.Contains(item))
			{
				continue;
			}
			if (item.HasPosition(CardPosition.Defence))
			{
				base.Bot.MonsterZone.Any((ClientCard c) => CheckNumber41(c));
				base.Enemy.MonsterZone.Any((ClientCard c) => CheckNumber41(c));
			}
			list.Add(item);
		}
		return list;
	}

	public int GetSpecialSummonDrawCount(CardLocation loc)
	{
		int num = 0;
		if (!DefaultCheckWhetherEnemyCanDraw())
		{
			return 0;
		}
		if (enemyResolvedEffectIdList.Contains(23434538))
		{
			num++;
		}
		if ((loc & CardLocation.Hand) != 0)
		{
			num += enemyResolvedEffectIdList.Count((int id) => id == 84192580);
		}
		if ((loc & (CardLocation)65) != 0)
		{
			num += enemyResolvedEffectIdList.Count((int id) => id == 42141493);
		}
		if ((loc & (CardLocation)48) != 0)
		{
			num += enemyResolvedEffectIdList.Count((int id) => id == 87126721);
		}
		return num;
	}

	public int GetBotBestPower(bool onlyATK = false)
	{
		return (from card in base.Bot.MonsterZone.GetMonsters()
			where !onlyATK || (!summonThisTurn.Contains(card) && base.Duel.Player == 0) || card.IsAttack()
			select card).Max((Func<ClientCard, int?>)((ClientCard card) => card.GetDefensePower())) ?? (-1);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo == null)
		{
			if (anubisTheLastJudgeSpSummoningStep > 0)
			{
				foreach (int cardId in new List<int> { 59576447, 85888377, 58053438, 6325660, 40366667, 95561146, 97045737, 10045474, 78114463 })
				{
					ClientCard clientCard = cards.FirstOrDefault((ClientCard c) => c.IsCode(cardId));
					if (clientCard != null)
					{
						if (anubisTheLastJudgeSpSummoningStep == 1)
						{
							anubisTheLastJudgeSpSummoningStep = 2;
						}
						else
						{
							anubisTheLastJudgeSpSummoningStep = 0;
						}
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard }, cards, min, max);
					}
				}
			}
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 0)
			{
				switch (lastChainCard.Id)
				{
				case 60411677:
					foreach (ClientCard normalEnemyTarget in GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster))
					{
						if (cards.Contains(normalEnemyTarget))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget }, cards, min, max);
						}
					}
					foreach (ClientCard card in cards)
					{
						if (enemyPlaceThisTurn.Contains(card))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { card }, cards, min, max);
						}
					}
					break;
				case 29095457:
					foreach (ClientCard item in base.Duel.CurrentChain)
					{
						if (item.Controller == 1 && item.IsOnField() && item.IsFaceup() && !item.IsDisabled() && !currentNegateCardList.Contains(item) && CheckCanBeTargeted(item, canBeTarget: true, CardType.Spell) && CheckCardShouldNegate(item) && cards.Contains(item))
						{
							currentNegateCardList.Add(item);
							return base.Util.CheckSelectCount(new List<ClientCard> { item }, cards, min, max);
						}
					}
					foreach (ClientCard normalEnemyTarget2 in GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster))
					{
						if (cards.Contains(normalEnemyTarget2))
						{
							currentNegateCardList.Add(normalEnemyTarget2);
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget2 }, cards, min, max);
						}
					}
					foreach (ClientCard item2 in ShuffleList(cards.Where((ClientCard c) => c.Controller == 1).ToList()))
					{
						if (cards.Contains(item2))
						{
							currentNegateCardList.Add(item2);
							return base.Util.CheckSelectCount(new List<ClientCard> { item2 }, cards, min, max);
						}
					}
					break;
				case 6325660:
					foreach (ClientCard normalEnemyTarget3 in GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: false))
					{
						if (cards.Contains(normalEnemyTarget3))
						{
							currentNegateCardList.Add(normalEnemyTarget3);
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget3 }, cards, min, max);
						}
					}
					break;
				case 10045474:
				{
					int sequence = lastChainCard.Sequence;
					foreach (ClientCard item3 in GetMonsterListForTargetNegate(canBeTarget: true, CardType.Trap))
					{
						if (!cards.Contains(item3))
						{
							continue;
						}
						currentNegateCardList.Add(item3);
						if (sequence >= 0)
						{
							ClientCard clientCard3 = base.Enemy.SpellZone[sequence];
							if (clientCard3 != null && clientCard3.IsFaceup())
							{
								currentNegateCardList.Add(clientCard3);
							}
						}
						return base.Util.CheckSelectCount(new List<ClientCard> { item3 }, cards, min, max);
					}
					foreach (ClientCard problematicEnemyCard in GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap))
					{
						if (!cards.Contains(problematicEnemyCard))
						{
							continue;
						}
						currentNegateCardList.Add(problematicEnemyCard);
						if (sequence >= 0)
						{
							ClientCard clientCard4 = base.Enemy.SpellZone[sequence];
							if (clientCard4 != null && clientCard4.IsFaceup())
							{
								currentNegateCardList.Add(clientCard4);
							}
						}
						return base.Util.CheckSelectCount(new List<ClientCard> { problematicEnemyCard }, cards, min, max);
					}
					break;
				}
				case 97800311:
					if (hint == 502)
					{
						foreach (ClientCard normalEnemyTarget4 in GetNormalEnemyTargetList())
						{
							if (cards.Contains(normalEnemyTarget4))
							{
								currentNegateCardList.Add(normalEnemyTarget4);
								return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget4 }, cards, min, max);
							}
						}
					}
					if (hint == 510)
					{
						int targetId = 85888377;
						bool flag = false;
						if (base.Bot.GetSpellCountWithoutField() <= 3 && base.Bot.HasInGraveyard(95561146))
						{
							flag = true;
						}
						if (base.Bot.GetSpells().Any((ClientCard c) => c.HasType(CardType.Continuous)) || base.Bot.GetMonsters().Any((ClientCard c) => c.HasType(CardType.Continuous)))
						{
							flag = true;
						}
						if (!flag)
						{
							targetId = 95561146;
						}
						ClientCard clientCard2 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId));
						if (clientCard2 != null)
						{
							currentNegateCardList.Add(clientCard2);
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard2 }, cards, min, max);
						}
						return base.Util.CheckSelectCount(cards, cards, min, max);
					}
					break;
				case 84815190:
					if (hint != 502)
					{
						break;
					}
					foreach (ClientCard normalEnemyTarget5 in GetNormalEnemyTargetList())
					{
						if (cards.Contains(normalEnemyTarget5))
						{
							currentNegateCardList.Add(normalEnemyTarget5);
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget5 }, cards, min, max);
						}
					}
					break;
				case 38354018:
				{
					List<ClientCard> normalEnemySpellTargetList = GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster);
					switch (hint)
					{
					case 519:
						return base.Util.CheckSelectCount(cards, cards, min, Math.Min(normalEnemySpellTargetList.Count, max));
					case 502:
					{
						List<ClientCard> list2 = new List<ClientCard>();
						foreach (ClientCard item4 in normalEnemySpellTargetList)
						{
							if (cards.Contains(item4))
							{
								list2.Add(item4);
								if (list2.Count >= max)
								{
									currentNegateCardList.AddRange(list2);
									return base.Util.CheckSelectCount(list2, cards, min, Math.Min(normalEnemySpellTargetList.Count, max));
								}
							}
						}
						break;
					}
					}
					break;
				}
				case 46772449:
				{
					if (base.Duel.CurrentChainInfo.Count <= 0)
					{
						break;
					}
					ChainInfo chainInfo = base.Duel.CurrentChainInfo[base.Duel.CurrentChainInfo.Count - 1];
					if (chainInfo.ActivateDescription == -1 || chainInfo.ActivateDescription == base.Util.GetStringId(46772449, 0))
					{
						List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster);
						problematicEnemyCardList.AddRange(GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster));
						problematicEnemyCardList.AddRange(from card in base.Enemy.Graveyard
							where card.HasType(CardType.Monster)
							orderby card.Attack descending
							select card);
						problematicEnemyCardList.AddRange(base.Enemy.Graveyard.Where((ClientCard card) => !card.HasType(CardType.Monster)));
						foreach (ClientCard item5 in problematicEnemyCardList)
						{
							if (cards.Contains(item5))
							{
								currentNegateCardList.Add(item5);
								return base.Util.CheckSelectCount(new List<ClientCard> { item5 }, cards, min, max);
							}
						}
					}
					else
					{
						if (chainInfo.ActivateDescription != base.Util.GetStringId(46772449, 1))
						{
							break;
						}
						switch (SPLittleKnightRemoveStep)
						{
						case 1:
							foreach (ClientCard monster in base.Bot.GetMonsters())
							{
								if (!monster.HasType(CardType.Continuous) && base.Duel.ChainTargets.Contains(monster) && cards.Contains(monster))
								{
									SPLittleKnightRemoveStep = 2;
									return base.Util.CheckSelectCount(new List<ClientCard> { monster }, cards, min, max);
								}
							}
							if (base.Duel.Player != 1)
							{
								break;
							}
							foreach (ClientCard item6 in from c in base.Bot.GetMonsters()
								where c.IsAttack() && !c.HasType(CardType.Continuous) && !c.IsCode(new List<int> { 95561146, 85888377 })
								orderby c.Attack
								select c)
							{
								if (base.Util.IsOneEnemyBetterThanValue(item6.Attack, onlyATK: true) && cards.Contains(item6))
								{
									SPLittleKnightRemoveStep = 2;
									return base.Util.CheckSelectCount(new List<ClientCard> { item6 }, cards, min, max);
								}
							}
							break;
						case 2:
						{
							if (base.Enemy.GetMonsterCount() > 0)
							{
								List<ClientCard> problematicEnemyCardList2 = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: true, CardType.Monster);
								if (problematicEnemyCardList2.Count() > 0)
								{
									foreach (ClientCard item7 in problematicEnemyCardList2)
									{
										if (cards.Contains(item7))
										{
											currentNegateCardList.Add(item7);
											SPLittleKnightRemoveStep = 0;
											return base.Util.CheckSelectCount(new List<ClientCard> { item7 }, cards, min, max);
										}
									}
								}
							}
							foreach (ClientCard chainTarget in base.Duel.ChainTargets)
							{
								if (!chainTarget.IsCode(new List<int> { 95561146, 85888377 }) && cards.Contains(chainTarget))
								{
									SPLittleKnightRemoveStep = 0;
									return base.Util.CheckSelectCount(new List<ClientCard> { chainTarget }, cards, min, max);
								}
							}
							if (base.Enemy.GetMonsterCount() != 0)
							{
								break;
							}
							List<ClientCard> list = (from c in base.Bot.GetMonsters()
								where !c.HasType(CardType.Continuous)
								select c).ToList();
							list.Sort(CompareUsableAttack);
							foreach (ClientCard item8 in list)
							{
								if (cards.Contains(item8))
								{
									SPLittleKnightRemoveStep = 0;
									return base.Util.CheckSelectCount(new List<ClientCard> { item8 }, cards, min, max);
								}
							}
							break;
						}
						default:
							SPLittleKnightRemoveStep = 0;
							break;
						}
					}
					break;
				}
				case 1528054:
					if (hint != 502)
					{
						break;
					}
					foreach (ClientCard normalEnemySpellTarget in GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster))
					{
						if (cards.Contains(normalEnemySpellTarget))
						{
							currentNegateCardList.Add(normalEnemySpellTarget);
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemySpellTarget }, cards, min, max);
						}
					}
					break;
				}
			}
		}
		if (currentSolvingChainInfo != null)
		{
			if (currentSolvingChainInfo.ActivateController == 0)
			{
				switch (currentSolvingChainInfo.ActivateId)
				{
				case 63198739:
					if (hint == 510)
					{
						int targetId6 = 56506740;
						if (activatedCardIdList.Contains(56506740) || !DefaultCheckWhetherBotCanSearch() || base.Bot.HasInSpellZone(56506740))
						{
							targetId6 = 29095457;
						}
						ClientCard clientCard13 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId6));
						if (clientCard13 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard13 }, cards, min, max);
						}
					}
					break;
				case 97522863:
					if (hint == 574)
					{
						int targetId5 = 69299029;
						if (activatedCardIdList.Contains(69299029) || base.Bot.HasInHandOrInSpellZone(69299029))
						{
							targetId5 = 59576447;
						}
						ClientCard clientCard11 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId5));
						if (clientCard11 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard11 }, cards, min, max);
						}
					}
					break;
				case 69299029:
				{
					List<int> list6 = new List<int>();
					switch (hint)
					{
					case 510:
						list6.AddRange(new List<int> { 95561146, 85888377 });
						break;
					case 506:
						if (songsOfTheDominatorsResolvedCount > 0)
						{
							list6.AddRange(new List<int> { 97522863, 60411677 });
						}
						else
						{
							list6.AddRange(new List<int> { 60411677, 97522863 });
						}
						break;
					}
					foreach (int targetId4 in list6)
					{
						ClientCard clientCard10 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId4));
						if (clientCard10 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard10 }, cards, min, max);
						}
					}
					break;
				}
				case 56506740:
				{
					if (hint != 506)
					{
						break;
					}
					List<int> list4 = new List<int> { 63198739, 29095457 };
					if ((summonCount == 0 || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth)) && (base.Bot.Hand.Any((ClientCard c) => c.IsCode(63198739, 56506740, 62514770)) | base.Bot.HasInMonstersZone(new List<int> { 95561146, 85888377, 62514770 }, notDisabled: false, hasXyzMaterial: false, faceUp: true) | base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(95561146, 85888377) && (c.IsFacedown() || base.Duel.CurrentChain.Contains(c)))))
					{
						list4.Insert(0, 29095457);
					}
					foreach (int targetId3 in list4)
					{
						ClientCard clientCard6 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId3));
						if (clientCard6 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
						}
					}
					break;
				}
				case 58053438:
				{
					List<int> list5 = new List<int> { 97045737, 40366667, 6325660 };
					foreach (int checkId in list5)
					{
						if (!activatedCardIdList.Contains(checkId) && !base.Bot.HasInHandOrInSpellZone(checkId) && !base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(checkId)))
						{
							ClientCard clientCard7 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId));
							if (clientCard7 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard7 }, cards, min, max);
							}
						}
					}
					foreach (int checkId2 in list5)
					{
						if (!base.Bot.HasInHandOrInSpellZone(checkId2))
						{
							ClientCard clientCard8 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId2));
							if (clientCard8 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard8 }, cards, min, max);
							}
						}
					}
					foreach (int checkId3 in list5)
					{
						ClientCard clientCard9 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId3));
						if (clientCard9 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard9 }, cards, min, max);
						}
					}
					break;
				}
				case 85888377:
				{
					List<ClientCard> list7 = new List<ClientCard>();
					for (int num = base.Duel.CurrentChain.Count - 1; num >= 0; num--)
					{
						ClientCard clientCard12 = base.Duel.CurrentChain[num];
						if (clientCard12 != null && clientCard12.IsFaceup() && cards.Contains(clientCard12))
						{
							list7.Add(clientCard12);
						}
					}
					foreach (ClientCard card2 in cards)
					{
						if (card2 != null && card2.IsFaceup() && !list7.Contains(card2))
						{
							list7.Add(card2);
						}
					}
					return base.Util.CheckSelectCount(list7, cards, min, max);
				}
				case 96633955:
				{
					foreach (ClientCard normalEnemyTarget6 in GetNormalEnemyTargetList(canBeTarget: false))
					{
						if (cards.Contains(normalEnemyTarget6))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { normalEnemyTarget6 }, cards, min, max);
						}
					}
					List<ClientCard> list3 = base.Duel.CurrentChain.Where((ClientCard c) => c != null && c.Controller == 1 && c.Location == CardLocation.Grave).ToList();
					list3.AddRange(base.Duel.ChainTargets.Where((ClientCard c) => c != null && c.Controller == 1 && c.Location == CardLocation.Grave).ToList());
					list3.AddRange((from c in base.Enemy.Graveyard
						where c.IsMonster()
						orderby c.Attack descending
						select c).ToList());
					return base.Util.CheckSelectCount(list3, cards, min, max);
				}
				case 1528054:
					foreach (int targetId2 in new List<int> { 95561146, 85888377 })
					{
						ClientCard clientCard5 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId2));
						if (clientCard5 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard5 }, cards, min, max);
						}
					}
					break;
				}
			}
			if (currentSolvingChainInfo.ActivateController == 1 && currentSolvingChainInfo.ActivateId == 15693423)
			{
				Logger.DebugWriteLine("=== Evenly Matched activated.");
				List<ClientCard> banishList = new List<ClientCard>();
				List<ClientCard> source = (from card in base.Bot.GetMonsters()
					where !card.HasType(CardType.Token)
					select card).ToList();
				List<ClientCard> collection = source.Where((ClientCard card) => card.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list8 = source.Where((ClientCard card) => !banishList.Contains(card) && CheckRemainInDeck(card.Id) > 0).ToList();
				list8.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list8);
				List<ClientCard> list9 = (from c in base.Bot.GetSpells()
					where c.IsFaceup()
					select c).ToList();
				banishList.AddRange(ShuffleList(list9));
				List<ClientCard> list10 = source.Where((ClientCard card) => !banishList.Contains(card)).ToList();
				list10.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list10);
				List<ClientCard> list11 = (from c in base.Bot.GetSpells()
					where c.IsFacedown()
					select c).ToList();
				banishList.AddRange(ShuffleList(list11));
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override int OnSelectOption(IList<int> options)
	{
		if (options.Contains(base.Util.GetStringId(49238328, 1)))
		{
			return options.IndexOf(base.Util.GetStringId(49238328, 1));
		}
		if (options.Contains(base.Util.GetStringId(78114463, 1)) && options.Contains(base.Util.GetStringId(78114463, 2)))
		{
			int result = options.IndexOf(base.Util.GetStringId(78114463, 1));
			int result2 = options.IndexOf(base.Util.GetStringId(78114463, 2));
			bool flag = false;
			if (base.Duel.CurrentChain.Count >= 2)
			{
				ClientCard secondLastChainCard = base.Duel.CurrentChain[base.Duel.CurrentChain.Count - 2];
				if (secondLastChainCard != null)
				{
					switch (secondLastChainCard.Id)
					{
					case 6325660:
					{
						bool flag3 = !activatedCardIdList.Contains(6325660) && !base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(6325660));
						if (flag3)
						{
							flag3 &= (base.Bot.HasInHand(6325660) && enemyActivateMonsterEffectFromHandGrave) || base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(6325660) && !placedThisTurn.Contains(c));
						}
						flag = flag || flag3;
						break;
					}
					case 40366667:
					case 97045737:
					{
						bool flag2 = !activatedCardIdList.Contains(secondLastChainCard.Id) && !base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(secondLastChainCard.Id));
						if (flag2)
						{
							flag2 &= base.Bot.HasInHand(secondLastChainCard.Id) || base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(secondLastChainCard.Id) && !placedThisTurn.Contains(c));
						}
						flag = flag || flag2;
						break;
					}
					case 58053438:
					{
						bool flag4 = !activatedCardIdList.Contains(secondLastChainCard.Id) && !base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(secondLastChainCard.Id));
						if (flag4)
						{
							flag4 &= (base.Bot.HasInHand(secondLastChainCard.Id) && base.Bot.Graveyard.Count((ClientCard c) => c.IsMonster()) == 0) || base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(secondLastChainCard.Id) && !placedThisTurn.Contains(c));
						}
						flag = flag || flag4;
						break;
					}
					case 10045474:
					case 85888377:
						flag |= base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(secondLastChainCard.Id) && !placedThisTurn.Contains(c));
						break;
					case 95561146:
						flag |= base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(secondLastChainCard.Id) && !placedThisTurn.Contains(c)) && !activatedCardIdList.Contains(secondLastChainCard.Id);
						break;
					}
					if (flag)
					{
						return result2;
					}
					if (secondLastChainCard.IsCode(solemnReportBanishIdList))
					{
						return result2;
					}
				}
			}
			return result;
		}
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null && currentSolvingChainInfo.ActivateController == 0 && currentSolvingChainInfo.ActivateId == 97522863 && options.Contains(1190))
		{
			return options.IndexOf(1190);
		}
		return base.OnSelectOption(options);
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone)
		{
			List<int> list = ShuffleList(new List<int> { 5, 6 });
			list.AddRange(ShuffleList(new List<int> { 0, 2, 4 }));
			list.AddRange(ShuffleList(new List<int> { 1, 3 }));
			foreach (int item in list)
			{
				int num = (int)Math.Pow(2.0, item);
				if ((available & num) == 0 || base.Bot.MonsterZone[item] != null)
				{
					continue;
				}
				List<int> list2 = (from c in base.Bot.GetSpells()
					where c.IsCode(10045474)
					select c.Sequence).ToList();
				list2.AddRange(infiniteImpermanenceNegatedColumns);
				if ((cardId != 95561146 && cardId != 85888377) || !list2.Contains(item))
				{
					if (cardId == 95561146 || cardId == 85888377)
					{
						Logger.DebugWriteLine("Apophis select zone: " + item);
						Logger.DebugWriteLine("infiniteImpermanenceNegatedColumns: " + string.Join(", ", list2));
					}
					return num;
				}
			}
		}
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null && currentSolvingChainInfo.ActivateController == 0)
		{
			switch (currentSolvingChainInfo.ActivateId)
			{
			case 69299029:
				foreach (int item2 in ShuffleList(new List<int> { 0, 1, 2, 3, 4 }))
				{
					int num3 = (int)Math.Pow(2.0, item2);
					if ((available & num3) != 0 && base.Bot.MonsterZone[item2] == null)
					{
						return num3;
					}
				}
				break;
			case 95561146:
				foreach (int item3 in ShuffleList(new List<int> { 0, 1, 2, 3, 4 }))
				{
					int num2 = (int)Math.Pow(2.0, item3);
					if ((available & num2) != 0 && base.Bot.MonsterZone[item3] == null && !infiniteImpermanenceNegatedColumns.Contains(item3))
					{
						return num2;
					}
				}
				break;
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == base.Util.GetStringId(29095457, 0))
		{
			return false;
		}
		return base.OnSelectYesNo(desc);
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
			int num = namedCard.Attack;
			if (cardId == 26096328 && CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth))
			{
				num += 2000;
			}
			if (cardId == 96633955)
			{
				int num2 = base.Bot.Banished.Count() + base.Enemy.Banished.Count();
				num += num2 * 200;
			}
			int value = Math.Max(GetBotBestPower(onlyATK: true), num);
			if (base.Util.IsAllEnemyBetterThanValue(value, onlyATK: true))
			{
				return CardPosition.FaceUpDefence;
			}
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public override void OnNewTurn()
	{
		if (base.Duel.Turn <= 1)
		{
			dimensionShifterCount = 0;
			songsOfTheDominatorsResolvedCount = 0;
			maxSummonCount = 1;
		}
		summonCount = maxSummonCount;
		enemyActivateMonsterEffectFromHandGrave = false;
		anubisTheLastJudgeSpSummoningStep = 0;
		SPLittleKnightRemoveStep = 0;
		activatingLodeSpSummonEffect = false;
		lodeSpSummonEffectResolved = false;
		songsOfTheDominatorsActivatedFromHand = false;
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		if (songsOfTheDominatorsResolvedCount > 0)
		{
			songsOfTheDominatorsResolvedCount--;
		}
		currentNegateCardList.Clear();
		activatedCardIdList.Clear();
		enemyPlaceThisTurn.Clear();
		summonThisTurn.Clear();
		placedThisTurn.Clear();
		activatedDivineSerpent1stList.Clear();
		activatedDivineSerpent2ndList.Clear();
		currentSummoningCount = 0;
		base.OnNewTurn();
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (player == 1 && card != null && card.IsMonster() && (card.Location == CardLocation.Hand || card.Location == CardLocation.Grave))
		{
			enemyActivateMonsterEffectFromHandGrave = true;
		}
		base.OnChaining(player, card);
	}

	public override void OnChainSolved(int chainIndex)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null)
		{
			if (currentSolvingChainInfo.ActivateController == 0)
			{
				switch (currentSolvingChainInfo.ActivateId)
				{
				case 6325660:
				case 40366667:
				case 58053438:
				case 97045737:
					activatedCardIdList.Add(currentSolvingChainInfo.ActivateId);
					break;
				case 69299029:
					if (currentSolvingChainInfo.ActivateDescription != base.Util.GetStringId(69299029, 0))
					{
						activatedCardIdList.Add(currentSolvingChainInfo.ActivateId);
					}
					break;
				}
			}
			if (!base.Duel.IsCurrentSolvingChainNegated())
			{
				if (currentSolvingChainInfo.ActivateController == 0)
				{
					switch (currentSolvingChainInfo.ActivateId)
					{
					case 56506740:
						if (activatingLodeSpSummonEffect)
						{
							lodeSpSummonEffectResolved = true;
						}
						break;
					case 58053438:
						if (songsOfTheDominatorsActivatedFromHand)
						{
							songsOfTheDominatorsResolvedCount = 2;
						}
						break;
					}
				}
				if (currentSolvingChainInfo.IsActivateCode(91800273))
				{
					dimensionShifterCount = 2;
				}
			}
		}
		base.OnChainSolved(chainIndex);
	}

	public override void OnChainEnd()
	{
		currentSummoningCount = 0;
		currentNegateCardList.Clear();
		activatingLodeSpSummonEffect = false;
		songsOfTheDominatorsActivatedFromHand = false;
		for (int num = enemyPlaceThisTurn.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard = enemyPlaceThisTurn[num];
			if (clientCard == null || !clientCard.IsOnField())
			{
				enemyPlaceThisTurn.RemoveAt(num);
			}
		}
		base.OnChainEnd();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (card != null)
		{
			if (currentControler == 1 && (currentLocation == 4 || currentLocation == 8))
			{
				enemyPlaceThisTurn.Add(card);
			}
			if (previousControler == 0 && previousLocation == 4 && currentLocation != 4)
			{
				if (summonThisTurn.Contains(card))
				{
					summonThisTurn.Remove(card);
				}
				if (activatedDivineSerpent1stList.Contains(card))
				{
					activatedDivineSerpent1stList.Remove(card);
				}
				if (activatedDivineSerpent2ndList.Contains(card))
				{
					activatedDivineSerpent2ndList.Remove(card);
				}
			}
			if (currentControler == 0 && currentLocation == 4)
			{
				summonThisTurn.Add(card);
			}
			if (currentControler == 0 && previousLocation == 8 && currentLocation != 8 && placedThisTurn.Contains(card))
			{
				placedThisTurn.Remove(card);
			}
			if (currentControler == 0 && currentLocation == 8)
			{
				ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
				if (currentSolvingChainInfo != null && currentSolvingChainInfo.ActivateController == 0 && currentSolvingChainInfo.ActivateId == 95561146 && card.IsCode(85888377))
				{
					placedThisTurn.Remove(card);
				}
				else
				{
					placedThisTurn.Add(card);
				}
			}
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	public void SelectSTPlace(ClientCard card = null, bool avoidImpermanence = false, List<int> avoidList = null)
	{
		if (card == null)
		{
			card = base.Card;
		}
		if (card.Location == CardLocation.SpellZone)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.SpellZone[i] == null && (!(card != null && card.Location == CardLocation.Hand && avoidImpermanence) || !infiniteImpermanenceNegatedColumns.Contains(i)) && (avoidList == null || !avoidList.Contains(i)))
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

	public bool AnubisTheLastJudgeActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster).Count > 0)
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool AnubisTheLastJudgeSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Grave))
		{
			return false;
		}
		if (base.Duel.MainPhase.ActivableCards.Contains(base.Card) && (!CheckWhetherNegated() || !CheckWhetherWillbeRemoved()))
		{
			return false;
		}
		anubisTheLastJudgeSpSummoningStep = 1;
		return true;
	}

	public bool PrimiteDragonEtherBerylActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return !CheckWhetherNegated();
		}
		if (base.ActivateDescription == base.Util.GetStringId(63198739, 1))
		{
			if (!base.Util.IsTurn1OrMain2())
			{
				return false;
			}
			if (!activatedCardIdList.Contains(56506741))
			{
				bool num = base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(56506740) && c.IsFaceup() && !c.IsDisabled());
				bool flag = base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(56506740) && c.IsFacedown());
				if (num | (base.Bot.HasInHand(56506740) && base.Bot.GetSpellCount() < 5 && !activatedCardIdList.Contains(56506740)))
				{
					return false;
				}
			}
			List<int> apophisCardIdList = new List<int> { 95561146, 85888377 };
			bool flag2 = base.Bot.HasInHand(apophisCardIdList) || base.Bot.HasInSpellZone(apophisCardIdList) || base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && apophisCardIdList.Contains(c.Id));
			if (!CheckWhetherWillbeRemoved())
			{
				return !CheckWhetherNegated() || flag2;
			}
			return false;
		}
		return !CheckWhetherNegated();
	}

	public bool TheManWithTheMarkActivate()
	{
		if (!CheckWhetherNegated())
		{
			return DefaultCheckWhetherBotCanSearch();
		}
		return false;
	}

	public bool Level4MonsterSummon()
	{
		if (!base.Card.IsCode(63198739, 97522863))
		{
			return false;
		}
		bool flag = base.Bot.HasInHand(63198739);
		if (!activatedCardIdList.Contains(56506740) && DefaultCheckWhetherBotCanSearch())
		{
			flag |= base.Bot.HasInHand(56506740) && base.Bot.GetSpellCountWithoutField() < 5;
			flag |= base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(56506740) && c.IsFacedown());
		}
		if (flag)
		{
			bool flag2 = false;
			if (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true) && CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth))
			{
				flag2 |= !activatedCardIdList.Contains(56506740) && !base.Bot.HasInHandOrInSpellZone(56506740) && CheckRemainInDeck(56506740) > 0;
				flag2 |= CheckRemainInDeck(29095457) > 0;
			}
			if (!base.Bot.HasInMonstersZone(63198739, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !activatedCardIdList.Contains(29095458) && base.Bot.HasInGraveyard(29095457) && base.Bot.GetSpellCountWithoutField() < 5)
			{
				flag2 = true;
			}
			if (flag2 && base.Card.IsCode(63198739))
			{
				summonCount--;
				return true;
			}
		}
		bool flag3 = base.Bot.HasInHand(97522863);
		flag3 |= base.Bot.HasInHand(60411677) && DefaultCheckWhetherBotCanSearch() && CheckRemainInDeck(97522863) > 0 && !activatedCardIdList.Contains(60411677);
		if (base.Bot.HasInHandOrInSpellZone(69299029) && !activatedCardIdList.Contains(69299030) && DefaultCheckWhetherBotCanSearch() && CheckRemainInDeck(97522863) > 0)
		{
			flag3 |= base.Bot.Graveyard.Any((ClientCard c) => c.IsTrap());
			int num = base.Bot.GetSpells().Count((ClientCard c) => c.IsFacedown());
			num += base.Bot.GetMonsters().Count((ClientCard c) => c.IsFacedown());
			num += GetCanSetSpells().Count();
			flag3 = flag3 || num >= 2;
		}
		if (flag3 && base.Card.IsCode(97522863))
		{
			summonCount--;
			return true;
		}
		if (base.Bot.ExtraDeck.Any((ClientCard c) => c.HasType(CardType.Synchro) && c.Level == 10) && (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(62514770) && c.IsTuner()) | (!activatedCardIdList.Contains(56506741) && base.Bot.HasInSpellZone(56506740, notDisabled: true, faceUp: true))))
		{
			summonCount--;
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
		base.AI.SelectOption(1);
		return true;
	}

	public bool TerraformingActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
		return true;
	}

	public bool PrimiteDrillbeamActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			if ((base.Bot.HasInHand(56506740) || base.Bot.HasInHand(63198739)) | (base.Bot.HasInSpellZone(95561146) || base.Bot.HasInSpellZone(85888377)) | base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(95561146, 85888377, 62514770)) | (base.Bot.HasInHand(85888377) && base.Bot.GetSpellCountWithoutField() <= 3))
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		bool flag = false;
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Spell);
		if (problematicEnemyCardList.Count((ClientCard c) => !c.IsDisabled()) > 0)
		{
			problematicEnemyCardList.RemoveAll((ClientCard c) => currentNegateCardList.Contains(c));
			if (problematicEnemyCardList.Count > 0)
			{
				flag = true;
			}
		}
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Controller == 1 && item.IsOnField() && item.IsFaceup() && !item.IsDisabled() && !currentNegateCardList.Contains(item) && CheckCanBeTargeted(item, canBeTarget: true, CardType.Spell) && CheckCardShouldNegate(item))
			{
				flag = true;
			}
		}
		if (base.Bot.HasInMonstersZone(63198739, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !activatedCardIdList.Contains(29095458) && (base.CurrentTiming & 0x200000) == 0 && GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Spell, forNegate: true).Count > 0)
		{
			flag = true;
		}
		if (DefaultOnBecomeTarget())
		{
			flag |= base.Enemy.GetSpells().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && CheckCanBeTargeted(c, canBeTarget: true, CardType.Spell));
			flag |= base.Enemy.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && CheckCanBeTargeted(c, canBeTarget: true, CardType.Spell));
		}
		if (base.Duel.Phase == DuelPhase.BattleStep)
		{
			ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: true, CardType.Spell);
			if (problematicEnemyMonster != null && !problematicEnemyMonster.IsDisabled())
			{
				flag = true;
			}
		}
		if (flag)
		{
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool PrimiteLordlyLodeActivate()
	{
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFaceup())
		{
			return false;
		}
		bool flag = PrimiteLordlyLodeActivateCheck();
		Logger.DebugWriteLine("PrimiteLordlyLodeActivate: " + flag);
		if (flag)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool PrimiteLordlyLodeActivateCheck()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		bool flag = false;
		if (base.Bot.HasInHandOrHasInMonstersZone(63198739) && DefaultCheckWhetherBotCanSearch())
		{
			flag |= CheckRemainInDeck(29095457) > 0;
			flag |= summonCount <= 0 && base.Card.Location == CardLocation.SpellZone && base.Card.IsFacedown();
		}
		if (summonCount > 0 && !base.Bot.HasInHand(63198739) && CheckRemainInDeck(63198739) > 0 && DefaultCheckWhetherBotCanSearch())
		{
			flag |= base.Bot.HasInGraveyard(29095457);
			flag |= CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		}
		if (!base.Bot.HasInSpellZone(56506740, notDisabled: true, faceUp: true))
		{
			flag |= DefaultCheckWhetherBotCanSearch();
			CardLocation loc;
			if (base.Bot.HasInHand(62514770))
			{
				loc = CardLocation.Hand;
			}
			else if (CheckRemainInDeck(62514770) > 0)
			{
				loc = CardLocation.Deck;
			}
			else
			{
				if (!base.Bot.HasInGraveyard(62514770))
				{
					return false;
				}
				loc = CardLocation.Grave;
			}
			if (GetSpecialSummonDrawCount(loc) < 2)
			{
				flag |= base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.Level == 4 && !c.HasType((CardType)75497472)) && base.Bot.ExtraDeck.Any((ClientCard c) => c.IsFaceup() && c.Level == 10 && c.HasType(CardType.Synchro));
			}
		}
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFacedown())
		{
			flag |= DefaultCheckWhetherBotCanSearch();
		}
		return flag;
	}

	public bool PrimiteLordlyLodeSpSummon()
	{
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFaceup())
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			if (!PrimiteLordlyLodeSpSummonCheck())
			{
				return false;
			}
			activatingLodeSpSummonEffect = true;
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool PrimiteLordlyLodeSpSummonCheck()
	{
		CardLocation loc;
		if (base.Bot.HasInHand(62514770))
		{
			loc = CardLocation.Hand;
		}
		else if (CheckRemainInDeck(62514770) > 0)
		{
			loc = CardLocation.Deck;
		}
		else
		{
			if (!base.Bot.HasInGraveyard(62514770))
			{
				return false;
			}
			loc = CardLocation.Grave;
		}
		return GetSpecialSummonDrawCount(loc) < 2;
	}

	public bool TreasuresOfTheKingsActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(69299029, 0))
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		bool result = false;
		if (base.Bot.GetSpellCountWithoutField() == 5)
		{
			if (!DefaultCheckWhetherBotCanSearch() || CheckRemainInDeck(97522863, 60411677) == 0)
			{
				result = false;
			}
			else
			{
				bool flag = base.Bot.Hand.Any((ClientCard c) => (c.IsSpell() || c.IsTrap()) && c != base.Card);
				bool num = base.Bot.Graveyard.Any((ClientCard c) => c.IsTrap());
				int num2 = base.Bot.GetMonsters().Count((ClientCard m) => m.IsFacedown()) + base.Bot.GetSpells().Count((ClientCard s) => s.IsFacedown() && s != base.Card);
				result = num || flag || num2 >= 2;
			}
		}
		else if (CheckRemainInDeck(95561146, 85888377) > 0)
		{
			result = true;
		}
		return result;
	}

	public bool DominusSparkActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(6325660) && c.IsOnField() && c != base.Card && !infiniteImpermanenceNegatedColumns.Contains(c.Sequence)))
		{
			return false;
		}
		bool flag = false;
		ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(-1, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Trap);
		flag = flag || problematicEnemyMonster != null;
		if (base.Duel.Phase == DuelPhase.BattleStep && base.Duel.Player == 1)
		{
			problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Trap);
			flag = flag || problematicEnemyMonster != null;
		}
		if (base.Duel.Phase == DuelPhase.End && base.Duel.Player == 1 && base.Duel.Turn == 1)
		{
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
			flag |= normalEnemyTargetList.Any((ClientCard c) => c.IsMonster() && c.IsFaceup());
		}
		if (flag | (DefaultOnBecomeTarget() && !CheckWhetherNegated()))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
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
			int sequence = base.Card.Sequence;
			int num = -1;
			if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.SpellZone)
			{
				num = lastChainCard.Sequence;
			}
			if ((sequence * num >= 0 && sequence + num == 4) || base.Util.IsChainTarget(base.Card) || (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.IsCode(18144506)))
			{
				return true;
			}
		}
		if (GetMonsterListForTargetNegate(canBeTarget: true, CardType.Trap).Count > 0)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool DominusNegateTrapActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(base.Card.Id) && c.IsOnField() && c != base.Card && !infiniteImpermanenceNegatedColumns.Contains(c.Sequence)))
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 1 && CheckCardShouldNegate(lastChainCard))
		{
			currentNegateCardList.Add(lastChainCard);
			SelectSTPlace(base.Card, avoidImpermanence: true);
			if (base.Card.IsCode(58053438) && base.Card.Location == CardLocation.Hand)
			{
				songsOfTheDominatorsActivatedFromHand = true;
			}
			return true;
		}
		return false;
	}

	public bool SongsOfTheDominatorsActivateFirst()
	{
		if (base.Bot.Graveyard.Any((ClientCard c) => c.HasType(CardType.Trap)))
		{
			return DominusNegateTrapActivate();
		}
		return false;
	}

	public bool ApophisTheSwampDeityActivate()
	{
		return ApophisTheSwampDeityActivateCheck(1);
	}

	public bool ApophisTheSwampDeityActivateForAttack()
	{
		return ApophisTheSwampDeityActivateCheck(2);
	}

	public bool ApophisTheSwampDeityActivateCheck(int activatePriority = 0)
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (currentSummoningCount + base.Bot.GetMonsters().Count((ClientCard c) => c.Sequence < 5) >= 5)
		{
			return false;
		}
		int num = base.Bot.GetSpells().Count((ClientCard c) => c != base.Card && (c.IsCode(85888377) || (c.IsCode(95561146) && (c.IsFaceup() || !activatedCardIdList.Contains(95561146)))));
		num += base.Bot.GetMonsters().Count((ClientCard c) => c.IsCode(new List<int> { 95561146, 85888377 }) && c.IsFaceup());
		bool flag = false;
		if (base.Duel.CurrentChain.Count > 0 && num > 0)
		{
			foreach (ClientCard item in base.Duel.CurrentChain)
			{
				if (item.IsFaceup() && item.IsOnField() && item.Controller == 1 && !currentNegateCardList.Contains(item))
				{
					Logger.DebugWriteLine("[ApophisTheSwampDeity] Negate card on chain: " + item.Name);
					flag = true;
					break;
				}
			}
		}
		if (DivineSerpentApophisSpSummonCheck())
		{
			int num2 = base.Duel.MainPhase.ActivableCards.Count((ClientCard c) => c.IsCode(95561146, 85888377));
			int num3 = base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && (c.IsCode(95561146, 85888377) || (c.IsCode(97800311) && activatedDivineSerpent1stList.Contains(c))));
			bool flag2 = num3 < 2 && num3 + num2 >= 2;
			if (flag2)
			{
				Logger.DebugWriteLine("[ApophisTheSwampDeity] DivineSerpentApophisSpSummonCheck: " + flag2);
			}
			flag = flag || flag2;
		}
		if (base.Bot.ExtraDeck.Any((ClientCard c) => c.IsCode(46772449)) && !SPLittleKnightSummonCheck() && SPLittleKnightSummonCheck(ignoreMaterialCheck: true) && base.Duel.Player == 0 && base.Bot.ExtraDeck.Any((ClientCard c) => c.IsCode(98978921)))
		{
			bool flag3 = base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Effect)) == 1;
			if (flag3)
			{
				Logger.DebugWriteLine("[ApophisTheSwampDeity] SPSmallKnightSummonCheck: " + flag3);
			}
			flag = flag || flag3;
		}
		if (CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && c.IsCode(97800311) && !activatedDivineSerpent2ndList.Contains(c)))
		{
			bool flag4 = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0 && !base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsCode(95561146, 85888377)) && !base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(95561146));
			if ((base.CurrentTiming & 4) != 0 && base.Duel.Player == 1)
			{
				flag4 |= GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster).Count() > 0 && !base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsCode(95561146, 85888377)) && !base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(95561146));
			}
			if (flag4)
			{
				Logger.DebugWriteLine("[ApophisTheSwampDeity] DivineSerpentApophisActivate: " + flag4);
			}
			flag = flag || flag4;
		}
		if (CheckWhetherCanActivateMonsterEffect(CardAttribute.Light) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster, ignore41: true) && !activatedCardIdList.Contains(1528055) && base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && c.IsCode(1528054)))
		{
			bool flag5 = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count((ClientCard c) => c.HasType((CardType)6)) > 0 && !base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsCode(95561146, 85888377)) && !base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(95561146));
			if ((base.CurrentTiming & 4) != 0 && base.Duel.Player == 1)
			{
				flag5 |= GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster).Count() > 0 && !base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsCode(95561146, 85888377)) && !base.Duel.MainPhase.ActivableCards.Any((ClientCard c) => c.IsCode(95561146));
			}
			if (flag5)
			{
				Logger.DebugWriteLine("[ApophisTheSwampDeity] SilhouhatteRabbitSummonCheck: " + flag5);
			}
			flag = flag || flag5;
		}
		if (base.Bot.HasInHandOrInSpellZone(29095457) && !activatedCardIdList.Contains(29095457))
		{
			bool flag6 = base.Bot.Hand.Any((ClientCard c) => c.IsCode(63198739, 56506740, 62514770));
			flag6 |= base.Bot.HasInMonstersZone(new List<int> { 95561146, 85888377, 62514770 }, notDisabled: false, hasXyzMaterial: false, faceUp: true);
			flag6 |= base.Bot.GetSpells().Any((ClientCard c) => c != base.Card && c.IsCode(95561146, 85888377) && (c.IsFacedown() || base.Duel.CurrentChain.Contains(c)));
			bool flag7 = !flag6;
			if (flag7)
			{
				Logger.DebugWriteLine("[ApophisTheSwampDeity] PrimiteDrillbeamSummonCheck: " + flag7);
			}
			flag = flag || !flag6;
		}
		if (base.Duel.Turn > 1 && base.Duel.Phase == DuelPhase.Main1 && base.Enemy.GetMonsterCount() == 0 && base.Duel.Player == 0 && base.Duel.CurrentChain.Count == 0 && activatePriority >= 2)
		{
			Logger.DebugWriteLine("[ApophisTheSwampDeity] Attack: " + true);
			flag = (byte)((flag ? 1u : 0u) | 1u) != 0;
		}
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.GetMonsterCount() == 0 && base.Enemy.GetMonsters().Sum((ClientCard c) => c.Attack) >= base.Bot.LifePoints && (base.CurrentTiming & 4) != 0)
		{
			Logger.DebugWriteLine("[ApophisTheSwampDeity] Defense: " + true);
			flag = (byte)((flag ? 1u : 0u) | 1u) != 0;
		}
		if (flag)
		{
			int num4 = base.Bot.GetSpells().Count((ClientCard c) => c != base.Card && c.IsFaceup() && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap));
			num4 += base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap));
			if (num4 > 0)
			{
				foreach (ClientCard item2 in base.Duel.CurrentChain)
				{
					if (item2.IsFaceup() && item2.IsOnField() && !currentNegateCardList.Contains(item2) && CheckCardShouldNegate(item2))
					{
						num4--;
						currentNegateCardList.Add(item2);
						if (num4 <= 0)
						{
							break;
						}
					}
				}
			}
			if (num4 > 0)
			{
				foreach (ClientCard item3 in (from c in base.Enemy.GetSpells()
					where c.IsFaceup() && !c.IsDisabled() && !currentNegateCardList.Contains(c)
					select c).ToList())
				{
					currentNegateCardList.Add(item3);
					num4--;
					if (num4 <= 0)
					{
						break;
					}
				}
			}
			if (num4 > 0)
			{
				foreach (ClientCard item4 in (from c in base.Enemy.GetMonsters()
					where c.IsFaceup() && !c.IsDisabled() && !currentNegateCardList.Contains(c)
					select c).ToList())
				{
					currentNegateCardList.Add(item4);
					num4--;
					if (num4 <= 0)
					{
						break;
					}
				}
			}
			currentSummoningCount++;
			return true;
		}
		return false;
	}

	public bool ApophisTheSerpentActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (currentSummoningCount + base.Bot.GetMonsters().Count((ClientCard c) => c.Sequence < 5) >= 5)
		{
			return false;
		}
		if (base.Bot.GetSpells().Any((ClientCard c) => c.IsFacedown() && c.IsCode(85888377) && !infiniteImpermanenceNegatedColumns.Contains(c.Sequence)) && base.Duel.Phase != DuelPhase.Main1 && base.Duel.Phase != DuelPhase.Main2)
		{
			return false;
		}
		currentSummoningCount++;
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool SpellNegateActivate()
	{
		if (CheckWhetherNegated() || base.Duel.LastChainPlayer != 1)
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && !CheckCardShouldNegate(lastChainCard))
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool SolemnReportBanishActivate()
	{
		if (SpellNegateActivate())
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.IsCode(solemnReportBanishIdList))
			{
				return true;
			}
		}
		return false;
	}

	public bool DivineSerpentApophisSpSummonCheck()
	{
		if (base.Duel.Player != 0)
		{
			return false;
		}
		bool flag = false;
		if (CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Bot.GetSpellCountWithoutField() < 5)
		{
			flag |= base.Bot.HasInGraveyard(new List<int> { 95561146, 85888377 });
			flag |= !CheckWhetherWillbeRemoved();
		}
		return flag | (GetBotBestPower(onlyATK: true) <= base.Util.GetBestPower(base.Enemy) && base.Util.GetBestPower(base.Enemy) < 2800);
	}

	public bool DivineSerpentApophisSpSummon()
	{
		if (!DivineSerpentApophisSpSummonCheck())
		{
			return false;
		}
		ClientCard item = base.Bot.GetMonsters().FirstOrDefault((ClientCard c) => c.IsFaceup() && c.IsCode(95561146));
		ClientCard item2 = base.Bot.GetMonsters().FirstOrDefault((ClientCard c) => c.IsFaceup() && c.IsCode(85888377));
		base.AI.SelectMaterials(new List<ClientCard> { item, item2 }, 500);
		return true;
	}

	public bool DivineSerpentApophisActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(97800311, 0))
		{
			activatedDivineSerpent1stList.Add(base.Card);
		}
		else
		{
			if (GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster).Count() == 0)
			{
				return false;
			}
			activatedDivineSerpent2ndList.Add(base.Card);
		}
		return true;
	}

	public bool Level10SynchroSummon()
	{
		if (!Level10SynchroSummonCheck())
		{
			return false;
		}
		if (!base.Card.IsCode(new List<int> { 84815190, 96633955 }))
		{
			return false;
		}
		int num = 0;
		if (GetBotBestPower(onlyATK: true) < base.Util.GetBestPower(base.Enemy) && base.Util.GetBestPower(base.Enemy) > 3000 && (!CheckWhetherCanActivateMonsterEffect(CardAttribute.Wind) || lodeSpSummonEffectResolved) && base.Duel.Phase == DuelPhase.Main1 && base.Duel.Turn > 1)
		{
			int num2 = base.Bot.Banished.Count() + base.Enemy.Banished.Count();
			if (3000 + num2 * 200 >= base.Util.GetBestPower(base.Enemy) && base.Duel.MainPhase.SummonableCards.Any((ClientCard c) => c.IsCode(96633955)) && !base.Card.IsCode(96633955))
			{
				return false;
			}
			num = 96633955;
		}
		if (num == 0 && CheckWhetherCanActivateMonsterEffect(CardAttribute.Wind) && base.Duel.MainPhase.SummonableCards.Any((ClientCard c) => c.IsCode(84815190)) && !base.Card.IsCode(84815190))
		{
			return false;
		}
		if (num == 0 && !CheckWhetherCanActivateMonsterEffect(CardAttribute.Wind) && base.Duel.MainPhase.SummonableCards.Any((ClientCard c) => c.IsCode(96633955)) && !base.Card.IsCode(96633955))
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard c) => c.IsFaceup() && c.Level == 4 && c.IsCode(63198739));
		if (clientCard == null)
		{
			clientCard = (from c in base.Bot.GetMonsters()
				orderby c.GetDefensePower()
				select c).FirstOrDefault((ClientCard c) => c.IsFaceup() && c.Level == 4 && !c.HasType((CardType)75497472));
		}
		if (clientCard == null)
		{
			return false;
		}
		ClientCard clientCard2 = (from c in base.Bot.GetMonsters()
			orderby c.GetDefensePower()
			select c).FirstOrDefault((ClientCard c) => c.IsFaceup() && c.Level == 6 && c.IsTuner());
		if (clientCard2 == null)
		{
			return false;
		}
		base.AI.SelectMaterials(new List<ClientCard> { clientCard, clientCard2 });
		return true;
	}

	public bool Level10SynchroSummonCheck()
	{
		if (CheckShouldNoMoreSpSummon())
		{
			return false;
		}
		if (GetSpecialSummonDrawCount(CardLocation.Extra) > 1 && CheckAtAdvantage())
		{
			return false;
		}
		return true;
	}

	public bool SwordsoulSupremeSovereignChengyingActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool BaronneDeFleurNegateEffect()
	{
		if (base.ActivateDescription != base.Util.GetStringId(84815190, 1))
		{
			return false;
		}
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Duel.LastChainPlayer == 1 && lastChainCard != null)
		{
			if (CheckAtAdvantage() && lastChainCard.IsCode(new List<int> { 23434538, 42141493, 87126721, 84192580 }))
			{
				return false;
			}
			if (base.Duel.LastChainTargets.Contains(base.Card) && lastChainCard.IsCode(97268402, 10045474, 78474168))
			{
				return false;
			}
		}
		currentNegateCardList.Add(lastChainCard);
		return true;
	}

	public bool BaronneDeFleurActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(84815190, 1))
		{
			return false;
		}
		if (base.Duel.Phase == DuelPhase.Standby)
		{
			return false;
		}
		if (GetNormalEnemyTargetList().Count() > 0)
		{
			return true;
		}
		return false;
	}

	public bool SuperdreadnoughtRailCannonJuggernautLiebeActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		return true;
	}

	public bool SuperdreadnoughtRailCannonJuggernautLiebeSpSummon()
	{
		if (!SuperdreadnoughtRailCannonJuggernautLiebeSpSummonCheck())
		{
			return false;
		}
		return true;
	}

	public bool SuperdreadnoughtRailCannonJuggernautLiebeSpSummonCheck()
	{
		int bestPower = base.Util.GetBestPower(base.Enemy);
		if (GetBotBestPower(onlyATK: true) < bestPower)
		{
			int num = 4000;
			if (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth) && !lodeSpSummonEffectResolved)
			{
				num += 2000;
			}
			if (num >= bestPower)
			{
				return true;
			}
		}
		return false;
	}

	public bool SuperdreadnoughtRailCannonFlyingLauncherActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(38354018, 2) && GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster).Count() > 0)
		{
			return true;
		}
		return false;
	}

	public bool SuperdreadnoughtRailCannonFlyingLauncherSpSummon()
	{
		bool flag = base.Bot.ExtraDeck.Any((ClientCard c) => c.IsCode(26096328));
		int bestPower = base.Util.GetBestPower(base.Enemy);
		int botBestPower = GetBotBestPower(onlyATK: true);
		bool flag2 = SuperdreadnoughtRailCannonJuggernautLiebeSpSummonCheck();
		if ((flag && flag2) || (!flag && botBestPower < bestPower && bestPower <= 3800) || (CheckWhetherCanActivateMonsterEffect(CardAttribute.Earth) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !lodeSpSummonEffectResolved && GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster).Count() > 0))
		{
			return true;
		}
		return false;
	}

	public bool EvilswarmExcitonKnightSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Light))
		{
			return false;
		}
		int num = base.Bot.GetMonsterCount() + base.Bot.GetSpellCount() + base.Bot.GetHandCount();
		int num2 = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() + base.Enemy.GetHandCount();
		if (num - 1 < num2)
		{
			return DefaultEvilswarmExcitonKnightEffect();
		}
		return false;
	}

	public bool SPLittleKnightActivate()
	{
		if (base.ActivateDescription == -1 || base.ActivateDescription == base.Util.GetStringId(46772449, 0))
		{
			List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster);
			problematicEnemyCardList.AddRange(GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster));
			problematicEnemyCardList.AddRange(from card in base.Enemy.Graveyard
				where card.HasType(CardType.Monster)
				orderby card.Attack descending
				select card);
			problematicEnemyCardList.AddRange(base.Enemy.Graveyard.Where((ClientCard card) => !card.HasType(CardType.Monster)));
			if (problematicEnemyCardList.Count() > 0)
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		else if (base.ActivateDescription == base.Util.GetStringId(46772449, 1))
		{
			ClientCard selfMonster = null;
			foreach (ClientCard monster in base.Bot.GetMonsters())
			{
				if (base.Duel.ChainTargets.Contains(monster))
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
				}
				if (base.Enemy.GetMonsterCount() > 0)
				{
					clientCard = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster);
				}
				if (clientCard != null)
				{
					SPLittleKnightRemoveStep = 1;
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
		}
		return false;
	}

	public bool SPLittleKnightSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster, ignore41: true) || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Dark))
		{
			return false;
		}
		List<ClientCard> list = (from c in base.Bot.GetMonsters()
			where c.IsFaceup() && c.HasType(CardType.Effect) && (!c.IsCode(1528054) || !summonThisTurn.Contains(c))
			orderby c.GetDefensePower()
			select c).ToList();
		if (SPLittleKnightSummonCheck() || (base.Util.IsTurn1OrMain2() && base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() + base.Enemy.Graveyard.Count > 0))
		{
			ClientCard clientCard = null;
			ClientCard clientCard2 = null;
			foreach (ClientCard item in list)
			{
				if (base.Util.IsTurn1OrMain2() && item.IsCode(new List<int> { 84815190, 96633955, 97800311 }))
				{
					continue;
				}
				if (item.HasType((CardType)75505728))
				{
					if (clientCard != null)
					{
						clientCard2 = item;
					}
					else
					{
						clientCard = item;
					}
				}
				else
				{
					clientCard2 = item;
				}
				if (clientCard != null && clientCard2 != null)
				{
					break;
				}
			}
			if (clientCard != null && clientCard2 != null)
			{
				base.AI.SelectMaterials(new List<ClientCard> { clientCard, clientCard2 });
				return true;
			}
		}
		return false;
	}

	public bool SPLittleKnightSummonCheck(bool ignoreMaterialCheck = false)
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster, ignore41: true) || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Dark) || lodeSpSummonEffectResolved)
		{
			return false;
		}
		bool flag = GetProblematicEnemyCardList(canBeTarget: true, !base.Util.IsTurn1OrMain2(), CardType.Monster).Count() > 0;
		if (GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster) != null)
		{
			flag = true;
		}
		if (flag)
		{
			if (ignoreMaterialCheck)
			{
				return true;
			}
			if (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType((CardType)75505728) && (!c.IsCode(1528054) || !summonThisTurn.Contains(c))))
			{
				return base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Effect)) >= 2;
			}
			return false;
		}
		return false;
	}

	public bool SilhouhatteRabbitActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(1528054, 0) || base.ActivateDescription == -1)
		{
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (GetNormalEnemySpellTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster).Count() > 0)
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool SilhouhatteRabbitSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster, ignore41: true) || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Light))
		{
			return false;
		}
		if (!SilhouhatteRabbitSummonCheck())
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard c) => c.IsFaceup() && c.IsCode(60411677));
		List<ClientCard> list = new List<ClientCard>();
		if (clientCard != null)
		{
			list.Add(clientCard);
		}
		List<ClientCard> list2 = (from c in base.Bot.GetMonsters()
			where c.IsFaceup() && c.HasType(CardType.Effect) && (!c.IsCode(1528054) || !summonThisTurn.Contains(c))
			orderby c.GetDefensePower()
			select c).ToList();
		if (list2.Count() > 0)
		{
			foreach (ClientCard item in list2)
			{
				if (!item.IsCode(new List<int> { 84815190, 96633955, 97800311, 46772449 }))
				{
					list.Add(item);
					if (list.Count() >= 2)
					{
						break;
					}
				}
			}
		}
		if (list.Count() >= 2)
		{
			base.AI.SelectMaterials(list);
			return true;
		}
		return false;
	}

	public bool SilhouhatteRabbitSummonCheck(bool ignoreMaterialCheck = false)
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster, ignore41: true) || !CheckWhetherCanActivateMonsterEffect(CardAttribute.Light) || lodeSpSummonEffectResolved)
		{
			return false;
		}
		if (GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() > 0)
		{
			return false;
		}
		if (CheckRemainInDeck(85888377) == 0)
		{
			return false;
		}
		if (base.Bot.Hand.Any((ClientCard c) => c.IsCode(85888377)) || base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(85888377) && c.IsFacedown()))
		{
			return false;
		}
		if (activatedCardIdList.Contains(1528054))
		{
			return false;
		}
		if (ignoreMaterialCheck)
		{
			return true;
		}
		return base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Effect) && (!c.IsCode(1528054) || !summonThisTurn.Contains(c))) >= 2;
	}

	public bool LinkSpiderSummon()
	{
		if (base.Bot.GetMonsterCount() + base.Duel.MainPhase.ActivableCards.Count((ClientCard c) => c.IsOnField() && c.IsCode(95561146, 85888377)) < 2)
		{
			return false;
		}
		if (base.Bot.ExtraDeck.Any((ClientCard c) => c.IsCode(46772449)))
		{
			if (SPLittleKnightSummonCheck())
			{
				return false;
			}
			if (SPLittleKnightSummonCheck(ignoreMaterialCheck: true))
			{
				return true;
			}
		}
		if (base.Bot.ExtraDeck.Any((ClientCard c) => c.IsCode(1528054)))
		{
			if (SilhouhatteRabbitSummonCheck())
			{
				return false;
			}
			if (SilhouhatteRabbitSummonCheck(ignoreMaterialCheck: true))
			{
				return true;
			}
		}
		return false;
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

	public bool MonstetReposForImportantMonsters()
	{
		if (!base.Card.IsFacedown())
		{
			return false;
		}
		return base.Card.HasType((CardType)8396864);
	}

	public bool SpellSetFirst()
	{
		int num = base.Bot.GetSpells().Count((ClientCard c) => c.IsFacedown() && c.Sequence < 5);
		if (num < 2 && base.Bot.LifePoints > 1500)
		{
			if (num == 1 && !base.Bot.HasInSpellZone(78114463) && base.Bot.HasInHand(78114463) && base.Bot.GetSpells().Any((ClientCard c) => c.IsTrap() && c.IsFacedown()))
			{
				if (!base.Card.IsCode(78114463))
				{
					return false;
				}
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
			List<ClientCard> canSetSpells = GetCanSetSpells();
			if (num + canSetSpells.Count() > 0 && canSetSpells.Contains(base.Card) && (!base.Bot.HasInHand(78114463) || base.Card.IsTrap()))
			{
				bool avoidImpermanence = base.Card.IsCode(78114463);
				SelectSTPlace(base.Card, avoidImpermanence);
				return true;
			}
		}
		return false;
	}

	public List<ClientCard> GetCanSetSpells()
	{
		List<ClientCard> list = new List<ClientCard>();
		foreach (ClientCard card in base.Bot.Hand)
		{
			bool flag = false;
			switch (base.Card.Id)
			{
			case 73628505:
				flag |= CheckRemainInDeck(69299029) > 0 && DefaultCheckWhetherBotCanSearch();
				break;
			case 56506740:
				flag |= PrimiteLordlyLodeActivateCheck() && !list.Any((ClientCard c) => c.IsCode(56506740));
				break;
			case 6325660:
				flag |= !enemyActivateMonsterEffectFromHandGrave;
				break;
			case 10045474:
			case 59576447:
			case 78114463:
				flag = true;
				break;
			case 40366667:
			case 97045737:
				flag |= base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() == 0;
				break;
			case 58053438:
				flag |= !base.Bot.Graveyard.Any((ClientCard c) => c.IsMonster());
				break;
			case 85888377:
				flag |= base.Bot.Hand.Any((ClientCard c) => c != card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap));
				flag |= base.Bot.GetSpells().Any((ClientCard c) => c != card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap));
				flag |= base.Bot.GetMonsters().Any((ClientCard c) => c != card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap));
				break;
			case 95561146:
				flag |= CheckRemainInDeck(85888377) > 0;
				flag |= base.Bot.HasInHandOrInSpellZone(85888377);
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				list.Add(card);
			}
		}
		return list;
	}

	public bool SpellSet()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay))
		{
			switch (base.Card.Id)
			{
			case 10045474:
				if (base.Bot.GetMonsterCount() == 0 && base.Bot.GetSpellCount() == 0 && !base.Bot.Hand.Any((ClientCard c) => !c.IsCode(10045474) && (c.IsTrap() || c.HasType(CardType.QuickPlay))) && base.Bot.Hand.Count() <= 6)
				{
					return false;
				}
				break;
			case 58053438:
				if (!base.Bot.Graveyard.Any((ClientCard c) => c.IsMonster()) && base.Bot.GetMonsterCount() == 0)
				{
					return false;
				}
				if (base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(58053438) && c.IsFacedown()))
				{
					return false;
				}
				break;
			case 85888377:
				if (!base.Bot.Hand.Any((ClientCard c) => c != base.Card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap)) && !base.Bot.GetSpells().Any((ClientCard c) => c != base.Card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap)) && !base.Bot.GetMonsters().Any((ClientCard c) => c != base.Card && c.HasType(CardType.Continuous) && c.HasType(CardType.Trap)) && !base.Bot.HasInHandOrInSpellZone(29095457) && (base.Bot.GetMonsterCount() != 0 || base.Bot.Hand.Any((ClientCard c) => c.IsCode(95561146, 85888377) && c != base.Card) || base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(95561146, 85888377) && c != base.Card && c.IsFacedown())))
				{
					return false;
				}
				break;
			case 78114463:
				if (base.Bot.LifePoints <= 1500)
				{
					return false;
				}
				break;
			case 6325660:
			case 40366667:
			case 95561146:
			case 97045737:
				if (base.Bot.GetSpells().Any((ClientCard c) => c.IsCode(base.Card.Id) && c.IsFacedown()))
				{
					return false;
				}
				break;
			}
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
}
