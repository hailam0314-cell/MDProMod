using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Exosister", "AI_Exosister", "Normal")]
internal class ExosisterExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int ExosisterElis = 16474916;

		public const int ExosisterStella = 43863925;

		public const int ExosisterIrene = 79858629;

		public const int ExosisterSophia = 5352328;

		public const int ExosisterMartha = 37343995;

		public const int Aratama = 16889337;

		public const int Sakitama = 67972302;

		public const int ExosisterPax = 77913594;

		public const int ExosisterArment = 4408198;

		public const int PotofExtravagance = 84211599;

		public const int ExosisterVadis = 77891946;

		public const int ExosisterReturnia = 197042;

		public const int ExosisterMikailis = 42741437;

		public const int ExosisterKaspitell = 78135071;

		public const int ExosisterGibrine = 5530780;

		public const int ExosisterAsophiel = 41524885;

		public const int ExosistersMagnifica = 59242457;

		public const int TellarknightConstellarCaduceus = 58858807;

		public const int StellarknightConstellarDiamond = 9272381;

		public const int DivineArsenalAAZEUS_SkyThunder = 90448279;

		public const int DonnerDaggerFurHire = 8728498;

		public const int NaturalExterio = 99916754;

		public const int NaturalBeast = 33198837;

		public const int ImperialOrder = 61740673;

		public const int SwordsmanLV7 = 37267041;

		public const int RoyalDecree = 51452091;

		public const int Number41BagooskatheTerriblyTiredTapir = 90590303;

		public const int InspectorBoarder = 15397015;

		public const int DimensionShifter = 91800273;
	}

	private const int SetcodeTimeLord = 74;

	private const int SetcodeShadoll = 157;

	private const int SetcodeInferoid = 187;

	private const int SetcodeOrcust = 283;

	private const int SetcodeExosister = 370;

	private const int SetcodeTearlaments = 385;

	private List<int> SetcodeForDiamond = new List<int> { 157, 187, 385 };

	private List<int> affectGraveCardIdList = new List<int>
	{
		71344451, 40975243, 87746184, 70534340, 45906428, 71490127, 3659803, 12071500, 6077601, 11827244,
		95238394, 81223446, 40003819, 72490637, 21011044, 59419719, 14735698, 45410988
	};

	private Dictionary<int, List<int>> DeckCountTable = new Dictionary<int, List<int>>
	{
		{
			3,
			new List<int> { 16474916, 43863925, 37343995, 16889337, 67972302, 23434538, 14558127, 77913594, 77891946 }
		},
		{
			2,
			new List<int> { 79858629, 5352328, 84211599, 24224830, 197042, 10045474 }
		},
		{
			1,
			new List<int> { 4408198 }
		}
	};

	private Dictionary<int, int> ExosisterMentionTable = new Dictionary<int, int>
	{
		{ 16474916, 43863925 },
		{ 43863925, 16474916 },
		{ 79858629, 5352328 },
		{ 5352328, 79858629 },
		{ 37343995, 16474916 }
	};

	private List<int> ExosisterSpellTrapList = new List<int> { 77913594, 4408198, 77891946, 197042 };

	private List<int> currentNegatingIdList = new List<int>();

	private bool enemyActivateMaxxC;

	private bool enemyActivateLockBird;

	private bool enemyMoveGrave;

	private bool paxCallToField;

	private List<int> infiniteImpermanenceList = new List<int>();

	private bool summoned;

	private bool elisEffect1Activated;

	private bool stellaEffect1Activated;

	private bool irenaEffect1Activated;

	private bool sophiaEffect1Activated;

	private bool marthaEffect1Activated;

	private bool mikailisEffect1Activated;

	private bool mikailisEffect3Activated;

	private bool kaspitellEffect1Activated;

	private bool kaspitellEffect3Activated;

	private bool gibrineEffect1Activated;

	private bool gibrineEffect3Activated;

	private bool asophielEffect1Activated;

	private bool asophielEffect3Activated;

	private bool sakitamaEffect1Activated;

	private List<int> exosisterTransformEffectList = new List<int>();

	private List<int> oncePerTurnEffectActivatedList = new List<int>();

	private List<ClientCard> activatedMagnificaList = new List<ClientCard>();

	private List<ClientCard> targetedMagnificaList = new List<ClientCard>();

	private List<int> transformDestList = new List<int>();

	private List<ClientCard> spSummonThisTurn = new List<ClientCard>();

	private bool potActivate;

	private List<ClientCard> removeChosenList = new List<ClientCard>();

	public ExosisterExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 59242457, ExosistersMagnificaActivateTrigger);
		AddExecutor(ExecutorType.Activate, 42741437, ExosisterMikailisActivate);
		AddExecutor(ExecutorType.Activate, 59242457, ExosistersMagnificaActivateBanish);
		AddExecutor(ExecutorType.Activate, 197042, ExosisterReturniaActivate);
		AddExecutor(ExecutorType.Activate, 77891946, ExosisterVadisActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 9272381);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossomActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, DefaultExosisterTransform);
		AddExecutor(ExecutorType.Activate, 4408198, ExosisterArmentActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 84211599, PotofExtravaganceActivate);
		AddExecutor(ExecutorType.Activate, 16889337);
		AddExecutor(ExecutorType.Activate, 8728498, DonnerDaggerFurHireActivate);
		AddExecutor(ExecutorType.Activate, 78135071, ExosisterKaspitellActivate);
		AddExecutor(ExecutorType.Activate, 5530780, ExosisterGibrineActivate);
		AddExecutor(ExecutorType.Activate, 41524885, ExosisterAsophielActivate);
		AddExecutor(ExecutorType.Activate, 5352328, ExosisterSophiaActivate);
		AddExecutor(ExecutorType.Activate, 79858629, ExosisterIreneActivate);
		AddExecutor(ExecutorType.Activate, 43863925, ExosisterStellaActivate);
		AddExecutor(ExecutorType.Activate, 16474916, ExosisterElisActivate);
		AddExecutor(ExecutorType.Activate, 67972302, SakitamaActivate);
		AddExecutor(ExecutorType.Activate, 77913594, ExosisterPaxActivate);
		AddExecutor(ExecutorType.Activate, 43863925, ExosisterStellaSecondActivate);
		AddExecutor(ExecutorType.SpSummon, 9272381);
		AddExecutor(ExecutorType.SpSummon, 8728498, DonnerDaggerFurHireSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 42741437, ExosisterMikailisAdvancedSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 78135071, ExosisterKaspitellAdvancedSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 78135071, ExosisterKaspitellSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 42741437, ExosisterMikailisSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 58858807, TellarknightConstellarCaduceusSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 59242457, ExosistersMagnificaSpSummonCheck);
		AddExecutor(ExecutorType.SpSummon, 46772449, base.DefaultEvilswarmExcitonKnightSummon);
		AddExecutor(ExecutorType.Activate, 46772449, base.DefaultEvilswarmExcitonKnightEffect);
		AddExecutor(ExecutorType.Summon, 43863925, ExosisterAvoidMaxxCSummonCheck);
		AddExecutor(ExecutorType.Summon, 5352328, ExosisterAvoidMaxxCSummonCheck);
		AddExecutor(ExecutorType.Summon, 79858629, ExosisterAvoidMaxxCSummonCheck);
		AddExecutor(ExecutorType.Summon, 16474916, ExosisterAvoidMaxxCSummonCheck);
		AddExecutor(ExecutorType.Activate, 37343995, ExosisterMarthaActivate);
		AddExecutor(ExecutorType.Summon, 43863925, ExosisterStellaSummonCheck);
		AddExecutor(ExecutorType.Summon, 16889337, AratamaSummonCheck);
		AddExecutor(ExecutorType.Summon, ExosisterForElisSummonCheck);
		AddExecutor(ExecutorType.Summon, ForSakitamaSummonCheck);
		AddExecutor(ExecutorType.Summon, 79858629, ExosisterIreneSummonCheck);
		AddExecutor(ExecutorType.Summon, Level4SummonCheck);
		AddExecutor(ExecutorType.Summon, ExosisterForArmentSummonCheck);
		AddExecutor(ExecutorType.Summon, ForDonnerSummonCheck);
		AddExecutor(ExecutorType.Activate, 77913594, ExosisterPaxActivateForEndSearch);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.SpellSet, SpellSetCheck);
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

	public ClientCard GetProblematicEnemyMonster(int attack = 0, bool canBeTarget = false)
	{
		List<ClientCard> list = (from c in base.Enemy.GetMonsters()
			where c?.Data != null && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())
			select c).ToList();
		if (list.Count > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			return list[0];
		}
		List<ClientCard> list2 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterDangerous() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list2.Count > 0)
		{
			list2.Sort(CardContainer.CompareCardAttack);
			list2.Reverse();
			return list2[0];
		}
		List<ClientCard> list3 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && c.IsMonsterInvincible() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list3.Count > 0)
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
		if (list4.Count > 0)
		{
			list4.Sort(CardContainer.CompareCardAttack);
			list4.Reverse();
			return list4[0];
		}
		return null;
	}

	public ClientCard GetProblematicEnemyCard(bool canBeTarget = false)
	{
		List<ClientCard> list = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && !removeChosenList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list.Count > 0)
		{
			list.Sort(CardContainer.CompareCardAttack);
			list.Reverse();
			return list[0];
		}
		List<ClientCard> list2 = base.Enemy.SpellZone.Where((ClientCard c) => c?.Data != null && !removeChosenList.Contains(c) && c.IsFloodgate() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list2.Count > 0)
		{
			return ShuffleCardList(list2)[0];
		}
		List<ClientCard> list3 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && !removeChosenList.Contains(c) && c.IsMonsterDangerous() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list3.Count > 0 && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			list3.Sort(CardContainer.CompareCardAttack);
			list3.Reverse();
			return list3[0];
		}
		List<ClientCard> list4 = base.Enemy.MonsterZone.Where((ClientCard c) => c?.Data != null && !removeChosenList.Contains(c) && c.IsMonsterInvincible() && c.IsFaceup() && (!canBeTarget || !c.IsShouldNotBeTarget())).ToList();
		if (list4.Count > 0)
		{
			list4.Sort(CardContainer.CompareCardAttack);
			list4.Reverse();
			return list4[0];
		}
		List<ClientCard> list5 = (from c in base.Enemy.GetMonsters()
			where !removeChosenList.Contains(c)
			select c).ToList();
		if (list5.Count > 0)
		{
			list5.Sort(CardContainer.CompareCardAttack);
			list5.Reverse();
			foreach (ClientCard item in list5)
			{
				if ((item.HasType(CardType.Fusion) || item.HasType(CardType.Ritual) || item.HasType(CardType.Synchro) || item.HasType(CardType.Xyz) || (item.HasType(CardType.Link) && item.LinkCount >= 2)) && (!canBeTarget || (!item.IsShouldNotBeTarget() && !item.IsShouldNotBeMonsterTarget())))
				{
					return item;
				}
			}
		}
		List<ClientCard> list6 = (from c in base.Enemy.GetSpells()
			where c.IsFaceup() && !removeChosenList.Contains(c) && (c.HasType(CardType.Equip) || c.HasType(CardType.Pendulum) || c.HasType(CardType.Field) || c.HasType(CardType.Continuous))
			select c).ToList();
		if (list6.Count > 0)
		{
			return ShuffleCardList(list6)[0];
		}
		return null;
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

	public void CheckEnemyMoveGrave()
	{
		if (base.Duel.LastChainPlayer != 1)
		{
			return;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (base.Duel.LastChainLocation == CardLocation.Grave && lastChainCard.Location == CardLocation.Grave)
		{
			Logger.DebugWriteLine("===Exosister: enemy activate effect from GY.");
			enemyMoveGrave = true;
			return;
		}
		if (affectGraveCardIdList.Contains(lastChainCard.Id))
		{
			Logger.DebugWriteLine("===Exosister: enemy activate effect that affect GY.");
			enemyMoveGrave = true;
			return;
		}
		foreach (ClientCard lastChainTarget in base.Duel.LastChainTargets)
		{
			if (lastChainTarget.Location == CardLocation.Grave)
			{
				Logger.DebugWriteLine("===Exosister: enemy target cards of GY.");
				enemyMoveGrave = true;
				break;
			}
		}
	}

	public int CheckExosisterMentionCard(int id)
	{
		if (!ExosisterMentionTable.ContainsKey(id))
		{
			return 0;
		}
		return ExosisterMentionTable[id];
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
		return true;
	}

	public bool CheckLessOperation()
	{
		if (!enemyActivateMaxxC)
		{
			return false;
		}
		return CheckAtAdvantage();
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

	public bool CheckAbleForXyz(ClientCard card)
	{
		if (card.IsFaceup() && !card.HasType(CardType.Xyz) && !card.HasType(CardType.Link) && !card.HasType(CardType.Token))
		{
			return card.Level == 4;
		}
		return false;
	}

	public bool CheckMarthaActivatable()
	{
		if (!marthaEffect1Activated && CheckCalledbytheGrave(37343995) == 0 && CheckRemainInDeck(16474916) > 0)
		{
			return !base.Bot.GetMonsters().Any((ClientCard card) => card.IsFacedown() || !card.HasType(CardType.Xyz));
		}
		return false;
	}

	public List<ClientCard> CheckDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		return base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && card.HasSetcode(283)).ToList();
	}

	public bool SpellNegatable(bool isCounter = false, ClientCard target = null)
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

	public bool CheckWhetherNegated(bool disablecheck = true)
	{
		if ((base.Card.IsSpell() || base.Card.IsTrap()) && SpellNegatable())
		{
			return true;
		}
		if (CheckCalledbytheGrave(base.Card.Id) > 0)
		{
			return true;
		}
		if (base.Card.IsMonster() && base.Card.Location == CardLocation.MonsterZone && base.Card.IsDefense() && (base.Enemy.MonsterZone.GetFirstMatchingFaceupCard((ClientCard card) => card.IsCode(90590303) && card.IsDefense() && !card.IsDisabled()) != null || base.Bot.MonsterZone.GetFirstMatchingFaceupCard((ClientCard card) => card.IsCode(90590303) && card.IsDefense() && !card.IsDisabled()) != null))
		{
			return true;
		}
		if (disablecheck)
		{
			return base.Card.IsDisabled();
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

	public void SelectXyzMaterial(int num = 2, bool needExosister = false)
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where CheckAbleForXyz(card)
			select card).ToList();
		if ((list != null && list.Count() < num) || (needExosister && !list.Any((ClientCard card) => card.HasSetcode(370))))
		{
			return;
		}
		List<ClientCard> list2 = new List<ClientCard>();
		if (needExosister)
		{
			List<ClientCard> list3 = list.Where((ClientCard card) => card.HasSetcode(370)).ToList();
			list3.Sort(CardContainer.CompareCardAttack);
			ClientCard item = list3[0];
			list2.Add(item);
			list.Remove(item);
		}
		List<ClientCard> list4 = list.Where((ClientCard card) => (card?.Data != null && !card.HasSetcode(370)) || (exosisterTransformEffectList.Contains(card.Id) && card.Id != 37343995)).ToList();
		list4.Sort(CardContainer.CompareCardAttack);
		foreach (ClientCard item2 in list4)
		{
			list2.Add(item2);
			if (list2.Count() >= num)
			{
				base.AI.SelectMaterials(list2);
				return;
			}
		}
		List<ClientCard> list5 = list.Where((ClientCard card) => card.Id == 37343995 || !exosisterTransformEffectList.Contains(card.Id)).ToList();
		list5.Sort(CardContainer.CompareCardAttack);
		foreach (ClientCard item3 in list5)
		{
			list2.Add(item3);
			if (list2.Count() >= num)
			{
				base.AI.SelectMaterials(list2);
				break;
			}
		}
	}

	public void SelectDetachMaterial(ClientCard activateCard)
	{
		base.AI.SelectCard(0);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (card == null)
		{
			return;
		}
		if (player == 1)
		{
			if (card.IsCode(23434538) && CheckCalledbytheGrave(23434538) == 0)
			{
				enemyActivateMaxxC = true;
			}
			if (card.IsCode(94145021) && CheckCalledbytheGrave(94145021) == 0)
			{
				enemyActivateLockBird = true;
			}
			if (card.IsCode(10045474))
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
			if (base.Duel.LastChainLocation == CardLocation.Grave && card.Location == CardLocation.Grave)
			{
				Logger.DebugWriteLine("===Exosister: enemy activate effect from GY.");
				enemyMoveGrave = true;
			}
		}
		base.OnChaining(player, card);
	}

	public override void OnSelectChain(IList<ClientCard> cards)
	{
		int lastChainPlayer = base.Duel.LastChainPlayer;
		base.Util.GetLastChainCard();
		if (lastChainPlayer == 1)
		{
			foreach (ClientCard lastChainTarget in base.Duel.LastChainTargets)
			{
				if (lastChainTarget.Location == CardLocation.Grave)
				{
					Logger.DebugWriteLine("===Exosister: enemy target cards of GY.");
					enemyMoveGrave = true;
					break;
				}
			}
		}
		base.OnSelectChain(cards);
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
		enemyMoveGrave = false;
		paxCallToField = false;
		potActivate = false;
		transformDestList.Clear();
		targetedMagnificaList.Clear();
		if (activatedMagnificaList.Count() > 0)
		{
			for (int num = activatedMagnificaList.Count() - 1; num >= 0; num--)
			{
				ClientCard clientCard = activatedMagnificaList[num];
				if (clientCard == null || clientCard.IsFacedown() || clientCard.Location != CardLocation.MonsterZone)
				{
					activatedMagnificaList.RemoveAt(num);
				}
			}
		}
		if (spSummonThisTurn.Count() > 0)
		{
			for (int num2 = spSummonThisTurn.Count() - 1; num2 >= 0; num2--)
			{
				ClientCard clientCard2 = spSummonThisTurn[num2];
				if (clientCard2 == null || clientCard2.IsFacedown() || clientCard2.Location != CardLocation.MonsterZone)
				{
					spSummonThisTurn.RemoveAt(num2);
				}
			}
		}
		base.OnChainEnd();
	}

	public override void OnNewTurn()
	{
		enemyActivateMaxxC = false;
		enemyActivateLockBird = false;
		infiniteImpermanenceList.Clear();
		currentNegatingIdList.Clear();
		summoned = false;
		elisEffect1Activated = false;
		stellaEffect1Activated = false;
		irenaEffect1Activated = false;
		sophiaEffect1Activated = false;
		marthaEffect1Activated = false;
		mikailisEffect1Activated = false;
		mikailisEffect3Activated = false;
		kaspitellEffect1Activated = false;
		kaspitellEffect3Activated = false;
		gibrineEffect1Activated = false;
		gibrineEffect3Activated = false;
		asophielEffect1Activated = false;
		asophielEffect3Activated = false;
		sakitamaEffect1Activated = false;
		exosisterTransformEffectList.Clear();
		oncePerTurnEffectActivatedList.Clear();
		activatedMagnificaList.Clear();
		spSummonThisTurn.Clear();
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		bool flag = false;
		if (hint == 509 && min == 1 && max == 1 && transformDestList.Count() > 0 && cards.All((ClientCard card) => card.Location == CardLocation.Extra && card.Rank == 4 && card.HasSetcode(370)))
		{
			flag = true;
		}
		if (hint == 507 && min == 1 && max == 1 && transformDestList.Count() > 0 && cards.All((ClientCard card) => card.Location == CardLocation.Overlay))
		{
			flag = true;
		}
		if (flag)
		{
			for (int num = 0; num < transformDestList.Count(); num++)
			{
				int targetId = transformDestList[num];
				ClientCard clientCard = cards.FirstOrDefault((ClientCard card) => card.IsCode(targetId));
				if (clientCard != null)
				{
					List<ClientCard> list = new List<ClientCard>();
					list.Add(clientCard);
					transformDestList.RemoveAt(num);
					spSummonThisTurn.AddRange(list);
					return base.Util.CheckSelectCount(list, cards, min, max);
				}
			}
		}
		if (base.Util.ChainContainsCard(15693423) && base.Util.ChainContainPlayer(1) && hint == 503)
		{
			int num2 = base.Bot.GetMonsterCount() + base.Bot.GetSpellCount();
			int num3 = base.Enemy.GetMonsterCount() + base.Enemy.GetSpellCount();
			if (num2 - num3 == min && min == max)
			{
				Logger.DebugWriteLine("===Exosister: Evenly Matched activated.");
				List<ClientCard> list2 = new List<ClientCard>();
				list2.AddRange(base.Bot.GetMonsters());
				list2.AddRange(base.Bot.GetSpells());
				List<ClientCard> list3 = new List<ClientCard>();
				List<ClientCard> magnificaList = list2.Where((ClientCard card) => card.IsCode(59242457)).ToList();
				if (magnificaList.Count > 0)
				{
					list2.RemoveAll((ClientCard c) => magnificaList.Contains(c));
					list3.AddRange(magnificaList);
				}
				if (!mikailisEffect1Activated)
				{
					List<ClientCard> mikailisList = list2.Where((ClientCard card) => spSummonThisTurn.Contains(card) && card.IsCode(42741437) && card.IsFaceup()).ToList();
					if (mikailisList.Count > 0)
					{
						list2.RemoveAll((ClientCard c) => mikailisList.Contains(c));
						list3.AddRange(mikailisList);
					}
				}
				if (!gibrineEffect1Activated)
				{
					List<ClientCard> gibrineList = list2.Where((ClientCard card) => spSummonThisTurn.Contains(card) && card.IsCode(5530780) && card.IsFaceup()).ToList();
					if (gibrineList.Count > 0)
					{
						list2.RemoveAll((ClientCard c) => gibrineList.Contains(c));
						list3.AddRange(gibrineList);
					}
				}
				if (!oncePerTurnEffectActivatedList.Contains(77891946))
				{
					List<ClientCard> vadisList = list2.Where((ClientCard card) => card.IsCode(77891946) && card.IsFacedown()).ToList();
					if (vadisList.Count > 0)
					{
						list2.RemoveAll((ClientCard c) => vadisList.Contains(c));
						list3.AddRange(vadisList);
					}
				}
				List<ClientCard> xyzList = list2.Where((ClientCard card) => card.IsMonster() && card.HasType(CardType.Xyz)).ToList();
				if (xyzList.Count > 0)
				{
					xyzList.Sort(CardContainer.CompareCardAttack);
					xyzList.Reverse();
					list2.RemoveAll((ClientCard c) => xyzList.Contains(c));
					list3.AddRange(xyzList);
				}
				List<ClientCard> monsterList = list2.Where((ClientCard card) => card.IsMonster()).ToList();
				if (monsterList.Count > 0)
				{
					monsterList.Sort(CardContainer.CompareCardAttack);
					monsterList.Reverse();
					list2.RemoveAll((ClientCard c) => monsterList.Contains(c));
					list3.AddRange(monsterList);
				}
				List<ClientCard> faceDownList = list2.Where((ClientCard card) => card.IsFacedown()).ToList();
				if (faceDownList.Count > 0)
				{
					list2.RemoveAll((ClientCard c) => faceDownList.Contains(c));
					list3.AddRange(ShuffleCardList(faceDownList));
				}
				list3.Reverse();
				return base.Util.CheckSelectCount(list3, cards, min, max);
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (namedCard != null)
		{
			if (base.Util.IsTurn1OrMain2())
			{
				bool flag = false;
				if (cardId == 90448279 || cardId == 59242457)
				{
					flag = true;
				}
				if (!namedCard.HasType(CardType.Xyz))
				{
					flag = true;
				}
				if (flag)
				{
					return CardPosition.FaceUpDefence;
				}
			}
			if (base.Duel.Player == 1 && (!namedCard.HasType(CardType.Xyz) || namedCard.Defense >= namedCard.Attack || base.Util.IsOneEnemyBetterThanValue(namedCard.Attack, onlyATK: true)))
			{
				return CardPosition.FaceUpDefence;
			}
			int value = Math.Max(base.Util.GetBestAttack(base.Bot), namedCard.Attack);
			if (base.Util.IsAllEnemyBetterThanValue(value, onlyATK: true))
			{
				return CardPosition.FaceUpDefence;
			}
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == base.Util.GetStringId(59242457, 2))
		{
			return true;
		}
		if (desc == base.Util.GetStringId(77913594, 1))
		{
			return paxCallToField;
		}
		return base.OnSelectYesNo(desc);
	}

	public override int OnSelectOption(IList<int> options)
	{
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		for (int i = 0; i < options.Count(); i++)
		{
			int num4 = options[i];
			if (num4 == base.Util.GetStringId(197042, 0))
			{
				num = i;
			}
			else if (num4 == base.Util.GetStringId(197042, 1))
			{
				num2 = i;
			}
			else if (num4 == base.Util.GetStringId(197042, 2))
			{
				num3 = i;
			}
		}
		if (num >= 0 || num2 >= 0 || num3 >= 0)
		{
			if (num < 0 && num2 < 0)
			{
				return num3;
			}
			if (num2 >= 0)
			{
				ClientCard problematicEnemyCard = GetProblematicEnemyCard(canBeTarget: true);
				if (problematicEnemyCard != null)
				{
					base.AI.SelectCard(problematicEnemyCard);
					return num2;
				}
				problematicEnemyCard = GetBestEnemyCard();
				if (problematicEnemyCard != null)
				{
					base.AI.SelectCard(problematicEnemyCard);
					return num2;
				}
			}
			_ = 0;
		}
		int num5 = -1;
		int num6 = -1;
		for (int j = 0; j < options.Count(); j++)
		{
			int num7 = options[j];
			if (num7 == base.Util.GetStringId(84211599, 0))
			{
				num6 = j;
			}
			else if (num7 == base.Util.GetStringId(84211599, 1))
			{
				num5 = j;
			}
		}
		if (num6 >= 0 || num5 >= 0)
		{
			if (base.Bot.ExtraDeck.Count() > 9 && num5 >= 0)
			{
				return num5;
			}
			return num6;
		}
		return base.OnSelectOption(options);
	}

	public bool AshBlossomActivate()
	{
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

	public bool CalledbytheGraveActivate()
	{
		if (CheckWhetherNegated())
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
				if (code == 91800273)
				{
					return false;
				}
				ClientCard firstMatchingCard = base.Enemy.Graveyard.GetFirstMatchingCard((ClientCard card) => card.IsMonster() && card.IsOriginalCode(code));
				if (firstMatchingCard != null)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					base.AI.SelectCard(firstMatchingCard);
					currentNegatingIdList.Add(code);
					return true;
				}
			}
			foreach (ClientCard item in base.Enemy.Graveyard)
			{
				if (base.Duel.ChainTargets.Contains(item))
				{
					int id = item.Id;
					base.AI.SelectCard(item);
					currentNegatingIdList.Add(id);
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
			int originCode = list2[0].GetOriginCode();
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(list2);
			currentNegatingIdList.Add(originCode);
			return true;
		}
		return false;
	}

	public List<ClientCard> GetPotofExtravaganceBanish()
	{
		List<ClientCard> banishList = new List<ClientCard>();
		ClientCard clientCard = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(90448279));
		if (clientCard != null)
		{
			banishList.Add(clientCard);
		}
		ClientCard clientCard2 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(9272381));
		if (clientCard2 != null)
		{
			banishList.Add(clientCard2);
		}
		ClientCard clientCard3 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(58858807));
		if (clientCard3 != null)
		{
			banishList.Add(clientCard3);
		}
		ClientCard clientCard4 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(46772449));
		if (clientCard4 != null)
		{
			banishList.Add(clientCard4);
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card.IsCode(41524885)) > 1)
		{
			ClientCard item = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(41524885));
			banishList.Add(item);
		}
		ClientCard clientCard5 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(5530780));
		if (clientCard5 != null)
		{
			banishList.Add(clientCard5);
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card.IsCode(42741437)) > 2)
		{
			ClientCard item2 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(42741437));
			banishList.Add(item2);
		}
		ClientCard clientCard6 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(41524885) && !banishList.Contains(card));
		if (clientCard6 != null)
		{
			banishList.Add(clientCard6);
		}
		ClientCard clientCard7 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(8728498));
		if (clientCard7 != null)
		{
			banishList.Add(clientCard7);
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card.IsCode(78135071)) > 1)
		{
			ClientCard item3 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(78135071));
			banishList.Add(item3);
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card.IsCode(59242457)) > 1)
		{
			ClientCard item4 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(59242457));
			banishList.Add(item4);
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card.IsCode(42741437)) > 1)
		{
			ClientCard item5 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(42741437) && !banishList.Contains(card));
			banishList.Add(item5);
		}
		ClientCard clientCard8 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(59242457) && !banishList.Contains(card));
		if (clientCard8 != null)
		{
			banishList.Add(clientCard8);
		}
		ClientCard clientCard9 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(78135071) && !banishList.Contains(card));
		if (clientCard9 != null)
		{
			banishList.Add(clientCard9);
		}
		ClientCard clientCard10 = base.Bot.ExtraDeck.FirstOrDefault((ClientCard card) => card.IsCode(42741437) && !banishList.Contains(card));
		if (clientCard10 != null)
		{
			banishList.Add(clientCard10);
		}
		return banishList;
	}

	public bool PotofExtravaganceActivate()
	{
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<ClientCard> potofExtravaganceBanish = GetPotofExtravaganceBanish();
		List<int> list = new List<int>();
		if (CheckMarthaActivatable())
		{
			if (!base.Bot.HasInHand(37343995))
			{
				list.Add(37343995);
			}
			if (base.Bot.HasInHand(37343995) && !base.Bot.HasInHandOrInSpellZone(24224830))
			{
				list.Add(24224830);
			}
		}
		int num = base.Bot.Hand.Count((ClientCard card) => card?.Data != null && card.HasSetcode(370));
		if (!stellaEffect1Activated && CheckCalledbytheGrave(43863925) == 0)
		{
			if (!base.Bot.HasInHand(43863925) && num > 0)
			{
				list.Add(43863925);
			}
			if (base.Bot.HasInHand(43863925) && num == 0)
			{
				list.AddRange(new List<int> { 5352328, 79858629, 43863925, 37343995, 16474916 });
			}
		}
		if (num >= 0 && !base.Bot.HasInHandOrInSpellZone(197042))
		{
			list.Add(197042);
		}
		List<int> collection = new List<int> { 16889337, 67972302, 23434538, 14558127, 10045474, 24224830, 77891946, 197042, 77913594 };
		list.AddRange(collection);
		base.AI.SelectCard(potofExtravaganceBanish);
		base.AI.SelectNextCard(list);
		if (base.Card.Location != CardLocation.SpellZone)
		{
			SelectSTPlace(null, avoidImpermanence: true);
		}
		potActivate = true;
		return true;
	}

	public bool SakitamaActivate()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1)
			{
				base.AI.SelectCard(16889337, 67972302);
				sakitamaEffect1Activated = true;
				return true;
			}
			if ((!CheckLessOperation() && base.Bot.HasInExtra(8728498) && !base.Bot.HasInHand(37343995)) || base.Bot.HasInHandOrInSpellZone(197042))
			{
				List<ClientCard> list = (from card in base.Bot.GetMonsters()
					where card.IsFaceup() && !card.HasType(CardType.Xyz) && card.Level != 4 && (card.Data == null || !card.HasSetcode(370))
					select card).ToList();
				if (list.Count() > 0)
				{
					if (list.Count() == 1)
					{
						List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
							where card.IsFaceup() && !card.HasType(CardType.Xyz)
							select card).ToList();
						list2.Sort(CardContainer.CompareCardAttack);
						list.AddRange(list2);
					}
					if (list.Count() == 1)
					{
						Logger.DebugWriteLine("===Exosister: activate sakitama for donner");
						base.AI.SelectCard(16889337, 67972302);
						sakitamaEffect1Activated = true;
						return true;
					}
				}
			}
			return false;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			base.AI.SelectCard(67972302, 16889337);
			return true;
		}
		return true;
	}

	public bool DonnerDaggerFurHireActivate()
	{
		if (CheckAtAdvantage() && !base.Bot.HasInHand(37343995) && !base.Bot.HasInHandOrInSpellZone(197042))
		{
			return false;
		}
		ClientCard clientCard = base.Util.GetProblematicEnemyMonster(0, canBeTarget: true);
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
			return true;
		}
		return false;
	}

	public bool ExosisterElisActivate()
	{
		if (base.ActivateDescription != base.Util.GetStringId(16474916, 0))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1)
		{
			elisEffect1Activated = true;
			return true;
		}
		return false;
	}

	public bool ExosisterStellaActivate()
	{
		return ExosisterStellaActivateInner(checkMartha: true);
	}

	public bool ExosisterStellaSecondActivate()
	{
		return ExosisterStellaActivateInner();
	}

	public bool ExosisterStellaActivateInner(bool checkMartha = false)
	{
		if (base.ActivateDescription != base.Util.GetStringId(43863925, 0) || CheckWhetherNegated())
		{
			return false;
		}
		bool flag = base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) >= 2;
		if (CheckLessOperation() && flag)
		{
			return false;
		}
		if (checkMartha && base.Bot.HasInHand(37343995) && flag && base.Bot.Hand.Count((ClientCard card) => card.IsMonster() && card.HasSetcode(37343995)) == 1)
		{
			return false;
		}
		base.AI.SelectCard(5352328, 79858629, 43863925, 16474916);
		stellaEffect1Activated = true;
		return true;
	}

	public bool ExosisterIreneActivate()
	{
		if (base.ActivateDescription != base.Util.GetStringId(79858629, 0) || CheckWhetherNegated())
		{
			return false;
		}
		List<int> list = new List<int>();
		foreach (int item in new List<int> { 79858629, 5352328, 4408198 })
		{
			if (base.Bot.HasInHand(item))
			{
				list.Add(item);
			}
		}
		if (elisEffect1Activated || base.Bot.Hand.Count((ClientCard card) => card.IsCode(16474916)) > 1)
		{
			list.Add(16474916);
		}
		foreach (int cardId in new List<int> { 77913594, 197042, 77891946 })
		{
			if ((oncePerTurnEffectActivatedList.Contains(cardId) && base.Bot.HasInHand(cardId)) || base.Bot.Hand.Count((ClientCard card) => card.IsCode(cardId)) > 1)
			{
				list.Add(cardId);
			}
		}
		if (list.Count() > 0)
		{
			Logger.DebugWriteLine("===Exosister: irene return " + list[0]);
			base.AI.SelectCard(list);
			return true;
		}
		return false;
	}

	public bool ExosisterSophiaActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(5352328, 0) && !CheckWhetherNegated())
		{
			sophiaEffect1Activated = true;
			return true;
		}
		return false;
	}

	public bool ExosisterMarthaActivate()
	{
		if (base.ActivateDescription != base.Util.GetStringId(37343995, 0))
		{
			return false;
		}
		if (CheckLessOperation() && base.Bot.GetMonsterCount() > 0)
		{
			return false;
		}
		marthaEffect1Activated = true;
		return true;
	}

	public bool DefaultExosisterTransform()
	{
		List<int> list = new List<int> { 16474916, 43863925, 79858629, 5352328, 37343995 };
		if (base.Card.IsDisabled() || !list.Contains(base.Card.Id))
		{
			return false;
		}
		if (!new List<int>
		{
			base.Util.GetStringId(16474916, 1),
			base.Util.GetStringId(43863925, 1),
			base.Util.GetStringId(79858629, 1),
			base.Util.GetStringId(5352328, 1),
			base.Util.GetStringId(37343995, 1)
		}.Contains(base.ActivateDescription) && base.ActivateDescription != -1)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(42741437) && !mikailisEffect1Activated && (base.Duel.Player == 1 || !mikailisEffect3Activated) && !transformDestList.Contains(42741437) && base.Bot.HasInExtra(42741437))
		{
			exosisterTransformEffectList.Add(base.Card.Id);
			transformDestList.Add(42741437);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(78135071) && !kaspitellEffect3Activated && base.Duel.Player == 0 && !transformDestList.Contains(78135071) && base.Bot.HasInExtra(78135071))
		{
			exosisterTransformEffectList.Add(base.Card.Id);
			transformDestList.Add(78135071);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(5530780) && !gibrineEffect1Activated && !transformDestList.Contains(5530780) && base.Bot.HasInExtra(5530780))
		{
			exosisterTransformEffectList.Add(base.Card.Id);
			transformDestList.Add(5530780);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(41524885) && !asophielEffect1Activated && !transformDestList.Contains(41524885) && base.Bot.HasInExtra(41524885))
		{
			exosisterTransformEffectList.Add(base.Card.Id);
			transformDestList.Add(41524885);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(78135071) && !kaspitellEffect1Activated && !transformDestList.Contains(78135071) && base.Bot.HasInExtra(78135071))
		{
			exosisterTransformEffectList.Add(base.Card.Id);
			transformDestList.Add(78135071);
			return true;
		}
		return false;
	}

	public bool ExosisterMikailisActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(42741437, 0))
		{
			if (base.Duel.Player == 0 && !mikailisEffect3Activated && base.Duel.Phase < DuelPhase.End && !DefaultOnBecomeTarget())
			{
				return false;
			}
			ClientCard problematicEnemyCard = GetProblematicEnemyCard(canBeTarget: true);
			if (problematicEnemyCard != null && base.Duel.LastChainPlayer != 0)
			{
				removeChosenList.Add(problematicEnemyCard);
				mikailisEffect1Activated = true;
				base.AI.SelectCard(problematicEnemyCard);
				return true;
			}
			if (base.Duel.LastChainPlayer == 1)
			{
				List<ClientCard> list = base.Duel.LastChainTargets.Where((ClientCard card) => card.Controller == 1 && (card.Location == CardLocation.Grave || card.Location == CardLocation.MonsterZone || card.Location == CardLocation.SpellZone || card.Location == CardLocation.FieldZone)).ToList();
				if (list.Count() > 0)
				{
					mikailisEffect1Activated = true;
					base.AI.SelectCard(ShuffleCardList(list));
					return true;
				}
			}
			problematicEnemyCard = GetBestEnemyCard(onlyFaceup: false, canBeTarget: true, checkGrave: true);
			if ((DefaultOnBecomeTarget() && !base.Util.ChainContainsCard(15693423)) || base.Bot.UnderAttack || (base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer != 0) || (base.Duel.Player == 0 && base.Bot.GetMonsters().Count((ClientCard card) => card.HasType(CardType.Xyz) && card.Rank == 4 && card.HasSetcode(370)) == 2 && base.Duel.LastChainPlayer != 0) || (base.Duel.Player == 1 && base.Enemy.GetMonsterCount() >= 2))
			{
				mikailisEffect1Activated = true;
				base.AI.SelectCard(problematicEnemyCard);
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		List<int> obj = new List<int> { 197042, 77891946, 77913594, 4408198 };
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>();
		foreach (int item in obj)
		{
			if (base.Bot.HasInHandOrInSpellZone(item) || CheckRemainInDeck(item) == 0)
			{
				list3.Add(item);
			}
			else if (item == 197042 && base.Bot.GetMonsters().Any((ClientCard card) => card.IsFacedown() || !card.HasSetcode(370)))
			{
				list3.Add(item);
			}
			else
			{
				list2.Add(item);
			}
		}
		list2.AddRange(list3);
		mikailisEffect3Activated = true;
		SelectDetachMaterial(base.Card);
		base.AI.SelectNextCard(list2);
		return true;
	}

	public bool ExosisterKaspitellActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(78135071, 0) || base.ActivateDescription == -1)
		{
			if (base.Enemy.HasInMonstersZone(15397015, notDisabled: true))
			{
				return false;
			}
			kaspitellEffect1Activated = true;
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		if (CheckMarthaActivatable() && CheckRemainInDeck(37343995) > 0 && !base.Bot.HasInHand(37343995))
		{
			kaspitellEffect3Activated = true;
			SelectDetachMaterial(base.Card);
			base.AI.SelectNextCard(37343995);
			return true;
		}
		if (!summoned && !sophiaEffect1Activated && CheckCalledbytheGrave(5352328) == 0 && !base.Bot.HasInHand(5352328) && (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1 || (base.Bot.HasInHand(16474916) && !elisEffect1Activated)))
		{
			kaspitellEffect3Activated = true;
			SelectDetachMaterial(base.Card);
			base.AI.SelectNextCard(5352328);
			return true;
		}
		if (!summoned && !base.Bot.HasInHand(43863925) && !stellaEffect1Activated && CheckCalledbytheGrave(43863925) == 0 && CheckRemainInDeck(43863925) > 0 && base.Bot.Hand.Any((ClientCard card) => card?.Data != null && card.IsMonster() && card.HasSetcode(370)))
		{
			kaspitellEffect3Activated = true;
			SelectDetachMaterial(base.Card);
			base.AI.SelectNextCard(43863925);
			return true;
		}
		kaspitellEffect3Activated = true;
		SelectDetachMaterial(base.Card);
		base.AI.SelectNextCard(37343995, 43863925, 16474916, 5352328, 79858629);
		return true;
	}

	public bool ExosisterGibrineActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(5530780, 0))
		{
			if (base.Duel.Player == 1)
			{
				ClientCard shouldBeDisabledBeforeItUseEffectMonster = base.Enemy.MonsterZone.GetShouldBeDisabledBeforeItUseEffectMonster();
				if (shouldBeDisabledBeforeItUseEffectMonster != null)
				{
					gibrineEffect1Activated = true;
					base.AI.SelectCard(shouldBeDisabledBeforeItUseEffectMonster);
					return true;
				}
			}
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone && !lastChainCard.IsDisabled() && !lastChainCard.IsShouldNotBeTarget() && !lastChainCard.IsShouldNotBeMonsterTarget())
			{
				gibrineEffect1Activated = true;
				base.AI.SelectCard(lastChainCard);
				return true;
			}
			return false;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		gibrineEffect3Activated = true;
		SelectDetachMaterial(base.Card);
		return true;
	}

	public bool ExosisterAsophielActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(41524885, 0) || base.ActivateDescription == -1)
		{
			if (base.Enemy.HasInMonstersZone(15397015, notDisabled: true))
			{
				return false;
			}
			asophielEffect1Activated = true;
			return true;
		}
		if (CheckWhetherNegated())
		{
			return false;
		}
		ClientCard problematicEnemyMonster = base.Util.GetProblematicEnemyMonster(0, canBeTarget: true);
		if (problematicEnemyMonster != null)
		{
			asophielEffect3Activated = true;
			SelectDetachMaterial(base.Card);
			base.AI.SelectNextCard(problematicEnemyMonster);
			return true;
		}
		return false;
	}

	public bool ExosistersMagnificaActivateTrigger()
	{
		if (base.ActivateDescription == base.Util.GetStringId(59242457, 1))
		{
			if (activatedMagnificaList.Contains(base.Card))
			{
				if (base.Card.Overlays.Contains(42741437) && !mikailisEffect1Activated && GetProblematicEnemyCard(canBeTarget: true) != null && !base.Duel.CurrentChain.Any((ClientCard card) => card == base.Card))
				{
					transformDestList.Add(42741437);
					return true;
				}
				if (base.Card.Overlays.Contains(5530780) && !gibrineEffect1Activated && base.Enemy.MonsterZone.GetShouldBeDisabledBeforeItUseEffectMonster() != null)
				{
					transformDestList.Add(5530780);
					return true;
				}
			}
			if ((DefaultOnBecomeTarget() && !base.Util.ChainContainsCard(15693423)) || (base.Duel.CurrentChain.Any((ClientCard c) => c == base.Card) && base.Duel.LastChainPlayer != 0))
			{
				targetedMagnificaList.Add(base.Card);
				transformDestList.AddRange(new List<int> { 59242457, 42741437, 5530780, 78135071, 41524885 });
				return true;
			}
		}
		return false;
	}

	public bool ExosistersMagnificaActivateBanish()
	{
		if (base.ActivateDescription == base.Util.GetStringId(59242457, 0))
		{
			if (CheckWhetherNegated())
			{
				return false;
			}
			ClientCard clientCard = GetProblematicEnemyCard();
			bool flag = false;
			if (clientCard != null)
			{
				flag = true;
				Logger.DebugWriteLine("===Exosister: magnifica target 1: " + clientCard?.Name);
			}
			if (base.Duel.LastChainPlayer == 1 && clientCard == null)
			{
				List<ClientCard> list = base.Duel.LastChainTargets.Where((ClientCard card) => card.Controller == 1 && (card.Location == CardLocation.MonsterZone || card.Location == CardLocation.SpellZone || card.Location == CardLocation.FieldZone)).ToList();
				if (list.Count() > 0)
				{
					clientCard = ShuffleCardList(list)[0];
					Logger.DebugWriteLine("===Exosister: magnifica target 2: " + clientCard?.Name);
				}
			}
			if (clientCard == null)
			{
				clientCard = GetBestEnemyCard();
				bool flag2 = !DefaultOnBecomeTarget() || base.Util.ChainContainsCard(15693423);
				bool flag3 = !targetedMagnificaList.Contains(base.Card);
				bool flag4 = !base.Bot.UnderAttack;
				bool flag5 = base.Duel.Phase != DuelPhase.End;
				bool flag6 = base.Duel.Player == 0 || base.Enemy.GetMonsterCount() < 2;
				Logger.DebugWriteLine("===Exosister: magnifica check flag: " + flag2 + " " + flag3 + " " + flag4 + " " + flag5 + " " + flag6);
				if (flag2 && flag3 && flag4 && flag5 && flag6)
				{
					clientCard = null;
				}
			}
			if (clientCard != null && (base.Duel.LastChainPlayer != 0 || base.Util.GetLastChainCard() == base.Card))
			{
				if (flag)
				{
					removeChosenList.Add(clientCard);
				}
				Logger.DebugWriteLine("===Exosister: magnifica target final: " + clientCard?.Name);
				activatedMagnificaList.Add(base.Card);
				base.AI.SelectCard(5530780, 41524885, 78135071, 42741437);
				base.AI.SelectNextCard(clientCard);
				return true;
			}
			return false;
		}
		return false;
	}

	public bool ExosisterPaxActivate()
	{
		if (potActivate || base.Bot.LifePoints <= 800)
		{
			return false;
		}
		List<int> list = new List<int> { 5352328, 79858629, 43863925, 37343995, 16474916 };
		List<int> list2 = new List<int> { 37343995, 43863925, 77891946, 197042, 5352328, 79858629, 4408198, 16474916 };
		if (base.Duel.Player == 0 && base.Duel.LastChainPlayer != 0)
		{
			if (CheckAtAdvantage() && GetProblematicEnemyCard(canBeTarget: true) != null && CheckRemainInDeck(197042) > 0 && !base.Bot.HasInHandOrInSpellZone(197042) && base.Bot.GetMonsterCount() > 0 && base.Bot.GetMonsters().All((ClientCard card) => card.IsFaceup() && card.HasSetcode(370)))
			{
				if (base.Card.Location != CardLocation.SpellZone)
				{
					SelectSTPlace(null, avoidImpermanence: true);
				}
				oncePerTurnEffectActivatedList.Add(base.Card.Id);
				base.AI.SelectCard(197042);
				paxCallToField = false;
				return true;
			}
			if (CheckMarthaActivatable() && CheckRemainInDeck(37343995) > 0 && !base.Bot.HasInHand(37343995))
			{
				if (base.Card.Location != CardLocation.SpellZone)
				{
					SelectSTPlace(null, avoidImpermanence: true);
				}
				oncePerTurnEffectActivatedList.Add(base.Card.Id);
				base.AI.SelectCard(37343995);
				paxCallToField = false;
				return true;
			}
			if (!stellaEffect1Activated && CheckCalledbytheGrave(43863925) == 0)
			{
				if (base.Bot.Hand.Count((ClientCard card) => card.IsCode(43863925)) == 0 && CheckRemainInDeck(43863925) > 0)
				{
					bool flag = !CheckLessOperation() && summoned && base.Bot.HasInMonstersZoneOrInGraveyard(16474916);
					if (base.Bot.Hand.Any((ClientCard card) => card?.Data != null && card.IsMonster() && card.HasSetcode(370)))
					{
						if (base.Card.Location != CardLocation.SpellZone)
						{
							SelectSTPlace(null, avoidImpermanence: true);
						}
						oncePerTurnEffectActivatedList.Add(base.Card.Id);
						base.AI.SelectCard(43863925);
						paxCallToField = flag;
						return true;
					}
				}
				bool flag2 = false;
				if (base.Bot.HasInHand(43863925) && base.Bot.Hand.Count((ClientCard card) => card?.Data != null && card.IsMonster() && card.HasSetcode(370)) == 1)
				{
					flag2 = true;
				}
				if (base.Bot.HasInMonstersZone(43863925) && base.Bot.Hand.Count((ClientCard card) => card?.Data != null && card.IsMonster() && card.HasSetcode(370)) == 0)
				{
					flag2 = true;
				}
				if (flag2)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					oncePerTurnEffectActivatedList.Add(base.Card.Id);
					base.AI.SelectCard(5352328, 79858629, 37343995, 43863925, 16474916);
					paxCallToField = false;
					return true;
				}
			}
			if (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1 && summoned && !CheckLessOperation() && (sakitamaEffect1Activated || !base.Bot.HasInHand(67972302)) && (stellaEffect1Activated || !base.Bot.HasInMonstersZone(43863925)) && (elisEffect1Activated || !base.Bot.HasInHand(16474916)))
			{
				foreach (int item in list)
				{
					int num = CheckExosisterMentionCard(item);
					if (num > 0 && base.Bot.HasInMonstersZoneOrInGraveyard(item) && CheckRemainInDeck(num) > 0)
					{
						if (base.Card.Location != CardLocation.SpellZone)
						{
							SelectSTPlace(null, avoidImpermanence: true);
						}
						oncePerTurnEffectActivatedList.Add(base.Card.Id);
						base.AI.SelectCard(item);
						paxCallToField = true;
						return true;
					}
				}
			}
		}
		bool flag3 = CheckInDanger();
		CheckEnemyMoveGrave();
		bool flag4 = false;
		if (!oncePerTurnEffectActivatedList.Contains(197042) && base.Bot.HasInSpellZone(197042) && base.Bot.GetMonsters().Count() == 0)
		{
			flag4 = true;
		}
		if (enemyMoveGrave || DefaultOnBecomeTarget() || flag3 || flag4)
		{
			List<int> list3 = list;
			bool flag5 = enemyMoveGrave || flag3 || flag4;
			if (!flag5 && !base.Bot.HasInMonstersZone(new List<int> { 16474916, 43863925, 79858629, 5352328, 37343995 }))
			{
				flag5 = true;
			}
			if (CheckAtAdvantage() && !enemyMoveGrave)
			{
				flag5 = false;
				list3 = list2;
			}
			foreach (int item2 in list3)
			{
				bool flag6 = false;
				if (flag5)
				{
					int num2 = CheckExosisterMentionCard(item2);
					flag6 = num2 > 0 && base.Bot.HasInMonstersZoneOrInGraveyard(num2) && CheckRemainInDeck(item2) > 0 && !exosisterTransformEffectList.Contains(item2) && !base.Bot.HasInMonstersZone(item2);
				}
				else
				{
					flag6 = !base.Bot.HasInHandOrHasInMonstersZone(item2) && !base.Bot.HasInSpellZone(item2) && CheckRemainInDeck(item2) > 0;
				}
				if (flag6)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					oncePerTurnEffectActivatedList.Add(base.Card.Id);
					base.AI.SelectCard(item2);
					paxCallToField = flag5;
					return true;
				}
			}
		}
		return false;
	}

	public bool ExosisterPaxActivateForEndSearch()
	{
		if (potActivate || base.Bot.LifePoints <= 800)
		{
			return false;
		}
		if (base.Duel.Player == 0 || base.Duel.Phase >= DuelPhase.End)
		{
			foreach (int item in new List<int> { 77891946, 37343995, 197042, 43863925, 5352328, 79858629, 4408198, 16474916 })
			{
				if (!base.Bot.HasInHandOrHasInMonstersZone(item) && !base.Bot.HasInSpellZone(item) && CheckRemainInDeck(item) > 0)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoidImpermanence: true);
					}
					oncePerTurnEffectActivatedList.Add(base.Card.Id);
					base.AI.SelectCard(5352328, 79858629, 37343995, 43863925, 16474916);
					paxCallToField = false;
					return true;
				}
			}
		}
		return false;
	}

	public bool ExosisterArmentActivate()
	{
		if (base.Bot.LifePoints <= 800)
		{
			return false;
		}
		ClientCard clientCard = null;
		if (base.Duel.Player == 0)
		{
			bool flag = false;
			if (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1 && summoned && !CheckLessOperation() && (sakitamaEffect1Activated || !base.Bot.HasInHand(67972302)) && (stellaEffect1Activated || !base.Bot.HasInMonstersZone(43863925)) && (elisEffect1Activated || !base.Bot.HasInHand(16474916)))
			{
				flag = true;
			}
			if (base.Duel.LastChainPlayer == 1)
			{
				foreach (ClientCard lastChainTarget in base.Duel.LastChainTargets)
				{
					if (lastChainTarget.Controller == 0 && lastChainTarget.Location == CardLocation.MonsterZone && lastChainTarget.IsFaceup() && lastChainTarget.HasSetcode(370))
					{
						clientCard = lastChainTarget;
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (clientCard == null && base.Duel.LastChainPlayer == 1)
		{
			foreach (ClientCard lastChainTarget2 in base.Duel.LastChainTargets)
			{
				if (lastChainTarget2.Controller == 0 && lastChainTarget2.Location == CardLocation.MonsterZone && lastChainTarget2.IsFaceup() && lastChainTarget2.HasSetcode(370))
				{
					clientCard = lastChainTarget2;
					break;
				}
			}
		}
		if (clientCard == null)
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.HasSetcode(370) && !card.HasType(CardType.Xyz)
				select card).ToList();
			if (list.Count() > 0)
			{
				list.Sort(CardContainer.CompareCardAttack);
				clientCard = list[0];
			}
		}
		if (clientCard == null)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(42741437) && !mikailisEffect1Activated && (base.Duel.Player == 1 || !mikailisEffect3Activated) && !transformDestList.Contains(42741437) && base.Bot.HasInExtra(42741437))
		{
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(clientCard);
			transformDestList.Add(42741437);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(78135071) && !kaspitellEffect3Activated && base.Duel.Player == 0 && !transformDestList.Contains(78135071) && base.Bot.HasInExtra(78135071))
		{
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(clientCard);
			transformDestList.Add(78135071);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(5530780) && !gibrineEffect1Activated && !transformDestList.Contains(5530780) && base.Bot.HasInExtra(5530780))
		{
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(clientCard);
			transformDestList.Add(5530780);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(41524885) && !asophielEffect1Activated && !transformDestList.Contains(41524885) && base.Bot.HasInExtra(41524885))
		{
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(clientCard);
			transformDestList.Add(41524885);
			return true;
		}
		if (!base.Bot.HasInMonstersZone(78135071) && !kaspitellEffect1Activated && !transformDestList.Contains(78135071) && base.Bot.HasInExtra(78135071))
		{
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoidImpermanence: true);
			}
			base.AI.SelectCard(clientCard);
			transformDestList.Add(78135071);
			return true;
		}
		return false;
	}

	public bool ExosisterVadisActivate()
	{
		if (base.Bot.LifePoints <= 800)
		{
			return false;
		}
		List<int> list = new List<int> { 5352328, 79858629, 43863925, 37343995, 16474916 };
		bool flag = false;
		bool flag2 = false;
		if (base.Duel.Player == 0 && base.Duel.Phase > DuelPhase.Draw && !CheckLessOperation())
		{
			flag = true;
		}
		CheckEnemyMoveGrave();
		if (enemyMoveGrave)
		{
			flag = true;
			flag2 = true;
		}
		if (!oncePerTurnEffectActivatedList.Contains(197042) && base.Bot.HasInSpellZone(197042) && base.Bot.GetMonsters().Count() == 0)
		{
			flag = true;
		}
		if (CheckInDanger() || (DefaultOnBecomeTarget() && !base.Util.ChainContainsCard(15693423)))
		{
			flag = true;
		}
		if (flag)
		{
			foreach (int item in list)
			{
				int num = CheckExosisterMentionCard(item);
				if (num <= 0 || CheckRemainInDeck(item) <= 0 || CheckRemainInDeck(num) <= 0)
				{
					continue;
				}
				if (flag2)
				{
					int num2 = 0;
					foreach (int item2 in new List<int> { item, num })
					{
						_ = item2;
						if (!base.Bot.HasInMonstersZone(item) && !exosisterTransformEffectList.Contains(item))
						{
							num2++;
						}
					}
					if (num2 == 0)
					{
						continue;
					}
				}
				oncePerTurnEffectActivatedList.Add(base.Card.Id);
				Logger.DebugWriteLine("Exosiseter Vadis decide: " + item);
				base.AI.SelectCard(item);
				base.AI.SelectNextCard(num);
				return true;
			}
		}
		return false;
	}

	public bool ExosisterReturniaActivate()
	{
		if (base.Bot.LifePoints <= 800)
		{
			return false;
		}
		ClientCard problematicEnemyCard = GetProblematicEnemyCard(canBeTarget: true);
		if (problematicEnemyCard != null && base.Duel.LastChainPlayer != 0)
		{
			Logger.DebugWriteLine("===Exosister: returnia target 1: " + problematicEnemyCard?.Name);
			removeChosenList.Add(problematicEnemyCard);
			oncePerTurnEffectActivatedList.Add(base.Card.Id);
			base.AI.SelectCard(problematicEnemyCard);
			return true;
		}
		if (base.Duel.LastChainPlayer == 1)
		{
			List<ClientCard> list = base.Duel.LastChainTargets.Where((ClientCard card) => card.Controller == 1 && (card.Location == CardLocation.Grave || card.Location == CardLocation.MonsterZone || card.Location == CardLocation.SpellZone || card.Location == CardLocation.FieldZone)).ToList();
			if (list.Count() > 0)
			{
				oncePerTurnEffectActivatedList.Add(base.Card.Id);
				List<ClientCard> list2 = ShuffleCardList(list);
				Logger.DebugWriteLine("===Exosister: returnia target 2: " + list2[0]?.Name);
				base.AI.SelectCard(list2);
				return true;
			}
		}
		problematicEnemyCard = GetBestEnemyCard(onlyFaceup: false, canBeTarget: true, checkGrave: true);
		bool flag = DefaultOnBecomeTarget() && problematicEnemyCard != null && (problematicEnemyCard.Location != CardLocation.Onfield || problematicEnemyCard.Id != 15693423);
		bool underAttack = base.Bot.UnderAttack;
		bool flag2 = base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End && base.Duel.LastChainPlayer != 0 && problematicEnemyCard != null && problematicEnemyCard.Location != CardLocation.Grave;
		bool flag3 = base.Duel.Player == 1 && base.Enemy.GetMonsterCount() >= 2 && base.Duel.LastChainPlayer != 0;
		Logger.DebugWriteLine("===Exosister: returnia check flag: " + flag + " " + underAttack + " " + flag2 + " " + flag3);
		if (flag || underAttack || flag2 || flag3)
		{
			oncePerTurnEffectActivatedList.Add(base.Card.Id);
			Logger.DebugWriteLine("===Exosister: returnia target 3: " + problematicEnemyCard?.Name);
			base.AI.SelectCard(problematicEnemyCard);
			return true;
		}
		return false;
	}

	public bool ExosisterAvoidMaxxCSummonCheck()
	{
		if (!base.Bot.HasInHand(37343995) || !base.Bot.HasInHand(16474916) || elisEffect1Activated || marthaEffect1Activated)
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (base.Card.Id != 16474916 && base.Card.Id != 37343995)
		{
			summoned = true;
			return true;
		}
		if (base.Card.IsCode(16474916))
		{
			if (base.Bot.Hand.Count((ClientCard card) => card?.Data != null && !card.IsCode(16474916) && !card.IsCode(37343995) && card.IsMonster() && card.HasSetcode(370)) > 0)
			{
				return false;
			}
			if (base.Bot.Hand.Count((ClientCard card) => card?.Data != null && card.IsCode(16474916)) > 1)
			{
				summoned = true;
				return true;
			}
		}
		return false;
	}

	public bool ExosisterStellaSummonCheck()
	{
		if (stellaEffect1Activated || base.Bot.HasInMonstersZone(43863925, notDisabled: true) || CheckWhetherNegated() || CheckLessOperation())
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (base.Bot.Hand.Count((ClientCard card) => card != base.Card && card?.Data != null && card.IsMonster() && card.HasSetcode(370)) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ExosisterIreneSummonCheck()
	{
		if (irenaEffect1Activated || CheckLessOperation() || CheckWhetherNegated() || CheckCalledbytheGrave(16474916) > 0 || CheckCalledbytheGrave(79858629) > 0)
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (CheckRemainInDeck(16474916) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ExosisterForElisSummonCheck()
	{
		if (elisEffect1Activated || CheckCalledbytheGrave(16474916) > 0 || CheckLessOperation())
		{
			return false;
		}
		if (base.Card?.Data == null)
		{
			return false;
		}
		if (!base.Card.HasSetcode(370) || (base.Card.IsCode(37343995) && CheckRemainInDeck(16474916) > 0))
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (base.Bot.Hand.Count((ClientCard card) => card != base.Card && card?.Data != null && card.IsCode(16474916)) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool AratamaSummonCheck()
	{
		if (sakitamaEffect1Activated || CheckCalledbytheGrave(16889337) > 0 || CheckCalledbytheGrave(67972302) > 0)
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (CheckRemainInDeck(67972302) > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ForSakitamaSummonCheck()
	{
		if (sakitamaEffect1Activated || CheckCalledbytheGrave(67972302) > 0 || CheckLessOperation())
		{
			return false;
		}
		if (base.Bot.Hand.Count((ClientCard card) => card?.Data != null && base.Card != card && card.IsCode(67972302)) == 0)
		{
			return false;
		}
		if (enemyActivateLockBird && CheckAtAdvantage())
		{
			return false;
		}
		if (base.Card?.Data != null && !base.Card.IsCode(37343995) && base.Card.Level == 4)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool Level4SummonCheck()
	{
		if (base.Card.Id == 37343995)
		{
			return false;
		}
		if (base.Bot.GetMonsters().Count((ClientCard card) => CheckAbleForXyz(card)) == 1)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ForDonnerSummonCheck()
	{
		if (!base.Bot.HasInExtra(8728498) || (!base.Bot.HasInHand(37343995) && !base.Bot.HasInHandOrInSpellZone(197042)))
		{
			return false;
		}
		if (CheckLessOperation())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.HasType(CardType.Xyz) && card.Level != 4 && (card.Data == null || !card.HasSetcode(370))
			select card).ToList();
		if (list.Count() == 0)
		{
			return false;
		}
		if (list.Count() == 1)
		{
			List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType(CardType.Xyz)
				select card).ToList();
			list2.Sort(CardContainer.CompareCardAttack);
			list.AddRange(list2);
		}
		if (list.Count() == 1)
		{
			List<ClientCard> list3 = base.Bot.Hand.Where((ClientCard card) => card?.Data != null && card.IsMonster()).ToList();
			if (list3.Count() > 0)
			{
				list3.Sort(CardContainer.CompareCardAttack);
				if (base.Card != list3[0])
				{
					return false;
				}
			}
			Logger.DebugWriteLine("===Exosister: summon for donner");
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ExosisterForArmentSummonCheck()
	{
		if (!base.Bot.HasInHandOrInSpellZone(4408198))
		{
			return false;
		}
		if (base.Card?.Data == null)
		{
			return false;
		}
		if (!base.Card.HasSetcode(370))
		{
			return false;
		}
		if (!base.Bot.GetMonsters().Any((ClientCard card) => card?.Data != null && card.IsFaceup() && card.HasSetcode(370)))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool ExosisterMikailisSpSummonCheck()
	{
		return ExosisterMikailisSpSummonCheckInner();
	}

	public bool ExosisterMikailisAdvancedSpSummonCheck()
	{
		if (!CheckLessOperation() || enemyActivateLockBird)
		{
			return false;
		}
		return ExosisterMikailisSpSummonCheckInner(shouldCheckLessOperation: false);
	}

	public bool ExosisterMikailisSpSummonCheckInner(bool shouldCheckLessOperation = true)
	{
		if (base.Bot.HasInMonstersZone(42741437) || mikailisEffect3Activated || (CheckLessOperation() && shouldCheckLessOperation))
		{
			return false;
		}
		if (!enemyActivateLockBird)
		{
			foreach (int exosisterSpellTrap in ExosisterSpellTrapList)
			{
				if (!base.Bot.HasInHandOrInSpellZone(exosisterSpellTrap))
				{
					SelectXyzMaterial();
					return true;
				}
			}
		}
		if (!mikailisEffect1Activated && !base.Bot.HasInMonstersZone(42741437) && GetProblematicEnemyCard(canBeTarget: true) != null)
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where CheckAbleForXyz(card) && card.HasSetcode(370)
				select card).ToList();
			if (list != null && list.Count() > 0)
			{
				SelectXyzMaterial(2, needExosister: true);
				return true;
			}
		}
		return false;
	}

	public bool ExosisterKaspitellSpSummonCheck()
	{
		return ExosisterKaspitellSpSummonCheckInner();
	}

	public bool ExosisterKaspitellAdvancedSpSummonCheck()
	{
		if (!CheckLessOperation() || enemyActivateLockBird)
		{
			return false;
		}
		return ExosisterKaspitellSpSummonCheckInner(shouldCheckLessOperation: false);
	}

	public bool ExosisterKaspitellSpSummonCheckInner(bool shouldCheckLessOperation = true)
	{
		if (base.Bot.HasInMonstersZone(78135071) || kaspitellEffect3Activated || (shouldCheckLessOperation && CheckLessOperation()))
		{
			return false;
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = false;
		if (marthaEffect1Activated || CheckCalledbytheGrave(37343995) > 0 || CheckRemainInDeck(37343995) == 0 || CheckRemainInDeck(16474916) == 0)
		{
			flag = false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard card) => card.HasType(CardType.Link) || card.HasType(CardType.Token)))
		{
			flag = false;
		}
		if (stellaEffect1Activated || CheckCalledbytheGrave(43863925) > 0 || CheckRemainInDeck(43863925) == 0 || !base.Bot.Hand.Any((ClientCard card) => card?.Data != null && card.IsMonster() && card.HasSetcode(370)))
		{
			flag2 = false;
		}
		if (base.Bot.GetMonsters().Count((ClientCard card) => card?.Data != null && card.HasType(CardType.Xyz) && card.HasType(CardType.Xyz) && !card.IsCode(59242457)) == 1)
		{
			flag3 = true;
		}
		if (enemyActivateLockBird)
		{
			flag = false;
			flag2 = false;
		}
		if (!flag && !flag2 && !flag3)
		{
			return false;
		}
		List<ClientCard> source = (from card in base.Bot.GetMonsters()
			where !card.HasType(CardType.Xyz) && !card.HasType(CardType.Token) && !card.HasType(CardType.Link)
			select card).ToList();
		if (source.Count() == 2 && source.All((ClientCard card) => card.Level == 4))
		{
			SelectXyzMaterial();
			return true;
		}
		return false;
	}

	public bool ExosistersMagnificaSpSummonCheck()
	{
		if (CheckLessOperation())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && card.HasType(CardType.Xyz) && card.Rank == 4 && card.HasSetcode(370)
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		base.AI.SelectMaterials(list);
		return true;
	}

	public bool CheckCaduceusInner(ClientCard card)
	{
		if (card?.Data == null)
		{
			return false;
		}
		foreach (int item in SetcodeForDiamond)
		{
			if (card.HasSetcode(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool TellarknightConstellarCaduceusSpSummonCheck()
	{
		if (base.Duel.Turn == 1 || !base.Bot.HasInExtra(9272381))
		{
			return false;
		}
		if (base.Enemy.Graveyard.Any((ClientCard card) => CheckCaduceusInner(card)))
		{
			SelectXyzMaterial();
			return true;
		}
		return false;
	}

	public bool DonnerDaggerFurHireSpSummonCheck()
	{
		if (!base.Bot.HasInHand(37343995) && !base.Bot.HasInHandOrInSpellZone(197042))
		{
			return false;
		}
		if (CheckLessOperation())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card.IsFaceup() && !card.HasType(CardType.Xyz) && card.Level != 4 && (card.Data == null || !card.HasSetcode(370))
			select card).ToList();
		if (list.Count() == 1)
		{
			List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType(CardType.Xyz)
				select card).ToList();
			list2.Sort(CardContainer.CompareCardAttack);
			list.AddRange(list2);
		}
		if (list.Count() > 1)
		{
			base.AI.SelectMaterials(list);
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
		if (new List<int> { 77913594, 4408198, 77891946, 197042 }.Contains(base.Card.Id) && base.Bot.HasInSpellZone(base.Card.Id))
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
