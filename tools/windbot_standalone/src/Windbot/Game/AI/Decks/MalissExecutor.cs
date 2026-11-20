using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Maliss", "AI_Maliss", "Normal")]
public class MalissExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int DominusImpulse = 40366667;

		public const int TERRAFORMING = 73628505;

		public const int GoldSarcophagus = 75500286;

		public const int BackupIgnister = 30118811;

		public const int WizardIgnister = 3723262;

		public const int MalissP_Dormouse = 32061192;

		public const int MalissP_WhiteRabbit = 69272449;

		public const int MalissP_ChessyCat = 96676583;

		public const int MalissP_MarchHare = 20938824;

		public const int MalissC_GWC06 = 20726052;

		public const int MalissC_MTP07 = 94722358;

		public const int MalissQ_RedRansom = 68059897;

		public const int MalissQ_WhiteBinder = 95454996;

		public const int MalissQ_HeartsCrypter = 21848500;

		public const int MalissInTheMirror = 93453053;

		public const int MalissInUnderground = 68337209;

		public const int Linguriboh = 24842059;

		public const int LinkDecoder = 30342076;

		public const int SP_LITTLE_KNIGHT = 29301450;

		public const int SALAMANGREAT_ALMIRAJ = 60303245;

		public const int SplashMage = 59859086;

		public const int CyberseWicckid = 52698008;

		public const int TranscodeTalker = 46947713;

		public const int AlliedCodeTalkerIgnister = 39138610;

		public const int FirewallDragon = 5043010;

		public const int LinkSpider = 98978921;

		public const int HaggardLizardose = 9763474;

		public const int AccesscodeTalker = 86066372;

		public const int Apollousa = 4280258;

		public const int Lancea = 34267821;

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

		public const int GhostMournerMoonlitChill = 52038441;

		public const int NibiruThePrimalBeing = 27204311;
	}

	private enum FinishPlan
	{
		FW_HC_Allied,
		HC_Allied,
		AlliedOnly
	}

	private const int SetcodeMaliss = 447;

	private const int SetcodeTimeLord = 74;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeHorus = 413;

	private const int SetcodeDarkWorld = 6;

	private const int SetcodeSkyStriker = 277;

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 96676583, 20938824, 69272449, 68337209, 30118811, 32061192, 14558127, 10045474, 40366667 }
		},
		{
			2,
			new List<int> { 23434538, 24224830 }
		},
		{
			1,
			new List<int> { 75500286, 73628505, 20726052, 34267821, 94722358, 65681983, 93453053, 3723262, 27204311 }
		}
	};

	private List<int> notToNegateIdList = new List<int> { 58699500, 20343502, 19403423 };

	private List<int> notToDestroySpellTrap = new List<int> { 50005218, 6767771 };

	private List<int> targetNegateIdList = new List<int>
	{
		97268402, 10045474, 52038441, 78474168, 74003290, 67037924, 9753964, 66192538, 23204029, 73445448,
		35103106, 30286474, 45002991, 5795980, 38511382, 53742162, 30430448
	};

	private bool usedNormalSummon;

	private bool ssDormouse;

	private bool ssWhiteRabbit;

	private bool ssChessyCat;

	private bool ssMarchHare;

	private bool ActiveMarchHare;

	private bool ssRRThisTurn;

	private bool ssWBThisTurn;

	private bool ssHCThisTurn;

	private bool enemyActivateLancea;

	private bool enemyActivateFuwalos;

	private bool ActiveUnderground;

	private bool blockWicckid;

	private bool mtp07SetThisTurn;

	private bool gwc06SetThisTurn;

	private bool splashNegatedThisTurn;

	private bool Allied_End;

	private bool fullBoard1;

	private bool goldstart;

	private bool undergroundstart;

	private bool nsplan;

	private bool nsBackupplan;

	private bool NSDorMouse;

	private bool nsLanceaplan;

	private int myTurnCount;

	private bool avoidLinkedZones;

	private bool wantLinkedToWicckid;

	private int? _wicckidEmzIndex;

	private int _transcodeZoneMask;

	private const int MZ0 = 1;

	private const int MZ1 = 2;

	private const int MZ2 = 4;

	private const int MZ3 = 8;

	private const int MZ4 = 16;

	private const int EMZ_L = 32;

	private const int EMZ_R = 64;

	private const int EMZ_ALL = 96;

	private int _wicckidEmzBit;

	private int _forceTranscodeBit;

	private bool step1Done;

	private bool step2Done;

	private int lastRevivedIdBySplash;

	private bool coreSetupComplete;

	private bool madeIt3;

	private bool resultSuccessFlag;

	private bool _didSplashToRR;

	private bool _didRRtoWicckid;

	private bool _didSummonToWicckidArrow;

	private bool _didWBFromWicckid;

	private bool _finishPlanDecided;

	private bool _preferWicckidArrows;

	private bool _rrSelfSSPlacing;

	private FinishPlan _finishPlan;

	private bool _oppJustActivatedPersistentSpell;

	private bool _oppJustSummoned;

	private bool _oppJustSet;

	private int _prefWindowTTL;

	private int _enemyMonsterCountSnap;

	private int _enemyFacedownSTSnap;

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

	private static readonly int EMZ_LEFT = 5;

	private static readonly int EMZ_RIGHT = 6;

	private static readonly int[] PreferCenterMainSeq = new int[5] { 2, 1, 3, 0, 4 };

	public MalissExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.SpellSet, 20726052, SpellSetCheck);
		AddExecutor(ExecutorType.SpellSet, 94722358, SpellSetCheck);
		AddExecutor(ExecutorType.Activate, 86066372, Accesscode_OnSummon_AtkUp);
		AddExecutor(ExecutorType.Activate, 86066372, Accesscode_Destroy_Ignition);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 4280258, DontSelfNG);
		AddExecutor(ExecutorType.Activate, 40366667, DontSelfNG);
		AddExecutor(ExecutorType.Activate, 39138610, Allied_NegateBanish);
		AddExecutor(ExecutorType.Activate, 5043010, FirewallBounce_OnOppSummon);
		AddExecutor(ExecutorType.Activate, 94722358, MTP07_OppTurn_RemoveEnemyOnly);
		AddExecutor(ExecutorType.Activate, 21848500, HC_Quick_ReturnBanished_AndBanishField);
		AddExecutor(ExecutorType.Activate, 20726052, GWC06_OppTurn_ReviveWB_HC);
		AddExecutor(ExecutorType.Activate, 29301450, ActLittleKnight);
		AddExecutor(ExecutorType.Activate, 68059897, RR_SS_FromBanished);
		AddExecutor(ExecutorType.Activate, 21848500, HC_OnBanished_SpecialSummon);
		AddExecutor(ExecutorType.Activate, 93453053, Mirror_Banish);
		AddExecutor(ExecutorType.Summon, 32061192, Step1_Dormouse_NormalSummon);
		AddExecutor(ExecutorType.Summon, 69272449, Step1_WhiteRabbit_NormalSummon);
		AddExecutor(ExecutorType.Summon, 96676583, TwoCC_NormalSummon);
		AddExecutor(ExecutorType.Summon, 96676583, Emergency_NormalCat);
		AddExecutor(ExecutorType.Summon, 20938824, NSMH);
		AddExecutor(ExecutorType.Summon, 30118811, NSBackup_L);
		AddExecutor(ExecutorType.Summon, 30118811, NSBackup);
		AddExecutor(ExecutorType.Activate, 32061192, Dormouse_SS_FromBanished);
		AddExecutor(ExecutorType.Activate, 32061192, Dormouse_ForMH);
		AddExecutor(ExecutorType.Activate, 32061192, Dormouse_Banish_Anytime);
		AddExecutor(ExecutorType.Activate, 69272449, Step1_WhiteRabbit_SS_FromBanished);
		AddExecutor(ExecutorType.Activate, 69272449, Step1_WhiteRabbit_SetTrapOnSummon);
		AddExecutor(ExecutorType.Activate, 20938824, Step1_MH_FromHand);
		AddExecutor(ExecutorType.Activate, 20938824, returnFromBanish);
		AddExecutor(ExecutorType.SpSummon, 24842059, LinguribohMHLine);
		AddExecutor(ExecutorType.SpSummon, 30342076, Step1_SSLinkDecoder);
		AddExecutor(ExecutorType.SpSummon, 68059897, Step2N_LinkSummon_RedRansom);
		AddExecutor(ExecutorType.SpSummon, 68059897, Step2_LinkSummon_RedRansom);
		AddExecutor(ExecutorType.Activate, 68059897, Step2_RedRansom_Search);
		AddExecutor(ExecutorType.Activate, 96676583, AnyDraw);
		AddExecutor(ExecutorType.SpSummon, 4280258, Link_Apo);
		AddExecutor(ExecutorType.SpSummon, 52698008, Step2N_RRtoWicckid);
		AddExecutor(ExecutorType.SpSummon, 30342076, Step_SummonLinkDecoderToWicckid);
		AddExecutor(ExecutorType.Activate, 52698008, Wicckid_SearchTuner);
		AddExecutor(ExecutorType.SpSummon, 59859086, Step_SplashToWB);
		AddExecutor(ExecutorType.Activate, 59859086, Step2N_SplashMage_ReviveP);
		AddExecutor(ExecutorType.Activate, 30118811, Flow3_BackupIgnister_AfterMakeIt3);
		AddExecutor(ExecutorType.Activate, 30118811, OneBody_Backup_SearchWizard);
		AddExecutor(ExecutorType.SpSummon, 95454996, Step2N_LinkSummon_WB);
		AddExecutor(ExecutorType.Activate, 95454996, WB_OnSummon_BanishGY);
		AddExecutor(ExecutorType.Activate, 20726052, GWC06_MyTurn_Extend);
		AddExecutor(ExecutorType.Activate, 95454996, WB_SetMalissTrap);
		AddExecutor(ExecutorType.Activate, 3723262, Step2_Fallback_Wizard_AfterSplashNegated);
		AddExecutor(ExecutorType.Activate, 30118811, Step2_Fallback_Backup_AfterSplashNegated);
		AddExecutor(ExecutorType.Activate, 3723262, Flow3_WizardIgnister_AfterMakeIt3);
		AddExecutor(ExecutorType.Activate, 95454996, WB_OnBanished_SelfSS);
		AddExecutor(ExecutorType.Activate, 20938824, ssFromHandMH);
		AddExecutor(ExecutorType.SpSummon, 21848500, Step_LinkSummon_HeartsCrypter);
		AddExecutor(ExecutorType.SpSummon, 39138610, Flow3_Link_Allied);
		AddExecutor(ExecutorType.Activate, 39138610, Allied_OnSummonTrigger);
		AddExecutor(ExecutorType.Activate, 94722358, MTP07_ForMH);
		AddExecutor(ExecutorType.SpSummon, 5043010, Flow3_Link_Firewall);
		AddExecutor(ExecutorType.SpSummon, 95454996, Step_WicckidPlusOneToWB);
		AddExecutor(ExecutorType.Activate, 96676583, ChessyCat_SS_FromBanished);
		AddExecutor(ExecutorType.SpSummon, 52698008, Step_RRtoWicckid);
		AddExecutor(ExecutorType.Activate, 68337209, Flow3_UnderGround_Available_SSAnyPawn);
		AddExecutor(ExecutorType.Activate, 30342076, LinkDecoder_ReviveFromGY);
		AddExecutor(ExecutorType.SpSummon, 46947713, SummonTranscode);
		AddExecutor(ExecutorType.Activate, 46947713, Transcode_ReviveLink3OrLower);
		AddExecutor(ExecutorType.Activate, 73628505, Terra_GrabUnderground);
		AddExecutor(ExecutorType.Activate, 75500286, GoldSarc_StartPiece);
		AddExecutor(ExecutorType.Activate, 68337209, Underground_ActivateStarter);
		AddExecutor(ExecutorType.SpSummon, 59859086, Step_SplashToRR);
		AddExecutor(ExecutorType.Activate, 59859086, Step2_SplashMage_ReviveP);
		AddExecutor(ExecutorType.SpSummon, 98978921);
		AddExecutor(ExecutorType.Summon, 14558127, Emergency_NS);
		AddExecutor(ExecutorType.Summon, 23434538, Emergency_NS);
		AddExecutor(ExecutorType.SpSummon, 24842059, OneBody_Link1_Linguriboh);
		AddExecutor(ExecutorType.SpSummon, 60303245, OneBody_Link1_Almiraj);
		AddExecutor(ExecutorType.Activate, 30118811, OneBody_Backup_SS);
		AddExecutor(ExecutorType.Activate, 30118811, OneBody_Backup_SearchWizard);
		AddExecutor(ExecutorType.Activate, 3723262, OneBody_Wizard_SS);
		AddExecutor(ExecutorType.SpSummon, 86066372, Flow3_Link_Accesscode);
		AddExecutor(ExecutorType.SpSummon, 24842059, T3Allow);
		AddExecutor(ExecutorType.SpSummon, 60303245, T3Allow);
		AddExecutor(ExecutorType.SpSummon, 46947713, EmerTranscode);
		AddExecutor(ExecutorType.SpSummon, 39138610, Emer_Allied);
		AddExecutor(ExecutorType.SpSummon, 39138610, Emer_Allied2);
		AddExecutor(ExecutorType.SpSummon, 29301450, SummonLittleKnightFast);
		AddExecutor(ExecutorType.SpSummon, 29301450, SPEmer);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
	}

	private static bool IsEmzSeq(int seq)
	{
		return seq >= 5;
	}

	private static int BitOfSeq(int seq)
	{
		return 1 << seq;
	}

	private static int LowestBit(int m)
	{
		return m & -m;
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

	private bool IsPawnId(int id)
	{
		if (id != 32061192 && id != 69272449 && id != 96676583)
		{
			return id == 20938824;
		}
		return true;
	}

	private int GetQueenLinkedMMZMask()
	{
		int num = 0;
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			if (monster != null && monster.IsFaceup() && (monster.IsCode(68059897) || monster.IsCode(95454996) || monster.IsCode(21848500)))
			{
				num |= monster.GetLinkedZones();
			}
		}
		return num & 0x1F;
	}

	private int LinkValOf(ClientCard c)
	{
		if (!c.HasType(CardType.Link))
		{
			return 1;
		}
		return Math.Max(1, c.LinkCount);
	}

	private bool IsOneVal(ClientCard c)
	{
		if (c.HasType(CardType.Link))
		{
			return Math.Max(1, c.LinkCount) == 1;
		}
		return true;
	}

	private bool IsMaliss(ClientCard c)
	{
		return c.HasSetcode(447);
	}

	private int ScoreForBanishedMaliss(ClientCard c)
	{
		if (c.IsCode(93453053))
		{
			return 100;
		}
		if (c.IsCode(94722358))
		{
			return 95;
		}
		if (c.IsCode(20726052))
		{
			return 90;
		}
		if (c.IsCode(68337209))
		{
			return 85;
		}
		if (c.IsCode(20938824))
		{
			return 80;
		}
		if (c.IsCode(96676583))
		{
			return 75;
		}
		if (c.IsCode(69272449))
		{
			return 70;
		}
		if (c.IsCode(32061192))
		{
			return 65;
		}
		return 50;
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone)
		{
			int num = 31;
			if (IsPawnId(cardId))
			{
				int num2 = GetQueenLinkedMMZMask() & available & num;
				if (num2 != 0)
				{
					int num3 = FirstBitFromOrder(num2, new int[5] { 4, 2, 8, 1, 16 });
					base.AI.SelectPlace(num3);
					return num3;
				}
			}
			switch (cardId)
			{
			case 39138610:
			{
				ClientCard firstMatchingCard = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(5043010) ?? false);
				int num7 = available & 0x60;
				if (firstMatchingCard != null && firstMatchingCard.IsFaceup())
				{
					bool flag = firstMatchingCard.Controller == 0 && firstMatchingCard.Location == CardLocation.MonsterZone && firstMatchingCard.Sequence == 2;
					int num8 = firstMatchingCard.GetLinkedZones() & available;
					int num9 = num8 & 0x60;
					if (num9 != 0)
					{
						int num12;
						if (flag && (num9 & 0x20) != 0 && (num9 & 0x40) != 0)
						{
							int num10 = 0;
							if ((available & 1) != 0)
							{
								num10++;
							}
							if ((available & 2) != 0)
							{
								num10++;
							}
							int num11 = 0;
							if ((available & 8) != 0)
							{
								num11++;
							}
							if ((available & 0x10) != 0)
							{
								num11++;
							}
							num12 = ((num10 > num11) ? 32 : ((num11 <= num10) ? FirstBitFromOrder(num9, new int[2] { 32, 64 }) : 64));
						}
						else
						{
							num12 = FirstBitFromOrder(num9, new int[2] { 32, 64 });
						}
						base.AI.SelectPlace(num12);
						return num12;
					}
					if (num7 != 0)
					{
						int num12;
						if (flag && (num7 & 0x20) != 0 && (num7 & 0x40) != 0)
						{
							int num13 = 0;
							if ((available & 1) != 0)
							{
								num13++;
							}
							if ((available & 2) != 0)
							{
								num13++;
							}
							int num14 = 0;
							if ((available & 8) != 0)
							{
								num14++;
							}
							if ((available & 0x10) != 0)
							{
								num14++;
							}
							num12 = ((num13 > num14) ? 32 : ((num14 <= num13) ? FirstBitFromOrder(num7, new int[2] { 32, 64 }) : 64));
						}
						else
						{
							num12 = FirstBitFromOrder(num7, new int[2] { 32, 64 });
						}
						base.AI.SelectPlace(num12);
						return num12;
					}
					if (num8 != 0)
					{
						int num12 = FirstBitFromOrder(num8, new int[5] { 4, 2, 8, 1, 16 });
						base.AI.SelectPlace(num12);
						return num12;
					}
				}
				int num15 = available & 0x60;
				if (num15 != 0)
				{
					int num16 = FirstBitFromOrder(num15, new int[2] { 32, 64 });
					base.AI.SelectPlace(num16);
					return num16;
				}
				return PreferSafeSummonZones(available);
			}
			case 52698008:
			{
				int num17 = ChooseAndRememberWicckidEmz(available);
				if (num17 != 0)
				{
					return num17;
				}
				return 0;
			}
			case 46947713:
			{
				int num18 = ((_forceTranscodeBit != 0) ? _forceTranscodeBit : _wicckidEmzBit);
				if (num18 != 0 && (available & num18) != 0)
				{
					return num18;
				}
				int num19 = available & 0x60;
				if (num19 != 0)
				{
					if ((num19 & 0x20) == 0)
					{
						return 64;
					}
					return 32;
				}
				return 0;
			}
			case 68059897:
				if (_rrSelfSSPlacing)
				{
					int num4 = 10;
					int linkedMaskFor = GetLinkedMaskFor(GetWicckid());
					int num5 = available & num4 & ~linkedMaskFor;
					if (num5 != 0)
					{
						int num6 = FirstBitFromOrder(num5, new int[2] { 2, 8 });
						base.AI.SelectPlace(num6);
						_rrSelfSSPlacing = false;
						return num6;
					}
				}
				break;
			}
			if (cardId == 30342076)
			{
				ClientCard firstMatchingCard2 = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(46947713) ?? false);
				int num20 = GetLinkedMaskFor(firstMatchingCard2) & 0x1F;
				int num21 = available & 0x1F & ~num20;
				if (num21 != 0)
				{
					int num22 = FirstBitFromOrder(num21, new int[5] { 4, 2, 8, 1, 16 });
					base.AI.SelectPlace(num22);
					return num22;
				}
			}
			if (cardId == 5043010)
			{
				ClientCard firstMatchingCard3 = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(46947713) ?? false);
				int num23 = 0;
				if (firstMatchingCard3 != null)
				{
					int num24 = GetLinkedMaskFor(firstMatchingCard3) & 0x1F;
					if ((num24 & 2) != 0)
					{
						num23 |= 2;
					}
					if ((num24 & 8) != 0)
					{
						num23 |= 8;
					}
				}
				int num25 = available & num23;
				if (num25 != 0)
				{
					int num26 = FirstBitFromOrder(num25, new int[2] { 2, 8 });
					base.AI.SelectPlace(num26);
					return num26;
				}
			}
			if (cardId == 46947713 || cardId == 86066372 || cardId == 39138610 || cardId == 95454996 || cardId == 21848500)
			{
				return PreferSafeSummonZones(available);
			}
			int num27 = GetMyLinkedMMZMask() & available & 0x1F;
			int num28 = available & 0x1F & ~num27;
			int num29 = ((avoidLinkedZones && num28 != 0) ? LowestBit(num28) : ((num27 == 0) ? LowestBit(available & 0x1F) : LowestBit(num27)));
			base.AI.SelectPlace(num29);
			return num29;
		}
		SelectSTPlace(base.Card, avoidImpermanence: true);
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

	public bool AshBlossomActivate()
	{
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard().IsCode(23434538) && CheckAtAdvantage() && base.Duel.Turn > 1)
		{
			return false;
		}
		return DefaultAshBlossomAndJoyousSpring();
	}

	public bool MaxxCActivate()
	{
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
				if (base.Util.GetLastChainCard().IsCode(23434538) && CheckAtAdvantage() && base.Duel.Turn > 1)
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
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (new List<int>().Contains(base.Card.Id) && base.Bot.HasInSpellZone(base.Card.Id))
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
			if (base.Card.IsCode(94722358))
			{
				mtp07SetThisTurn = true;
			}
			if (base.Card.IsCode(20726052))
			{
				gwc06SetThisTurn = true;
			}
			return true;
		}
		return false;
	}

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219) || card.HasSetcode(413) || card.HasSetcode(6) || card.HasSetcode(277))).ToList();
		List<int> dangerMonsterIdList = new List<int> { 99937011, 63542003, 9411399, 28954097, 30680659, 32731036 };
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

	private int LinkVal(ClientCard c)
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

	private bool HasFreeEMZ()
	{
		ClientCard[] monsterZone = base.Bot.MonsterZone;
		bool num = monsterZone.Length > 5 && monsterZone[5] == null;
		bool flag = monsterZone.Length > 6 && monsterZone[6] == null;
		return num || flag;
	}

	public override void OnChainSolved(int chainIndex)
	{
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		base.Duel.GetCurrentSolvingChainCard();
		base.Duel.IsCurrentSolvingChainNegated();
		if (currentSolvingChainCard != null && !base.Duel.IsCurrentSolvingChainNegated() && currentSolvingChainCard.Controller == 1)
		{
			if (currentSolvingChainCard.IsCode(34267821))
			{
				enemyActivateLancea = true;
			}
			if (currentSolvingChainCard.IsCode(23434538))
			{
				enemyActivateMaxxC = true;
			}
			if (currentSolvingChainCard.IsCode(42141493))
			{
				enemyActivateFuwalos = true;
			}
			if (currentSolvingChainCard.IsCode(94145021))
			{
				enemyActivateLockBird = true;
			}
			if (currentSolvingChainCard.IsCode(10045474))
			{
				for (int i = 0; i < 5; i++)
				{
					if (base.Enemy.SpellZone[i] == currentSolvingChainCard)
					{
						infiniteImpermanenceList.Add(4 - i);
						break;
					}
				}
			}
			ClientCard currentSolvingChainCard2 = base.Duel.GetCurrentSolvingChainCard();
			if (currentSolvingChainCard2 != null)
			{
				if (currentSolvingChainCard2.IsSpell() && (currentSolvingChainCard2.HasType(CardType.Field) || currentSolvingChainCard2.HasType(CardType.Continuous) || currentSolvingChainCard2.HasType(CardType.Equip)))
				{
					_oppJustActivatedPersistentSpell = true;
				}
				_prefWindowTTL = Math.Max(_prefWindowTTL, 2);
			}
		}
		if (currentSolvingChainCard != null && currentSolvingChainCard.Controller == 0 && currentSolvingChainCard.IsCode(59859086) && base.Duel.IsCurrentSolvingChainNegated())
		{
			splashNegatedThisTurn = true;
		}
	}

	public override void OnChainEnd()
	{
		escapeTargetList.Clear();
		currentNegateCardList.Clear();
		currentDestroyCardList.Clear();
		enemyActivateInfiniteImpermanenceFromHand = false;
		_oppJustActivatedPersistentSpell = false;
		int monsterCount = base.Enemy.GetMonsterCount();
		if (monsterCount > _enemyMonsterCountSnap)
		{
			_oppJustSummoned = true;
		}
		_enemyMonsterCountSnap = monsterCount;
		int num = base.Enemy.SpellZone.Count((ClientCard c) => c?.IsFacedown() ?? false);
		if (num > _enemyFacedownSTSnap)
		{
			_oppJustSet = true;
		}
		_enemyFacedownSTSnap = num;
		for (int num2 = enemyPlaceThisTurn.Count - 1; num2 >= 0; num2--)
		{
			ClientCard clientCard = enemyPlaceThisTurn[num2];
			if (clientCard == null || (clientCard.Location != CardLocation.SpellZone && clientCard.Location != CardLocation.MonsterZone))
			{
				enemyPlaceThisTurn.RemoveAt(num2);
			}
		}
		base.OnChainEnd();
	}

	public override void OnNewTurn()
	{
		if (base.Duel.Player == 0)
		{
			myTurnCount++;
		}
		enemyActivateLancea = false;
		enemyActivateFuwalos = false;
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
		usedNormalSummon = false;
		ssChessyCat = false;
		ssDormouse = false;
		ssMarchHare = false;
		ssWhiteRabbit = false;
		ActiveMarchHare = false;
		ActiveUnderground = false;
		step1Done = false;
		step2Done = false;
		lastRevivedIdBySplash = 0;
		mtp07SetThisTurn = false;
		gwc06SetThisTurn = false;
		splashNegatedThisTurn = false;
		ssRRThisTurn = false;
		ssWBThisTurn = false;
		ssHCThisTurn = false;
		_didSplashToRR = (_didRRtoWicckid = (_didSummonToWicckidArrow = (_didWBFromWicckid = false)));
		_finishPlanDecided = false;
		_preferWicckidArrows = false;
		_rrSelfSSPlacing = false;
		_forceTranscodeBit = 0;
		_oppJustActivatedPersistentSpell = false;
		_oppJustSummoned = false;
		_oppJustSet = false;
		_enemyMonsterCountSnap = base.Enemy.GetMonsterCount();
		_enemyFacedownSTSnap = base.Enemy.SpellZone.Count((ClientCard c) => c?.IsFacedown() ?? false);
		_prefWindowTTL = 0;
		fullBoard1 = false;
		Allied_End = false;
		nsplan = false;
		nsBackupplan = false;
		NSDorMouse = false;
		nsLanceaplan = false;
		base.OnNewTurn();
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == base.Util.GetStringId(95454996, 0))
		{
			if (base.Bot.Graveyard.Count <= 0)
			{
				return base.Enemy.Graveyard.Count > 0;
			}
			return true;
		}
		return base.OnSelectYesNo(desc);
	}

	private bool DontSelfNG()
	{
		return base.Duel.LastChainPlayer != 0;
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
		if (cards != null && cards.Count > 0 && currentSolvingChainCard != null)
		{
			if (currentSolvingChainCard.IsCode(68059897))
			{
				List<ClientCard> list = cards.Where((ClientCard c) => c != null && (c.IsCode(68337209) || c.IsCode(93453053))).ToList();
				if (list.Count > 0)
				{
					bool num = ShouldSearchUnderground();
					int chooseId = 0;
					if (num && list.Any((ClientCard c) => c.IsCode(68337209)))
					{
						chooseId = 68337209;
					}
					else if (list.Any((ClientCard c) => c.IsCode(93453053)))
					{
						chooseId = 93453053;
					}
					if (chooseId != 0)
					{
						ClientCard item = list.First((ClientCard c) => c.IsCode(chooseId));
						return new List<ClientCard> { item };
					}
				}
			}
			if (hint == 510 && currentSolvingChainCard.IsCode(95454996))
			{
				ClientCard clientCard = null;
				clientCard = cards.FirstOrDefault((ClientCard c) => c.Id == 20726052 && c.Location == CardLocation.Deck);
				if (clientCard == null)
				{
					clientCard = cards.FirstOrDefault((ClientCard c) => c.Id == 20726052);
				}
				if (clientCard == null)
				{
					clientCard = cards.FirstOrDefault((ClientCard c) => c.Id == 94722358);
				}
				if (clientCard != null)
				{
					if (clientCard.Id == 20726052)
					{
						gwc06SetThisTurn = true;
					}
					else if (clientCard.Id == 94722358)
					{
						mtp07SetThisTurn = true;
					}
					return new List<ClientCard> { clientCard };
				}
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	private int GetMMZCount()
	{
		return base.Bot.MonsterZone.Take(5).Count((ClientCard c) => c != null);
	}

	private bool HasFreeMMZ()
	{
		return GetMMZCount() < 5;
	}

	private bool HaveTwoBodies()
	{
		return base.Bot.GetMonsterCount() >= 2;
	}

	private bool ShouldFastEndToSPLK()
	{
		if (!enemyActivateMaxxC)
		{
			return enemyActivateFuwalos;
		}
		return true;
	}

	private bool Step1Complete()
	{
		if (base.Bot.HasInMonstersZone(32061192))
		{
			return base.Bot.HasInMonstersZone(69272449);
		}
		return false;
	}

	private bool CanStartStep1()
	{
		if (enemyActivateLancea || ShouldFastEndToSPLK())
		{
			return false;
		}
		if (HaveTwoBodies())
		{
			return false;
		}
		if (!HasFreeMMZ())
		{
			return false;
		}
		if (!base.Bot.HasInHand(32061192) && !base.Bot.HasInHand(69272449) && !base.Bot.HasInHand(75500286) && !base.Bot.HasInHand(73628505))
		{
			return base.Bot.HasInHand(68337209);
		}
		return true;
	}

	private bool CanContinueStep1()
	{
		if (enemyActivateLancea)
		{
			return false;
		}
		if (!HaveTwoBodies())
		{
			return HasFreeMMZ();
		}
		return false;
	}

	private int PickMalissTrapToSet()
	{
		int num = ((base.Duel.Player == 0 && base.Bot.HasInMonstersZone(32061192) && base.Bot.HasInMonstersZone(69272449)) ? 20726052 : ((base.Duel.Player == 0 && (base.Bot.HasInMonstersZoneOrInGraveyard(68059897) || base.Bot.HasInBanished(68059897))) ? 20726052 : ((base.Duel.Player == 0 && base.Bot.HasInHand(20938824) && !ActiveMarchHare && !ssWhiteRabbit) ? 20726052 : ((base.Duel.Player == 0 && nsBackupplan) ? 20726052 : ((base.Duel.Player != 1) ? 94722358 : 94722358)))));
		if (CheckRemainInDeck(num) > 0)
		{
			return num;
		}
		return 0;
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

	private bool Step1_Dormouse_NormalSummon()
	{
		if (!CanStartStep1())
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		usedNormalSummon = true;
		NSDorMouse = true;
		return true;
	}

	private bool Dormouse_ForMH()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!CanContinueStep1())
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		int num = 0;
		num = ((!goldstart && !undergroundstart) ? ((CheckRemainInDeck(20938824) > 0) ? 20938824 : ((CheckRemainInDeck(69272449) > 0) ? 69272449 : 96676583)) : ((CheckRemainInDeck(69272449) > 0) ? 69272449 : 96676583));
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		return true;
	}

	private bool Step1_WhiteRabbit_SS_FromBanished()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Card.Id != 69272449)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 300)
		{
			return false;
		}
		ssWhiteRabbit = true;
		return true;
	}

	private bool Step1_WhiteRabbit_SetTrapOnSummon()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 69272449)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		int num = PickMalissTrapToSet();
		switch (num)
		{
		case 0:
			return false;
		case 20726052:
			gwc06SetThisTurn = true;
			break;
		}
		if (num == 94722358)
		{
			mtp07SetThisTurn = true;
		}
		base.AI.SelectCard(num);
		SelectSafeSTZoneAwayFromImperm();
		if (Step1Complete())
		{
			step1Done = true;
		}
		return true;
	}

	private bool Step1_WhiteRabbit_NormalSummon()
	{
		if (!CanStartStep1())
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192))
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		usedNormalSummon = true;
		return true;
	}

	private bool Dormouse_SS_FromBanished()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 32061192)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 300)
		{
			return false;
		}
		ssDormouse = true;
		return true;
	}

	private bool ChessyCat_SS_FromBanished()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 96676583)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 300)
		{
			return false;
		}
		ssChessyCat = true;
		return true;
	}

	private bool SummonLittleKnightFast()
	{
		if (!base.Bot.HasInMonstersZone(98978921) && !base.Bot.HasInMonstersZone(24842059))
		{
			return false;
		}
		if (!HaveTwoBodies())
		{
			return false;
		}
		List<ClientCard> list = (from c in base.Bot.GetMonsters()
			where c != null && c.IsFaceup() && c.HasType(CardType.Effect)
			orderby c.Attack
			select c).Take(2).ToList();
		if (list.Count < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		step1Done = true;
		return true;
	}

	private bool SPEmer()
	{
		if (base.Bot.HasInMonstersZone(5043010) || base.Bot.HasInMonstersZone(39138610) || base.Bot.HasInMonstersZone(4280258) || base.Bot.HasInMonstersZone(86066372))
		{
			return false;
		}
		if (!HaveTwoBodies())
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(2, (ClientCard m) => m.HasType(CardType.Effect), 2, 2, new int[3] { 4280258, 39138610, 86066372 });
		if (list.Count != 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool GoldSarc_StartPiece()
	{
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449))
		{
			return false;
		}
		int num = 0;
		if (!base.Bot.HasInMonstersZone(32061192) && CheckRemainInDeck(32061192) > 0 && !ssDormouse)
		{
			num = 32061192;
		}
		else if (!base.Bot.HasInMonstersZone(69272449) && CheckRemainInDeck(69272449) > 0 && !ssWhiteRabbit)
		{
			num = 69272449;
		}
		else
		{
			if (!madeIt3 || ssChessyCat)
			{
				return false;
			}
			num = 96676583;
		}
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		if (base.Card.Location == CardLocation.Hand)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
		}
		goldstart = true;
		return true;
	}

	private bool ExistsForUnderground(int id)
	{
		if (CheckRemainInDeck(id) <= 0 && !base.Bot.HasInHand(id))
		{
			return base.Bot.HasInGraveyard(id);
		}
		return true;
	}

	private bool Underground_ActivateStarter()
	{
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (step1Done)
		{
			return false;
		}
		int num = 0;
		if (ExistsForUnderground(32061192))
		{
			num = 32061192;
		}
		else if (ExistsForUnderground(69272449))
		{
			num = 69272449;
		}
		else if (ExistsForUnderground(96676583))
		{
			num = 96676583;
		}
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectYesNo(opt: true);
		base.AI.SelectCard(num);
		ActiveUnderground = true;
		undergroundstart = true;
		return true;
	}

	private bool Terra_GrabUnderground()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (ActiveUnderground)
		{
			return false;
		}
		if (base.Bot.HasInHand(68337209) || base.Bot.HasInSpellZone(68337209))
		{
			return false;
		}
		base.AI.SelectCard(68337209);
		if (base.Card.Location == CardLocation.Hand)
		{
			SelectSTPlace(base.Card, avoidImpermanence: true);
		}
		return true;
	}

	private bool HaveBackupOrWizardInHand()
	{
		if (!base.Bot.HasInHand(30118811))
		{
			return base.Bot.HasInHand(3723262);
		}
		return true;
	}

	private bool HaveMHInHand()
	{
		return base.Bot.HasInHand(20938824);
	}

	private bool IsMalissBody(ClientCard c)
	{
		if (c != null && c.IsFaceup() && c.HasSetcode(447))
		{
			return c.IsCode(32061192, 69272449, 96676583, 20938824);
		}
		return false;
	}

	private bool Emergency_NormalCat()
	{
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(68337209) || base.Bot.HasInHand(73628505) || base.Bot.HasInHand(75500286))
		{
			return false;
		}
		if (!HaveMHInHand())
		{
			return false;
		}
		usedNormalSummon = true;
		return true;
	}

	private bool OneBody_Link1_Linguriboh()
	{
		if (!HaveBackupOrWizardInHand())
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(24842059))
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 1)
		{
			return false;
		}
		return true;
	}

	private bool OneBody_Link1_Almiraj()
	{
		if (!HaveBackupOrWizardInHand())
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(60303245))
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 1)
		{
			return false;
		}
		return true;
	}

	private bool OneBody_Backup_SS()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(24842059) && !base.Bot.HasInMonstersZone(59859086))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		avoidLinkedZones = true;
		return true;
	}

	private bool OneBody_Backup_SearchWizard()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!base.Card.IsCode(30118811))
		{
			return false;
		}
		if (base.Bot.Hand.Count == 0)
		{
			return false;
		}
		bool flag = base.Bot.HasInHand(3723262);
		int searchId = 0;
		if (flag && CheckRemainInDeck(20938824) > 0 && base.Bot.Hand.Count > 0)
		{
			searchId = 20938824;
		}
		else if (CheckRemainInDeck(32061192) > 0 && nsplan && base.Bot.HasInMonstersZone(60303245))
		{
			searchId = 32061192;
		}
		else if (CheckRemainInDeck(69272449) > 0 && nsBackupplan)
		{
			searchId = 69272449;
		}
		else if (!flag && CheckRemainInDeck(3723262) > 0 && base.Bot.Hand.Count > 0)
		{
			searchId = 3723262;
		}
		else
		{
			if (CheckRemainInDeck(69272449) <= 0)
			{
				return false;
			}
			searchId = 69272449;
		}
		base.AI.SelectCard(searchId);
		List<ClientCard> list = base.Bot.Hand.Where((ClientCard h) => h != null).ToList();
		List<ClientCard> candidates = list.Where((ClientCard h) => h.Id != 3723262).ToList();
		List<ClientCard> list2 = new List<ClientCard>(list.Count);
		if (searchId == 32061192)
		{
			list2.AddRange(candidates.Where((ClientCard c) => c.Id == 32061192));
		}
		List<ClientCard> source = ((Func<IEnumerable<ClientCard>>)(() => (searchId == 20938824) ? candidates.Where((ClientCard c) => c.Id != 20938824) : candidates))().ToList();
		IEnumerable<IGrouping<int, ClientCard>> dupGroups = from c in source
			group c by c.Id into g
			where g.Count() >= 2
			select g;
		list2.AddRange(dupGroups.SelectMany((IGrouping<int, ClientCard> g) => g));
		int[] array = new int[4] { 27204311, 34267821, 73628505, 75500286 };
		foreach (int id in array)
		{
			list2.AddRange(source.Where((ClientCard c) => c.Id == id && dupGroups.All((IGrouping<int, ClientCard> g) => g.Key != id)));
		}
		HashSet<ClientCard> already = new HashSet<ClientCard>(list2);
		list2.AddRange(source.Where((ClientCard c) => !already.Contains(c)));
		list2 = list2.Where((ClientCard c) => c != null).Distinct().ToList();
		if (searchId == 32061192)
		{
			base.AI.SelectNextCard(searchId);
		}
		if (searchId == 69272449)
		{
			base.AI.SelectNextCard(searchId);
		}
		else
		{
			if (list2 == null)
			{
				return false;
			}
			base.AI.SelectNextCard(list2);
		}
		avoidLinkedZones = true;
		if (base.Bot.HasInMonstersZone(24842059))
		{
			blockWicckid = true;
		}
		if (GetMMZCount() >= 5 && base.Bot.HasInHand(3723262))
		{
			fullBoard1 = true;
		}
		return true;
	}

	private bool OneBody_Wizard_SS()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(24842059) && !base.Bot.HasInMonstersZone(60303245))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		ClientCard clientCard = PickGYCybersePriority();
		if (clientCard == null)
		{
			return false;
		}
		avoidLinkedZones = true;
		blockWicckid = true;
		base.AI.SelectCard(clientCard);
		return true;
	}

	private ClientCard PickGYCybersePriority()
	{
		ClientCard clientCard = PickGYMalissPriority();
		if (clientCard != null)
		{
			return clientCard;
		}
		return base.Bot.Graveyard.GetMatchingCards((ClientCard c) => c != null && c.IsMonster() && c.HasRace(CardRace.Cyberse) && c.Level <= 4).ToList().FirstOrDefault();
	}

	private ClientCard PickGYMalissPriority()
	{
		int[] array = new int[4] { 32061192, 69272449, 96676583, 20938824 };
		foreach (int id in array)
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(id) && g.IsMonster());
			if (firstMatchingCard != null)
			{
				return firstMatchingCard;
			}
		}
		return null;
	}

	private bool TwoCC_NormalSummon()
	{
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(75500286) || base.Bot.HasInHand(68337209))
		{
			return false;
		}
		if (base.Bot.Hand.GetMatchingCards((ClientCard c) => c?.IsCode(96676583) ?? false).Count >= 2)
		{
			usedNormalSummon = true;
			return true;
		}
		return false;
	}

	private bool IsMalissCost(ClientCard card)
	{
		if (card == null)
		{
			return false;
		}
		if (card.IsCode(96676583))
		{
			return true;
		}
		if (card.IsCode(69272449) && NSDorMouse && !ssWhiteRabbit)
		{
			return true;
		}
		if (card.IsCode(69272449) && ssWhiteRabbit)
		{
			return true;
		}
		if (card.IsCode(32061192) && ssDormouse)
		{
			return true;
		}
		if (card.IsCode(20938824) && ActiveMarchHare)
		{
			return true;
		}
		return false;
	}

	private bool IsMalissCost2(ClientCard card)
	{
		if (card == null)
		{
			return false;
		}
		if (!card.IsCode(96676583) && !card.IsCode(69272449) && !card.IsCode(20938824))
		{
			return card.IsCode(68337209);
		}
		return true;
	}

	private bool AnyDraw()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 96676583)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard clientCard = null;
		clientCard = base.Bot.Hand.FirstOrDefault((ClientCard c) => c?.IsCode(93453053) ?? false);
		if (clientCard == null)
		{
			clientCard = base.Bot.Hand.FirstOrDefault(IsMalissCost);
		}
		if (clientCard == null)
		{
			IGrouping<int, ClientCard> grouping = (from c in base.Bot.Hand.Where(IsMalissCost2)
				group c by c.Id).FirstOrDefault((IGrouping<int, ClientCard> g) => g.Count() >= 2);
			if (grouping != null)
			{
				clientCard = grouping.First();
			}
		}
		if (clientCard == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		return true;
	}

	private int PickTB11CostCandidateId()
	{
		List<ClientCard> source = (from c in base.Bot.GetMonsters()
			where c != null && c.IsFaceup() && c.HasSetcode(447)
			select c).ToList();
		source = source.Where((ClientCard c) => (!c.IsCode(69272449) || !ssWhiteRabbit) && (!c.IsCode(96676583) || !ssChessyCat) && (!c.IsCode(20938824) || !ssMarchHare) && (!c.IsCode(32061192) || !ssDormouse) && (!c.IsCode(68059897) || !ssRRThisTurn) && (!c.IsCode(95454996) || !ssWBThisTurn) && (!c.IsCode(21848500) || !ssHCThisTurn)).ToList();
		int[] array = new int[7] { 69272449, 96676583, 20938824, 32061192, 68059897, 95454996, 21848500 };
		foreach (int id in array)
		{
			if (source.Any((ClientCard c) => c.IsCode(id)))
			{
				return id;
			}
		}
		return 0;
	}

	private int PickPFromGYForSplash()
	{
		int[] array;
		if (enemyActivateLancea)
		{
			array = new int[6] { 32061192, 69272449, 96676583, 20938824, 30118811, 3723262 };
			foreach (int num in array)
			{
				if (base.Bot.HasInGraveyard(num))
				{
					return num;
				}
			}
		}
		array = new int[4] { 32061192, 69272449, 96676583, 20938824 };
		foreach (int num2 in array)
		{
			if (base.Bot.HasInGraveyard(num2))
			{
				return num2;
			}
		}
		return 0;
	}

	private bool HaveUndergroundOnHandOrField()
	{
		if (!base.Bot.HasInHand(68337209))
		{
			return base.Bot.HasInSpellZone(68337209);
		}
		return true;
	}

	private bool ShouldSearchUnderground()
	{
		if (ActiveUnderground)
		{
			return false;
		}
		if (HaveUndergroundOnHandOrField())
		{
			return false;
		}
		return CheckRemainInDeck(68337209) > 0;
	}

	private bool Step2_RedRansom_Search()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		int num = 0;
		if (ShouldSearchUnderground())
		{
			num = 68337209;
		}
		else if (CheckRemainInDeck(93453053) > 0)
		{
			num = 93453053;
		}
		if (num == 0)
		{
			return false;
		}
		step2Done = true;
		avoidLinkedZones = false;
		coreSetupComplete = true;
		return true;
	}

	private List<ClientCard> PickMaterialsForRedRansom()
	{
		ClientCard splash = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(59859086));
		if (splash == null)
		{
			return new List<ClientCard>();
		}
		ClientCard clientCard = null;
		if (lastRevivedIdBySplash != 0)
		{
			clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.IsCode(lastRevivedIdBySplash));
		}
		if (clientCard == null)
		{
			clientCard = base.Bot.MonsterZone.FirstOrDefault((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(447) && c != splash && !c.HasType(CardType.Link));
		}
		if (clientCard == null)
		{
			return new List<ClientCard>();
		}
		return new List<ClientCard> { splash, clientCard };
	}

	private bool Step2_SplashMage_ReviveP()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (step2Done)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(59859086))
		{
			return false;
		}
		int num = PickPFromGYForSplash();
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		lastRevivedIdBySplash = num;
		return true;
	}

	private bool Step2N_SplashMage_ReviveP()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(59859086))
		{
			return false;
		}
		int num = PickPFromGYForSplash();
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		lastRevivedIdBySplash = num;
		return true;
	}

	private bool Step2_LinkSummon_RedRansom()
	{
		if (step2Done)
		{
			return false;
		}
		List<ClientCard> list = PickMaterialsForRedRansom();
		if (list.Count != 2)
		{
			return false;
		}
		if (base.Util.GetBotAvailZonesFromExtraDeck(list) == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		madeIt3 = true;
		return true;
	}

	private bool Step2N_LinkSummon_WB()
	{
		List<ClientCard> list = PickMaterialsForRedRansom();
		if (list.Count != 2)
		{
			return false;
		}
		if (base.Util.GetBotAvailZonesFromExtraDeck(list) == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool Step2N_LinkSummon_RedRansom()
	{
		if (!base.Bot.HasInMonstersZone(20938824) || base.Bot.GetMonsterCount() < 3)
		{
			return false;
		}
		madeIt3 = true;
		return true;
	}

	private bool Step2_Fallback_Wizard_AfterSplashNegated()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!splashNegatedThisTurn)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		ClientCard clientCard = PickGYMalissPriority();
		if (clientCard == null)
		{
			return false;
		}
		avoidLinkedZones = true;
		blockWicckid = true;
		base.AI.SelectCard(clientCard);
		return true;
	}

	private bool Step2_Fallback_Backup_AfterSplashNegated()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!splashNegatedThisTurn)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		int num = ((!base.Bot.HasInHand(3723262) && CheckRemainInDeck(3723262) > 0) ? 3723262 : ((CheckRemainInDeck(20938824) > 0) ? 20938824 : 0));
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		ClientCard clientCard = base.Bot.Hand.FirstOrDefault((ClientCard h) => h != null && h.Id != 3723262 && h != base.Card);
		if (clientCard != null)
		{
			base.AI.SelectNextCard(clientCard);
		}
		avoidLinkedZones = true;
		blockWicckid = true;
		return true;
	}

	private bool HaveUG()
	{
		if (!base.Bot.HasInHand(68337209))
		{
			return base.Bot.HasInSpellZone(68337209);
		}
		return true;
	}

	private bool HasSelfSSAvailable(int id)
	{
		return id switch
		{
			32061192 => !ssDormouse, 
			69272449 => !ssWhiteRabbit, 
			96676583 => !ssChessyCat, 
			20938824 => !ActiveMarchHare, 
			_ => true, 
		};
	}

	private int PickUG_DHG_DormouseFirst()
	{
		int[] array = new int[4] { 32061192, 69272449, 96676583, 20938824 };
		foreach (int num in array)
		{
			if (HasSelfSSAvailable(num) && ExistsForUnderground(num))
			{
				return num;
			}
		}
		return 0;
	}

	private bool Flow3_UnderGround_Available_SSAnyPawn()
	{
		if (!step2Done)
		{
			return false;
		}
		if (!HaveUG())
		{
			return false;
		}
		if (base.Card.Id != 68337209)
		{
			return false;
		}
		if (resultSuccessFlag)
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			base.AI.SelectYesNo(opt: false);
			return true;
		}
		if (enemyActivateLancea)
		{
			base.AI.SelectYesNo(opt: false);
			return true;
		}
		int num = 0;
		num = ((CheckRemainInDeck(93453053) > 0 && nsplan) ? 93453053 : ((CheckRemainInDeck(93453053) <= 0 || !NSDorMouse) ? PickUG_DHG_DormouseFirst() : 93453053));
		if (num == 0)
		{
			base.AI.SelectYesNo(opt: false);
			return true;
		}
		base.AI.SelectYesNo(opt: true);
		base.AI.SelectCard(num);
		resultSuccessFlag = true;
		return true;
	}

	private bool Mirror_Banish()
	{
		if (DescIs(93453053, 1))
		{
			ClientCard clientCard = PickMirrorGYTargetForSearch();
			if (clientCard == null)
			{
				return false;
			}
			base.AI.SelectCard(clientCard);
			int[] ids = Mirror_SearchOrderForType(isTrap: false, clientCard.IsMonster());
			base.AI.SelectNextCard(ids);
			return true;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard clientCard2 = PickMirrorCostCandidate();
		if (clientCard2 == null)
		{
			return false;
		}
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (monster.IsMonsterShouldBeDisabledBeforeItUseEffect() && !monster.IsDisabled() && base.Duel.LastChainPlayer != 0)
			{
				if (base.Card.Location == CardLocation.Hand)
				{
					SelectSTPlace(base.Card, avoidImpermanence: true);
				}
				base.AI.SelectCard(monster);
				base.AI.SelectNextCard(clientCard2);
				return true;
			}
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard == null || lastChainCard.Controller != 1 || lastChainCard.Location != CardLocation.MonsterZone || lastChainCard.IsDisabled() || lastChainCard.IsShouldNotBeTarget() || lastChainCard.IsShouldNotBeSpellTrapTarget())
		{
			return false;
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
				}
			}
		}
		base.AI.SelectNextCard(clientCard2);
		return true;
	}

	private bool Flow3_Link_Accesscode()
	{
		if (base.Bot.HasInExtra(39138610))
		{
			return false;
		}
		if (BlockAccesscodeOnT1())
		{
			return false;
		}
		int[] avoidIds = new int[2] { 39138610, 5043010 };
		List<ClientCard> list = PickLinkMatsMinCount(4, (ClientCard m) => m.HasType(CardType.Effect), 2, 4, avoidIds);
		if (list.Count < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool Flow3_BackupIgnister_AfterMakeIt3()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!madeIt3)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		avoidLinkedZones = true;
		return true;
	}

	private bool Flow3_WizardIgnister_AfterMakeIt3()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (!madeIt3)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		ClientCard clientCard = PickGYMalissPriority();
		if (clientCard == null)
		{
			return false;
		}
		avoidLinkedZones = true;
		base.AI.SelectCard(clientCard);
		return true;
	}

	private bool RR_SS_FromBanished()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 68059897)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 900)
		{
			return false;
		}
		int num = 0;
		int[] array = new int[4] { 96676583, 20938824, 69272449, 32061192 };
		foreach (int num2 in array)
		{
			if (CheckRemainInDeck(num2) > 0 && PawnSelfSS_AvailableId(num2))
			{
				num = num2;
				break;
			}
		}
		bool flag = num != 0;
		base.AI.SelectYesNo(flag);
		if (flag)
		{
			base.AI.SelectCard(num);
		}
		_rrSelfSSPlacing = true;
		ssRRThisTurn = true;
		return true;
	}

	private bool Wicckid_SearchTuner()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (CheckRemainInDeck(30118811) <= 0)
		{
			return false;
		}
		ClientCard clientCard = PickGYCyberseForWicckidCost_Safe();
		if (clientCard == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		base.AI.SelectNextCard(30118811);
		avoidLinkedZones = false;
		return true;
	}

	private bool LinkDecoder_ReviveFromGY()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (Allied_End)
		{
			return false;
		}
		return true;
	}

	private bool Transcode_ReviveLink3OrLower()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		ClientCard clientCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c.IsCode(52698008)) ?? base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c.IsCode(59859086)) ?? base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c.IsCode(68059897)) ?? base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c.IsCode(95454996)) ?? (from c in base.Bot.Graveyard.GetMatchingCards((ClientCard c) => c.IsMonster() && c.HasType(CardType.Link) && c.LinkCount <= 3 && !c.IsCode(46947713))
			orderby c.Attack descending
			select c).FirstOrDefault();
		if (clientCard == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		avoidLinkedZones = false;
		return true;
	}

	private bool Allied_NegateBanish()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (CheckWhetherNegated() || !CheckLastChainShouldNegated())
		{
			return false;
		}
		ClientCard firstMatchingCard = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(39138610) ?? false);
		if (firstMatchingCard == null || firstMatchingCard.IsDisabled())
		{
			return false;
		}
		if (!base.Bot.GetMonsters().Any((ClientCard m) => m != null && m.HasType(CardType.Link) && !m.IsCode(39138610)))
		{
			return false;
		}
		List<int> list = new List<int> { 52698008, 95454996, 46947713, 68059897 };
		base.AI.SelectCard(list.ToArray());
		return true;
	}

	private ClientCard GetWicckid()
	{
		return base.Bot.MonsterZone.GetFirstMatchingFaceupCard((ClientCard c) => c?.IsCode(52698008) ?? false);
	}

	private int GetLinkedMaskFor(ClientCard link)
	{
		if (link == null || !link.IsFaceup() || !link.HasType(CardType.Link))
		{
			return 0;
		}
		return link.GetLinkedZones() & 0x1F;
	}

	private bool PawnSelfSS_AvailableId(int id)
	{
		return id switch
		{
			32061192 => !ssDormouse, 
			69272449 => !ssWhiteRabbit, 
			96676583 => !ssChessyCat, 
			20938824 => !ActiveMarchHare, 
			_ => false, 
		};
	}

	private bool QueenSelfSS_AvailableId(int id)
	{
		return id switch
		{
			21848500 => !ssHCThisTurn, 
			95454996 => !ssWBThisTurn, 
			68059897 => !ssRRThisTurn, 
			_ => false, 
		};
	}

	private bool Dormouse_Banish_Anytime()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Id != 32061192)
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (!HasFreeMMZ())
		{
			return false;
		}
		int num = 0;
		if (CheckRemainInDeck(69272449) > 0 && PawnSelfSS_AvailableId(69272449))
		{
			num = 69272449;
		}
		else if (CheckRemainInDeck(96676583) > 0 && PawnSelfSS_AvailableId(96676583))
		{
			num = 96676583;
		}
		if (num == 0)
		{
			return false;
		}
		base.AI.SelectCard(num);
		return true;
	}

	private bool IsCyberse(ClientCard c)
	{
		if (c != null && c.HasType(CardType.Monster))
		{
			return c.HasRace(CardRace.Cyberse);
		}
		return false;
	}

	private bool RR_HOPT_Spent_ThisTurn()
	{
		return ssRRThisTurn;
	}

	private bool RR_CanStillSS_ThisTurn()
	{
		if (RR_HOPT_Spent_ThisTurn())
		{
			return false;
		}
		return true;
	}

	private int Score_WicckidCost(ClientCard c)
	{
		if (c == null)
		{
			return -2147483648;
		}
		if (c.IsCode(68059897) && RR_HOPT_Spent_ThisTurn())
		{
			return -999;
		}
		if (c.IsCode(68059897) && RR_CanStillSS_ThisTurn())
		{
			return 1000;
		}
		if (c.IsCode(24842059) || c.IsCode(60303245))
		{
			return 120;
		}
		if (c.IsCode(32061192) && ssDormouse)
		{
			return 90;
		}
		if (c.IsCode(69272449) && ssWhiteRabbit)
		{
			return 70;
		}
		if (c.IsCode(96676583) && ssChessyCat)
		{
			return 60;
		}
		if (c.IsCode(20938824) && ssMarchHare)
		{
			return 60;
		}
		if (c.IsCode(30118811))
		{
			return 20;
		}
		if (c.IsCode(3723262))
		{
			return 20;
		}
		return 30;
	}

	private ClientCard PickGYCyberseForWicckidCost_Safe()
	{
		List<ClientCard> list = (base.Bot.Graveyard ?? new List<ClientCard>()).Where(IsCyberse).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		ClientCard result = null;
		int num = -2147483648;
		foreach (ClientCard item in list)
		{
			int num2 = Score_WicckidCost(item);
			if (num2 > num)
			{
				result = item;
				num = num2;
			}
		}
		return result;
	}

	private bool BlockAccesscodeOnT1()
	{
		if (base.Duel.Player == 0)
		{
			return base.Duel.Turn == 1;
		}
		return false;
	}

	private bool Allied_OnSummonTrigger()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(39138610) ?? false) == null)
		{
			return false;
		}
		if (!base.Bot.Graveyard.Any((ClientCard c) => c != null && c.IsMonster() && c.HasRace(CardRace.Cyberse) && c.Attack == 2300))
		{
			return false;
		}
		List<int> list = new List<int> { 68059897, 95454996, 46947713 };
		base.AI.SelectCard(list.ToArray());
		return true;
	}

	private bool ShouldSummonTranscode()
	{
		bool num = base.Bot.HasInGraveyard(59859086);
		bool flag = base.Bot.HasInMonstersZone(68059897);
		bool flag2 = base.Bot.HasInMonstersZone(30342076) || base.Bot.HasInGraveyard(30342076);
		if (num)
		{
			return flag || flag2;
		}
		return false;
	}

	private bool Step_SplashToRR()
	{
		if (_didSplashToRR)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(30342076))
		{
			return false;
		}
		if (!base.Bot.HasInExtra(68059897))
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(2, (ClientCard m) => m.HasType(CardType.Effect), 2, 2, new int[1] { 52698008 });
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		_didSplashToRR = true;
		return true;
	}

	private bool Step_SplashToWB()
	{
		if (_didSplashToRR)
		{
			return false;
		}
		if (!base.Bot.HasInExtra(95454996))
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(2, (ClientCard m) => m.HasType(CardType.Effect), 2, 2, new int[1] { 52698008 });
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		_didSplashToRR = true;
		return true;
	}

	private bool Step_RRtoWicckid()
	{
		if (enemyActivateLancea)
		{
			return false;
		}
		if (blockWicckid)
		{
			return true;
		}
		if (!_didSplashToRR || _didRRtoWicckid)
		{
			return false;
		}
		if (!base.Bot.HasInExtra(52698008))
		{
			return false;
		}
		ClientCard rr = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(68059897) ?? false);
		if (rr == null)
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard m) => m != null && m != rr && IsInEMZ(m)) ?? base.Bot.MonsterZone.FirstOrDefault((ClientCard m) => m != null && m != rr);
		if (clientCard == null)
		{
			return false;
		}
		if (!HasFreeEMZ() && !IsInEMZ(rr) && !IsInEMZ(clientCard))
		{
			return false;
		}
		base.AI.SelectCard(52698008);
		base.AI.SelectMaterials(new List<ClientCard> { rr, clientCard });
		wantLinkedToWicckid = true;
		_preferWicckidArrows = true;
		_didRRtoWicckid = true;
		return true;
	}

	private bool Step2N_RRtoWicckid()
	{
		if (enemyActivateLancea)
		{
			return false;
		}
		if (blockWicckid)
		{
			return false;
		}
		ClientCard rr = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(68059897) ?? false);
		if (rr == null)
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard m) => m != null && m != rr && IsInEMZ(m)) ?? base.Bot.MonsterZone.FirstOrDefault((ClientCard m) => m != null && m != rr);
		if (clientCard == null)
		{
			return false;
		}
		if (!HasFreeEMZ() && !IsInEMZ(rr) && !IsInEMZ(clientCard))
		{
			return false;
		}
		base.AI.SelectCard(52698008);
		base.AI.SelectMaterials(new List<ClientCard> { rr, clientCard });
		wantLinkedToWicckid = true;
		_preferWicckidArrows = true;
		_didRRtoWicckid = true;
		return true;
	}

	private bool Step_SummonLinkDecoderToWicckid()
	{
		if (!_didRRtoWicckid || _didSummonToWicckidArrow)
		{
			return false;
		}
		if (base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard m) => m?.IsCode(52698008) ?? false) == null)
		{
			return false;
		}
		_preferWicckidArrows = true;
		avoidLinkedZones = false;
		return true;
	}

	private bool Step1_SSLinkDecoder()
	{
		if ((base.Bot.HasInMonstersZone(32061192) || base.Bot.HasInMonstersZone(69272449) || base.Bot.HasInMonstersZone(96676583) || base.Bot.HasInMonstersZone(30118811)) && base.Bot.GetMonsterCount() == 1 && base.Bot.HasInHand(20938824))
		{
			return true;
		}
		return false;
	}

	private bool IsMalissMonster(ClientCard c)
	{
		if (c != null && c.IsMonster())
		{
			return c.HasSetcode(447);
		}
		return false;
	}

	private bool CanMakeLinkNWithFlexibleTwo(ClientCard a, ClientCard b, int target)
	{
		int num = 1;
		int num2 = ((a == null || !a.HasType(CardType.Link)) ? 1 : Math.Max(1, a.LinkCount));
		int num3 = 1;
		int num4 = ((b == null || !b.HasType(CardType.Link)) ? 1 : Math.Max(1, b.LinkCount));
		if (num + num3 != target && num + num4 != target && num2 + num3 != target)
		{
			return num2 + num4 == target;
		}
		return true;
	}

	private bool Step_WicckidPlusOneToWB()
	{
		if (!base.Bot.HasInExtra(95454996))
		{
			return false;
		}
		ClientCard wic = GetWicckid();
		if (wic != null)
		{
			List<ClientCard> source = (from m in base.Bot.GetMonsters()
				where m != null && m.IsFaceup() && m != wic && m.HasType(CardType.Effect)
				select m).ToList();
			Func<ClientCard, bool> okPair = (ClientCard p) => (IsMalissMonster(p) || IsMalissMonster(wic)) && CanMakeLinkNWithFlexibleTwo(wic, p, 3);
			ClientCard clientCard = source.FirstOrDefault((ClientCard p) => !p.IsCode(68059897) && okPair(p)) ?? source.FirstOrDefault((ClientCard p) => p.IsCode(68059897) && okPair(p));
			if (clientCard != null)
			{
				base.AI.SelectMaterials(new List<ClientCard> { wic, clientCard });
				_didWBFromWicckid = true;
				EnsureFinishPlanAfterWB();
				return true;
			}
		}
		List<ClientCard> list = PickLinkMatsMinCount(3, (ClientCard m) => m.HasType(CardType.Effect), 2, 3, new int[3] { 4280258, 5043010, 39138610 }, requireMaliss: true);
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		_didWBFromWicckid = true;
		EnsureFinishPlanAfterWB();
		return true;
	}

	private void EnsureFinishPlanAfterWB()
	{
		if (!_finishPlanDecided)
		{
			int num = base.Bot.MonsterZone.Count((ClientCard m) => m != null && !m.IsCode(68059897));
			bool num2 = base.Bot.HasInExtra(5043010);
			bool flag = base.Bot.HasInExtra(21848500);
			bool flag2 = base.Bot.HasInExtra(39138610);
			bool flag3 = CanReachAlliedNow();
			if (num2 && flag && flag2 && num >= 3 && flag3)
			{
				_finishPlan = FinishPlan.FW_HC_Allied;
			}
			else if (flag && flag2 && num >= 2 && flag3)
			{
				_finishPlan = FinishPlan.HC_Allied;
			}
			else
			{
				_finishPlan = FinishPlan.AlliedOnly;
			}
			_finishPlanDecided = true;
		}
	}

	private bool ssFromHandMH()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (GetMMZCount() > 3)
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Duel.Player == 0)
		{
			List<ClientCard> arg = base.Bot.Graveyard.GetMatchingCards((ClientCard c) => c != null && c.HasSetcode(447) && c != base.Card).ToList();
			ClientCard clientCard = ((Func<IEnumerable<ClientCard>, ClientCard>)delegate(IEnumerable<ClientCard> src)
			{
				int[] array = new int[3] { 95454996, 21848500, 68059897 };
				foreach (int id in array)
				{
					ClientCard clientCard2 = src.FirstOrDefault((ClientCard x) => x.HasType(CardType.Monster) && x.Id == id && QueenSelfSS_AvailableId(x.Id));
					if (clientCard2 != null)
					{
						return clientCard2;
					}
				}
				return src.FirstOrDefault((ClientCard x) => x.HasType(CardType.Monster) && QueenSelfSS_AvailableId(x.Id));
			})(arg);
			if (clientCard == null)
			{
				return false;
			}
			base.AI.SelectCard(clientCard);
			ssMarchHare = true;
			return DontSelfNG();
		}
		if (base.Bot.HasInGraveyard(95454996) && !ssWBThisTurn)
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(95454996) ?? false);
			if (firstMatchingCard == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard);
		}
		else if (base.Bot.HasInGraveyard(21848500) && !ssHCThisTurn)
		{
			ClientCard firstMatchingCard2 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(21848500) ?? false);
			if (firstMatchingCard2 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard2);
		}
		else
		{
			if (!base.Bot.HasInGraveyard(68059897) || ssRRThisTurn)
			{
				return false;
			}
			ClientCard firstMatchingCard3 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(68059897) ?? false);
			if (firstMatchingCard3 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard3);
		}
		ssMarchHare = true;
		return DontSelfNG();
	}

	private bool Step1_MH_FromHand()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Hand)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() > 1)
		{
			return false;
		}
		if (enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(69272449) && base.Bot.HasInGraveyard(94722358))
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(94722358) ?? false);
			if (firstMatchingCard == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard);
			ssMarchHare = true;
			return DontSelfNG();
		}
		if (base.Bot.HasInMonstersZone(30342076) && base.Bot.HasInGraveyard(32061192))
		{
			ClientCard firstMatchingCard2 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(32061192) ?? false);
			if (firstMatchingCard2 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard2);
			ssMarchHare = true;
			return DontSelfNG();
		}
		if (base.Bot.HasInMonstersZone(30342076) && base.Bot.HasInGraveyard(69272449))
		{
			ClientCard firstMatchingCard3 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(69272449) ?? false);
			if (firstMatchingCard3 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard3);
			ssMarchHare = true;
			return DontSelfNG();
		}
		if (base.Bot.HasInMonstersZone(30342076) && base.Bot.HasInGraveyard(96676583))
		{
			ClientCard firstMatchingCard4 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(96676583) ?? false);
			if (firstMatchingCard4 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard4);
			ssMarchHare = true;
			return DontSelfNG();
		}
		if (base.Bot.HasInMonstersZone(24842059) && base.Bot.HasInGraveyard(20938824))
		{
			ClientCard firstMatchingCard5 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(20938824) ?? false);
			if (firstMatchingCard5 == null)
			{
				return false;
			}
			base.AI.SelectCard(firstMatchingCard5);
			ssMarchHare = true;
			return DontSelfNG();
		}
		return false;
	}

	private bool returnFromBanish()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		ClientCard firstMatchingCard = base.Bot.Banished.GetFirstMatchingCard((ClientCard c) => c.IsFaceup() && c.IsCode(20938824));
		if (firstMatchingCard == null)
		{
			return false;
		}
		base.AI.SelectCard(firstMatchingCard);
		ActiveMarchHare = true;
		return true;
	}

	private bool WB_OnSummon_BanishGY()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		if (base.Duel.Player == 0)
		{
			int num = Math.Max(0, 5 - GetMMZCount());
			bool flag = base.Bot.Graveyard.GetMatchingCards((ClientCard g) => IsMalissPawn(g)).Count > 1;
			if (!ActiveMarchHare && flag)
			{
				ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(20938824));
				if (firstMatchingCard != null)
				{
					list.Add(firstMatchingCard);
				}
			}
			if (num > 0 && !ssRRThisTurn && !Allied_End)
			{
				ClientCard firstMatchingCard2 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(68059897));
				if (firstMatchingCard2 != null && !list.Contains(firstMatchingCard2) && !ShouldSkipBanishing(firstMatchingCard2))
				{
					list.Add(firstMatchingCard2);
					num--;
				}
			}
			if (num > 0 && !ssHCThisTurn && !Allied_End)
			{
				ClientCard firstMatchingCard3 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(21848500));
				if (firstMatchingCard3 != null && !list.Contains(firstMatchingCard3) && !ShouldSkipBanishing(firstMatchingCard3))
				{
					list.Add(firstMatchingCard3);
					num--;
				}
			}
			if (num > 0 && !ssWhiteRabbit && !Allied_End && flag)
			{
				ClientCard firstMatchingCard4 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(69272449));
				if (firstMatchingCard4 != null && !list.Contains(firstMatchingCard4) && !ShouldSkipBanishing(firstMatchingCard4))
				{
					list.Add(firstMatchingCard4);
					num--;
				}
			}
			if (num > 0 && !ssDormouse && !Allied_End && flag)
			{
				ClientCard firstMatchingCard5 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(32061192));
				if (firstMatchingCard5 != null && !list.Contains(firstMatchingCard5) && !ShouldSkipBanishing(firstMatchingCard5))
				{
					list.Add(firstMatchingCard5);
					num--;
				}
			}
			if (num > 0 && !ssChessyCat && !Allied_End && flag)
			{
				ClientCard firstMatchingCard6 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(96676583));
				if (firstMatchingCard6 != null && !list.Contains(firstMatchingCard6) && !ShouldSkipBanishing(firstMatchingCard6))
				{
					list.Add(firstMatchingCard6);
					num--;
				}
			}
			if (num >= 0 && flag)
			{
				ClientCard firstMatchingCard7 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(96676583) || g.IsCode(32061192));
				list.Add(firstMatchingCard7);
			}
			list = list.Where((ClientCard c) => c != null).Distinct().Take(3)
				.ToList();
			foreach (ClientCard item in PickEnemyGYThreats(3 - list.Count))
			{
				if (list.Count >= 3)
				{
					break;
				}
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		else
		{
			int num2 = Math.Max(0, 5 - GetMMZCount());
			if (!base.Bot.HasInSpellZone(94722358) && num2 > 0 && !ssWhiteRabbit)
			{
				ClientCard firstMatchingCard8 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(69272449));
				if (firstMatchingCard8 != null)
				{
					list.Add(firstMatchingCard8);
					num2--;
				}
			}
			if (list.Count >= 0 && !base.Bot.HasInHand(20938824) && !ActiveMarchHare)
			{
				ClientCard firstMatchingCard9 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(20938824));
				if (firstMatchingCard9 != null)
				{
					list.Add(firstMatchingCard9);
				}
			}
			if (num2 > 0 && !ssRRThisTurn)
			{
				ClientCard firstMatchingCard10 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(68059897));
				if (firstMatchingCard10 != null && !list.Contains(firstMatchingCard10) && !ShouldSkipBanishing(firstMatchingCard10))
				{
					list.Add(firstMatchingCard10);
					num2--;
				}
			}
			if (num2 > 0 && !ssHCThisTurn)
			{
				ClientCard firstMatchingCard11 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard g) => g.IsCode(21848500));
				if (firstMatchingCard11 != null && !list.Contains(firstMatchingCard11) && !ShouldSkipBanishing(firstMatchingCard11))
				{
					list.Add(firstMatchingCard11);
					num2--;
				}
			}
			foreach (ClientCard item2 in PickEnemyGYThreats(3 - list.Count))
			{
				if (list.Count >= 3)
				{
					break;
				}
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		if (list.Count > 3)
		{
			list = list.Take(3).ToList();
		}
		if (list.Count < 3)
		{
			foreach (ClientCard item3 in PickEnemyGYThreats(3 - list.Count))
			{
				if (!list.Contains(item3))
				{
					list.Add(item3);
				}
			}
		}
		base.AI.SelectCard(list);
		return true;
	}

	private bool IsMalissPawn(ClientCard c)
	{
		if (c != null)
		{
			if (!c.IsCode(96676583) && !c.IsCode(69272449) && !c.IsCode(32061192))
			{
				return c.IsCode(20938824);
			}
			return true;
		}
		return false;
	}

	private List<ClientCard> PickEnemyGYThreats(int need)
	{
		List<ClientCard> list = new List<ClientCard>();
		if (need <= 0)
		{
			return list;
		}
		foreach (ClientCard item in GetDangerousCardinEnemyGrave())
		{
			if (list.Count >= need)
			{
				break;
			}
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		if (list.Count < need)
		{
			int num = base.Enemy.Graveyard.Count - 1;
			while (num >= 0 && list.Count < need)
			{
				ClientCard clientCard = base.Enemy.Graveyard[num];
				if (clientCard != null && !list.Contains(clientCard))
				{
					list.Add(clientCard);
				}
				num--;
			}
		}
		if (list.Count < need)
		{
			foreach (ClientCard item2 in base.Enemy.Graveyard)
			{
				if (list.Count >= need)
				{
					break;
				}
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private bool WB_OnBanished_SelfSS()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 900)
		{
			return false;
		}
		base.AI.SelectYesNo(opt: true);
		ssWBThisTurn = true;
		return true;
	}

	private int PickMalissTrapForWB()
	{
		if (CheckRemainInDeck(20726052) > 0 || base.Bot.HasInGraveyard(20726052))
		{
			return 20726052;
		}
		if (CheckRemainInDeck(94722358) > 0 || base.Bot.HasInGraveyard(94722358))
		{
			return 94722358;
		}
		return 0;
	}

	private bool WB_SetMalissTrap()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.ActivateDescription != base.Util.GetStringId(95454996, 1))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		SelectSafeSTZoneAwayFromImperm();
		return true;
	}

	private List<ClientCard> PickLinkMatsMinCount(int targetLink, Func<ClientCard, bool> isEligible, int minCount, int maxCount, IEnumerable<int> avoidIds = null, bool requireMaliss = false)
	{
		List<ClientCard> list = (from m in base.Bot.GetMonsters()
			where m != null && m.IsFaceup() && isEligible(m)
			select m).ToList();
		if (list.Count < minCount)
		{
			return new List<ClientCard>();
		}
		HashSet<int> avoid = new HashSet<int>(avoidIds ?? Enumerable.Empty<int>());
		Func<IEnumerable<ClientCard>, List<ClientCard>> OrderForFirst = (IEnumerable<ClientCard> src) => src.OrderByDescending(LinkValOf).ThenBy((ClientCard m) => avoid.Contains(m.Id) ? 1 : 0).ThenBy((ClientCard m) => m.Attack)
			.ToList();
		Func<IEnumerable<ClientCard>, List<ClientCard>> OrderForLater = (IEnumerable<ClientCard> src) => src.OrderBy((ClientCard m) => (!IsOneVal(m)) ? 1 : 0).ThenBy(delegate(ClientCard m)
		{
			if (!m.HasType(CardType.Link))
			{
				return -1;
			}
			return (Math.Max(1, m.LinkCount) != 1) ? 1 : 0;
		}).ThenBy(LinkValOf)
			.ThenBy((ClientCard m) => avoid.Contains(m.Id) ? 1 : 0)
			.ThenBy((ClientCard m) => m.Attack)
			.ToList();
		List<ClientCard> arg = list.Where((ClientCard m) => !avoid.Contains(m.Id)).ToList();
		List<ClientCard> arg2 = list.ToList();
		int firstMaxAllowed = targetLink - Math.Max(0, minCount - 1);
		Func<List<ClientCard>, List<ClientCard>> func = delegate(List<ClientCard> pool)
		{
			List<ClientCard> chosen = new List<ClientCard>();
			Func<List<ClientCard>, int, bool> Dfs = null;
			Dfs = delegate(List<ClientCard> avail, int sum)
			{
				if (sum > targetLink || chosen.Count > maxCount)
				{
					return false;
				}
				if (chosen.Count >= minCount && sum == targetLink)
				{
					if (requireMaliss && !chosen.Any(IsMalissBody))
					{
						return false;
					}
					if (base.Util.GetBotAvailZonesFromExtraDeck(chosen) == 0)
					{
						return false;
					}
					return true;
				}
				int num = Math.Max(0, minCount - chosen.Count);
				if (sum + num > targetLink)
				{
					return false;
				}
				List<ClientCard> list3 = ((chosen.Count == 0) ? OrderForFirst(avail) : OrderForLater(avail));
				int i;
				for (i = 0; i < list3.Count; i++)
				{
					ClientCard clientCard = list3[i];
					List<ClientCard> arg3 = list3.Where((ClientCard x, int idx) => idx != i).ToList();
					int num2 = LinkValOf(clientCard);
					foreach (int item in ((chosen.Count != 0) ? ((num2 <= 1) ? new int[1] { 1 } : new int[2] { 1, num2 }) : ((num2 <= 1) ? new int[1] { 1 } : new int[2]
					{
						Math.Min(num2, firstMaxAllowed),
						1
					})).Distinct())
					{
						int num3 = sum + item;
						if (num3 <= targetLink)
						{
							int num4 = Math.Max(0, minCount - (chosen.Count + 1));
							if (num3 + num4 <= targetLink)
							{
								chosen.Add(clientCard);
								if (Dfs(arg3, num3))
								{
									return true;
								}
								chosen.RemoveAt(chosen.Count - 1);
							}
						}
					}
				}
				return false;
			};
			return Dfs(OrderForFirst(pool), 0) ? chosen : null;
		};
		List<ClientCard> list2 = func(arg);
		if (list2 != null && list2.Count > 0)
		{
			return list2;
		}
		list2 = func(arg2);
		return list2 ?? new List<ClientCard>();
	}

	private bool Link_Apo()
	{
		if (!base.Bot.HasInMonstersZone(68059897) || !base.Bot.HasInMonstersZone(30342076))
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(4, (ClientCard m) => m.HasType(CardType.Monster), 2, 2, new int[3] { 46947713, 39138610, 86066372 });
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		blockWicckid = true;
		return true;
	}

	private bool Flow3_Link_Firewall()
	{
		if (base.Bot.HasInMonstersZone(21848500) && base.Bot.HasInMonstersZone(30342076) && base.Bot.HasInMonstersZone(52698008))
		{
			List<ClientCard> list = PickLinkMatsMinCount(4, (ClientCard m) => m.HasType(CardType.Monster), 2, 2, new int[5] { 52698008, 4280258, 46947713, 39138610, 86066372 });
			if (list.Count == 0)
			{
				return false;
			}
			base.AI.SelectMaterials(list);
			return true;
		}
		return false;
	}

	private bool Step_LinkSummon_HeartsCrypter()
	{
		if (base.Bot.HasInMonstersZone(95454996) && base.Bot.HasInMonstersZone(68059897) && base.Bot.HasInMonstersZone(4280258) && base.Bot.GetMonsterCount() < 5)
		{
			return false;
		}
		List<ClientCard> list = (from c in base.Bot.GetMonsters()
			where c != null && c.IsFaceup() && c.HasType(CardType.Effect)
			select c).ToList();
		if (list.Count < 3)
		{
			return false;
		}
		HashSet<int> avoid = new HashSet<int> { 52698008, 4280258, 39138610, 86066372, 5043010, 46947713 };
		List<ClientCard> source = (from m in list
			orderby avoid.Contains(m.Id) ? 2 : 0, m.HasType(CardType.Link) ? 1 : 0, m.Attack
			select m).ToList();
		List<ClientCard> list2 = source.Take(3).ToList();
		if (!list2.Any(IsMaliss))
		{
			ClientCard clientCard = source.FirstOrDefault(IsMaliss);
			if (clientCard == null)
			{
				return false;
			}
			list2[2] = clientCard;
		}
		if (base.Util.GetBotAvailZonesFromExtraDeck(list2) == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list2);
		return true;
	}

	private bool HC_Quick_ReturnBanished_AndBanishField()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.ActivateDescription != base.Util.GetStringId(21848500, 0))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		RefreshNoChainWindows();
		List<ClientCard> banishedMaliss = GetBanishedMaliss();
		if (banishedMaliss.Count == 0)
		{
			return false;
		}
		bool flag = banishedMaliss.Count > 0;
		bool flag2 = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, (CardType)7).Count > 0;
		if (base.Duel.Player == 0)
		{
			if (!flag && !flag2)
			{
				return false;
			}
		}
		else if (!flag2 && !IsPreferredRemovalTiming())
		{
			return false;
		}
		if (!flag)
		{
			return false;
		}
		ClientCard clientCard = PickBanishedMalissForHC(banishedMaliss);
		if (clientCard == null)
		{
			return false;
		}
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, (CardType)7);
		if (problematicEnemyCardList.Count == 0)
		{
			ClientCard bestEnemyCard = GetBestEnemyCard(onlyFaceup: false, canBeTarget: true);
			if (bestEnemyCard != null)
			{
				problematicEnemyCardList.Add(bestEnemyCard);
			}
		}
		if (problematicEnemyCardList.Count == 0)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		base.AI.SelectNextCard(problematicEnemyCardList);
		ConsumePreferredWindow();
		return DontSelfNG();
	}

	private List<ClientCard> GetBanishedMaliss()
	{
		return base.Bot.Banished.GetMatchingCards((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(447)).ToList();
	}

	private ClientCard PickBanishedMalissForHC(List<ClientCard> cand)
	{
		cand = cand.Where((ClientCard c) => !c.IsCode(95454996) && !c.IsCode(68059897)).ToList();
		if (cand.Count == 0)
		{
			return null;
		}
		return cand.OrderByDescending(ScoreForBanishedMaliss).First();
	}

	private bool HC_OnBanished_SpecialSummon()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Card.Location != CardLocation.Removed)
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		if (base.Bot.LifePoints <= 900)
		{
			return false;
		}
		base.AI.SelectYesNo(opt: true);
		ssHCThisTurn = true;
		return true;
	}

	private bool HasMalissLinkFaceup()
	{
		return base.Bot.GetMonsters().Any((ClientCard c) => c != null && c.IsFaceup() && c.HasSetcode(447) && c.HasType(CardType.Link));
	}

	private bool CanReachAlliedNow()
	{
		return PickLinkMatsMinCount(5, (ClientCard m) => m.HasType(CardType.Effect), 3, 5, new int[3] { 4280258, 5043010, 86066372 }).Count > 0;
	}

	private bool Flow3_Link_Allied()
	{
		if (!base.Bot.HasInMonstersZoneOrInGraveyard(68059897) || !base.Bot.HasInMonstersZoneOrInGraveyard(95454996))
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(5, (ClientCard m) => m.HasType(CardType.Effect), 3, 5, new int[3] { 4280258, 5043010, 86066372 });
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		Allied_End = true;
		return true;
	}

	private bool Emer_Allied()
	{
		if (!enemyActivateLancea)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(46947713) || !base.Bot.HasInMonstersZone(59859086))
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(5, (ClientCard m) => m.HasType(CardType.Effect), 3, 3);
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		Allied_End = true;
		return true;
	}

	private bool Emer_Allied2()
	{
		List<ClientCard> list = (from m in base.Bot.GetMonsters()
			where m != null
			select m).ToList();
		if (list.Count != 3)
		{
			return false;
		}
		if (list.Where((ClientCard m) => m.HasType(CardType.Link) && m.LinkCount == 3).ToList().Count != 1)
		{
			return false;
		}
		if (!base.Bot.HasInGraveyard(68059897) && !base.Bot.HasInGraveyard(95454996) && !base.Bot.HasInGraveyard(46947713))
		{
			return false;
		}
		List<ClientCard> list2 = PickLinkMatsMinCount(5, (ClientCard m) => m.HasType(CardType.Effect), 3, 3);
		if (list2.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list2);
		Allied_End = true;
		return true;
	}

	private ClientCard FindGWC06TargetByOrder(params int[] ids)
	{
		foreach (int id in ids)
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(id) ?? false);
			if (firstMatchingCard != null)
			{
				return firstMatchingCard;
			}
			ClientCard firstMatchingCard2 = base.Bot.Banished.GetFirstMatchingCard((ClientCard c) => c != null && c.IsCode(id) && c.IsFaceup());
			if (firstMatchingCard2 != null)
			{
				return firstMatchingCard2;
			}
		}
		return null;
	}

	private int PickGWC06CostCandidateId()
	{
		if (base.Bot.HasInMonstersZone(95454996) && !ssWBThisTurn)
		{
			return 95454996;
		}
		if (base.Bot.HasInMonstersZone(68059897) && !ssRRThisTurn)
		{
			return 68059897;
		}
		int num = PickTB11CostCandidateId();
		if (num != 0)
		{
			return num;
		}
		return 0;
	}

	private ClientCard PickGWC06TargetExtend()
	{
		if (base.Duel.Turn > 2)
		{
			return FindGWC06TargetByOrder(21848500, 68059897, 95454996);
		}
		return FindGWC06TargetByOrder(95454996, 68059897, 21848500, 69272449, 32061192, 96676583, 20938824);
	}

	private bool GWC06_MyTurn_Extend()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.Duel.Player == 0 && GetMMZCount() >= 4 && base.Bot.HasInMonstersZone(5043010) && (base.Bot.HasInMonstersZoneOrInGraveyard(68059897) || base.Bot.HasInMonstersZoneOrInGraveyard(95454996) || base.Bot.HasInMonstersZoneOrInGraveyard(21848500)))
		{
			return false;
		}
		if (base.Duel.Player != 0)
		{
			return false;
		}
		if (base.Duel.Phase != DuelPhase.Main1 && base.Duel.Phase != DuelPhase.Main2)
		{
			return false;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(95454996) && (!base.Bot.HasInMonstersZone(68059897) || base.Bot.GetMonsterCount() != 1))
		{
			return false;
		}
		ClientCard clientCard = PickGWC06TargetExtend();
		if (clientCard == null)
		{
			return false;
		}
		if (gwc06SetThisTurn)
		{
			int num = PickGWC06CostCandidateId();
			if (num == 0)
			{
				return false;
			}
			base.AI.SelectCard(num);
			base.AI.SelectNextCard(clientCard);
			return DontSelfNG();
		}
		base.AI.SelectCard(clientCard);
		return DontSelfNG();
	}

	private bool GWC06_OppTurn_ReviveWB_HC()
	{
		if (base.Duel.Player != 1)
		{
			return false;
		}
		if (!base.Bot.HasInSpellZone(20726052))
		{
			return false;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (GetMMZCount() >= 5)
		{
			return false;
		}
		ClientCard clientCard = FindGWC06TargetByOrder(95454996, 21848500);
		if (clientCard == null)
		{
			return false;
		}
		if (gwc06SetThisTurn)
		{
			int num = PickGWC06CostCandidateId();
			if (num == 0)
			{
				return false;
			}
			base.AI.SelectCard(num);
			base.AI.SelectNextCard(clientCard);
			return DontSelfNG();
		}
		base.AI.SelectCard(clientCard);
		return DontSelfNG();
	}

	private static int FirstBit(int mask)
	{
		for (int i = 0; i < 32; i++)
		{
			int num = 1 << i;
			if ((mask & num) != 0)
			{
				return num;
			}
		}
		return 0;
	}

	private static int FirstBitFromOrder(int mask, int[] order)
	{
		foreach (int num in order)
		{
			if ((mask & num) != 0)
			{
				return num;
			}
		}
		return FirstBit(mask);
	}

	private int PreferSafeSummonZones(int available)
	{
		int num = 31;
		int num2 = available & ~num;
		if (num2 != 0)
		{
			return FirstBit(num2);
		}
		int num3 = 0;
		try
		{
			num3 = base.Enemy.GetLinkedZones();
		}
		catch
		{
		}
		int num4 = available & num & ~num3;
		if (num4 != 0)
		{
			return FirstBitFromOrder(num4, new int[5] { 4, 2, 8, 1, 16 });
		}
		return FirstBit(available);
	}

	private int PickMTP07CostCandidateId()
	{
		return PickTB11CostCandidateId();
	}

	private ClientCard PickMTP07EnemyRemovalTarget()
	{
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap);
		if (problematicEnemyCardList.Count > 0)
		{
			return problematicEnemyCardList[0];
		}
		ClientCard bestEnemyMonster = GetBestEnemyMonster(onlyFaceup: false, canBeTarget: true);
		if (bestEnemyMonster != null)
		{
			return bestEnemyMonster;
		}
		ClientCard bestEnemySpell = GetBestEnemySpell(onlyFaceup: false, canBeTarget: true);
		if (bestEnemySpell != null)
		{
			return bestEnemySpell;
		}
		return base.Enemy.GetMonsters().FirstOrDefault((ClientCard c) => c != null) ?? base.Enemy.GetSpells().FirstOrDefault((ClientCard c) => c != null);
	}

	private bool MTP07_ForMH()
	{
		if (base.Bot.GetMonsterCount() != 1 || !base.Bot.HasInMonstersZone(69272449))
		{
			return false;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		int num = PickMTP07SearchId();
		if (num == 0)
		{
			return false;
		}
		if (mtp07SetThisTurn)
		{
			int num2 = PickMTP07CostCandidateId();
			if (num2 == 0)
			{
				return false;
			}
			base.AI.SelectCard(num2);
		}
		base.AI.SelectNextCard(num);
		return DontSelfNG();
	}

	private bool MTP07_OppTurn_RemoveEnemyOnly()
	{
		if (base.Duel.Player != 1)
		{
			return false;
		}
		if (!HasMalissLinkFaceup())
		{
			return false;
		}
		if (CheckSpellWillBeNegate())
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreSpells: false, CardType.Trap);
		if (problematicEnemyCardList.Count == 0 && !IsPreferredRemovalTiming())
		{
			return false;
		}
		bool flag = base.Duel.Phase == DuelPhase.Main1 && base.Enemy.GetMonsterCount() > 0;
		if (problematicEnemyCardList.Count == 0 && !(IsPreferredRemovalTiming() || flag))
		{
			return false;
		}
		int num = PickMTP07SearchId();
		if (num == 0)
		{
			return false;
		}
		ClientCard clientCard = null;
		clientCard = ((problematicEnemyCardList.Count > 0) ? problematicEnemyCardList[0] : ((!flag) ? PickMTP07EnemyRemovalTarget() : (from c in base.Enemy.MonsterZone
			where c?.IsFaceup() ?? false
			orderby c.Attack descending
			select c).FirstOrDefault()));
		if (clientCard == null)
		{
			return false;
		}
		if (mtp07SetThisTurn)
		{
			int num2 = PickMTP07CostCandidateId();
			if (num2 == 0)
			{
				return false;
			}
			base.AI.SelectCard(num2);
			base.AI.SelectNextCard(num);
			base.AI.SelectNextCard(clientCard);
			ConsumePreferredWindow();
			return DontSelfNG();
		}
		base.AI.SelectCard(num);
		base.AI.SelectNextCard(clientCard);
		ConsumePreferredWindow();
		return DontSelfNG();
	}

	private bool Emergency_NS()
	{
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(96676583) || base.Bot.HasInHand(75500286) || base.Bot.HasInHand(68337209))
		{
			return false;
		}
		if (!base.Bot.HasInHand(30118811))
		{
			return false;
		}
		usedNormalSummon = true;
		nsplan = true;
		return true;
	}

	private bool IsWicInEMZ(ClientCard wic)
	{
		if (wic != null && wic.Location == CardLocation.MonsterZone)
		{
			if (wic.Sequence != EMZ_LEFT)
			{
				return wic.Sequence == EMZ_RIGHT;
			}
			return true;
		}
		return false;
	}

	private IEnumerable<int> GetWicDownSeq(ClientCard wic)
	{
		if (IsWicInEMZ(wic))
		{
			if (wic.Sequence == EMZ_LEFT)
			{
				yield return 1;
				yield return 2;
			}
			if (wic.Sequence == EMZ_RIGHT)
			{
				yield return 3;
				yield return 4;
			}
		}
	}

	private bool IsUnderWic(ClientCard wic, ClientCard m)
	{
		if (wic == null || m == null)
		{
			return false;
		}
		if (m.Controller != wic.Controller)
		{
			return false;
		}
		if (m.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (m.Sequence < 0 || m.Sequence > 4)
		{
			return false;
		}
		return GetWicDownSeq(wic).Contains(m.Sequence);
	}

	private ClientCard PickUnderlingForTranscode(ClientCard wic, IList<ClientCard> pool)
	{
		if (!IsWicInEMZ(wic))
		{
			return null;
		}
		List<ClientCard> list = pool.Where((ClientCard x) => x != null && x != wic && x.IsFaceup() && x.Location == CardLocation.MonsterZone && x.Controller == wic.Controller && x.Sequence >= 0 && x.Sequence <= 4 && IsUnderWic(wic, x) && (x.HasType(CardType.Link) || x.HasType(CardType.Effect)) && !x.HasType(CardType.Token)).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		ClientCard clientCard = list.FirstOrDefault((ClientCard x) => x.IsCode(30342076));
		if (clientCard != null)
		{
			return clientCard;
		}
		return (from x in list
			orderby (!x.HasType(CardType.Link)) ? 1 : 2 descending, x.Attack descending
			select x).FirstOrDefault();
	}

	private bool SummonTranscode()
	{
		if (!enemyActivateLancea)
		{
			return false;
		}
		ClientCard clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard x) => x != null && x.IsFaceup() && x.IsCode(52698008));
		if (!IsWicInEMZ(clientCard))
		{
			return false;
		}
		ClientCard clientCard2 = PickUnderlingForTranscode(clientCard, base.Bot.GetMonsters());
		if (clientCard2 == null)
		{
			return false;
		}
		SelectLinkMaterialsPair(clientCard, clientCard2);
		int num = EmzBitFor(clientCard);
		if (num != 0)
		{
			base.AI.SelectPlace(num);
		}
		return true;
	}

	private void SelectLinkMaterialsPair(ClientCard a, ClientCard b)
	{
		try
		{
			List<ClientCard> cards = new List<ClientCard> { a, b };
			base.AI.SelectMaterials(cards);
			return;
		}
		catch
		{
		}
		base.AI.SelectCard(a);
		base.AI.SelectNextCard(b);
	}

	private int EmzBitFor(ClientCard link)
	{
		if (link == null || link.Location != CardLocation.MonsterZone)
		{
			return 0;
		}
		if (link.Sequence == EMZ_LEFT)
		{
			return 1 << EMZ_LEFT;
		}
		if (link.Sequence == EMZ_RIGHT)
		{
			return 1 << EMZ_RIGHT;
		}
		return 0;
	}

	private int ChooseAndRememberWicckidEmz(int available)
	{
		int num = available & 0x60;
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		int num3 = -2147483648;
		int[] array = new int[2] { 32, 64 };
		foreach (int num4 in array)
		{
			if ((num & num4) != 0)
			{
				int num5 = 0;
				int num6 = DownBitOfEmz(num4);
				if (num6 == 2 && IsMainFreeSeq(1))
				{
					num5 += 10;
				}
				if (num6 == 8 && IsMainFreeSeq(3))
				{
					num5 += 10;
				}
				if (num5 > num3)
				{
					num3 = num5;
					num2 = num4;
				}
			}
		}
		if (num2 == 0)
		{
			num2 = (((num & 0x20) != 0) ? 32 : 64);
		}
		_wicckidEmzBit = num2;
		return num2;
	}

	private bool IsMainFreeSeq(int seq)
	{
		return !base.Bot.MonsterZone.GetMonsters().Any((ClientCard m) => m != null && m.Controller == 0 && m.Sequence == seq);
	}

	private int DownBitOfEmz(int emzBit)
	{
		return emzBit switch
		{
			32 => 2, 
			64 => 8, 
			_ => 0, 
		};
	}

	private bool IsPreferredRemovalTiming()
	{
		if (base.Duel.Player != 1)
		{
			return false;
		}
		if (_prefWindowTTL > 0)
		{
			return true;
		}
		if (_oppJustActivatedPersistentSpell || _oppJustSummoned || _oppJustSet)
		{
			return true;
		}
		return false;
	}

	private void ConsumePreferredWindow()
	{
		_prefWindowTTL = 0;
		_oppJustActivatedPersistentSpell = false;
		_oppJustSummoned = false;
		_oppJustSet = false;
	}

	private void RefreshNoChainWindows()
	{
		bool flag = base.Duel.Player == 1 && (base.Duel.Phase == DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2);
		int monsterCount = base.Enemy.GetMonsterCount();
		if (monsterCount > _enemyMonsterCountSnap)
		{
			_oppJustSummoned = true;
			if (flag)
			{
				_prefWindowTTL = Math.Max(_prefWindowTTL, 2);
			}
		}
		_enemyMonsterCountSnap = monsterCount;
		int num = base.Enemy.SpellZone.Count((ClientCard c) => c?.IsFacedown() ?? false);
		if (num > _enemyFacedownSTSnap)
		{
			_oppJustSet = true;
			if (flag)
			{
				_prefWindowTTL = Math.Max(_prefWindowTTL, 2);
			}
		}
		_enemyFacedownSTSnap = num;
		if (flag)
		{
			if (_prefWindowTTL > 0)
			{
				_prefWindowTTL--;
			}
			if (_prefWindowTTL == 0)
			{
				_oppJustActivatedPersistentSpell = false;
				_oppJustSummoned = false;
				_oppJustSet = false;
			}
		}
		else
		{
			_prefWindowTTL = 0;
			_oppJustActivatedPersistentSpell = false;
			_oppJustSummoned = false;
			_oppJustSet = false;
		}
	}

	private bool FirewallBounce_OnOppSummon()
	{
		if (DefaultCheckWhetherCardIdIsNegated(base.Card.Id))
		{
			return false;
		}
		if (base.ActivateDescription != base.Util.GetStringId(5043010, 0))
		{
			return false;
		}
		if (base.Duel.LastSummonPlayer != 1)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate();
		if (monsterListForTargetNegate != null)
		{
			foreach (ClientCard item in monsterListForTargetNegate)
			{
				if (item != null && item.Controller == 1 && item.IsMonster() && item.IsFaceup() && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		foreach (ClientCard item2 in from x in base.Enemy.GetMonsters()
			orderby x.Attack descending
			select x)
		{
			if (item2 != null && item2.IsMonster() && item2.IsFaceup() && !list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectCard(list);
		return true;
	}

	private bool Accesscode_OnSummon_AtkUp()
	{
		List<ClientCard> list = new List<ClientCard>();
		int[] array = new int[4] { 21848500, 95454996, 68059897, 46947713 };
		foreach (int id in array)
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(id) ?? false);
			if (firstMatchingCard != null)
			{
				list.Add(firstMatchingCard);
			}
		}
		base.AI.SelectCard(list);
		return true;
	}

	private bool Accesscode_Destroy_Ignition()
	{
		if (base.ActivateDescription != base.Util.GetStringId(86066372, 1))
		{
			return false;
		}
		if (base.Enemy.GetFieldCount() == 0)
		{
			return false;
		}
		IEnumerable<ClientCard> graveyardMonsters = base.Bot.GetGraveyardMonsters();
		IEnumerable<ClientCard> first = (graveyardMonsters ?? Enumerable.Empty<ClientCard>()).Where((ClientCard c) => c.HasType(CardType.Link));
		graveyardMonsters = base.Bot.GetMonsters();
		List<ClientCard> list = (from c in first.Concat((graveyardMonsters ?? Enumerable.Empty<ClientCard>()).Where((ClientCard c) => c.HasType(CardType.Link) && c != base.Card))
			orderby c.Location != CardLocation.Grave, c.LinkCount, c.Attack
			select c).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		List<ClientCard> list2 = new List<ClientCard>();
		list2.AddRange(from s in base.Enemy.GetSpells()
			where s?.IsFacedown() ?? false
			select s);
		list2.AddRange(from s in base.Enemy.GetSpells()
			where s != null && !s.IsFacedown()
			select s);
		list2.AddRange(from m in base.Enemy.GetMonsters()
			orderby m.Attack descending
			select m);
		if (list2.Count == 0)
		{
			return false;
		}
		base.AI.SelectCard(list);
		base.AI.SelectNextCard(list2);
		return true;
	}

	private bool AlreadySSFromBanishThisTurn(ClientCard c)
	{
		if (c == null)
		{
			return false;
		}
		if (c.Controller != 0)
		{
			return false;
		}
		if (c.IsCode(32061192))
		{
			return ssDormouse;
		}
		if (c.IsCode(69272449))
		{
			return ssWhiteRabbit;
		}
		if (c.IsCode(96676583))
		{
			return ssChessyCat;
		}
		if (c.IsCode(68059897))
		{
			return ssRRThisTurn;
		}
		if (c.IsCode(21848500))
		{
			return ssHCThisTurn;
		}
		if (c.IsCode(95454996))
		{
			return ssWBThisTurn;
		}
		return false;
	}

	private bool ShouldSkipBanishing(ClientCard c)
	{
		if (c == null)
		{
			return true;
		}
		if (c.Controller != 0)
		{
			return false;
		}
		if (AlreadySSFromBanishThisTurn(c))
		{
			return true;
		}
		if (c.IsCode(20938824) && ActiveMarchHare)
		{
			return true;
		}
		return false;
	}

	private bool DescIs(int cardId, params int[] idx)
	{
		if (base.ActivateDescription == -1)
		{
			return true;
		}
		foreach (int option in idx)
		{
			if (base.ActivateDescription == base.Util.GetStringId(cardId, option))
			{
				return true;
			}
		}
		return false;
	}

	private ClientCard PickMirrorCostCandidate()
	{
		int[] array = new int[4] { 69272449, 96676583, 32061192, 20938824 };
		foreach (int id in array)
		{
			ClientCard firstMatchingCard = base.Bot.Hand.GetFirstMatchingCard((ClientCard c) => c?.IsCode(id) ?? false);
			if (firstMatchingCard != null)
			{
				return firstMatchingCard;
			}
		}
		ClientCard clientCard = (from c in base.Bot.GetMonsters()
			where c != null && c.HasSetcode(447) && !c.HasType(CardType.Link)
			orderby c.Attack
			select c).FirstOrDefault();
		if (clientCard != null)
		{
			return clientCard;
		}
		int[] avoid = new int[3] { 21848500, 95454996, 68059897 };
		return (from c in base.Bot.GetMonsters()
			where c != null && c.HasSetcode(447) && c.HasType(CardType.Link) && !avoid.Contains(c.Id)
			orderby c.Attack
			select c).FirstOrDefault();
	}

	private int[] Mirror_SearchOrderForType(bool isTrap, bool isMon)
	{
		if (!isTrap)
		{
			if (isMon)
			{
				if (!base.Bot.HasInHand(69272449) && NSDorMouse)
				{
					return new int[4] { 69272449, 20938824, 32061192, 96676583 };
				}
				return new int[4] { 20938824, 32061192, 69272449, 96676583 };
			}
			return new int[4] { 20938824, 32061192, 69272449, 96676583 };
		}
		return new int[2] { 20726052, 94722358 };
	}

	private ClientCard PickMirrorGYTargetForSearch()
	{
		if (NSDorMouse && !ssRRThisTurn)
		{
			ClientCard firstMatchingCard = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(68059897) ?? false);
			if (firstMatchingCard != null)
			{
				return firstMatchingCard;
			}
		}
		int[] array = new int[4] { 20938824, 32061192, 69272449, 96676583 };
		foreach (int id in array)
		{
			ClientCard firstMatchingCard2 = base.Bot.Graveyard.GetFirstMatchingCard((ClientCard c) => c?.IsCode(id) ?? false);
			if (firstMatchingCard2 != null && CheckRemainInDeck(id) > 0)
			{
				return firstMatchingCard2;
			}
		}
		return null;
	}

	private int PickMTP07SearchId()
	{
		if (CheckRemainInDeck(20938824) > 0)
		{
			return 20938824;
		}
		int[] array = new int[3] { 32061192, 69272449, 96676583 };
		foreach (int num in array)
		{
			if (CheckRemainInDeck(num) > 0)
			{
				return num;
			}
		}
		return 0;
	}

	private void SelectSafeSTZoneAwayFromImperm()
	{
		List<int> list = Enumerable.Range(0, 5).Where(delegate(int seq)
		{
			if (base.Bot.SpellZone[seq] != null)
			{
				return false;
			}
			return !infiniteImpermanenceList.Contains(seq);
		}).ToList();
		if (list.Count == 0)
		{
			list = (from seq in Enumerable.Range(0, 5)
				where base.Bot.SpellZone[seq] == null
				select seq).ToList();
		}
		int num = 0;
		foreach (int item in list)
		{
			num |= 1 << item;
		}
		base.AI.SelectPlace(num);
	}

	private bool T3Allow()
	{
		if (base.Duel.Player != 0)
		{
			return false;
		}
		if (myTurnCount < 2)
		{
			return false;
		}
		if (!HaveBackupOrWizardInHand() || !base.Bot.HasInHand(20938824))
		{
			return false;
		}
		return true;
	}

	private bool NSBackup()
	{
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(96676583) || base.Bot.HasInHand(68337209) || base.Bot.HasInHand(73628505) || base.Bot.HasInHand(75500286))
		{
			return false;
		}
		nsBackupplan = true;
		return true;
	}

	private bool NSBackup_L()
	{
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(96676583) || base.Bot.HasInHand(20938824) || base.Bot.HasInHand(68337209) || base.Bot.HasInHand(73628505) || base.Bot.HasInHand(75500286))
		{
			return false;
		}
		nsLanceaplan = true;
		return true;
	}

	private bool NSMH()
	{
		if (base.Bot.Hand.GetMatchingCards((ClientCard c) => c?.IsCode(20938824) ?? false).Count < 2)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 0)
		{
			return false;
		}
		if (usedNormalSummon)
		{
			return false;
		}
		if (base.Bot.HasInHand(32061192) || base.Bot.HasInHand(69272449) || base.Bot.HasInHand(96676583) || base.Bot.HasInHand(68337209) || base.Bot.HasInHand(73628505) || base.Bot.HasInHand(75500286))
		{
			return false;
		}
		return true;
	}

	private bool LinguribohMHLine()
	{
		if (!base.Bot.HasInHand(20938824) || !base.Bot.HasInMonstersZone(20938824))
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() != 1)
		{
			return false;
		}
		return true;
	}

	private bool EmerTranscode()
	{
		if (!enemyActivateLancea)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() < 3)
		{
			return false;
		}
		List<ClientCard> list = PickLinkMatsMinCount(3, (ClientCard m) => m.HasType(CardType.Effect), 2, 2);
		if (list.Count == 0)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}
}
