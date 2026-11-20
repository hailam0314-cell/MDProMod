using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Dogmatika", "AI_Dogmatika", "Normal")]
public class DogmatikaExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int DogmatikaAlbaZoa = 51522296;

		public const int ThesIrisSwordsoul = 62849088;

		public const int DogmatikaFleurdelis = 69680031;

		public const int DogmatikaMaximus = 95679145;

		public const int DiabellstarTheBlackWitch = 72270339;

		public const int DogmatikaEcclesia = 60303688;

		public const int KnightmareCorruptorIblee = 10158145;

		public const int NadirServant = 1984618;

		public const int DogmatikaLamity = 31002402;

		public const int DogmatikaMacabre = 60921537;

		public const int SinfulSpoilsOfDoom_Rciela = 16240772;

		public const int WANTED_SeekerOfSinfulSpoils = 80845034;

		public const int DogmatikaMatrix = 35569555;

		public const int DogmatikaPunishment = 82956214;

		public const int GranguignolTheDuskDragon = 24915933;

		public const int TitanikladTheAshDragon = 41373230;

		public const int GaruraWingsOfResonantLife = 11765832;

		public const int ElderEntityNtss = 80532587;

		public const int DespianLuluwalilith = 53971455;

		public const int PSYFramelordOmega = 74586817;

		public const int HeraldOfTheArcLight = 79606837;

		public const int SuperStarslayerTYPHON = 93039339;

		public const int SPLittleKnight = 29301450;

		public const int SecureGardna = 2220237;

		public const int Linguriboh = 24842059;

		public const int SalamangreatAlmiraj = 60303245;

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

		public const int GhostMournerMoonlitChill = 52038441;
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodePhantom = 219;

	private const int SetcodeOrcust = 283;

	private const int SetcodeDogmatika = 325;

	private const int hintTimingMainEnd = 4;

	private const int hintDamageStep = 8192;

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 60303688, 14558127, 23434538, 10158145, 1984618, 80845034, 35569555, 10045474, 82956214 }
		},
		{
			2,
			new List<int> { 51522296, 69680031, 24224830 }
		},
		{
			1,
			new List<int> { 62849088, 95679145, 72270339, 31002402, 60921537, 16240772, 65681983 }
		}
	};

	private List<int> notToNegateIdList = new List<int> { 58699500, 20343502 };

	private List<int> discardEnemyExtraIdList = new List<int> { 90448279, 29301450, 90590303, 70534340, 60465049, 24094258, 86066372 };

	private List<int> currentNegatingIdList = new List<int>();

	private bool enemyActivateMaxxC;

	private bool enemyActivateLockBird;

	private List<int> infiniteImpermanenceList = new List<int>();

	private bool summoned;

	private List<int> activatedCardIdList = new List<int>();

	private List<ClientCard> currentNegateMonsterList = new List<ClientCard>();

	private List<ClientCard> currentDestroyCardList = new List<ClientCard>();

	private List<int> discardExtraThisTurn = new List<int>();

	private int banSpSummonFromExTurn;

	private List<ClientCard> activatedMatrixList = new List<ClientCard>();

	private List<int> maximusDiscardExtraIdList = new List<int>();

	private bool checkedEnemyExtra;

	private bool matrixActivating;

	private bool avoid2Monster = true;

	private bool confirmLink2;

	private int omegaActivateCount;

	private int dimensionShifterCount;

	private int enemySpSummonFromExLastTurn;

	private int enemySpSummonFromExThisTurn;

	private bool enemySpSummonFromDeck;

	private bool enemySpSummonFromExtra;

	public DogmatikaExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 80845034, WANTED_SeekerOfSinfulSpoilsActivate);
		AddExecutor(ExecutorType.Activate, 60303245, SalamangreatAlmirajActivate);
		AddExecutor(ExecutorType.Activate, 74586817, PSYFramelordOmegaActivate);
		AddExecutor(ExecutorType.Activate, 51522296, DogmatikaAlbaZoaActivate);
		AddExecutor(ExecutorType.Activate, 95679145, DogmatikaMaximusActivate);
		AddExecutor(ExecutorType.Activate, 72270339, DiabellstarTheBlackWitchActivate);
		AddExecutor(ExecutorType.Activate, 62849088, ThesIrisSwordsoulActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 24842059, LinguribohActivate);
		AddExecutor(ExecutorType.Activate, 82956214, DogmatikaPunishmentActivate);
		AddExecutor(ExecutorType.Activate, 69680031, DogmatikaFleurdelisActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 16240772, SinfulSpoilsOfDoom_RcielaActivate);
		AddExecutor(ExecutorType.Activate, ClearIrisFlag);
		AddExecutor(ExecutorType.Activate, 79606837, HeraldOfTheArcLightActivate);
		AddExecutor(ExecutorType.Activate, 80532587, ElderEntityNtssActivate);
		AddExecutor(ExecutorType.Activate, 24915933, GranguignolTheDuskDragonActivate);
		AddExecutor(ExecutorType.Activate, 11765832, GaruraWingsOfResonantLifeActivate);
		AddExecutor(ExecutorType.Activate, 10158145, KnightmareCorruptorIbleeActivate);
		AddExecutor(ExecutorType.Activate, 41373230, TitanikladTheAshDragonActivate);
		AddExecutor(ExecutorType.Activate, 53971455, DespianLuluwalilithActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 93039339, SuperStarslayerTYPHONActivate);
		AddExecutor(ExecutorType.SpSummon, 24842059, LinguribohSpSummon);
		AddExecutor(ExecutorType.SpSummon, 60303245, SalamangreatAlmirajSpSummon);
		AddExecutor(ExecutorType.SpSummon, 2220237, SecureGardnaSpSummon);
		AddExecutor(ExecutorType.Summon, 10158145, KnightmareCorruptorIbleeSummon);
		AddExecutor(ExecutorType.Activate, 1984618, NadirServantActivate);
		AddExecutor(ExecutorType.Summon, 60303688, DogmatikaEcclesiaSummon);
		AddExecutor(ExecutorType.Activate, 60303688, DogmatikaEcclesiaActivate);
		AddExecutor(ExecutorType.Activate, 35569555, DogmatikaMatrixActivate);
		AddExecutor(ExecutorType.Activate, 31002402, DogmatikaLamityActivate);
		AddExecutor(ExecutorType.Activate, 60921537, DogmatikaMacabreActivate);
		AddExecutor(ExecutorType.Activate, 69680031, DogmatikaFleurdelisDelayActivate);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
		AddExecutor(ExecutorType.Summon, SummonForTYPHONCheck);
		AddExecutor(ExecutorType.SpSummon, 72270339, DiabellstarTheBlackWitchSpSummon);
		AddExecutor(ExecutorType.Activate, 31002402, DogmatikaLamityDelayActivate);
		AddExecutor(ExecutorType.SpSummon, 93039339, SuperStarslayerTYPHONSpSummon);
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

	public void UpdateBanSpSummonFromExTurn(int newTurn)
	{
		if (base.Duel.Player == 1)
		{
			newTurn--;
		}
		banSpSummonFromExTurn = Math.Max(banSpSummonFromExTurn, newTurn);
	}

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false, bool ignoreCurrentDestroy = false)
	{
		List<ClientCard> list = (from c in base.Enemy.GetMonsters()
			where c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget()) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(c))
			select c).ToList();
		if (list.Count() > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			return list[0];
		}
		List<ClientCard> list2 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterDangerous() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget()) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(c))).ToList();
		if (list2.Count() > 0)
		{
			list2.Sort(CardContainer.CompareCardAttack);
			list2.Reverse();
			return list2[0];
		}
		List<ClientCard> list3 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterInvincible() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget()) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(c))).ToList();
		if (list3.Count() > 0)
		{
			list3.Sort(CardContainer.CompareCardAttack);
			list3.Reverse();
			return list3[0];
		}
		if (attack >= 0)
		{
			if (attack == 0)
			{
				attack = base.Util.GetBestAttack(base.Bot);
			}
			List<ClientCard> list4 = (from card in base.Enemy.MonsterZone.GetMonsters()
				where card.GetDefensePower() >= attack && card.GetDefensePower() > 0 && card.IsAttack() && (!canBeTarget || !card.IsShouldNotBeTarget()) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
				select card).ToList();
			if (list4.Count() > 0)
			{
				list4.Sort(CardContainer.CompareCardAttack);
				list4.Reverse();
				return list4[0];
			}
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

	public ClientCard GetBestEnemyMonster(bool onlyFaceup = false, bool canBeTarget = false, bool ignoreCurrentDestroy = false)
	{
		ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget, ignoreCurrentDestroy);
		if (problematicEnemyMonster != null)
		{
			return problematicEnemyMonster;
		}
		problematicEnemyMonster = (from c in base.Enemy.MonsterZone
			where c?.Data != null && c.HasType(CardType.Monster) && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget()) && (!ignoreCurrentDestroy || currentDestroyCardList.Contains(c))
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
			return ShuffleCardList(list)[0];
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

	public List<ClientCard> GetDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		List<ClientCard> list = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && (card.HasSetcode(283) || card.HasSetcode(219))).ToList();
		List<int> dangerMonsterIdList = new List<int> { 99937011, 63542003, 9411399, 28954097, 30680659, 74586817 };
		list.AddRange(base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => dangerMonsterIdList.Contains(card.Id)));
		return list;
	}

	public List<ClientCard> GetNormalEnemyTargetList(bool canBeTarget = true, bool targetKnightmare = true, bool ignoreCurrentDestroy = false)
	{
		List<ClientCard> targetList = GetProblematicEnemyCardList(canBeTarget);
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card.IsFaceup() && !targetList.Contains(card) && !card.IsCode(10158145) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		targetList.AddRange(list);
		targetList.AddRange(ShuffleCardList((from card in base.Enemy.GetSpells()
			where !ignoreCurrentDestroy || !currentDestroyCardList.Contains(card)
			select card).ToList()));
		targetList.AddRange(ShuffleCardList((from card in base.Enemy.GetMonsters()
			where card.IsFacedown() && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
			select card).ToList()));
		if (targetKnightmare)
		{
			List<ClientCard> collection = (from card in base.Enemy.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card) && card.IsCode(10158145) && (!ignoreCurrentDestroy || !currentDestroyCardList.Contains(card))
				select card).ToList();
			targetList.AddRange(collection);
		}
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

	public List<int> GetNeedSearchRitualCardIdList()
	{
		List<int> list = new List<int>();
		bool flag = !base.Bot.HasInHand(51522296) && CheckRemainInDeck(51522296) > 0;
		int num = base.Bot.Graveyard.Where((ClientCard card) => card?.HasType((CardType)8256) ?? false).Sum((ClientCard c) => new int?(c.Level).GetValueOrDefault());
		bool flag2 = base.Bot.HasInHandOrInSpellZone(31002402) && base.Bot.HasInExtra(53971455) && flag;
		if (base.Bot.HasInHandOrInGraveyard(60921537))
		{
			flag2 = flag2 || (num >= 12 && flag);
		}
		if (flag2)
		{
			list.Add(51522296);
		}
		if (base.Bot.HasInHand(51522296) && base.Bot.HasInExtra(53971455) && !base.Bot.HasInHandOrInSpellZone(31002402) && CheckRemainInDeck(31002402) > 0)
		{
			list.Add(31002402);
		}
		if (base.Bot.HasInHand(51522296) && !base.Bot.HasInHandOrInSpellZone(60921537) && CheckRemainInDeck(60921537) > 0 && num >= 12)
		{
			list.Add(60921537);
		}
		return list;
	}

	public ClientCard GetExtraToDiscard(int baseAtk = 0, ClientCard avoidDestroyEnemyCard = null)
	{
		ClientCard clientCard = null;
		if (baseAtk <= 2500 && base.Bot.HasInExtra(80532587) && CheckCalledbytheGrave(80532587) == 0)
		{
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, targetKnightmare: false);
			if (normalEnemyTargetList.Count() > 0 && (normalEnemyTargetList.Count() != 1 || normalEnemyTargetList[0] != avoidDestroyEnemyCard))
			{
				clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(80532587));
				if (clientCard != null)
				{
					return clientCard;
				}
			}
		}
		if (baseAtk <= 1500 && base.Bot.HasInExtra(11765832) && CheckCalledbytheGrave(11765832) == 0 && !activatedCardIdList.Contains(11765832) && !enemyActivateLockBird)
		{
			clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(11765832));
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		if (baseAtk <= 2500 && base.Bot.HasInExtra(41373230) && CheckCalledbytheGrave(41373230) == 0 && !discardExtraThisTurn.Contains(41373230) && !enemyActivateLockBird && ((!activatedCardIdList.Contains(60303688) && CheckRemainInDeck(60303688) > 0) | (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)) && !base.Bot.HasInHand(69680031) && CheckRemainInDeck(69680031) > 0)))
		{
			clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(41373230));
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		if (baseAtk <= 2500 && base.Bot.HasInExtra(24915933) && (base.Bot.HasInExtra(53971455) | (CheckRemainInDeck(60303688, 69680031, 95679145) > 0)))
		{
			clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(24915933));
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		if (baseAtk <= 600 && base.Bot.HasInExtra(79606837) && !enemyActivateLockBird && GetNeedSearchRitualCardIdList().Count() > 0)
		{
			clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(79606837));
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		if (baseAtk <= 2800 && base.Bot.HasInExtra(74586817))
		{
			clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(74586817));
			if (clientCard != null)
			{
				return clientCard;
			}
		}
		List<ClientCard> list = base.Bot.ExtraDeck.Where((ClientCard card) => card != null && card.Attack >= baseAtk).ToList();
		if (list.Count() > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			return list[0];
		}
		return clientCard;
	}

	public void CheckDeactiveFlag()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && base.Duel.LastChainPlayer == 1)
		{
			if (lastChainCard.IsCode(23434538))
			{
				enemyActivateMaxxC = false;
			}
			if (lastChainCard.IsCode(94145021))
			{
				enemyActivateLockBird = false;
			}
			if (lastChainCard.IsCode(91800273))
			{
				dimensionShifterCount = 0;
			}
			if (lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone)
			{
				currentNegateMonsterList.Add(lastChainCard);
			}
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

	public bool CheckWhetherNegated(bool toFieldCheck = false)
	{
		if ((base.Card.IsSpell() || base.Card.IsTrap()) && CheckSpellWillBeNegate())
		{
			return true;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return true;
		}
		if (base.Card.IsMonster() && (toFieldCheck || base.Card.Location == CardLocation.MonsterZone))
		{
			if ((toFieldCheck || base.Card.IsDefense()) && (base.Enemy.MonsterZone.Any((ClientCard card) => CheckNumber41(card)) || base.Bot.MonsterZone.Any((ClientCard card) => CheckNumber41(card))))
			{
				return true;
			}
			if (base.Enemy.HasInSpellZone(82732705, notDisabled: true, faceUp: true))
			{
				return true;
			}
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
		foreach (int item in new List<int> { 94853057, 61528025, 30241314, 81674782 })
		{
			foreach (ClientField item2 in new List<ClientField> { base.Bot, base.Enemy })
			{
				if (item2.HasInMonstersZone(item, notDisabled: true) || item2.HasInSpellZone(item, notDisabled: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CheckAtAdvantage()
	{
		if (GetProblematicEnemyMonster() == null && (!base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && !card.IsCode(10158145)) || (base.Duel.Player == 0 && base.Duel.Turn == 1)))
		{
			return true;
		}
		return false;
	}

	public bool CheckShouldNoMoreSpSummon()
	{
		if (CheckAtAdvantage() && enemyActivateMaxxC && base.Util.IsTurn1OrMain2())
		{
			return (byte)(0u | (base.Bot.HasInHandOrInSpellZone(82956214) ? 1u : 0u) | (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.Level >= 7 && card.HasRace(CardRace.SpellCaster)) ? 1u : 0u) | ((base.Bot.HasInHand(69680031) && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325))) ? 1u : 0u)) != 0;
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
		if (lastChainCard.IsCode(94145021))
		{
			bool flag = false;
			foreach (int item in new List<int> { 60303688, 1984618 })
			{
				if (base.Bot.HasInHandOrInSpellZone(item) && !activatedCardIdList.Contains(item))
				{
					flag = true;
				}
			}
			if (discardExtraThisTurn.Contains(41373230))
			{
				flag = true;
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool CheckHasExtraOnField(ClientCard exceptCard = null)
	{
		List<ClientCard> monsters = base.Bot.GetMonsters();
		monsters.AddRange(base.Enemy.GetMonsters());
		return monsters.Any((ClientCard card) => card.HasType((CardType)75505728) && card != exceptCard);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override int OnSelectOption(IList<int> options)
	{
		foreach (int item in new List<int>
		{
			base.Util.GetStringId(62849088, 4),
			base.Util.GetStringId(62849088, 2)
		})
		{
			for (int i = 0; i < options.Count(); i++)
			{
				if (options[i] == item)
				{
					return i;
				}
			}
		}
		return base.OnSelectOption(options);
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
			if (base.Duel.Player == 1)
			{
				if (!namedCard.HasType(CardType.Ritual) || namedCard.Defense >= namedCard.Attack || base.Util.IsOneEnemyBetterThanValue(namedCard.Attack, onlyATK: true))
				{
					return CardPosition.FaceUpDefence;
				}
			}
			else if (namedCard.HasType(CardType.Ritual))
			{
				return CardPosition.FaceUpAttack;
			}
			int num = namedCard.Attack;
			if (cardId == 69680031 && !activatedCardIdList.Contains(cardId + 1) && base.Duel.Player == 0)
			{
				num += 500;
			}
			int value = Math.Max(base.Util.GetBestAttack(base.Bot), num);
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
				List<ClientCard> source = (from clientCard9 in base.Bot.GetMonsters()
					where !clientCard9.HasType(CardType.Token)
					select clientCard9).ToList();
				List<ClientCard> collection = source.Where((ClientCard clientCard9) => clientCard9.IsFacedown()).ToList();
				banishList.AddRange(collection);
				List<ClientCard> list = source.Where((ClientCard clientCard9) => !clientCard9.HasType(CardType.Ritual) && !banishList.Contains(clientCard9)).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list);
				List<ClientCard> spells = base.Bot.GetSpells();
				banishList.AddRange(ShuffleCardList(spells));
				List<ClientCard> list2 = source.Where((ClientCard clientCard9) => clientCard9.HasType(CardType.Ritual) && !banishList.Contains(clientCard9)).ToList();
				list2.Sort(CardContainer.CompareCardAttack);
				banishList.AddRange(list2);
				return base.Util.CheckSelectCount(banishList, cards, min, max);
			}
		}
		if (maximusDiscardExtraIdList.Count() > 0 && min == 1 && max == 1 && hint == 504)
		{
			List<ClientCard> list3 = new List<ClientCard>();
			foreach (int checkId in maximusDiscardExtraIdList)
			{
				ClientCard clientCard = cards.FirstOrDefault((ClientCard clientCard9) => clientCard9.IsCode(checkId));
				if (clientCard != null)
				{
					list3.Add(clientCard);
				}
			}
			if (list3.Count() >= max)
			{
				if (list3.Count() > 0)
				{
					discardExtraThisTurn.Add(list3[0]?.Id ?? 0);
				}
				return base.Util.CheckSelectCount(list3, cards, min, max);
			}
		}
		if (matrixActivating && hint == 504 && min == 1 && max == 1)
		{
			bool flag = true;
			int num3 = 0;
			foreach (ClientCard card2 in cards)
			{
				flag &= card2.Location == CardLocation.Extra;
				if (num3 == 0)
				{
					num3 = card2.Controller + 1;
				}
				else if (num3 < 3 && num3 != card2.Controller + 1)
				{
					num3 = 3;
				}
			}
			Logger.DebugWriteLine("===Matrix: extraFlag = " + flag + ", enemyFlag = " + num3);
			if (flag && num3 < 3)
			{
				List<ClientCard> list4 = new List<ClientCard>();
				if (num3 == 1)
				{
					ClientCard clientCard2 = null;
					ClientCard clientCard3 = null;
					ClientCard clientCard4 = null;
					ClientCard clientCard5 = null;
					ClientCard clientCard6 = null;
					ClientCard clientCard7 = null;
					ClientCard clientCard8 = null;
					foreach (ClientCard card3 in cards)
					{
						if (card3.Id == 80532587)
						{
							clientCard2 = card3;
						}
						if (card3.Id == 41373230)
						{
							clientCard3 = card3;
						}
						if (card3.Id == 11765832)
						{
							clientCard4 = card3;
						}
						if (card3.Id == 79606837)
						{
							clientCard5 = card3;
						}
						if (card3.Id == 74586817)
						{
							clientCard6 = card3;
						}
						if (card3.Id == 24915933)
						{
							clientCard7 = card3;
						}
						if (card3.Id == 53971455)
						{
							clientCard8 = card3;
						}
					}
					List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, targetKnightmare: true, ignoreCurrentDestroy: true);
					if (clientCard2 != null && normalEnemyTargetList.Count() > 0)
					{
						list4.Add(clientCard2);
					}
					if (clientCard3 != null && !activatedCardIdList.Contains(41373230) && !discardEnemyExtraIdList.Contains(41373230) && ((!activatedCardIdList.Contains(60303688) && CheckRemainInDeck(60303688) > 0 && CheckCalledbytheGrave(60303688) == 0) | (CheckRemainInDeck(69680031) > 0 && !base.Bot.HasInHand(69680031) && !enemyActivateLockBird)))
					{
						list4.Add(clientCard3);
					}
					if (clientCard4 != null && !activatedCardIdList.Contains(11765832) && !enemyActivateLockBird)
					{
						list4.Add(clientCard4);
					}
					if (clientCard5 != null && GetNeedSearchRitualCardIdList().Count() > 0)
					{
						list4.Add(clientCard5);
					}
					if (clientCard6 != null)
					{
						list4.Add(clientCard6);
					}
					if (clientCard7 != null)
					{
						list4.Add(clientCard7);
					}
					if (clientCard8 != null && !activatedCardIdList.Contains(53971455) && !discardEnemyExtraIdList.Contains(53971455))
					{
						list4.Add(clientCard8);
					}
					if (list4.Count() > 0)
					{
						discardExtraThisTurn.Add(list4[0]?.Id ?? 0);
					}
				}
				if (num3 == 2)
				{
					checkedEnemyExtra = true;
					avoid2Monster = false;
					confirmLink2 = false;
					List<int> list5 = new List<int> { 96380700, 48068378, 14812471, 32995276, 30342076, 24842059, 3679218 };
					foreach (ClientCard card4 in cards)
					{
						NamedCard namedCard = NamedCard.Get(card4.Id);
						if (namedCard != null)
						{
							confirmLink2 |= namedCard.HasType(CardType.Link) && namedCard.Level <= 2;
							avoid2Monster |= (namedCard.HasType(CardType.Link) && namedCard.Level <= 2) || namedCard.HasType((CardType)8396800);
							if (base.Enemy.HasInMonstersZone(10158145) && list5.Contains(card4.Id))
							{
								list4.Add(card4);
							}
						}
					}
					list4 = ShuffleCardList(list4);
					foreach (ClientCard card5 in cards)
					{
						if (!list4.Contains(card5))
						{
							NamedCard namedCard2 = NamedCard.Get(card5.Id);
							if (namedCard2 != null && base.Enemy.HasInMonstersZone(10158145) && namedCard2.HasType(CardType.Link) && namedCard2.Level <= base.Enemy.GetMonsterCount())
							{
								list4.Add(card5);
							}
						}
					}
					foreach (ClientCard card6 in cards)
					{
						if (!list4.Contains(card6) && discardEnemyExtraIdList.Contains(card6.Id))
						{
							list4.Add(card6);
						}
					}
					List<ClientCard> list6 = new List<ClientCard>();
					List<ClientCard> list7 = new List<ClientCard>();
					foreach (ClientCard card in cards)
					{
						if (!list4.Contains(card))
						{
							if (cards.Any((ClientCard oc) => card != oc && card.IsCode(oc.Id)))
							{
								list7.Add(card);
							}
							else
							{
								list6.Add(card);
							}
						}
					}
					list4.AddRange(list6.OrderByDescending((ClientCard c) => NamedCard.Get(c.Id)?.Attack ?? 0));
					list4.AddRange(list7.OrderByDescending((ClientCard c) => NamedCard.Get(c.Id)?.Attack ?? 0));
				}
				if (list4.Count() > 0)
				{
					return base.Util.CheckSelectCount(list4, cards, min, max);
				}
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override void OnNewTurn()
	{
		if (base.Duel.Turn <= 1)
		{
			banSpSummonFromExTurn = 0;
			checkedEnemyExtra = false;
			avoid2Monster = true;
			dimensionShifterCount = 0;
			enemySpSummonFromExLastTurn = 0;
			enemySpSummonFromExThisTurn = 0;
		}
		enemyActivateMaxxC = false;
		enemyActivateLockBird = false;
		omegaActivateCount = 0;
		enemySpSummonFromExLastTurn = enemySpSummonFromExThisTurn;
		enemySpSummonFromExThisTurn = 0;
		currentNegatingIdList.Clear();
		if (dimensionShifterCount > 0)
		{
			dimensionShifterCount--;
		}
		infiniteImpermanenceList.Clear();
		summoned = false;
		activatedCardIdList.Clear();
		discardExtraThisTurn.Clear();
		activatedMatrixList.Clear();
		if (base.Duel.Player == 1 && banSpSummonFromExTurn > 0)
		{
			banSpSummonFromExTurn--;
		}
		base.OnNewTurn();
	}

	public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		if (previousControler == 1 && currentLocation == 4)
		{
			if (previousLocation == 1)
			{
				enemySpSummonFromDeck = true;
			}
			if (previousLocation == 64)
			{
				enemySpSummonFromExtra = true;
				enemySpSummonFromExThisTurn++;
			}
		}
		base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	public override BattlePhaseAction OnBattle(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		if (attackers.Count() == 1 && defenders.Count() == 1 && defenders[0].IsCode(10158145) && !confirmLink2)
		{
			return new BattlePhaseAction(BattlePhaseAction.BattleAction.ToMainPhaseTwo);
		}
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

	public override BattlePhaseAction OnSelectAttackTarget(ClientCard attacker, IList<ClientCard> defenders)
	{
		foreach (ClientCard defender in defenders)
		{
			attacker.RealPower = attacker.Attack;
			defender.RealPower = defender.GetDefensePower();
			if (OnPreBattleBetween(attacker, defender))
			{
				if (attacker.RealPower > defender.RealPower)
				{
					return base.AI.Attack(attacker, defender);
				}
				if (attacker.RealPower == defender.RealPower && defender.IsAttack() && base.Bot.GetMonsterCount() >= base.Enemy.GetMonsterCount())
				{
					return base.AI.Attack(attacker, defender);
				}
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
		if (!defender.IsMonsterHasPreventActivationEffectInBattle() && !activatedCardIdList.Contains(69680032) && base.Bot.HasInMonstersZone(69680031, notDisabled: true, hasXyzMaterial: false, faceUp: true) && attacker.HasSetcode(325))
		{
			attacker.RealPower += 500;
		}
		return base.OnPreBattleBetween(attacker, defender);
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (card == null)
		{
			return;
		}
		if (player == 1 && card.IsCode(10045474) && !DefaultCheckWhetherCardIdIsNegated(10045474))
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
		base.OnChaining(player, card);
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
		if (currentSolvingChainInfo.IsCode(91800273))
		{
			dimensionShifterCount = 2;
		}
		if (!currentSolvingChainInfo.IsCode(10045474))
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
		currentDestroyCardList.Clear();
		maximusDiscardExtraIdList.Clear();
		matrixActivating = false;
		base.OnChainEnd();
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

	public bool DogmatikaAlbaZoaActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Enemy.GetMonsters().Any((ClientCard card) => card?.HasType((CardType)75505728) ?? false) && base.Duel.Phase == DuelPhase.Main1 && base.Enemy.ExtraDeck.Count() > 1)
		{
			return false;
		}
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool ThesIrisSwordsoulActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckShouldNoMoreSpSummon())
			{
				return false;
			}
			return true;
		}
		if (enemySpSummonFromDeck || enemySpSummonFromExtra)
		{
			return true;
		}
		return false;
	}

	public bool ClearIrisFlag()
	{
		enemySpSummonFromDeck = false;
		enemySpSummonFromExtra = false;
		return false;
	}

	public bool DogmatikaFleurdelisActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)) && base.Enemy.GetMonsters().Any((ClientCard card) => card.IsFaceup()))
			{
				List<ClientCard> monsterListForTargetNegate = GetMonsterListForTargetNegate(canBeMonsterTarget: true);
				if (monsterListForTargetNegate.Count() > 0)
				{
					ClientCard clientCard = monsterListForTargetNegate[0];
					currentNegateMonsterList.Add(clientCard);
					base.AI.SelectYesNo(opt: true);
					base.AI.SelectCard(clientCard);
					activatedCardIdList.Add(69680031);
					return true;
				}
				if (base.Bot.HasInHand(62849088))
				{
					ClientCard problematicEnemyMonster = GetProblematicEnemyMonster(0, canBeTarget: true);
					if (problematicEnemyMonster != null)
					{
						base.AI.SelectYesNo(opt: true);
						base.AI.SelectCard(problematicEnemyMonster);
					}
					else
					{
						List<ClientCard> list = ShuffleCardList((from card in base.Enemy.GetMonsters()
							where card.IsFaceup() && !card.IsDisabled()
							select card).ToList());
						if (list.Count() > 0)
						{
							base.AI.SelectYesNo(opt: true);
							base.AI.SelectCard(list);
						}
						else
						{
							base.AI.SelectYesNo(opt: false);
						}
					}
					activatedCardIdList.Add(69680031);
					return true;
				}
			}
			if (base.Duel.Player == 0 && base.Enemy.GetMonsterCount() == 0)
			{
				int totalAttackingMonsterAttack = base.Util.GetTotalAttackingMonsterAttack(0);
				if (totalAttackingMonsterAttack < base.Enemy.LifePoints)
				{
					totalAttackingMonsterAttack += (from card in base.Bot.GetMonsters()
						where card.HasSetcode(325)
						select card).Count() * 500 + 3000;
					if (totalAttackingMonsterAttack >= base.Enemy.LifePoints)
					{
						activatedCardIdList.Add(69680031);
						base.AI.SelectYesNo(opt: false);
						return true;
					}
				}
			}
			if (base.Duel.Player == 1 && base.Bot.GetMonsterCount() == 0 && base.Util.GetTotalAttackingMonsterAttack(1) >= base.Bot.LifePoints && base.Duel.Phase == DuelPhase.Main1 && (base.CurrentTiming & 4) != 0 && base.Duel.Turn > 1)
			{
				activatedCardIdList.Add(69680031);
				List<ClientCard> list2 = ShuffleCardList((from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && !card.IsDisabled()
					select card).ToList());
				if (list2.Count() > 0)
				{
					base.AI.SelectYesNo(opt: true);
					base.AI.SelectCard(list2);
				}
				else
				{
					base.AI.SelectYesNo(opt: false);
				}
				return true;
			}
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			activatedCardIdList.Add(base.Card.Id + 1);
			return true;
		}
		return false;
	}

	public bool DogmatikaFleurdelisDelayActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			bool flag = false;
			bool flag2 = base.Duel.LastChainPlayer == -1 && base.CurrentTiming <= 0;
			if (base.Duel.Player == 0 && base.Duel.Phase == DuelPhase.Main1 && flag2 && base.Duel.Turn > 1)
			{
				Logger.DebugWriteLine("=== timing: " + base.CurrentTiming);
				int attack = base.Util.GetBestAttack(base.Bot);
				List<ClientCard> source = (from card in base.Enemy.MonsterZone.GetMonsters()
					where card.GetDefensePower() >= attack
					select card).ToList();
				List<ClientCard> source2 = (from card in base.Enemy.MonsterZone.GetMonsters()
					where card.GetDefensePower() >= 3000
					select card).ToList();
				if (source.Count() > source2.Count())
				{
					flag = true;
				}
			}
			if (((base.Bot.HasInHandOrInSpellZone(16240772) && base.Duel.Player == 0 && flag2) || base.Bot.GetSpells().Any((ClientCard card) => card.IsCode(16240772) && card.IsFacedown())) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.Level >= 7 && card.HasRace(CardRace.SpellCaster)))
			{
				flag = true;
			}
			if (flag)
			{
				List<ClientCard> list = ShuffleCardList((from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && !card.IsDisabled()
					select card).ToList());
				if (list.Count() > 0)
				{
					base.AI.SelectYesNo(opt: true);
					base.AI.SelectCard(list);
				}
				else
				{
					base.AI.SelectYesNo(opt: false);
				}
				activatedCardIdList.Add(69680031);
				return true;
			}
		}
		return false;
	}

	public bool DogmatikaMaximusActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (CheckShouldNoMoreSpSummon())
			{
				return false;
			}
			foreach (int dumpId in new List<int> { 80532587, 11765832, 53971455 })
			{
				IEnumerable<ClientCard> source = from card in base.Bot.GetGraveyardMonsters()
					where card.IsCode(dumpId)
					select card;
				if (source.Count() > 1)
				{
					IEnumerable<ClientCard> source2 = source.Where((ClientCard card) => card.ProcCompleted == 0);
					if (source2.Count() > 0)
					{
						base.AI.SelectCard(source2.ToList());
						return true;
					}
					base.AI.SelectCard(source.ToList());
					return true;
				}
			}
			List<ClientCard> list = ShuffleCardList(base.Bot.Graveyard.Where((ClientCard card) => card != null && card.IsMonster() && card.ProcCompleted == 0 && card.HasType((CardType)75505728)).ToList());
			if (list.Count() > 0)
			{
				base.AI.SelectCard(list);
				return true;
			}
			List<ClientCard> list2 = (from card in base.Bot.Graveyard
				where card != null && card.IsMonster() && card.HasType((CardType)75505728)
				orderby card.Attack
				select card).ToList();
			if (list2.Count() > 0)
			{
				base.AI.SelectCard(list2);
				return true;
			}
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (CheckWhetherWillbeRemoved())
			{
				return false;
			}
			List<int> list3 = new List<int>();
			foreach (int item in new List<int> { 80532587, 79606837, 11765832, 41373230, 24915933, 74586817, 53971455 })
			{
				if (base.Bot.HasInExtra(item) && !activatedCardIdList.Contains(item) && (item != 80532587 || GetNormalEnemyTargetList(canBeTarget: true, targetKnightmare: false).Count() != 0) && (!enemyActivateLockBird || (item != 79606837 && item != 11765832)) && (item != 79606837 || (!base.Bot.HasInMonstersZone(51522296) && GetNeedSearchRitualCardIdList().Count() != 0)) && (item != 11765832 || !activatedCardIdList.Contains(11765832)) && (!discardExtraThisTurn.Contains(item) || (item != 41373230 && item != 53971455)))
				{
					list3.Add(item);
				}
			}
			maximusDiscardExtraIdList.AddRange(list3);
			activatedCardIdList.Add(base.Card.Id);
			UpdateBanSpSummonFromExTurn(1);
			return true;
		}
		return false;
	}

	public bool DiabellstarTheBlackWitchSpSummon()
	{
		bool flag = false;
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.MonsterZone[i] == null)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			if (base.Bot.HasInHandOrInSpellZone(80845034))
			{
				base.AI.SelectCard(80845034);
				return true;
			}
			if (activatedMatrixList.Count() > 0)
			{
				base.AI.SelectCard(activatedMatrixList);
				return true;
			}
			if ((from card in base.Bot.GetSpells()
				where card.IsCode(35569555)
				select card).Count() > 1)
			{
				base.AI.SelectCard(35569555);
				return true;
			}
		}
		if (!base.Bot.HasInHand(69680031) || (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.HasSetcode(325)
			select card).Count() > 1)
		{
			foreach (int checkId in new List<int> { 60303688, 95679145 })
			{
				ClientCard clientCard = null;
				if (activatedCardIdList.Contains(checkId))
				{
					clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsCode(checkId));
				}
				if (clientCard == null)
				{
					clientCard = base.Bot.GetMonsters().FirstOrDefault((ClientCard card) => card.IsCode(checkId) && card.IsDisabled());
				}
				if (clientCard != null)
				{
					base.AI.SelectCard(clientCard);
					return true;
				}
			}
		}
		if (flag)
		{
			foreach (int item in new List<int> { 10158145, 62849088 })
			{
				if (base.Bot.HasInHand(item))
				{
					base.AI.SelectCard(item);
					return true;
				}
			}
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFacedown()
			orderby NamedCard.Get(card.Id)?.Attack ?? card.Attack
			select card).ToList();
		if (list.Count() > 0)
		{
			base.AI.SelectCard(list);
			return true;
		}
		if (flag)
		{
			if (base.Bot.HasInHand(69680031) && (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.HasSetcode(325)
				select card).Count() == 0)
			{
				base.AI.SelectCard(69680031);
				return true;
			}
			if (CheckRemainInDeck(60921537) > 0)
			{
				ClientCard clientCard2 = base.Bot.Hand.FirstOrDefault((ClientCard card) => card.IsCode(51522296));
				if (clientCard2 != null)
				{
					base.AI.SelectCard(clientCard2);
					return true;
				}
			}
			foreach (int dumpId in new List<int> { 82956214, 10045474, 24224830, 14558127, 23434538 })
			{
				if (base.Bot.Hand.Where((ClientCard card) => card.IsCode(dumpId)).Count() + base.Bot.SpellZone.Where((ClientCard card) => card?.IsCode(dumpId) ?? false).Count() > 1)
				{
					base.AI.SelectCard(dumpId);
					return true;
				}
			}
			foreach (ClientCard item2 in (from card in base.Bot.GetMonsters()
				where card.HasType((CardType)67117120)
				orderby card.Attack
				select card).ToList())
			{
				if (!base.Bot.HasInHand(69680031) || CheckHasExtraOnField(item2))
				{
					base.AI.SelectCard(item2);
					return true;
				}
			}
		}
		if (base.Bot.GetMonsterCount() == 0 || CheckRemainInDeck(16240772) > 0)
		{
			foreach (int item3 in new List<int> { 65681983, 10045474, 24224830, 82956214, 60921537, 31002402 })
			{
				if (base.Bot.HasInHandOrInSpellZone(item3))
				{
					base.AI.SelectCard(item3);
					return true;
				}
			}
		}
		return false;
	}

	public bool DiabellstarTheBlackWitchActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			base.AI.SelectCard(16240772, 80845034);
			SelectSTPlace();
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		return false;
	}

	public bool DogmatikaEcclesiaSummon()
	{
		if (enemyActivateLockBird)
		{
			return false;
		}
		if (CheckWhetherNegated())
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

	public bool DogmatikaEcclesiaActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (activatedCardIdList.Contains(base.Card.Id))
			{
				return false;
			}
			if (CheckShouldNoMoreSpSummon() && (!base.Bot.HasInHand(69680031) || base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325))))
			{
				return false;
			}
			if (enemyActivateLockBird)
			{
				if (base.Bot.HasInHand(69680031) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)))
				{
					return true;
				}
				return false;
			}
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (((base.Duel.Player == 0 && base.Duel.Phase == DuelPhase.End) || (base.Duel.Player == 1 && base.Duel.Phase < DuelPhase.End)) && !base.Bot.HasInHand(69680031) && CheckRemainInDeck(69680031) > 0)
			{
				base.AI.SelectCard(69680031);
				activatedCardIdList.Add(base.Card.Id);
				UpdateBanSpSummonFromExTurn(1);
				return true;
			}
			if (CheckAtAdvantage() && enemyActivateMaxxC)
			{
				List<int> list = new List<int> { 82956214, 69680031 };
				if (DogmatikaMatrixCanActivate())
				{
					list.Add(35569555);
				}
				list.AddRange(new List<int> { 95679145, 51522296, 60921537, 31002402 });
				list.Add(35569555);
				foreach (int item in list)
				{
					if (!base.Bot.HasInHandOrInSpellZone(item) && CheckRemainInDeck(item) > 0)
					{
						base.AI.SelectCard(item);
						activatedCardIdList.Add(base.Card.Id);
						UpdateBanSpSummonFromExTurn(1);
						return true;
					}
				}
			}
			bool flag = DogmatikaMatrixCanActivate() && !activatedCardIdList.Contains(35569555) && CheckRemainInDeck(35569555) > 0 && !base.Bot.HasInHand(35569555);
			if (flag && base.Enemy.GetMonsterCount() > 0)
			{
				base.AI.SelectCard(35569555);
				activatedCardIdList.Add(base.Card.Id);
				UpdateBanSpSummonFromExTurn(1);
				return true;
			}
			List<int> needSearchRitualCardIdList = GetNeedSearchRitualCardIdList();
			bool flag2 = CheckRemainInDeck(95679145) > 0 && !activatedCardIdList.Contains(95679145) && base.Bot.Graveyard.Where((ClientCard card) => card.HasType((CardType)75505728)).Count() > 0;
			if (needSearchRitualCardIdList.Count() > 0)
			{
				if (flag)
				{
					base.AI.SelectCard(35569555);
					activatedCardIdList.Add(base.Card.Id);
					UpdateBanSpSummonFromExTurn(1);
					return true;
				}
				if (flag2 && base.Bot.HasInExtra(79606837))
				{
					base.AI.SelectCard(95679145);
					activatedCardIdList.Add(base.Card.Id);
					UpdateBanSpSummonFromExTurn(1);
					return true;
				}
			}
			if (flag2)
			{
				base.AI.SelectCard(95679145);
				activatedCardIdList.Add(base.Card.Id);
				UpdateBanSpSummonFromExTurn(1);
				return true;
			}
			List<int> list2 = new List<int> { 82956214, 69680031 };
			if (DogmatikaMatrixCanActivate())
			{
				list2.Add(35569555);
			}
			list2.AddRange(new List<int> { 95679145, 51522296, 60921537, 31002402 });
			list2.Add(35569555);
			foreach (int item2 in list2)
			{
				if (!base.Bot.HasInHandOrInSpellZone(item2) && CheckRemainInDeck(item2) > 0)
				{
					base.AI.SelectCard(item2);
					activatedCardIdList.Add(base.Card.Id);
					UpdateBanSpSummonFromExTurn(1);
					return true;
				}
			}
		}
		return false;
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
		if (base.Enemy.HasInMonstersZone(10158145, notDisabled: true, hasXyzMaterial: false, faceUp: true) && !confirmLink2)
		{
			return false;
		}
		return DefaultMaxxC();
	}

	public bool KnightmareCorruptorIbleeSummon()
	{
		if (banSpSummonFromExTurn > 0)
		{
			return false;
		}
		if (CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (activatedCardIdList.Contains(10158145))
		{
			return false;
		}
		if (base.Bot.HasInExtra(60303245) || base.Bot.HasInExtra(24842059))
		{
			summoned = true;
			return true;
		}
		base.Bot.HasInExtra(29301450);
		return false;
	}

	public bool KnightmareCorruptorIbleeActivate()
	{
		if (base.Util.IsTurn1OrMain2())
		{
			return true;
		}
		if (base.Duel.Turn > 1)
		{
			if (base.Bot.HasInHand(35569555) && DogmatikaMatrixCanActivate())
			{
				return true;
			}
			if (base.Enemy.GetMonsterCount() > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool NadirServantActivate()
	{
		if (CheckWhetherNegated() || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		ClientCard clientCard = null;
		int searchId = 0;
		if (!activatedCardIdList.Contains(60303688) && CheckCalledbytheGrave(60303688) == 0 && !base.Bot.HasInHand(60303688) && (CheckHasExtraOnField() || !summoned) && (base.Bot.HasInGraveyard(60303688) || CheckRemainInDeck(60303688) > 0))
		{
			searchId = 60303688;
			clientCard = GetExtraToDiscard(1500);
		}
		if ((searchId == 0 || clientCard == null) && ((!activatedCardIdList.Contains(95679145) && CheckCalledbytheGrave(95679145) == 0 && base.Bot.HasInGraveyard(95679145)) || CheckRemainInDeck(95679145) > 0))
		{
			searchId = 95679145;
			clientCard = GetExtraToDiscard(1500);
		}
		if ((searchId == 0 || clientCard == null) && ((!base.Bot.HasInHand(69680031) && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)) && base.Bot.HasInGraveyard(69680031)) || CheckRemainInDeck(69680031) > 0))
		{
			searchId = 69680031;
			clientCard = GetExtraToDiscard(2500);
		}
		if ((searchId == 0 || clientCard == null) && (base.Bot.HasInGraveyard(60303688) || CheckRemainInDeck(60303688) > 0))
		{
			searchId = 60303688;
			clientCard = GetExtraToDiscard(1500);
		}
		if (clientCard != null && searchId > 0)
		{
			discardExtraThisTurn.Add(clientCard?.Id ?? 0);
			base.AI.SelectCard(clientCard);
			ClientCard clientCard2 = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card?.IsCode(searchId) ?? false);
			if (clientCard2 != null)
			{
				base.AI.SelectNextCard(clientCard2);
			}
			else
			{
				base.AI.SelectNextCard(searchId);
			}
			activatedCardIdList.Add(base.Card.Id);
			UpdateBanSpSummonFromExTurn(1);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool DogmatikaLamityActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Bot.HasInExtra(53971455))
		{
			base.AI.SelectYesNo(opt: true);
			base.AI.SelectCard(51522296);
			base.AI.SelectNextCard(53971455);
			discardExtraThisTurn.Add(53971455);
			activatedCardIdList.Add(base.Card.Id);
			UpdateBanSpSummonFromExTurn(1);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool DogmatikaLamityDelayActivate()
	{
		if (CheckWhetherNegated() || base.Bot.HasInExtra(53971455))
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(51522296, notDisabled: false, hasXyzMaterial: false, faceUp: true))
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		int num = 0;
		foreach (ClientCard item in (from card in base.Bot.GetMonsters()
			where !card.HasType(CardType.Xyz) && card.IsFacedown()
			orderby card.Level descending
			select card).ToList())
		{
			list.Add(item);
			num += item.Level;
			if (num >= 12)
			{
				break;
			}
		}
		ClientCard handSummonTarget = base.Bot.Hand.FirstOrDefault((ClientCard card) => card.IsCode(51522296));
		if (handSummonTarget == null)
		{
			return false;
		}
		int num2 = 0;
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where !card.HasType((CardType)75497472) && card.IsFaceup()
			orderby card.Level descending
			select card).ToList();
		list2.AddRange((from card in base.Bot.Hand
			where card.IsMonster() && card != handSummonTarget
			orderby card.Level descending
			select card).ToList());
		foreach (ClientCard item2 in list2)
		{
			if (num >= 12 || num2 >= 1)
			{
				break;
			}
			list.Add(item2);
			num += item2.Level;
			num2++;
		}
		if (num >= 12)
		{
			base.AI.SelectYesNo(opt: false);
			base.AI.SelectCard(51522296);
			base.AI.SelectNextCard(list);
			activatedCardIdList.Add(base.Card.Id);
			UpdateBanSpSummonFromExTurn(1);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool DogmatikaMacabreActivate()
	{
		if (base.Bot.HasInMonstersZone(51522296))
		{
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.Graveyard
			where card?.HasType((CardType)8256) ?? false
			orderby card.Level descending
			select card).ToList();
		List<ClientCard> list2 = new List<ClientCard>();
		int num = 0;
		List<int> list3 = new List<int> { 53971455, 41373230 };
		foreach (ClientCard item in list)
		{
			if (item.IsCode(74586817))
			{
				continue;
			}
			if (CheckAtAdvantage())
			{
				foreach (int item2 in list3)
				{
					if (item.IsCode(item2))
					{
						discardExtraThisTurn.Contains(item2);
					}
				}
			}
			num += item.Level;
			list2.Add(item);
			if (num >= 12)
			{
				break;
			}
		}
		if (num >= 12)
		{
			ClientCard clientCard = base.Bot.Graveyard.FirstOrDefault((ClientCard card) => card.IsCode(51522296));
			if (clientCard != null)
			{
				base.AI.SelectCard(clientCard);
			}
			else
			{
				base.AI.SelectCard(51522296);
			}
			base.AI.SelectMaterials(list2, 500);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return false;
	}

	public bool SinfulSpoilsOfDoom_RcielaActivate()
	{
		ClientCard clientCard = null;
		bool flag = false;
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.Level >= 7 && card.HasRace(CardRace.SpellCaster)
			orderby card.Attack descending, card.Level descending
			select card).ToList();
		bool flag2 = list.Count() == 1 && list[0].IsCode(51522296);
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.IsMonster())
		{
			bool flag3 = lastChainCard.IsCode(97268402, 52038441);
			if (base.Duel.Turn > 1 || !flag3)
			{
				foreach (ClientCard lastChainTarget in base.Duel.LastChainTargets)
				{
					if (list.Contains(lastChainTarget) && (!flag3 || !lastChainTarget.IsCode(72270339)))
					{
						clientCard = lastChainTarget;
						flag = true;
						break;
					}
				}
			}
		}
		if (clientCard == null && !flag2)
		{
			clientCard = list.FirstOrDefault((ClientCard card) => !card.IsCode(51522296));
		}
		if (DefaultOnBecomeTarget() && !flag2)
		{
			flag = true;
		}
		bool num;
		int num2;
		if (clientCard != null)
		{
			int targetAttack = clientCard.Attack;
			if (GetProblematicEnemyMonster(-1, canBeTarget: true, ignoreCurrentDestroy: true) != null)
			{
				flag = true;
			}
			if (!flag2)
			{
				List<ClientCard> list2 = (from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && card.Attack > 0 && card.Attack <= targetAttack && !currentDestroyCardList.Contains(card) && (base.Duel.Player == 1 || card != base.Enemy.BattlingMonster)
					select card).ToList();
				if (list2.Count() > 1)
				{
					flag = true;
					currentDestroyCardList.AddRange(list2);
				}
			}
			int botWorstPower = base.Util.GetWorstBotMonster()?.GetDefensePower() ?? 0;
			num = base.Duel.Player == 1 && base.Enemy.GetMonsters().Any((ClientCard card) => card.Attack >= botWorstPower && card.IsMonsterHasPreventActivationEffectInBattle()) && base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2;
			if (flag2)
			{
				ClientCard battlingMonster = base.Bot.BattlingMonster;
				if (battlingMonster == null || !battlingMonster.IsCode(51522296))
				{
					goto IL_02e1;
				}
			}
			if ((base.Bot.BattlingMonster?.GetDefensePower() ?? 0) > (base.Enemy.BattlingMonster?.GetDefensePower() ?? 0) || base.Duel.LastChainPlayer == 0 || (base.CurrentTiming & 0x2000) == 0)
			{
				goto IL_02e1;
			}
			num2 = ((base.CurrentTiming > 0) ? 1 : 0);
			goto IL_02e2;
		}
		goto IL_02e7;
		IL_02e1:
		num2 = 0;
		goto IL_02e2;
		IL_02e7:
		if (flag)
		{
			SelectSTPlace(null, avoidImpermanence: true);
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
		IL_02e2:
		if (((num ? 1u : 0u) | (uint)num2) != 0)
		{
			flag = true;
		}
		goto IL_02e7;
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
				if (base.Enemy.Graveyard.GetFirstMatchingCard((ClientCard card) => card.IsMonster() && card.IsOriginalCode(code)) != null)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					base.AI.SelectCard(code);
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
					int originCode = item.GetOriginCode();
					base.AI.SelectCard(item);
					currentNegatingIdList.Add(originCode);
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
					int id = list[0].Id;
					base.AI.SelectCard(list);
					currentNegatingIdList.Add(id);
					return true;
				}
			}
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			return false;
		}
		List<ClientCard> dangerousCardinEnemyGrave = GetDangerousCardinEnemyGrave(onlyMonster: true);
		if (dangerousCardinEnemyGrave.Count() > 0)
		{
			int id2 = dangerousCardinEnemyGrave[0].Id;
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(dangerousCardinEnemyGrave);
			currentNegatingIdList.Add(id2);
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
			int originCode = base.Util.GetLastChainCard().GetOriginCode();
			switch (originCode)
			{
			case 0:
				return false;
			case 72270339:
				return false;
			}
			if (CheckCalledbytheGrave(originCode) > 0)
			{
				return false;
			}
			if (CheckRemainInDeck(originCode) > 0)
			{
				if (base.Card.Location != CardLocation.SpellZone)
				{
					SelectSTPlace(null, avoidImpermanence: true);
				}
				base.AI.SelectAnnounceID(originCode);
				currentNegatingIdList.Add(originCode);
				CheckDeactiveFlag();
				return true;
			}
		}
		return false;
	}

	public bool WANTED_SeekerOfSinfulSpoilsActivate()
	{
		if (base.Card.Location == CardLocation.Hand || (base.Card.Location == CardLocation.SpellZone && base.Card.HasPosition(CardPosition.FaceDown)))
		{
			activatedCardIdList.Add(base.Card.Id);
			SelectSTPlace(null, avoidImpermanence: true);
			return true;
		}
		return true;
	}

	public bool DogmatikaMatrixCanActivate()
	{
		return CheckRemainInDeck(51522296, 31002402, 60921537) > 0;
	}

	public bool DogmatikaMatrixActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand || (base.Card.Location == CardLocation.SpellZone && base.Card.HasPosition(CardPosition.FaceDown)))
		{
			List<int> needSearchRitualCardIdList = GetNeedSearchRitualCardIdList();
			if (base.Enemy.GetMonsterCount() == 0)
			{
				if (!base.Bot.MonsterZone.Any((ClientCard card) => card != null && card.IsFaceup() && card.HasType(CardType.Ritual) && card.HasSetcode(325)) && needSearchRitualCardIdList.Count() <= 0)
				{
					return false;
				}
				SelectSTPlace(null, avoidImpermanence: true);
				base.AI.SelectCard(needSearchRitualCardIdList);
				base.AI.SelectYesNo(opt: true);
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			if (needSearchRitualCardIdList.Count() <= 0)
			{
				SelectSTPlace(null, avoidImpermanence: true);
				base.AI.SelectYesNo(opt: true);
				if (CheckRemainInDeck(51522296) > 0 && CheckRemainInDeck(31002402, 60921537) > 0)
				{
					base.AI.SelectCard(51522296);
					base.AI.SelectNextCard(31002402, 60921537);
				}
				else
				{
					base.AI.SelectCard(51522296, 31002402, 60921537);
					DogmatikaMatrixNextSearch();
				}
				activatedCardIdList.Add(base.Card.Id);
				return true;
			}
			SelectSTPlace(null, avoidImpermanence: true);
			base.AI.SelectCard(needSearchRitualCardIdList);
			base.AI.SelectYesNo(opt: true);
			DogmatikaMatrixNextSearch();
			activatedCardIdList.Add(base.Card.Id);
			return true;
		}
		int opt = 0;
		if (CheckWhetherWillbeRemoved())
		{
			opt = 1;
		}
		if (!checkedEnemyExtra && base.Enemy.ExtraDeck.Count() > 0)
		{
			opt = 1;
		}
		if (base.Enemy.HasInMonstersZone(10158145) && avoid2Monster)
		{
			opt = 1;
		}
		if (!base.Bot.HasInExtra(80532587) || GetNormalEnemyTargetList(canBeTarget: true, targetKnightmare: false).Count() <= 0)
		{
			List<int> obj = new List<int> { 11765832, 53971455, 41373230, 24915933, 74586817 };
			bool flag = false;
			foreach (int item in obj)
			{
				flag |= !discardExtraThisTurn.Contains(item) && !activatedCardIdList.Contains(item) && base.Bot.HasInExtra(item);
			}
			if (!flag)
			{
				opt = 1;
			}
		}
		Logger.DebugWriteLine("===Matrix option: " + opt);
		matrixActivating = true;
		base.AI.SelectOption(opt);
		activatedMatrixList.Add(base.Card);
		return true;
	}

	public void DogmatikaMatrixNextSearch()
	{
		List<ClientCard> monsters = base.Bot.GetMonsters();
		monsters.AddRange(base.Enemy.GetMonsters());
		bool flag = monsters.Any((ClientCard card) => card.HasType((CardType)75505728));
		if (!activatedCardIdList.Contains(60303688) && CheckCalledbytheGrave(60303688) == 0 && (flag || !summoned) && CheckRemainInDeck(60303688) > 0)
		{
			base.AI.SelectNextCard(60303688);
			return;
		}
		if (CheckRemainInDeck(95679145) > 0 && !activatedCardIdList.Contains(95679145) && base.Bot.Graveyard.Where((ClientCard card) => card.HasType((CardType)75505728)).Count() > 0)
		{
			base.AI.SelectNextCard(95679145);
			return;
		}
		if (CheckRemainInDeck(69680031) > 0 && !base.Bot.HasInHand(69680031) && flag && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)))
		{
			base.AI.SelectNextCard(69680031);
			return;
		}
		foreach (int item in new List<int> { 82956214, 60303688, 35569555, 95679145, 69680031, 51522296, 31002402, 60921537 })
		{
			if (CheckRemainInDeck(item) > 0)
			{
				base.AI.SelectNextCard(item);
				break;
			}
		}
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

	public bool DogmatikaPunishmentActivate()
	{
		if (CheckWhetherNegated() || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		ClientCard clientCard = null;
		ClientCard clientCard2 = null;
		List<ClientCard> problematicEnemyCardList = GetProblematicEnemyCardList(canBeTarget: true, ignoreNormalSpell: true);
		if (problematicEnemyCardList.Count() > 0 && base.Duel.LastChainPlayer != 0 && base.Bot.HasInExtra(80532587))
		{
			foreach (ClientCard item in problematicEnemyCardList)
			{
				if (item.IsFaceup() && item.IsMonster() && item.Attack <= 2500)
				{
					clientCard = item;
					clientCard2 = GetExtraToDiscard(2500, item);
					if (clientCard2 != null)
					{
						break;
					}
				}
			}
			if (clientCard == null || clientCard2 == null)
			{
				List<ClientCard> list = (from card in base.Enemy.GetMonsters()
					where card.IsFaceup() && !card.IsShouldNotBeTarget() && card.IsShouldNotBeSpellTrapTarget()
					select card).ToList();
				list.Sort(CardContainer.CompareCardAttack);
				foreach (ClientCard item2 in list)
				{
					if (item2.IsFaceup() && item2.IsMonster() && item2.Attack <= 2500)
					{
						clientCard = item2;
						clientCard2 = GetExtraToDiscard(2500, item2);
						if (clientCard2 != null)
						{
							break;
						}
					}
				}
			}
		}
		if (clientCard == null || clientCard2 == null)
		{
			clientCard = GetProblematicEnemyMonster(0, canBeTarget: true, ignoreCurrentDestroy: true);
			if (clientCard != null)
			{
				clientCard2 = GetExtraToDiscard(clientCard.Attack, clientCard);
			}
		}
		if (clientCard == null || clientCard2 == null)
		{
			bool flag = DefaultOnBecomeTarget();
			bool flag2 = base.Bot.UnderAttack && (base.Bot.BattlingMonster?.GetDefensePower() ?? 0) <= (base.Enemy.BattlingMonster?.GetDefensePower() ?? 0) && base.Duel.LastChainPlayer != 0;
			bool flag3 = base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer != 0;
			bool flag4 = base.Duel.Player == 1 && avoid2Monster && base.Enemy.GetMonsterCount() >= 2 && base.Duel.LastChainPlayer != 0;
			Logger.DebugWriteLine("===punishment check flag: " + flag + " " + flag2 + " " + flag3 + " " + flag4);
			if (flag || flag2 || flag3 || flag4)
			{
				foreach (ClientCard item3 in (from c in base.Enemy.GetMonsters()
					where c.IsFaceup() && !c.IsShouldNotBeTarget() && !currentDestroyCardList.Contains(c)
					orderby c.Attack descending
					select c).ToList())
				{
					clientCard2 = GetExtraToDiscard(item3.Attack, item3);
					if (clientCard2 != null)
					{
						clientCard = item3;
						break;
					}
				}
			}
		}
		if (clientCard != null && clientCard2 != null)
		{
			base.AI.SelectCard(clientCard);
			base.AI.SelectNextCard(clientCard2);
			currentDestroyCardList.Add(clientCard);
			discardExtraThisTurn.Add(clientCard2?.Id ?? 0);
			activatedCardIdList.Add(base.Card.Id);
			UpdateBanSpSummonFromExTurn(2);
			return true;
		}
		return false;
	}

	public bool GranguignolTheDuskDragonActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			base.AI.SelectCard(53971455, 60303688, 95679145, 69680031);
			return true;
		}
		return false;
	}

	public bool TitanikladTheAshDragonActivate()
	{
		if (!activatedCardIdList.Contains(60303688) && CheckRemainInDeck(60303688) > 0 && CheckCalledbytheGrave(60303688) == 0)
		{
			base.AI.SelectOption(1);
			base.AI.SelectCard(60303688);
			return true;
		}
		if (CheckRemainInDeck(69680031) > 0)
		{
			if (!base.Bot.HasInHand(69680031) && !enemyActivateLockBird && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)))
			{
				base.AI.SelectOption(0);
				base.AI.SelectCard(69680031);
				return true;
			}
			if (base.Duel.Player == 1 && base.Enemy.GetMonsterCount() == 0)
			{
				base.AI.SelectOption(1);
				base.AI.SelectCard(69680031);
				return true;
			}
		}
		if (CheckRemainInDeck(95679145) > 0)
		{
			base.AI.SelectOption(1);
			base.AI.SelectCard(95679145);
			return true;
		}
		return false;
	}

	public bool GaruraWingsOfResonantLifeActivate()
	{
		activatedCardIdList.Add(base.Card.Id);
		return true;
	}

	public bool ElderEntityNtssActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			List<ClientCard> normalEnemyTargetList = GetNormalEnemyTargetList(canBeTarget: true, targetKnightmare: true, ignoreCurrentDestroy: true);
			if (normalEnemyTargetList.Count() > 0)
			{
				currentDestroyCardList.Add(normalEnemyTargetList[0]);
				base.AI.SelectCard(normalEnemyTargetList);
				return true;
			}
		}
		return false;
	}

	public bool DespianLuluwalilithActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (!activatedCardIdList.Contains(60303688) && CheckRemainInDeck(60303688) > 0 && CheckCalledbytheGrave(60303688) == 0 && !enemyActivateLockBird)
			{
				base.AI.SelectCard(60303688);
				return true;
			}
			if (CheckRemainInDeck(62849088) > 0)
			{
				base.AI.SelectCard(62849088);
				return true;
			}
			if (base.Duel.Turn > 1 && base.Enemy.GetMonsterCount() == 0 && CheckRemainInDeck(69680031) > 0)
			{
				base.AI.SelectCard(69680031);
				return true;
			}
			if (base.Bot.HasInHand(69680031) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.HasSetcode(325)))
			{
				foreach (int item in new List<int> { 95679145, 60303688, 69680031 })
				{
					if (CheckRemainInDeck(item) > 0)
					{
						base.AI.SelectCard(item);
						return true;
					}
				}
			}
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			List<ClientCard> list = base.Duel.CurrentChain.Where((ClientCard card) => card.Controller == 1 && !currentNegateMonsterList.Contains(card) && (card.Location == CardLocation.MonsterZone || card.Location == CardLocation.SpellZone)).ToList();
			list.AddRange(GetProblematicEnemyCardList());
			list.AddRange(ShuffleCardList((from card in base.Enemy.GetSpells()
				where card.IsFaceup()
				select card).ToList()));
			list.AddRange(ShuffleCardList((from card in base.Enemy.GetMonsters()
				where card.IsFaceup()
				select card).ToList()));
			if (list.Count() > 0)
			{
				currentNegateMonsterList.Add(list[0]);
				base.AI.SelectYesNo(opt: true);
				base.AI.SelectCard(list);
			}
			else
			{
				base.AI.SelectYesNo(opt: false);
			}
			return true;
		}
		return false;
	}

	public bool PSYFramelordOmegaActivate()
	{
		if (base.Card.Location == CardLocation.Grave && omegaActivateCount <= 5)
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			List<ClientCard> dangerousCardinEnemyGrave = GetDangerousCardinEnemyGrave(onlyMonster: true);
			if (dangerousCardinEnemyGrave.Count() > 0)
			{
				base.AI.SelectCard(dangerousCardinEnemyGrave);
				omegaActivateCount++;
				return true;
			}
			List<int> list = new List<int> { 11765832, 80532587 };
			foreach (int item in list)
			{
				if (!base.Bot.HasInExtra(item) && base.Bot.HasInGraveyard(item))
				{
					base.AI.SelectCard(item);
					omegaActivateCount++;
					return true;
				}
			}
			foreach (int item2 in new List<int> { 60921537, 31002402, 51522296, 82956214, 60303688, 69680031, 35569555 })
			{
				if (CheckRemainInDeck(item2) <= 0 && base.Bot.HasInGraveyard(item2))
				{
					base.AI.SelectCard(item2);
					omegaActivateCount++;
					return true;
				}
			}
			list.AddRange(new List<int> { 93039339, 41373230, 79606837, 53971455, 24842059, 60303245, 2220237 });
			foreach (int item3 in list)
			{
				if (!base.Bot.HasInExtra(item3) && base.Bot.HasInGraveyard(item3))
				{
					base.AI.SelectCard(item3);
					omegaActivateCount++;
					return true;
				}
			}
		}
		return false;
	}

	public bool HeraldOfTheArcLightActivate()
	{
		base.AI.SelectCard(GetNeedSearchRitualCardIdList());
		return true;
	}

	public bool SuperStarslayerTYPHONSpSummon()
	{
		ClientCard clientCard = (from card in base.Bot.GetMonsters()
			where card.IsFaceup()
			orderby card.Attack descending
			select card).FirstOrDefault();
		if (clientCard == null || clientCard.Attack >= 3000)
		{
			return false;
		}
		if ((GetProblematicEnemyMonster(clientCard.Attack) != null) | clientCard.HasType(CardType.Link) | (clientCard.Level <= 4))
		{
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
			targetList.AddRange(ShuffleCardList((from card in base.Enemy.GetMonsters()
				where card.IsFacedown() && !targetList.Contains(card)
				select card).ToList()));
			targetList.AddRange(ShuffleCardList((from card in base.Bot.GetMonsters()
				where card.IsFacedown() && !targetList.Contains(card)
				select card).ToList()));
			targetList.AddRange(from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !targetList.Contains(card)
				orderby card.Attack
				select card);
			base.AI.SelectCard(base.Card.Overlays);
			base.AI.SelectNextCard(targetList);
			return true;
		}
		return false;
	}

	public bool SPLittleKnightActivate()
	{
		return false;
	}

	public bool SecureGardnaSpSummon()
	{
		if (base.Bot.HasInHand(95679145) && !base.Bot.Graveyard.Any((ClientCard card) => card.IsMonster() && card.HasType((CardType)75505728)))
		{
			return true;
		}
		return false;
	}

	public bool LinguribohSpSummon()
	{
		if (base.Enemy.GetSpells().Any((ClientCard card) => card.IsFacedown()))
		{
			return true;
		}
		if (!base.Bot.HasInExtra(60303245))
		{
			return true;
		}
		return false;
	}

	public bool LinguribohActivate()
	{
		if (CheckLastChainShouldNegated())
		{
			return true;
		}
		return false;
	}

	public bool SalamangreatAlmirajSpSummon()
	{
		if (base.Bot.HasInMonstersZone(10158145, notDisabled: false, hasXyzMaterial: false, faceUp: true))
		{
			base.AI.SelectMaterials(10158145);
			return true;
		}
		if (base.Bot.HasInHand(new List<int> { 60303688, 95679145, 1984618 }))
		{
			List<ClientCard> list = base.Bot.MonsterZone.Where((ClientCard card) => card != null && card.IsFaceup() && card.Attack <= 1000 && !card.HasType((CardType)75505856)).ToList();
			if (list.Count() > 0)
			{
				list.Sort(CardContainer.CompareCardAttack);
				base.AI.SelectMaterials(list);
				return true;
			}
		}
		return false;
	}

	public bool SalamangreatAlmirajActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return true;
		}
		if ((base.Duel.Player == 1 && (!base.Bot.HasInHand(69680031) || activatedCardIdList.Contains(69680031))) | DefaultOnBecomeTarget() | (base.Bot.UnderAttack && base.Bot.BattlingMonster == base.Card))
		{
			base.AI.SelectCard(base.Util.GetBestBotMonster());
			return true;
		}
		if (!base.Util.ChainContainsCard(16240772))
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card != base.Card
				orderby card.Attack descending
				select card).ToList();
			list.AddRange(base.Bot.GetMonsters().GetMatchingCards((ClientCard card) => card.IsFacedown()));
			foreach (ClientCard item in list)
			{
				if (base.Util.IsChainTarget(item))
				{
					base.AI.SelectCard(item);
					return true;
				}
			}
		}
		return false;
	}

	public bool SummonForTYPHONCheck()
	{
		if (!base.Bot.HasInExtra(93039339) || base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup()) || banSpSummonFromExTurn > 0)
		{
			return false;
		}
		if (enemySpSummonFromExLastTurn < 2 && enemySpSummonFromExThisTurn < 2)
		{
			return false;
		}
		if (base.Card.IsCode(10158145) && !CheckWhetherNegated())
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

	public bool MonsterRepos()
	{
		int num = base.Card.Attack + 1;
		int num2 = 0;
		if (!activatedCardIdList.Contains(69680032) && base.Bot.HasInMonstersZone(69680031, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			num2 += 500;
		}
		if (base.Card.HasSetcode(325))
		{
			num += num2;
		}
		if (base.Card.IsFaceup() && base.Card.IsDefense() && num <= 1)
		{
			return false;
		}
		int num3 = 0;
		foreach (ClientCard monster in base.Bot.GetMonsters())
		{
			int num4 = monster.Attack;
			if (monster.HasSetcode(325))
			{
				num4 += num2;
			}
			if (num4 >= num3)
			{
				num3 = num4;
			}
		}
		bool flag = base.Util.IsAllEnemyBetterThanValue(num3, onlyATK: true);
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

	public bool SpellSetCheck()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (base.Card.IsCode(82956214) && base.Bot.HasInSpellZone(base.Card.Id))
		{
			return false;
		}
		if (base.Card.IsCode(16240772) && !base.Bot.HasInHand(69680031) && !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFaceup() && card.Level >= 7 && card.HasRace(CardRace.SpellCaster)))
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
