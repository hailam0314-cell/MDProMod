using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Albaz", "AI_Albaz", "Normal")]
public class AlbazExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int TheBystialLubellion = 32731036;

		public const int AlbionTheShroudedDragon = 25451383;

		public const int BystialSaronir = 60242223;

		public const int AluberTheJesterOfDespia = 62962630;

		public const int FallenOfAlbaz = 68468459;

		public const int SpringansKitt = 45484331;

		public const int GuidingQuemTheVirtuous = 45883110;

		public const int BlazingCartesiaTheVirtuous = 95515789;

		public const int TriBrigadeMercourier = 19096726;

		public const int DespianTragedy = 36577931;

		public const int NadirServant = 1984618;

		public const int FusionDeployment = 6498706;

		public const int BrandedInWhite = 34995106;

		public const int BrandedFusion = 44362883;

		public const int GoldSarcophagus = 75500286;

		public const int FoolishBurial = 81439173;

		public const int BrandedInHighSpirits = 29948294;

		public const int BrandedOpening = 36637374;

		public const int BrandedInRed = 82738008;

		public const int BrandedLost = 18973184;

		public const int BrightestBlazingBrandedKing = 19271881;

		public const int BrandedBeast = 32756828;

		public const int BrandedRetribution = 17751597;

		public const int GuardianChimera = 11321089;

		public const int AlbionTheSanctifireDragon = 38811586;

		public const int MirrorjadeTheIcebladeDragon = 44146295;

		public const int BorreloadFuriousDragon = 92892239;

		public const int LubellionTheSearingDragon = 70534340;

		public const int AlbaLenatusTheAbyssDragon = 3410461;

		public const int GranguignolTheDuskDragon = 24915933;

		public const int DespianQuaeritis = 72272462;

		public const int SprindTheIrondashDragon = 1906812;

		public const int TitanikladTheAshDragon = 41373230;

		public const int RindbrummTheStrikingDragon = 51409648;

		public const int AlbionTheBrandedDragon = 87746184;

		public const int DespianLuluwalilith = 53971455;

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

		public const int NibiruThePrimalBeing = 27204311;
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeBranded = 349;

	private const int SetcodeDespain = 356;

	private const int SetcodeBystial = 392;

	private const int SetcodeHorus = 413;

	private const int hintTimingMainEnd = 4;

	private const int hintBattleStart = 8;

	private List<int> notToNegateIdList = new List<int> { 58699500, 20343502, 25451383, 19403423 };

	private List<int> cannotBeFusionMaterialIdList = new List<int>
	{
		3410461, 38811586, 79229522, 65029288, 30864377, 33964637, 87116928, 13735899, 28226490, 80453041,
		63845230, 2992467, 16366810, 40217358, 47346782, 50893987, 71459017, 84433295, 85101097
	};

	private List<int> albazFusionMonster = new List<int> { 41373230, 1906812, 87746184, 70534340, 3410461, 44146295, 51409648, 38811586 };

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 62962630, 14558127, 23434538, 10045474 }
		},
		{
			2,
			new List<int> { 68468459, 1984618, 6498706, 24224830 }
		},
		{
			1,
			new List<int>
			{
				32731036, 25451383, 60242223, 45484331, 45883110, 95515789, 19096726, 36577931, 34995106, 44362883,
				75500286, 81439173, 29948294, 36637374, 65681983, 82738008, 18973184, 19271881, 32756828, 17751597
			}
		}
	};

	private List<int> dangerousDragonIdList = new List<int>
	{
		27548199, 92892239, 98630720, 9753964, 99585850, 24361622, 27572350, 69120785, 96402918, 74294676,
		42752141, 18511599, 35103106, 26268488
	};

	private List<int> notToDestroySpellTrap = new List<int> { 50005218, 6767771 };

	private List<int> targetNegateIdList = new List<int>
	{
		97268402, 10045474, 52038441, 78474168, 74003290, 67037924, 9753964, 66192538, 23204029, 73445448,
		35103106, 30286474, 45002991, 5795980, 38511382, 53742162, 30430448
	};

	private bool summoned;

	private bool enemyActivateMaxxC;

	private bool enemyActivateLockBird;

	private int dimensionShifterCount;

	private bool enemyActivateInfiniteImpermanenceFromHand;

	private bool theBystialLubellionSelecting;

	private bool albionTheShroudedDragonSelecting;

	private bool nadirActivated;

	private bool fusionToGYFlag;

	private bool spSummoningAlbaz;

	private int cartesiaSummonGoal;

	private int sanctifireSelectPositionCount;

	private int quemSummonFlag;

	private List<ClientCard> cartesiaMaterialList = new List<ClientCard>();

	private List<ClientCard> brandedInRedMaterialList = new List<ClientCard>();

	private List<int> infiniteImpermanenceList = new List<int>();

	private List<ClientCard> currentNegateCardList = new List<ClientCard>();

	private List<ClientCard> currentDestroyCardList = new List<ClientCard>();

	private List<ClientCard> sendToGYThisTurn = new List<ClientCard>();

	private List<int> activatedCardIdList = new List<int>();

	private ClientCard fusionTarget;

	private List<ClientCard> selectedFusionMaterial = new List<ClientCard>();

	private List<ClientCard> enemyPlaceThisTurn = new List<ClientCard>();

	public AlbazExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Summon, AdvanceSummon);
		AddExecutor(ExecutorType.SpSummon, 3410461, AlbaLenatusTheAbyssDragonSpSummon);
		AddExecutor(ExecutorType.MonsterSet, 68468459, FallenOfAlbazSet);
		AddExecutor(ExecutorType.Activate, 95515789, BlazingCartesiaTheVirtuousActivateInGrave);
		AddExecutor(ExecutorType.Activate, 17751597, BrandedRetributionActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 32756828, BrandedBeastActivate);
		AddExecutor(ExecutorType.Activate, 19271881, BrightestBlazingBrandedKingActivate);
		AddExecutor(ExecutorType.Activate, 36637374, BrandedOpeningActivate);
		AddExecutor(ExecutorType.Activate, 29948294, BrandedInHighSpiritsActivate);
		AddExecutor(ExecutorType.Activate, 51409648, RindbrummTheStrikingDragonActivate);
		AddExecutor(ExecutorType.Activate, 11321089, GuardianChimeraActivate);
		AddExecutor(ExecutorType.Activate, 92892239, BorreloadFuriousDragonActivate);
		AddExecutor(ExecutorType.Activate, 19096726, TriBrigadeMercourierActivate);
		AddExecutor(ExecutorType.Activate, 72272462, DespianQuaeritisActivate);
		AddExecutor(ExecutorType.Activate, 44146295, MirrorjadeTheIcebladeDragonActivate);
		AddExecutor(ExecutorType.Activate, 32731036, TheBystialLubellionActivate);
		AddExecutor(ExecutorType.SpSummon, 32731036, TheBystialLubellionSpSummon);
		AddExecutor(ExecutorType.Activate, 81439173, FoolishBurialActivate);
		AddExecutor(ExecutorType.Activate, 75500286, GoldSarcophagusActivate);
		AddExecutor(ExecutorType.Activate, 62962630, AluberTheJesterOfDespiaActivate);
		AddExecutor(ExecutorType.Activate, 38811586, AlbionTheSanctifireDragonActivate);
		AddExecutor(ExecutorType.Activate, 6498706, FusionDeploymentActivate);
		AddExecutor(ExecutorType.Activate, 25451383, AlbionTheShroudedDragonActivate);
		AddExecutor(ExecutorType.Activate, 60242223, BystialSaronirActivate);
		AddExecutor(ExecutorType.Summon, 62962630, AluberTheJesterOfDespiaSummon);
		AddExecutor(ExecutorType.Summon, 45883110, GuidingQuemTheVirtuousSummonForSearch);
		AddExecutor(ExecutorType.Activate, 45484331, SpringansKittActivate);
		AddExecutor(ExecutorType.Summon, 45484331, SpringansKittSummon);
		AddExecutor(ExecutorType.Activate, 18973184, BrandedLostCardActivate);
		AddExecutor(ExecutorType.Activate, 24915933, GranguignolTheDuskDragonActivate);
		AddExecutor(ExecutorType.Activate, 3410461, AlbaLenatusTheAbyssDragonActivate);
		AddExecutor(ExecutorType.Activate, 1906812, SprindTheIrondashDragonActivate);
		AddExecutor(ExecutorType.Activate, 41373230, TitanikladTheAshDragonActivate);
		AddExecutor(ExecutorType.Activate, 87746184, AlbionTheBrandedDragonActivate);
		AddExecutor(ExecutorType.Activate, 70534340, LubellionTheSearingDragonActivate);
		AddExecutor(ExecutorType.Summon, 95515789, BlazingCartesiaTheVirtuousSummon);
		AddExecutor(ExecutorType.Activate, 68468459, FallenOfAlbazActivate);
		AddExecutor(ExecutorType.Activate, 44362883, BrandedFusionActivate);
		AddExecutor(ExecutorType.Activate, 95515789, BlazingCartesiaTheVirtuousActivate);
		AddExecutor(ExecutorType.Activate, 34995106, BrandedInWhiteActivate);
		AddExecutor(ExecutorType.Activate, 82738008, BrandedInRedActivate);
		AddExecutor(ExecutorType.Activate, 18973184, BrandedLostActivate);
		AddExecutor(ExecutorType.Summon, 68468459, FallenOfAlbazSummon);
		AddExecutor(ExecutorType.Summon, 45883110, GuidingQuemTheVirtuousSummon);
		AddExecutor(ExecutorType.Activate, 36577931, DespianTragedyActivate);
		AddExecutor(ExecutorType.Activate, 19096726, TriBrigadeMercourierActivateForSearch);
		AddExecutor(ExecutorType.Activate, 1984618, NadirServantActivate);
		AddExecutor(ExecutorType.MonsterSet, SetForChimera);
		AddExecutor(ExecutorType.MonsterSet, 36577931, DespianTragedySet);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
		AddExecutor(ExecutorType.Activate, 45883110, GuidingQuemTheVirtuousActivate);
		AddExecutor(ExecutorType.Activate, 53971455, DespianLuluwalilithActivate);
		AddExecutor(ExecutorType.Activate, FloogateActivate);
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

	public bool CheckWhetherNegated(bool disablecheck = true, bool toFieldCheck = false, CardType type = (CardType)0)
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
			if (((toFieldCheck && (type & CardType.Link) != 0) || base.Card.IsDefense()) && (base.Enemy.MonsterZone.Any((ClientCard card) => CheckNumber41(card)) || base.Bot.MonsterZone.Any((ClientCard card) => CheckNumber41(card))))
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

	public bool CheckNumber41(ClientCard card)
	{
		if (card != null && card.IsFaceup() && card.IsCode(90590303) && card.IsDefense())
		{
			return !card.IsDisabled();
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
		if (base.Duel.Turn == 1 && lastChainCard.IsCode(23434538))
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
		if (CheckAtAdvantage() && enemyActivateMaxxC && !enemyActivateLockBird && (base.Duel.Turn == 1 || base.Duel.Phase >= DuelPhase.Main2))
		{
			return true;
		}
		return false;
	}

	public bool CheckWhetherCanSummon()
	{
		if (base.Duel.Player == 0 && base.Duel.Phase < DuelPhase.End)
		{
			return !summoned;
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

	public bool CheckWhetherShouldKeepInGrave(ClientCard c)
	{
		if (c.IsCode(24915933) && c.Location == CardLocation.Grave)
		{
			return true;
		}
		if (!c.IsCode(3410461, 87746184, 41373230, 53971455, 1906812))
		{
			return false;
		}
		if (sendToGYThisTurn.Contains(c))
		{
			return c.Location == CardLocation.Grave;
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

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219) || card.HasSetcode(413))).ToList();
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
			where c.IsFaceup() && !currentDestroyCardList.Contains(c) && c.HasType((CardType)17694720) && CheckCanBeTargeted(c, canBeTarget, selfType) && !notToDestroySpellTrap.Contains(c.Id)
			select c).ToList();
		if (list6.Count > 0 && !ignoreSpells)
		{
			resultList.AddRange(ShuffleList(list6));
		}
		return resultList;
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
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && enemyPlaceThisTurn.Contains(card)
			select card).ToList()));
		targetList.AddRange(ShuffleList((from card in base.Enemy.GetSpells()
			where (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)) && !enemyPlaceThisTurn.Contains(card)
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
		ClientCard clientCard = base.Enemy.MonsterZone.FirstOrDefault((ClientCard card) => card?.Data != null && card.IsMonsterShouldBeDisabledBeforeItUseEffect() && card.IsFaceup() && !card.IsShouldNotBeTarget() && CheckCanBeTargeted(card, canBeTarget, selfType) && !currentNegateCardList.Contains(card));
		if (clientCard != null)
		{
			list.Add(clientCard);
		}
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.Location == CardLocation.MonsterZone && item.Controller == 1 && !item.IsDisabled() && CheckCanBeTargeted(item, canBeTarget, selfType) && !currentNegateCardList.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public override BattlePhaseAction OnBattle(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		if (attackers.Count() > 0 && defenders.Count() > 0)
		{
			List<ClientCard> list = attackers.OrderBy((ClientCard card) => card.Attack).ToList();
			ClientCard clientCard = attackers.FirstOrDefault((ClientCard c) => c.IsCode(3410461) && !c.IsDisabled());
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

	public override bool OnSelectHand()
	{
		return true;
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
				List<ClientCard> source = (from clientCard57 in base.Bot.GetMonsters()
					where !clientCard57.HasType(CardType.Token)
					select clientCard57).ToList();
				List<ClientCard> collection = source.Where((ClientCard clientCard57) => clientCard57.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list = source.Where((ClientCard clientCard57) => !banishList.Contains(clientCard57) && CheckRemainInDeck(clientCard57.Id) > 0).ToList();
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
				List<ClientCard> list4 = source.Where((ClientCard clientCard57) => !banishList.Contains(clientCard57) && !clientCard57.HasType((CardType)75505728) && CheckRemainInDeck(clientCard57.Id) == 0).ToList();
				list4.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list4);
				List<ClientCard> list5 = source.Where((ClientCard clientCard57) => !banishList.Contains(clientCard57) && clientCard57.HasType((CardType)75505728) && base.Bot.HasInExtra(clientCard57.Id)).ToList();
				list5.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list5);
				List<ClientCard> list6 = source.Where((ClientCard clientCard57) => !banishList.Contains(clientCard57) && clientCard57.HasType((CardType)75505728) && !base.Bot.HasInExtra(clientCard57.Id)).ToList();
				list6.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list6);
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
			if (hint == 506)
			{
				Dictionary<int, Func<bool>> dictionary = new Dictionary<int, Func<bool>>();
				switch (currentSolvingChainCard.Id)
				{
				case 45484331:
				case 62962630:
				case 62962631:
					dictionary = new Dictionary<int, Func<bool>>
					{
						{
							44362883,
							() => BrandedFusionActivateCheck()
						},
						{
							18973184,
							delegate
							{
								if (base.Duel.Player == 0 && base.Duel.Phase >= DuelPhase.End)
								{
									return false;
								}
								if (base.Bot.HasInHandOrInSpellZone(44362883) && BrandedFusionActivateCheck())
								{
									return true;
								}
								if (base.Bot.HasInHandOrInSpellZone(34995106) && BrandedInWhiteActivateCheck())
								{
									return true;
								}
								if (base.Bot.HasInHandOrInSpellZone(82738008) && BrandedInRedActivateCheck() != null)
								{
									return true;
								}
								if (!summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion())
								{
									return true;
								}
								return (base.Bot.HasInMonstersZone(95515789) || (!summoned && base.Bot.HasInHand(95515789))) ? true : false;
							}
						},
						{ 29948294, BrandedInHighSpiritsActivateCheck },
						{
							82738008,
							() => (base.Duel.Phase == DuelPhase.End && nadirActivated) || BrandedInRedActivateCheck() != null
						},
						{ 34995106, BrandedInWhiteActivateCheck },
						{
							17751597,
							() => cards.Any((ClientCard c) => c.IsCode(17751597) && c.Location == CardLocation.Removed)
						},
						{
							19271881,
							() => base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(albazFusionMonster))
						},
						{
							36637374,
							() => base.Bot.Hand.Count > 2
						}
					};
					break;
				case 1984618:
				{
					if (!summoned)
					{
						ClientCard clientCard = cards.FirstOrDefault((ClientCard c) => c.IsCode(45883110));
						if (clientCard != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard }, cards, min, max);
						}
					}
					List<CardLocation> list7 = new List<CardLocation>
					{
						CardLocation.Grave,
						CardLocation.Deck
					};
					if (base.Bot.HasInGraveyard(51409648) && cards.Where((ClientCard c) => c.IsOriginalCode(68468459) && c.Location == CardLocation.Grave).Count() == 1)
					{
						list7 = new List<CardLocation>
						{
							CardLocation.Deck,
							CardLocation.Grave
						};
					}
					int[] array = new int[2] { 68468459, 45883110 };
					foreach (int checkId in array)
					{
						foreach (CardLocation loc in list7)
						{
							ClientCard clientCard2 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(checkId) && c.Location == loc);
							if (clientCard2 != null)
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard2 }, cards, min, max);
							}
						}
					}
					break;
				}
				case 18973184:
				case 19096726:
				case 29948294:
					dictionary = new Dictionary<int, Func<bool>>
					{
						{
							19096726,
							() => base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(albazFusionMonster)) || (base.Bot.HasInMonstersZone(95515789) && base.Bot.HasInHandOrHasInMonstersZone(68468459))
						},
						{
							45484331,
							() => CheckWhetherCanSummon() && !activatedCardIdList.Contains(45484332)
						},
						{
							68468459,
							() => (CheckWhetherCanSummon() && CheckAlbazFusion()) || base.Bot.HasInMonstersZone(95515789)
						},
						{
							45883110,
							() => CheckWhetherCanSummon()
						},
						{
							95515789,
							() => CheckWhetherCanSummon() || (!CheckShouldNoMoreSpSummon() && base.Bot.HasInMonstersZoneOrInGraveyard(68468459))
						},
						{
							25451383,
							() => !CheckWhetherWillbeRemoved() && !activatedCardIdList.Contains(25451383)
						}
					};
					break;
				case 3410461:
					dictionary = new Dictionary<int, Func<bool>>
					{
						{
							44362883,
							() => BrandedFusionActivateCheck(endPhaseCheck: false)
						},
						{
							6498706,
							() => true
						}
					};
					break;
				}
				foreach (KeyValuePair<int, Func<bool>> pair in dictionary)
				{
					ClientCard clientCard3 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair.Key));
					if (clientCard3 != null && pair.Value())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard3 }, cards, min, max);
					}
				}
			}
			List<ClientCard> selectedFusionMaterialList;
			switch (currentSolvingChainCard.Id)
			{
			case 32731036:
			{
				Dictionary<int, Func<bool>> dictionary3 = new Dictionary<int, Func<bool>>();
				switch (hint)
				{
				case 527:
					dictionary3.Add(18973184, delegate
					{
						bool flag3 = base.Bot.HasInHandOrHasInMonstersZone(95515789);
						if (!activatedCardIdList.Contains(44362883) && (base.Bot.HasInHand(44362883) || (!summoned && CheckRemainInDeck(44362883) > 0 && base.Bot.HasInHand(new int[2] { 62962630, 45484331 }))))
						{
							flag3 = true;
						}
						flag3 |= !summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion();
						flag3 |= base.Bot.HasInHandOrInSpellZone(34995106) && BrandedInWhiteActivateCheck();
						return flag3 | (base.Bot.HasInHandOrInSpellZone(82738008) && BrandedInRedActivateCheck() != null);
					});
					dictionary3.Add(32756828, () => true);
					break;
				case 506:
					dictionary3.Add(60242223, () => true);
					break;
				}
				foreach (KeyValuePair<int, Func<bool>> pair11 in dictionary3)
				{
					ClientCard clientCard46 = cards.FirstOrDefault((ClientCard c) => c.Id == pair11.Key);
					if (clientCard46 != null && pair11.Value())
					{
						SelectSTPlace(clientCard46);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard46 }, cards, min, max);
					}
				}
				break;
			}
			case 68468459:
			case 68468460:
				if (hint == 509)
				{
					foreach (int targetId in new List<int> { 44146295, 3410461, 87746184, 38811586, 70534340, 92892239, 41373230, 51409648 })
					{
						if (targetId != 70534340 || base.Bot.Hand.Count != 0)
						{
							ClientCard clientCard27 = cards.FirstOrDefault((ClientCard c) => c.IsCode(targetId));
							if (clientCard27 != null)
							{
								fusionTarget = clientCard27;
								return base.Util.CheckSelectCount(new List<ClientCard> { clientCard27 }, cards, min, max);
							}
						}
					}
				}
				if (hint == 511)
				{
					if (cards.Count == 1)
					{
						selectedFusionMaterial.AddRange(cards);
						return base.Util.CheckSelectCount(cards, cards, min, max);
					}
					List<ClientCard> list28 = cards.OrderByDescending((ClientCard clientCard57) => clientCard57.GetDefensePower()).ToList();
					selectedFusionMaterial.Add(list28[0]);
					return base.Util.CheckSelectCount(list28, cards, min, max);
				}
				break;
			case 45883110:
				foreach (KeyValuePair<int, Func<bool>> pair7 in new Dictionary<int, Func<bool>>
				{
					{
						95515789,
						() => sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184)) && CheckRemainInDeck(29948294) > 0
					},
					{
						44362883,
						() => base.Bot.HasInGraveyard(17751597)
					},
					{
						68468459,
						() => !base.Bot.HasInGraveyard(68468459)
					},
					{
						19096726,
						() => base.Bot.HasInHandOrInSpellZone(34995106)
					},
					{
						17751597,
						() => true
					},
					{
						19271881,
						() => !base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(albazFusionMonster) && fusionToGYFlag)
					},
					{
						29948294,
						() => fusionToGYFlag
					},
					{
						25451383,
						() => true
					}
				})
				{
					ClientCard clientCard15 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair7.Key));
					if (clientCard15 != null && pair7.Value())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard15 }, cards, min, max);
					}
				}
				break;
			case 95515789:
			case 95515790:
				if (hint == 509)
				{
					cartesiaMaterialList = cartesiaMaterialList.Where((ClientCard c) => c != null && (c.Location == CardLocation.MonsterZone || c.Location == CardLocation.Hand)).ToList();
					List<ClientCard> list41 = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.GetDefensePower() <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)).ToList();
					list41.AddRange(base.Bot.Hand.Where((ClientCard c) => c.IsMonster() && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))));
					if (cartesiaSummonGoal > 0)
					{
						BlazingCartesiaTheVirtuousFusionCheck(cards, cartesiaSummonGoal, list41, cartesiaMaterialList, out var clientCard42, out selectedFusionMaterialList);
						if (clientCard42 != null)
						{
							fusionTarget = clientCard42;
							return base.Util.CheckSelectCount(new List<ClientCard> { fusionTarget }, cards, min, max);
						}
					}
					BlazingCartesiaTheVirtuousFusionCheck(cards, 0, list41, cartesiaMaterialList, out var clientCard43, out selectedFusionMaterialList);
					if (clientCard43 != null)
					{
						fusionTarget = clientCard43;
						return base.Util.CheckSelectCount(new List<ClientCard> { fusionTarget }, cards, min, max);
					}
				}
				if (hint == 511)
				{
					List<ClientCard> list42 = cartesiaMaterialList.Intersect(cards).ToList();
					if (list42 != null && list42.Count > 0)
					{
						selectedFusionMaterial.Add(list42[0]);
						return base.Util.CheckSelectCount(list42, cards, min, max);
					}
					ClientCard clientCard44 = cards.FirstOrDefault((ClientCard c) => c != null && c.IsCode(32731036) && c.Location == CardLocation.MonsterZone);
					if (clientCard44 != null && !base.Bot.HasInHandOrInSpellZone(32756828) && (activatedCardIdList.Contains(32731037) || CheckRemainInDeck(18973184, 32756828) == 0) && (base.Util.IsTurn1OrMain2() || base.Enemy.MonsterZone.Count((ClientCard c) => c != null && c.GetDefensePower() < 2500) > 0))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard44 }, cards, min, max);
					}
					ClientCard clientCard45 = (from c in cards
						where c.Attack <= 2500 && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					if (clientCard45 != null)
					{
						selectedFusionMaterial.Add(clientCard45);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard45 }, cards, min, max);
					}
					clientCard45 = cards.OrderBy((ClientCard c) => c.GetDefensePower()).FirstOrDefault();
					if (clientCard45 != null)
					{
						selectedFusionMaterial.Add(clientCard45);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard45 }, cards, min, max);
					}
				}
				break;
			case 1984618:
			{
				if (hint != 504)
				{
					break;
				}
				if (summoned)
				{
					if (CheckRemainInDeck(95515789) > 0)
					{
						ClientCard clientCard11 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(53971455));
						if (clientCard11 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard11 }, cards, min, max);
						}
					}
					if (!base.Bot.MonsterZone.Any((ClientCard c) => c?.HasType(CardType.Fusion) ?? false) && CheckRemainInDeck(45484331) > 0)
					{
						ClientCard clientCard12 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(1906812));
						if (clientCard12 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard12 }, cards, min, max);
						}
					}
				}
				NadirServantActivateCheck(cards, force: true, out var target);
				if (target != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { target }, cards, min, max);
				}
				break;
			}
			case 6498706:
			{
				int num2 = FusionDeploymentSpSummonTarget();
				if (num2 <= 0)
				{
					break;
				}
				if (hint == 526)
				{
					switch (num2)
					{
					case 95515789:
					{
						ClientCard clientCard10 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(24915933));
						if (clientCard10 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard10 }, cards, min, max);
						}
						break;
					}
					case 68468459:
						foreach (ClientCard item in ShuffleList(new List<ClientCard>(cards)))
						{
							if (item.IsCode(albazFusionMonster))
							{
								return base.Util.CheckSelectCount(new List<ClientCard> { item }, cards, min, max);
							}
						}
						break;
					}
				}
				if (hint != 509)
				{
					break;
				}
				foreach (ClientCard card2 in cards)
				{
					if (card2.IsCode(num2))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { card2 }, cards, min, max);
					}
				}
				break;
			}
			case 34995106:
				if (hint == 509)
				{
					BrandedInWhiteFusionTarget(cards, out var target7);
					if (target7 != null)
					{
						fusionTarget = target7;
						return base.Util.CheckSelectCount(new List<ClientCard> { target7 }, cards, min, max);
					}
				}
				if (hint != 511 || fusionTarget == null)
				{
					break;
				}
				if (fusionTarget.IsCode(92892239))
				{
					CardLocation[] array2 = new CardLocation[3]
					{
						CardLocation.Grave,
						CardLocation.Hand,
						CardLocation.MonsterZone
					};
					foreach (CardLocation loc8 in array2)
					{
						List<ClientCard> list31 = (from c in cards
							where c.Location == loc8
							orderby c.GetDefensePower()
							select c).ToList();
						int num4 = base.Bot.Banished.Where((ClientCard c) => c.IsOriginalCode(68468459)).Count();
						num4 += selectedFusionMaterial.Where((ClientCard c) => c.IsOriginalCode(68468459)).Count();
						foreach (ClientCard item2 in list31)
						{
							if (!item2.IsOriginalCode(68468459) || num4 <= 0)
							{
								selectedFusionMaterial.Add(item2);
								return base.Util.CheckSelectCount(new List<ClientCard> { item2 }, cards, min, max);
							}
						}
					}
				}
				if (fusionTarget.IsCode(72272462))
				{
					if (selectedFusionMaterial.Count == 0)
					{
						CardLocation[] array2 = new CardLocation[3]
						{
							CardLocation.Grave,
							CardLocation.Hand,
							CardLocation.MonsterZone
						};
						foreach (CardLocation loc9 in array2)
						{
							List<ClientCard> list32 = (from c in cards
								where c.Location == loc9 && c.HasSetcode(356) && (loc9 != CardLocation.Grave || !CheckWhetherShouldKeepInGrave(c))
								orderby c.GetDefensePower()
								select c).ToList();
							if (list32.Count > 0)
							{
								selectedFusionMaterial.Add(list32[0]);
								return base.Util.CheckSelectCount(list32, cards, min, max);
							}
						}
					}
					else
					{
						CardLocation[] array2 = new CardLocation[3]
						{
							CardLocation.Grave,
							CardLocation.Hand,
							CardLocation.MonsterZone
						};
						foreach (CardLocation loc10 in array2)
						{
							List<ClientCard> list33 = (from c in cards
								where c.Location == loc10 && c.HasAttribute((CardAttribute)48) && (loc10 != CardLocation.Grave || !CheckWhetherShouldKeepInGrave(c))
								orderby c.GetDefensePower()
								select c).ToList();
							if (list33.Count <= 0)
							{
								continue;
							}
							if (!activatedCardIdList.Contains(19096727))
							{
								ClientCard clientCard30 = list33.FirstOrDefault((ClientCard c) => c.IsCode(19096726));
								if (clientCard30 != null)
								{
									selectedFusionMaterial.Add(clientCard30);
									return base.Util.CheckSelectCount(new List<ClientCard> { clientCard30 }, cards, min, max);
								}
							}
							if (!activatedCardIdList.Contains(36577931) && CheckRemainInDeck(62962630, 45883110) > 0)
							{
								ClientCard clientCard31 = list33.FirstOrDefault((ClientCard c) => c.IsCode(36577931));
								if (clientCard31 != null)
								{
									selectedFusionMaterial.Add(clientCard31);
									return base.Util.CheckSelectCount(new List<ClientCard> { clientCard31 }, cards, min, max);
								}
							}
							selectedFusionMaterial.Add(list33[0]);
							return base.Util.CheckSelectCount(list33, cards, min, max);
						}
					}
				}
				if (fusionTarget.IsCode(11321089))
				{
					List<ClientCard> list34 = ChimeraFusionMaterialList().Intersect(cards).ToList();
					if (list34.Count > 0)
					{
						return base.Util.CheckSelectCount(list34, cards, min, max);
					}
				}
				if (!fusionTarget.IsCode(albazFusionMonster))
				{
					break;
				}
				if (selectedFusionMaterial.Count == 0)
				{
					CardLocation[] array2 = new CardLocation[3]
					{
						CardLocation.Grave,
						CardLocation.MonsterZone,
						CardLocation.Hand
					};
					for (int num = 0; num < array2.Length; num++)
					{
						_ = array2[num];
						ClientCard clientCard32 = (from c in cards
							where c.IsCode(68468459)
							orderby c.GetDefensePower()
							select c).FirstOrDefault();
						if (clientCard32 != null)
						{
							selectedFusionMaterial.Add(clientCard32);
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard32 }, cards, min, max);
						}
					}
					break;
				}
				if (fusionTarget.IsOriginalCode(3410461) && cancelable)
				{
					return null;
				}
				if (base.Util.IsTurn1OrMain2() && !CheckWhetherWillbeRemoved())
				{
					ClientCard clientCard33 = cards.FirstOrDefault((ClientCard c) => c.IsCode(24915933) && c.Location == CardLocation.MonsterZone);
					if (clientCard33 != null)
					{
						selectedFusionMaterial.Add(clientCard33);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard33 }, cards, min, max);
					}
				}
				foreach (Func<ClientCard, bool> func7 in new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c),
					(ClientCard c) => c.Location == CardLocation.MonsterZone && c.GetDefensePower() <= 2000,
					(ClientCard c) => c.Location == CardLocation.Hand,
					(ClientCard c) => c.Location == CardLocation.Grave,
					(ClientCard c) => c.Location == CardLocation.MonsterZone
				})
				{
					List<ClientCard> list35 = (from c in cards
						where func7(c)
						orderby c.GetDefensePower()
						select c).ToList();
					if (list35.Count > 0)
					{
						selectedFusionMaterial.Add(list35[0]);
						return base.Util.CheckSelectCount(new List<ClientCard> { list35[0] }, cards, min, max);
					}
				}
				break;
			case 44362883:
			{
				if (hint == 509)
				{
					foreach (KeyValuePair<int, Func<bool>> pair9 in new Dictionary<int, Func<bool>>
					{
						{
							41373230,
							() => base.Enemy.HasInMonstersZone(48626373)
						},
						{
							51409648,
							() => CheckWhetherWillbeRemoved() && CheckRemainInDeck(19096726) > 0
						},
						{
							38811586,
							() => CheckShouldNoMoreSpSummon()
						},
						{
							87746184,
							() => base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsMonster() && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList)) | base.Bot.HasInHandOrHasInMonstersZone(19096726) | base.Bot.GetMonsters().Any((ClientCard c) => c.GetDefensePower() <= 1800 && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList))
						},
						{
							70534340,
							() => !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && base.Bot.Hand.Count > 0
						},
						{
							44146295,
							() => base.Bot.HasInMonstersZone(new List<int> { 24915933, 87746184, 70534340 })
						}
					})
					{
						ClientCard clientCard24 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair9.Key));
						if (clientCard24 != null && pair9.Value())
						{
							fusionTarget = clientCard24;
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard24 }, cards, min, max);
						}
					}
				}
				if (hint != 511)
				{
					break;
				}
				if (selectedFusionMaterial.Count == 0)
				{
					CardLocation[] array2 = new CardLocation[3]
					{
						CardLocation.Deck,
						CardLocation.Hand,
						CardLocation.MonsterZone
					};
					foreach (CardLocation loc7 in array2)
					{
						ClientCard clientCard25 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(68468459) && c.Location == loc7);
						if (clientCard25 != null)
						{
							selectedFusionMaterial.Add(clientCard25);
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard25 }, cards, min, max);
						}
					}
				}
				if (fusionTarget == null)
				{
					break;
				}
				Dictionary<int, List<int>> dictionary2 = new Dictionary<int, List<int>>();
				dictionary2.Add(38811586, new List<int> { 95515789, 97268402, 45883110 });
				dictionary2.Add(44146295, new List<int> { 24915933, 87746184, 53971455 });
				dictionary2.Add(70534340, new List<int> { 36577931, 60242223, 25451383, 62962630, 19096726 });
				dictionary2.Add(41373230, new List<int> { 32731036, 25451383, 60242223 });
				dictionary2.Add(51409648, new List<int> { 19096726, 45484331 });
				dictionary2.Add(87746184, new List<int> { 32731036, 95515789, 45883110 });
				dictionary2.TryGetValue(fusionTarget.GetOriginCode(), out var value);
				if (value == null || value.Count <= 0)
				{
					break;
				}
				foreach (CardLocation location in new List<CardLocation>
				{
					CardLocation.Deck,
					CardLocation.Hand,
					CardLocation.MonsterZone
				})
				{
					foreach (int checkId4 in value)
					{
						ClientCard clientCard26 = cards.FirstOrDefault((ClientCard c) => c.Location == location && c.IsCode(checkId4));
						if (clientCard26 != null)
						{
							selectedFusionMaterial.Add(clientCard26);
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard26 }, cards, min, max);
						}
					}
				}
				break;
			}
			case 75500286:
			{
				GoldSarcophagusTarget(cards, out var target2);
				if (target2 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { target2 }, cards, min, max);
				}
				break;
			}
			case 81439173:
			case 81439174:
			{
				FoolishBurialTarget(cards, out var target5);
				if (target5 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { target5 }, cards, min, max);
				}
				break;
			}
			case 29948294:
				if (hint == 526)
				{
					if (base.Duel.Phase == DuelPhase.End && base.Bot.HasInMonstersZone(45883110))
					{
						ClientCard clientCard38 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(95515789));
						if (clientCard38 != null)
						{
							fusionTarget = clientCard38;
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard38 }, cards, min, max);
						}
					}
					if (base.Duel.CurrentChain.Any((ClientCard c) => c.IsOriginalCode(25451383) && c.Location == CardLocation.Hand))
					{
						ClientCard clientCard39 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(25451383));
						if (clientCard39 != null)
						{
							fusionTarget = clientCard39;
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard39 }, cards, min, max);
						}
					}
					foreach (int discardId in new List<int> { 60242223, 25451383, 32731036, 95515789, 68468459, 19096726 })
					{
						ClientCard clientCard40 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(discardId));
						if (clientCard40 != null && (discardId != 32731036 || base.Duel.Player != 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase != DuelPhase.Main2) || CheckRemainInDeck(60242223) <= 0 || activatedCardIdList.Contains(32731036)))
						{
							fusionTarget = clientCard40;
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard40 }, cards, min, max);
						}
					}
				}
				if (hint != 504)
				{
					break;
				}
				foreach (int discardId2 in new List<int> { 87746184, 41373230, 51409648, 3410461, 24915933 })
				{
					if (!sendToGYThisTurn.Any((ClientCard c) => c.IsOriginalCode(discardId2)))
					{
						ClientCard clientCard41 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(discardId2));
						if (clientCard41 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard41 }, cards, min, max);
						}
					}
				}
				break;
			case 36637374:
				if (hint != 574)
				{
					break;
				}
				foreach (KeyValuePair<int, Func<bool>> pair5 in new Dictionary<int, Func<bool>>
				{
					{
						62962630,
						() => !activatedCardIdList.Contains(62962630) && !DefaultCheckWhetherCardIdIsNegated(62962630) && (!CheckWhetherCanSummon() || !base.Bot.HasInHand(62962630))
					},
					{
						45883110,
						() => true
					},
					{
						36577931,
						() => true
					}
				})
				{
					ClientCard clientCard13 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair5.Key));
					if (clientCard13 != null && pair5.Value())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard13 }, cards, min, max);
					}
				}
				break;
			case 82738008:
				if (hint == 509)
				{
					List<ClientCard> list39 = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.GetDefensePower() <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)).ToList();
					list39.AddRange(base.Bot.Hand.Where((ClientCard c) => c.IsMonster() && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))));
					BrandedInRedFusionCheck(cards, 0, list39, brandedInRedMaterialList, out var clientCard36, out selectedFusionMaterialList);
					if (clientCard36 != null)
					{
						fusionTarget = clientCard36;
						return base.Util.CheckSelectCount(new List<ClientCard> { fusionTarget }, cards, min, max);
					}
				}
				if (hint == 511)
				{
					List<ClientCard> list40 = brandedInRedMaterialList.Intersect(cards).ToList();
					if (list40 != null && list40.Count > 0)
					{
						selectedFusionMaterial.Add(list40[0]);
						return base.Util.CheckSelectCount(list40, cards, min, max);
					}
					ClientCard clientCard37 = (from c in cards
						where c.Attack <= 2500 && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					if (clientCard37 != null)
					{
						selectedFusionMaterial.Add(clientCard37);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard37 }, cards, min, max);
					}
					clientCard37 = cards.OrderBy((ClientCard c) => c.GetDefensePower()).FirstOrDefault();
					if (clientCard37 != null)
					{
						selectedFusionMaterial.Add(clientCard37);
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard37 }, cards, min, max);
					}
				}
				break;
			case 19271881:
			{
				if (hint != 514)
				{
					break;
				}
				List<int> list29 = new List<int> { 44146295, 38811586, 51409648 };
				foreach (Func<ClientCard, bool> func6 in new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => base.Duel.CurrentChain.Contains(c),
					(ClientCard c) => true
				})
				{
					List<ClientCard> list30 = cards.Where((ClientCard c) => func6(c)).ToList();
					if (list30.Count <= 0)
					{
						continue;
					}
					foreach (int checkId5 in list29)
					{
						ClientCard clientCard28 = list30.FirstOrDefault((ClientCard c) => c.IsOriginalCode(checkId5));
						if (clientCard28 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard28 }, cards, min, max);
						}
					}
					ClientCard clientCard29 = list30.FirstOrDefault((ClientCard c) => base.Duel.CurrentChain.Contains(c));
					if (clientCard29 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard29 }, cards, min, max);
					}
				}
				break;
			}
			case 17751597:
			{
				ClientCard clientCard20 = cards.FirstOrDefault((ClientCard c) => c.IsCode(70534340));
				if (clientCard20 != null)
				{
					selectedFusionMaterial.Add(clientCard20);
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard20 }, cards, min, max);
				}
				foreach (int checkId3 in new List<int> { 87746184, 44146295, 41373230, 3410461, 38811586, 1906812 })
				{
					List<ClientCard> list21 = cards.Where((ClientCard c) => c != null && c.IsCode(checkId3) && c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c)).ToList();
					if (list21.Count > 0)
					{
						selectedFusionMaterial.Add(list21[0]);
						return base.Util.CheckSelectCount(new List<ClientCard> { list21[0] }, cards, min, max);
					}
				}
				List<ClientCard> list22 = cards.Where((ClientCard c) => c != null && c.Location == CardLocation.Grave).ToList();
				if (list22.Count > 0)
				{
					selectedFusionMaterial.Add(list22[0]);
					return base.Util.CheckSelectCount(new List<ClientCard> { list22[0] }, cards, min, max);
				}
				ClientCard clientCard21 = (from c in cards
					where c != null && c.Location == CardLocation.MonsterZone
					orderby c.GetDefensePower()
					select c).FirstOrDefault();
				if (clientCard21 != null)
				{
					selectedFusionMaterial.Add(clientCard21);
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard21 }, cards, min, max);
				}
				break;
			}
			case 11321089:
			{
				List<ClientCard> targetList = new List<ClientCard>();
				targetList.AddRange(GetProblematicEnemyCardList(canBeTarget: false, ignoreSpells: false, CardType.Monster));
				int bestBotPower = base.Util.GetBestPower(base.Bot);
				targetList.AddRange(from c in base.Enemy.MonsterZone
					where c != null && !targetList.Contains(c) && c.GetDefensePower() >= bestBotPower
					orderby c.GetDefensePower() descending
					select c);
				targetList.AddRange(ShuffleList(enemyPlaceThisTurn));
				return base.Util.CheckSelectCount(targetList, cards, min, max);
			}
			case 38811586:
				if (hint == base.Util.GetStringId(38811586, 1))
				{
					ClientCard clientCard22 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(68468459));
					if (clientCard22 != null && CheckAlbazFusion())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard22 }, cards, min, max);
					}
					ClientCard clientCard23 = (from c in cards
						where c.IsFloodgate()
						orderby c.GetDefensePower() descending
						select c).FirstOrDefault();
					if (clientCard23 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard23 }, cards, min, max);
					}
					return base.Util.CheckSelectCount(cards.OrderByDescending((ClientCard c) => c.GetDefensePower()).ToList(), cards, min, max);
				}
				break;
			case 44146295:
			{
				List<ClientCard> list11 = ShuffleList(cards.Where((ClientCard c) => c.Controller == 1 && c.IsFloodgate()).ToList());
				if (list11.Count > 0)
				{
					return base.Util.CheckSelectCount(list11, cards, min, max);
				}
				List<ClientCard> list12 = (from c in cards
					where c.Controller == 1 && (c.HasType((CardType)8396992) || (c.HasType(CardType.Link) && c.LinkCount >= 2))
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list12.Count > 0)
				{
					return base.Util.CheckSelectCount(list12, cards, min, max);
				}
				int worstBotPower = base.Util.GetWorstBotMonster()?.GetDefensePower() ?? 0;
				List<ClientCard> list13 = (from c in cards
					where c.Controller == 1 && c.GetDefensePower() >= worstBotPower
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list13.Count > 0)
				{
					return base.Util.CheckSelectCount(list13, cards, min, max);
				}
				List<ClientCard> list14 = (from c in cards
					where c.Controller == 1 && (c.IsMonsterDangerous() || c.IsMonsterInvincible())
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list14.Count > 0)
				{
					return base.Util.CheckSelectCount(list14, cards, min, max);
				}
				List<ClientCard> list15 = (from c in cards
					where c.Controller == 1
					orderby (!c.IsFacedown()) ? c.GetDefensePower() : 0 descending
					select c).ToList();
				if (list15.Count > 0)
				{
					return base.Util.CheckSelectCount(list15, cards, min, max);
				}
				ClientCard clientCard6 = cards.FirstOrDefault((ClientCard c) => c.Controller == 0 && c.IsCode(36577931, 19096726));
				if (clientCard6 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard6 }, cards, min, max);
				}
				ClientCard clientCard7 = cards.FirstOrDefault((ClientCard c) => c.Controller == 0 && c.IsCode(32731036));
				if (clientCard7 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard7 }, cards, min, max);
				}
				List<ClientCard> list16 = (from c in cards
					where c.Controller == 0
					orderby (!c.IsFacedown()) ? c.GetDefensePower() : 0
					select c).ToList();
				if (list16.Count > 0)
				{
					return base.Util.CheckSelectCount(list16, cards, min, max);
				}
				break;
			}
			case 70534340:
			{
				if (hint == 509)
				{
					LubellionTheSearingDragonFusionTarget(cards, out var target6);
					if (target6 != null)
					{
						fusionTarget = target6;
						return base.Util.CheckSelectCount(new List<ClientCard> { target6 }, cards, min, max);
					}
				}
				if (hint != 511 || fusionTarget == null)
				{
					break;
				}
				if (fusionTarget.IsCode(92892239))
				{
					foreach (Func<ClientCard, bool> func2 in new List<Func<ClientCard, bool>>
					{
						(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed && c.IsCode(87746184),
						(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed && c.IsCode(68468459),
						(ClientCard c) => c.Location == CardLocation.Grave && c.IsCode(87746184) && !CheckWhetherShouldKeepInGrave(c),
						(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed && c.IsCode(41373230),
						(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed,
						(ClientCard c) => c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c),
						(ClientCard c) => c.Location == CardLocation.Grave,
						(ClientCard c) => c.Location == CardLocation.Hand || c.Location == CardLocation.MonsterZone
					})
					{
						List<ClientCard> list23 = (from c in cards
							where func2(c)
							orderby c.GetDefensePower()
							select c).ToList();
						if (list23.Count > 0)
						{
							selectedFusionMaterial.Add(list23[0]);
							return base.Util.CheckSelectCount(new List<ClientCard> { list23[0] }, cards, min, max);
						}
					}
				}
				List<Func<ClientCard, bool>> list24 = new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed,
					(ClientCard c) => c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c),
					(ClientCard c) => c.IsCode(70534340),
					(ClientCard c) => c.Location == CardLocation.Grave,
					(ClientCard c) => c.Location == CardLocation.MonsterZone,
					(ClientCard c) => c.Location == CardLocation.Hand
				};
				if (selectedFusionMaterial.Count == 0)
				{
					if (fusionTarget.IsOriginalCode(72272462))
					{
						foreach (Func<ClientCard, bool> func3 in list24)
						{
							List<ClientCard> list25 = (from c in cards
								where func3(c) && c.HasSetcode(356)
								orderby c.GetDefensePower()
								select c).ToList();
							if (list25.Count > 0)
							{
								selectedFusionMaterial.Add(list25[0]);
								return base.Util.CheckSelectCount(list25, cards, min, max);
							}
						}
					}
					if (fusionTarget.IsCode(albazFusionMonster))
					{
						foreach (Func<ClientCard, bool> func4 in list24)
						{
							List<ClientCard> list26 = (from c in cards
								where func4(c) && c.IsCode(68468459)
								orderby c.GetDefensePower()
								select c).ToList();
							if (list26.Count > 0)
							{
								selectedFusionMaterial.Add(list26[0]);
								return base.Util.CheckSelectCount(list26, cards, min, max);
							}
						}
					}
				}
				if (fusionTarget.IsCode(3410461) && selectedFusionMaterial.Count > 0)
				{
					list24 = new List<Func<ClientCard, bool>>
					{
						(ClientCard c) => c.IsFaceup() && c.Location == CardLocation.Removed,
						(ClientCard c) => c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c),
						(ClientCard c) => c.IsCode(70534340)
					};
				}
				foreach (Func<ClientCard, bool> func5 in list24)
				{
					List<ClientCard> list27 = (from c in cards
						where func5(c)
						orderby c.GetDefensePower()
						select c).ToList();
					if (list27.Count > 0)
					{
						selectedFusionMaterial.Add(list27[0]);
						return base.Util.CheckSelectCount(list27, cards, min, max);
					}
				}
				if (fusionTarget.IsOriginalCode(3410461) && cancelable)
				{
					return null;
				}
				break;
			}
			case 24915933:
				if (hint == 504)
				{
					GranguignolTheDuskDragonSendToGYTarget(cards, out var target3);
					if (target3 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { target3 }, cards, min, max);
					}
				}
				if (hint != 509)
				{
					break;
				}
				foreach (KeyValuePair<int, Func<bool>> pair6 in new Dictionary<int, Func<bool>>
				{
					{
						72272462,
						() => base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.Attack >= base.Util.GetBestPower(base.Bot) && (!c.HasType(CardType.Fusion) || c.Level < 8))
					},
					{
						45883110,
						() => base.Bot.HasInMonstersZone(44146295) || base.Util.GetOneEnemyBetterThanValue(1500) == null
					},
					{
						53971455,
						() => base.Duel.Player != 0 || !base.Bot.HasInHandOrInSpellZone(44362883)
					}
				})
				{
					ClientCard clientCard14 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(pair6.Key));
					if (clientCard14 != null && pair6.Value())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard14 }, cards, min, max);
					}
				}
				break;
			case 72272462:
				foreach (KeyValuePair<int, Func<bool>> pair2 in new Dictionary<int, Func<bool>>
				{
					{
						68468459,
						() => CheckAlbazFusion()
					},
					{
						45883110,
						() => !DefaultCheckWhetherCardIdIsNegated(45883110)
					},
					{
						62962630,
						() => !DefaultCheckWhetherCardIdIsNegated(62962630)
					},
					{
						36577931,
						() => true
					}
				})
				{
					ClientCard clientCard5 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair2.Key));
					if (clientCard5 != null && pair2.Value())
					{
						fusionTarget = clientCard5;
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard5 }, cards, min, max);
					}
				}
				break;
			case 1906812:
				foreach (KeyValuePair<int, Func<bool>> pair4 in new Dictionary<int, Func<bool>>
				{
					{
						68468459,
						() => CheckAlbazFusion()
					},
					{
						45484331,
						() => !DefaultCheckWhetherCardIdIsNegated(45484331)
					}
				})
				{
					ClientCard clientCard9 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair4.Key));
					if (clientCard9 != null && pair4.Value())
					{
						fusionTarget = clientCard9;
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard9 }, cards, min, max);
					}
				}
				break;
			case 41373230:
				foreach (KeyValuePair<int, Func<bool>> pair3 in new Dictionary<int, Func<bool>>
				{
					{
						68468459,
						() => CheckAlbazFusion()
					},
					{
						45883110,
						() => !DefaultCheckWhetherCardIdIsNegated(45883110)
					}
				})
				{
					ClientCard clientCard8 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(pair3.Key));
					if (clientCard8 != null && pair3.Value())
					{
						fusionTarget = clientCard8;
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard8 }, cards, min, max);
					}
				}
				break;
			case 51409648:
			{
				if (hint == 509)
				{
					foreach (KeyValuePair<int, Func<bool>> pair10 in new Dictionary<int, Func<bool>>
					{
						{
							68468459,
							() => CheckAlbazFusion()
						},
						{
							51409648,
							() => true
						},
						{
							25451383,
							() => true
						}
					})
					{
						ClientCard clientCard34 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(pair10.Key));
						if (clientCard34 != null && pair10.Value())
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard34 }, cards, min, max);
						}
					}
				}
				if (hint != 505)
				{
					break;
				}
				List<ClientCard> list36 = (from c in GetProblematicEnemyCardList(canBeTarget: false, ignoreSpells: true, CardType.Monster).Intersect(cards)
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list36.Count > 0)
				{
					return base.Util.CheckSelectCount(list36, cards, min, max);
				}
				int worstBotPower2 = base.Util.GetWorstBotMonster()?.GetDefensePower() ?? 0;
				List<ClientCard> list37 = (from c in cards
					where c.IsFaceup() && c.Controller == 1 && c.GetDefensePower() > worstBotPower2
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list37.Count > 0)
				{
					return base.Util.CheckSelectCount(list37, cards, min, max);
				}
				foreach (int checkId6 in new List<int> { 62962630, 45484331 })
				{
					ClientCard clientCard35 = cards.FirstOrDefault((ClientCard c) => c.Controller == 0 && c.IsCode(checkId6));
					if (clientCard35 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard35 }, cards, min, max);
					}
				}
				List<ClientCard> list38 = (from c in cards
					where c.Controller == 1
					orderby c.GetDefensePower() descending
					select c).ToList();
				if (list38.Count > 0)
				{
					return base.Util.CheckSelectCount(list38, cards, min, max);
				}
				return base.Util.CheckSelectCount((from c in cards
					where c.Controller == 0
					orderby c.GetDefensePower() descending
					select c).ToList(), cards, min, max);
			}
			case 87746184:
				if (hint == 509)
				{
					AlbionTheBrandedDragonFusionTarget(cards, out var target4);
					if (target4 != null)
					{
						fusionTarget = target4;
						return base.Util.CheckSelectCount(new List<ClientCard> { target4 }, cards, min, max);
					}
				}
				if (hint == 511 && fusionTarget != null)
				{
					if (fusionTarget.IsCode(92892239))
					{
						CardLocation[] array2 = new CardLocation[3]
						{
							CardLocation.Grave,
							CardLocation.Hand,
							CardLocation.MonsterZone
						};
						foreach (CardLocation loc3 in array2)
						{
							List<ClientCard> list17 = (from c in cards
								where c.Location == loc3
								orderby c.GetDefensePower()
								select c).ToList();
							int num3 = base.Bot.Banished.Where((ClientCard c) => c.IsOriginalCode(68468459)).Count();
							num3 += selectedFusionMaterial.Where((ClientCard c) => c.IsOriginalCode(68468459)).Count();
							foreach (ClientCard item3 in list17)
							{
								if (!item3.IsOriginalCode(68468459) || num3 <= 0)
								{
									selectedFusionMaterial.Add(item3);
									return base.Util.CheckSelectCount(new List<ClientCard> { item3 }, cards, min, max);
								}
							}
						}
					}
					if (fusionTarget.IsCode(72272462))
					{
						if (selectedFusionMaterial.Count == 0)
						{
							CardLocation[] array2 = new CardLocation[3]
							{
								CardLocation.Grave,
								CardLocation.Hand,
								CardLocation.MonsterZone
							};
							foreach (CardLocation loc4 in array2)
							{
								List<ClientCard> list18 = (from c in cards
									where c.Location == loc4 && c.HasSetcode(356) && (loc4 != CardLocation.Grave || !CheckWhetherShouldKeepInGrave(c))
									orderby c.GetDefensePower()
									select c).ToList();
								if (list18.Count > 0)
								{
									selectedFusionMaterial.Add(list18[0]);
									return base.Util.CheckSelectCount(list18, cards, min, max);
								}
							}
						}
						else
						{
							CardLocation[] array2 = new CardLocation[3]
							{
								CardLocation.Grave,
								CardLocation.Hand,
								CardLocation.MonsterZone
							};
							foreach (CardLocation loc5 in array2)
							{
								List<ClientCard> list19 = (from c in cards
									where c.Location == loc5 && c.HasAttribute((CardAttribute)48) && (loc5 != CardLocation.Grave || !CheckWhetherShouldKeepInGrave(c))
									orderby c.GetDefensePower()
									select c).ToList();
								if (list19.Count <= 0)
								{
									continue;
								}
								if (!activatedCardIdList.Contains(19096727))
								{
									ClientCard clientCard16 = list19.FirstOrDefault((ClientCard c) => c.IsCode(19096726));
									if (clientCard16 != null)
									{
										selectedFusionMaterial.Add(clientCard16);
										return base.Util.CheckSelectCount(new List<ClientCard> { clientCard16 }, cards, min, max);
									}
								}
								if (!activatedCardIdList.Contains(36577931) && CheckRemainInDeck(62962630, 45883110) > 0)
								{
									ClientCard clientCard17 = list19.FirstOrDefault((ClientCard c) => c.IsCode(36577931));
									if (clientCard17 != null)
									{
										selectedFusionMaterial.Add(clientCard17);
										return base.Util.CheckSelectCount(new List<ClientCard> { clientCard17 }, cards, min, max);
									}
								}
								selectedFusionMaterial.Add(list19[0]);
								return base.Util.CheckSelectCount(list19, cards, min, max);
							}
						}
					}
					if (fusionTarget.IsCode(albazFusionMonster))
					{
						if (selectedFusionMaterial.Count == 0)
						{
							CardLocation[] array2 = new CardLocation[3]
							{
								CardLocation.Grave,
								CardLocation.MonsterZone,
								CardLocation.Hand
							};
							foreach (CardLocation loc6 in array2)
							{
								ClientCard clientCard18 = (from c in cards
									where c.IsCode(68468459) && c.Location == loc6
									orderby c.GetDefensePower()
									select c).FirstOrDefault();
								if (clientCard18 != null)
								{
									selectedFusionMaterial.Add(clientCard18);
									return base.Util.CheckSelectCount(new List<ClientCard> { clientCard18 }, cards, min, max);
								}
							}
						}
						else
						{
							if (fusionTarget.IsOriginalCode(3410461) && cancelable)
							{
								return null;
							}
							foreach (Func<ClientCard, bool> func in new List<Func<ClientCard, bool>>
							{
								(ClientCard c) => c.Location == CardLocation.Grave && !CheckWhetherShouldKeepInGrave(c),
								(ClientCard c) => c.Location == CardLocation.MonsterZone && c.GetDefensePower() <= 2000,
								(ClientCard c) => c.Location == CardLocation.Grave,
								(ClientCard c) => c.Location == CardLocation.Hand,
								(ClientCard c) => c.Location == CardLocation.MonsterZone
							})
							{
								List<ClientCard> list20 = (from c in cards
									where func(c)
									orderby c.GetDefensePower()
									select c).ToList();
								if (list20.Count > 0)
								{
									selectedFusionMaterial.Add(list20[0]);
									return base.Util.CheckSelectCount(new List<ClientCard> { list20[0] }, cards, min, max);
								}
							}
						}
					}
				}
				if (hint != 574)
				{
					break;
				}
				foreach (KeyValuePair<int, Func<bool>> pair8 in new Dictionary<int, Func<bool>>
				{
					{
						29948294,
						() => base.Bot.HasInMonstersZone(45883110) && BrandedInHighSpiritsActivateCheck()
					},
					{
						19271881,
						() => base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(albazFusionMonster))
					},
					{
						82738008,
						() => base.Bot.Graveyard.Any((ClientCard c) => c != null && (c.HasSetcode(356) || c.IsCode(68468459)))
					},
					{
						17751597,
						() => base.Bot.Graveyard.Where((ClientCard c) => c?.IsCode(albazFusionMonster) ?? false).Count() > 1
					},
					{
						44362883,
						() => CheckRemainInDeck(68468459) > 0
					},
					{
						32756828,
						() => base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(392))
					},
					{
						18973184,
						() => true
					},
					{
						34995106,
						() => true
					}
				})
				{
					ClientCard clientCard19 = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(pair8.Key));
					if (clientCard19 != null && pair8.Value())
					{
						fusionTarget = clientCard19;
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard19 }, cards, min, max);
					}
				}
				break;
			case 53971455:
			{
				if (hint == 573)
				{
					List<ClientCard> list8 = cards.Where((ClientCard c) => c.IsFaceup() && c.Controller == 1).ToList();
					List<ClientCard> list9 = GetProblematicEnemyCardList().Intersect(list8).ToList();
					if (list9.Count > 0)
					{
						return base.Util.CheckSelectCount(ShuffleList(list9), cards, min, max);
					}
					List<ClientCard> list10 = GetMonsterListForTargetNegate().Intersect(list8).ToList();
					if (list10.Count > 0)
					{
						return base.Util.CheckSelectCount(ShuffleList(list10), cards, min, max);
					}
					if (list8.Count > 0)
					{
						return base.Util.CheckSelectCount(ShuffleList(list8), cards, min, max);
					}
				}
				if (hint != 509)
				{
					break;
				}
				CardLocation[] array2 = new CardLocation[2]
				{
					CardLocation.Deck,
					CardLocation.Hand
				};
				foreach (CardLocation loc2 in array2)
				{
					foreach (int checkId2 in new List<int> { 95515789, 45883110 })
					{
						ClientCard clientCard4 = cards.FirstOrDefault((ClientCard c) => c.Location == loc2 && c.IsOriginalCode(checkId2));
						if (clientCard4 != null)
						{
							return base.Util.CheckSelectCount(new List<ClientCard> { clientCard4 }, cards, min, max);
						}
					}
				}
				break;
			}
			}
		}
		bool flag = hint == 501;
		bool flag2 = hint == 507 && cards.All((ClientCard c) => c.Location == CardLocation.Hand);
		if (min == 1 && max == 1 && (flag || flag2))
		{
			if (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(36637374))
			{
				ClientCard clientCard47 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.IsCode(36577931));
				if (clientCard47 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard47 }, cards, min, max);
				}
			}
			if (flag)
			{
				foreach (ClientCard card3 in cards)
				{
					if (card3.IsCode(25451383) && base.Duel.CurrentChain.Contains(card3))
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { card3 }, cards, min, max);
					}
				}
				foreach (int id in new List<int> { 17751597, 25451383, 60242223, 19271881, 29948294, 95515789, 36577931 })
				{
					ClientCard clientCard48 = cards.FirstOrDefault((ClientCard c) => c.IsCode(id));
					if (clientCard48 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard48 }, cards, min, max);
					}
				}
			}
			foreach (ClientCard card in cards)
			{
				if (cards.Where((ClientCard c) => c.IsCode(card.Id)).Count() > 1)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { card }, cards, min, max);
				}
			}
			foreach (int id2 in new List<int>
			{
				17751597, 29948294, 36577931, 6498706, 32756828, 25451383, 36637374, 75500286, 81439173, 68468459,
				82738008, 10045474, 32731036, 18973184, 45484331, 45883110, 65681983, 24224830, 19096726, 14558127,
				23434538
			})
			{
				if (id2 != 18973184 || !base.Bot.HasInHand(44362883) || !BrandedFusionActivateCheck())
				{
					ClientCard clientCard49 = cards.FirstOrDefault((ClientCard c) => c.IsCode(id2));
					if (clientCard49 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard49 }, cards, min, max);
					}
				}
			}
		}
		else if (flag && min > 0 && min == max)
		{
			List<ClientCard> list43 = new List<ClientCard>();
			List<int> graveEffectIdList = new List<int> { 25451383, 17751597, 29948294, 19271881, 36577931 };
			list43.AddRange(ShuffleList(cards.Where((ClientCard c) => c.IsCode(graveEffectIdList)).ToList()));
			List<ClientCard> list44 = cards.Except(list43).ToList();
			HashSet<int> hashSet = new HashSet<int>();
			for (int num5 = list44.Count - 1; num5 >= 0; num5--)
			{
				ClientCard clientCard50 = list44[num5];
				if (!hashSet.Add(clientCard50.Id))
				{
					list43.Add(clientCard50);
					list44.Remove(clientCard50);
				}
			}
			foreach (int id3 in new List<int>
			{
				6498706, 32756828, 25451383, 36637374, 75500286, 81439173, 68468459, 82738008, 10045474, 32731036,
				18973184, 45484331, 45883110, 65681983, 24224830, 19096726, 14558127, 23434538
			})
			{
				ClientCard clientCard51 = list44.FirstOrDefault((ClientCard c) => c.IsCode(id3));
				if (clientCard51 != null)
				{
					list43.Add(clientCard51);
				}
			}
			if (list43.Count > min)
			{
				list43 = list43.Take(min).ToList();
			}
			return base.Util.CheckSelectCount(list43, cards, min, max);
		}
		if (theBystialLubellionSelecting)
		{
			theBystialLubellionSelecting = false;
			ClientCard clientCard52 = TheBystialLubellionSpSummonCost(cards);
			if (clientCard52 != null)
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard52 }, cards, min, max);
			}
			List<ClientCard> list45 = new List<ClientCard>(cards);
			list45.Sort(CardContainer.CompareCardAttack);
			return base.Util.CheckSelectCount(list45, cards, min, max);
		}
		if (albionTheShroudedDragonSelecting || (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(60242223)))
		{
			ClientCard clientCard53 = cards.FirstOrDefault((ClientCard c) => c.IsCode(17751597));
			if (clientCard53 != null && (clientCard53.Location == CardLocation.Deck || (from c in base.Bot.GetGraveyardMonsters()
				where c.IsCode(albazFusionMonster)
				select c).Count() < 2))
			{
				return base.Util.CheckSelectCount(new List<ClientCard> { clientCard53 }, cards, min, max);
			}
			if (base.Bot.HasInGraveyard(17751597) || (base.Bot.HasInGraveyard(36577931) && !activatedCardIdList.Contains(36577931)))
			{
				foreach (KeyValuePair<int, Func<bool>> pair12 in new Dictionary<int, Func<bool>>
				{
					{
						44362883,
						() => BrandedFusionActivateCheck()
					},
					{
						18973184,
						delegate
						{
							if (base.Duel.Player == 0 && base.Duel.Phase >= DuelPhase.End)
							{
								return false;
							}
							if (base.Bot.HasInHandOrInSpellZone(44362883) && BrandedFusionActivateCheck())
							{
								return true;
							}
							if (base.Bot.HasInHandOrInSpellZone(34995106) && BrandedInWhiteActivateCheck())
							{
								return true;
							}
							if (base.Bot.HasInHandOrInSpellZone(82738008) && BrandedInRedActivateCheck() != null)
							{
								return true;
							}
							if (!summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion())
							{
								return true;
							}
							return (base.Bot.HasInMonstersZone(95515789) || (!summoned && base.Bot.HasInHand(95515789))) ? true : false;
						}
					},
					{ 29948294, BrandedInHighSpiritsActivateCheck },
					{
						82738008,
						() => BrandedInRedActivateCheck() != null
					},
					{ 34995106, BrandedInWhiteActivateCheck },
					{
						17751597,
						() => cards.Any((ClientCard c) => c.IsCode(17751597) && c.Location == CardLocation.Removed)
					},
					{
						19271881,
						() => base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(albazFusionMonster))
					},
					{
						36637374,
						() => base.Bot.Hand.Count > 2
					}
				})
				{
					ClientCard clientCard54 = cards.FirstOrDefault((ClientCard clientCard57) => clientCard57.Location == CardLocation.Deck && clientCard57.IsCode(pair12.Key));
					if (clientCard54 != null && pair12.Value())
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard54 }, cards, min, max);
					}
				}
			}
			if (albionTheShroudedDragonSelecting && FallenOfAlbazSetCheck() && (summoned || !base.Bot.HasInHand(new List<int> { 68468459, 29948294 })))
			{
				List<int> list46 = new List<int> { 17751597, 29948294, 19271881, 34995106, 36637374, 82738008, 32756828, 18973184 };
				if (!BrandedFusionActivateCheck())
				{
					list46.Add(44362883);
				}
				foreach (int checkId7 in list46)
				{
					ClientCard clientCard55 = cards.FirstOrDefault((ClientCard c) => c.Id == checkId7);
					if (clientCard55 != null)
					{
						return base.Util.CheckSelectCount(new List<ClientCard> { clientCard55 }, cards, min, max);
					}
				}
			}
			foreach (int checkId8 in new List<int> { 29948294, 36637374, 19271881, 32756828, 18973184 })
			{
				ClientCard clientCard56 = cards.FirstOrDefault((ClientCard c) => c.IsCode(checkId8) && c.Location == CardLocation.Deck);
				if (clientCard56 != null)
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { clientCard56 }, cards, min, max);
				}
			}
			return base.Util.CheckSelectCount(ShuffleList(new List<ClientCard>(cards)), cards, min, max);
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override int OnSelectOption(IList<int> options)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null)
		{
			if (options.Count == 2 && options.Contains(1190) && options.Contains(1152))
			{
				if (currentSolvingChainInfo.IsCode(36637374))
				{
					if (!CheckShouldNoMoreSpSummon() || summoned || base.Duel.Player != 0)
					{
						return options.IndexOf(1152);
					}
					return options.IndexOf(1190);
				}
				if (fusionTarget != null && (currentSolvingChainInfo.IsCode(72272462) || currentSolvingChainInfo.IsCode(41373230) || currentSolvingChainInfo.IsCode(1906812)))
				{
					if (fusionTarget.IsCode(68468459))
					{
						if (!CheckAlbazFusion())
						{
							return options.IndexOf(1190);
						}
						return options.IndexOf(1152);
					}
					if (fusionTarget.IsCode(45883110, 45484331))
					{
						if (!CheckShouldNoMoreSpSummon())
						{
							return options.IndexOf(1152);
						}
						return options.IndexOf(1190);
					}
					if (fusionTarget.IsCode(62962630))
					{
						if (!activatedCardIdList.Contains(62962630))
						{
							return options.IndexOf(1152);
						}
						return options.IndexOf(1190);
					}
					if (!CheckShouldNoMoreSpSummon() || summoned)
					{
						return options.IndexOf(1152);
					}
					return options.IndexOf(1190);
				}
			}
			if (currentSolvingChainInfo.IsCode(87746184) && fusionTarget != null)
			{
				if (fusionTarget.IsOriginalCode(29948294) && base.Duel.Player == 0)
				{
					if (!BrandedInHighSpiritsActivateCheck())
					{
						return options.IndexOf(1153);
					}
					return options.IndexOf(1190);
				}
				if (fusionTarget.IsOriginalCode(82738008) && base.Duel.Player == 0)
				{
					if (nadirActivated)
					{
						return options.IndexOf(1153);
					}
					if (BrandedInRedActivateCheck() == null)
					{
						return options.IndexOf(1153);
					}
					return options.IndexOf(1190);
				}
				if (fusionTarget.Data != null)
				{
					if (!(fusionTarget.Data.HasType(CardType.Trap) | (fusionTarget.Data.HasType(CardType.QuickPlay) && base.Duel.Player == 0) | (base.Bot.Hand.Count >= 6 && base.Duel.Player == 0)))
					{
						return options.IndexOf(1190);
					}
					return options.IndexOf(1153);
				}
			}
		}
		return base.OnSelectOption(options);
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo != null && currentSolvingChainInfo.ActivatePlayer == 0 && currentSolvingChainInfo.IsCode(1906812))
		{
			return SprindTheIrondashDragonMoveZone(available);
		}
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
		if (desc == base.Util.GetStringId(29948294, 2))
		{
			if (CheckWhetherWillbeRemoved())
			{
				return false;
			}
			if (fusionTarget != null && fusionTarget.IsOriginalCode(19096726))
			{
				return !base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(albazFusionMonster));
			}
		}
		if (desc == base.Util.GetStringId(51409648, 2))
		{
			return base.Enemy.MonsterZone.Any((ClientCard c) => c != null) | base.Bot.MonsterZone.Any((ClientCard c) => c != null && (c.IsOriginalCode(62962630) || c.IsOriginalCode(45484331)));
		}
		if (desc == base.Util.GetStringId(53971455, 2))
		{
			return base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled()) | base.Enemy.SpellZone.Any((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled());
		}
		if (desc == base.Util.GetStringId(82738008, 0))
		{
			brandedInRedMaterialList = brandedInRedMaterialList.Where((ClientCard c) => c != null && (c.Location == CardLocation.MonsterZone || c.Location == CardLocation.Hand)).ToList();
			List<ClientCard> list = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.Attack <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)).ToList();
			list.AddRange(base.Bot.Hand.Where((ClientCard c) => c.IsMonster() && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))));
			BrandedInRedFusionCheck(base.Bot.ExtraDeck, 0, list, brandedInRedMaterialList, out var clientCard, out var _);
			return clientCard != null;
		}
		if (desc == base.Util.GetStringId(1906812, 2))
		{
			ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
			if (currentSolvingChainCard != null)
			{
				return SprindTheIrondashDragonDestroyValue(currentSolvingChainCard.Sequence) > 0;
			}
		}
		return base.OnSelectYesNo(desc);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(38811586))
		{
			sanctifireSelectPositionCount++;
			if (sanctifireSelectPositionCount >= 2 && base.Duel.Phase <= DuelPhase.Main2)
			{
				return CardPosition.FaceUpDefence;
			}
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
			int value = Math.Max(base.Util.GetBestAttack(base.Bot), attack);
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
		}
		summoned = false;
		enemyActivateMaxxC = false;
		enemyActivateLockBird = false;
		enemyActivateInfiniteImpermanenceFromHand = false;
		nadirActivated = false;
		fusionToGYFlag = false;
		spSummoningAlbaz = false;
		cartesiaSummonGoal = 0;
		sanctifireSelectPositionCount = 0;
		quemSummonFlag = 0;
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		cartesiaMaterialList.Clear();
		brandedInRedMaterialList.Clear();
		infiniteImpermanenceList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		sendToGYThisTurn.Clear();
		activatedCardIdList.Clear();
		enemyPlaceThisTurn.Clear();
		base.OnNewTurn();
	}

	public override void OnChaining(int player, ClientCard card)
	{
		base.Duel.LastChainTargets.Clear();
		if (card == null)
		{
			return;
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
		if (currentSolvingChainInfo != null)
		{
			if (currentSolvingChainInfo.ActivatePlayer == 0)
			{
				List<int> ids = new List<int> { 1984618, 6498706, 44362883, 82738008 };
				if (currentSolvingChainInfo.IsCode(ids))
				{
					activatedCardIdList.Add(currentSolvingChainInfo.ActivateId);
				}
			}
			if (!base.Duel.IsCurrentSolvingChainNegated())
			{
				if (currentSolvingChainInfo.ActivatePlayer == 1)
				{
					if (currentSolvingChainInfo.IsCode(23434538))
					{
						enemyActivateMaxxC = true;
					}
					if (currentSolvingChainInfo.IsCode(94145021))
					{
						enemyActivateLockBird = true;
					}
					if (currentSolvingChainInfo.IsCode(91800273))
					{
						dimensionShifterCount = 2;
					}
				}
				if (currentSolvingChainInfo.ActivatePlayer == 0 && currentSolvingChainInfo.IsCode(1984618))
				{
					nadirActivated = true;
				}
			}
		}
		fusionTarget = null;
		selectedFusionMaterial.Clear();
		sanctifireSelectPositionCount = 0;
		base.OnChainSolved(chainIndex);
	}

	public override void OnChainEnd()
	{
		cartesiaSummonGoal = 0;
		cartesiaMaterialList.Clear();
		brandedInRedMaterialList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		enemyActivateInfiniteImpermanenceFromHand = false;
		theBystialLubellionSelecting = false;
		albionTheShroudedDragonSelecting = false;
		spSummoningAlbaz = false;
		for (int num = enemyPlaceThisTurn.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard = enemyPlaceThisTurn[num];
			if (clientCard == null || (clientCard.Location != CardLocation.SpellZone && clientCard.Location != CardLocation.MonsterZone))
			{
				enemyPlaceThisTurn.RemoveAt(num);
			}
		}
		if (quemSummonFlag > 0)
		{
			quemSummonFlag--;
		}
		base.OnChainEnd();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (previousControler == 1 && card != null && card.IsCode(10045474) && previousLocation == 2 && currentLocation == 8)
		{
			enemyActivateInfiniteImpermanenceFromHand = true;
		}
		if (card != null)
		{
			if (currentControler == 1 && (currentLocation == 4 || currentLocation == 8))
			{
				enemyPlaceThisTurn.Add(card);
			}
			if (currentControler == 0)
			{
				base.Duel.GetCurrentSolvingChainCard();
				if (previousLocation == 16 && currentLocation != 16)
				{
					sendToGYThisTurn.Remove(card);
				}
				if (currentLocation == 16)
				{
					if (card.HasType(CardType.Fusion))
					{
						fusionToGYFlag = true;
					}
					sendToGYThisTurn.Add(card);
				}
				if (currentLocation == 4 && card != null && card.IsCode(45883110))
				{
					quemSummonFlag = 2;
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

	public bool TheBystialLubellionSpSummon()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			return false;
		}
		if (TheBystialLubellionSpSummonCost(base.Bot.GetMonsters()) != null)
		{
			theBystialLubellionSelecting = true;
			activatedCardIdList.Add(base.Card.Id - 1);
			return true;
		}
		return false;
	}

	public ClientCard TheBystialLubellionSpSummonCost(IList<ClientCard> costList)
	{
		foreach (KeyValuePair<int, Func<ClientCard, bool>> pair in new Dictionary<int, Func<ClientCard, bool>>
		{
			{
				87746184,
				(ClientCard card) => sendToGYThisTurn.All((ClientCard c) => !c.IsCode(87746184))
			},
			{
				60242223,
				(ClientCard card) => !activatedCardIdList.Contains(60242224) && !CheckWhetherWillbeRemoved()
			},
			{
				41373230,
				(ClientCard card) => base.Util.IsTurn1OrMain2() || card.GetDefensePower() < 2500
			},
			{
				3410461,
				(ClientCard card) => base.Util.IsTurn1OrMain2() || card.IsDisabled() || card.GetDefensePower() < 2500
			},
			{
				25451383,
				(ClientCard card) => base.Util.IsTurn1OrMain2() || card.GetDefensePower() < 2500
			},
			{
				92892239,
				(ClientCard card) => card.IsDisabled() && CheckRemainInDeck(32756828, 18973184) > 0
			}
		})
		{
			foreach (ClientCard item in costList.Where((ClientCard card) => card.IsCode(pair.Key)).ToList())
			{
				if (item != null && pair.Value(item))
				{
					return item;
				}
			}
		}
		return null;
	}

	public bool TheBystialLubellionActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, base.Card.Location == CardLocation.MonsterZone, CardType.Monster))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			activatedCardIdList.Add(base.Card.Id);
		}
		else
		{
			activatedCardIdList.Add(base.Card.Id + 1);
		}
		return true;
	}

	public bool AlbionTheShroudedDragonActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		bool flag = CheckRemainInDeck(17751597, 36637374, 19271881, 29948294) > 0;
		if (base.Bot.HasInGraveyard(17751597))
		{
			flag |= CheckRemainInDeck(44362883, 32756828, 82738008, 34995106, 18973184) > 0;
		}
		if (base.Bot.HasInSpellZone(32756828))
		{
			flag |= CheckRemainInDeck(18973184) > 0;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			flag |= CheckRemainInDeck(34995106) > 0;
		}
		if (FallenOfAlbazSetCheck() && (summoned || !base.Bot.HasInHand(new List<int> { 68468459, 29948294 })))
		{
			flag |= base.Bot.HasInHand(new List<int> { 32756828, 29948294, 34995106, 82738008, 18973184, 36637374, 17751597, 19271881 });
		}
		if (flag)
		{
			activatedCardIdList.Add(base.Card.Id);
			albionTheShroudedDragonSelecting = true;
			return true;
		}
		return false;
	}

	public bool BystialSaronirActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Util.GetLastChainCard() != null && base.Duel.LastChainPlayer == 1)
			{
				List<ClientCard> list = base.Duel.LastChainTargets.Where((ClientCard c) => CheckBystialCanBanish(c)).ToList();
				if (list.Count > 0)
				{
					base.AI.SelectCard(list);
					currentDestroyCardList.Add(list[0]);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			List<ClientCard> list2 = (from c in base.Duel.CurrentChain
				where c != null && c.Controller == 1 && CheckBystialCanBanish(c) && !currentDestroyCardList.Contains(c)
				orderby c.GetDefensePower() descending
				select c).ToList();
			if (list2.Count > 0)
			{
				base.AI.SelectCard(list2);
				currentDestroyCardList.Add(list2[0]);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (!CheckShouldNoMoreSpSummon())
			{
				ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsCode(19096726) ?? false);
				if (clientCard != null && !activatedCardIdList.Contains(19096727))
				{
					base.AI.SelectCard(clientCard);
					currentDestroyCardList.Add(clientCard);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
				ClientCard clientCard2 = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsCode(36577931) ?? false);
				if (clientCard2 != null && !activatedCardIdList.Contains(36577931))
				{
					base.AI.SelectCard(clientCard2);
					currentDestroyCardList.Add(clientCard2);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
				if (base.Bot.HasInGraveyard(32731036) && !activatedCardIdList.Contains(32731036) && base.Duel.Player == 0 && CheckRemainInDeck(18973184, 32756828) > 0 && base.CurrentTiming == -1)
				{
					List<ClientCard> list3 = (from card in base.Enemy.Graveyard
						where card != null && CheckBystialCanBanish(card)
						orderby card.Attack descending
						select card).ToList();
					list3.AddRange((from card in base.Bot.Graveyard
						where card != null && CheckBystialCanBanish(card) && !card.IsCode(32731036) && !CheckWhetherShouldKeepInGrave(card)
						orderby card.Attack
						select card).ToList());
					if (list3.Count > 0)
					{
						base.AI.SelectCard(list3);
						currentDestroyCardList.Add(list3[0]);
						activatedCardIdList.Add(base.Card.Id);
						return true;
					}
				}
			}
			if (base.Bot.UnderAttack && base.Bot.BattlingMonster == null)
			{
				List<ClientCard> list4 = (from c in base.Enemy.Graveyard
					where CheckBystialCanBanish(c)
					orderby c.GetDefensePower() descending
					select c).ToList();
				list4.AddRange(from c in base.Bot.Graveyard
					where CheckBystialCanBanish(c)
					orderby c.GetDefensePower()
					select c);
				if (list4.Count > 0)
				{
					base.AI.SelectCard(list4);
					currentDestroyCardList.Add(list4[0]);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			if (base.Duel.Player == 1 && (base.Duel.Phase == DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2) && !activatedCardIdList.Contains(32756828) && base.Bot.SpellZone.Any((ClientCard c) => c != null && c.IsCode(32756828) && (c.IsFacedown() || !c.IsDisabled())) && !base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(392)) && GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap).Count > 0)
			{
				List<ClientCard> list5 = (from c in base.Enemy.Graveyard
					where CheckBystialCanBanish(c)
					orderby c.GetDefensePower() descending
					select c).ToList();
				list5.AddRange(from c in base.Bot.Graveyard
					where CheckBystialCanBanish(c)
					orderby c.GetDefensePower()
					select c);
				if (list5.Count > 0)
				{
					base.AI.SelectCard(list5);
					currentDestroyCardList.Add(list5[0]);
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
		}
		if (base.Card.Location == CardLocation.Grave && !CheckWhetherWillbeRemoved())
		{
			if (base.Bot.HasInGraveyard(17751597))
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
			if (CheckRemainInDeck(32731036, 17751597, 29948294, 19271881, 36637374) > 0)
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
		}
		return false;
	}

	public bool CheckBystialCanBanish(ClientCard c)
	{
		if (c != null && c.Location == CardLocation.Grave && c.IsMonster())
		{
			return c.HasAttribute((CardAttribute)48);
		}
		return false;
	}

	public bool AluberTheJesterOfDespiaSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || enemyActivateLockBird || activatedCardIdList.Contains(base.Card.Id))
		{
			return false;
		}
		summoned = true;
		return true;
	}

	public bool AluberTheJesterOfDespiaActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeTarget: true, CardType.Monster);
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 0)
		{
			base.AI.SelectCard(monsterListForTargetNegate);
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
		{
			return false;
		}
		base.AI.SelectCard(monsterListForTargetNegate);
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool FallenOfAlbazSummon()
	{
		if (CheckAlbazFusion(base.Card))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool FallenOfAlbazSet()
	{
		if (FallenOfAlbazSetCheck())
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool FallenOfAlbazSetCheck()
	{
		if (!base.Bot.HasInExtra(3410461) || nadirActivated)
		{
			return false;
		}
		if (!base.Bot.HasInSpellZone(18973184, notDisabled: true, faceUp: true) || base.Bot.GetHandCount() < 2)
		{
			foreach (int dangerousDragonId in dangerousDragonIdList)
			{
				if (base.Enemy.HasInMonstersZone(dangerousDragonId, notDisabled: true, hasXyzMaterial: false, faceUp: true))
				{
					return true;
				}
			}
		}
		if ((from c in base.Enemy.GetMonsters()
			where c != null && c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasRace(CardRace.Dragon)
			select c).Count() > 1)
		{
			return true;
		}
		return false;
	}

	public bool CheckAlbazFusion(ClientCard exceptCost = null)
	{
		List<ClientCard> enemyMonsterList;
		return CheckAlbazFusion(exceptCost, out enemyMonsterList);
	}

	public bool CheckAlbazFusion(ClientCard exceptCost, out List<ClientCard> enemyMonsterList)
	{
		enemyMonsterList = null;
		int num = base.Bot.Hand.Where((ClientCard c) => c != exceptCost).Count();
		if (num <= 0 || base.Enemy.GetMonsterCount() == 0)
		{
			return false;
		}
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || activatedCardIdList.Contains(68468459) || nadirActivated)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295) && base.Bot.HasInExtra(44146295))
		{
			ClientCard clientCard = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasType((CardType)75505728)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard };
				return true;
			}
		}
		if (base.Bot.HasInExtra(3410461))
		{
			List<ClientCard> list = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasRace(CardRace.Dragon)
				orderby c.GetDefensePower() descending
				select c).ToList();
			if (list.Count > 0)
			{
				enemyMonsterList = list;
				return true;
			}
		}
		if (base.Bot.HasInExtra(87746184))
		{
			ClientCard clientCard2 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Light)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard2 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard2 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(38811586))
		{
			ClientCard clientCard3 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Light) && c.HasRace(CardRace.SpellCaster)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard3 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard3 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(70534340))
		{
			ClientCard clientCard4 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (num >= 2 && clientCard4 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard4 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(92892239))
		{
			ClientCard clientCard5 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard5 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard5 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(41373230))
		{
			ClientCard clientCard6 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.Attack >= 2500
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard6 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard6 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(51409648))
		{
			ClientCard clientCard7 = (from c in base.Enemy.GetMonsters()
				where c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasRace((CardRace)49664)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard7 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard7 };
				return true;
			}
		}
		if (base.Bot.HasInExtra(1906812))
		{
			ClientCard clientCard8 = (from c in base.Enemy.GetMonsters()
				where c != null && c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && enemyPlaceThisTurn.Contains(c) && c.IsSpecialSummoned && c.GetDefensePower() >= base.Util.GetBestPower(base.Bot)
				orderby c.GetDefensePower() descending
				select c).FirstOrDefault();
			if (clientCard8 != null)
			{
				enemyMonsterList = new List<ClientCard> { clientCard8 };
				return true;
			}
		}
		return false;
	}

	public bool FallenOfAlbazActivate()
	{
		if (base.Bot.HasInExtra(3410461) && base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(dangerousDragonIdList)))
		{
			return false;
		}
		if (CheckAlbazFusion())
		{
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool SpringansKittSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || enemyActivateLockBird || activatedCardIdList.Contains(base.Card.Id + 1))
		{
			return false;
		}
		summoned = true;
		return true;
	}

	public bool SpringansKittActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			if (CheckShouldNoMoreSpSummon() && (!summoned | activatedCardIdList.Contains(44362883) | base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType((CardType)75505728))))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool GuidingQuemTheVirtuousSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (activatedCardIdList.Contains(base.Card.Id))
		{
			return false;
		}
		summoned = true;
		return true;
	}

	public bool GuidingQuemTheVirtuousSummonForSearch()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (activatedCardIdList.Contains(base.Card.Id))
		{
			return false;
		}
		if (base.Bot.HasInGraveyard(17751597) && CheckRemainInDeck(44362883, 18973184, 34995106, 82738008) > 0)
		{
			summoned = true;
			return true;
		}
		if (base.Bot.HasInGraveyard(new int[3] { 44362883, 18973184, 32756828 }) && CheckRemainInDeck(17751597) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool GuidingQuemTheVirtuousActivate()
	{
		int num = -1;
		if (base.ActivateDescription >= base.Util.GetStringId(45883110, 0))
		{
			num = base.ActivateDescription - base.Util.GetStringId(45883110, 0);
		}
		Logger.DebugWriteLine("Guiding desc: " + num);
		Logger.DebugWriteLine("Guiding timing: " + base.CurrentTiming);
		Logger.DebugWriteLine("Guiding flag: " + quemSummonFlag);
		if ((base.ActivateDescription == -1 && quemSummonFlag == 0) || base.ActivateDescription == base.Util.GetStringId(45883110, 1))
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			foreach (KeyValuePair<int, Func<ClientCard, bool>> pair in new List<KeyValuePair<int, Func<ClientCard, bool>>>
			{
				new KeyValuePair<int, Func<ClientCard, bool>>(38811586, (ClientCard c) => c.IsCanRevive() && !activatedCardIdList.Contains(38811586)),
				new KeyValuePair<int, Func<ClientCard, bool>>(44146295, (ClientCard c) => c.IsCanRevive()),
				new KeyValuePair<int, Func<ClientCard, bool>>(68468459, delegate
				{
					if (CheckAlbazFusion(null, out var enemyMonsterList) && !base.Util.ChainContainsCard(new int[2] { 87746184, 70534340 }) && !spSummoningAlbaz)
					{
						bool flag = enemyMonsterList.Count > 1;
						if (enemyMonsterList.Count > 0)
						{
							ClientCard clientCard2 = enemyMonsterList[0];
							flag |= clientCard2.HasType((CardType)75505856);
							flag |= clientCard2.IsFloodgate() || clientCard2.IsOneForXyz() || base.Util.GetWorstBotMonster().GetDefensePower() < clientCard2.Attack;
						}
						return flag;
					}
					return false;
				}),
				new KeyValuePair<int, Func<ClientCard, bool>>(87746184, (ClientCard c) => c.IsCanRevive() && base.Bot.HasInSpellZone(32756828) && base.Bot.MonsterZone.Any((ClientCard oc) => oc != null && oc.IsFaceup() && oc.HasSetcode(392))),
				new KeyValuePair<int, Func<ClientCard, bool>>(95515789, (ClientCard c) => base.Duel.Player == 0 || !activatedCardIdList.Contains(95515790)),
				new KeyValuePair<int, Func<ClientCard, bool>>(19096726, (ClientCard c) => base.Bot.MonsterZone.Any((ClientCard oc) => oc != null && oc.IsFaceup() && oc.IsCode(albazFusionMonster))),
				new KeyValuePair<int, Func<ClientCard, bool>>(38811586, (ClientCard c) => c.IsCanRevive()),
				new KeyValuePair<int, Func<ClientCard, bool>>(45484331, (ClientCard c) => true)
			})
			{
				ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c != null && c.IsOriginalCode(pair.Key) && pair.Value(c));
				if (clientCard != null)
				{
					if (clientCard.IsOriginalCode(68468459))
					{
						spSummoningAlbaz = true;
					}
					base.AI.SelectCard(clientCard);
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
		}
		if ((base.ActivateDescription == -1 && quemSummonFlag > 0) || base.ActivateDescription == base.Util.GetStringId(45883110, 0))
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || CheckWhetherWillbeRemoved())
			{
				return false;
			}
			quemSummonFlag = 0;
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool BlazingCartesiaTheVirtuousSummon()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		bool flag = base.Bot.HasInHandOrInSpellZone(36637374) && !activatedCardIdList.Contains(62962630) && CheckRemainInDeck(62962630) > 0;
		flag |= base.Bot.HasInHand(25451383) && !activatedCardIdList.Contains(25451383);
		flag |= base.Bot.HasInHandOrHasInMonstersZone(60242223) && !activatedCardIdList.Contains(60242224);
		if (base.Bot.HasInExtra(24915933))
		{
			bool flag2 = base.Bot.Hand.Any((ClientCard c) => c != base.Card && c.Attack < 2000 && c.HasAttribute((CardAttribute)48));
			flag2 |= base.Bot.MonsterZone.Any((ClientCard c) => c != null && !c.IsCode(cannotBeFusionMaterialIdList) && c.Attack < 2000 && c.HasAttribute((CardAttribute)48));
			flag = flag || flag2;
		}
		if (flag)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool BlazingCartesiaTheVirtuousActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckShouldNoMoreSpSummon() || CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			if (base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(82738008)))
			{
				return false;
			}
			List<ClientCard> list = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.Attack <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)).ToList();
			list.AddRange(base.Bot.Hand.Where((ClientCard c) => c.IsMonster() && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))));
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && base.Duel.LastChainPlayer == 1)
			{
				List<ClientCard> list2 = base.Duel.LastChainTargets.Where((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.MonsterZone && (!c.IsCode(cannotBeFusionMaterialIdList) || c.Attack <= 2500)).ToList();
				if (list2.Count > 0)
				{
					if (lastChainCard.IsCode(targetNegateIdList))
					{
						list2 = list2.Where((ClientCard c) => c.Attack <= 2500).ToList();
					}
					BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 0, list, list2, out var clientCard, out var selectedFusionMaterialList);
					if (clientCard != null)
					{
						Logger.DebugWriteLine("cartesia prepare fusion1: " + clientCard.Name);
						cartesiaMaterialList.AddRange(selectedFusionMaterialList.Intersect(list2));
						activatedCardIdList.Add(base.Card.Id + 1);
						return true;
					}
				}
			}
			if (!((CheckWhetherCanSummon() && !activatedCardIdList.Contains(62962630) && base.Bot.HasInHand(62962630)) | (CheckWhetherCanSummon() && !activatedCardIdList.Contains(45484331) && base.Bot.HasInHand(45484331))))
			{
				ClientCard clientCard2 = base.Duel.CurrentChain.FirstOrDefault((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.Hand && c.IsOriginalCode(25451383));
				if (clientCard2 != null)
				{
					BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 0, list, new List<ClientCard> { clientCard2 }, out var clientCard3, out var selectedFusionMaterialList2);
					if (clientCard3 != null)
					{
						Logger.DebugWriteLine("cartesia prepare fusion2: " + clientCard3.Name);
						cartesiaMaterialList.AddRange(selectedFusionMaterialList2.Intersect(new List<ClientCard> { clientCard2 }));
						activatedCardIdList.Add(base.Card.Id + 1);
						return true;
					}
				}
			}
			bool flag = (base.Duel.Player == 0 && !CheckShouldNoMoreSpSummon()) || base.Duel.Player == 1;
			List<ClientCard> selectedFusionMaterialList3;
			if (!base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true) && flag)
			{
				BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 44146295, list, null, out var clientCard4, out selectedFusionMaterialList3);
				if (clientCard4 != null)
				{
					Logger.DebugWriteLine("cartesia prepare fusion3: " + clientCard4.Name);
					cartesiaSummonGoal = 44146295;
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
			if (flag && base.Duel.Player == 0)
			{
				foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
				{
					{
						60242223,
						() => !activatedCardIdList.Contains(60242224) && !DefaultCheckWhetherCardIdIsNegated(60242223)
					},
					{
						36577931,
						() => !activatedCardIdList.Contains(36577931) && !DefaultCheckWhetherCardIdIsNegated(36577931)
					}
				})
				{
					ClientCard clientCard5 = list.FirstOrDefault((ClientCard c) => c.IsCode(pair.Key));
					if (clientCard5 != null && pair.Value())
					{
						BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 24915933, list, new List<ClientCard> { clientCard5 }, out var clientCard6, out var _);
						if (clientCard6 != null)
						{
							Logger.DebugWriteLine("cartesia prepare fusion4: " + clientCard6.Name);
							cartesiaSummonGoal = 24915933;
							cartesiaMaterialList.Add(clientCard5);
							activatedCardIdList.Add(base.Card.Id + 1);
							return true;
						}
					}
				}
			}
			if (flag)
			{
				BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 38811586, list, new List<ClientCard> { base.Card }, out var clientCard7, out var _);
				if (clientCard7 != null)
				{
					Logger.DebugWriteLine("cartesia prepare fusion5: " + clientCard7.Name);
					cartesiaSummonGoal = 38811586;
					cartesiaMaterialList.Add(base.Card);
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
			if (flag && GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster) != null)
			{
				BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 92892239, list, null, out var clientCard8, out selectedFusionMaterialList3);
				if (clientCard8 != null)
				{
					Logger.DebugWriteLine("cartesia prepare fusion6: " + clientCard8.Name);
					cartesiaSummonGoal = 92892239;
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
			if (flag && ((base.Duel.Player == 0 && base.CurrentTiming == -1) | (base.Duel.Player == 1 && (base.CurrentTiming & 4) != 0)))
			{
				BlazingCartesiaTheVirtuousFusionCheck(base.Bot.ExtraDeck, 0, list, null, out var clientCard9, out selectedFusionMaterialList3);
				if (clientCard9 != null)
				{
					Logger.DebugWriteLine("cartesia prepare fusion7: " + clientCard9.Name);
					activatedCardIdList.Add(base.Card.Id + 1);
					return true;
				}
			}
		}
		return false;
	}

	public bool BlazingCartesiaTheVirtuousActivateInGrave()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 2);
			return true;
		}
		return false;
	}

	public void BlazingCartesiaTheVirtuousFusionCheck(IList<ClientCard> canSummonList, int mustSummonId, List<ClientCard> materialList, List<ClientCard> mustMaterialList, out ClientCard fusionTarget, out List<ClientCard> selectedFusionMaterialList)
	{
		fusionTarget = null;
		selectedFusionMaterialList = new List<ClientCard>();
		Dictionary<int, List<Func<ClientCard, bool>>> obj = new Dictionary<int, List<Func<ClientCard, bool>>>
		{
			{
				24915933,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsCode(95515789),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute((CardAttribute)48)
				}
			},
			{
				38811586,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsCode(68468459),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Light) && c.HasRace(CardRace.SpellCaster)
				}
			},
			{
				44146295,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsCode(68468459),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasType((CardType)75505728)
				}
			},
			{
				87746184,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsCode(68468459),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Light)
				}
			},
			{
				70534340,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.IsCode(68468459),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark)
				}
			},
			{
				72272462,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => c.HasSetcode(356),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute((CardAttribute)48)
				}
			},
			{
				92892239,
				new List<Func<ClientCard, bool>>
				{
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon),
					(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon)
				}
			}
		};
		Dictionary<int, Func<ClientCard, ClientCard, bool>> dictionary = new Dictionary<int, Func<ClientCard, ClientCard, bool>>
		{
			{
				38811586,
				delegate
				{
					int num5 = base.Bot.Graveyard.Count((ClientCard clientCard4) => clientCard4 != null && clientCard4.IsMonster() && clientCard4.IsCanRevive());
					num5 += base.Enemy.Graveyard.Count((ClientCard clientCard4) => clientCard4 != null && clientCard4.IsMonster() && clientCard4.IsCanRevive());
					if (!CheckWhetherWillbeRemoved() || ((base.CurrentTiming & 4) > 0 && base.Util.GetOneEnemyBetterThanValue(base.Card.GetDefensePower()) != null && base.Util.GetOneEnemyBetterThanValue(3000) == null))
					{
						num5 += 2;
					}
					return num5 >= 2;
				}
			},
			{
				70534340,
				(ClientCard c1, ClientCard c2) => base.Bot.Hand.Count((ClientCard clientCard4) => clientCard4 != c1 && clientCard4 != c2) > 0
			},
			{
				44146295,
				(ClientCard c1, ClientCard c2) => !CheckWhetherWillbeRemoved() && !base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true)
			}
		};
		foreach (KeyValuePair<int, List<Func<ClientCard, bool>>> pair in obj)
		{
			if (mustSummonId > 0 && mustSummonId != pair.Key)
			{
				continue;
			}
			ClientCard clientCard = canSummonList.FirstOrDefault((ClientCard c) => c?.IsCode(pair.Key) ?? false);
			if (clientCard == null)
			{
				continue;
			}
			Func<ClientCard, bool> func = pair.Value[0];
			Func<ClientCard, bool> func2 = pair.Value[1];
			if (mustMaterialList != null && mustMaterialList.Count > 0)
			{
				foreach (ClientCard mustMaterial in mustMaterialList)
				{
					if (!func(mustMaterial) && !func2(mustMaterial))
					{
						continue;
					}
					foreach (ClientCard material in materialList)
					{
						if (material != mustMaterial)
						{
							bool num = (func(mustMaterial) && func2(material)) | (func2(mustMaterial) && func(material));
							dictionary.TryGetValue(pair.Key, out var value);
							if (num & (value?.Invoke(mustMaterial, material) ?? true))
							{
								fusionTarget = clientCard;
								selectedFusionMaterialList.Add(mustMaterial);
								selectedFusionMaterialList.Add(material);
								return;
							}
						}
					}
				}
				continue;
			}
			for (int num2 = 0; num2 < materialList.Count - 1; num2++)
			{
				ClientCard clientCard2 = materialList[num2];
				if (!func(clientCard2) && !func2(clientCard2))
				{
					continue;
				}
				for (int num3 = num2 + 1; num3 < materialList.Count; num3++)
				{
					ClientCard clientCard3 = materialList[num3];
					bool num4 = (func(clientCard2) && func2(clientCard3)) | (func2(clientCard2) && func(clientCard3));
					dictionary.TryGetValue(pair.Key, out var value2);
					if (num4 & (value2?.Invoke(clientCard2, clientCard3) ?? true))
					{
						fusionTarget = clientCard;
						selectedFusionMaterial.Add(clientCard2);
						selectedFusionMaterial.Add(clientCard3);
						return;
					}
				}
			}
		}
	}

	public bool TriBrigadeMercourierActivate()
	{
		if (base.Card.Location == CardLocation.Hand || base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster) || !CheckLastChainShouldNegated())
			{
				return false;
			}
			if (base.Util.GetLastChainCard().Location == CardLocation.MonsterZone)
			{
				currentNegateCardList.Add(base.Util.GetLastChainCard());
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		_ = base.Card.Location;
		_ = 32;
		return false;
	}

	public bool TriBrigadeMercourierActivateForSearch()
	{
		if (base.Card.Location == CardLocation.Hand || base.Card.Location == CardLocation.MonsterZone)
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Removed)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
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
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard.Location == CardLocation.MonsterZone || lastChainCard.Location == CardLocation.SpellZone)
			{
				currentNegateCardList.Add(base.Util.GetLastChainCard());
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

	public bool DespianTragedyActivate()
	{
		if (base.ActivateDescription != base.Util.GetStringId(base.Card.Id, 1))
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Trap))
		{
			return false;
		}
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				44362883,
				() => BrandedFusionActivateCheck()
			},
			{
				18973184,
				delegate
				{
					if (base.Duel.Player == 0 && base.Duel.Phase >= DuelPhase.End)
					{
						return false;
					}
					if (base.Bot.HasInHandOrInSpellZone(44362883) && BrandedFusionActivateCheck())
					{
						return true;
					}
					if (base.Bot.HasInHandOrInSpellZone(34995106) && BrandedInWhiteActivateCheck())
					{
						return true;
					}
					if (base.Bot.HasInHandOrInSpellZone(82738008) && BrandedInRedActivateCheck() != null)
					{
						return true;
					}
					if (!summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion())
					{
						return true;
					}
					return (base.Bot.HasInMonstersZone(95515789) || (!summoned && base.Bot.HasInHand(95515789))) ? true : false;
				}
			},
			{ 29948294, BrandedInHighSpiritsActivateCheck },
			{
				82738008,
				() => BrandedInRedActivateCheck() != null
			},
			{ 34995106, BrandedInWhiteActivateCheck },
			{
				19271881,
				() => base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(albazFusionMonster))
			},
			{
				36637374,
				() => base.Bot.Hand.Count > 2 && !activatedCardIdList.Contains(36637374)
			}
		})
		{
			ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
			if (clientCard != null && pair.Value())
			{
				activatedCardIdList.Add(base.Card.Id);
				base.AI.SelectCard(clientCard);
				SelectSTPlace(clientCard, avoidImpermanence: true);
				return true;
			}
		}
		return false;
	}

	public bool DespianTragedySet()
	{
		if (base.Bot.Graveyard.Any((ClientCard c) => c != null && c.HasType((CardType)6) && c.HasSetcode(349)))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool NadirServantActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (NadirServantActivateCheck(null, force: false, out var _))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool NadirServantActivateCheck(IList<ClientCard> cards, bool force, out ClientCard target)
	{
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				87746184,
				() => !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184))
			},
			{
				53971455,
				() => CheckRemainInDeck(95515789, 45883110) > 0
			},
			{
				41373230,
				() => CheckRemainInDeck(45883110) > 0
			},
			{
				1906812,
				() => CheckRemainInDeck(45484331) > 0
			},
			{
				51409648,
				() => base.Bot.Graveyard.Any((ClientCard c) => c?.IsOriginalCode(68468459) ?? false)
			},
			{
				3410461,
				() => force && CheckRemainInDeck(6498706, 44362883) > 0
			},
			{
				24915933,
				() => force
			}
		})
		{
			if (cards == null)
			{
				if (base.Bot.HasInExtra(pair.Key) && pair.Value())
				{
					target = null;
					return true;
				}
				continue;
			}
			ClientCard clientCard = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(pair.Key));
			if (clientCard != null && pair.Value())
			{
				target = clientCard;
				return true;
			}
		}
		target = null;
		return false;
	}

	public bool FusionDeploymentActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if (FusionDeploymentSpSummonTarget() > 0 && !base.Bot.HasInHand(18973184))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public int FusionDeploymentSpSummonTarget()
	{
		if (CheckRemainInDeck(68468459) > 0 && CheckAlbazFusion(base.Card) && GetProblematicEnemyMonster(0, canBeTarget: false, ignoreCurrentDestroy: false, CardType.Monster) != null)
		{
			return 68468459;
		}
		if (CheckRemainInDeck(95515789) > 0 && base.Bot.HasInExtra(24915933) && (base.Bot.Hand.Any((ClientCard c) => c.IsMonster() && c.HasAttribute((CardAttribute)48)) || base.Bot.GetMonsters().Any((ClientCard c) => c.IsMonster() && c.HasAttribute((CardAttribute)48) && !c.IsCode(cannotBeFusionMaterialIdList))))
		{
			return 95515789;
		}
		if (CheckRemainInDeck(68468459) > 0 && CheckAlbazFusion(base.Card))
		{
			return 68468459;
		}
		return 0;
	}

	public bool BrandedInWhiteActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Spell))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			SelectSTPlace(base.Card);
			return true;
		}
		if (_BrandedInWhiteActivateCheck(activate: true))
		{
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool BrandedInWhiteActivateCheck()
	{
		return _BrandedInWhiteActivateCheck();
	}

	public bool _BrandedInWhiteActivateCheck(bool activate = false)
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || activatedCardIdList.Contains(34995106) || nadirActivated)
		{
			return false;
		}
		if (CheckShouldNoMoreSpSummon() && base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.GetDefensePower() >= 2000))
		{
			return false;
		}
		if (BrandedInWhiteFusionTarget(base.Bot.ExtraDeck, out var target) > 0)
		{
			if (activate)
			{
				Logger.DebugWriteLine("White prepare fusion: " + target?.Name);
			}
			return true;
		}
		return false;
	}

	public int BrandedInWhiteFusionTarget(IList<ClientCard> cards, out ClientCard target)
	{
		target = null;
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				44146295,
				delegate
				{
					if (base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) || base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true))
					{
						return false;
					}
					return (base.Bot.Graveyard.Any((ClientCard c) => c.IsCode(68468459)) | base.Bot.MonsterZone.Any((ClientCard c) => c?.IsOriginalCode(68468459) ?? false) | base.Bot.Hand.Any((ClientCard c) => c.IsOriginalCode(68468459))) && (base.Bot.Graveyard.Any((ClientCard c) => c != null && !sendToGYThisTurn.Contains(c) && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasType((CardType)75505728)) | base.Bot.MonsterZone.Any((ClientCard c) => c != null && !c.IsCode(cannotBeFusionMaterialIdList) && (c.IsCode(albazFusionMonster) || c.IsCode(24915933))));
				}
			},
			{
				92892239,
				delegate
				{
					if (base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() > 0)
					{
						List<ClientCard> list = base.Bot.Hand.Where((ClientCard c) => c != null && c.IsMonster() && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon)).ToList();
						list.AddRange(base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.IsMonster() && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon) && !c.IsCode(cannotBeFusionMaterialIdList)).ToList());
						List<ClientCard> list2 = base.Bot.Graveyard.Where((ClientCard c) => c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList) && !CheckWhetherShouldKeepInGrave(c)).ToList();
						bool num = list.Any((ClientCard c) => c.IsCode(68468459)) || list2.Any((ClientCard c) => c.IsCode(68468459));
						int num2 = list.Count;
						if (num)
						{
							num2 += list2.Count;
						}
						return num2 >= 2;
					}
					return false;
				}
			},
			{
				11321089,
				delegate
				{
					if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || DefaultCheckWhetherCardIdIsNegated(11321089))
					{
						return false;
					}
					return base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() != 0 && ChimeraFusionMaterialList().Count > 0;
				}
			},
			{
				70534340,
				delegate
				{
					if (activatedCardIdList.Contains(70534340) || DefaultCheckWhetherCardIdIsNegated(70534340) || CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
					{
						return false;
					}
					List<ClientCard> list = new List<ClientCard>(from c in base.Bot.Graveyard
						where c?.IsMonster() ?? false
						orderby c.GetDefensePower()
						select c).ToList();
					list.AddRange(from c in base.Bot.GetMonsters()
						orderby c.GetDefensePower()
						select c);
					list.AddRange(base.Bot.Hand);
					ClientCard albaz = (from c in list
						where c.IsCode(68468459)
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					ClientCard darkMonster = list.Where((ClientCard c) => c != albaz && c.HasAttribute(CardAttribute.Dark)).FirstOrDefault();
					if (albaz == null || darkMonster == null)
					{
						return false;
					}
					return (base.Bot.Hand.Count((ClientCard c) => c != albaz && c != darkMonster && !c.IsCode(34995106)) != 0) ? true : false;
				}
			},
			{
				38811586,
				delegate
				{
					List<ClientCard> list = new List<ClientCard>(from c in base.Bot.Graveyard
						where c?.IsMonster() ?? false
						orderby c.GetDefensePower()
						select c).ToList();
					list.AddRange(from c in base.Bot.GetMonsters()
						orderby c.GetDefensePower()
						select c);
					list.AddRange(base.Bot.Hand);
					ClientCard albaz = list.FirstOrDefault((ClientCard c) => c.IsCode(68468459));
					ClientCard lightSpellcaster = list.FirstOrDefault((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.HasAttribute(CardAttribute.Light));
					return albaz != null && lightSpellcaster != null && base.Enemy.GetGraveyardMonsters().Count + base.Bot.Graveyard.Where((ClientCard c) => c.IsMonster() && c != albaz && c != lightSpellcaster).Count() + (base.Bot.HasInHand(23434538) ? 1 : 0) >= 2;
				}
			},
			{
				51409648,
				() => base.Bot.HasInGraveyard(19096726) && (base.Bot.HasInHandOrHasInMonstersZone(68468459) | base.Bot.HasInGraveyard(68468459))
			},
			{
				72272462,
				delegate
				{
					if (base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.Attack >= 2500 && (!c.HasType(CardType.Fusion) || c.Level < 8)))
					{
						if ((from c in base.Bot.Graveyard
							where c != null && c.HasSetcode(356) && !CheckWhetherShouldKeepInGrave(c)
							orderby c.GetDefensePower()
							select c).FirstOrDefault() != null)
						{
							return base.Bot.HasInHandOrHasInMonstersZone(68468459) | base.Bot.HasInGraveyard(68468459);
						}
						List<ClientCard> list = (from c in base.Bot.Hand
							where c.IsMonster()
							orderby c.GetDefensePower()
							select c).ToList();
						list.AddRange((from c in base.Bot.MonsterZone
							where c != null && !c.IsCode(cannotBeFusionMaterialIdList)
							orderby c.GetDefensePower()
							select c).ToList());
						ClientCard despian = list.FirstOrDefault((ClientCard c) => c.HasSetcode(356));
						if (despian != null)
						{
							return list.Any((ClientCard c) => c != despian && c.HasAttribute((CardAttribute)48)) | base.Bot.HasInGraveyard(68468459);
						}
					}
					return false;
				}
			},
			{
				41373230,
				delegate
				{
					List<ClientCard> list = new List<ClientCard>(from c in base.Bot.Graveyard
						where c?.IsMonster() ?? false
						orderby c.GetDefensePower()
						select c).ToList();
					list.AddRange(from c in base.Bot.GetMonsters()
						orderby c.GetDefensePower()
						select c);
					list.AddRange(base.Bot.Hand);
					ClientCard clientCard = (from c in list
						where c.IsCode(68468459)
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					foreach (ClientCard item in list)
					{
						if (item != clientCard && item.IsMonster() && item.Attack >= 2500 && !item.IsCode(cannotBeFusionMaterialIdList) && !base.Util.IsTurn1OrMain2())
						{
							bool num = base.Enemy.GetMonsterCount() == 0 && !CheckWhetherShouldKeepInGrave(item) && (item.IsFacedown() || item.Location != CardLocation.MonsterZone);
							int expectedAttack = 2900 + item.Level * 100;
							int botBestPower = base.Util.GetBestPower(base.Bot);
							int num2 = base.Enemy.MonsterZone.Count((ClientCard c) => c != null && c.GetDefensePower() >= botBestPower);
							int num3 = base.Enemy.MonsterZone.Count((ClientCard c) => c != null && c.GetDefensePower() >= expectedAttack);
							return num || num3 < num2;
						}
					}
					return false;
				}
			},
			{
				3410461,
				delegate
				{
					if (base.Util.GetOneEnemyBetterThanMyBest() == null && base.Duel.MainPhase.CanBattlePhase)
					{
						ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsOriginalCode(68468459) ?? false);
						if (clientCard == null)
						{
							clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsCode(68468459) ?? false);
						}
						if (clientCard == null)
						{
							return false;
						}
						foreach (ClientCard item2 in base.Bot.Graveyard)
						{
							if (item2 != null && item2 != clientCard && item2.IsMonster() && item2.HasRace(CardRace.Dragon) && !item2.IsCode(cannotBeFusionMaterialIdList))
							{
								return true;
							}
						}
					}
					return false;
				}
			}
		})
		{
			target = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
			if (target != null && pair.Value())
			{
				return pair.Key;
			}
		}
		target = null;
		return 0;
	}

	public List<ClientCard> ChimeraFusionMaterialList(bool dragonCheck = true)
	{
		int num = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount();
		List<ClientCard> list = (from c in base.Bot.MonsterZone
			where c != null && c.GetDefensePower() <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)
			orderby c.GetDefensePower()
			select c).ToList();
		List<ClientCard> list2 = (from c in base.Bot.Hand
			where c.IsMonster()
			orderby c.GetDefensePower()
			select c).ToList();
		if (num >= 2 && list.Count >= 2)
		{
			if (list.Count < 2 || list2.Count < 1)
			{
				return new List<ClientCard>();
			}
			foreach (ClientCard item in list2)
			{
				for (int num2 = 0; num2 < list.Count - 1; num2++)
				{
					ClientCard clientCard = list[num2];
					if (clientCard.IsCode(item.Id) || item.IsCode(clientCard.Id))
					{
						continue;
					}
					for (int num3 = num2 + 1; num3 < list.Count; num3++)
					{
						ClientCard clientCard2 = list[num3];
						if (!clientCard2.IsCode(item.Id) && !item.IsCode(clientCard2.Id) && !clientCard2.IsCode(clientCard.Id) && !clientCard.IsCode(clientCard2.Id))
						{
							List<ClientCard> list3 = new List<ClientCard> { item, clientCard, clientCard2 };
							if (dragonCheck && list3.Any((ClientCard c) => c.HasRace(CardRace.Dragon)))
							{
								return list3;
							}
						}
					}
				}
			}
		}
		if (num == 1 || list.Count == 1)
		{
			if (list.Count < 1 || list2.Count < 2)
			{
				return new List<ClientCard>();
			}
			foreach (ClientCard item2 in list)
			{
				for (int num4 = 0; num4 < list2.Count - 1; num4++)
				{
					ClientCard clientCard3 = list2[num4];
					if (clientCard3.IsCode(item2.Id) || item2.IsCode(clientCard3.Id))
					{
						continue;
					}
					for (int num5 = num4 + 1; num5 < list2.Count; num5++)
					{
						ClientCard clientCard4 = list2[num5];
						if (!clientCard4.IsCode(item2.Id) && !item2.IsCode(clientCard4.Id) && !clientCard4.IsCode(clientCard3.Id) && !clientCard3.IsCode(clientCard4.Id))
						{
							List<ClientCard> list4 = new List<ClientCard> { item2, clientCard3, clientCard4 };
							if (dragonCheck && list4.Any((ClientCard c) => c.HasRace(CardRace.Dragon)))
							{
								return list4;
							}
						}
					}
				}
			}
		}
		return new List<ClientCard>();
	}

	public bool BrandedFusionActivate()
	{
		if (BrandedFusionActivateCheck())
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool BrandedFusionActivateCheck(bool endPhaseCheck = true)
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || activatedCardIdList.Contains(44362883))
		{
			return false;
		}
		if (!base.Bot.HasInHandOrHasInMonstersZone(68468459) && CheckRemainInDeck(68468459) == 0)
		{
			return false;
		}
		if (endPhaseCheck && base.Duel.Phase >= DuelPhase.End)
		{
			return false;
		}
		return true;
	}

	public bool GoldSarcophagusActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if (GoldSarcophagusTarget(null, out var _) > 0)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public int GoldSarcophagusTarget(IList<ClientCard> cards, out ClientCard target)
	{
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				36577931,
				() => !activatedCardIdList.Contains(36577931) && !DefaultCheckWhetherCardIdIsNegated(36577931)
			},
			{
				19096726,
				() => !activatedCardIdList.Contains(19096727) && !DefaultCheckWhetherCardIdIsNegated(19096726)
			}
		})
		{
			int key = pair.Key;
			if (!pair.Value())
			{
				continue;
			}
			if (cards != null)
			{
				target = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
				if (target != null && pair.Value())
				{
					return key;
				}
			}
			else if (CheckRemainInDeck(key) > 0)
			{
				target = null;
				return key;
			}
		}
		target = null;
		return 0;
	}

	public bool FoolishBurialActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (FoolishBurialTarget(null, out var _) > 0)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public int FoolishBurialTarget(IList<ClientCard> cards, out ClientCard target)
	{
		if (!activatedCardIdList.Contains(36577931) && !DefaultCheckWhetherCardIdIsNegated(36577931))
		{
			if (cards != null)
			{
				target = cards.FirstOrDefault((ClientCard c) => c.IsCode(36577931));
				if (target != null)
				{
					return 36577931;
				}
			}
			else if (CheckRemainInDeck(36577931) > 0)
			{
				target = null;
				return 36577931;
			}
		}
		if ((CheckRemainInDeck(17751597) > 0) | (base.Bot.HasInGraveyard(17751597) && CheckRemainInDeck(44362883) > 0))
		{
			foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
			{
				{
					60242223,
					() => !activatedCardIdList.Contains(60242224) && !DefaultCheckWhetherCardIdIsNegated(60242223)
				},
				{
					25451383,
					() => !activatedCardIdList.Contains(25451383) && !DefaultCheckWhetherCardIdIsNegated(25451383)
				}
			})
			{
				int key = pair.Key;
				if (!pair.Value())
				{
					continue;
				}
				if (cards != null)
				{
					target = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
					if (target != null && pair.Value())
					{
						return key;
					}
				}
				else if (CheckRemainInDeck(key) > 0)
				{
					target = null;
					return key;
				}
			}
		}
		if (!base.Bot.HasInGraveyard(68468459) && (base.Bot.HasInHand(new List<int> { 82738008, 34995106 }) | (base.Bot.HasInHand(95515789) && base.Bot.MonsterZone.Count((ClientCard c) => c != null && c.Sequence < 5) < 5)))
		{
			int num = ((!base.Bot.HasInHandOrInSpellZone(44362883)) ? 1 : 2);
			if (cards != null)
			{
				List<ClientCard> list = cards.Where((ClientCard c) => c.IsCode(68468459)).ToList();
				if (list.Count >= num)
				{
					target = list.First();
					return 68468459;
				}
			}
			else if (CheckRemainInDeck(68468459) >= num)
			{
				target = null;
				return 68468459;
			}
		}
		target = null;
		return 0;
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
				if (DefaultCheckWhetherCardIdIsNegated(code))
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
					currentDestroyCardList.Add(firstMatchingCard);
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
				List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => card.IsMonster()).ToList();
				if (list.Count > 0)
				{
					list.Sort(CardContainer.CompareCardAttack);
					list.Reverse();
					int id = list[0].Id;
					base.AI.SelectCard(id);
					currentDestroyCardList.Add(list[0]);
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

	public bool BrandedInHighSpiritsActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Spell))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (BrandedInHighSpiritsActivateCheck())
		{
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool BrandedInHighSpiritsActivateCheck()
	{
		bool lubellionCheck = base.Bot.HasInHand(32731036) && CheckRemainInDeck(60242223) > 0 && !activatedCardIdList.Contains(32731036) && base.Duel.Player == 0 && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2) && !CheckWhetherWillbeRemoved();
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || activatedCardIdList.Contains(29948294) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		foreach (KeyValuePair<int, Func<bool>> item in new Dictionary<int, Func<bool>>
		{
			{
				87746184,
				() => !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184)) && !lubellionCheck && base.Bot.Hand.Any((ClientCard c) => BrandedInHighSpiritDiscardDragonCheck(c))
			},
			{
				41373230,
				() => !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(41373230)) && !lubellionCheck && base.Bot.Hand.Any((ClientCard c) => BrandedInHighSpiritDiscardDragonCheck(c)) && CheckRemainInDeck(45883110, 68468459) > 0
			},
			{
				24915933,
				() => base.Bot.Hand.Any((ClientCard c) => c.HasRace(CardRace.SpellCaster) && (!CheckWhetherCanSummon() || !c.IsOriginalCode(45883110)))
			},
			{
				51409648,
				() => base.Bot.Hand.Any((ClientCard c) => c.HasRace(CardRace.WindBeast) && (!c.IsCode(19096726) || !base.Bot.MonsterZone.Any((ClientCard clientCard) => clientCard != null && clientCard.IsFaceup() && clientCard.IsCode(albazFusionMonster))))
			}
		})
		{
			if (base.Bot.HasInExtra(item.Key) && item.Value())
			{
				return true;
			}
		}
		return false;
	}

	public bool BrandedInHighSpiritDiscardDragonCheck(ClientCard card)
	{
		if (!card.HasRace(CardRace.Dragon))
		{
			return false;
		}
		if (base.Duel.Player == 0 && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2))
		{
			if (card.IsOriginalCode(25451383) && !activatedCardIdList.Contains(25451383))
			{
				return false;
			}
			if (card.IsOriginalCode(32731036) && !activatedCardIdList.Contains(32731036) && CheckRemainInDeck(60242223) > 0)
			{
				return false;
			}
		}
		return true;
	}

	public bool BrandedOpeningActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if (base.Duel.Player == 0)
		{
			if (base.Bot.HasInHand(25451383) && !CheckWhetherWillbeRemoved() && !activatedCardIdList.Contains(25451383))
			{
				return false;
			}
			if ((base.Bot.HasInHand(95515789) && !summoned) | (!activatedCardIdList.Contains(6498706) && base.Bot.HasInHandOrInSpellZone(6498706) && !CheckShouldNoMoreSpSummon() && base.Bot.HasInExtra(24915933) && CheckRemainInDeck(95515789) > 0))
			{
				return false;
			}
		}
		if ((CheckRemainInDeck(62962630) > 0 && !activatedCardIdList.Contains(62962630) && !enemyActivateLockBird) | (CheckRemainInDeck(45883110) > 0))
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			activatedCardIdList.Add(base.Card.Id);
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
				return true;
			}
		}
		return false;
	}

	public bool BrandedInRedActivate()
	{
		ClientCard clientCard = BrandedInRedActivateCheck(updateMaterialList: true);
		if (clientCard != null)
		{
			base.AI.SelectCard(clientCard);
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public ClientCard BrandedInRedActivateCheck(bool updateMaterialList = false)
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell) || activatedCardIdList.Contains(82738008))
		{
			return null;
		}
		if (base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(95515789)))
		{
			return null;
		}
		if (base.Duel.CurrentChain.Any((ClientCard c) => c != null && c.Controller == 0 && c.IsCode(87746184)) && !base.Util.ChainContainPlayer(1))
		{
			return null;
		}
		if (nadirActivated)
		{
			return null;
		}
		List<ClientCard> list = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.Attack <= 2500 && !c.IsCode(cannotBeFusionMaterialIdList)).ToList();
		list.AddRange(base.Bot.Hand.Where((ClientCard c) => c.IsMonster() && (!CheckWhetherCanSummon() || ((activatedCardIdList.Contains(62962630) || !c.IsCode(62962630)) && (activatedCardIdList.Contains(45484331) || !c.IsCode(45484331))))));
		List<ClientCard> list2 = base.Bot.Graveyard.Where((ClientCard c) => c != null && c.IsMonster() && !c.HasType((CardType)8256) && (c.IsCode(68468459) || c.HasSetcode(356))).ToList();
		List<ClientCard> selectedFusionMaterialList;
		if (base.Duel.LastChainPlayer == 1)
		{
			List<ClientCard> list3 = base.Duel.LastChainTargets.Where((ClientCard c) => c != null && c.Location == CardLocation.Grave && c.Controller == 0 && !c.HasType((CardType)8256) && (c.IsCode(68468459) || c.HasSetcode(356))).ToList();
			if (list3.Count > 0)
			{
				foreach (ClientCard item in list3)
				{
					List<ClientCard> materialList = new List<ClientCard>(list) { item };
					BrandedInRedFusionCheck(base.Bot.ExtraDeck, 0, materialList, new List<ClientCard> { item }, out var clientCard, out selectedFusionMaterialList);
					if (clientCard != null)
					{
						if (updateMaterialList)
						{
							Logger.DebugWriteLine("Red prepare fusion 1: " + clientCard.Name);
						}
						return item;
					}
				}
			}
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null)
			{
				List<ClientCard> list4 = base.Duel.LastChainTargets.Where((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.MonsterZone && (!c.IsCode(cannotBeFusionMaterialIdList) || c.Attack <= 2500)).ToList();
				if (list4.Count > 0)
				{
					if (lastChainCard.IsCode(targetNegateIdList))
					{
						list4 = list4.Where((ClientCard c) => c.Attack <= 2500 && !c.IsCode(87746184)).ToList();
					}
					foreach (ClientCard item2 in list2)
					{
						List<ClientCard> materialList2 = new List<ClientCard>(list) { item2 };
						BrandedInRedFusionCheck(base.Bot.ExtraDeck, 0, materialList2, list4, out var clientCard2, out var selectedFusionMaterialList2);
						if (clientCard2 != null)
						{
							if (updateMaterialList)
							{
								Logger.DebugWriteLine("Red prepare fusion 2: " + clientCard2.Name);
								brandedInRedMaterialList.AddRange(selectedFusionMaterialList2.Intersect(list4));
							}
							return item2;
						}
					}
				}
			}
		}
		bool num = base.Duel.Player == 0 && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2) && !summoned && (base.Bot.HasInHand(new int[3] { 62962630, 45883110, 45484331 }) || (base.Bot.HasInHand(68468459) && CheckAlbazFusion(base.Card)));
		bool flag = base.Duel.Player == 1 || base.CurrentTiming == -1;
		if (num || !flag)
		{
			return null;
		}
		if (!base.Bot.MonsterZone.Any((ClientCard c) => c?.HasType(CardType.Fusion) ?? false) && base.Duel.LastChainPlayer != 0 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) && !activatedCardIdList.Contains(70534340))
		{
			foreach (ClientCard item3 in list2)
			{
				List<ClientCard> materialList3 = new List<ClientCard>(list) { item3 };
				BrandedInRedFusionCheck(base.Bot.ExtraDeck, 70534340, materialList3, new List<ClientCard> { item3 }, out var clientCard3, out selectedFusionMaterialList);
				if (clientCard3 != null)
				{
					return item3;
				}
			}
		}
		if (GetProblematicEnemyCardList(canBeTarget: false, ignoreSpells: false, CardType.Monster).Count > 0 || (base.Duel.Phase == DuelPhase.End && base.Duel.Player == 1))
		{
			if (!enemyActivateLockBird)
			{
				foreach (ClientCard item4 in list2)
				{
					List<ClientCard> materialList4 = new List<ClientCard>(list) { item4 };
					BrandedInRedFusionCheck(base.Bot.ExtraDeck, 11321089, materialList4, new List<ClientCard> { item4 }, out var clientCard4, out selectedFusionMaterialList);
					if (clientCard4 != null)
					{
						if (updateMaterialList)
						{
							Logger.DebugWriteLine("Red prepare fusion 3: " + clientCard4.Name);
						}
						return item4;
					}
				}
			}
			foreach (ClientCard item5 in list2)
			{
				List<ClientCard> materialList5 = new List<ClientCard>(list) { item5 };
				BrandedInRedFusionCheck(base.Bot.ExtraDeck, 92892239, materialList5, new List<ClientCard> { item5 }, out var clientCard5, out selectedFusionMaterialList);
				if (clientCard5 != null)
				{
					if (updateMaterialList)
					{
						Logger.DebugWriteLine("Red prepare fusion 4: " + clientCard5.Name);
					}
					return item5;
				}
			}
		}
		return null;
	}

	public void BrandedInRedFusionCheck(IList<ClientCard> canSummonList, int mustSummonId, List<ClientCard> materialList, List<ClientCard> mustMaterialList, out ClientCard fusionTarget, out List<ClientCard> selectedFusionMaterialList)
	{
		fusionTarget = null;
		selectedFusionMaterialList = new List<ClientCard>();
		foreach (Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>> tuple in new List<Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>>
		{
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(44146295, new List<Func<ClientCard, bool>>
			{
				(ClientCard c) => c.IsCode(68468459),
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasType((CardType)75505728)
			}, (List<ClientCard> l) => !base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true), BrandedInRedUsing2SubFunc),
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(70534340, new List<Func<ClientCard, bool>>
			{
				(ClientCard c) => c.IsCode(68468459),
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark)
			}, (List<ClientCard> l) => !base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true) && base.Bot.Hand.Count((ClientCard c) => !l.Contains(c)) > 0 && !activatedCardIdList.Contains(70534340), BrandedInRedUsing2SubFunc),
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(11321089, null, null, BrandedInRedForChimeraFunc),
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(72272462, new List<Func<ClientCard, bool>>
			{
				(ClientCard c) => c.HasSetcode(356),
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute((CardAttribute)48)
			}, null, BrandedInRedUsing2SubFunc),
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(92892239, new List<Func<ClientCard, bool>>
			{
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon),
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Dark) && c.HasRace(CardRace.Dragon)
			}, null, BrandedInRedUsing2SubFunc),
			new Tuple<int, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, Func<List<ClientCard>, List<ClientCard>, List<Func<ClientCard, bool>>, Func<List<ClientCard>, bool>, List<ClientCard>>>(38811586, new List<Func<ClientCard, bool>>
			{
				(ClientCard c) => c.IsCode(68468459),
				(ClientCard c) => !c.IsCode(cannotBeFusionMaterialIdList) && c.HasAttribute(CardAttribute.Light) && c.HasRace(CardRace.SpellCaster)
			}, (List<ClientCard> l) => base.Enemy.Graveyard.Count((ClientCard c) => c != null && c.IsMonster() && c.IsCanRevive()) + base.Bot.Graveyard.Count((ClientCard c) => c != null && !l.Contains(c)) >= 2, BrandedInRedUsing2SubFunc)
		})
		{
			if (mustSummonId > 0 && mustSummonId != tuple.Item1)
			{
				continue;
			}
			ClientCard clientCard = canSummonList.FirstOrDefault((ClientCard c) => c?.IsCode(tuple.Item1) ?? false);
			if (clientCard != null)
			{
				List<ClientCard> list = tuple.Item4(materialList, mustMaterialList, tuple.Item2, tuple.Item3);
				if (list.Count > 0)
				{
					fusionTarget = clientCard;
					selectedFusionMaterialList = list;
					break;
				}
			}
		}
	}

	public List<ClientCard> BrandedInRedUsing2SubFunc(List<ClientCard> materialList, List<ClientCard> mustMaterialList, List<Func<ClientCard, bool>> checkFuncList, Func<List<ClientCard>, bool> extraCheckFunc)
	{
		List<ClientCard> list = new List<ClientCard>();
		Func<ClientCard, bool> func = checkFuncList[0];
		Func<ClientCard, bool> func2 = checkFuncList[1];
		if (mustMaterialList != null && mustMaterialList.Count > 0)
		{
			foreach (ClientCard mustMaterial in mustMaterialList)
			{
				if (!func(mustMaterial) && !func2(mustMaterial))
				{
					continue;
				}
				foreach (ClientCard material in materialList)
				{
					if (material != mustMaterial && (((func(mustMaterial) && func2(material)) | (func2(mustMaterial) && func(material))) & (extraCheckFunc?.Invoke(new List<ClientCard> { mustMaterial, material }) ?? true)))
					{
						list.Add(mustMaterial);
						list.Add(material);
						return list;
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < materialList.Count - 1; i++)
			{
				ClientCard clientCard = materialList[i];
				if (!func(clientCard) && !func2(clientCard))
				{
					continue;
				}
				for (int j = i + 1; j < materialList.Count; j++)
				{
					ClientCard clientCard2 = materialList[j];
					if (((func(clientCard) && func2(clientCard2)) | (func2(clientCard) && func(clientCard2))) & (extraCheckFunc?.Invoke(new List<ClientCard> { clientCard, clientCard2 }) ?? true))
					{
						selectedFusionMaterial.Add(clientCard);
						selectedFusionMaterial.Add(clientCard2);
						return list;
					}
				}
			}
		}
		return selectedFusionMaterial;
	}

	public List<ClientCard> BrandedInRedForChimeraFunc(List<ClientCard> materialList, List<ClientCard> mustMaterialList, List<Func<ClientCard, bool>> checkFuncList, Func<List<ClientCard>, bool> extraCheckFunc)
	{
		List<ClientCard> result = new List<ClientCard>();
		int num = base.Enemy.MonsterZone.Count((ClientCard c) => c != null);
		num += base.Enemy.SpellZone.Count((ClientCard c) => c != null && c.Type != 2 && c.Type != 4);
		if (num == 0 || CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return result;
		}
		List<ClientCard> list = (from c in materialList
			where c.Location == CardLocation.MonsterZone
			orderby c.Attack
			select c).ToList();
		List<ClientCard> list2 = (from c in materialList
			where c.Location == CardLocation.Hand || c.Location == CardLocation.Grave
			orderby c.Attack
			select c).ToList();
		if (num >= 2)
		{
			foreach (ClientCard item in list2)
			{
				for (int num2 = 0; num2 < list.Count - 1; num2++)
				{
					ClientCard clientCard = list[num2];
					if (clientCard.IsCode(item.Id) || item.IsCode(clientCard.Id))
					{
						continue;
					}
					for (int num3 = num2 + 1; num3 < list.Count; num3++)
					{
						ClientCard clientCard2 = list[num3];
						if (!clientCard2.IsCode(clientCard.Id) && !clientCard.IsCode(clientCard2.Id) && !clientCard2.IsCode(item.Id) && !item.IsCode(clientCard2.Id))
						{
							return new List<ClientCard> { item, clientCard, clientCard2 };
						}
					}
				}
			}
		}
		foreach (ClientCard item2 in list)
		{
			for (int num4 = 0; num4 < list2.Count - 1; num4++)
			{
				ClientCard clientCard3 = list2[num4];
				if (clientCard3.IsCode(item2.Id) || item2.IsCode(clientCard3.Id))
				{
					continue;
				}
				for (int num5 = num4 + 1; num5 < list2.Count; num5++)
				{
					ClientCard clientCard4 = list2[num5];
					if (!clientCard4.IsCode(clientCard3.Id) && !clientCard3.IsCode(clientCard4.Id) && !clientCard4.IsCode(item2.Id) && !item2.IsCode(clientCard4.Id))
					{
						return new List<ClientCard> { item2, clientCard3, clientCard4 };
					}
				}
			}
		}
		return result;
	}

	public bool BrandedLostActivate()
	{
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFaceup())
		{
			return true;
		}
		return false;
	}

	public bool BrandedLostCardActivate()
	{
		if (base.Card.Location == CardLocation.SpellZone && base.Card.IsFaceup())
		{
			return false;
		}
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Spell))
		{
			return false;
		}
		if (!summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion() && base.Bot.Hand.Count < 3)
		{
			return false;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
		return true;
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

	public bool BrightestBlazingBrandedKingActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Trap))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			if (base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsFaceup() && (c.Location == CardLocation.MonsterZone || c.Location == CardLocation.SpellZone) && !c.IsCode(albazFusionMonster)))
			{
				return false;
			}
			if (base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 1 && c.IsFaceup() && !currentNegateCardList.Contains(c) && !currentDestroyCardList.Contains(c) && (c.Location == CardLocation.MonsterZone || c.Location == CardLocation.SpellZone)))
			{
				currentNegateCardList.AddRange(base.Enemy.MonsterZone.Where((ClientCard c) => c?.IsFaceup() ?? false));
				currentNegateCardList.AddRange(base.Enemy.SpellZone.Where((ClientCard c) => c?.IsFaceup() ?? false));
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool BrandedBeastActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Trap))
		{
			return false;
		}
		int num = -1;
		if (base.ActivateDescription >= base.Util.GetStringId(32756828, 0))
		{
			num = base.ActivateDescription - base.Util.GetStringId(32756828, 0);
		}
		Logger.DebugWriteLine("Beast: " + num);
		if (base.ActivateDescription == base.Util.GetStringId(base.Card.Id, 0))
		{
			ClientCard clientCard = null;
			ClientCard clientCard2 = null;
			List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap);
			if (problematicEnemyCardList.Count > 0)
			{
				clientCard = problematicEnemyCardList[0];
			}
			if (clientCard == null)
			{
				if (base.Duel.Player == 1)
				{
					if ((base.CurrentTiming & 4) > 0)
					{
						List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
						if (normalEnemyTargetList.Count > 0)
						{
							clientCard = normalEnemyTargetList[0];
						}
					}
				}
				else
				{
					clientCard = base.Util.GetOneEnemyBetterThanMyBest(onlyATK: true, canBeTarget: true);
				}
			}
			bool flag = DefaultOnBecomeTarget();
			int num2 = base.Bot.MonsterZone.Count((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(392));
			flag |= num2 > 0 && base.Duel.ChainTargets.Count((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.MonsterZone && c.IsFaceup() && c.HasSetcode(392)) == num2;
			flag |= base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 1 && c.Location == CardLocation.SpellZone && c.IsCode(12580477));
			if (clientCard == null && flag)
			{
				clientCard2 = base.Duel.ChainTargets.FirstOrDefault((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.MonsterZone && c.IsFaceup() && c.HasSetcode(392));
				if (clientCard2 == null)
				{
					if (!activatedCardIdList.Contains(87746185) && !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184)))
					{
						List<ClientCard> list = (from c in base.Bot.MonsterZone
							where c?.IsCode(87746184) ?? false
							orderby c.GetDefensePower()
							select c).ToList();
						if (list.Count > 0)
						{
							clientCard2 = list[0];
						}
					}
					if (clientCard2 == null)
					{
						clientCard2 = (from c in base.Bot.MonsterZone
							where c != null && c.HasRace(CardRace.Dragon) && (!c.IsOriginalCode(68468459) || !base.Util.ChainContainsCard(68468459))
							orderby c.GetDefensePower()
							select c).FirstOrDefault();
					}
				}
				List<ClientCard> normalEnemyTargetList2 = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Trap);
				if (normalEnemyTargetList2.Count > 0)
				{
					clientCard = normalEnemyTargetList2[0];
				}
			}
			if (clientCard != null)
			{
				if (!activatedCardIdList.Contains(87746185) && !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184)))
				{
					List<ClientCard> list2 = (from c in base.Bot.MonsterZone
						where c?.IsCode(87746184) ?? false
						orderby c.GetDefensePower()
						select c).ToList();
					if (list2.Count > 0)
					{
						clientCard2 = list2[0];
					}
				}
				if (clientCard2 == null)
				{
					clientCard2 = (from c in base.Bot.MonsterZone
						where c != null && c.HasRace(CardRace.Dragon) && (!c.IsOriginalCode(68468459) || !base.Util.ChainContainsCard(68468459))
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
				}
			}
			if (clientCard2 != null && clientCard != null)
			{
				activatedCardIdList.Add(base.Card.Id);
				base.AI.SelectCard(clientCard2);
				base.AI.SelectNextCard(clientCard);
				currentDestroyCardList.Add(clientCard);
				return true;
			}
		}
		if (base.Duel.Phase == DuelPhase.End && base.Bot.HasInGraveyard(18973184))
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			base.AI.SelectCard(18973184);
			return true;
		}
		return false;
	}

	public bool BrandedRetributionActivate()
	{
		if (base.Card.Location == CardLocation.SpellZone && base.Duel.LastChainPlayer == 1)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Trap))
			{
				return false;
			}
			if ((base.Bot.Graveyard.Where((ClientCard c) => c?.IsCode(albazFusionMonster) ?? false).Count() > 1) | base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(70534340, 1906812, 3410461)))
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Trap))
			{
				return false;
			}
			foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
			{
				{
					44362883,
					() => BrandedFusionActivateCheck()
				},
				{
					18973184,
					delegate
					{
						if (base.Duel.Player == 0 && base.Duel.Phase >= DuelPhase.End)
						{
							return false;
						}
						if (base.Bot.HasInHandOrInSpellZone(44362883) && BrandedFusionActivateCheck())
						{
							return true;
						}
						if (base.Bot.HasInHandOrInSpellZone(34995106) && BrandedInWhiteActivateCheck())
						{
							return true;
						}
						if (base.Bot.HasInHandOrInSpellZone(82738008) && BrandedInRedActivateCheck() != null)
						{
							return true;
						}
						if (!summoned && base.Bot.HasInHand(68468459) && CheckAlbazFusion())
						{
							return true;
						}
						return (base.Bot.HasInMonstersZone(95515789) || (!summoned && base.Bot.HasInHand(95515789))) ? true : false;
					}
				},
				{
					29948294,
					() => (base.Duel.Player != 1 || (!fusionToGYFlag && base.Duel.Phase == DuelPhase.End)) && BrandedInHighSpiritsActivateCheck()
				},
				{
					82738008,
					() => BrandedInRedActivateCheck() != null
				},
				{ 34995106, BrandedInWhiteActivateCheck },
				{
					19271881,
					() => (base.Duel.Player != 1 || (!fusionToGYFlag && base.Duel.Phase == DuelPhase.End)) && base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && c.IsCode(albazFusionMonster))
				},
				{
					36637374,
					() => base.Bot.Hand.Count > 2 && !activatedCardIdList.Contains(36637374)
				}
			})
			{
				ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
				if (clientCard != null && pair.Value())
				{
					activatedCardIdList.Add(base.Card.Id);
					base.AI.SelectCard(clientCard);
					return true;
				}
			}
		}
		return false;
	}

	public bool GuardianChimeraActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		return true;
	}

	public bool AlbionTheSanctifireDragonActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			List<ClientCard> list = base.Enemy.Graveyard.Where((ClientCard c) => c != null && c.IsMonster() && c.IsCanRevive()).ToList();
			list.AddRange(base.Bot.Graveyard.Where((ClientCard c) => c != null && c.IsMonster() && c.IsCanRevive()).ToList());
			List<ClientCard> list2 = new List<ClientCard>();
			if (CheckAlbazFusion(null, out var enemyMonsterList) && !spSummoningAlbaz)
			{
				bool flag = enemyMonsterList.Count > 1;
				if (enemyMonsterList.Count > 0)
				{
					ClientCard clientCard = enemyMonsterList[0];
					flag |= clientCard.HasType((CardType)75505856);
					flag |= clientCard.IsFloodgate() || clientCard.IsOneForXyz() || base.Util.GetWorstBotMonster().GetDefensePower() < clientCard.Attack;
					flag |= base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer == -1;
				}
				if (flag)
				{
					ClientCard albaz = list.FirstOrDefault((ClientCard c) => c.IsOriginalCode(68468459));
					ClientCard clientCard2 = (from c in list
						where c != albaz && !currentDestroyCardList.Contains(c)
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					if (albaz != null && clientCard2 != null && (GetProblematicEnemyMonster(0, canBeTarget: false, ignoreCurrentDestroy: false, CardType.Monster) != null || Math.Max(clientCard2.Attack, clientCard2.Defense) <= albaz.Defense))
					{
						Logger.DebugWriteLine("Sanctifire 1");
						list2.AddRange(new ClientCard[2] { albaz, clientCard2 });
						spSummoningAlbaz = true;
					}
				}
			}
			if (list2.Count == 0)
			{
				ClientCard floogateCard = (from c in list
					where c.IsFloodgate()
					orderby c.GetDefensePower() descending
					select c).FirstOrDefault();
				if (floogateCard != null)
				{
					ClientCard clientCard3 = (from c in list
						where c != floogateCard
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					if (clientCard3 != null)
					{
						Logger.DebugWriteLine("Sanctifire 2");
						list2.AddRange(new ClientCard[2] { floogateCard, clientCard3 });
					}
				}
			}
			if (list2.Count == 0 && base.Duel.LastChainPlayer == 1)
			{
				List<ClientCard> list3 = base.Duel.LastChainTargets.Intersect(list).ToList();
				if (list3.Count > 0)
				{
					ClientCard target = ShuffleList(list3)[0];
					ClientCard clientCard4 = null;
					clientCard4 = ((target.GetDefensePower() < 2000) ? (from c in list
						where c != target
						orderby c.GetDefensePower() descending
						select c).FirstOrDefault() : (from c in list
						where c != target
						orderby c.GetDefensePower()
						select c).FirstOrDefault());
					if (clientCard4 != null)
					{
						Logger.DebugWriteLine("Sanctifire 3");
						list2.AddRange(new ClientCard[2] { target, clientCard4 });
					}
				}
			}
			if (list2.Count == 0 && base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End)
			{
				foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
				{
					{
						92892239,
						() => base.Enemy.GetSpellCount() > 0
					},
					{
						44146295,
						() => !base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true)
					},
					{
						62962630,
						() => !activatedCardIdList.Contains(62962630)
					},
					{
						45484331,
						() => !activatedCardIdList.Contains(45484332)
					}
				})
				{
					ClientCard target2 = list.FirstOrDefault((ClientCard c) => c.IsCode(pair.Key));
					if (target2 != null && pair.Value())
					{
						ClientCard item = (from c in list
							where c != target2
							orderby c.GetDefensePower()
							select c).FirstOrDefault();
						list2.Add(target2);
						list2.Add(item);
						Logger.DebugWriteLine("Sanctifire 4");
						break;
					}
				}
			}
			if (list2.Count == 0 && base.Duel.Player == 1 && base.Enemy.Hand.Count > 0 && base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() == 0 && (base.CurrentTiming & 8) > 0)
			{
				ClientCard summonTarget = list.OrderByDescending((ClientCard c) => c.Attack).FirstOrDefault();
				foreach (KeyValuePair<int, Func<bool>> pair2 in new Dictionary<int, Func<bool>>
				{
					{
						44146295,
						() => !base.Bot.HasInMonstersZone(44146295, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !base.Bot.HasInSpellZone(44146295, notDisabled: false, faceUp: true)
					},
					{
						62962630,
						() => !activatedCardIdList.Contains(62962630)
					}
				})
				{
					ClientCard clientCard5 = list.FirstOrDefault((ClientCard c) => c.IsCode(pair2.Key));
					if (clientCard5 != null && pair2.Value())
					{
						summonTarget = clientCard5;
						break;
					}
				}
				if (summonTarget != null)
				{
					ClientCard item2 = (from c in list
						where c != summonTarget
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					list2.Add(summonTarget);
					list2.Add(item2);
					Logger.DebugWriteLine("Sanctifire 5");
				}
			}
			if (list2.Count > 0)
			{
				base.AI.SelectMaterials(list2, 509);
				currentDestroyCardList.AddRange(list2);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			List<ClientCard> source = new List<ClientCard>
			{
				base.Bot.MonsterZone[2],
				base.Bot.MonsterZone[5],
				base.Bot.MonsterZone[6]
			};
			List<ClientCard> source2 = new List<ClientCard>
			{
				base.Enemy.MonsterZone[2],
				base.Enemy.MonsterZone[5],
				base.Enemy.MonsterZone[6]
			};
			if (source2.Any((ClientCard c) => c != null && (c.IsFloodgate() || c.IsMonsterDangerous())))
			{
				return true;
			}
			return source.Select((ClientCard c) => c?.GetDefensePower() ?? 0).Sum() < source2.Select((ClientCard c) => c?.GetDefensePower() ?? 0).Sum();
		}
		return false;
	}

	public bool MirrorjadeTheIcebladeDragonActivate()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			bool flag = GetProblematicEnemyMonster(0, canBeTarget: false, ignoreCurrentDestroy: false, CardType.Monster) != null;
			if (base.Enemy.GetMonsterCount() > 0)
			{
				flag |= base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer != 0;
				int num = ((base.Enemy.BattlingMonster != null) ? base.Enemy.BattlingMonster.GetDefensePower() : 0);
				int num2 = ((base.Bot.BattlingMonster != null) ? base.Bot.BattlingMonster.GetDefensePower() : 0);
				flag |= num > 0 && num > num2 && base.Duel.LastChainPlayer != 0 && !currentDestroyCardList.Contains(base.Enemy.BattlingMonster);
				flag |= DefaultOnBecomeTarget() && base.Duel.LastChainPlayer != 0;
			}
			if (base.Duel.CurrentChain.Any((ClientCard c) => c.IsCode(27204311) && !DefaultCheckWhetherCardIdIsNegated(27204311)))
			{
				flag |= base.Enemy.GetMonsterCount() > 0;
				flag |= base.Bot.HasInMonstersZone(new int[3] { 32731036, 36577931, 19096726 });
			}
			if (flag)
			{
				foreach (KeyValuePair<int, Func<bool>> item in new Dictionary<int, Func<bool>>
				{
					{
						87746184,
						() => !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184)) && !DefaultCheckWhetherCardIdIsNegated(87746184) && (base.Duel.Player != 0 || !base.Bot.HasInMonstersZone(87746184) || !base.Bot.HasInGraveyard(32731036))
					},
					{
						51409648,
						() => base.Duel.Player == 1 && base.Bot.Graveyard.Any((ClientCard c) => c.IsOriginalCode(68468459)) && base.Bot.Hand.Count > 0 && !activatedCardIdList.Contains(68468459) && !DefaultCheckWhetherCardIdIsNegated(68468459)
					},
					{
						41373230,
						() => CheckRemainInDeck(45883110) > 0
					},
					{
						1906812,
						() => CheckRemainInDeck(45484331) > 0
					},
					{
						3410461,
						() => CheckRemainInDeck(6498706, 44362883) > 0
					},
					{
						70534340,
						() => true
					},
					{
						38811586,
						() => true
					}
				})
				{
					if (base.Bot.HasInExtra(item.Key) && item.Value())
					{
						base.AI.SelectCard(item.Key);
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool BorreloadFuriousDragonActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Monster);
		if (problematicEnemyCardList.Count > 0)
		{
			ClientCard clientCard = (from c in base.Bot.GetMonsters()
				orderby c.GetDefensePower() + (c.IsCode(albazFusionMonster) ? 1 : 0)
				select c).FirstOrDefault();
			if (clientCard != null)
			{
				base.AI.SelectCard(clientCard);
				base.AI.SelectNextCard(problematicEnemyCardList);
				currentDestroyCardList.Add(clientCard);
				currentDestroyCardList.Add(problematicEnemyCardList[0]);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true, CardType.Monster);
		if (base.Duel.LastChainPlayer == 1)
		{
			List<ClientCard> list = base.Duel.LastChainTargets.Where((ClientCard c) => c.Location == CardLocation.MonsterZone && c.Controller == 0).ToList();
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.IsCode(targetNegateIdList))
			{
				list = (from c in list
					where !c.IsCode(95515789) || c.Attack < 2500
					orderby c.Attack
					select c).ToList();
			}
			if (list.Count > 0)
			{
				base.AI.SelectCard(list);
				base.AI.SelectNextCard(normalEnemyTargetList);
				currentDestroyCardList.Add(list[0]);
				currentDestroyCardList.Add(normalEnemyTargetList[0]);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End)
		{
			List<ClientCard> list2 = (from c in base.Bot.MonsterZone
				where c != null && c.GetDefensePower() <= 2500
				orderby c.GetDefensePower()
				select c).ToList();
			if (list2.Count > 0)
			{
				base.AI.SelectCard(list2);
				base.AI.SelectNextCard(normalEnemyTargetList);
				currentDestroyCardList.Add(list2[0]);
				currentDestroyCardList.Add(normalEnemyTargetList[0]);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public bool LubellionTheSearingDragonActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone && LubellionTheSearingDragonFusionTarget(base.Bot.ExtraDeck, out var _) > 0)
		{
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public int LubellionTheSearingDragonFusionTarget(IList<ClientCard> cards, out ClientCard target)
	{
		target = null;
		bool hasAlbaz = base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsOriginalCode(68468459));
		hasAlbaz |= base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsOriginalCode(68468459));
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				44146295,
				() => hasAlbaz && !DefaultCheckWhetherCardIdIsNegated(44146295)
			},
			{
				24915933,
				() => base.Bot.HasInBanished(95515789)
			},
			{
				92892239,
				() => base.Bot.Banished.Where((ClientCard c) => c.IsFaceup() && c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList)).Count() + base.Bot.Graveyard.Where((ClientCard c) => c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList) && (base.Duel.Player == 1 || !CheckWhetherShouldKeepInGrave(c))).Count() >= 2
			},
			{
				51409648,
				() => hasAlbaz && base.Bot.HasInBanished(19096726)
			},
			{
				41373230,
				delegate
				{
					if (!hasAlbaz)
					{
						return false;
					}
					ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster(onlyFaceup: true);
					ClientCard bestBotMonster = base.Util.GetBestBotMonster(onlyATK: true);
					int num = bestEnemyMonster?.GetDefensePower() ?? 0;
					int num2 = bestBotMonster?.Attack ?? 0;
					if (num > 0 && num >= num2)
					{
						List<ClientCard> list = new List<ClientCard>(base.Bot.Banished);
						list.AddRange(base.Bot.Graveyard);
						foreach (ClientCard item in list)
						{
							if (item != null && item.IsFaceup() && item.Attack >= 2500 && 2900 + item.Level >= num)
							{
								return true;
							}
						}
					}
					return false;
				}
			},
			{
				72272462,
				() => base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.Attack > 2500 && (!c.HasType(CardType.Fusion) || c.Level < 8)) && (base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(356)) | base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(356)))
			},
			{
				3410461,
				() => hasAlbaz && !base.Bot.HasInExtra(87746184)
			},
			{
				87746184,
				() => !activatedCardIdList.Contains(87746184) && !DefaultCheckWhetherCardIdIsNegated(87746184) && (base.Bot.Banished.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasAttribute(CardAttribute.Light)) | base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasAttribute(CardAttribute.Light)))
			}
		})
		{
			target = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
			if (target != null && pair.Value())
			{
				return pair.Key;
			}
		}
		target = null;
		return 0;
	}

	public bool AlbaLenatusTheAbyssDragonSpSummon()
	{
		List<ClientCard> list = (from c in base.Enemy.GetMonsters()
			where c != null && c.IsFaceup() && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasRace(CardRace.Dragon)
			select c).ToList();
		if (list.Count > 0)
		{
			bool num = list.Count > 1;
			int bestBotPower = base.Util.GetBestAttack(base.Bot);
			if (num | list.Any((ClientCard c) => c.GetDefensePower() >= bestBotPower))
			{
				if (!list.Any((ClientCard c) => c.IsCode(68468459)))
				{
					ClientCard clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c?.IsCode(68468459) ?? false);
					if (clientCard != null)
					{
						list.Add(clientCard);
					}
				}
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		return false;
	}

	public bool AlbaLenatusTheAbyssDragonActivate()
	{
		if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool GranguignolTheDuskDragonActivate()
	{
		int num = -1;
		if (base.ActivateDescription >= base.Util.GetStringId(24915933, 0))
		{
			num = base.ActivateDescription - base.Util.GetStringId(24915933, 0);
		}
		Logger.DebugWriteLine("granguignol: " + num);
		if (base.ActivateDescription == -1 || base.ActivateDescription == base.Util.GetStringId(24915933, 0))
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || CheckWhetherWillbeRemoved())
			{
				return false;
			}
			if (GranguignolTheDuskDragonSendToGYTarget(null, out var _) > 0)
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.ActivateDescription == base.Util.GetStringId(24915933, 1))
		{
			if (CheckWhetherNegated(disablecheck: true, base.Card.Location == CardLocation.MonsterZone, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public int GranguignolTheDuskDragonSendToGYTarget(IList<ClientCard> cards, out ClientCard target)
	{
		bool needSendBranded = base.Bot.HasInGraveyard(17751597) && CheckRemainInDeck(44362883) > 0;
		if (CheckRemainInDeck(17751597) > 0)
		{
			needSendBranded |= base.Bot.Graveyard.Any((ClientCard c) => c != null && c.HasType((CardType)6) && c.HasSetcode(349) && (!fusionToGYFlag || !c.IsCode(19271881, 29948294)));
			needSendBranded |= !activatedCardIdList.Contains(25451383) && !CheckWhetherWillbeRemoved() && base.Bot.HasInHandOrInGraveyard(25451383);
			needSendBranded |= base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.Location == CardLocation.Grave && c.IsCode(60242223));
		}
		foreach (KeyValuePair<int, Func<bool>> pair in new List<KeyValuePair<int, Func<bool>>>
		{
			new KeyValuePair<int, Func<bool>>(60242223, () => !activatedCardIdList.Contains(60242223) && needSendBranded),
			new KeyValuePair<int, Func<bool>>(25451383, () => !activatedCardIdList.Contains(25451383) && needSendBranded),
			new KeyValuePair<int, Func<bool>>(87746184, () => !activatedCardIdList.Contains(87746185) && !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(87746184))),
			new KeyValuePair<int, Func<bool>>(41373230, () => !activatedCardIdList.Contains(41373230) && !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(41373230)) && CheckRemainInDeck(45883110, 68468459) > 0),
			new KeyValuePair<int, Func<bool>>(32731036, () => base.Bot.HasInMonstersZone(new int[2] { 87746184, 41373230 }) && CheckRemainInDeck(18973184, 32756828) > 0),
			new KeyValuePair<int, Func<bool>>(1906812, () => !activatedCardIdList.Contains(1906812) && !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(1906812)) && CheckRemainInDeck(45484331) > 0),
			new KeyValuePair<int, Func<bool>>(53971455, () => CheckRemainInDeck(95515789, 45883110) > 0),
			new KeyValuePair<int, Func<bool>>(25451383, () => true)
		})
		{
			if (cards == null)
			{
				if ((CheckRemainInDeck(pair.Key) > 0 || base.Bot.HasInExtra(pair.Key)) && pair.Value())
				{
					target = null;
					return pair.Key;
				}
				continue;
			}
			ClientCard clientCard = cards.FirstOrDefault((ClientCard c) => c.IsOriginalCode(pair.Key));
			if (clientCard != null && pair.Value())
			{
				target = clientCard;
				return pair.Key;
			}
		}
		target = null;
		return 0;
	}

	public bool DespianQuaeritisActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone && base.Duel.Phase == DuelPhase.Main1 && !CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			if ((base.CurrentTiming & 4) != 0 && base.Duel.Player == 1)
			{
				int bestBotPower = base.Util.GetBestPower(base.Bot);
				if (base.Enemy.GetMonsters().Any((ClientCard c) => c.GetDefensePower() >= bestBotPower && c.IsAttack() && (!c.HasType(CardType.Fusion) || c.Level < 8)))
				{
					activatedCardIdList.Add(base.Card.Id);
					return true;
				}
			}
			if (base.Duel.Player == 0 && base.Enemy.GetMonsters().Any((ClientCard c) => c.IsAttack() && (!c.HasType(CardType.Fusion) || c.Level < 8)))
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return true;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool SprindTheIrondashDragonActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			if (SprindTheIrondashDragonMoveZone(0, base.Card) > 0)
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		return false;
	}

	public int SprindTheIrondashDragonMoveZone(int available = 0, ClientCard selfCard = null)
	{
		int result = -1;
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.MonsterZone[i] != null)
			{
				continue;
			}
			int num2 = (int)Math.Pow(2.0, i);
			if (available <= 0 || (available & num2) != 0)
			{
				int num3 = SprindTheIrondashDragonDestroyValue(i, selfCard);
				if (num3 > num)
				{
					result = num2;
					num = num3;
				}
			}
		}
		return result;
	}

	public int SprindTheIrondashDragonDestroyValue(int zoneId, ClientCard selfCard = null)
	{
		int num = 0;
		if (zoneId == 1 || zoneId == 3)
		{
			ClientCard clientCard = base.Bot.MonsterZone[(zoneId + 9) / 2];
			if (clientCard != null && clientCard != selfCard && clientCard.IsFaceup())
			{
				num -= 5;
			}
			ClientCard clientCard2 = base.Enemy.MonsterZone[(11 - zoneId) / 2];
			if (clientCard2 != null && clientCard2.IsFaceup())
			{
				num += 2;
			}
		}
		ClientCard clientCard3 = base.Bot.SpellZone[zoneId];
		if (clientCard3 != null && clientCard3.IsFaceup())
		{
			num--;
		}
		ClientCard clientCard4 = base.Enemy.MonsterZone[5 - zoneId];
		if (clientCard4 != null && clientCard4.IsFaceup())
		{
			num++;
			if (clientCard4.IsFloodgate() || clientCard4.IsMonsterDangerous())
			{
				num += 5;
			}
		}
		ClientCard clientCard5 = base.Enemy.SpellZone[5 - zoneId];
		if (clientCard5 != null && clientCard5.IsFaceup())
		{
			num++;
			if (clientCard5.IsFloodgate())
			{
				num += 5;
			}
		}
		return num;
	}

	public bool TitanikladTheAshDragonActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool RindbrummTheStrikingDragonActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			bool flag = false;
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null)
			{
				flag = base.Duel.LastChainPlayer == 1;
				flag |= base.Duel.LastChainPlayer == 0 && lastChainCard.IsCode(44146295) && lastChainCard.Location == CardLocation.MonsterZone && base.Enemy.GetMonsterCount() == 0;
			}
			if (flag)
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster) || base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 0 && c.IsCode(45883110)))
			{
				return false;
			}
			ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.IsOriginalCode(68468459));
			bool flag2 = base.Card.IsCanRevive() && base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasType((CardType)75505728));
			if (CheckAlbazFusion(null, out var enemyMonsterList) && clientCard != null && !spSummoningAlbaz)
			{
				flag2 |= enemyMonsterList.Count > 1;
				if (enemyMonsterList.Count > 0)
				{
					ClientCard clientCard2 = enemyMonsterList[0];
					flag2 |= clientCard2.HasType((CardType)75505856);
					flag2 |= clientCard2.IsFloodgate() || clientCard2.IsOneForXyz() || base.Util.GetWorstBotMonster()?.GetDefensePower() < clientCard2.Attack;
					flag2 |= base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer == -1;
				}
			}
			if (flag2)
			{
				clientCard = (from c in base.Bot.Graveyard
					where c.IsCode(68468459)
					orderby c.GetDefensePower()
					select c).FirstOrDefault();
				if (flag2 && clientCard != null)
				{
					spSummoningAlbaz = true;
					activatedCardIdList.Add(base.Card.Id + 1);
					base.AI.SelectCard(clientCard);
					return true;
				}
			}
		}
		return false;
	}

	public bool AlbionTheBrandedDragonActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			if (AlbionTheBrandedDragonFusionTarget(base.Bot.ExtraDeck, out var _) > 0)
			{
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public int AlbionTheBrandedDragonFusionTarget(IList<ClientCard> cards, out ClientCard target)
	{
		target = null;
		foreach (KeyValuePair<int, Func<bool>> pair in new Dictionary<int, Func<bool>>
		{
			{
				44146295,
				() => (!CheckWhetherNegated() && CheckShouldNoMoreSpSummon()) | base.Bot.Graveyard.Any((ClientCard c) => c != null && !sendToGYThisTurn.Contains(c) && !c.IsCode(cannotBeFusionMaterialIdList) && c.HasType((CardType)75505728))
			},
			{
				70534340,
				delegate
				{
					if (!activatedCardIdList.Contains(70534340))
					{
						List<ClientCard> list = new List<ClientCard>(base.Bot.GetMonsters());
						list.AddRange(base.Bot.Graveyard);
						bool flag = false;
						bool flag2 = list.Any((ClientCard c) => c.IsOriginalCode(68468459));
						foreach (ClientCard item in list)
						{
							if (!flag && item.IsCode(68468459) && (!flag2 || !item.IsOriginalCode(25451383)))
							{
								flag = true;
							}
							else if (item.HasAttribute(CardAttribute.Dark))
							{
								return true;
							}
						}
						if (base.Bot.HasInHand(19096726) && base.Bot.Hand.Count >= 2)
						{
							return true;
						}
					}
					return false;
				}
			},
			{
				92892239,
				delegate
				{
					if (base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() > 0)
					{
						int num = base.Bot.Graveyard.Where((ClientCard c) => c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList) && (base.Duel.Player == 1 || !CheckWhetherShouldKeepInGrave(c))).Count();
						if (base.Duel.Player == 1 && base.Bot.GetMonsters().Any((ClientCard c) => c.HasRace(CardRace.Dragon) && c.HasAttribute(CardAttribute.Dark) && !c.IsCode(cannotBeFusionMaterialIdList)))
						{
							num++;
						}
						return num >= 2;
					}
					return false;
				}
			},
			{
				38811586,
				delegate
				{
					ClientCard albaz = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.IsCode(68468459));
					ClientCard lightSpellcaster = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.HasAttribute(CardAttribute.Light));
					return base.Enemy.GetGraveyardMonsters().Count + base.Bot.Graveyard.Where((ClientCard c) => c.IsMonster() && c != albaz && c != lightSpellcaster).Count() + (base.Bot.HasInHand(23434538) ? 1 : 0) >= 2;
				}
			},
			{
				51409648,
				() => base.Bot.HasInGraveyard(19096726)
			},
			{
				72272462,
				delegate
				{
					if (base.Enemy.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.Attack >= 2500 && (!c.HasType(CardType.Fusion) || c.Level < 8)))
					{
						ClientCard despian = (from c in base.Bot.Graveyard
							where c != null && c.HasSetcode(356) && !CheckWhetherShouldKeepInGrave(c)
							orderby c.GetDefensePower()
							select c).FirstOrDefault();
						if (despian == null)
						{
							despian = (from c in base.Bot.MonsterZone
								where c?.HasSetcode(356) ?? false
								orderby c.GetDefensePower()
								select c).FirstOrDefault();
						}
						if (despian != null)
						{
							return base.Bot.Graveyard.Any((ClientCard c) => c.HasAttribute((CardAttribute)48) && !CheckWhetherShouldKeepInGrave(c) && c != despian);
						}
					}
					return false;
				}
			},
			{
				41373230,
				delegate
				{
					ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsOriginalCode(68468459) ?? false);
					if (clientCard == null)
					{
						clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsCode(68468459) ?? false);
					}
					foreach (ClientCard item2 in base.Bot.Graveyard)
					{
						if (item2 != null && item2 != clientCard && item2.IsMonster() && item2.Attack >= 2500 && !item2.IsCode(cannotBeFusionMaterialIdList))
						{
							return (!base.Util.IsTurn1OrMain2() && base.Enemy.GetMonsterCount() == 0) | !CheckWhetherShouldKeepInGrave(item2);
						}
					}
					return false;
				}
			},
			{
				3410461,
				delegate
				{
					if (base.Util.GetOneEnemyBetterThanMyBest() == null)
					{
						ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsOriginalCode(68468459) ?? false);
						if (clientCard == null)
						{
							clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard c) => c?.IsCode(68468459) ?? false);
						}
						foreach (ClientCard item3 in base.Bot.Graveyard)
						{
							if (item3 != null && item3 != clientCard && item3.IsMonster() && item3.HasRace(CardRace.Dragon) && !item3.IsCode(cannotBeFusionMaterialIdList))
							{
								return true;
							}
						}
					}
					return false;
				}
			},
			{
				24915933,
				() => cards != null
			}
		})
		{
			target = cards.FirstOrDefault((ClientCard card) => card.IsCode(pair.Key));
			if (target != null && pair.Value())
			{
				return pair.Key;
			}
		}
		target = null;
		return 0;
	}

	public bool DespianLuluwalilithActivate()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (CheckWhetherNegated(disablecheck: true, toFieldCheck: false, CardType.Monster))
			{
				return false;
			}
			if (CheckRemainInDeck(45883110, 95515789) > 0)
			{
				activatedCardIdList.Add(base.Card.Id + 1);
				return true;
			}
		}
		return false;
	}

	public bool SetForChimera()
	{
		if (base.Card.Level <= 4)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() > 0 || !base.Bot.HasInHandOrInSpellZone(34995106) || !base.Bot.HasInExtra(11321089))
		{
			return false;
		}
		if (DefaultCheckWhetherCardIdIsNegated(11321089) || CheckWhetherNegated(disablecheck: true, toFieldCheck: true, CardType.Monster))
		{
			return false;
		}
		if (base.Enemy.MonsterZone.All((ClientCard c) => c == null) && base.Enemy.SpellZone.All((ClientCard c) => c == null))
		{
			return false;
		}
		for (int num = 0; num < base.Bot.Hand.Count - 1; num++)
		{
			ClientCard clientCard = base.Bot.Hand[num];
			if (!clientCard.IsMonster() || clientCard.IsCode(base.Card.Id) || base.Card.IsCode(clientCard.Id))
			{
				continue;
			}
			for (int num2 = num + 1; num2 < base.Bot.Hand.Count; num2++)
			{
				ClientCard clientCard2 = base.Bot.Hand[num2];
				if (clientCard2.IsMonster() && !clientCard2.IsCode(base.Card.Id) && !base.Card.IsCode(clientCard2.Id) && !clientCard2.IsCode(clientCard.Id) && !clientCard.IsCode(clientCard2.Id) && (base.Card.HasRace(CardRace.Dragon) || clientCard.HasRace(CardRace.Dragon) || clientCard2.HasRace(CardRace.Dragon)))
				{
					summoned = true;
					return true;
				}
			}
		}
		return false;
	}

	public bool AdvanceSummon()
	{
		if (base.Card.Level < 5)
		{
			return false;
		}
		List<ClientCard> releaseGoal = base.Bot.MonsterZone.Where((ClientCard c) => c != null && c.IsFaceup() && !c.IsDisabled() && c.IsCode(10158145)).ToList();
		if (releaseGoal.Count > 0)
		{
			if (base.Card.Level <= 6)
			{
				base.AI.SelectMaterials(releaseGoal);
				summoned = true;
				return true;
			}
			if (base.Card.Level >= 7)
			{
				if (releaseGoal.Count < 2)
				{
					ClientCard clientCard = (from c in base.Bot.MonsterZone
						where c != null && !releaseGoal.Contains(c)
						orderby c.GetDefensePower()
						select c).FirstOrDefault();
					if (clientCard.GetDefensePower() > base.Card.Attack)
					{
						return false;
					}
					releaseGoal.Add(clientCard);
				}
				if (releaseGoal.Count >= 2)
				{
					base.AI.SelectMaterials(releaseGoal);
					summoned = true;
					return true;
				}
			}
		}
		return false;
	}

	public bool SpellSetCheck()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		switch (base.Card.Id)
		{
		case 29948294:
			if (!base.Bot.HasInMonstersZone(45883110) && (CheckRemainInDeck(45883110) <= 0 || !sendToGYThisTurn.Any((ClientCard c) => c.IsCode(41373230))))
			{
				return false;
			}
			break;
		case 36637374:
			if (CheckRemainInDeck(62962630, 45883110) <= 0)
			{
				return false;
			}
			break;
		case 19271881:
			if (!base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(albazFusionMonster)))
			{
				return false;
			}
			break;
		case 32756828:
			if (!((base.Bot.HasInGraveyard(18973184) || base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(392))) | (base.Bot.HasInHand(60242223) && (base.Enemy.Graveyard.Any((ClientCard c) => CheckBystialCanBanish(c)) || base.Bot.Graveyard.Any((ClientCard c) => CheckBystialCanBanish(c))))))
			{
				return false;
			}
			break;
		case 17751597:
			if (!((base.Bot.Graveyard.Where((ClientCard c) => c.IsCode(albazFusionMonster)).Count() > 1) | base.Bot.MonsterZone.Any((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(albazFusionMonster))))
			{
				return false;
			}
			break;
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

	protected override bool DefaultSetForDiabellze()
	{
		if (base.DefaultSetForDiabellze())
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool FloogateActivate()
	{
		if (base.Card.Owner != 1 && !base.Card.IsFloodgate())
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Executors.Any((CardExecutor e) => e != null && e.Type == ExecutorType.Activate && e.CardId == base.Card.Id))
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.IsFaceup() && CheckCanBeTargeted(lastChainCard, canBeTarget: true, CardType.Monster) && (lastChainCard.Location == CardLocation.MonsterZone || lastChainCard.Location == CardLocation.SpellZone))
			{
				base.AI.SelectCard(lastChainCard);
			}
		}
		return base.Duel.LastChainPlayer == 1;
	}
}
