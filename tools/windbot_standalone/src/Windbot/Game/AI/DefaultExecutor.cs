using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI;

public abstract class DefaultExecutor : Executor
{
	protected class _CardId
	{
		public const int JizukirutheStarDestroyingKaiju = 63941210;

		public const int ThunderKingtheLightningstrikeKaiju = 48770333;

		public const int DogorantheMadFlameKaiju = 93332803;

		public const int RadiantheMultidimensionalKaiju = 28674152;

		public const int GadarlatheMysteryDustKaiju = 36956512;

		public const int KumongoustheStickyStringKaiju = 29726552;

		public const int GamecieltheSeaTurtleKaiju = 55063751;

		public const int SuperAntiKaijuWarMachineMechaDogoran = 84769941;

		public const int SandaionTheTimelord = 33015627;

		public const int GabrionTheTimelord = 6616912;

		public const int MichionTheTimelord = 7733560;

		public const int ZaphionTheTimelord = 28929131;

		public const int HailonTheTimelord = 34137269;

		public const int RaphionTheTimelord = 60222213;

		public const int SadionTheTimelord = 65314286;

		public const int MetaionTheTimelord = 74530899;

		public const int KamionTheTimelord = 91712985;

		public const int LazionTheTimelord = 92435533;

		public const int LeftArmofTheForbiddenOne = 7902349;

		public const int RightLegofTheForbiddenOne = 8124921;

		public const int LeftLegofTheForbiddenOne = 44519536;

		public const int RightArmofTheForbiddenOne = 70903634;

		public const int ExodiaTheForbiddenOne = 33396948;

		public const int UltimateConductorTytanno = 18940556;

		public const int ElShaddollConstruct = 20366274;

		public const int AllyOfJusticeCatastor = 26593852;

		public const int DupeFrog = 46239604;

		public const int MaraudingCaptain = 2460565;

		public const int BlackRoseDragon = 73580471;

		public const int JudgmentDragon = 57774843;

		public const int TopologicTrisbaena = 72529749;

		public const int EvilswarmExcitonKnight = 46772449;

		public const int HarpiesFeatherDuster = 18144506;

		public const int DarkMagicAttack = 2314238;

		public const int MysticalSpaceTyphoon = 5318639;

		public const int CosmicCyclone = 8267140;

		public const int GalaxyCyclone = 5133471;

		public const int BookOfMoon = 14087893;

		public const int CompulsoryEvacuationDevice = 94192409;

		public const int CallOfTheHaunted = 97077563;

		public const int Scapegoat = 73915051;

		public const int BreakthroughSkill = 78474168;

		public const int SolemnJudgment = 41420027;

		public const int SolemnWarning = 84749824;

		public const int SolemnStrike = 40605147;

		public const int TorrentialTribute = 53582587;

		public const int EvenlyMatched = 15693423;

		public const int HeavyStorm = 19613556;

		public const int HammerShot = 26412047;

		public const int DarkHole = 53129443;

		public const int Raigeki = 12580477;

		public const int SmashingGround = 97169186;

		public const int PotOfDesires = 35261759;

		public const int AllureofDarkness = 1475311;

		public const int DimensionalBarrier = 83326048;

		public const int InterruptedKaijuSlumber = 99330325;

		public const int ChickenGame = 67616300;

		public const int SantaClaws = 46565218;

		public const int CastelTheSkyblasterMusketeer = 82633039;

		public const int CrystalWingSynchroDragon = 50954680;

		public const int NumberS39UtopiaTheLightning = 56832966;

		public const int Number39Utopia = 84013237;

		public const int UltimayaTzolkin = 1686814;

		public const int MekkKnightCrusadiaAstram = 21887175;

		public const int HamonLordofStrikingThunder = 32491822;

		public const int MoonMirrorShield = 19508728;

		public const int PhantomKnightsFogBlade = 25542642;

		public const int VampireFraeulein = 6039967;

		public const int InjectionFairyLily = 79575620;

		public const int BlueEyesChaosMAXDragon = 55410871;

		public const int AshBlossom = 14558127;

		public const int MaxxC = 23434538;

		public const int LockBird = 94145021;

		public const int GhostOgreAndSnowRabbit = 59438930;

		public const int GhostBelle = 73642296;

		public const int EffectVeiler = 97268402;

		public const int GhostMournerMoonlitChill = 52038441;

		public const int ArtifactLancea = 34267821;

		public const int DimensionShifter = 91800273;

		public const int NibiruThePrimalBeing = 27204311;

		public const int MulcharmyPurulia = 84192580;

		public const int MulcharmyFuwalos = 42141493;

		public const int MulcharmyNyalus = 87126721;

		public const int CalledByTheGrave = 24224830;

		public const int CrossoutDesignator = 65681983;

		public const int InfiniteImpermanence = 10045474;

		public const int GalaxySoldier = 46659709;

		public const int MacroCosmos = 30241314;

		public const int UpstartGoblin = 70368879;

		public const int CyberEmergency = 60600126;

		public const int TheAgentOfCreationVenus = 64734921;

		public const int EaterOfMillions = 63845230;

		public const int InvokedPurgatrio = 12307878;

		public const int ChaosAncientGearGiant = 51788412;

		public const int UltimateAncientGearGolem = 12652643;

		public const int RedDragonArchfiend = 70902743;

		public const int ImperialOrder = 61740673;

		public const int RoyalDecreel = 51452091;

		public const int NaturalExterio = 99916754;

		public const int NaturiaBeast = 33198837;

		public const int SwordsmanLV7 = 37267041;

		public const int AntiSpellFragrance = 58921041;

		public const int Number41BagooskatheTerriblyTiredTapir = 90590303;

		public const int SkillDrain = 82732705;

		public const int DimensionalFissure = 81674782;

		public const int BanisheroftheRadiance = 94853057;

		public const int BanisheroftheLight = 61528025;

		public const int KashtiraAriseHeart = 48626373;

		public const int MaskedHERODarkLaw = 58481572;

		public const int VaylantzWorld_ShinraBansho = 49568943;

		public const int VaylantzWorld_KonigWissen = 75952542;

		public const int DivineArsenalAAZEUS_SkyThunder = 90448279;

		public const int LightningStorm = 14532163;

		public const int MistakenArrest = 4227096;

		public const int ThunderKingRaiOh = 71564252;

		public const int ThunderDragonColossus = 15291624;

		public const int DeckLockdown = 1149109;

		public const int DoomZDestruction = 80320877;

		public const int Mistake = 59305593;

		public const int BelialMarquisOfDarkness = 33655493;

		public const int ChirubiméPrincessOfAutumnLeaves = 87294988;

		public const int PerformapalBarokuriboh = 19050066;

		public const int LabrynthArchfiend = 48745395;

		public const int HarpiesPetDragonFearsomeFireBlast = 4991081;

		public const int DynaHeroFurHire = 25123713;

		public const int Hieracosphinx = 82260502;

		public const int SpeedroidPassinglider = 26420373;

		public const int TyrOfTheNordicChampions = 2333365;

		public const int ValkyrianKnight = 99348756;

		public const int Victoria = 75162696;

		public const int MadolcheChouxvalier = 75363626;

		public const int LadyOfD = 67511500;

		public const int MermailAbysslung = 95466842;

		public const int HarpiesPetBabyDragon = 6924874;

		public const int HandHoldingGenie = 94535485;

		public const int GolemDragon = 9666558;

		public const int TwilightRoseKnight = 2986553;

		public const int PerformapalThunderhino = 70458081;

		public const int MiracleFlipper = 131182;

		public const int Decoyroid = 25034083;

		public const int AltergeistFifinellag = 12977245;

		public const int BatterymanD = 55401221;

		public const int Watthopper = 61380658;

		public const int EgyptianGodSlime = 42166000;

		public const int DinowrestlerChimeraTWrextle = 22900219;

		public const int DinowrestlerGigaSpinosavate = 58672736;

		public const int ScarredWarrior = 45298492;

		public const int SharkFortress = 50449881;

		public const int HeroicChampionClaivesolish = 97453744;

		public const int GhostrickAlucard = 75367227;

		public const int DinowrestlerKingTWrextle = 77967790;

		public const int NumberF0UtopicFutureZexal = 41522092;

		public const int PerformapalMissDirector = 92932860;

		public const int AncientWarriorsMasterfulSunMou = 40140448;

		public const int AncientWarriorsVirtuousLiuXuan = 40428851;

		public const int CommandKnight = 10375182;

		public const int HunterOwl = 51962254;

		public const int RokketRecharger = 5969957;

		public const int EmissaryOfTheOasis = 6103294;

		public const int Zuttomozaurus = 24454387;

		public const int Otoshidamashi = 14957440;

		public const int NaturiaMosquito = 17285476;

		public const int RescueACEHydrant = 37617348;

		public const int MeizenTheBattleNinja = 11825276;

		public const int VindikiteRGenex = 73483491;

		public const int PrincessCologne = 75574498;

		public const int Number48ShadowLich = 1426714;

		public const int PhantomToken = 1426715;

		public const int DuelLinkDragonTheDuelDragon = 60025883;

		public const int DuelDragonToken = 60025884;

		public const int SeleneQueenOfTheMasterMagicians = 45819647;

		public const int TheWingedDragonofRaSphereMode = 10000080;

		public const int SelettriceVaalmonica = 23093373;

		public const int PerformageTrapezeWitch = 33206889;

		public const int PoseidraTheStormingAtlantean = 99193444;

		public const int RockOfTheVanquisher = 28168628;

		public const int SpiralDischarge = 29477860;

		public const int GaiaTheDragonChampion = 66889139;

		public const int CrusadiaVanguard = 55312487;

		public const int GladiatorBeastDomitianus = 33652635;

		public const int PatricianOfDarkness = 19153634;

		public const int DictatorOfD = 66961194;

		public const int LoThePrayersOfTheVoicelessVoice = 25801745;

		public const int BarrierOfTheVoicelessVoice = 98477480;

		public const int DiabellzeOfTheOriginalSin = 53765052;

		public const int PotOfExtravagance = 49238328;
	}

	protected class _Setcode
	{
		public const int Watt = 14;

		public const int Speedroid = 8214;

		public const int EarthboundImmortal = 4129;

		public const int Naturia = 42;

		public const int Nordic = 66;

		public const int TimeLord = 74;

		public const int Harpie = 100;

		public const int Madolche = 113;

		public const int Ghostrick = 141;

		public const int OddEyes = 153;

		public const int Performapal = 159;

		public const int Performage = 198;

		public const int BlueEyes = 221;

		public const int FurHire = 276;

		public const int Altergeist = 259;

		public const int Crusadia = 278;

		public const int Danger = 286;

		public const int Endymion = 298;

		public const int AncientWarriors = 311;

		public const int RescueACE = 395;

		public const int VanquishSoul = 405;

		public const int Horus = 413;
	}

	protected int lightningStormOption = -1;

	private Dictionary<int, int> calledbytheGraveIdCountMap = new Dictionary<int, int>();

	private List<int> crossoutDesignatorIdList = new List<int>();

	private int mistakenArrestAffectedCount;

	protected List<int> resolvedEffectIdList = new List<int>();

	protected List<int> enemyResolvedEffectIdList = new List<int>();

	protected List<int> infiniteImpermanenceNegatedColumns = new List<int>();

	protected Dictionary<int, Func<ClientCard, bool>> DefenderProtectRule = new Dictionary<int, Func<ClientCard, bool>>
	{
		{
			33655493,
			(ClientCard defender) => defender.IsFaceup()
		},
		{
			87294988,
			(ClientCard defender) => defender.HasRace(CardRace.Plant)
		},
		{
			19050066,
			(ClientCard defender) => true
		},
		{
			48745395,
			(ClientCard defender) => defender.HasRace(CardRace.Fiend) && !defender.IsCode(48745395)
		},
		{
			4991081,
			(ClientCard defender) => defender.Level <= 6 && defender.HasSetcode(100)
		},
		{
			25123713,
			(ClientCard defender) => defender.HasSetcode(276)
		},
		{
			82260502,
			(ClientCard defender) => defender.IsFacedown()
		},
		{
			26420373,
			(ClientCard defender) => defender.HasSetcode(8214)
		},
		{
			2333365,
			(ClientCard defender) => defender.HasSetcode(66)
		},
		{
			99348756,
			(ClientCard defender) => defender.HasRace(CardRace.Warrior) && !defender.IsCode(99348756)
		},
		{
			75162696,
			(ClientCard defender) => defender.HasRace(CardRace.Fairy)
		},
		{
			75363626,
			(ClientCard defender) => defender.HasSetcode(113) && !defender.IsCode(75363626)
		},
		{
			67511500,
			(ClientCard defender) => defender.HasRace(CardRace.Dragon)
		},
		{
			95466842,
			(ClientCard defender) => defender.HasAttribute(CardAttribute.Water)
		},
		{
			6924874,
			(ClientCard defender) => defender.HasSetcode(100) && !defender.IsCode(6924874)
		},
		{
			94535485,
			(ClientCard defender) => true
		},
		{
			9666558,
			(ClientCard defender) => defender.HasRace(CardRace.Dragon)
		},
		{
			2460565,
			(ClientCard defender) => defender.HasRace(CardRace.Warrior)
		},
		{
			2986553,
			(ClientCard defender) => defender.HasRace(CardRace.Plant)
		},
		{
			70458081,
			(ClientCard defender) => defender.HasSetcode(159)
		},
		{
			131182,
			(ClientCard defender) => defender.IsFaceup()
		},
		{
			25034083,
			(ClientCard defender) => defender.IsFaceup()
		},
		{
			46239604,
			(ClientCard defender) => true
		},
		{
			12977245,
			(ClientCard defender) => defender.HasSetcode(259)
		},
		{
			55401221,
			(ClientCard defender) => defender.HasRace(CardRace.Thunder) && !defender.IsCode(55401221)
		},
		{
			61380658,
			(ClientCard defender) => defender.HasSetcode(14) && defender.IsFaceup()
		},
		{
			42166000,
			(ClientCard defender) => true
		},
		{
			22900219,
			(ClientCard defender) => true
		},
		{
			58672736,
			(ClientCard defender) => true
		},
		{
			45298492,
			(ClientCard defender) => defender.HasRace(CardRace.Warrior) && defender.IsFaceup()
		},
		{
			50449881,
			(ClientCard defender) => true
		},
		{
			97453744,
			(ClientCard defender) => true
		},
		{
			75367227,
			(ClientCard defender) => defender.HasSetcode(141) || defender.IsFacedown()
		},
		{
			21887175,
			(ClientCard defender) => true
		},
		{
			77967790,
			(ClientCard defender) => true
		},
		{
			41522092,
			(ClientCard defender) => true
		}
	};

	protected Dictionary<int, Func<ClientCard, List<ClientCard>, bool>> DefenderInvisbleRule = new Dictionary<int, Func<ClientCard, List<ClientCard>, bool>>
	{
		{
			1686814,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasType(CardType.Synchro))
		},
		{
			92932860,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.HasSetcode(153))
		},
		{
			40140448,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasSetcode(311))
		},
		{
			40428851,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasSetcode(311))
		},
		{
			10375182,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender))
		},
		{
			51962254,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasAttribute(CardAttribute.Wind))
		},
		{
			5969957,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.IsExtraCard() && monster.HasAttribute(CardAttribute.Dark))
		},
		{
			6103294,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.HasType(CardType.Normal) && monster.Level <= 3)
		},
		{
			24454387,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasRace(CardRace.Dinosaur))
		},
		{
			14957440,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.HasType(CardType.Tuner))
		},
		{
			17285476,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender) && monster.HasSetcode(42))
		},
		{
			37617348,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.IsCode(37617348) && monster.HasSetcode(395))
		},
		{
			11825276,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.IsFacedown())
		},
		{
			73483491,
			(ClientCard defender, List<ClientCard> list) => true
		},
		{
			75574498,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.Equals(defender))
		},
		{
			1426714,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.IsCode(1426715))
		},
		{
			60025883,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.IsCode(60025884))
		},
		{
			45819647,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => monster.HasSetcode(298))
		},
		{
			10000080,
			(ClientCard defender, List<ClientCard> list) => true
		},
		{
			23093373,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.IsCode(23093373))
		},
		{
			33206889,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.IsCode(33206889) && monster.HasSetcode(198))
		},
		{
			99193444,
			(ClientCard defender, List<ClientCard> list) => list.Any((ClientCard monster) => !monster.IsCode(99193444))
		}
	};

	protected DefaultExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 67616300, DefaultChickenGame);
		AddExecutor(ExecutorType.Activate, 49568943, DefaultVaylantzWorld_ShinraBansho);
		AddExecutor(ExecutorType.Activate, 75952542, DefaultVaylantzWorld_KonigWissen);
		AddExecutor(ExecutorType.Activate, 46565218);
		AddExecutor(ExecutorType.SpellSet, DefaultSetForDiabellze);
	}

	public override BattlePhaseAction OnSelectAttackTarget(ClientCard attacker, IList<ClientCard> defenders)
	{
		foreach (ClientCard defender in defenders)
		{
			attacker.RealPower = attacker.Attack;
			defender.RealPower = defender.GetDefensePower();
			if (OnPreBattleBetween(attacker, defender) && (attacker.RealPower > defender.RealPower || (attacker.RealPower >= defender.RealPower && attacker.IsLastAttacker && defender.IsAttack())))
			{
				return base.AI.Attack(attacker, defender);
			}
		}
		if (attacker.CanDirectAttack)
		{
			return base.AI.Attack(attacker, null);
		}
		return null;
	}

	public override bool OnPreBattleBetween(ClientCard attacker, ClientCard defender)
	{
		if (!attacker.IsMonsterHasPreventActivationEffectInBattle())
		{
			if (defender.IsMonsterInvincible() && defender.IsDefense())
			{
				return false;
			}
			if (defender.IsMonsterDangerous() && (attacker.IsDisabled() || ((!attacker.IsCode(18940556) || !defender.IsDefense()) && (!attacker.IsCode(20366274) || !defender.IsSpecialSummoned) && (!attacker.IsCode(26593852) || defender.HasAttribute(CardAttribute.Dark)))))
			{
				return false;
			}
			if (defender.EquipCards.Any((ClientCard equip) => equip.IsCode(19508728) && !equip.IsDisabled()))
			{
				return false;
			}
			if (!defender.IsDisabled())
			{
				if (defender.IsCode(21887175) && defender.IsAttack() && attacker.IsSpecialSummoned)
				{
					defender.RealPower += attacker.Attack;
				}
				if (defender.IsCode(50954680) && defender.IsAttack() && attacker.Level >= 5)
				{
					defender.RealPower += attacker.Attack;
				}
				if (defender.IsCode(26593852) && !attacker.HasAttribute(CardAttribute.Dark))
				{
					return false;
				}
				if (defender.IsCode(56832966) && defender.IsAttack() && defender.HasXyzMaterial(2, 84013237))
				{
					defender.RealPower = 5000;
				}
				if (defender.IsCode(6039967))
				{
					defender.RealPower += ((base.Enemy.LifePoints > 3000) ? 3000 : (base.Enemy.LifePoints - 100));
				}
				if (defender.IsCode(79575620) && base.Enemy.LifePoints > 2000)
				{
					defender.RealPower += 3000;
				}
			}
		}
		if (!defender.IsMonsterHasPreventActivationEffectInBattle())
		{
			if (attacker.IsCode(56832966) && !attacker.IsDisabled() && attacker.HasXyzMaterial(2, 84013237))
			{
				attacker.RealPower = 5000;
			}
			if (attacker.IsCode(63845230) && !attacker.IsDisabled())
			{
				attacker.RealPower = 9999;
			}
			if (attacker.IsMonsterInvincible())
			{
				attacker.RealPower = 9999;
			}
			if (attacker.EquipCards.Any((ClientCard equip) => equip.IsCode(19508728) && !equip.IsDisabled()))
			{
				attacker.RealPower = defender.RealPower + 100;
			}
		}
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (!monster.IsDisabled() && monster != defender)
			{
				Func<ClientCard, bool> value = (ClientCard card) => false;
				if (DefenderProtectRule.TryGetValue(monster.Id, out value) && value(defender))
				{
					return false;
				}
			}
		}
		if (attacker.EquipCards.Any((ClientCard equip) => equip.IsCode(19508728) && !equip.IsDisabled()))
		{
			attacker.RealPower = defender.RealPower + 100;
		}
		if (!defender.IsDisabled())
		{
			Func<ClientCard, List<ClientCard>, bool> value2 = (ClientCard card, List<ClientCard> monsterList) => false;
			if (DefenderInvisbleRule.TryGetValue(defender.Id, out value2) && value2(defender, base.Enemy.GetMonsters()))
			{
				return false;
			}
		}
		if (base.Enemy.GetMonsters().Any((ClientCard monster) => !monster.Equals(defender) && monster.IsCode(32491822) && !monster.IsDisabled() && monster.IsDefense()))
		{
			return false;
		}
		if (defender.OwnTargets.Any((ClientCard card) => card.IsCode(25542642) && !card.IsDisabled()))
		{
			return false;
		}
		if (defender.HasSetcode(4129) && !defender.IsDisabled())
		{
			return false;
		}
		if ((base.Enemy.HasInMonstersZone(28168628, notDisabled: true) && base.Enemy.GetMonsters().Any((ClientCard card) => card.HasSetcode(405))) || base.Enemy.HasInMonstersZone(33652635, notDisabled: true) || base.Enemy.HasInMonstersZone(19153634) || (base.Enemy.HasInMonstersZone(66961194, notDisabled: true) && base.Enemy.GetMonsters().Any((ClientCard card) => card.HasSetcode(221))))
		{
			if (defender.HasPosition(CardPosition.FaceDown))
			{
				return false;
			}
			if (base.Enemy.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.Attack > defender.Attack))
			{
				return false;
			}
		}
		if (base.Enemy.HasInSpellZone(29477860, notDisabled: true) && base.Enemy.HasInMonstersZone(66889139) && !defender.IsCode(66889139))
		{
			return false;
		}
		if (base.Enemy.HasInSpellZone(55312487, notDisabled: true) && base.Enemy.GetMonsters().Any((ClientCard card) => card.HasSetcode(278) && card.HasType(CardType.Link)) && !defender.HasType(CardType.Link))
		{
			return false;
		}
		if (defender.IsCode(37617348) && !defender.IsDisabled() && base.Enemy.GetMonsters().Any((ClientCard monster) => monster.HasSetcode(395) && !monster.IsCode(37617348)))
		{
			return false;
		}
		if (base.Enemy.HasInSpellZone(98477480, notDisabled: true) && base.Enemy.HasInMonstersZone(25801745, notDisabled: false, hasXyzMaterial: false, faceUp: true) && base.Enemy.GetMonsters().Any((ClientCard card) => card.HasType(CardType.Ritual) && card.IsFaceup()) && !defender.HasType(CardType.Ritual))
		{
			return false;
		}
		return true;
	}

	public override bool OnPreActivate(ClientCard card)
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && base.Duel.Phase == DuelPhase.Standby && lastChainCard.IsCode(33015627, 6616912, 7733560, 28929131, 34137269, 60222213, 65314286, 74530899, 91712985, 92435533))
		{
			return false;
		}
		if ((card.Location == CardLocation.Hand || (card.Location == CardLocation.SpellZone && card.IsFacedown())) && ((card.IsSpell() && DefaultSpellWillBeNegated()) || (card.IsTrap() && DefaultTrapWillBeNegated())))
		{
			return false;
		}
		return true;
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (namedCard != null && namedCard.Attack == 0)
		{
			return CardPosition.FaceUpDefence;
		}
		return (CardPosition)0;
	}

	public override bool OnSelectBattleReplay()
	{
		if (base.Bot.BattlingMonster == null)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>(base.Duel.Fields[1].GetMonsters());
		list.Sort(CardContainer.CompareDefensePower);
		list.Reverse();
		BattlePhaseAction battlePhaseAction = OnSelectAttackTarget(base.Bot.BattlingMonster, list);
		if (battlePhaseAction != null && battlePhaseAction.Action == BattlePhaseAction.BattleAction.Attack)
		{
			return true;
		}
		return false;
	}

	public override bool OnSelectMonsterSummonOrSet(ClientCard card)
	{
		if (card.Level <= 4 && base.Bot.GetMonsters().Count((ClientCard m) => m.IsFaceup()) == 0)
		{
			return base.Util.IsAllEnemyBetterThanValue(card.Attack, onlyATK: true);
		}
		return false;
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		int num = base.Bot.ExtraDeck.Count / 2;
		if (!cancelable && min == num && max == num && base.Duel.Player == 1 && (base.Duel.Phase == DuelPhase.Main1 || base.Duel.Phase == DuelPhase.Main2) && cards.All((ClientCard card) => card.Controller == 0 && (card.Location == CardLocation.Hand || card.Location == CardLocation.Extra)))
		{
			Logger.DebugWriteLine("Dogmatika Alba Zoa solved");
			List<ClientCard> list = new List<ClientCard>(base.Bot.ExtraDeck);
			int count = list.Count;
			while (count-- > 1)
			{
				int index = Program.Rand.Next(list.Count);
				ClientCard value = list[count];
				list[count] = list[index];
				list[index] = value;
			}
			return base.Util.CheckSelectCount(list, cards, min, max);
		}
		return null;
	}

	public override void OnPlayerHint(int player, int hintType, int description)
	{
		base.OnPlayerHint(player, hintType, description);
		if (player == 0 || player == 1)
		{
			ClientField clientField = ((player == 0) ? base.Bot : base.Enemy);
			switch (hintType)
			{
			case 6:
				clientField.HintDescriptions.Add(description);
				break;
			case 7:
				clientField.HintDescriptions.Remove(description);
				break;
			}
		}
	}

	public override void OnHintZone(int player, int zone)
	{
		base.OnHintZone(player, zone);
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo == null || !currentSolvingChainInfo.IsCode(10045474))
		{
			return;
		}
		for (int i = 0; i <= 4; i++)
		{
			if ((zone & (256 << i)) != 0)
			{
				if (currentSolvingChainInfo.ActivatePlayer == 0)
				{
					infiniteImpermanenceNegatedColumns.Add(i);
				}
				else
				{
					infiniteImpermanenceNegatedColumns.Add(4 - i);
				}
			}
		}
	}

	public override void OnReceivingAnnouce(int player, int data)
	{
		if ((player == 1 && data == base.Util.GetStringId(14532163, 0)) || data == base.Util.GetStringId(14532163, 1))
		{
			lightningStormOption = data - base.Util.GetStringId(14532163, 0);
		}
		base.OnReceivingAnnouce(player, data);
	}

	public override void OnChainSolved(int chainIndex)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo == null || base.Duel.IsCurrentSolvingChainNegated())
		{
			return;
		}
		if (currentSolvingChainInfo.IsActivateCode(94145021))
		{
			resolvedEffectIdList.Add(94145021);
		}
		if (currentSolvingChainInfo.ActivatePlayer != 1)
		{
			return;
		}
		if (currentSolvingChainInfo.IsActivateCode(23434538))
		{
			enemyResolvedEffectIdList.Add(23434538);
		}
		if (currentSolvingChainInfo.IsActivateCode(84192580))
		{
			enemyResolvedEffectIdList.Add(84192580);
		}
		if (currentSolvingChainInfo.IsActivateCode(42141493))
		{
			enemyResolvedEffectIdList.Add(42141493);
		}
		if (currentSolvingChainInfo.IsActivateCode(87126721))
		{
			enemyResolvedEffectIdList.Add(87126721);
		}
		if (currentSolvingChainInfo.IsActivateCode(4227096))
		{
			if (base.Duel.Player == 1)
			{
				mistakenArrestAffectedCount = Math.Max(mistakenArrestAffectedCount, 3);
			}
			else
			{
				mistakenArrestAffectedCount = Math.Max(mistakenArrestAffectedCount, 2);
			}
		}
	}

	public override void OnChainEnd()
	{
		lightningStormOption = -1;
		base.OnChainEnd();
	}

	public override void OnNewTurn()
	{
		infiniteImpermanenceNegatedColumns.Clear();
		resolvedEffectIdList.Clear();
		enemyResolvedEffectIdList.Clear();
		if (base.Duel.Turn <= 1)
		{
			calledbytheGraveIdCountMap.Clear();
			mistakenArrestAffectedCount = 0;
		}
		mistakenArrestAffectedCount = Math.Max(mistakenArrestAffectedCount - 1, 0);
		foreach (int item in calledbytheGraveIdCountMap.Keys.ToList())
		{
			if (calledbytheGraveIdCountMap[item] > 0)
			{
				calledbytheGraveIdCountMap[item]--;
			}
		}
		crossoutDesignatorIdList.Clear();
		base.OnNewTurn();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (card != null)
		{
			ClientCard currentSolvingChainCard = base.Duel.GetCurrentSolvingChainCard();
			if (currentSolvingChainCard != null && currentLocation == 32)
			{
				int num = card.Id;
				if (card.Data != null)
				{
					num = ((card.Data.Alias <= 0) ? card.Id : card.Data.Alias);
				}
				if (currentSolvingChainCard.IsCode(24224830))
				{
					calledbytheGraveIdCountMap[num] = 2;
				}
				if (currentSolvingChainCard.IsCode(65681983))
				{
					crossoutDesignatorIdList.Add(num);
				}
			}
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	protected bool DefaultMysticalSpaceTyphoon()
	{
		if (base.Duel.CurrentChain.Any((ClientCard card) => card.IsCode(5318639)))
		{
			return false;
		}
		List<ClientCard> spells = base.Enemy.GetSpells();
		if (spells.Count == 0)
		{
			return false;
		}
		ClientCard clientCard = base.Enemy.SpellZone.GetFloodgate();
		if (clientCard == null)
		{
			if (base.Duel.Player == 0)
			{
				clientCard = spells.FirstOrDefault((ClientCard card) => card.IsFacedown());
			}
			if (base.Duel.Player == 1)
			{
				clientCard = spells.FirstOrDefault((ClientCard card) => card.HasType(CardType.Continuous) || card.HasType(CardType.Equip) || card.HasType(CardType.Field));
			}
		}
		if (clientCard == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		return true;
	}

	protected bool DefaultCosmicCyclone()
	{
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item.IsCode(8267140))
			{
				return false;
			}
		}
		if (base.Bot.LifePoints > 1000)
		{
			return DefaultMysticalSpaceTyphoon();
		}
		return false;
	}

	protected bool DefaultGalaxyCyclone()
	{
		List<ClientCard> spells = base.Enemy.GetSpells();
		if (spells.Count == 0)
		{
			return false;
		}
		ClientCard clientCard = null;
		clientCard = ((base.Card.Location != CardLocation.Grave) ? spells.FirstOrDefault((ClientCard card) => card.IsFacedown()) : base.Util.GetBestEnemySpell(onlyFaceup: true));
		if (clientCard == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		return true;
	}

	protected bool DefaultBookOfMoon()
	{
		if (base.Util.IsAllEnemyBetter(onlyATK: true))
		{
			ClientCard highestAttackMonster = base.Enemy.GetMonsters().GetHighestAttackMonster(canBeTarget: true);
			if (highestAttackMonster != null && highestAttackMonster.HasType(CardType.Effect) && !highestAttackMonster.HasType(CardType.Link) && (highestAttackMonster.HasType(CardType.Xyz) || highestAttackMonster.Level > 4))
			{
				base.AI.SelectCard(highestAttackMonster);
				return true;
			}
		}
		return false;
	}

	protected bool DefaultCompulsoryEvacuationDevice()
	{
		ClientCard problematicEnemyMonster = base.Util.GetProblematicEnemyMonster(0, canBeTarget: true);
		if (problematicEnemyMonster != null)
		{
			base.AI.SelectCard(problematicEnemyMonster);
			return true;
		}
		if (base.Util.IsChainTarget(base.Card))
		{
			ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster(onlyFaceup: false, canBeTarget: true);
			if (bestEnemyMonster != null)
			{
				base.AI.SelectCard(bestEnemyMonster);
				return true;
			}
		}
		return false;
	}

	protected bool DefaultCallOfTheHaunted()
	{
		if (!base.Util.IsAllEnemyBetter(onlyATK: true))
		{
			return false;
		}
		ClientCard card = (from clientCard in base.Bot.Graveyard.GetMatchingCards((ClientCard clientCard) => clientCard.IsCanRevive())
			orderby clientCard.Attack descending
			select clientCard).FirstOrDefault();
		base.AI.SelectCard(card);
		return true;
	}

	protected bool DefaultScapegoat()
	{
		if (DefaultSpellWillBeNegated())
		{
			return false;
		}
		if (base.Duel.Player == 0)
		{
			return false;
		}
		if (base.Duel.Phase == DuelPhase.End)
		{
			return true;
		}
		if (DefaultOnBecomeTarget())
		{
			return true;
		}
		if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)
		{
			if (base.Enemy.HasInMonstersZone(new int[5] { 18940556, 12307878, 51788412, 12652643, 70902743 }, notDisabled: true))
			{
				return false;
			}
			if (base.Util.GetTotalAttackingMonsterAttack(1) >= base.Bot.LifePoints)
			{
				return true;
			}
		}
		return false;
	}

	protected bool DefaultMaxxC()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		return base.Duel.Player == 1;
	}

	protected bool DefaultAshBlossomAndJoyousSpring()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		int[] ids = new int[4] { 30241314, 70368879, 60600126, 64734921 };
		if (base.Util.GetLastChainCard().IsCode(ids))
		{
			return false;
		}
		if (base.Util.GetLastChainCard().HasSetcode(286) && base.Util.GetLastChainCard().Location == CardLocation.Hand)
		{
			return false;
		}
		return base.Duel.LastChainPlayer == 1;
	}

	protected bool DefaultGhostOgreAndSnowRabbit()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Util.GetLastChainCard() != null && base.Util.GetLastChainCard().IsDisabled())
		{
			return false;
		}
		return DefaultTrap();
	}

	protected bool DefaultGhostBelleAndHauntedMansion()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		return DefaultTrap();
	}

	protected bool DefaultEffectVeiler()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && ((lastChainCard.IsCode(46659709) && base.Enemy.Hand.Count >= 3) || lastChainCard.IsCode(97268402, 10045474)))
		{
			return false;
		}
		return DefaultBreakthroughSkill();
	}

	protected bool DefaultCalledByTheGrave()
	{
		int[] array = new int[7] { 23434538, 94145021, 59438930, 14558127, 73642296, 97268402, 34267821 };
		if (base.Duel.LastChainPlayer == 1)
		{
			int[] array2 = array;
			foreach (int num in array2)
			{
				if (base.Util.GetLastChainCard().IsCode(num))
				{
					base.AI.SelectCard(num);
					return UniqueFaceupSpell();
				}
			}
		}
		return false;
	}

	protected bool DefaultInfiniteImpermanence()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && ((lastChainCard.IsCode(46659709) && base.Enemy.Hand.Count >= 3) || lastChainCard.IsCode(97268402, 10045474)))
		{
			return false;
		}
		return DefaultDisableMonster();
	}

	protected bool DefaultBreakthroughSkill()
	{
		if (!DefaultUniqueTrap())
		{
			return false;
		}
		return DefaultDisableMonster();
	}

	protected bool DefaultDisableMonster()
	{
		if (base.Duel.Player == 1)
		{
			ClientCard shouldBeDisabledBeforeItUseEffectMonster = base.Enemy.MonsterZone.GetShouldBeDisabledBeforeItUseEffectMonster();
			if (shouldBeDisabledBeforeItUseEffectMonster != null)
			{
				base.AI.SelectCard(shouldBeDisabledBeforeItUseEffectMonster);
				return true;
			}
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone && !lastChainCard.IsDisabled() && !lastChainCard.IsShouldNotBeTarget() && !lastChainCard.IsShouldNotBeSpellTrapTarget())
		{
			base.AI.SelectCard(lastChainCard);
			return true;
		}
		if (base.Bot.BattlingMonster != null && base.Enemy.BattlingMonster != null && !base.Enemy.BattlingMonster.IsDisabled() && base.Enemy.BattlingMonster.IsCode(63845230))
		{
			base.AI.SelectCard(base.Enemy.BattlingMonster);
			return true;
		}
		if (base.Duel.Phase == DuelPhase.BattleStart && base.Duel.Player == 1 && base.Enemy.HasInMonstersZone(56832966, notDisabled: true))
		{
			base.AI.SelectCard(56832966);
			return true;
		}
		return false;
	}

	protected bool DefaultSolemnJudgment()
	{
		if (!base.Util.IsChainTargetOnly(base.Card) && (base.Duel.Player != 0 || base.Duel.LastChainPlayer != -1) && !DefaultOnlyHorusSpSummoning())
		{
			return DefaultTrap();
		}
		return false;
	}

	protected bool DefaultSolemnWarning()
	{
		if (base.Bot.LifePoints > 2000 && (base.Duel.Player != 0 || base.Duel.LastChainPlayer != -1) && !DefaultOnlyHorusSpSummoning())
		{
			return DefaultTrap();
		}
		return false;
	}

	protected bool DefaultSolemnStrike()
	{
		if (base.Bot.LifePoints > 1500 && (base.Duel.Player != 0 || base.Duel.LastChainPlayer != -1) && !DefaultOnlyHorusSpSummoning())
		{
			return DefaultTrap();
		}
		return false;
	}

	protected bool DefaultOnlyHorusSpSummoning()
	{
		if (base.Duel.SummoningCards.Count != 0)
		{
			bool flag = false;
			foreach (ClientCard summoningCard in base.Duel.SummoningCards)
			{
				if (!summoningCard.HasSetcode(413) || summoningCard.LastLocation != CardLocation.Grave)
				{
					flag = true;
					break;
				}
			}
			return !flag;
		}
		return false;
	}

	protected bool DefaultTorrentialTribute()
	{
		if (!base.Util.HasChainedTrap(0))
		{
			return base.Util.IsAllEnemyBetter(onlyATK: true);
		}
		return false;
	}

	protected bool DefaultHeavyStorm()
	{
		return base.Bot.GetSpellCount() < base.Enemy.GetSpellCount();
	}

	protected bool DefaultHarpiesFeatherDusterFirst()
	{
		return base.Enemy.GetSpellCount() >= 2;
	}

	protected bool DefaultHammerShot()
	{
		return base.Util.IsOneEnemyBetter(onlyATK: true);
	}

	protected bool DefaultDarkHole()
	{
		return base.Util.IsOneEnemyBetter();
	}

	protected bool DefaultRaigeki()
	{
		return base.Util.IsOneEnemyBetter();
	}

	protected bool DefaultSmashingGround()
	{
		return base.Util.IsOneEnemyBetter();
	}

	protected bool DefaultPotOfDesires()
	{
		return base.Bot.Deck.Count > 15;
	}

	protected bool DefaultSpellSet()
	{
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay) || DefaultSpellMustSetFirst())
		{
			return base.Bot.GetSpellCountWithoutField() < 4;
		}
		return false;
	}

	protected bool DefaultMonsterSummon()
	{
		if (base.Card.Level <= 4)
		{
			return true;
		}
		if (!UniqueFaceupMonster())
		{
			return false;
		}
		int num = (int)Math.Ceiling(((double)base.Card.Level - 4.0) / 2.0);
		for (int i = 0; i < 7; i++)
		{
			ClientCard clientCard = base.Bot.MonsterZone[i];
			if (clientCard != null && clientCard.GetDefensePower() < base.Card.Attack)
			{
				num--;
			}
		}
		return num <= 0;
	}

	protected bool DefaultField()
	{
		return base.Bot.SpellZone[5] == null;
	}

	protected bool DefaultMonsterRepos()
	{
		if (base.Card.IsMonsterInvincible())
		{
			return base.Card.IsDefense();
		}
		if (base.Card.Attack == 0)
		{
			if (base.Card.IsFaceup() && base.Card.IsAttack())
			{
				return true;
			}
			if (base.Card.IsFaceup() && base.Card.IsDefense())
			{
				return false;
			}
		}
		if (base.Enemy.HasInMonstersZone(55410871, notDisabled: true) && base.Card.IsAttack() && (4000 - base.Card.Defense) * 2 > 4000 - base.Card.Attack)
		{
			return false;
		}
		if (base.Enemy.HasInMonstersZone(55410871, notDisabled: true) && base.Card.IsDefense() && base.Card.IsFaceup() && (4000 - base.Card.Defense) * 2 > 4000 - base.Card.Attack)
		{
			return true;
		}
		bool flag = base.Util.IsAllEnemyBetter();
		if (base.Card.IsAttack() && flag)
		{
			return true;
		}
		if (base.Card.IsDefense() && !flag && (base.Card.Attack >= base.Card.Defense || base.Card.Attack >= base.Util.GetBestPower(base.Enemy)))
		{
			return true;
		}
		return false;
	}

	protected bool DefaultSpellWillBeNegated()
	{
		if ((!base.Bot.HasInSpellZone(61740673, notDisabled: true, faceUp: true) && !base.Enemy.HasInSpellZone(61740673, notDisabled: true)) || base.Util.ChainContainsCard(61740673))
		{
			return DefaultCheckWhetherCardIsNegated(base.Card);
		}
		return true;
	}

	protected bool DefaultTrapWillBeNegated()
	{
		if ((!base.Bot.HasInSpellZone(51452091, notDisabled: true, faceUp: true) && !base.Enemy.HasInSpellZone(51452091, notDisabled: true)) || base.Util.ChainContainsCard(51452091))
		{
			return DefaultCheckWhetherCardIsNegated(base.Card);
		}
		return true;
	}

	protected bool DefaultSpellMustSetFirst()
	{
		if (!base.Bot.HasInSpellZone(58921041, notDisabled: true, faceUp: true))
		{
			return base.Enemy.HasInSpellZone(58921041, notDisabled: true);
		}
		return true;
	}

	protected bool DefaultOnBecomeTarget()
	{
		if (base.Util.IsChainTarget(base.Card))
		{
			return true;
		}
		int[] ids = new int[6] { 46772449, 73580471, 57774843, 72529749, 15693423, 90448279 };
		int[] ids2 = new int[2] { 53129443, 99330325 };
		int[] destroyAllOpponentMonsterList = new int[1] { 12580477 };
		int[] cardId = new int[2] { 18144506, 2314238 };
		if (base.Util.ChainContainsCard(ids))
		{
			return true;
		}
		if (base.Enemy.HasInSpellZone(cardId, notDisabled: true) && base.Card.Location == CardLocation.SpellZone)
		{
			return true;
		}
		if (base.Util.ChainContainsCard(ids2) && base.Card.Location == CardLocation.MonsterZone)
		{
			return true;
		}
		if (base.Duel.CurrentChain.Any((ClientCard c) => c.Controller == 1 && c.IsCode(destroyAllOpponentMonsterList)) && base.Card.Location == CardLocation.MonsterZone)
		{
			return true;
		}
		if (lightningStormOption == 0 && base.Card.Location == CardLocation.MonsterZone && base.Card.IsAttack())
		{
			return true;
		}
		if (lightningStormOption == 1 && base.Card.Location == CardLocation.SpellZone)
		{
			return true;
		}
		return false;
	}

	protected bool DefaultTrap()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Duel.LastChainPlayer != -1 || base.Duel.LastSummonPlayer == 0)
		{
			return base.Duel.LastChainPlayer == 1;
		}
		return true;
	}

	protected bool DefaultUniqueTrap()
	{
		if (base.Util.HasChainedTrap(0))
		{
			return false;
		}
		return UniqueFaceupSpell();
	}

	protected bool UniqueFaceupSpell()
	{
		return !base.Bot.GetSpells().Any((ClientCard card) => card.IsCode(base.Card.Id) && card.IsFaceup());
	}

	protected bool UniqueFaceupMonster()
	{
		return !base.Bot.GetMonsters().Any((ClientCard card) => card.IsCode(base.Card.Id) && card.IsFaceup());
	}

	protected bool DefaultDontChainMyself()
	{
		if (base.Type != ExecutorType.Activate)
		{
			return true;
		}
		if (base.Executors.Any((CardExecutor exec) => exec.Type == base.Type && exec.CardId == base.Card.Id))
		{
			return false;
		}
		return base.Duel.LastChainPlayer != 0;
	}

	protected bool DefaultChickenGame()
	{
		if (base.Executors.Count((CardExecutor exec) => exec.Type == base.Type && exec.CardId == base.Card.Id) > 1)
		{
			return false;
		}
		if (base.Card.IsFacedown())
		{
			return true;
		}
		if (base.Bot.LifePoints <= 1000)
		{
			return false;
		}
		if (base.Bot.LifePoints <= base.Enemy.LifePoints && base.ActivateDescription == base.Util.GetStringId(67616300, 0))
		{
			return true;
		}
		if (base.Bot.LifePoints > base.Enemy.LifePoints && base.ActivateDescription == base.Util.GetStringId(67616300, 1))
		{
			return true;
		}
		return false;
	}

	protected bool DefaultAllureofDarkness()
	{
		return base.Bot.Hand.FirstOrDefault((ClientCard card) => card.HasAttribute(CardAttribute.Dark)) != null;
	}

	protected bool DefaultDimensionalBarrier()
	{
		if (base.Duel.Player != 0)
		{
			List<ClientCard> monsters = base.Enemy.GetMonsters();
			int[] array = new int[13];
			bool flag = false;
			bool flag2 = false;
			foreach (ClientCard item in monsters)
			{
				if (!item.HasType((CardType)75497472))
				{
					if (item.HasType(CardType.Tuner))
					{
						flag = true;
					}
					else
					{
						flag2 = true;
					}
					if (!item.HasType(CardType.Token))
					{
						array[item.Level] = array[item.Level] + 1;
					}
				}
				if (item.IsOneForXyz())
				{
					base.AI.SelectOption(3);
					return true;
				}
			}
			if (flag && flag2)
			{
				base.AI.SelectOption(2);
				return true;
			}
			for (int i = 1; i <= 12; i++)
			{
				if (array[i] > 1)
				{
					base.AI.SelectOption(3);
					return true;
				}
			}
			ClientCard clientCard = base.Enemy.SpellZone[6];
			ClientCard clientCard2 = base.Enemy.SpellZone[7];
			if (clientCard != null && clientCard2 != null && clientCard.LScale != clientCard2.RScale)
			{
				base.AI.SelectOption(4);
				return true;
			}
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Duel.LastChainPlayer == 1 && lastChainCard != null && !lastChainCard.IsDisabled() && (lastChainCard.HasType((CardType)6) || lastChainCard.Location == CardLocation.MonsterZone))
		{
			if (lastChainCard.HasType(CardType.Ritual))
			{
				base.AI.SelectOption(0);
				return true;
			}
			if (lastChainCard.HasType(CardType.Fusion))
			{
				base.AI.SelectOption(1);
				return true;
			}
			if (lastChainCard.HasType(CardType.Synchro))
			{
				base.AI.SelectOption(2);
				return true;
			}
			if (lastChainCard.HasType(CardType.Xyz))
			{
				base.AI.SelectOption(3);
				return true;
			}
			if (lastChainCard.IsFusionSpell())
			{
				base.AI.SelectOption(1);
				return true;
			}
		}
		if (base.Util.IsChainTarget(base.Card))
		{
			base.AI.SelectOption(3);
			return true;
		}
		return false;
	}

	protected bool DefaultInterruptedKaijuSlumber()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			base.AI.SelectCard(55063751, 29726552, 36956512, 28674152, 93332803, 48770333, 63941210);
			return true;
		}
		if (DefaultDarkHole())
		{
			base.AI.SelectCard(63941210, 48770333, 93332803, 28674152, 36956512, 29726552, 55063751);
			base.AI.SelectNextCard(84769941, 55063751, 29726552, 36956512, 28674152, 93332803, 48770333);
			return true;
		}
		return false;
	}

	protected bool DefaultKaijuSpsummon()
	{
		IList<int> ids = new int[8] { 63941210, 36956512, 55063751, 28674152, 29726552, 48770333, 93332803, 84769941 };
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (monster.IsCode(ids))
			{
				return base.Card.GetDefensePower() > monster.GetDefensePower();
			}
		}
		ClientCard floodgate = base.Enemy.MonsterZone.GetFloodgate();
		if (floodgate != null)
		{
			base.AI.SelectCard(floodgate);
			return true;
		}
		floodgate = base.Enemy.MonsterZone.GetDangerousMonster();
		if (floodgate != null)
		{
			base.AI.SelectCard(floodgate);
			return true;
		}
		floodgate = base.Util.GetOneEnemyBetterThanValue(base.Card.GetDefensePower());
		if (floodgate != null)
		{
			base.AI.SelectCard(floodgate);
			return true;
		}
		return false;
	}

	protected bool DefaultNumberS39UtopiaTheLightningSummon()
	{
		int bestAttack = base.Util.GetBestAttack(base.Bot);
		return base.Util.IsOneEnemyBetterThanValue(bestAttack, onlyATK: false);
	}

	protected bool DefaultNumberS39UtopiaTheLightningEffect()
	{
		if (base.Card.IsAttack() && base.Card.Attack < 5000)
		{
			if (!base.Enemy.BattlingMonster.IsAttack() && !base.Enemy.BattlingMonster.IsFacedown())
			{
				return base.Enemy.BattlingMonster.GetDefensePower() >= base.Card.Attack;
			}
			return true;
		}
		return false;
	}

	protected bool DefaultEvilswarmExcitonKnightSummon()
	{
		int num = base.Bot.GetMonsterCount() + base.Bot.GetSpellCount() + base.Bot.GetHandCount();
		int num2 = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount() + base.Enemy.GetHandCount();
		if (num - 1 < num2)
		{
			return DefaultEvilswarmExcitonKnightEffect();
		}
		return false;
	}

	protected bool DefaultEvilswarmExcitonKnightEffect()
	{
		int num = base.Bot.GetMonsterCount() + base.Bot.GetSpellCount();
		int num2 = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount();
		if (num < num2)
		{
			return true;
		}
		int valueOrDefault = ((IEnumerable<ClientCard>)base.Bot.GetMonsters()).Sum((Func<ClientCard, int?>)((ClientCard monster) => monster.GetDefensePower())).GetValueOrDefault();
		int valueOrDefault2 = ((IEnumerable<ClientCard>)base.Enemy.GetMonsters()).Sum((Func<ClientCard, int?>)((ClientCard monster) => monster.GetDefensePower())).GetValueOrDefault();
		return valueOrDefault < valueOrDefault2;
	}

	protected bool DefaultStardustDragonSummon()
	{
		int bestAttack = base.Util.GetBestAttack(base.Bot);
		int bestPower = base.Util.GetBestPower(base.Enemy);
		if (bestAttack > bestPower || bestPower > 2500)
		{
			return base.Util.IsTurn1OrMain2();
		}
		return true;
	}

	protected bool DefaultStardustDragonEffect()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return base.Duel.LastChainPlayer == 1;
		}
		return true;
	}

	protected bool DefaultCastelTheSkyblasterMusketeerSummon()
	{
		return base.Util.GetProblematicEnemyCard() != null;
	}

	protected bool DefaultCastelTheSkyblasterMusketeerEffect()
	{
		if (base.ActivateDescription == base.Util.GetStringId(82633039, 0))
		{
			return false;
		}
		ClientCard problematicEnemyCard = base.Util.GetProblematicEnemyCard();
		if (problematicEnemyCard != null)
		{
			base.AI.SelectCard(0);
			base.AI.SelectNextCard(problematicEnemyCard);
			return true;
		}
		return false;
	}

	protected bool DefaultScarlightRedDragonArchfiendSummon()
	{
		int bestAttack = base.Util.GetBestAttack(base.Bot);
		int bestPower = base.Util.GetBestPower(base.Enemy);
		if (bestAttack > bestPower || bestPower > 3000)
		{
			return DefaultScarlightRedDragonArchfiendEffect();
		}
		return true;
	}

	protected bool DefaultTimelordSummon()
	{
		return base.Bot.GetMonsterCount() == 0;
	}

	protected bool DefaultScarlightRedDragonArchfiendEffect()
	{
		int num = base.Bot.GetMonsters().Count((ClientCard monster) => !monster.Equals(base.Card) && monster.IsSpecialSummoned && monster.HasType(CardType.Effect) && monster.Attack <= base.Card.Attack);
		int num2 = base.Enemy.GetMonsters().Count((ClientCard monster) => monster.IsSpecialSummoned && monster.HasType(CardType.Effect) && monster.Attack <= base.Card.Attack);
		if (num > num2 || num2 <= 0)
		{
			return num2 >= 3;
		}
		return true;
	}

	protected bool DefaultHonestEffect()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (DefaultCheckWhetherCardIsNegated(base.Card))
			{
				return false;
			}
			if (base.Bot.BattlingMonster.IsAttack())
			{
				if (base.Bot.BattlingMonster.Attack >= base.Enemy.BattlingMonster.Attack && base.Bot.BattlingMonster.Attack < base.Enemy.LifePoints)
				{
					if (base.Bot.BattlingMonster.Attack < base.Enemy.BattlingMonster.Defense)
					{
						return base.Bot.BattlingMonster.Attack + base.Enemy.BattlingMonster.Attack > base.Enemy.BattlingMonster.Defense;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return base.Util.IsTurn1OrMain2();
	}

	protected bool DefaultVaylantzWorld_ShinraBansho()
	{
		if (DefaultSpellWillBeNegated())
		{
			return false;
		}
		return true;
	}

	protected bool DefaultVaylantzWorld_KonigWissen()
	{
		if (DefaultSpellWillBeNegated())
		{
			return false;
		}
		List<ClientCard> monsters = base.Enemy.GetMonsters();
		if (monsters.Count == 0)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> collection = (from card in monsters
			where card?.Data != null && card.IsFloodgate() && card.IsFaceup() && !card.IsShouldNotBeTarget()
			orderby card.Attack descending
			select card).ToList();
		List<ClientCard> collection2 = (from card in monsters
			where card?.Data != null && card.IsMonsterDangerous() && card.IsFaceup() && !card.IsShouldNotBeTarget()
			orderby card.Attack descending
			select card).ToList();
		List<ClientCard> collection3 = (from card in monsters
			where card?.Data != null && card.HasType(CardType.Monster) && card.IsFaceup() && card.IsShouldNotBeTarget()
			orderby card.Attack descending
			select card).ToList();
		list.AddRange(collection);
		list.AddRange(collection2);
		list.AddRange(collection3);
		if (list != null && list.Count > 0)
		{
			base.AI.SelectCard(list);
			return true;
		}
		return false;
	}

	protected bool DefaultCheckWhetherCardIsNegated(ClientCard card)
	{
		if (card == null)
		{
			return true;
		}
		if (card.Data == null)
		{
			return card.IsDisabled();
		}
		int num = card.Data.Alias;
		if (num == 0)
		{
			num = card.Data.Id;
		}
		if (!crossoutDesignatorIdList.Contains(num) && (!calledbytheGraveIdCountMap.ContainsKey(num) || calledbytheGraveIdCountMap[num] <= 0))
		{
			if (card.IsDisabled())
			{
				return (card.Location & CardLocation.Onfield) > (CardLocation)0;
			}
			return false;
		}
		return true;
	}

	protected bool DefaultCheckWhetherCardIdIsNegated(int cardId)
	{
		if (!crossoutDesignatorIdList.Contains(cardId))
		{
			if (calledbytheGraveIdCountMap.ContainsKey(cardId))
			{
				return calledbytheGraveIdCountMap[cardId] > 0;
			}
			return false;
		}
		return true;
	}

	protected int GetCalledbytheGraveIdCount(int cardId)
	{
		if (!calledbytheGraveIdCountMap.ContainsKey(cardId))
		{
			return 0;
		}
		return calledbytheGraveIdCountMap[cardId];
	}

	protected virtual bool DefaultSetForDiabellze()
	{
		if (base.Card == null)
		{
			return false;
		}
		if (base.Card.Id == 49238328)
		{
			return false;
		}
		if (base.Enemy.HasInMonstersZone(53765052, notDisabled: true, hasXyzMaterial: false, faceUp: true) && base.Card.HasType(CardType.Spell) && !base.Card.HasType(CardType.QuickPlay))
		{
			if (base.Bot.SpellZone.Any((ClientCard c) => c != null && base.Duel.MainPhase.ActivableCards.Contains(c) && c.HasType(CardType.Spell) && !base.Card.HasType(CardType.QuickPlay) && c.IsFacedown()))
			{
				return false;
			}
			foreach (CardExecutor executor in base.Executors)
			{
				if (executor.Type == ExecutorType.Activate && executor.CardId == base.Card.Id && (executor.Func == null || executor.Func()))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected bool DefaultCheckAllAvailableSpellColumnNegated()
	{
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.SpellZone[i] == null && !infiniteImpermanenceNegatedColumns.Contains(i))
			{
				return false;
			}
		}
		return true;
	}

	protected bool DefaultCheckWhetherSpellActivateWillBeNegated(ClientCard card)
	{
		if (card == null)
		{
			return false;
		}
		if (card.Location == CardLocation.SpellZone)
		{
			return infiniteImpermanenceNegatedColumns.Contains(card.Sequence);
		}
		return DefaultCheckAllAvailableSpellColumnNegated();
	}

	protected bool DefaultCheckWhetherBotCanSearch()
	{
		if (resolvedEffectIdList.Contains(94145021))
		{
			return false;
		}
		if (mistakenArrestAffectedCount > 0)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(71564252, notDisabled: true, hasXyzMaterial: false, faceUp: true) || base.Enemy.HasInMonstersZone(71564252, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			return false;
		}
		if (base.Enemy.HasInMonstersZone(15291624))
		{
			return false;
		}
		if (base.Bot.HasInSpellZone(1149109, notDisabled: true, faceUp: true) || base.Enemy.HasInSpellZone(1149109, notDisabled: true, faceUp: true) || base.Bot.HasInSpellZone(59305593, notDisabled: true, faceUp: true) || base.Enemy.HasInSpellZone(59305593, notDisabled: true, faceUp: true))
		{
			return false;
		}
		if (base.Enemy.HasInSpellZone(80320877, notDisabled: true, faceUp: true))
		{
			return false;
		}
		return true;
	}

	protected bool DefaultCheckWhetherEnemyCanDraw()
	{
		if (resolvedEffectIdList.Contains(94145021))
		{
			return false;
		}
		return true;
	}
}
