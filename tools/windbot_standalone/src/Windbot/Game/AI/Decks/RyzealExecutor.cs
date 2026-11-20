using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Ryzeal", "AI_Ryzeal", "Normal")]
public class RyzealExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int IceRyzeal = 8633261;

		public const int ThodeRyzeal = 35844557;

		public const int NodeRyzeal = 72238166;

		public const int ExRyzeal = 34022970;

		public const int SeventhTachyon = 7477101;

		public const int TripleTacticsTalent = 25311006;

		public const int Bonfire = 85106525;

		public const int RyzealPlugIn = 60394026;

		public const int RyzealCross = 6798031;

		public const int MereologicAggregator = 9940036;

		public const int RyzealDeadnader = 34909328;

		public const int Number104Masquerade = 2061963;

		public const int RyzealDuodrive = 7511613;

		public const int TwinsOfTheEclipse = 45852939;

		public const int FullArmoredUtopicRayLancer = 1269512;

		public const int TornadoDragon = 6983839;

		public const int StarliegePhotonBlastDragon = 16643334;

		public const int AbyssDweller = 21044178;

		public const int Number60DugaresTheTimeless = 66011101;

		public const int DonnerDaggerFurHire = 8728498;
	}

	private const int attrbuteLightDark = 48;

	private const int SetcodeTimeLord = 74;

	private const int SetcodeAtlantean = 119;

	private const int SetcodeInfernoid = 187;

	private const int SetcodeMajespecter = 208;

	private const int SetcodePhantomKnight = 4315;

	private const int SetcodeSkyStriker = 277;

	private const int SetcodeOrcust = 283;

	private const int SetcodeSangen = 425;

	private const int SetcodeTenpaiDragon = 426;

	private const int SetcodeBranded = 349;

	private const int SetcodeFloowandereeze = 365;

	private const int SetcodeLabrynth = 382;

	private const int SetcodeTearlaments = 385;

	private const int SetcodeHorus = 413;

	private const int SetcodeRyzeal = 446;

	private const int hintTimingMainEnd = 4;

	private List<int> NotToNegateIdList = new List<int> { 58699500, 20343502, 25451383, 19403423 };

	private List<int> AlbazFusionList = new List<int> { 1906812, 38811586, 41373230, 44146295, 51409648, 51409648, 87746184 };

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 8633261, 35844557, 34022970, 14558127, 97268402, 7477101, 10045474 }
		},
		{
			2,
			new List<int> { 42141493, 59438930, 23434538, 35261759, 24224830 }
		},
		{
			1,
			new List<int> { 72238166, 84192580, 87126721, 94145021, 25311006, 85106525, 60394026, 65681983, 6798031 }
		}
	};

	private List<int> NotToDestroySpellTrap = new List<int> { 50005218, 6767771 };

	private List<int> targetNegateIdList = new List<int>
	{
		97268402, 10045474, 52038441, 78474168, 9940036, 74003290, 67037924, 9753964, 66192538, 23204029,
		73445448, 35103106, 30286474, 45002991, 5795980, 38511382, 53742162, 30430448
	};

	private List<int> NeedIceToSolveIdList = new List<int> { 80978111, 87170768 };

	private List<ClientCard> currentCanActivateEffect = new List<ClientCard>();

	private int maxSummonCount = 1;

	private int summonCount = 1;

	private bool enemyActivateMaxxC;

	private bool enemyActivatePurulia;

	private bool enemyActivateFuwalos;

	private bool enemyActivateNyalus;

	private bool lockBirdSolved;

	private int dimensionShifterCount;

	private bool botActivateMulcharmy;

	private bool botSolvingCross;

	private List<int> CheckSetcodeList = new List<int> { 4315, 283, 119, 446, 426, 425, 187, 277, 382, 385 };

	private List<int> CheckBotSolvedList = new List<int> { 23434538, 84192580, 42141493, 87126721, 21044178, 46772449, 60394026 };

	private bool enemyActivateInfiniteImpermanenceFromHand;

	private ClientCard deadnaderDestroySelf;

	private List<int> infiniteImpermanenceList = new List<int>();

	private List<ClientCard> currentNegateCardList = new List<ClientCard>();

	private List<ClientCard> currentDestroyCardList = new List<ClientCard>();

	private List<int> activatedCardIdList = new List<int>();

	private List<int> spSummonedCardIdList = new List<int>();

	private List<int> botSolvedCardIdList = new List<int>();

	private List<ClientCard> enemyPlaceThisTurn = new List<ClientCard>();

	private List<ClientCard> summonThisTurn = new List<ClientCard>();

	private List<ClientCard> hardToDestroyCardList = new List<ClientCard>();

	private List<ClientCard> cannotDestroyCardList = new List<ClientCard>();

	private HashSet<int> enemyDeckTypeRecord = new HashSet<int>();

	public RyzealExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 97268402, EffectVeilerActivate);
		AddExecutor(ExecutorType.Activate, 59438930, GhostOgreAndSnowRabbitActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 6798031, RyzealCrossActivateCard);
		AddExecutor(ExecutorType.Activate, 46772449, EvilswarmExcitonKnightActivate);
		AddExecutor(ExecutorType.Activate, 34909328, RyzealDeadnaderActivate);
		AddExecutor(ExecutorType.Activate, 7511613, RyzealDuodriveActivate);
		AddExecutor(ExecutorType.Activate, 45852939, TwinsOfTheEclipseActivate);
		AddExecutor(ExecutorType.Activate, 21044178, AbyssDwellerActivate);
		AddExecutor(ExecutorType.Activate, 6983839, TornadoDragonActivate);
		AddExecutor(ExecutorType.Activate, 94145021, LockBirdActivate);
		AddExecutor(ExecutorType.Activate, 84192580, MulcharmyPuruliaActivate);
		AddExecutor(ExecutorType.Activate, 87126721, MulcharmyNyalusActivate);
		AddExecutor(ExecutorType.Activate, 42141493, MulcharmyFuwalosActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 85106525, BonfireActivate);
		AddExecutor(ExecutorType.Activate, 8728498, DonnerDaggerFurHireActivate);
		AddExecutor(ExecutorType.Activate, 66011101, Number60DugaresTheTimelessActivate);
		AddExecutor(ExecutorType.Activate, 25311006, TripleTacticsTalentActivate);
		AddExecutor(ExecutorType.Activate, 85106525, BonfireActivateToSearchNecessary);
		AddExecutor(ExecutorType.Activate, 7477101, SeventhTachyonActivate);
		AddExecutor(ExecutorType.Repos, ChangePositionFirst);
		AddExecutor(ExecutorType.SpSummon, 46772449, EvilswarmExcitonKnightSpSummon);
		AddExecutor(ExecutorType.SpSummon, LessSpSummonExtra);
		AddExecutor(ExecutorType.SpSummon, 7511613, FirstRyzealDuodriveSpSummon);
		AddExecutor(ExecutorType.SpSummon, SecondXyzSummon);
		AddExecutor(ExecutorType.SpSummon, 45852939, TwinsOfTheEclipseSpSummon);
		AddExecutor(ExecutorType.SpSummon, FinalXyzSummon);
		AddExecutor(ExecutorType.SpSummon, 8728498, DonnerDaggerFurHireSpSummon);
		AddExecutor(ExecutorType.Activate, 35261759, PotOfDesireActivateForContinue);
		AddExecutor(ExecutorType.Activate, 60394026, RyzealPlugInActivateFirst);
		AddExecutor(ExecutorType.Activate, 72238166, NodeRyzealActivateFirst);
		AddExecutor(ExecutorType.Activate, 6798031, RyzealCrossActivateRecycleFirst);
		AddExecutor(ExecutorType.SpSummon, 8633261, IceRyzealSpSummonFirst);
		AddExecutor(ExecutorType.SpSummon, 72238166, NodeRyzealSpSummonFirst);
		AddExecutor(ExecutorType.Summon, 34022970, ExRyzealSummon);
		AddExecutor(ExecutorType.SpSummon, 34022970, ExRyzealSpSummon);
		AddExecutor(ExecutorType.SpSummon, 35844557, ThodeRyzealSpSummon);
		AddExecutor(ExecutorType.Summon, 8633261, IceRyzealSummon);
		AddExecutor(ExecutorType.SpSummon, 34022970, ExRyzealSpSummonLater);
		AddExecutor(ExecutorType.Summon, 35844557, ThodeRyzealSummon);
		AddExecutor(ExecutorType.SpSummon, 72238166, NodeRyzealSpSummon);
		AddExecutor(ExecutorType.Summon, Level4Summon);
		AddExecutor(ExecutorType.Activate, 72238166, NodeRyzealActivate);
		AddExecutor(ExecutorType.Activate, 60394026, RyzealPlugInActivate);
		AddExecutor(ExecutorType.SpSummon, 8633261, IceRyzealSpSummon);
		AddExecutor(ExecutorType.Activate, 9940036, MereologicAggregatorActivateFirst);
		AddExecutor(ExecutorType.Activate, 8633261, IceRyzealActivate);
		AddExecutor(ExecutorType.Activate, 35844557, ThodeRyzealActivate);
		AddExecutor(ExecutorType.Activate, 34022970, ExRyzealActivate);
		AddExecutor(ExecutorType.Activate, 9940036, MereologicAggregatorActivateLater);
		AddExecutor(ExecutorType.Activate, 6798031, RyzealCrossActivateRecycleLater);
		AddExecutor(ExecutorType.Activate, 35261759, PotOfDesiresActivate);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
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

	public bool CheckWhetherHaveFinalMonster()
	{
		ClientCard[] monsterZone = base.Bot.MonsterZone;
		foreach (ClientCard clientCard in monsterZone)
		{
			if (clientCard != null)
			{
				if (clientCard.IsCode(90590303) && clientCard.IsDefense())
				{
					return true;
				}
				if (clientCard.IsCode(21044178) && clientCard.Overlays.Count() > 0)
				{
					return true;
				}
				if (clientCard.IsCode(34909328) && clientCard.Overlays.Count() > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CheckWhetherNegated(bool disablecheck = true, bool toFieldCheck = false, CardType type = (CardType)0, bool ignore41 = false)
	{
		bool flag = type == (CardType)0 && base.Card.IsMonster();
		flag = flag || (type & CardType.Monster) != 0;
		bool num = (type == (CardType)0 && (base.Card.IsSpell() || base.Card.IsTrap())) || (type & CardType.Spell) != 0 || (type & CardType.Trap) != 0;
		bool isCounter = (type & CardType.Counter) != 0;
		if (num && toFieldCheck && CheckSpellWillBeNegate(isCounter))
		{
			return true;
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
			if (infiniteImpermanenceList.Contains(item))
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
		if (base.Duel.Player == 1 && card.IsCode(84192580, 42141493, 87126721))
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
		if (CheckAtAdvantage() && enemyActivateMaxxC && !lockBirdSolved && (base.Duel.Turn == 1 || base.Duel.Phase >= DuelPhase.Main2))
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
		if (lockBirdSolved || (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2))
		{
			return false;
		}
		if (enemyActivatePurulia && (loc & CardLocation.Hand) != 0)
		{
			return true;
		}
		if (enemyActivateFuwalos && (loc & (CardLocation)65) != 0)
		{
			return true;
		}
		if (enemyActivateNyalus && (loc & (CardLocation)48) != 0)
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

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false, bool ignoreCurrentDestroy = false, CardType selfType = (CardType)0)
	{
		ClientCard clientCard = (from card in base.Enemy.GetMonsters()
			where card?.Data != null && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard != null)
		{
			return clientCard;
		}
		ClientCard clientCard2 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard2 != null)
		{
			return clientCard2;
		}
		ClientCard clientCard3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard3 != null)
		{
			return clientCard3;
		}
		ClientCard clientCard4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && card.EquipCards.Count > 0 && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard4 != null)
		{
			return clientCard4;
		}
		ClientCard clientCard5 = (from card in base.Enemy.MonsterZone
			where card != null && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && (card.HasType((CardType)8396992) || (card.HasType(CardType.Link) && card.LinkCount >= 2)) && CheckCanBeTargeted(card, canBeTarget, selfType) && CheckShouldNotIgnore(card)
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard5 != null)
		{
			return clientCard5;
		}
		ClientCard clientCard6 = base.Enemy.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(68468459) && !c.IsDisabled() && !currentDestroyCardList.Contains(c) && !currentNegateCardList.Contains(c) && base.Duel.CurrentChain.Contains(c));
		if (clientCard6 != null)
		{
			return clientCard6;
		}
		if (attack >= 0)
		{
			if (attack == 0)
			{
				attack = base.Util.GetBestAttack(base.Bot);
			}
			ClientCard clientCard7 = (from card in base.Enemy.MonsterZone
				where card != null && card.GetDefensePower() >= attack && card.GetDefensePower() > 0 && card.IsAttack() && CheckCanBeTargeted(card, canBeTarget, selfType) && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
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
			if (!currentDestroyCardList.Contains(cards))
			{
				return !currentNegateCardList.Contains(cards);
			}
			return false;
		}
		return true;
	}

	public bool CheckCanContinueSummon(bool skipDuodriver = false)
	{
		bool flag = summonCount > 0 && !activatedCardIdList.Contains(8633261) && base.Bot.HasInHand(8633261) && !DefaultCheckWhetherCardIdIsNegated(8633261);
		if (base.Bot.HasInHand(35844557) && !spSummonedCardIdList.Contains(35844557) && !activatedCardIdList.Contains(35844557) && !DefaultCheckWhetherCardIdIsNegated(35844557))
		{
			flag |= base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446));
			flag |= base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446));
		}
		flag |= !spSummonedCardIdList.Contains(34022970) && !activatedCardIdList.Contains(34022970) && base.Bot.HasInHand(34022970) && !DefaultCheckWhetherCardIdIsNegated(34022970) && !CheckWhetherWillbeRemoved();
		return flag | (!activatedCardIdList.Contains(7511614) && base.Bot.HasInExtra(7511613) && !DefaultCheckWhetherCardIdIsNegated(7511613) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && summonCount > 0 && base.Bot.Hand.Count((ClientCard c) => c.Level == 4) > 0 && GetLevel4CountOnField() == 1 && !lockBirdSolved && !skipDuodriver);
	}

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(4315) || card.HasSetcode(413))).ToList();
		List<int> dangerMonsterIdList = new List<int> { 99937011, 63542003, 9411399, 28954097, 30680659 };
		list.AddRange(base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => dangerMonsterIdList.Contains(card.Id)));
		return list;
	}

	public List<ClientCard> GetProblematicEnemyCardList(bool canBeTarget = false, bool ignoreSpells = false, CardType selfType = (CardType)0)
	{
		List<ClientCard> resultList = new List<ClientCard>();
		List<ClientCard> list = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !currentDestroyCardList.Contains(card) && card.IsFloodgate() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list.Count > 0)
		{
			resultList.AddRange(list);
		}
		List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && !resultList.Contains(c) && !currentDestroyCardList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && CheckCanBeTargeted(c, canBeTarget, selfType)).ToList();
		if (list2.Count > 0)
		{
			resultList.AddRange(ShuffleList(list2));
		}
		List<ClientCard> list3 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentDestroyCardList.Contains(card) && card.IsMonsterDangerous() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list3.Count > 0 && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			resultList.AddRange(list3);
		}
		List<ClientCard> list4 = (from card in base.Enemy.MonsterZone
			where card?.Data != null && !resultList.Contains(card) && !currentDestroyCardList.Contains(card) && card.IsMonsterInvincible() && card.IsFaceup() && CheckCanBeTargeted(card, canBeTarget, selfType)
			orderby card.Attack descending
			select card).ToList();
		if (list4.Count > 0)
		{
			resultList.AddRange(list4);
		}
		List<ClientCard> list5 = (from card in base.Enemy.GetMonsters()
			where !currentDestroyCardList.Contains(card)
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
			where c.IsFaceup() && !currentDestroyCardList.Contains(c) && c.HasType((CardType)17694720) && CheckCanBeTargeted(c, canBeTarget, selfType) && !NotToDestroySpellTrap.Contains(c.Id)
			select c).ToList();
		if (list6.Count > 0 && !ignoreSpells)
		{
			resultList.AddRange(ShuffleList(list6));
		}
		return resultList;
	}

	public List<ClientCard> GetNormalEnemyTargetList(bool canBeTarget = true, bool ignoreCurrentDestroy = false, CardType selfType = (CardType)0, bool forNegate = false)
	{
		List<ClientCard> targetList = GetProblematicEnemyCardList(canBeTarget, ignoreSpells: false, selfType);
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup() && !targetList.Contains(card) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && (!forNegate || (!card.IsDisabled() && card.HasType(CardType.Effect)))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		targetList.AddRange(list);
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && enemyPlaceThisTurn.Contains(card) && card.IsFacedown()
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && !enemyPlaceThisTurn.Contains(card) && card.IsFacedown()
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetMonsters()
			where card.IsFacedown() && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && (!forNegate || (!card.IsDisabled() && card.HasType(CardType.Effect)))
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

	public List<ClientCard> GetLevel4OnField(Func<ClientCard, bool> filter)
	{
		return (from c in base.Bot.GetMonsters()
			where (filter == null || filter(c)) && c.IsFaceup() && !c.HasType((CardType)75497472) && c.Level == 4
			orderby c.GetDefensePower()
			select c).ToList();
	}

	public int GetLevel4CountOnField()
	{
		return base.Bot.GetMonsters().Count((ClientCard c) => c.IsFaceup() && !c.HasType((CardType)75497472) && c.Level == 4);
	}

	public int GetLevel4FinalCountOnField(bool checkSupport, out bool hasNode)
	{
		int num = GetLevel4CountOnField();
		if (base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970) && !CheckWhetherWillbeRemoved() && (checkSupport || (!activatedCardIdList.Contains(34022970) && base.Bot.MonsterZone.All((ClientCard c) => c != null && (c.IsFacedown() || (!c.HasType(CardType.Link) && c.Level == 4))))))
		{
			num++;
		}
		if (base.Bot.HasInHand(35844557) && !spSummonedCardIdList.Contains(35844557) && (base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446)) | base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446))) && (checkSupport || !activatedCardIdList.Contains(35844557)))
		{
			num++;
		}
		hasNode = base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(72238166) && !c.IsDisabled());
		if (base.Bot.HasInHand(72238166) && !spSummonedCardIdList.Contains(72238166) && (base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz)) | base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz))))
		{
			hasNode = true;
			num++;
		}
		if (base.Bot.HasInHand(60394026) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) && checkSupport)
		{
			List<ClientCard> source = base.Bot.Graveyard.Where((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && !c.HasType(CardType.Xyz) && c.Level == 4).ToList();
			int num2 = 0 | ((source.Count() > 0) ? 1 : 0);
			hasNode |= source.Any((ClientCard c) => c.IsCode(72238166));
			List<ClientCard> source2 = base.Bot.Banished.Where((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && !c.HasType(CardType.Xyz) && c.Level == 4).ToList();
			int num3 = num2 | ((source2.Count() > 0) ? 1 : 0);
			hasNode |= source2.Any((ClientCard c) => c.IsCode(72238166));
			if (num3 != 0)
			{
				num++;
			}
		}
		hasNode &= !CheckWhetherWillbeRemoved() && !activatedCardIdList.Contains(72238166) && !DefaultCheckWhetherCardIdIsNegated(72238166);
		hasNode &= base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsMonster() && c.HasSetcode(446) && !c.IsCode(72238166) && c.Level == 4);
		if (hasNode && base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && !c.HasType(CardType.Xyz) && c.Level == 4 && !c.IsCode(72238166)) && (checkSupport || GetCostFromHandAndField(null, sendNotNecessary: false).Count() > 0))
		{
			num++;
		}
		if (checkSupport)
		{
			int num4 = 2;
			if (summonCount > 0 && base.Bot.Hand.Any((ClientCard c) => c.Level == 4 && !c.IsCode(34022970, 35844557, 72238166)))
			{
				num++;
				num4++;
			}
			if (base.Bot.Hand.Count() >= num4 && base.Bot.HasInHand(8633261) && !spSummonedCardIdList.Contains(8633261) && !CheckWhetherWillbeRemoved())
			{
				num++;
			}
		}
		return num;
	}

	public List<ClientCard> GetCostFromHandAndFieldFirst(ClientCard exceptCard)
	{
		return base.Bot.MonsterZone.Where((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(NeedIceToSolveIdList) && c != exceptCard && !c.HasType(CardType.Token)).ToList();
	}

	public List<ClientCard> GetCostFromHandAndField(ClientCard exceptCard, bool sendNotNecessary)
	{
		List<ClientCard> resultList = GetCostFromHandAndFieldFirst(exceptCard);
		if (!activatedCardIdList.Contains(45852940))
		{
			List<ClientCard> source = base.Bot.Graveyard.Where((ClientCard c) => c.HasType(CardType.Xyz)).ToList();
			ClientCard clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(45852939) && !resultList.Contains(c));
			if (clientCard == null)
			{
				clientCard = base.Bot.SpellZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(45852939) && !resultList.Contains(c));
			}
			if (clientCard != null)
			{
				int num = 0;
				foreach (int overlay in clientCard.Overlays)
				{
					NamedCard namedCard = NamedCard.Get(overlay);
					if (namedCard != null && namedCard.HasType(CardType.Xyz))
					{
						num++;
					}
				}
				if ((num >= 2) | (num > 0 && source.Count() > 0) | (source.Count() > 1 && source.Count((ClientCard c) => c.IsCanRevive()) > 0))
				{
					resultList.Add(clientCard);
				}
			}
		}
		if (base.Bot.HasInSpellZone(6798031, notDisabled: true, faceUp: true))
		{
			if (base.Bot.HasInExtra(7511613) && !activatedCardIdList.Contains(7511614) && !lockBirdSolved)
			{
				bool flag = true;
				ClientCard clientCard2 = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(7511613) && !resultList.Contains(c));
				if (clientCard2 == null)
				{
					flag = false;
					clientCard2 = base.Bot.SpellZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(7511613) && !resultList.Contains(c));
				}
				if (clientCard2 != null)
				{
					int num2 = base.Bot.MonsterZone.Sum((ClientCard c) => c?.Overlays.Count() ?? 0);
					if (!flag || num2 < 2)
					{
						resultList.Add(clientCard2);
					}
				}
			}
			if (base.Bot.HasInExtra(34909328))
			{
				ClientCard clientCard3 = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsCode(34909328) && c.Overlays.Count() == 0 && !resultList.Contains(c));
				if (clientCard3 != null)
				{
					resultList.Add(clientCard3);
				}
			}
		}
		List<ClientCard> collection = base.Bot.SpellZone.Where((ClientCard c) => c != null && c.Data != null && c.Data.HasType(CardType.Monster) && !c.Data.HasType((CardType)16793600) && !resultList.Contains(c)).ToList();
		resultList.AddRange(collection);
		List<ClientCard> collection2 = base.Bot.MonsterZone.Where((ClientCard c) => c != null && !resultList.Contains(c) && c.Owner == 1).ToList();
		resultList.AddRange(collection2);
		if (sendNotNecessary)
		{
			List<ClientCard> collection3 = (from c in base.Bot.MonsterZone
				where c != null && c.HasType(CardType.Xyz) && c.GetDefensePower() < 2500 && c.Overlays.Count() == 0 && !resultList.Contains(c)
				orderby c.GetDefensePower()
				select c).ToList();
			resultList.AddRange(collection3);
			List<int> unimportantList = new List<int> { 87126721, 84192580, 42141493, 7477101 };
			resultList.AddRange(base.Bot.Hand.Where((ClientCard c) => c != null && c.IsCode(unimportantList) && !resultList.Contains(c)));
			foreach (int checkId in new List<int> { 8633261, 35844557, 34022970 })
			{
				if (summonCount == 0 && spSummonedCardIdList.Contains(checkId))
				{
					List<ClientCard> collection4 = base.Bot.Hand.Where((ClientCard c) => c != null && c != exceptCard && !resultList.Contains(c) && c.IsCode(checkId)).ToList();
					resultList.AddRange(collection4);
				}
			}
			foreach (ClientCard card in base.Bot.Hand)
			{
				if (base.Bot.Hand.Count((ClientCard c) => c != null && !resultList.Contains(c) && c.IsCode(card.Id)) > 1)
				{
					resultList.Add(card);
				}
			}
			if (resultList.Count() == 0)
			{
				foreach (int checkId2 in new List<int> { 65681983, 24224830, 10045474, 59438930, 94145021, 14558127, 23434538 })
				{
					List<ClientCard> collection5 = base.Bot.Hand.Where((ClientCard c) => c != null && c != exceptCard && !resultList.Contains(c) && c.IsCode(checkId2)).ToList();
					resultList.AddRange(collection5);
				}
			}
		}
		return resultList;
	}

	public int GetBotCurrentTotalAttack(List<ClientCard> exceptList = null)
	{
		if (base.Util.IsTurn1OrMain2() || botSolvedCardIdList.Contains(46772449))
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

	public int GetNegateEffectCount()
	{
		return 0 + ((GetCalledbytheGraveIdCount(23434538) < 2 && base.Bot.HasInHand(23434538)) ? 1 : 0) + ((GetCalledbytheGraveIdCount(14558127) < 2 && base.Bot.HasInHand(14558127)) ? 1 : 0) + ((GetCalledbytheGraveIdCount(97268402) < 2 && base.Bot.HasInHand(97268402)) ? 1 : 0) + ((GetCalledbytheGraveIdCount(59438930) < 2 && base.Bot.HasInHand(59438930)) ? 1 : 0) + ((GetCalledbytheGraveIdCount(94145021) < 2 && base.Bot.HasInHand(94145021)) ? 1 : 0) + base.Bot.SpellZone.Count((ClientCard c) => c != null && c.IsFacedown() && c.IsCode(10045474)) + Math.Min(4 - base.Bot.GetSpellCountWithoutField(), base.Bot.Hand.Count((ClientCard c) => c.IsCode(10045474)));
	}

	public override BattlePhaseAction OnBattle(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		if (attackers.Count() > 0 && defenders.Count() > 0)
		{
			List<ClientCard> list = attackers.OrderBy((ClientCard card) => card.Attack).ToList();
			ClientCard clientCard = attackers.FirstOrDefault((ClientCard c) => c.IsCode(1269512) && !c.IsDisabled());
			if (clientCard != null)
			{
				list.Remove(clientCard);
				list.Insert(0, clientCard);
			}
			for (int num = 0; num < list.Count; num++)
			{
				ClientCard clientCard2 = list[num];
				clientCard2.IsLastAttacker = num == list.Count - 1;
				BattlePhaseAction battlePhaseAction = OnSelectAttackTarget(clientCard2, defenders);
				if (battlePhaseAction != null)
				{
					return battlePhaseAction;
				}
			}
		}
		return base.OnBattle(attackers, defenders);
	}

	public override ClientCard OnSelectAttacker(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		ClientCard clientCard = attackers.FirstOrDefault((ClientCard c) => c.IsCode(45852939) && !c.IsDisabled());
		if (clientCard != null && base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.GetDefensePower() <= 2500))
		{
			return clientCard;
		}
		return null;
	}

	public override void OnSelectChain(IList<ClientCard> cards)
	{
		if (cards != null && cards.Count() > 0)
		{
			currentCanActivateEffect.Clear();
			currentCanActivateEffect.AddRange(cards);
		}
		base.OnSelectChain(cards);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		ClientCard clientCard2;
		ClientCard clientCard3;
		int num;
		int num2;
		if (currentSolvingChainInfo != null)
		{
			if (botSolvingCross)
			{
				if (hint == 532)
				{
					foreach (Func<ClientCard, bool> func in new List<Func<ClientCard, bool>>
					{
						(ClientCard c) => c.IsDisabled() && c.IsCode(7511613),
						(ClientCard c) => c.IsCode(7511613),
						(ClientCard c) => c.IsDisabled() && c.IsCode(34909328),
						(ClientCard c) => c.IsCode(34909328)
					})
					{
						ClientCard clientCard = cards.FirstOrDefault((ClientCard c) => c != null && func(c));
						if (clientCard != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard }, cards, min, max);
						}
					}
				}
				if (hint == 519)
				{
					List<ClientCard> selected = cards.OrderBy((ClientCard c) => c.Attack).ToList();
					botSolvingCross = false;
					return base.Util.CheckSelectCount(selected, cards, min, max);
				}
			}
			if (currentSolvingChainInfo.ActivatePlayer == 1 && currentSolvingChainInfo.IsCode(15693423))
			{
				Logger.DebugWriteLine("=== Evenly Matched activated.");
				List<ClientCard> banishList = new List<ClientCard>();
				List<ClientCard> source = (from clientCard28 in base.Bot.GetMonsters()
					where !clientCard28.HasType(CardType.Token)
					select clientCard28).ToList();
				List<ClientCard> collection = source.Where((ClientCard clientCard28) => clientCard28.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list = source.Where((ClientCard clientCard28) => !banishList.Contains(clientCard28) && CheckRemainInDeck(clientCard28.Id) > 0).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list);
				bool canUsePluginToSpSummonDeadnader = base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsCanRevive() && c.IsCode(34909328));
				canUsePluginToSpSummonDeadnader |= base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(34909328));
				List<ClientCard> list2 = (from c in base.Bot.GetSpells()
					where c.IsFaceup()
					select c).ToList();
				banishList.AddRange(ShuffleList(list2));
				List<ClientCard> list3 = (from c in base.Bot.GetSpells()
					where c.IsFacedown() && (!canUsePluginToSpSummonDeadnader || !c.IsCode(60394026))
					select c).ToList();
				banishList.AddRange(ShuffleList(list3));
				List<ClientCard> list4 = source.Where((ClientCard clientCard28) => !banishList.Contains(clientCard28) && !clientCard28.HasType((CardType)75505728) && CheckRemainInDeck(clientCard28.Id) == 0).ToList();
				list4.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list4);
				List<ClientCard> list5 = source.Where((ClientCard clientCard28) => !banishList.Contains(clientCard28) && clientCard28.HasType((CardType)75505728) && base.Bot.HasInExtra(clientCard28.Id)).ToList();
				list5.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list5);
				List<ClientCard> list6 = source.Where((ClientCard clientCard28) => !banishList.Contains(clientCard28) && clientCard28.HasType((CardType)75505728) && !base.Bot.HasInExtra(clientCard28.Id)).ToList();
				list6.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list6);
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
			if (currentSolvingChainInfo.ActivatePlayer == 0)
			{
				if (hint == 506)
				{
					if (currentSolvingChainInfo.IsCode(35844557))
					{
						clientCard2 = cards.FirstOrDefault((ClientCard c) => c.IsCode(8633261));
						clientCard3 = cards.FirstOrDefault((ClientCard c) => c.IsCode(34022970));
						if (clientCard2 != null)
						{
							if (base.Duel.Player == 0 && summonCount > 0)
							{
								num = ((base.Duel.Phase < DuelPhase.End) ? 1 : 0);
								if (num != 0 && !base.Bot.HasInHand(8633261) && !activatedCardIdList.Contains(8633261))
								{
									num2 = ((!DefaultCheckWhetherCardIdIsNegated(8633261)) ? 1 : 0);
									goto IL_04ff;
								}
							}
							else
							{
								num = 0;
							}
							num2 = 0;
							goto IL_04ff;
						}
						goto IL_05a8;
					}
					goto IL_061b;
				}
				goto IL_0bd9;
			}
			goto IL_1545;
		}
		goto IL_17ea;
		IL_1545:
		if ((hint == 507 || hint == 504 || hint == 501) && cards.All((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.Hand))
		{
			List<ClientCard> resultList = new List<ClientCard>();
			foreach (int code in new List<int> { 87126721, 84192580, 42141493, 7477101 })
			{
				List<ClientCard> list7 = cards.Where((ClientCard c) => c.IsCode(code) && !resultList.Contains(c)).ToList();
				if (list7.Count() > 0)
				{
					resultList.AddRange(list7);
				}
			}
			foreach (ClientCard card in cards)
			{
				if (!resultList.Contains(card) && cards.Where((ClientCard c) => c.IsCode(card.Id) && !resultList.Contains(c)).Count() > 1)
				{
					resultList.Add(card);
				}
			}
			foreach (int code2 in new List<int>
			{
				97268402, 10045474, 59438930, 25311006, 72238166, 94145021, 60394026, 65681983, 24224830, 6798031,
				35844557, 34022970, 8633261
			})
			{
				ClientCard clientCard4 = cards.FirstOrDefault((ClientCard c) => c.IsCode(code2) && !resultList.Contains(c));
				if (clientCard4 != null)
				{
					resultList.Add(clientCard4);
				}
			}
			return base.Util.CheckSelectCount(resultList, cards, min, max);
		}
		goto IL_17ea;
		IL_061b:
		if (currentSolvingChainInfo.IsCode(34022970))
		{
			ClientCard clientCard5 = cards.FirstOrDefault((ClientCard c) => c.IsCode(35844557));
			ClientCard clientCard6 = cards.FirstOrDefault((ClientCard c) => c.IsCode(72238166));
			if (clientCard5 != null && ((clientCard6 == null) | (!base.Bot.HasInHand(35844557) && !spSummonedCardIdList.Contains(35844557) && !activatedCardIdList.Contains(35844557))))
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard5 }, cards, min, max);
			}
			if (clientCard6 != null && ((clientCard5 == null) | (spSummonedCardIdList.Contains(35844557) && activatedCardIdList.Contains(35844557) && !DefaultCheckWhetherCardIdIsNegated(35844557)) | (CheckShouldNoMoreSpSummon(CardLocation.Hand) && !CheckShouldNoMoreSpSummon(CardLocation.Grave) && !spSummonedCardIdList.Contains(72238166))))
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
			}
		}
		if (currentSolvingChainInfo.IsCode(85106525) || currentSolvingChainInfo.IsCode(7477101))
		{
			if (!base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970) && !CheckWhetherWillbeRemoved())
			{
				ClientCard clientCard7 = cards.FirstOrDefault((ClientCard c) => c.IsCode(34022970));
				if (clientCard7 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard7 }, cards, min, max);
				}
			}
			if (!base.Bot.HasInHand(8633261) && ((summonCount > 0 && !activatedCardIdList.Contains(8633261)) | (!spSummonedCardIdList.Contains(8633261) && base.Bot.Hand.Count > 0)))
			{
				ClientCard clientCard8 = cards.FirstOrDefault((ClientCard c) => c.IsCode(8633261));
				if (clientCard8 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard8 }, cards, min, max);
				}
			}
			foreach (int targetId in new List<int> { 34022970, 8633261 })
			{
				ClientCard clientCard9 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId));
				if (clientCard9 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard9 }, cards, min, max);
				}
			}
		}
		if (currentSolvingChainInfo.IsCode(7511613))
		{
			if (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
			{
				ClientCard clientCard10 = cards.FirstOrDefault((ClientCard c) => c.IsCode(6798031));
				if (clientCard10 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard10 }, cards, min, max);
				}
				ClientCard clientCard11 = cards.FirstOrDefault((ClientCard c) => c.IsCode(60394026));
				if (clientCard11 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard11 }, cards, min, max);
				}
			}
			foreach (KeyValuePair<int, Func<bool>> pair in new List<KeyValuePair<int, Func<bool>>>
			{
				new KeyValuePair<int, Func<bool>>(8633261, () => base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && c.IsCode(NeedIceToSolveIdList)) && !spSummonedCardIdList.Contains(8633261) && !CheckWhetherWillbeRemoved()),
				new KeyValuePair<int, Func<bool>>(34022970, () => !spSummonedCardIdList.Contains(34022970) && !activatedCardIdList.Contains(34022970) && !DefaultCheckWhetherCardIdIsNegated(34022970) && !CheckWhetherWillbeRemoved()),
				new KeyValuePair<int, Func<bool>>(8633261, () => summonCount > 0 && !activatedCardIdList.Contains(8633261) && !DefaultCheckWhetherCardIdIsNegated(8633261)),
				new KeyValuePair<int, Func<bool>>(35844557, () => !spSummonedCardIdList.Contains(35844557) && !activatedCardIdList.Contains(35844557) && !DefaultCheckWhetherCardIdIsNegated(35844557)),
				new KeyValuePair<int, Func<bool>>(72238166, () => !spSummonedCardIdList.Contains(72238166))
			})
			{
				if (!base.Bot.HasInHand(pair.Key) && pair.Value())
				{
					ClientCard clientCard12 = cards.FirstOrDefault((ClientCard c) => c.IsCode(pair.Key));
					if (clientCard12 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard12 }, cards, min, max);
					}
				}
			}
			foreach (int id in new List<int> { 34022970, 8633261, 35844557, 72238166 })
			{
				if (!base.Bot.HasInHand(id))
				{
					ClientCard clientCard13 = cards.FirstOrDefault((ClientCard c) => c.IsCode(id));
					if (clientCard13 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard13 }, cards, min, max);
					}
				}
			}
			return base.Util.CheckSelectCount(ShuffleList(cards.ToList()), cards, min, max);
		}
		goto IL_0bd9;
		IL_0bd9:
		if (hint == 509)
		{
			if (currentSolvingChainInfo.IsCode(8633261))
			{
				ClientCard clientCard14 = cards.FirstOrDefault((ClientCard c) => c.IsCode(35844557));
				ClientCard clientCard15 = cards.FirstOrDefault((ClientCard c) => c.IsCode(34022970));
				ClientCard clientCard16 = cards.FirstOrDefault((ClientCard c) => c.IsCode(72238166));
				if (clientCard14 != null && (((!activatedCardIdList.Contains(35844557) && !DefaultCheckWhetherCardIdIsNegated(35844557)) | (base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970))) || (clientCard15 == null && clientCard16 == null)))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard14 }, cards, min, max);
				}
				if (clientCard15 != null && (((!activatedCardIdList.Contains(34022970) && !DefaultCheckWhetherCardIdIsNegated(34022970)) | (base.Bot.HasInHand(35844557) && !spSummonedCardIdList.Contains(35844557))) || (clientCard14 == null && clientCard16 == null)))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard15 }, cards, min, max);
				}
				if (clientCard16 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard16 }, cards, min, max);
				}
			}
			if (currentSolvingChainInfo.IsCode(45852939))
			{
				ClientCard item = TwinsOfTheEclipseRebornTarget(new List<ClientCard>(cards));
				return base.Util.CheckSelectCount(new List<ClientCard> { item }, cards, min, max);
			}
		}
		if (hint == 507 && currentSolvingChainInfo.IsCode(25311006))
		{
			foreach (ClientCard card4 in cards)
			{
				foreach (int checkSetcode in CheckSetcodeList)
				{
					if (card4.HasSetcode(checkSetcode))
					{
						enemyDeckTypeRecord.Add(checkSetcode);
					}
				}
			}
			return base.Util.CheckSelectCount(ShuffleList(cards.ToList()), cards, min, max);
		}
		if (hint == 513 && currentSolvingChainInfo.IsCode(34909328, 7511613, 60394026))
		{
			ClientCard clientCard17 = cards.FirstOrDefault((ClientCard c) => c.IsCode(45852939, 9940036));
			if (clientCard17 != null)
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard17 }, cards, min, max);
			}
			foreach (ClientCard card2 in cards)
			{
				if (base.Bot.Hand.Count((ClientCard c) => c.IsCode(card2.Id)) > 0)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { card2 }, cards, min, max);
				}
			}
			foreach (ClientCard card3 in cards)
			{
				if (cards.Count((ClientCard c) => c.IsCode(card3.Id)) > 1)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { card3 }, cards, min, max);
				}
			}
		}
		if (hint == 519 && currentSolvingChainInfo.IsCode(7511613))
		{
			List<ClientCard> resultList2 = new List<ClientCard>();
			foreach (int ownerId in new List<int> { 16643334, 7511613, 45852939, 1269512, 46772449, 66011101, 34909328 })
			{
				List<ClientCard> list8 = cards.Where((ClientCard c) => c.OwnTargets.Any((ClientCard oc) => oc.IsCode(ownerId))).ToList();
				if (list8.Count() <= 0)
				{
					continue;
				}
				ClientCard clientCard18 = list8.FirstOrDefault((ClientCard c) => c.IsCode(34909328));
				if (clientCard18 != null)
				{
					resultList2.Add(clientCard18);
				}
				foreach (Func<ClientCard, bool> item2 in new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => !resultList2.Contains(c) && !c.IsCode(9940036, 45852939) && base.Bot.HasInSpellZone(6798031) && c.HasSetcode(446) && (c.Data == null || (c.Data.Attribute & 0x30) != 0),
					(ClientCard c) => !resultList2.Contains(c) && !c.IsCode(9940036, 45852939) && base.Bot.HasInSpellZone(6798031) && c.HasSetcode(446),
					(ClientCard c) => !resultList2.Contains(c) && !c.IsCode(9940036, 45852939) && (c.Data == null || (c.Data.Attribute & 0x30) != 0),
					(ClientCard c) => !resultList2.Contains(c) && !c.IsCode(9940036, 45852939),
					(ClientCard c) => !resultList2.Contains(c)
				})
				{
					foreach (ClientCard item3 in list8)
					{
						if (item2(item3))
						{
							resultList2.Add(item3);
						}
					}
				}
			}
			return base.Util.CheckSelectCount(resultList2, cards, min, max);
		}
		if (currentSolvingChainInfo.IsCode(60394026) && cards.All((ClientCard c) => c.Location == CardLocation.MonsterZone))
		{
			ClientCard clientCard19 = cards.FirstOrDefault((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(21044178) && c.Overlays.Count() < 2);
			if (clientCard19 != null && AbyssDwellerSummonCheck())
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard19 }, cards, min, max);
			}
			ClientCard clientCard20 = cards.FirstOrDefault((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(7511613) && c.Overlays.Count() == 1);
			if (clientCard20 != null && base.Bot.HasInMonstersZone(16643334, notDisabled: true, hasXyzMaterial: false, faceUp: true))
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard20 }, cards, min, max);
			}
			ClientCard clientCard21 = cards.FirstOrDefault((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(34909328));
			if (clientCard21 != null)
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard21 }, cards, min, max);
			}
			if (base.Bot.HasInSpellZone(6798031, notDisabled: true, faceUp: true))
			{
				ClientCard clientCard22 = cards.FirstOrDefault((ClientCard c) => c?.HasSetcode(446) ?? false);
				if (clientCard22 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard22 }, cards, min, max);
				}
			}
			ClientCard clientCard23 = cards.FirstOrDefault((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(6983839) && c.Overlays.Count() == 1);
			if (clientCard23 != null && TornadoDragonSummonCheck())
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard23 }, cards, min, max);
			}
			ClientCard clientCard24 = cards.FirstOrDefault((ClientCard c) => c?.IsCode(90590303) ?? false);
			if (clientCard24 != null && Number41BagooskatheTerriblyTiredTapirSummonCheck())
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard24 }, cards, min, max);
			}
			clientCard20 = cards.FirstOrDefault((ClientCard c) => c != null && !c.IsDisabled() && c.IsCode(7511613));
			if (clientCard20 != null)
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard20 }, cards, min, max);
			}
		}
		if (currentSolvingChainInfo.IsCode(66011101) && cards.All((ClientCard c) => c.Location == CardLocation.MonsterZone))
		{
			ClientCard clientCard25 = (from c in cards
				where c != null && (c.HasPosition(CardPosition.FaceUpAttack) || !summonThisTurn.Contains(c))
				orderby c.Attack descending
				select c).FirstOrDefault();
			if (clientCard25 != null)
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard25 }, cards, min, max);
			}
		}
		goto IL_1545;
		IL_04ff:
		bool flag = (byte)num2 != 0;
		flag = flag || clientCard3 == null;
		flag |= base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && c.IsCode(NeedIceToSolveIdList)) && !spSummonedCardIdList.Contains(8633261) && !CheckWhetherWillbeRemoved();
		if (num == 0)
		{
			flag |= DefaultCheckWhetherCardIdIsNegated(34022970);
			flag |= spSummonedCardIdList.Contains(34022970) || activatedCardIdList.Contains(34022970);
		}
		if (flag)
		{
			return base.Util.CheckSelectCount(new List<ClientCard> { clientCard2 }, cards, min, max);
		}
		goto IL_05a8;
		IL_17ea:
		if (currentSolvingChainInfo == null)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null)
			{
				if (lastChainCard.Controller == 0 && lastChainCard.IsCode(34909328))
				{
					switch (hint)
					{
					case 519:
					{
						if (deadnaderDestroySelf != null)
						{
							ClientCard clientCard26 = cards.FirstOrDefault((ClientCard c) => c.IsCode(9940036, 45852939));
							if (clientCard26 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard26 }, cards, min, max);
							}
						}
						List<ClientCard> selected2 = cards.OrderBy((ClientCard c) => c.Attack).ToList();
						return base.Util.CheckSelectCount(selected2, cards, min, max);
					}
					case 502:
						if (deadnaderDestroySelf != null && cards.Contains(deadnaderDestroySelf))
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { deadnaderDestroySelf }, cards, min, max);
						}
						foreach (ClientCard item4 in CanDestroyList())
						{
							if (cards.Contains(item4))
							{
								currentDestroyCardList.Add(item4);
								return base.Util.CheckSelectCount(new List<ClientCard> { item4 }, cards, min, max);
							}
						}
						break;
					}
				}
				if (hint == 519 && base.Bot.HasInHandOrInSpellZone(60394026))
				{
					foreach (int checkId in new List<int> { 72238166, 35844557, 34022970 })
					{
						if (!activatedCardIdList.Contains(checkId))
						{
							ClientCard clientCard27 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId));
							if (clientCard27 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard27 }, cards, min, max);
							}
						}
					}
				}
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
		IL_05a8:
		if (clientCard3 != null && (((!base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970) && !activatedCardIdList.Contains(34022970)) | base.Bot.HasInHand(8633261)) || clientCard2 == null))
		{
			return base.Util.CheckSelectCount(new List<ClientCard> { clientCard3 }, cards, min, max);
		}
		goto IL_061b;
	}

	public override bool OnSelectHand()
	{
		HashSet<int> tenpaiList = new HashSet<int> { 426, 425 };
		return enemyDeckTypeRecord.Count() <= 0 || !enemyDeckTypeRecord.All((int c) => tenpaiList.Contains(c));
	}

	public override int OnSelectOption(IList<int> options)
	{
		bool flag = false;
		for (int i = 0; i < 3; i++)
		{
			if (options.Contains(base.Util.GetStringId(25311006, i)))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return TripleTacticsTalentDecision(options);
		}
		bool flag2 = false;
		for (int j = 0; j < 3; j++)
		{
			if (options.Contains(base.Util.GetStringId(66011101, j)))
			{
				flag2 = true;
				break;
			}
		}
		if (flag2)
		{
			if (options.Contains(base.Util.GetStringId(66011101, 2)) && Number60DugaresTheTimelessDoubleTarget() != null)
			{
				int num = options.IndexOf(base.Util.GetStringId(66011101, 2));
				if (num >= 0)
				{
					return num;
				}
			}
			if (options.Contains(base.Util.GetStringId(66011101, 0)) && Number60DugaresTheTimelessDrawEffect())
			{
				int num2 = options.IndexOf(base.Util.GetStringId(66011101, 0));
				if (num2 >= 0)
				{
					return num2;
				}
			}
			if (options.Contains(base.Util.GetStringId(66011101, 1)) && Number60DugaresTheTimelessRebornEffect())
			{
				int num3 = options.IndexOf(base.Util.GetStringId(66011101, 1));
				if (num3 >= 0)
				{
					return num3;
				}
			}
		}
		base.Duel.GetCurrentSolvingChainCard();
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
				if ((available & num) != 0 && base.Bot.MonsterZone[item] == null)
				{
					return num;
				}
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == base.Util.GetStringId(60394026, 1))
		{
			Logger.DebugWriteLine("** plugin set material");
			return true;
		}
		return base.OnSelectYesNo(desc);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		if (cardId == 90590303 && (base.Util.IsTurn1OrMain2() || base.Duel.Player == 1))
		{
			return CardPosition.FaceUpDefence;
		}
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
			int num = Math.Max(base.Util.GetBestAttack(base.Bot), attack);
			if (base.Bot.HasInExtra(66011101) && GetLevel4FinalCountOnField(checkSupport: true, out var _) >= 2)
			{
				num *= 2;
			}
			if (base.Util.IsAllEnemyBetterThanValue(num, onlyATK: true))
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
			maxSummonCount = 1;
			hardToDestroyCardList.Clear();
			cannotDestroyCardList.Clear();
		}
		summonCount = maxSummonCount;
		enemyActivateMaxxC = false;
		enemyActivatePurulia = false;
		enemyActivateFuwalos = false;
		enemyActivateNyalus = false;
		lockBirdSolved = false;
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		enemyActivateInfiniteImpermanenceFromHand = false;
		botActivateMulcharmy = false;
		deadnaderDestroySelf = null;
		botSolvingCross = false;
		infiniteImpermanenceList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		activatedCardIdList.Clear();
		spSummonedCardIdList.Clear();
		botSolvedCardIdList.Clear();
		enemyPlaceThisTurn.Clear();
		summonThisTurn.Clear();
		currentCanActivateEffect.Clear();
		base.OnNewTurn();
	}

	public override void OnChaining(int player, ClientCard card)
	{
		base.Duel.LastChainTargets.Clear();
		if (card == null)
		{
			return;
		}
		if (player == 1)
		{
			if (card.IsCode(10045474))
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
			if (card.HasSetcode(365))
			{
				enemyDeckTypeRecord.Add(365);
			}
		}
		if (player == 0 && card.IsCode(84192580, 42141493, 87126721))
		{
			botActivateMulcharmy = true;
		}
		base.OnChaining(player, card);
	}

	public override void OnChainSolved(int chainIndex)
	{
		botSolvingCross = false;
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null && !base.Duel.IsCurrentSolvingChainNegated() && !base.Duel.IsCurrentSolvingChainNegated())
		{
			if (currentSolvingChainInfo.IsCode(94145021))
			{
				lockBirdSolved = true;
			}
			if (currentSolvingChainInfo.IsCode(91800273))
			{
				dimensionShifterCount = 2;
			}
			if (currentSolvingChainInfo.ActivatePlayer == 1)
			{
				if (currentSolvingChainInfo.IsCode(23434538))
				{
					enemyActivateMaxxC = true;
				}
				if (currentSolvingChainInfo.IsCode(84192580))
				{
					enemyActivatePurulia = true;
				}
				if (currentSolvingChainInfo.IsCode(42141493))
				{
					enemyActivateFuwalos = true;
				}
				if (currentSolvingChainInfo.IsCode(87126721))
				{
					enemyActivateNyalus = true;
				}
			}
			if (currentSolvingChainInfo.ActivatePlayer == 0)
			{
				foreach (int checkBotSolved in CheckBotSolvedList)
				{
					if (currentSolvingChainInfo.IsCode(checkBotSolved))
					{
						botSolvedCardIdList.Add(checkBotSolved);
					}
				}
			}
		}
		base.OnChainSolved(chainIndex);
	}

	public override void OnChainEnd()
	{
		for (int num = cannotDestroyCardList.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard = cannotDestroyCardList[num];
			if (clientCard == null || !clientCard.IsOnField())
			{
				cannotDestroyCardList.RemoveAt(num);
			}
		}
		for (int num2 = hardToDestroyCardList.Count - 1; num2 >= 0; num2--)
		{
			ClientCard clientCard2 = hardToDestroyCardList[num2];
			if (clientCard2 == null || !clientCard2.IsOnField())
			{
				hardToDestroyCardList.RemoveAt(num2);
			}
		}
		foreach (ClientCard currentDestroyCard in currentDestroyCardList)
		{
			if (currentDestroyCard != null && currentDestroyCard.IsOnField())
			{
				if (hardToDestroyCardList.Contains(currentDestroyCard))
				{
					cannotDestroyCardList.Add(currentDestroyCard);
				}
				else
				{
					hardToDestroyCardList.Add(currentDestroyCard);
				}
			}
		}
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		currentCanActivateEffect.Clear();
		enemyActivateInfiniteImpermanenceFromHand = false;
		botSolvingCross = false;
		deadnaderDestroySelf = null;
		for (int num3 = enemyPlaceThisTurn.Count - 1; num3 >= 0; num3--)
		{
			ClientCard clientCard3 = enemyPlaceThisTurn[num3];
			if (clientCard3 == null || !clientCard3.IsOnField())
			{
				enemyPlaceThisTurn.RemoveAt(num3);
			}
		}
		base.OnChainEnd();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (card != null)
		{
			if (previousControler == 1 && card.IsCode(10045474) && previousLocation == 2 && currentLocation == 8)
			{
				enemyActivateInfiniteImpermanenceFromHand = true;
			}
			if (card.Owner == 1)
			{
				foreach (int checkSetcode in CheckSetcodeList)
				{
					if (card.HasSetcode(checkSetcode))
					{
						enemyDeckTypeRecord.Add(checkSetcode);
					}
				}
				if (card.IsCode(AlbazFusionList))
				{
					enemyDeckTypeRecord.Add(349);
				}
			}
			if (currentControler == 1 && (currentLocation == 4 || currentLocation == 8))
			{
				enemyPlaceThisTurn.Add(card);
			}
			if (previousControler == 0 && previousLocation == 4 && currentLocation != 4 && summonThisTurn.Contains(card))
			{
				summonThisTurn.Remove(card);
			}
			if (currentControler == 0 && currentLocation == 4)
			{
				summonThisTurn.Add(card);
			}
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	public override void OnSpSummoned()
	{
		if (base.Duel.GetCurrentSolvingChainCard() != null)
		{
			return;
		}
		foreach (ClientCard lastSummonedCard in base.Duel.LastSummonedCards)
		{
			if (lastSummonedCard.Controller == 0 && lastSummonedCard.IsCode(8633261, 35844557, 72238166, 34022970))
			{
				spSummonedCardIdList.Add(lastSummonedCard.GetOriginCode());
			}
		}
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

	public bool IceRyzealSpSummonFirst()
	{
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) | (GetLevel4CountOnField() >= 2)))
		{
			return false;
		}
		List<ClientCard> costFromHandAndFieldFirst = GetCostFromHandAndFieldFirst(base.Card);
		if (costFromHandAndFieldFirst.Count() > 0)
		{
			base.AI.SelectCard(costFromHandAndFieldFirst);
			return true;
		}
		return false;
	}

	public bool IceRyzealSpSummon()
	{
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) | (GetLevel4CountOnField() >= 2)))
		{
			return false;
		}
		if (base.Card.Level != 4)
		{
			return false;
		}
		if (summonCount <= 0 && GetLevel4CountOnField() == 1)
		{
			List<ClientCard> costFromHandAndField = GetCostFromHandAndField(base.Card, sendNotNecessary: false);
			if (costFromHandAndField.Count() > 0)
			{
				base.AI.SelectCard(costFromHandAndField);
				return true;
			}
			if (base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled() && c.IsFloodgate()) || !CheckWhetherHaveFinalMonster())
			{
				List<ClientCard> costFromHandAndField2 = GetCostFromHandAndField(base.Card, sendNotNecessary: true);
				if (costFromHandAndField2.Count() > 0)
				{
					base.AI.SelectCard(costFromHandAndField2);
					return true;
				}
			}
		}
		return false;
	}

	public bool IceRyzealSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true))
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) | (GetLevel4CountOnField() >= 2)))
		{
			return false;
		}
		summonCount--;
		return true;
	}

	public bool IceRyzealActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon(CardLocation.Deck) && GetLevel4CountOnField() >= 2)
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool ThodeRyzealSpSummon()
	{
		int level4CountOnField = GetLevel4CountOnField();
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && ((base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) || level4CountOnField >= 2) | (level4CountOnField == 1 && summonCount > 0)))
		{
			return false;
		}
		bool flag = level4CountOnField == 1;
		flag |= !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && CheckRemainInDeck(8633261, 34022970) > 0 && !activatedCardIdList.Contains(35844557) && !lockBirdSolved;
		if (GetLevel4CountOnField() == 0)
		{
			flag |= GetLevel4FinalCountOnField(checkSupport: true, out var _) >= 2 && !CheckWhetherHaveFinalMonster();
		}
		return flag;
	}

	public bool ThodeRyzealSummon()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Extra))
		{
			int level4CountOnField = GetLevel4CountOnField();
			if (level4CountOnField == 1 && (!activatedCardIdList.Contains(35844557) & (!base.Bot.HasInHand(34022970) || activatedCardIdList.Contains(34022970))))
			{
				summonCount--;
				return true;
			}
			if (base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) || level4CountOnField >= 2)
			{
				return false;
			}
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		summonCount--;
		return true;
	}

	public bool ThodeRyzealActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool NodeRyzealSpSummon()
	{
		int level4CountOnField = GetLevel4CountOnField();
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && ((base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) || level4CountOnField >= 2) | (level4CountOnField == 1 && summonCount > 0)))
		{
			return false;
		}
		return (level4CountOnField == 1) | (!CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Bot.Graveyard.Any((ClientCard c) => !c.HasType(CardType.Xyz) && c.HasSetcode(446) && c.Level == 4));
	}

	public bool NodeRyzealSpSummonFirst()
	{
		int level4CountOnField = GetLevel4CountOnField();
		if (CheckShouldNoMoreSpSummon((CardLocation)67) && ((base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.HasType(CardType.Xyz)) || level4CountOnField >= 2) | (level4CountOnField == 1 && summonCount > 0)))
		{
			return false;
		}
		if (!activatedCardIdList.Contains(base.Card.Id) && GetCostFromHandAndField(base.Card, sendNotNecessary: false).Count() > 0)
		{
			return true;
		}
		return false;
	}

	public bool NodeRyzealActivate()
	{
		if (NodeRyzealActivateInner(sendNotNessary: true))
		{
			return true;
		}
		return false;
	}

	public bool NodeRyzealActivateFirst()
	{
		if (NodeRyzealActivateInner(sendNotNessary: false))
		{
			return true;
		}
		return false;
	}

	public bool NodeRyzealActivateInner(bool sendNotNessary)
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon((CardLocation)80) && base.Bot.GetMonsters().Count((ClientCard c) => c.HasType(CardType.Xyz) && c.IsFaceup()) > 0)
		{
			return false;
		}
		ClientCard nonLightDarkTarget = (from c in base.Bot.Graveyard
			where c != null && !c.HasType(CardType.Xyz) && c.HasSetcode(446) && c.Level == 4 && !c.HasAttribute((CardAttribute)48)
			orderby c.GetDefensePower() descending
			select c).FirstOrDefault();
		ClientCard clientCard = (from c in base.Bot.Graveyard
			where c != null && !c.HasType(CardType.Xyz) && c.HasSetcode(446) && c.Level == 4 && c != nonLightDarkTarget
			orderby c.GetDefensePower() descending
			select c).FirstOrDefault();
		if (nonLightDarkTarget == null || clientCard == null)
		{
			return false;
		}
		List<ClientCard> cards = new List<ClientCard> { nonLightDarkTarget, clientCard };
		List<ClientCard> costFromHandAndField = GetCostFromHandAndField(base.Card, sendNotNecessary: false);
		if (costFromHandAndField.Count() > 0)
		{
			base.AI.SelectCard(costFromHandAndField);
			base.AI.SelectNextCard(cards);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (GetLevel4CountOnField() == 1 && sendNotNessary)
		{
			List<ClientCard> costFromHandAndField2 = GetCostFromHandAndField(base.Card, sendNotNecessary: true);
			if (costFromHandAndField2.Count() > 0)
			{
				base.AI.SelectCard(costFromHandAndField2);
				base.AI.SelectNextCard(cards);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool ExRyzealSpSummon()
	{
		if (CheckShouldNoMoreSpSummon((CardLocation)66))
		{
			if (!CheckWhetherHaveFinalMonster() && GetLevel4CountOnField() == 1 && ExRyzealDiscardExtra())
			{
				return true;
			}
			return false;
		}
		if (base.Duel.Turn == 1 && ((!activatedCardIdList.Contains(34022970) && !lockBirdSolved && !DefaultCheckWhetherCardIdIsNegated(34022970) && !base.Bot.HasInMonstersZone(90590303)) | (!base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz)) && GetLevel4CountOnField() == 1)))
		{
			base.AI.SelectCard(34909328, 7511613);
			return true;
		}
		if (ExRyzealDiscardExtra())
		{
			return true;
		}
		return false;
	}

	public bool ExRyzealDiscardExtra()
	{
		List<int> list = new List<int>
		{
			9940036, 45852939, 2061963, 16643334, 6983839, 21044178, 46772449, 1269512, 66011101, 7511613,
			34909328
		};
		List<int> list2 = new List<int>();
		foreach (int item in list)
		{
			if ((item != 9940036 || base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled() && !c.IsShouldNotBeMonsterTarget()) || !base.Enemy.SpellZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled() && !c.IsShouldNotBeMonsterTarget())) && (item != 45852939 || (base.Bot.Graveyard.Count((ClientCard c) => c.HasType(CardType.Xyz)) >= 2 && base.Bot.Graveyard.Any((ClientCard c) => c.HasType(CardType.Xyz) && c.IsCanRevive()))) && (item != 2061963 || (CheckRemainInDeck(7477101) <= 0 && !base.Bot.HasInHandOrInSpellZone(2061963))) && (item != 6983839 || base.Enemy.GetSpellCount() <= 0))
			{
				list2.Add(item);
			}
		}
		list2.AddRange(list);
		foreach (int item2 in list2)
		{
			if (base.Bot.HasInExtra(item2))
			{
				base.AI.SelectCard(item2);
				return true;
			}
		}
		return false;
	}

	public bool ExRyzealSpSummonLater()
	{
		return false;
	}

	public bool ExRyzealSummon()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Extra) && GetLevel4CountOnField() == 1 && (!activatedCardIdList.Contains(34022970) & (!base.Bot.HasInHand(35844557) || activatedCardIdList.Contains(35844557))))
		{
			summonCount--;
			return true;
		}
		return false;
	}

	public bool ExRyzealActivate()
	{
		if (CheckWhetherNegated())
		{
			if (base.Enemy.GetMonsters().Count((ClientCard c) => c.IsCode(90590303) && c.IsFaceup() && !c.IsDisabled() && c.HasPosition(CardPosition.FaceUpDefence)) == 1 && currentCanActivateEffect.Any((ClientCard c) => c?.IsCode(9940036) ?? false))
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool MulcharmyFuwalosActivate()
	{
		if (CheckWhetherNegated() || base.Duel.Player == 0)
		{
			return false;
		}
		if (lockBirdSolved || base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(94145021)))
		{
			return false;
		}
		if (base.Duel.Phase > DuelPhase.Main1)
		{
			return false;
		}
		botActivateMulcharmy = true;
		return true;
	}

	public bool MulcharmyPuruliaActivate()
	{
		if (CheckWhetherNegated() || base.Duel.Player == 0)
		{
			return false;
		}
		if (lockBirdSolved || base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(94145021)))
		{
			return false;
		}
		if (base.Duel.Phase > DuelPhase.Main1)
		{
			return false;
		}
		if (botActivateMulcharmy)
		{
			return false;
		}
		botActivateMulcharmy = true;
		return true;
	}

	public bool MulcharmyNyalusActivate()
	{
		if (CheckWhetherNegated() || base.Duel.Player == 0)
		{
			return false;
		}
		if (lockBirdSolved || base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(94145021)))
		{
			return false;
		}
		if (base.Duel.Phase > DuelPhase.Main1)
		{
			return false;
		}
		if (botActivateMulcharmy)
		{
			return false;
		}
		botActivateMulcharmy = true;
		return true;
	}

	public bool AshBlossomActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (DefaultAshBlossomAndJoyousSpring())
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard.Location == CardLocation.MonsterZone || lastChainCard.Location == CardLocation.SpellZone)
			{
				currentNegateCardList.Add(base.Util.GetLastChainCard());
			}
			return true;
		}
		return false;
	}

	public bool GhostOgreAndSnowRabbitActivate()
	{
		if (CheckWhetherNegated() || base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard == null || lastChainCard.IsDisabled())
		{
			return false;
		}
		if (lastChainCard.IsMonster() && !lastChainCard.HasType((CardType)75505728))
		{
			return false;
		}
		return true;
	}

	public bool MaxxCActivate()
	{
		if (CheckWhetherNegated() || base.Duel.LastChainPlayer == 0 || lockBirdSolved)
		{
			return false;
		}
		return DefaultMaxxC();
	}

	public bool LockBirdActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.Player == 0)
		{
			return false;
		}
		if (new List<int> { 84192580, 42141493 }.Intersect(botSolvedCardIdList).Any() && base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() + 6 < base.Bot.Hand.Count())
		{
			return false;
		}
		if (botSolvedCardIdList.Contains(23434538) && (!activatedCardIdList.Contains(14558127) || !activatedCardIdList.Contains(97268402)))
		{
			return false;
		}
		return true;
	}

	public bool EffectVeilerActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeTarget: true, CardType.Monster);
		if (monsterListForTargetNegate.Count > 0)
		{
			ClientCard clientCard = monsterListForTargetNegate[0];
			currentNegateCardList.Add(clientCard);
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
	}

	public bool SeventhTachyonActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if ((!spSummonedCardIdList.Contains(34022970) && !base.Bot.HasInHand(34022970)) & (activatedCardIdList.Contains(8633261) || summonCount <= 0 || !base.Bot.HasInHand(8633261) || DefaultCheckWhetherCardIdIsNegated(8633261)))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool TripleTacticsTalentActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (TripleTacticsTalentDecision(null) == -1)
		{
			return false;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
		return true;
	}

	public int TripleTacticsTalentDecision(IList<int> options)
	{
		if (base.Enemy.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && (c.IsFloodgate() || (c.IsCode(90590303) && c.HasPosition(CardPosition.FaceUpDefence)))))
		{
			if (options == null)
			{
				return 1;
			}
			int num = options.IndexOf(base.Util.GetStringId(25311006, 1));
			if (num >= 0)
			{
				return num;
			}
		}
		if (!lockBirdSolved && !CheckCanContinueSummon())
		{
			if (options == null)
			{
				return 1;
			}
			int num2 = options.IndexOf(base.Util.GetStringId(25311006, 0));
			if (num2 >= 0)
			{
				return num2;
			}
		}
		if (base.Enemy.Hand.Count() > 0)
		{
			if (options == null)
			{
				return 1;
			}
			int num3 = options.IndexOf(base.Util.GetStringId(25311006, 2));
			if (num3 >= 0)
			{
				return num3;
			}
		}
		return -1;
	}

	public bool PotOfDesiresActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Bot.Deck.Count >= 15)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool PotOfDesireActivateForContinue()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Bot.Deck.Count >= 15 && !CheckCanContinueSummon() && CheckRemainInDeck(8633261, 35844557, 34022970) > 0)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool BonfireActivateToSearchNecessary()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if ((!spSummonedCardIdList.Contains(34022970) && !base.Bot.HasInHand(34022970)) | (!activatedCardIdList.Contains(8633261) && summonCount > 0 && !base.Bot.HasInHand(8633261) && !DefaultCheckWhetherCardIdIsNegated(8633261)))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool BonfireActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (!activatedCardIdList.Contains(66011101))
		{
			ClientCard clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(66011101) && !c.IsDisabled());
			if (clientCard != null && clientCard.Overlays.Count() >= 2)
			{
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
		}
		return false;
	}

	public bool CalledbytheGraveActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Duel.LastChainPlayer == 1)
		{
			if (lastChainCard != null && lastChainCard.IsMonster())
			{
				int code = base.Util.GetLastChainCard().GetOriginCode();
				if (code == 0)
				{
					return false;
				}
				if (DefaultCheckWhetherCardIdIsNegated(code))
				{
					return false;
				}
				List<int> list = new List<int> { 84192580, 42141493, 87126721 };
				if (base.Duel.Player == 0 && base.Bot.HasInHand(code) && !list.Contains(code))
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
					currentDestroyCardList.Add(firstMatchingCard);
					currentNegateCardList.AddRange(base.Enemy.MonsterZone.Where((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(code)));
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
					_ = item.Id;
					base.AI.SelectCard(item);
					currentDestroyCardList.Add(item);
					return true;
				}
			}
			if (base.Duel.ChainTargets.Contains(base.Card))
			{
				List<ClientCard> list2 = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => card.IsMonster()).ToList();
				if (list2.Count > 0)
				{
					list2.Sort(CardContainer.CompareCardAttack);
					list2.Reverse();
					int id = list2[0].Id;
					base.AI.SelectCard(id);
					currentDestroyCardList.Add(list2[0]);
					return true;
				}
			}
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			return false;
		}
		List<ClientCard> dangerousCardinEnemyGrave = GetDangerousCardinEnemyGrave(onlyMonster: true);
		if (dangerousCardinEnemyGrave.Count > 0)
		{
			int id2 = dangerousCardinEnemyGrave[0].Id;
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(id2);
			currentDestroyCardList.Add(dangerousCardinEnemyGrave[0]);
			return true;
		}
		return false;
	}

	public bool RyzealPlugInActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.Player == 0 && base.CurrentTiming == -1)
		{
			bool flag = GetLevel4CountOnField() == 1;
			if (GetLevel4CountOnField() == 0)
			{
				flag |= GetLevel4FinalCountOnField(checkSupport: true, out var _) >= 2 && !CheckWhetherHaveFinalMonster();
			}
			if (flag)
			{
				List<int> list = new List<int> { 72238166, 34022970, 35844557, 8633261 };
				foreach (int id in list)
				{
					if (activatedCardIdList.Contains(id))
					{
						continue;
					}
					ClientCard clientCard = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id));
					if (clientCard == null)
					{
						clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id));
					}
					if (clientCard != null)
					{
						base.AI.SelectCard(clientCard);
						SelectSTPlace(base.Card, avoidImpermanence: true);
						return true;
					}
				}
				foreach (int id2 in list)
				{
					ClientCard clientCard2 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id2));
					if (clientCard2 == null)
					{
						clientCard2 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id2));
					}
					if (clientCard2 != null)
					{
						base.AI.SelectCard(clientCard2);
						SelectSTPlace(base.Card, avoidImpermanence: true);
						return true;
					}
				}
			}
		}
		if (!base.Bot.HasInMonstersZone(34909328, notDisabled: true, hasXyzMaterial: true, faceUp: true) && !base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(45852939)) && !DefaultCheckWhetherCardIdIsNegated(34909328) && !base.Util.ChainContainPlayer(0))
		{
			ClientCard clientCard3 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(34909328) && c.IsCanRevive());
			if (clientCard3 == null)
			{
				clientCard3 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(34909328) && c.IsCanRevive());
			}
			if (clientCard3 != null)
			{
				base.AI.SelectCard(clientCard3);
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
		}
		if (base.Bot.HasInSpellZone(6798031, notDisabled: true, faceUp: true) && !activatedCardIdList.Contains(6798033) && CheckRemainInDeck(34022970, 8633261, 72238166, 35844557) > 0)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.IsMonster() && lastChainCard.Controller == 1 && CheckCardShouldNegate(lastChainCard))
			{
				bool flag2 = false;
				bool flag3 = false;
				if (base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(7511613) && c.Controller == 0) && activatedCardIdList.Contains(7511614))
				{
					flag2 = base.Bot.MonsterZone.Where((ClientCard c) => c?.HasType(CardType.Xyz) ?? false).Sum((ClientCard c) => c.Overlays.Count()) >= 3;
				}
				else if (!base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && c.Overlays.Count() > 0))
				{
					flag2 |= base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz) && c.HasSetcode(446));
					if (!flag2 && (base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && c.IsCanRevive() && c.HasType(CardType.Xyz)) | base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && c.IsCanRevive() && c.HasType(CardType.Xyz))))
					{
						flag2 = true;
						flag3 = true;
					}
				}
				if (flag2)
				{
					ClientCard clientCard4 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(34909328));
					if (clientCard4 == null)
					{
						clientCard4 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(34909328));
					}
					if (clientCard4 != null)
					{
						base.AI.SelectCard(clientCard4);
						SelectSTPlace(base.Card, avoidImpermanence: true);
						return true;
					}
					if (flag3)
					{
						ClientCard clientCard5 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(7511613));
						if (clientCard5 == null)
						{
							clientCard5 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(7511613));
						}
						if (clientCard5 != null)
						{
							base.AI.SelectCard(clientCard5);
							SelectSTPlace(base.Card, avoidImpermanence: true);
							return true;
						}
					}
					else
					{
						List<int> list2 = new List<int> { 72238166, 34022970, 35844557, 8633261 };
						foreach (int id3 in list2)
						{
							if (activatedCardIdList.Contains(id3))
							{
								continue;
							}
							ClientCard clientCard6 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id3));
							if (clientCard6 == null)
							{
								clientCard6 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id3));
							}
							if (clientCard6 != null)
							{
								base.AI.SelectCard(clientCard6);
								SelectSTPlace(base.Card, avoidImpermanence: true);
								return true;
							}
						}
						foreach (int id4 in list2)
						{
							ClientCard clientCard7 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id4));
							if (clientCard7 == null)
							{
								clientCard7 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id4));
							}
							if (clientCard7 != null)
							{
								base.AI.SelectCard(clientCard7);
								SelectSTPlace(base.Card, avoidImpermanence: true);
								return true;
							}
						}
					}
				}
			}
		}
		bool num = DefaultOnBecomeTarget() && base.Card.Location == CardLocation.SpellZone;
		bool flag4 = base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End;
		if ((num || flag4) && (!base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 1 && c.IsCode(15693423)) || deadnaderDestroySelf == null))
		{
			ClientCard clientCard8 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(34909328));
			if (clientCard8 == null)
			{
				clientCard8 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(34909328));
			}
			if (clientCard8 != null)
			{
				base.AI.SelectCard(clientCard8);
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
			ClientCard clientCard9 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(7511613));
			if (clientCard9 == null)
			{
				clientCard9 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCanRevive() && c.IsCode(7511613));
			}
			if (clientCard9 != null)
			{
				base.AI.SelectCard(clientCard9);
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
			List<int> list3 = new List<int> { 72238166, 34022970, 35844557, 8633261 };
			foreach (int id5 in list3)
			{
				if (activatedCardIdList.Contains(id5))
				{
					continue;
				}
				ClientCard clientCard10 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id5));
				if (clientCard10 == null)
				{
					clientCard10 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id5));
				}
				if (clientCard10 != null)
				{
					base.AI.SelectCard(clientCard10);
					SelectSTPlace(base.Card, avoidImpermanence: true);
					return true;
				}
			}
			if (!flag4)
			{
				foreach (int id6 in list3)
				{
					ClientCard clientCard11 = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id6));
					if (clientCard11 == null)
					{
						clientCard11 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(id6));
					}
					if (clientCard11 != null)
					{
						base.AI.SelectCard(clientCard11);
						SelectSTPlace(base.Card, avoidImpermanence: true);
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool RyzealPlugInActivateFirst()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Duel.Player == 0 && base.CurrentTiming == -1 && !activatedCardIdList.Contains(72238166) && !DefaultCheckWhetherCardIdIsNegated(72238166) && GetCostFromHandAndField(base.Card, sendNotNecessary: false).Count() > 0)
		{
			ClientCard clientCard = base.Bot.Banished.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(72238166));
			if (clientCard == null)
			{
				clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(72238166));
			}
			if (clientCard != null)
			{
				base.AI.SelectCard(clientCard);
				SelectSTPlace(base.Card, avoidImpermanence: true);
				return true;
			}
		}
		return false;
	}

	public bool RyzealCrossActivateCard()
	{
		if (base.ActivateDescription == base.Util.GetStringId(6798031, 3))
		{
			ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
			if (currentSolvingChainInfo != null && !base.Duel.IsCurrentSolvingChainNegated() && CheckCardShouldNegate(currentSolvingChainInfo))
			{
				Logger.DebugWriteLine("** cross negate");
				activatedCardIdList.Add(6798033);
				botSolvingCross = true;
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFaceup())
		{
			return false;
		}
		bool flag = RyzealCrossActivateRecycleFirst();
		bool canSetMaterial = base.Bot.HasInHandOrInSpellZone(60394026) && CheckRemainInDeck(8633261, 34022970, 72238166, 35844557) > 0 && (base.Bot.Graveyard.Any((ClientCard c) => c != null && c.HasSetcode(446) && (c.IsCanRevive() || !c.HasType(CardType.Xyz))) || base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && (c.IsCanRevive() || !c.HasType(CardType.Xyz))));
		flag |= base.Bot.MonsterZone.Count((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz) && c.HasSetcode(446) && (c.Overlays.Count() > 0 || canSetMaterial)) > 0;
		if (base.Duel.MainPhase.SpecialSummonableCards.Any((ClientCard c) => c.IsCode(7511613)))
		{
			flag |= RyzealDuodriveSpSummonCheck();
		}
		return flag;
	}

	public bool RyzealCrossActivateRecycleFirst()
	{
		if (CheckWhetherNegated() || base.Card.Location != CardLocation.SpellZone || !base.Card.IsFaceup())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(6798031, 3))
		{
			return false;
		}
		List<int> obj = new List<int> { 60394026, 7511613, 34909328, 72238166, 34022970, 8633261, 35844557 };
		List<ClientCard> list = new List<ClientCard>();
		foreach (int id in obj)
		{
			ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.IsCode(id));
			if (clientCard != null && CheckRemainInDeck(id) + base.Bot.ExtraDeck.Count((ClientCard c) => c.IsCode(id)) + base.Bot.Hand.Count((ClientCard c) => c.IsCode(id)) == 0)
			{
				if (clientCard.HasType(CardType.Xyz) && GetLevel4CountOnField() == 1)
				{
					continue;
				}
				list.Add(clientCard);
			}
			if (list.Count() >= 2)
			{
				base.AI.SelectCard(list);
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
		}
		return false;
	}

	public bool RyzealCrossActivateRecycleLater()
	{
		if (CheckWhetherNegated() || base.Card.Location != CardLocation.SpellZone || !base.Card.IsFaceup())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(6798031, 3))
		{
			return false;
		}
		SortedDictionary<int, List<int>> sortedDictionary = new SortedDictionary<int, List<int>>();
		foreach (int id in new List<int> { 60394026, 7511613, 34909328, 72238166, 34022970, 8633261, 35844557 })
		{
			int key = CheckRemainInDeck(id) + base.Bot.ExtraDeck.Count((ClientCard c) => c.IsCode(id));
			if (!sortedDictionary.ContainsKey(key))
			{
				sortedDictionary.Add(key, new List<int>());
			}
			sortedDictionary[key].Add(id);
		}
		List<ClientCard> list = new List<ClientCard>();
		foreach (KeyValuePair<int, List<int>> item in sortedDictionary)
		{
			foreach (int id2 in item.Value)
			{
				ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.IsCode(id2));
				if (clientCard != null)
				{
					list.Add(clientCard);
				}
			}
		}
		if (list.Count() >= 2)
		{
			base.AI.SelectCard(list);
			activatedCardIdList.Add(base.Card.Id + 1);
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
			int code = base.Util.GetLastChainCard().Id;
			int alias = base.Util.GetLastChainCard().Alias;
			if (alias != 0 && alias - code < 10)
			{
				code = alias;
			}
			if (code == 0)
			{
				return false;
			}
			if (DefaultCheckWhetherCardIdIsNegated(code))
			{
				return false;
			}
			if (CheckRemainInDeck(code) > 0)
			{
				if (base.Card.Location != CardLocation.SpellZone)
				{
					SelectSTPlace(null, avoidImpermanence: true);
				}
				base.AI.SelectAnnounceID(code);
				currentNegateCardList.AddRange(base.Enemy.MonsterZone.Where((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(code)));
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
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeTarget: true, CardType.Trap);
		if (monsterListForTargetNegate.Count > 0)
		{
			ClientCard clientCard = monsterListForTargetNegate[0];
			currentNegateCardList.Add(clientCard);
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

	public bool AbyssDwellerSummonCheck()
	{
		bool flag = enemyDeckTypeRecord.Contains(119);
		flag |= enemyDeckTypeRecord.Contains(283);
		flag |= enemyDeckTypeRecord.Contains(4315);
		flag |= enemyDeckTypeRecord.Count() > 0 && enemyDeckTypeRecord.All((int c) => c == 385);
		if (base.Enemy.Hand.Count() + base.Enemy.Deck.Count() + base.Enemy.Graveyard.Count() + base.Enemy.Banished.Count() + base.Enemy.ExtraDeck.Count() > 65)
		{
			flag |= !enemyDeckTypeRecord.Contains(187);
		}
		return flag;
	}

	public bool Number41BagooskatheTerriblyTiredTapirSummonCheck()
	{
		return (enemyDeckTypeRecord.Contains(365) | enemyDeckTypeRecord.Contains(349) | enemyDeckTypeRecord.Contains(446)) & base.Util.IsTurn1OrMain2();
	}

	public bool TornadoDragonSummonCheck()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		return enemyDeckTypeRecord.Contains(382) | base.Enemy.SpellZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled() && c.IsFloodgate()) | (base.Enemy.SpellZone.Count((ClientCard c) => c != null && !c.IsShouldNotBeMonsterTarget() && !NotToDestroySpellTrap.Contains(c.Id)) >= 3) | (!base.Util.IsTurn1OrMain2() && !botSolvedCardIdList.Contains(46772449) && base.Enemy.GetMonsterCount() == 0 && base.Enemy.SpellZone.Count((ClientCard c) => c != null && !c.IsShouldNotBeMonsterTarget() && !NotToDestroySpellTrap.Contains(c.Id)) > 0);
	}

	public bool EvilswarmExcitonKnightSpSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || base.Duel.Turn == 1)
		{
			return false;
		}
		return DefaultEvilswarmExcitonKnightSummon();
	}

	public bool LessSpSummonExtra()
	{
		if (!CheckShouldNoMoreSpSummon(CardLocation.Extra))
		{
			return false;
		}
		ClientCard clientCard = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(90590303));
		if (clientCard != null && Number41BagooskatheTerriblyTiredTapirSummonCheck())
		{
			if (base.Card != clientCard)
			{
				return false;
			}
			List<ClientCard> level4OnField = GetLevel4OnField(null);
			if (level4OnField.Count() >= 2)
			{
				base.AI.SelectMaterials(level4OnField);
				return true;
			}
		}
		ClientCard clientCard2 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(21044178));
		if (clientCard2 != null && AbyssDwellerSummonCheck())
		{
			if (base.Card != clientCard2)
			{
				return false;
			}
			List<ClientCard> level4OnField2 = GetLevel4OnField(null);
			if (level4OnField2.Count() >= 2)
			{
				base.AI.SelectMaterials(level4OnField2);
				return true;
			}
		}
		ClientCard clientCard3 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(34909328));
		if (clientCard3 != null)
		{
			List<ClientCard> level4OnField3 = GetLevel4OnField((ClientCard c) => c.HasSetcode(446));
			if (level4OnField3.Count() >= 2)
			{
				if (base.Card != clientCard3)
				{
					return false;
				}
				base.AI.SelectMaterials(level4OnField3);
				return true;
			}
		}
		return false;
	}

	public bool FirstRyzealDuodriveSpSummon()
	{
		if (!RyzealDuodriveSpSummonCheck())
		{
			return false;
		}
		if (base.Bot.Graveyard.Count((ClientCard c) => c.HasSetcode(446) && c.IsMonster()) == 0 && !CheckShouldNoMoreSpSummon(CardLocation.Hand) && base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970) && base.Duel.MainPhase.SpecialSummonableCards.Any((ClientCard c) => c.IsCode(34022970)) && base.Bot.ExtraDeck.Count((ClientCard c) => c.IsCode(34909328, 7511613)) > 2)
		{
			return false;
		}
		List<ClientCard> level4OnField = GetLevel4OnField(null);
		List<ClientCard> source = level4OnField.Where((ClientCard c) => !c.IsCode(72238166) || c.IsDisabled() || activatedCardIdList.Contains(72238166)).ToList();
		if (source.Count() >= 2)
		{
			base.AI.SelectMaterials(source.Take(2).ToList());
			return true;
		}
		if (level4OnField.Count() > 2 && !CheckCanContinueSummon())
		{
			base.AI.SelectMaterials(level4OnField.Take(2).ToList());
			return true;
		}
		if (level4OnField.Count() >= 2 && !CheckCanContinueSummon(skipDuodriver: true))
		{
			base.AI.SelectMaterials(level4OnField.Take(2).ToList());
			return true;
		}
		return false;
	}

	public bool RyzealDuodriveSpSummonCheck()
	{
		return base.Duel.MainPhase.SpecialSummonableCards.Any((ClientCard c) => c.IsCode(7511613)) & !base.Bot.HasInMonstersZone(7511613, notDisabled: true, hasXyzMaterial: true, faceUp: true) & (CheckRemainInDeck(8633261, 35844557, 72238166, 34022970, 60394026, 6798031) >= 2) & !DefaultCheckWhetherCardIdIsNegated(7511613) & !activatedCardIdList.Contains(7511614) & !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) & !lockBirdSolved & !CheckShouldNoMoreSpSummon(CardLocation.Extra);
	}

	public bool SecondXyzSummon()
	{
		if (base.Card.Location != CardLocation.Extra)
		{
			return false;
		}
		bool hasNode;
		int level4FinalCountOnField = GetLevel4FinalCountOnField(checkSupport: true, out hasNode);
		bool result = SecondXyzSummonInner();
		Logger.DebugWriteLine("Second Xyz Count: " + level4FinalCountOnField);
		Logger.DebugWriteLine("Second Xyz Summon: " + result);
		return result;
	}

	public bool SecondXyzSummonInner()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Extra))
		{
			return false;
		}
		if (RyzealDuodriveSpSummonCheck())
		{
			Logger.DebugWriteLine("Second: summon duodriver first");
			return false;
		}
		if (GetLevel4FinalCountOnField(checkSupport: true, out var hasNode) < 4)
		{
			return false;
		}
		List<ClientCard> level4OnField = GetLevel4OnField(null);
		List<ClientCard> source = level4OnField.Where((ClientCard c) => !c.IsCode(72238166) || c.IsDisabled() || activatedCardIdList.Contains(72238166)).ToList();
		ClientCard clientCard = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(21044178));
		if (clientCard != null && AbyssDwellerSummonCheck())
		{
			if (base.Card != clientCard)
			{
				return false;
			}
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
			if (level4OnField.Count() > 2 && !CheckCanContinueSummon())
			{
				base.AI.SelectMaterials(level4OnField.Take(2).ToList());
				return true;
			}
		}
		ClientCard clientCard2 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(90590303));
		if (clientCard2 != null && (hasNode & base.Util.IsTurn1OrMain2() & (base.Bot.HasInExtra(45852939) && base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz))) & (GetNegateEffectCount() >= 2 || lockBirdSolved)))
		{
			if (base.Card != clientCard2)
			{
				return false;
			}
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
			if (level4OnField.Count() >= 2 && base.Bot.HasInHandOrInSpellZone(60394026))
			{
				base.AI.SelectMaterials(level4OnField.Take(2).ToList());
				return true;
			}
		}
		ClientCard clientCard3 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(16643334));
		bool flag = false;
		if (clientCard3 != null)
		{
			int num = 0;
			ClientCard[] monsterZone = base.Bot.MonsterZone;
			foreach (ClientCard clientCard4 in monsterZone)
			{
				if (clientCard4 != null && clientCard4.IsCode(7511613))
				{
					num += clientCard4.Overlays.Count();
				}
			}
			if (base.Bot.HasInHandOrInSpellZone(60394026))
			{
				num++;
			}
			flag = num >= 2;
		}
		if (clientCard3 != null && flag && enemyDeckTypeRecord.Contains(277))
		{
			if (base.Card != clientCard3)
			{
				return false;
			}
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
		}
		ClientCard clientCard5 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(66011101));
		if (clientCard5 != null && !lockBirdSolved && base.Bot.Deck.Count() > 2)
		{
			if (base.Card != clientCard5)
			{
				return false;
			}
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
			if (level4OnField.Count() >= 2 && base.Bot.HasInHandOrInSpellZone(60394026))
			{
				base.AI.SelectMaterials(level4OnField.Take(2).ToList());
				return true;
			}
		}
		if (clientCard3 != null && flag)
		{
			if (base.Card != clientCard3)
			{
				return false;
			}
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
		}
		ClientCard clientCard6 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(34909328));
		if (clientCard6 == null)
		{
			ClientCard clientCard7 = base.Duel.MainPhase.SummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(6983839));
			if (clientCard7 != null && TornadoDragonSummonCheck() && base.Card == clientCard7)
			{
				if (source.Count() >= 2)
				{
					base.AI.SelectMaterials(source.Take(2).ToList());
					return true;
				}
				if (level4OnField.Count() >= 2 && base.Bot.HasInHandOrInSpellZone(60394026))
				{
					base.AI.SelectMaterials(level4OnField.Take(2).ToList());
					return true;
				}
			}
		}
		if (clientCard6 != null && base.Card == clientCard6)
		{
			if (source.Count() >= 2)
			{
				base.AI.SelectMaterials(source.Take(2).ToList());
				return true;
			}
			if (level4OnField.Count() >= 2 && base.Bot.HasInHandOrInSpellZone(60394026))
			{
				base.AI.SelectMaterials(level4OnField.Take(2).ToList());
				return true;
			}
		}
		Logger.DebugWriteLine("Second: no monster to spsummon");
		return false;
	}

	public bool TwinsOfTheEclipseSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Extra))
		{
			return false;
		}
		if (base.Util.IsTurn1OrMain2())
		{
			bool flag = base.Bot.HasInHand(72238166) && !spSummonedCardIdList.Contains(72238166);
			flag |= base.Bot.HasInMonstersZone(72238166, notDisabled: true, hasXyzMaterial: false, faceUp: true);
			if (base.Bot.HasInHandOrInSpellZone(60394026))
			{
				flag |= base.Bot.Graveyard.Any((ClientCard c) => c.IsCode(72238166));
				flag |= base.Bot.Banished.Any((ClientCard c) => c.IsFaceup() && c.IsCode(72238166));
			}
			flag &= !activatedCardIdList.Contains(72238166) && !DefaultCheckWhetherCardIdIsNegated(72238166);
			List<ClientCard> list = new List<ClientCard>();
			ClientCard clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(7511613));
			if (activatedCardIdList.Contains(7511614) && clientCard != null)
			{
				list.Add(clientCard);
				flag |= !CheckWhetherWillbeRemoved() && clientCard.Overlays.Any((int id) => id == 72238166);
			}
			ClientCard clientCard2 = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(66011101));
			if (clientCard2 != null && (activatedCardIdList.Contains(66011101) || clientCard2.IsDisabled()))
			{
				list.Add(clientCard2);
				flag |= !CheckWhetherWillbeRemoved() && clientCard2.Overlays.Any((int id) => id == 72238166);
			}
			ClientCard clientCard3 = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(90590303));
			if (clientCard3 != null)
			{
				list.Add(clientCard3);
				flag |= !CheckWhetherWillbeRemoved() && clientCard3.Overlays.Any((int id) => id == 72238166);
			}
			if (list.Count() >= 2 && flag)
			{
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		else
		{
			if (botSolvedCardIdList.Contains(46772449))
			{
				return false;
			}
			List<ClientCard> level4OnField = GetLevel4OnField(null);
			List<ClientCard> list2 = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.IsFaceup() && c.HasType(CardType.Xyz) && c.Rank == 4 && c.Attack < 2500).ToList();
			if (GetLevel4FinalCountOnField(checkSupport: true, out var _) + list2.Count() < 4)
			{
				return false;
			}
			level4OnField.AddRange(list2);
			level4OnField = (from c in level4OnField
				where c != null && c.Attack < 2500
				orderby c.GetDefensePower()
				select c).Take(2).ToList();
			if (level4OnField.Count() >= 2)
			{
				if (base.Enemy.MonsterZone.Count((ClientCard c) => c != null && c.GetDefensePower() < 2500) >= 2 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !DefaultCheckWhetherCardIdIsNegated(45852939) && level4OnField.Sum((ClientCard c) => c.Attack) < 5000)
				{
					base.AI.SelectMaterials(level4OnField);
					return true;
				}
				if (level4OnField.Sum((ClientCard c) => (!botSolvedCardIdList.Contains(60394026) || c.HasType(CardType.Xyz)) ? c.Attack : 0) < 2500 && !base.Duel.MainPhase.SpecialSummonableCards.Any((ClientCard c) => c.IsCode(34909328)))
				{
					base.AI.SelectMaterials(level4OnField);
					return true;
				}
			}
		}
		return false;
	}

	public bool FinalXyzSummon()
	{
		if (base.Card.Location != CardLocation.Extra)
		{
			return false;
		}
		bool hasNode;
		int level4FinalCountOnField = GetLevel4FinalCountOnField(checkSupport: false, out hasNode);
		bool result = FinalXyzSummonInner();
		Logger.DebugWriteLine("Final Xyz Count: " + level4FinalCountOnField);
		Logger.DebugWriteLine("Final Xyz Summon: " + result);
		return result;
	}

	public bool FinalXyzSummonInner()
	{
		if (RyzealDuodriveSpSummonCheck())
		{
			Logger.DebugWriteLine("Final: summon duodriver first");
			return false;
		}
		if (GetLevel4FinalCountOnField(checkSupport: false, out var _) >= 4)
		{
			return false;
		}
		ClientCard clientCard = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(90590303));
		if (clientCard != null && Number41BagooskatheTerriblyTiredTapirSummonCheck())
		{
			if (base.Card != clientCard)
			{
				return false;
			}
			List<ClientCard> level4OnField = GetLevel4OnField(null);
			if (level4OnField.Count() >= 2)
			{
				base.AI.SelectMaterials(level4OnField);
				return true;
			}
		}
		ClientCard clientCard2 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(21044178));
		if (clientCard2 != null && AbyssDwellerSummonCheck())
		{
			if (base.Card != clientCard2)
			{
				return false;
			}
			List<ClientCard> level4OnField2 = GetLevel4OnField(null);
			if (level4OnField2.Count() >= 2)
			{
				base.AI.SelectMaterials(level4OnField2);
				return true;
			}
		}
		ClientCard clientCard3 = base.Duel.MainPhase.SpecialSummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(34909328));
		if (clientCard3 != null)
		{
			List<ClientCard> level4OnField3 = GetLevel4OnField((ClientCard c) => c.HasSetcode(446));
			if (level4OnField3.Count() >= 2)
			{
				if (base.Card != clientCard3)
				{
					return false;
				}
				base.AI.SelectMaterials(level4OnField3);
				return true;
			}
		}
		if (clientCard3 == null && base.Duel.MainPhase.SummonableCards.FirstOrDefault((ClientCard c) => c.IsCode(6983839)) != null && TornadoDragonSummonCheck())
		{
			List<ClientCard> level4OnField4 = GetLevel4OnField(null);
			if (level4OnField4.Count() >= 2)
			{
				base.AI.SelectMaterials(level4OnField4);
				return true;
			}
		}
		return false;
	}

	public bool DonnerDaggerFurHireSpSummon()
	{
		if (CheckShouldNoMoreSpSummon(CardLocation.Extra))
		{
			return false;
		}
		bool flag = base.Enemy.MonsterZone.Any((ClientCard c) => c != null && !c.IsShouldNotBeMonsterTarget()) && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster);
		List<ClientCard> illegalList = (from c in base.Bot.GetMonsters()
			where c.IsFaceup() && c.Level != 4 && c.Rank != 4
			orderby c.GetDefensePower()
			select c).ToList();
		bool num = base.Bot.HasInHand(34022970) && !spSummonedCardIdList.Contains(34022970) && !activatedCardIdList.Contains(34022970) && illegalList.Count() > 0;
		bool flag2 = !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Enemy.MonsterZone.Any((ClientCard c) => c != null && !c.IsShouldNotBeMonsterTarget() && c.IsFloodgate() && !c.IsDisabled());
		if (num || flag2)
		{
			if (illegalList.Count() == 1 && flag)
			{
				List<ClientCard> list = (from card in base.Bot.GetMonsters()
					where card.IsFaceup() && !illegalList.Contains(card) && (card.Owner == 1 || !card.HasType(CardType.Xyz))
					select card).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				illegalList.AddRange(list);
			}
			if (illegalList.Count() > 1)
			{
				List<ClientCard> list2 = illegalList.Take(2).ToList();
				if (base.Util.GetBotAvailZonesFromExtraDeck(list2) > 0)
				{
					base.AI.SelectMaterials(list2);
					return true;
				}
			}
		}
		if (base.Duel.Phase == DuelPhase.Main2)
		{
			List<ClientCard> enemyOwnerMonsters = (from c in base.Bot.MonsterZone
				where c != null && c.IsFaceup() && c.Owner == 1
				orderby c.GetDefensePower()
				select c).ToList();
			if (enemyOwnerMonsters.Count() > 0 && flag)
			{
				if (enemyOwnerMonsters.Count() == 1)
				{
					List<ClientCard> collection = (from c in base.Bot.GetMonsters()
						where c.IsFaceup() && !enemyOwnerMonsters.Contains(c) && (!c.HasType(CardType.Xyz) || c.Overlays.Count() == 0)
						orderby c.GetDefensePower()
						select c).ToList();
					enemyOwnerMonsters.AddRange(collection);
				}
				if (enemyOwnerMonsters.Count() > 1)
				{
					List<ClientCard> list3 = enemyOwnerMonsters.Take(2).ToList();
					if (base.Util.GetBotAvailZonesFromExtraDeck(list3) > 0)
					{
						base.AI.SelectMaterials(list3);
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool MereologicAggregatorActivateFirst()
	{
		List<Func<ClientCard, bool>> list = new List<Func<ClientCard, bool>>
		{
			(ClientCard c) => c.IsCode(44665365, 48546368, 54178659) && c.IsMonster(),
			(ClientCard c) => c.IsCode(4280258) && c.Attack >= 800,
			(ClientCard c) => c.IsCode(47297616) && c.Attack >= 500 && c.Defense >= 500,
			(ClientCard c) => c.IsCode(19652159) && c.Attack >= 1000 && c.Defense >= 1000,
			(ClientCard c) => c.IsCode(79600447) && base.Enemy.MonsterZone.Any((ClientCard m) => m != null && m.IsFaceup() && m.IsCode(23288411) && m.Attack >= 1000)
		};
		List<ClientCard> list2 = new List<ClientCard>(base.Enemy.GetMonsters());
		list2.AddRange(base.Enemy.GetSpells());
		foreach (ClientCard item in list2)
		{
			if (item == null || item.IsFacedown() || item.IsDisabled())
			{
				continue;
			}
			foreach (Func<ClientCard, bool> item2 in list)
			{
				if (item2(item))
				{
					base.AI.SelectCard(item);
					currentNegateCardList.Add(item);
					activatedCardIdList.Add(base.Card.Id + 2);
					return true;
				}
			}
		}
		return false;
	}

	public bool MereologicAggregatorActivateLater()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 0 && lastChainCard.IsCode(34022970))
		{
			ClientCard clientCard = base.Enemy.GetMonsters().FirstOrDefault((ClientCard c) => c.IsFaceup() && !c.IsDisabled() && c.IsCode(90590303) && c.HasPosition(CardPosition.FaceUpDefence) && !currentNegateCardList.Contains(c));
			if (clientCard != null)
			{
				currentNegateCardList.Add(clientCard);
				base.AI.SelectCard(clientCard);
				activatedCardIdList.Add(base.Card.Id + 2);
				return true;
			}
		}
		List<ClientCard> list = (from c in GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster, forNegate: true)
			where c.IsFaceup() && !c.IsDisabled()
			select c).ToList();
		if (list.Count() > 0)
		{
			currentNegateCardList.Add(list[0]);
			base.AI.SelectCard(list);
			activatedCardIdList.Add(base.Card.Id + 2);
			return true;
		}
		if (lastChainCard != null && lastChainCard.Controller == 0 && lastChainCard.IsCode(34022970))
		{
			foreach (ClientCard monster in base.Bot.GetMonsters())
			{
				if (!monster.IsFacedown() && !base.Duel.CurrentChain.Contains(monster) && !monster.IsDisabled() && monster.HasType(CardType.Effect) && (monster.IsCode(8633261, 35844557) | (monster.IsCode(72238166) && activatedCardIdList.Contains(72238166)) | (monster.HasType(CardType.Xyz) && !monster.HasXyzMaterial() && !monster.IsCode(34909328, 7511613, 1269512))))
				{
					base.AI.SelectCard(monster);
					activatedCardIdList.Add(base.Card.Id + 2);
					return true;
				}
			}
		}
		return false;
	}

	public bool RyzealDeadnaderActivate()
	{
		if (base.ActivateDescription == 96)
		{
			Logger.DebugWriteLine("** deadnader replace destroy");
			if (deadnaderDestroySelf != base.Card)
			{
				activatedCardIdList.Add(34909330);
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(34909328, 1))
		{
			bool flag = false;
			bool ignoreCurrentDestroy = false;
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.IsCode(targetNegateIdList))
			{
				flag = true;
				ignoreCurrentDestroy = true;
			}
			flag |= base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 1 && !c.IsDisabled() && !DefaultCheckWhetherCardIdIsNegated(c.Id) && c.IsCode(15693423, 35480699));
			if (flag | (base.Card.Overlays.Count() == 1 && !activatedCardIdList.Contains(34909328) && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster).Count() == 0))
			{
				bool flag2 = base.Bot.SpellZone.Count((ClientCard c) => c != null && c.IsFacedown() && c.IsCode(60394026) && !base.Duel.ChainTargets.Contains(c)) > 0;
				bool flag3 = !activatedCardIdList.Contains(45852940) && !DefaultCheckWhetherCardIdIsNegated(45852939) && !CheckWhetherWillbeRemoved();
				flag2 |= flag3 && base.Card.Overlays.Contains(45852939);
				if (base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 1 && !c.IsDisabled() && !DefaultCheckWhetherCardIdIsNegated(c.Id) && c.IsCode(15693423)))
				{
					flag2 |= base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.HasType(CardType.Xyz) && c.Overlays.Contains(45852939));
				}
				if (flag2)
				{
					deadnaderDestroySelf = base.Card;
					return true;
				}
			}
			if (CanDestroyList(ignoreCurrentDestroy).Count() > 0)
			{
				return true;
			}
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool RyzealDuodriveActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(7511613, 1))
		{
			int num = 0;
			ClientCard[] monsterZone = base.Bot.MonsterZone;
			foreach (ClientCard clientCard in monsterZone)
			{
				if (clientCard != null && clientCard.Overlays.Count() != 0 && (!clientCard.IsCode(66011101) || clientCard.IsDisabled() || activatedCardIdList.Contains(66011101)))
				{
					num += clientCard.Overlays.Count();
				}
			}
			if (num >= 2)
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool TwinsOfTheEclipseActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated())
			{
				return base.Bot.HasInHandOrInSpellZone(60394026);
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			ClientCard clientCard = TwinsOfTheEclipseRebornTarget(null);
			if (clientCard != null)
			{
				ClientCard clientCard2 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.IsCode(9940036));
				if (clientCard2 != null)
				{
					base.AI.SelectCard(new List<ClientCard> { clientCard, clientCard2 });
					return true;
				}
				ClientCard clientCard3 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.HasType(CardType.Xyz) && !c.HasAttribute((CardAttribute)48));
				if (clientCard3 != null)
				{
					base.AI.SelectCard(new List<ClientCard> { clientCard, clientCard3 });
					return true;
				}
				ClientCard clientCard4 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.HasType(CardType.Xyz));
				if (clientCard4 != null)
				{
					base.AI.SelectCard(new List<ClientCard> { clientCard, clientCard4 });
					return true;
				}
			}
			Logger.DebugWriteLine("** Twins of The Eclipse: although cannot find target, still should activate.");
			return true;
		}
		return false;
	}

	public ClientCard TwinsOfTheEclipseRebornTarget(List<ClientCard> targetList)
	{
		if (targetList == null)
		{
			targetList = base.Bot.Graveyard.Where((ClientCard c) => c.HasType(CardType.Xyz) && c.IsCanRevive()).ToList();
		}
		ClientCard clientCard = targetList.FirstOrDefault((ClientCard c) => c.IsCode(7511613));
		ClientCard clientCard2 = targetList.FirstOrDefault((ClientCard c) => c.IsCode(34909328));
		ClientCard clientCard3 = targetList.FirstOrDefault((ClientCard c) => c.IsCode(90590303));
		ClientCard clientCard4 = targetList.FirstOrDefault((ClientCard c) => c.IsCode(21044178));
		if (clientCard3 != null && !DefaultCheckWhetherCardIdIsNegated(90590303) && (clientCard2 == null || activatedCardIdList.Contains(34909328)) && (base.Duel.Turn != 1 || clientCard == null))
		{
			return clientCard3;
		}
		if (clientCard4 != null && !DefaultCheckWhetherCardIdIsNegated(21044178) && !botSolvedCardIdList.Contains(21044178) && AbyssDwellerSummonCheck())
		{
			return clientCard4;
		}
		if (clientCard2 != null)
		{
			return clientCard2;
		}
		if (clientCard != null && (!activatedCardIdList.Contains(7511614) || base.Bot.HasInHandOrInSpellZone(6798031)))
		{
			return clientCard;
		}
		if (targetList.Count() > 0)
		{
			return ShuffleList(targetList)[0];
		}
		return null;
	}

	public List<ClientCard> CanDestroyList(bool ignoreCurrentDestroy = false)
	{
		List<ClientCard> list = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy, CardType.Monster).Except(currentNegateCardList).ToList();
		List<int> cannotDestroyList = new List<int>(NotToDestroySpellTrap);
		list.RemoveAll((ClientCard c) => c.IsCode(cannotDestroyList));
		List<int> undestoryableCardIdlist = new List<int>
		{
			94977269, 58604027, 8062132, 10817524, 53315891, 10000090, 86221741, 71222868, 83257450, 97489701,
			97165977, 24550676, 55410871, 72664875, 85908279, 13331639, 20654247, 43228023, 99585850, 92770064,
			10497636, 77313225
		};
		list.RemoveAll((ClientCard c) => !c.IsDisabled() && c.IsCode(undestoryableCardIdlist));
		list.RemoveAll((ClientCard c) => !c.IsDisabled() && c.HasSetcode(208));
		if (base.Enemy.GetSpells().Any((ClientCard c) => c.IsFacedown()) || base.Enemy.GetMonsters().Any((ClientCard c) => c.IsFacedown()))
		{
			list.RemoveAll((ClientCard c) => c.IsCode(81497285));
		}
		list.RemoveAll((ClientCard c) => !c.IsDisabled() && c.HasSetcode(208));
		return list;
	}

	public bool TornadoDragonActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> spells = base.Enemy.GetSpells();
		if (spells.Count == 0)
		{
			return false;
		}
		ClientCard clientCard = base.Enemy.SpellZone.GetFloodgate();
		if (clientCard == null && base.Duel.Player == 1)
		{
			List<ClientCard> list = spells.Where((ClientCard c) => c.IsFaceup() && !NotToDestroySpellTrap.Contains(c.Id) && !currentDestroyCardList.Contains(c) && c.HasType((CardType)17694720)).ToList();
			if (list.Count() > 0)
			{
				clientCard = ShuffleList(list)[0];
			}
		}
		if (clientCard != null)
		{
			currentDestroyCardList.Add(clientCard);
			base.AI.SelectCard(clientCard);
			return true;
		}
		if (clientCard == null)
		{
			List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c != null && c.IsFacedown() && !currentDestroyCardList.Contains(c) && enemyPlaceThisTurn.Contains(c)).ToList();
			if (list2.Count() > 0)
			{
				clientCard = ShuffleList(list2)[0];
			}
		}
		if (clientCard == null)
		{
			List<ClientCard> list3 = base.Enemy.SpellZone.Where((ClientCard c) => c != null && c.IsFacedown() && !currentDestroyCardList.Contains(c)).ToList();
			if (list3.Count() > 0)
			{
				clientCard = ShuffleList(list3)[0];
			}
		}
		if ((base.Duel.Player == 0) | (base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End) | DefaultOnBecomeTarget())
		{
			currentDestroyCardList.Add(clientCard);
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
	}

	public bool EvilswarmExcitonKnightActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		return DefaultEvilswarmExcitonKnightEffect();
	}

	public bool AbyssDwellerActivate()
	{
		if (botSolvedCardIdList.Contains(21044178))
		{
			return false;
		}
		if (base.Duel.Player == 0 && base.Bot.HasInHandOrInSpellZone(60394026))
		{
			using List<int>.Enumerator enumerator = new List<int> { 72238166, 35844557, 34022970 }.GetEnumerator();
			if (enumerator.MoveNext())
			{
				int current = enumerator.Current;
				if (base.Card.Overlays.Contains(current) && !base.Bot.HasInHand(current) && !activatedCardIdList.Contains(current))
				{
					return true;
				}
				return false;
			}
		}
		if (base.Duel.Player == 1)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			if (enemyDeckTypeRecord.Contains(119))
			{
				return true;
			}
			return base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() > 0;
		}
		return false;
	}

	public bool Number60DugaresTheTimelessActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (Number60DugaresTheTimelessDrawEffect() || Number60DugaresTheTimelessDoubleTarget() != null || Number60DugaresTheTimelessRebornEffect())
		{
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool Number60DugaresTheTimelessDrawEffect()
	{
		if (lockBirdSolved || base.Bot.Deck.Count < 2)
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public ClientCard Number60DugaresTheTimelessDoubleTarget()
	{
		if (base.Util.IsTurn1OrMain2())
		{
			return null;
		}
		ClientCard clientCard = (from c in base.Bot.MonsterZone
			where c != null && (c.HasPosition(CardPosition.FaceUpAttack) || !summonThisTurn.Contains(c))
			orderby c.Attack descending
			select c).FirstOrDefault();
		if (clientCard != null)
		{
			int attack = clientCard.Attack;
			ClientCard clientCard2 = (from c in base.Enemy.MonsterZone
				where c != null && c.IsFaceup() && (c.IsDisabled() || !c.IsMonsterInvincible())
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard2 != null)
			{
				int num = clientCard2.GetDefensePower();
				if (clientCard2.IsAttack())
				{
					num--;
				}
				if (attack < num && attack * 2 > num)
				{
					return clientCard;
				}
			}
			if (!botSolvedCardIdList.Contains(46772449))
			{
				int botCurrentTotalAttack = GetBotCurrentTotalAttack();
				if (botCurrentTotalAttack < base.Enemy.LifePoints && botCurrentTotalAttack + attack >= base.Enemy.LifePoints)
				{
					return clientCard;
				}
			}
		}
		return null;
	}

	public bool Number60DugaresTheTimelessRebornEffect()
	{
		return false;
	}

	public bool DonnerDaggerFurHireActivate()
	{
		if (CheckAtAdvantage() && !base.Bot.HasInHand(34022970))
		{
			return false;
		}
		ClientCard clientCard = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: false, CardType.Monster);
		if (clientCard == null)
		{
			List<ClientCard> monsters = base.Enemy.GetMonsters();
			if (monsters.Count() > 0)
			{
				monsters.Sort(CardContainer.CompareCardAttack);
				monsters.Reverse();
				clientCard = monsters[0];
			}
		}
		if (clientCard != null)
		{
			base.AI.SelectCard(base.Card);
			base.AI.SelectNextCard(clientCard);
			currentDestroyCardList.Add(clientCard);
			return true;
		}
		return false;
	}

	public bool Level4Summon()
	{
		if (CheckShouldNoMoreSpSummon((CardLocation)66))
		{
			return false;
		}
		ClientCard clientCard = (from c in base.Bot.Hand
			where c.Level == 4
			orderby c.Attack
			select c).FirstOrDefault();
		if (clientCard == null || base.Card != clientCard)
		{
			return false;
		}
		if (GetLevel4CountOnField() == 1)
		{
			ClientCard clientCard2 = (from c in base.Duel.MainPhase.SummonableCards
				where c != null && c.Level == 4
				orderby c.Attack
				select c).FirstOrDefault();
			if (base.Card != clientCard2)
			{
				return false;
			}
			summonCount--;
			return true;
		}
		return false;
	}

	public bool SpellSetCheck()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay))
		{
			if (base.Card.IsCode(10045474) && base.Bot.GetMonsterCount() == 0 && base.Bot.GetSpellCount() == 0 && !base.Bot.Hand.Any((ClientCard c) => !c.IsCode(10045474) && (c.IsTrap() || c.HasType(CardType.QuickPlay))) && base.Bot.Hand.Count() <= 6)
			{
				return false;
			}
			if (base.Card.IsCode(60394026) && !(base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && (c.Level == 4 || c.IsCanRevive())) | base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(446) && (c.Level == 4 || c.IsCanRevive()))))
			{
				return false;
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

	public bool ChangePositionFirst()
	{
		if (base.Card.IsFacedown() && base.Card.Level == 4)
		{
			return true;
		}
		if (base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.HasPosition(CardPosition.FaceUpDefence) && !c.IsDisabled() && c.IsCode(90590303)))
		{
			return false;
		}
		if (base.Card.IsCode(90590303))
		{
			bool flag = base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFloodgate() && !c.IsDisabled());
			if (base.Card.IsDefense())
			{
				if (!flag)
				{
					return !base.Util.IsTurn1OrMain2();
				}
				return false;
			}
			if (!flag)
			{
				return base.Util.IsTurn1OrMain2();
			}
			return true;
		}
		return false;
	}

	protected override bool DefaultSetForDiabellze()
	{
		if (base.DefaultSetForDiabellze())
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}
}
