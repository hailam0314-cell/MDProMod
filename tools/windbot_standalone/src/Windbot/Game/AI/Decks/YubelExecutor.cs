using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Yubel", "AI_Yubel", "Normal")]
public class YubelExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int YUBEL = 78371393;

		public const int YUBEL_TERROR_INCARNATE = 4779091;

		public const int SPIRIT_OF_YUBEL = 90829280;

		public const int PHANTOM_OF_YUBEL = 80453041;

		public const int SAMSARA_D_LOTUS = 62318994;

		public const int GRUESOME_GRAVE_SQUIRMER = 24215921;

		public const int FABLED_LURRIE = 97651498;

		public const int SHARVARA = 41165831;

		public const int FIENDSMITH_ENGRAVER = 60764609;

		public const int LACRIMA_CT = 28803166;

		public const int DARK_BECKONING_BEAST = 81034083;

		public const int CHAOS_SUMMONING_BEAST = 27439792;

		public const int NIGHTMARE_PAIN = 65261141;

		public const int NIGHTMARE_THRONE = 93729896;

		public const int SPIRIT_GATES = 80312545;

		public const int FIENDSMITH_TRACT = 98567237;

		public const int ABOMINABLE_CHAMBER = 80801743;

		public const int FIENDSMITHS_PARADISE = 99989863;

		public const int TERRAFORMING = 73628505;

		public const int FIENDSMITHS_DESIRAE = 82135803;

		public const int VARUDASN_FINAL_BRINGER = 70636044;

		public const int DDD_WAVE_HIGH_KING_CAESAR = 79559912;

		public const int UNCHAINED_LORD_OF_YAMA = 24269961;

		public const int UNCHAINED_SOUL_OF_RAGE = 67680512;

		public const int SP_LITTLE_KNIGHT = 29301450;

		public const int MOON_OF_THE_CLOSED_HEAVEN = 71818935;

		public const int FIENDSMITHS_REQUIEM = 2463794;

		public const int SALAMANGREAT_ALMIRAJ = 60303245;

		public const int NECROQUIP = 93860227;

		public const int GORGONOFZIL = 12067160;

		public const int GUSTAVMAX = 56910167;

		public const int JUGGERNAUT = 26096328;

		public const int UNCHAINDEDABOMINATION = 29479265;

		public const int Fuwalos = 42141493;

		public const int NaturalExterio = 99916754;

		public const int NaturalBeast = 33198837;

		public const int ImperialOrder = 61740673;

		public const int SwordsmanLV7 = 37267041;

		public const int RoyalDecree = 51452091;

		public const int Number41BagooskatheTerriblyTiredTapir = 90590303;

		public const int InspectorBoarder = 15397015;

		public const int SkillDrain = 82732705;

		public const int DivineArsenalAAZEUS_SkyThunder = 90448279;

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

	private enum ThroneStage
	{
		None,
		Searching,
		AwaitDestroyPrompt
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeHorus = 413;

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 90829280, 62318994, 93729896, 80312545, 14558127, 81034083, 10045474 }
		},
		{
			2,
			new List<int> { 23434538, 24224830, 24215921 }
		},
		{
			1,
			new List<int>
			{
				60764609, 99989863, 78371393, 4779091, 80801743, 65681983, 24215921, 97651498, 65261141, 73628505,
				98567237, 41165831, 27439792, 28803166
			}
		}
	};

	private List<int> notToNegateIdList = new List<int> { 58699500, 20343502, 19403423 };

	private List<int> notToDestroySpellTrap = new List<int> { 50005218, 6767771 };

	private List<int> targetNegateIdList = new List<int>
	{
		97268402, 10045474, 52038441, 78474168, 74003290, 67037924, 9753964, 66192538, 23204029, 73445448,
		35103106, 30286474, 45002991, 5795980, 38511382, 53742162, 30430448
	};

	private static readonly int[] LinkFodderPriority = new int[14]
	{
		41165831, 4779091, 67680512, 24269961, 24215921, 97651498, 28803166, 60303245, 81034083, 27439792,
		62318994, 90829280, 78371393, 60764609
	};

	private static readonly HashSet<int> YUBEL_SET = new HashSet<int> { 78371393, 4779091, 90829280, 80453041 };

	private int _totalAttack;

	private int _totalBotAttack;

	private bool enemyActivateMaxxC;

	private bool enemyActivateLockBird;

	private int dimensionShifterCount;

	private bool enemyActivateInfiniteImpermanenceFromHand;

	private List<int> infiniteImpermanenceList = new List<int>();

	private List<ClientCard> currentNegateCardList = new List<ClientCard>();

	private List<ClientCard> currentDestroyCardList = new List<ClientCard>();

	private List<ClientCard> sendToGYThisTurn = new List<ClientCard>();

	private List<int> activatedCardIdList = new List<int>();

	private List<ClientCard> enemyPlaceThisTurn = new List<ClientCard>();

	private List<ClientCard> escapeTargetList = new List<ClientCard>();

	private List<ClientCard> summonThisTurn = new List<ClientCard>();

	private bool _yubelWantsTribute;

	private ThroneStage _throneStage;

	private int _gateReviveTargetId;

	private int _gateDiscardPreferredId;

	private bool _gateWantsRecycle;

	private bool _spQuickMode;

	private bool moonSummoned;

	private bool requiemSummoned;

	private bool thronePending;

	private bool throneSearched;

	private int throneDesiredPick;

	private static readonly HashSet<int> NEVER_SAC = new HashSet<int> { 80453041, 79559912, 70636044, 29301450, 12067160, 29479265 };

	private static readonly int[] YubelCostPriority = new int[6] { 62318994, 97651498, 81034083, 27439792, 24215921, 60303245 };

	private bool InThroneFlow
	{
		get
		{
			if (!thronePending)
			{
				return _throneStage != ThroneStage.None;
			}
			return true;
		}
	}

	public YubelExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 93729896, ActNightmareThroneSearch);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 62318994, ActSamsaraDLotusGY);
		AddExecutor(ExecutorType.Activate, 78371393);
		AddExecutor(ExecutorType.Activate, 29479265, UnchainedAbominationActivate);
		AddExecutor(ExecutorType.Activate, 80453041, DontSelfNG);
		AddExecutor(ExecutorType.Activate, 80801743, ActAbo);
		AddExecutor(ExecutorType.Activate, 29301450, ActLittleKnight);
		AddExecutor(ExecutorType.Activate, 79559912, DontSelfNG);
		AddExecutor(ExecutorType.Activate, 82135803, ActDesirae);
		AddExecutor(ExecutorType.Activate, 70636044, ActVarudras);
		AddExecutor(ExecutorType.Activate, 67680512, ActRageQuickLink);
		AddExecutor(ExecutorType.Activate, 99989863, ActParadise);
		AddExecutor(ExecutorType.Activate, 60764609, ActEngraverHand);
		AddExecutor(ExecutorType.Activate, 98567237, ActTract);
		AddExecutor(ExecutorType.SpSummon, 97651498);
		AddExecutor(ExecutorType.SpSummon, 2463794, SSRequiem);
		AddExecutor(ExecutorType.Activate, 2463794, ActRequiemMZ);
		AddExecutor(ExecutorType.Activate, 28803166, ActLacimaCT);
		AddExecutor(ExecutorType.Activate, 2463794, ActRequiemEQ);
		AddExecutor(ExecutorType.SpSummon, 93860227, SSNecroquip);
		AddExecutor(ExecutorType.Activate, 60764609, ActEngraverGY);
		AddExecutor(ExecutorType.SpSummon, 79559912);
		AddExecutor(ExecutorType.Activate, 28803166, ActLacimaCTGY);
		AddExecutor(ExecutorType.Activate, S1_ActivateTerraformingForThrone);
		AddExecutor(ExecutorType.Activate, 93729896, S6_ChainThroneFollowUp);
		AddExecutor(ExecutorType.Summon, 81034083, NSDarkBeckoningBeast);
		AddExecutor(ExecutorType.Activate, 81034083, ActDarkBeckoningBeast);
		AddExecutor(ExecutorType.Activate, 80312545, S4_ActivateSpiritGates);
		AddExecutor(ExecutorType.Activate, 80312545, Gate_RecycleContinuous);
		AddExecutor(ExecutorType.Summon, 62318994, NSSamsaraDLotus);
		AddExecutor(ExecutorType.Activate, 62318994, ActSamsaraDLotus);
		AddExecutor(ExecutorType.SpSummon, 90829280);
		AddExecutor(ExecutorType.Activate, 90829280);
		AddExecutor(ExecutorType.Activate, 65261141, ActNightmarePainHand);
		AddExecutor(ExecutorType.Activate, 65261141, ActNightmarePainEffect);
		AddExecutor(ExecutorType.Activate, 24215921, SSGGS);
		AddExecutor(ExecutorType.Activate, 24215921, ActGGSGY);
		AddExecutor(ExecutorType.SpSummon, 71818935, SSMoon);
		AddExecutor(ExecutorType.SpSummon, 24269961, L2YamaSetup);
		AddExecutor(ExecutorType.Activate, 24269961, ActYamaMZ);
		AddExecutor(ExecutorType.Activate, 24269961, ActYamaGY);
		AddExecutor(ExecutorType.SpSummon, 67680512, L2RageKeepYama);
		AddExecutor(ExecutorType.SpSummon, 29479265, L4ABOSS);
		AddExecutor(ExecutorType.Activate, 41165831, ActSharvara);
		AddExecutor(ExecutorType.Activate, 41165831, ActSharvaraGY);
		AddExecutor(ExecutorType.SpSummon, 70636044);
		AddExecutor(ExecutorType.SpSummon, 60303245, AlmirajSummon);
		AddExecutor(ExecutorType.Activate, 80312545, Gate_Revive00Fiend);
		AddExecutor(ExecutorType.SpSummon, 80453041, SSPhantom);
		AddExecutor(ExecutorType.SpSummon, 56910167);
		AddExecutor(ExecutorType.Activate, 56910167);
		AddExecutor(ExecutorType.SpSummon, 26096328);
		AddExecutor(ExecutorType.Activate, 26096328);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
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

	public override bool OnSelectHand()
	{
		return true;
	}

	public List<ClientCard> ShuffleCardList(List<ClientCard> list)
	{
		int count = list.Count;
		while (count-- > 1)
		{
			int index = Program.Rand.Next(count + 1);
			ClientCard value = list[index];
			list[index] = list[count];
			list[count] = value;
		}
		return list;
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

	private bool MonsterRepos()
	{
		if (base.Card.Id == 90829280 || base.Card.Id == 78371393 || base.Card.Id == 4779091 || base.Card.Id == 80453041)
		{
			if (base.Card.IsDefense())
			{
				base.AI.SelectPosition(CardPosition.Attack);
				return true;
			}
			return false;
		}
		if (base.Card.IsFacedown())
		{
			return true;
		}
		if (CheckInDanger() && _totalAttack > _totalBotAttack)
		{
			return base.Card.IsDefense();
		}
		return DefaultMonsterRepos();
	}

	public bool CheckAtAdvantage()
	{
		if (GetProblematicEnemyMonster() == null && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup()))
		{
			return true;
		}
		return false;
	}

	public bool CheckInDanger()
	{
		if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
		{
			int num = 0;
			foreach (ClientCard monster in base.Enemy.GetMonsters())
			{
				if (monster.IsAttack() && !monster.Attacked)
				{
					num += monster.Attack;
				}
			}
			if (num >= base.Bot.LifePoints)
			{
				return true;
			}
		}
		return false;
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone && cardId == 67680512)
		{
			int num = GetMyLinkedMMZMask() & available & 0x1F;
			int num2 = ((num != 0) ? LowestBit(num) : LowestBit(available & 0x1F));
			base.AI.SelectPlace(num2);
			return num2;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		if (positions == null || positions.Count == 0)
		{
			return base.OnSelectPosition(cardId, positions);
		}
		bool flag = YUBEL_SET.Contains(cardId) || (base.Card != null && YUBEL_SET.Contains(base.Card.Id)) || (base.Card != null && (base.Card.Name?.Contains("Yubel") ?? false));
		if (!flag)
		{
			return base.OnSelectPosition(cardId, positions);
		}
		CardPosition cardPosition = (positions.Contains(CardPosition.FaceUpAttack) ? CardPosition.FaceUpAttack : (positions.Contains(CardPosition.Attack) ? CardPosition.Attack : ((CardPosition)0)));
		if (flag && cardPosition != 0)
		{
			base.AI.SelectPosition(cardPosition);
			return cardPosition;
		}
		CardPosition cardPosition2 = positions[0];
		base.AI.SelectPosition(cardPosition2);
		return cardPosition2;
	}

	public bool AshBlossomActivate()
	{
		if (BlockIfThrone("AshBlossom"))
		{
			return false;
		}
		if (InThroneFlow)
		{
			return false;
		}
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard().IsCode(23434538) && CheckAtAdvantage())
		{
			return false;
		}
		return DefaultAshBlossomAndJoyousSpring();
	}

	public bool MaxxCActivate()
	{
		if (BlockIfThrone("MaxxC"))
		{
			return false;
		}
		if (InThroneFlow)
		{
			return false;
		}
		if (CheckWhetherNegated() || base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		return DefaultMaxxC();
	}

	public bool InfiniteImpermanenceActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (!monster.IsMonsterShouldBeDisabledBeforeItUseEffect() || monster.IsDisabled() || base.Duel.LastChainPlayer == 0)
			{
				continue;
			}
			if (base.Card.Location == CardLocation.SpellZone)
			{
				for (int i = 0; i < 5; i++)
				{
					if (base.Bot.SpellZone[i] == base.Card)
					{
						infiniteImpermanenceList.Add(i);
						break;
					}
				}
			}
			if (base.Card.Location == CardLocation.Hand)
			{
				SelectSTPlace(base.Card, avoidImpermanence: true);
			}
			base.AI.SelectCard(monster);
			return true;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Card.Location == CardLocation.SpellZone)
		{
			int num = -1;
			int num2 = -1;
			for (int j = 0; j < 5; j++)
			{
				if (base.Bot.SpellZone[j] == base.Card)
				{
					num = j;
				}
				if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.SpellZone && base.Enemy.SpellZone[j] == lastChainCard)
				{
					num2 = j;
				}
				else if (base.Duel.Player == 0 && base.Util.GetProblematicEnemySpell() != null && base.Enemy.SpellZone[j] != null && base.Enemy.SpellZone[j].IsFloodgate())
				{
					num2 = j;
				}
			}
			if ((num * num2 >= 0 && num + num2 == 4) || base.Util.IsChainTarget(base.Card) || (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.IsCode(18144506)))
			{
				ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true);
				base.Enemy.GetMonsters();
				base.AI.SelectCard(problematicEnemyMonster);
				infiniteImpermanenceList.Add(num);
				return true;
			}
		}
		if (lastChainCard == null || lastChainCard.Controller != 1 || lastChainCard.Location != CardLocation.MonsterZone || lastChainCard.IsDisabled() || lastChainCard.IsShouldNotBeTarget() || lastChainCard.IsShouldNotBeSpellTrapTarget())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			for (int k = 0; k < 5; k++)
			{
				if (base.Bot.SpellZone[k] == base.Card)
				{
					infiniteImpermanenceList.Add(k);
					break;
				}
			}
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
		}
		if (lastChainCard != null)
		{
			base.AI.SelectCard(lastChainCard);
		}
		else
		{
			List<ClientCard> monsters = base.Enemy.GetMonsters();
			monsters.Sort(CardContainer.CompareCardAttack);
			monsters.Reverse();
			foreach (ClientCard item in monsters)
			{
				if (item.IsFaceup() && !item.IsShouldNotBeTarget() && !item.IsShouldNotBeSpellTrapTarget())
				{
					base.AI.SelectCard(item);
					return true;
				}
			}
		}
		return true;
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

	public bool SpellSetCheck()
	{
		if (base.Card.Id == 99989863)
		{
			return false;
		}
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (new List<int> { 80801743 }.Contains(base.Card.Id) && base.Bot.HasInSpellZone(base.Card.Id))
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
		return false;
	}

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219) || card.HasSetcode(413))).ToList();
		List<int> dangerMonsterIdList = new List<int> { 99937011, 63542003, 9411399, 28954097, 30680659 };
		list.AddRange(base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => dangerMonsterIdList.Contains(card.Id)));
		return list;
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
			if (base.Enemy.HasInSpellZone(82732705, notDisabled: true))
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
		if (attack >= 0)
		{
			if (attack == 0)
			{
				attack = base.Util.GetBestAttack(base.Bot);
			}
			ClientCard clientCard6 = (from card in base.Enemy.MonsterZone
				where card != null && card.GetDefensePower() >= attack && card.GetDefensePower() > 0 && card.IsAttack() && CheckCanBeTargeted(card, canBeTarget, selfType) && (ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
				orderby card.Attack descending
				select card).FirstOrDefault();
			if (clientCard6 != null)
			{
				return clientCard6;
			}
		}
		return null;
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
		if (monsters.Count > 0 && !onlyFaceup)
		{
			return ShuffleCardList(monsters)[0];
		}
		return null;
	}

	public ClientCard GetBestEnemySpell(bool onlyFaceup = false, bool canBeTarget = false)
	{
		List<ClientCard> list = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list.Count > 0)
		{
			return ShuffleCardList(list)[0];
		}
		List<ClientCard> list2 = (from card in base.Enemy.GetSpells()
			where !card.IsFaceup() || !card.IsCode(15693423)
			select card).ToList();
		List<ClientCard> list3 = list2.Where((ClientCard ecard) => ecard.IsFaceup() && (ecard.HasType(CardType.Continuous) || ecard.HasType(CardType.Field) || ecard.HasType(CardType.Pendulum))).ToList();
		if (list3.Count > 0)
		{
			return ShuffleCardList(list3)[0];
		}
		if (list2.Count > 0 && !onlyFaceup)
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
		if (checkGrave && base.Enemy.Graveyard.Count > 0)
		{
			List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard c) => c.IsMonster()).ToList();
			if (list.Count > 0)
			{
				list.Sort(CardContainer.CompareCardAttack);
				list.Reverse();
				return list[0];
			}
			return ShuffleCardList(base.Enemy.Graveyard.ToList())[0];
		}
		return null;
	}

	public override void OnChainSolved(int chainIndex)
	{
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		ClientCard currentSolvingChainCard2 = base.Duel.GetCurrentSolvingChainCard();
		bool flag = base.Duel.IsCurrentSolvingChainNegated();
		Logger.DebugWriteLine($"[CHAIN] Solved idx={chainIndex} negated={flag} solving={CardStr(currentSolvingChainCard2)}");
		if (currentSolvingChainCard == null || base.Duel.IsCurrentSolvingChainNegated() || currentSolvingChainCard.Controller != 1)
		{
			return;
		}
		if (currentSolvingChainCard.IsCode(23434538))
		{
			enemyActivateMaxxC = true;
		}
		if (currentSolvingChainCard.IsCode(42141493))
		{
			enemyActivateMaxxC = true;
		}
		if (currentSolvingChainCard.IsCode(94145021))
		{
			enemyActivateLockBird = true;
		}
		if (!currentSolvingChainCard.IsCode(10045474))
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			if (base.Enemy.SpellZone[i] == currentSolvingChainCard)
			{
				infiniteImpermanenceList.Add(4 - i);
				break;
			}
		}
	}

	public override void OnChainEnd()
	{
		escapeTargetList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		enemyActivateInfiniteImpermanenceFromHand = false;
		for (int num = enemyPlaceThisTurn.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard = enemyPlaceThisTurn[num];
			if (clientCard == null || (clientCard.Location != CardLocation.SpellZone && clientCard.Location != CardLocation.MonsterZone))
			{
				enemyPlaceThisTurn.RemoveAt(num);
			}
		}
		if (thronePending && _throneStage == ThroneStage.None)
		{
			thronePending = false;
		}
		ResetThroneFlow();
		Logger.DebugWriteLine("[CHAIN] OnChainEnd");
		base.OnChainEnd();
	}

	private void ResetThroneFlow()
	{
		Logger.DebugWriteLine($"[THRONE] Reset flow (was stage={_throneStage}, pending={thronePending})");
		thronePending = false;
		throneSearched = false;
		throneDesiredPick = 0;
		_throneStage = ThroneStage.None;
	}

	public override void OnNewTurn()
	{
		if (base.Duel.Turn <= 1)
		{
			dimensionShifterCount = 0;
		}
		enemyActivateMaxxC = false;
		enemyActivateLockBird = false;
		enemyActivateInfiniteImpermanenceFromHand = false;
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		infiniteImpermanenceList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		sendToGYThisTurn.Clear();
		activatedCardIdList.Clear();
		enemyPlaceThisTurn.Clear();
		summonThisTurn.Clear();
		thronePending = false;
		throneSearched = false;
		throneDesiredPick = 0;
		_gateReviveTargetId = 0;
		_gateDiscardPreferredId = 0;
		_gateWantsRecycle = false;
		_spQuickMode = false;
		base.OnNewTurn();
	}

	private int PriorityIndex(int id)
	{
		int num = Array.IndexOf(LinkFodderPriority, id);
		if (num < 0)
		{
			return 999;
		}
		return num;
	}

	private bool IsProtectedMaterial(ClientCard c, bool allowUseYubelForLink = false)
	{
		if (c == null)
		{
			return true;
		}
		if (NEVER_SAC.Contains(c.Id))
		{
			return true;
		}
		if (c.EquipCards != null && c.EquipCards.Count > 0)
		{
			return true;
		}
		if (c.HasType(CardType.Link) && c.LinkCount >= 2)
		{
			return true;
		}
		if (c.HasType((CardType)8396864))
		{
			return true;
		}
		return false;
	}

	private ClientCard[] GetSafeMaterials(int need)
	{
		bool allowYubel = ShouldUseYubelForLink();
		return (from m in base.Bot.GetMonsters()
			where !IsProtectedMaterial(m, allowYubel)
			orderby PriorityIndex(m.Id), m.Attack
			select m).Take(need).ToArray();
	}

	private bool ShouldUseYubelForLink()
	{
		if (base.Duel.Player != 0)
		{
			return false;
		}
		List<ClientCard> list = (from m in base.Bot.GetMonsters()
			where m != null
			select m).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		bool num = list.Count <= 2;
		bool flag = ((!HasInExtra(2463794)) ? (HasInExtra(67680512) || HasInExtra(24269961) || HasInExtra(29301450)) : (HasInExtra(71818935) || HasInExtra(67680512) || HasInExtra(24269961) || HasInExtra(29301450)));
		return num && flag;
	}

	private bool HasInExtra(int id)
	{
		return base.Bot.ExtraDeck.Any((ClientCard c) => c != null && c.Id == id);
	}

	private bool DontSelfNG()
	{
		return base.Duel.LastChainPlayer != 0;
	}

	private int LowestBit(int mask)
	{
		return mask & -mask;
	}

	private int GetMyLinkedMMZMask()
	{
		int num = 0;
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (monster != null && monster.IsFaceup() && monster.HasType(CardType.Link))
			{
				num |= monster.GetLinkedZones();
			}
		}
		return num & 0x1F;
	}

	private bool S1_ActivateTerraformingForThrone()
	{
		if (base.Type != ExecutorType.Activate)
		{
			return false;
		}
		if (base.Card.Id != 73628505)
		{
			return false;
		}
		if (base.Bot.HasInHandOrInSpellZone(93729896))
		{
			return false;
		}
		base.AI.SelectCard(93729896);
		return true;
	}

	private bool ActNightmareThroneSearch()
	{
		if (base.Type != ExecutorType.Activate)
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand && base.Bot.HasInSpellZone(93729896))
		{
			return false;
		}
		int num = 0;
		if (!base.Bot.HasInHand(62318994) && CheckRemainInDeck(62318994) > 0)
		{
			num = 62318994;
		}
		else if (base.Bot.HasInHand(62318994) && !base.Bot.HasInHand(81034083) && CheckRemainInDeck(81034083) > 0)
		{
			num = 81034083;
		}
		else if (base.Bot.HasInHand(62318994) && base.Bot.HasInHand(81034083) && !base.Bot.HasInHand(27439792) && CheckRemainInDeck(27439792) > 0)
		{
			num = 27439792;
		}
		thronePending = true;
		throneSearched = false;
		throneDesiredPick = num;
		Logger.DebugWriteLine("[THRONE] Activate search; desiredPick=" + ((num == 0) ? "(auto)" : num.ToString()));
		DumpChain("ThroneActivate");
		return true;
	}

	private bool NSDarkBeckoningBeast()
	{
		if (base.Duel.Phase != DuelPhase.Main1)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(81034083) && base.Bot.HasInHand(62318994))
		{
			return false;
		}
		return true;
	}

	private bool ActDarkBeckoningBeast()
	{
		if (base.Duel.Phase != DuelPhase.Main1)
		{
			return false;
		}
		if (CheckRemainInDeck(80312545) > 0 && !base.Bot.HasInSpellZone(80312545))
		{
			base.AI.SelectCard(80312545);
			return true;
		}
		if (CheckRemainInDeck(27439792) > 0)
		{
			base.AI.SelectCard(27439792);
			return true;
		}
		return false;
	}

	private bool S4_ActivateSpiritGates()
	{
		if (base.Type != ExecutorType.Activate)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (base.Bot.HasInSpellZone(80312545, notDisabled: true, faceUp: true))
		{
			return false;
		}
		int num = 0;
		if (base.Bot.HasInMonstersZone(81034083, notDisabled: false, hasXyzMaterial: false, faceUp: true) && CheckRemainInDeck(27439792) > 0)
		{
			num = 27439792;
		}
		else if (CheckRemainInDeck(81034083) > 0)
		{
			num = 81034083;
		}
		else if (CheckRemainInDeck(27439792) > 0)
		{
			num = 27439792;
		}
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		return true;
	}

	private bool Gate_RecycleContinuous()
	{
		if (base.Card.Location != CardLocation.SpellZone)
		{
			return false;
		}
		if (!HaveFaceupLevel10())
		{
			return false;
		}
		if (!base.Bot.HasInGraveyard(65261141))
		{
			return false;
		}
		_gateWantsRecycle = true;
		return true;
	}

	private bool Is00FiendId(int id)
	{
		if (id != 78371393 && id != 90829280 && id != 81034083 && id != 27439792)
		{
			return id == 62318994;
		}
		return true;
	}

	private int PlanSpiritGatesReviveTarget()
	{
		if (base.Bot.HasInMonstersZone(60303245) && base.Bot.HasInGraveyard(81034083))
		{
			return 81034083;
		}
		bool flag = base.Bot.HasInMonstersZone(90829280, notDisabled: true);
		bool flag2 = base.Bot.HasInGraveyard(90829280);
		bool flag3 = base.Bot.HasInHand(90829280);
		if (!flag && (flag2 || flag3))
		{
			return 90829280;
		}
		if (base.Bot.HasInMonstersZone(60303245, notDisabled: true) && base.Bot.HasInGraveyard(81034083) && HasInExtra(71818935))
		{
			return 81034083;
		}
		if (!base.Bot.HasInMonstersZone(62318994, notDisabled: true) && base.Bot.HasInGraveyard(62318994))
		{
			return 62318994;
		}
		if (base.Bot.HasInGraveyard(81034083))
		{
			return 81034083;
		}
		if (base.Bot.HasInGraveyard(27439792))
		{
			return 27439792;
		}
		if (base.Bot.HasInGraveyard(78371393))
		{
			return 78371393;
		}
		if (!flag && flag3)
		{
			return 90829280;
		}
		return 0;
	}

	private int PickSpiritGatesDiscard(int reviveTargetId)
	{
		if (reviveTargetId == 90829280 && base.Bot.HasInHand(90829280))
		{
			return 90829280;
		}
		if (base.Bot.HasInHand(99989863))
		{
			return 99989863;
		}
		if (base.Bot.HasInHand(27439792))
		{
			return 27439792;
		}
		if (base.Bot.HasInHand(4779091))
		{
			return 4779091;
		}
		List<ClientCard> list = base.Bot.Hand.ToList();
		list.Sort((ClientCard a, ClientCard b) => ScoreOwnCardForCost(a).CompareTo(ScoreOwnCardForCost(b)));
		if (list.Count <= 0)
		{
			return 0;
		}
		return list[0].Id;
	}

	private bool Gate_Revive00Fiend()
	{
		if (base.Card.Location != CardLocation.SpellZone)
		{
			return false;
		}
		if (requiemSummoned)
		{
			if (!base.Bot.HasInHandOrInGraveyard(90829280))
			{
				return false;
			}
			if (base.Bot.Hand.Count <= 0)
			{
				return false;
			}
			_gateReviveTargetId = 90829280;
			_gateDiscardPreferredId = PickSpiritGatesDiscard(_gateReviveTargetId);
			_gateWantsRecycle = false;
			return true;
		}
		int num = PlanSpiritGatesReviveTarget();
		if (num == 0)
		{
			return false;
		}
		if (base.Bot.Hand.Count <= 0)
		{
			return false;
		}
		int num2 = PickSpiritGatesDiscard(num);
		if (num2 == 0)
		{
			return false;
		}
		_gateReviveTargetId = num;
		_gateDiscardPreferredId = num2;
		_gateWantsRecycle = false;
		return true;
	}

	private bool HaveFaceupLevel10()
	{
		return base.Bot.MonsterZone.Any((ClientCard m) => m != null && m.IsFaceup() && m.Level == 10);
	}

	private bool ActNightmarePainHand()
	{
		if (base.Bot.HasInSpellZone(65261141, notDisabled: true, faceUp: true))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			return true;
		}
		return false;
	}

	private bool ActNightmarePainEffect()
	{
		if (base.Card.Location == CardLocation.SpellZone)
		{
			if (CheckRemainInDeck(24215921) == 0)
			{
				return false;
			}
			if (base.Bot.HasInMonstersZone(90829280) || base.Bot.HasInHand(90829280))
			{
				base.AI.SelectCard(90829280);
				base.AI.SelectNextCard(24215921);
				return true;
			}
		}
		return false;
	}

	private bool S6_ChainThroneFollowUp()
	{
		if (base.Type != ExecutorType.Activate)
		{
			return false;
		}
		if (sendToGYThisTurn.Any((ClientCard c) => c != null && YUBEL_SET.Contains(c.Id)) && base.Bot.HasInHand(93729896))
		{
			base.AI.SelectYesNo(opt: true);
			return true;
		}
		return false;
	}

	private bool NSSamsaraDLotus()
	{
		if (base.Bot.HasInMonstersZone(90829280))
		{
			return false;
		}
		return true;
	}

	private bool ActSamsaraDLotus()
	{
		if (base.Duel.Player == 0 && base.Card.Location == CardLocation.MonsterZone)
		{
			base.AI.SelectCard(90829280);
			return true;
		}
		if (base.Duel.Player == 1 && (base.Bot.HasInMonstersZone(78371393) || base.Bot.HasInMonstersZone(90829280)))
		{
			return true;
		}
		return false;
	}

	private bool ActSamsaraDLotusGY()
	{
		if (base.Card.Location == CardLocation.Grave && base.Bot.HasInMonstersZone(78371393))
		{
			base.AI.SelectOption(1);
			return true;
		}
		return false;
	}

	private bool ActTract()
	{
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (requiemSummoned)
		{
			return false;
		}
		base.AI.SelectCard(97651498);
		base.AI.SelectNextCard(97651498);
		return true;
	}

	private bool ActParadise()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZoneOrInGraveyard(82135803) || base.Bot.HasInBanished(82135803))
		{
			return false;
		}
		base.AI.SelectCard(82135803);
		return DontSelfNG();
	}

	private bool ActDesirae()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		ClientCard bestEnemyCard = GetBestEnemyCard(onlyFaceup: true, canBeTarget: true);
		if (bestEnemyCard == null)
		{
			return false;
		}
		if (base.Bot.HasInGraveyard(2463794))
		{
			base.AI.SelectCard(2463794);
			base.AI.SelectNextCard(bestEnemyCard);
			return true;
		}
		base.AI.SelectCard(bestEnemyCard);
		return true;
	}

	private bool ActRequiemMZ()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (base.Bot.HasInHand(28803166) || CheckRemainInDeck(28803166) > 0)
		{
			base.AI.SelectCard(28803166);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			return true;
		}
		if (base.Bot.HasInHand(60764609) || CheckRemainInDeck(60764609) > 0)
		{
			base.AI.SelectCard(60764609);
			base.AI.SelectPosition(CardPosition.FaceUpDefence);
			return true;
		}
		return false;
	}

	private bool ActRequiemEQ()
	{
		if (!HasInExtra(93860227))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(28803166))
		{
			base.AI.SelectCard(28803166);
			return true;
		}
		if (base.Bot.HasInMonstersZone(60764609))
		{
			base.AI.SelectCard(60764609);
			return true;
		}
		return false;
	}

	private bool SSNecroquip()
	{
		if (base.Bot.HasInSpellZone(2463794) && base.Bot.HasInMonstersZone(28803166))
		{
			base.AI.SelectCard(2463794);
			base.AI.SelectNextCard(28803166);
			return true;
		}
		if (base.Bot.HasInSpellZone(2463794) && base.Bot.HasInMonstersZone(60764609))
		{
			base.AI.SelectCard(2463794);
			base.AI.SelectNextCard(60764609);
			return true;
		}
		return false;
	}

	private bool ActLacimaCT()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(60764609) && !base.Bot.HasInBanished(60764609))
			{
				base.AI.SelectCard(60764609);
				return true;
			}
			if (!base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(99989863) && !base.Bot.HasInBanished(99989863))
			{
				base.AI.SelectCard(99989863);
				return true;
			}
			return false;
		}
		return false;
	}

	private bool ActLacimaCTGY()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (base.Bot.HasInBanished(99989863) || base.Bot.HasInHandOrInGraveyard(99989863))
			{
				return false;
			}
			return DontSelfNG();
		}
		return false;
	}

	private bool ActEngraverHand()
	{
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (!base.Bot.HasInHandOrInSpellZoneOrInGraveyard(98567237) && !base.Bot.HasInBanished(98567237))
		{
			base.AI.SelectCard(98567237);
			return true;
		}
		return false;
	}

	private bool ActEngraverGY()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInGraveyard(97651498))
		{
			base.AI.SelectCard(97651498);
			return true;
		}
		if (base.Bot.HasInGraveyard(71818935))
		{
			base.AI.SelectCard(71818935);
			return true;
		}
		return false;
	}

	private bool SSMoon()
	{
		if (moonSummoned)
		{
			return false;
		}
		if (requiemSummoned)
		{
			return false;
		}
		if (!HasInExtra(2463794))
		{
			return false;
		}
		ClientCard[] safeMaterials = GetSafeMaterials(2);
		if (safeMaterials.Length < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(safeMaterials);
		moonSummoned = true;
		return true;
	}

	private bool ActAbo()
	{
		if (base.Bot.HasInGraveyard(67680512))
		{
			base.AI.SelectCard(67680512);
			return true;
		}
		return false;
	}

	private bool AlmirajSummon()
	{
		if (base.Bot.GetMonsterCount() > 1)
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().First();
		if (clientCard.IsCode(new int[1] { 81034083 }))
		{
			base.AI.SelectMaterials(clientCard);
			return true;
		}
		return false;
	}

	private bool SSGGS()
	{
		if (!DontSelfNG())
		{
			return false;
		}
		if (BlockIfThrone("GGS"))
		{
			return false;
		}
		if (base.Duel.Player == 1)
		{
			return false;
		}
		if (InThroneFlow)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(90829280))
		{
			base.AI.SelectYesNo(opt: true);
			base.AI.SelectCard(90829280);
			return true;
		}
		base.AI.SelectYesNo(opt: false);
		return true;
	}

	private bool ActGGSGY()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInGraveyard(90829280))
		{
			base.AI.SelectCard(90829280);
			return true;
		}
		return false;
	}

	private bool ActLittleKnight()
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
					clientCard = GetBestEnemyMonster(onlyFaceup: true, canBeTarget: true);
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

	private bool ActRageQuickLink()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (base.Duel.Player != 1)
		{
			return false;
		}
		if (base.Duel.Phase < DuelPhase.Main1 || base.Duel.Phase > DuelPhase.Main2)
		{
			return false;
		}
		if (!HasValidRageLinkCandidate())
		{
			return false;
		}
		ClientCard bestEnemyMonster = GetBestEnemyMonster(onlyFaceup: true, canBeTarget: true);
		if (bestEnemyMonster == null)
		{
			return false;
		}
		base.AI.SelectCard(bestEnemyMonster);
		return DontSelfNG();
	}

	private bool SSPhantom()
	{
		List<int> list = new List<int> { 4779091, 78371393, 81034083, 27439792, 90829280 };
		if (!base.Bot.HasInGraveyard(list))
		{
			return false;
		}
		if (base.Bot.HasInHandOrInMonstersZoneOrInGraveyard(4779091))
		{
			base.AI.SelectCard(4779091);
		}
		else if (base.Bot.HasInMonstersZoneOrInGraveyard(78371393))
		{
			base.AI.SelectCard(78371393);
		}
		else
		{
			if (!base.Bot.HasInGraveyard(90829280))
			{
				return false;
			}
			base.AI.SelectCard(90829280);
		}
		base.AI.SelectNextCard(list);
		return true;
	}

	private bool ActSharvara()
	{
		if (BlockIfThrone("Sharvara"))
		{
			return false;
		}
		if (base.Duel.Player == 1)
		{
			return false;
		}
		if (InThroneFlow)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(90829280))
		{
			base.AI.SelectCard(90829280);
			return true;
		}
		if (base.Bot.HasInMonstersZone(78371393))
		{
			base.AI.SelectCard(78371393);
			return true;
		}
		return false;
	}

	private bool ActSharvaraGY()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		return true;
	}

	private bool ActVarudras()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true);
		int activateDescription = base.ActivateDescription;
		int stringId = base.Util.GetStringId(70636044, 1);
		int stringId2 = base.Util.GetStringId(70636044, 2);
		Logger.DebugWriteLine("[Varudras] desc: " + activateDescription + ", timing = " + base.CurrentTiming);
		ClientCard clientCard = normalEnemyTargetList.FirstOrDefault((ClientCard c) => c != null && c.Controller == 1);
		if (activateDescription == stringId && base.Duel.LastChainPlayer == 1 && base.Duel.CurrentChain.Count > 0)
		{
			if (!CheckLastChainShouldNegated())
			{
				return false;
			}
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		if (activateDescription == stringId || activateDescription == stringId2 || activateDescription == -1)
		{
			if (normalEnemyTargetList.Count == 0)
			{
				return false;
			}
			if (clientCard != null)
			{
				normalEnemyTargetList.Insert(0, clientCard);
			}
			else
			{
				ClientCard clientCard2 = (from c in base.Bot.GetMonsters().Concat(base.Bot.GetSpells())
					where c != null
					select c).OrderBy(ScoreOwnCardForCost).FirstOrDefault();
				if (clientCard2 != null)
				{
					normalEnemyTargetList.Insert(0, clientCard2);
				}
			}
			if (activateDescription == stringId && base.Duel.CurrentChain.Count == 0)
			{
				activatedCardIdList.Add(base.Card.Id + 1);
			}
			if (activateDescription == stringId2)
			{
				activatedCardIdList.Add(base.Card.Id + 2);
			}
			base.AI.SelectCard(normalEnemyTargetList);
			return true;
		}
		return false;
	}

	private bool ShouldVarudrasDetachForPop(ClientCard target)
	{
		if (target == null)
		{
			return false;
		}
		if (target.IsFloodgate() || target.IsMonsterDangerous() || target.IsMonsterInvincible())
		{
			return true;
		}
		return ScoreEnemyCardForRemoval(target) >= 3000;
	}

	private bool ActYamaGY()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInGraveyard(90829280))
		{
			return false;
		}
		base.AI.SelectCard(90829280);
		base.AI.SelectYesNo(opt: false);
		return true;
	}

	private bool ActYamaMZ()
	{
		if (CheckRemainInDeck(41165831) == 0)
		{
			return false;
		}
		base.AI.SelectCard(41165831);
		return true;
	}

	private bool SSRequiem()
	{
		if (CheckRemainInDeck(28803166) == 0 && !base.Bot.HasInHand(28803166))
		{
			return false;
		}
		requiemSummoned = true;
		return true;
	}

	private bool L4ABOSS()
	{
		if (!HasInExtra(29479265))
		{
			return false;
		}
		List<ClientCard> monsters = base.Bot.GetMonsters();
		ClientCard clientCard = monsters.FirstOrDefault((ClientCard m) => m != null && m.Id == 24269961);
		ClientCard clientCard2 = monsters.FirstOrDefault((ClientCard m) => m != null && m.Id == 67680512);
		ClientCard clientCard3 = monsters.FirstOrDefault((ClientCard m) => m != null && m.Id == 78371393);
		ClientCard clientCard4 = monsters.FirstOrDefault((ClientCard m) => m != null && m.Id == 4779091);
		if (clientCard != null && clientCard2 != null && IsInEMZ(clientCard))
		{
			base.AI.SelectMaterials(new ClientCard[2] { clientCard, clientCard2 });
			return true;
		}
		if (clientCard2 != null && clientCard3 != null && clientCard4 != null && IsInEMZ(clientCard2))
		{
			base.AI.SelectMaterials(new ClientCard[3] { clientCard2, clientCard3, clientCard4 });
			return true;
		}
		ClientCard firstLink = clientCard2 ?? clientCard ?? (from m in monsters
			where m.HasType(CardType.Link)
			orderby m.LinkCount descending
			select m).FirstOrDefault();
		if (firstLink != null)
		{
			List<ClientCard> list = monsters.Where((ClientCard m) => m != firstLink && !NEVER_SAC.Contains(m.Id)).OrderBy(ScoreOwnCardForCost).ToList();
			List<ClientCard> list2 = new List<ClientCard> { firstLink };
			int num = 4 - LinkValue(firstLink);
			foreach (ClientCard item in list)
			{
				list2.Add(item);
				num -= LinkValue(item);
				if (num <= 0)
				{
					break;
				}
			}
			if (num <= 0)
			{
				base.AI.SelectMaterials(list2.ToArray());
				return true;
			}
		}
		return false;
	}

	private int LinkValue(ClientCard c)
	{
		if (c == null || !c.HasType(CardType.Link))
		{
			return 1;
		}
		return Math.Max(1, c.LinkCount);
	}

	private bool IsInEMZ(ClientCard c)
	{
		ClientCard[] monsterZone = base.Bot.MonsterZone;
		if (monsterZone.Length <= 5 || monsterZone[5] != c)
		{
			if (monsterZone.Length > 6)
			{
				return monsterZone[6] == c;
			}
			return false;
		}
		return true;
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

	private ClientCard[] GetSafeMaterialsExcluding(HashSet<int> excludeIds, int need)
	{
		return (from m in base.Bot.GetMonsters()
			where m != null && (excludeIds == null || !excludeIds.Contains(m.Id)) && !IsProtectedMaterial(m)
			orderby PriorityIndex(m.Id), m.Attack
			select m).Take(need).ToArray();
	}

	private bool CanMakeRageWithoutYama()
	{
		return GetSafeMaterialsExcluding(new HashSet<int> { 24269961 }, 2).Length >= 2;
	}

	private bool L2YamaSetup()
	{
		ClientCard[] safeMaterials = GetSafeMaterials(2);
		if (safeMaterials.Length < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(safeMaterials);
		return true;
	}

	private bool L2RageKeepYama()
	{
		if (!base.Bot.HasInMonstersZone(24269961, notDisabled: true))
		{
			return false;
		}
		if (!CanMakeRageWithoutYama())
		{
			return false;
		}
		ClientCard[] safeMaterialsExcluding = GetSafeMaterialsExcluding(new HashSet<int> { 24269961 }, 2);
		if (safeMaterialsExcluding.Length < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(safeMaterialsExcluding);
		return true;
	}

	private bool HasFreeEMZ()
	{
		ClientCard[] monsterZone = base.Bot.MonsterZone;
		bool num = monsterZone.Length > 5 && monsterZone[5] == null;
		bool flag = monsterZone.Length > 6 && monsterZone[6] == null;
		return num || flag;
	}

	private bool HasValidRageLinkCandidate()
	{
		bool num = HasInExtra(29301450);
		bool flag = HasInExtra(12067160);
		flag = HasInExtra(12067160);
		if (num)
		{
			return true;
		}
		if (flag && HasFreeEMZ())
		{
			return true;
		}
		return false;
	}

	private bool YesNoFor(int desc, int cardId, int idx)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (desc == base.Util.GetStringId(cardId, idx))
		{
			if (currentSolvingChainInfo == null || !currentSolvingChainInfo.IsCode(cardId))
			{
				return currentSolvingChainCard?.IsCode(cardId) ?? false;
			}
			return true;
		}
		return false;
	}

	public override bool OnSelectYesNo(int desc)
	{
		Logger.DebugWriteLine($"[DEBUG] OnSelectYesNo: desc={desc}");
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		DumpChain("OnSelectYesNo");
		Logger.DebugWriteLine($"[THRONE] OnSelectYesNo desc={desc} stage={_throneStage} solving={CardStr(currentSolvingChainCard)}");
		if (currentSolvingChainInfo != null && currentSolvingChainInfo.ActivatePlayer == 1)
		{
			return false;
		}
		if (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(93729896))
		{
			DebugThroneDescMap(desc);
			if (_throneStage == ThroneStage.None && !throneSearched)
			{
				_throneStage = ThroneStage.Searching;
				return true;
			}
			if (_throneStage == ThroneStage.AwaitDestroyPrompt || throneSearched)
			{
				_throneStage = ThroneStage.None;
				return false;
			}
			if (_throneStage == ThroneStage.Searching && !throneSearched)
			{
				return true;
			}
		}
		if (YesNoFor(desc, 70636044, 1))
		{
			ClientCard bestEnemyCard = GetBestEnemyCard();
			if (bestEnemyCard != null)
			{
				return ShouldVarudrasDetachForPop(bestEnemyCard);
			}
			return false;
		}
		if (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(70636044) && base.Duel.CurrentChain.Count > 0)
		{
			ClientCard clientCard = GetNormalEnemyTargetList(canBeTarget: true, ignoreCurrentDestroy: true).FirstOrDefault((ClientCard c) => c.Controller == 1);
			if (clientCard == null)
			{
				return false;
			}
			return ShouldVarudrasDetachForPop(clientCard);
		}
		if (YesNoFor(desc, 78371393, 2))
		{
			return _yubelWantsTribute = base.Bot.GetMonsters().Any((ClientCard m) => m != null && m.Id == 62318994);
		}
		return base.OnSelectYesNo(desc);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		Logger.DebugWriteLine($"[DEBUG] OnSelectCard: hint={hint} (0x{hint:X}), min={min}, max={max}, cancelable={cancelable}, candidates={cards?.Count ?? 0}");
		bool flag = (long)hint == 500 || hint.ToString().ToLower().Contains("release");
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (cards != null && cards.Count > 0)
		{
			if (_throneStage == ThroneStage.Searching && currentSolvingChainCard != null && currentSolvingChainCard.IsCode(93729896) && !throneSearched && cards != null && cards.Count > 0)
			{
				throneSearched = true;
				_throneStage = ThroneStage.AwaitDestroyPrompt;
				ClientCard clientCard = null;
				if (throneDesiredPick != 0)
				{
					clientCard = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == throneDesiredPick);
				}
				if (clientCard == null)
				{
					clientCard = cards.FirstOrDefault((ClientCard c) => c != null && YUBEL_SET.Contains(c.Id)) ?? cards[0];
				}
				Logger.DebugWriteLine("[THRONE] Search pick => " + CardStr(clientCard));
				return new ClientCard[1] { clientCard };
			}
			if (base.Card != null && base.Card.Id == 80312545 && cards != null && cards.Count > 0)
			{
				if (_gateWantsRecycle)
				{
					ClientCard clientCard2 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == 65261141);
					if (clientCard2 != null)
					{
						return new ClientCard[1] { clientCard2 };
					}
					ClientCard clientCard3 = cards.FirstOrDefault((ClientCard c) => c != null && c.IsSpell() && c.HasType(CardType.Continuous));
					if (clientCard3 != null)
					{
						return new ClientCard[1] { clientCard3 };
					}
				}
				if (cards.All((ClientCard c) => c != null && c.Controller == 0 && c.Location == CardLocation.Hand) && _gateDiscardPreferredId != 0)
				{
					ClientCard clientCard4 = cards.FirstOrDefault((ClientCard c) => c.Id == _gateDiscardPreferredId);
					if (clientCard4 == null)
					{
						List<ClientCard> list = cards.OrderBy(ScoreOwnCardForCost).ToList();
						return new ClientCard[1] { list[0] };
					}
					return new ClientCard[1] { clientCard4 };
				}
				if (cards.Any((ClientCard c) => c != null && c.Location == CardLocation.Grave))
				{
					if (requiemSummoned)
					{
						ClientCard clientCard5 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == 90829280);
						if (clientCard5 != null)
						{
							return new ClientCard[1] { clientCard5 };
						}
					}
					if (_gateReviveTargetId != 0)
					{
						ClientCard clientCard6 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == _gateReviveTargetId);
						if (clientCard6 != null)
						{
							return new ClientCard[1] { clientCard6 };
						}
					}
					if (moonSummoned)
					{
						int[] array = new int[2] { 90829280, 78371393 };
						foreach (int id in array)
						{
							ClientCard clientCard7 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == id);
							if (clientCard7 != null)
							{
								return new ClientCard[1] { clientCard7 };
							}
						}
					}
					else
					{
						int[] array = new int[5] { 90829280, 62318994, 81034083, 27439792, 78371393 };
						foreach (int id2 in array)
						{
							ClientCard clientCard8 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == id2);
							if (clientCard8 != null)
							{
								return new ClientCard[1] { clientCard8 };
							}
						}
					}
					ClientCard clientCard9 = cards.FirstOrDefault((ClientCard c) => c != null && Is00FiendId(c.Id));
					if (clientCard9 != null)
					{
						return new ClientCard[1] { clientCard9 };
					}
				}
			}
			if (base.Card.Id == 93729896 && _throneStage == ThroneStage.Searching && thronePending && !throneSearched)
			{
				throneSearched = true;
				_throneStage = ThroneStage.AwaitDestroyPrompt;
				ClientCard clientCard10 = null;
				if (throneDesiredPick != 0)
				{
					clientCard10 = cards.FirstOrDefault((ClientCard c) => c.Id == throneDesiredPick);
				}
				if (clientCard10 == null)
				{
					clientCard10 = cards[0];
				}
				Logger.DebugWriteLine("[THRONE] Search pick => " + CardStr(clientCard10));
				return new ClientCard[1] { clientCard10 };
			}
			if (_yubelWantsTribute && flag && cards != null && cards.Count > 0)
			{
				ClientCard clientCard11 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == 62318994);
				if (clientCard11 != null)
				{
					_yubelWantsTribute = false;
					return new ClientCard[1] { clientCard11 };
				}
				_yubelWantsTribute = false;
			}
			if (base.Card.Id == 70636044 && hint == 502 && cards != null && cards.Count > 0)
			{
				ClientCard clientCard12 = (from c in cards
					where c != null && c.Controller == 1
					orderby ScoreEnemyCardForRemoval(c) descending
					select c).FirstOrDefault();
				if (clientCard12 == null)
				{
					return new ClientCard[1] { cards[0] };
				}
				return new ClientCard[1] { clientCard12 };
			}
			if (base.Card.Id == 29479265 && hint == 502 && cards != null && cards.Count > 0)
			{
				ClientCard clientCard13 = (from c in cards
					where c != null && c.Controller == 1
					orderby ScoreEnemyCardForRemoval(c) descending
					select c).FirstOrDefault();
				if (clientCard13 == null)
				{
					return new ClientCard[1] { cards[0] };
				}
				return new ClientCard[1] { clientCard13 };
			}
			if (currentSolvingChainCard != null && currentSolvingChainCard.IsCode(67680512) && cards.Any((ClientCard c) => c != null && c.Location == CardLocation.Extra))
			{
				ClientCard clientCard14 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == 29301450);
				if (clientCard14 != null)
				{
					return new List<ClientCard> { clientCard14 };
				}
				ClientCard clientCard15 = null;
				clientCard15 = cards.FirstOrDefault((ClientCard c) => c != null && c.Id == 12067160);
				if (clientCard15 != null && HasFreeEMZ())
				{
					return new List<ClientCard> { clientCard15 };
				}
				return new List<ClientCard> { cards[0] };
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	private int ScoreOwnCardForCost(ClientCard c)
	{
		if (c == null)
		{
			return 2147483647;
		}
		int num = 5000;
		if (NEVER_SAC.Contains(c.Id))
		{
			return 2147483647;
		}
		if (YUBEL_SET.Contains(c.Id))
		{
			return 9000;
		}
		if ((c.HasType(CardType.Link) && c.LinkCount >= 2) || c.HasType((CardType)8396864))
		{
			num += 2000;
		}
		if (c.EquipCards != null && c.EquipCards.Count > 0)
		{
			num += 1000;
		}
		int num2 = Array.IndexOf(YubelCostPriority, c.Id);
		if (num2 >= 0)
		{
			num = 10 + num2;
		}
		return num + Math.Max(0, c.Attack / 100);
	}

	private int ScoreEnemyCardForRemoval(ClientCard c)
	{
		if (c == null)
		{
			return -1;
		}
		int num = 0;
		if (c.IsFloodgate())
		{
			num += 6000;
		}
		if (c.IsMonsterDangerous())
		{
			num += 4000;
		}
		if (c.IsMonsterInvincible())
		{
			num += 3500;
		}
		if (c.EquipCards != null && c.EquipCards.Count > 0)
		{
			num += 800;
		}
		if (c.HasType((CardType)8396992))
		{
			num += 700;
		}
		if (c.HasType(CardType.Link) && c.LinkCount >= 2)
		{
			num += 700;
		}
		return num + Math.Max(0, c.Attack);
	}

	private string CardStr(ClientCard c)
	{
		if (c == null)
		{
			return "null";
		}
		string text = c.Location.ToString();
		string text2 = (c.IsFaceup() ? "FU" : "FD");
		return $"{c.Name}#{c.Id} [{text}] P{c.Controller} {text2}";
	}

	private void DumpChain(string tag = "")
	{
		Logger.DebugWriteLine(string.Format("[CHAIN]{0} turn={1} player={2} phase={3} chainCount={4}", (tag == "") ? "" : (" (" + tag + ")"), base.Duel.Turn, base.Duel.Player, base.Duel.Phase, base.Duel.CurrentChain.Count));
		for (int i = 0; i < base.Duel.CurrentChain.Count; i++)
		{
			ClientCard c = base.Duel.CurrentChain[i];
			Logger.DebugWriteLine($"  [{i}] {CardStr(c)}");
		}
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (currentSolvingChainCard != null)
		{
			Logger.DebugWriteLine($"  -> Solving: {CardStr(currentSolvingChainCard)}  ActivateDescription={base.ActivateDescription}");
		}
		if (base.Duel.ChainTargets != null && base.Duel.ChainTargets.Count > 0)
		{
			IEnumerable<string> values = base.Duel.ChainTargets.Where((ClientCard t) => t != null).Select(CardStr);
			Logger.DebugWriteLine("  targets: " + string.Join(" | ", values));
		}
	}

	private void DebugThroneDescMap(int incomingDesc)
	{
		for (int i = 0; i < 5; i++)
		{
			int stringId = base.Util.GetStringId(93729896, i);
			Logger.DebugWriteLine($"[THRONE] desc map i={i} strId={stringId} match={stringId == incomingDesc}");
		}
	}

	private bool BlockIfThrone(string who)
	{
		if (InThroneFlow)
		{
			Logger.DebugWriteLine("[THRONE] BLOCKED " + who + " during Throne flow");
			return true;
		}
		return false;
	}
}
