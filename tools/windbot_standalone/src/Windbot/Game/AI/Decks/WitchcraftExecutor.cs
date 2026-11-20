using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Witchcraft", "AI_Witchcraft", "Normal")]
internal class WitchcraftExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int PSYDriver = 49036338;

		public const int GolemAruru = 71074418;

		public const int MadameVerre = 21522601;

		public const int Haine = 84523092;

		public const int Schmietta = 21744288;

		public const int Pittore = 95245544;

		public const int PSYGamma = 38814750;

		public const int Potterie = 59851535;

		public const int Genni = 64756282;

		public const int Collaboration = 10805153;

		public const int ThatGrassLooksGreener = 11110587;

		public const int PotofExtravagance = 49238328;

		public const int DarkRulerNoMore = 54693926;

		public const int Creation = 57916305;

		public const int Reasoning = 58577036;

		public const int MetalfoesFusion = 73594093;

		public const int Holiday = 83301414;

		public const int Draping = 56894757;

		public const int Unveiling = 70226289;

		public const int MagiciansLeftHand = 13758665;

		public const int Scroll = 19673561;

		public const int MagiciansRestage = 40252269;

		public const int WitchcrafterBystreet = 83289866;

		public const int MagicianRightHand = 87769556;

		public const int Masterpiece = 55072170;

		public const int Patronus = 94553671;

		public const int BorreloadSavageDragon = 27548199;

		public const int DracoBerserkeroftheTenyi = 5041348;

		public const int PSYOmega = 74586817;

		public const int TGWonderMagician = 98558751;

		public const int BorrelswordDragon = 85289965;

		public const int KnightmareUnicorn = 38342335;

		public const int KnightmarePhoenix = 2857636;

		public const int PSYLambda = 8802510;

		public const int CrystronHalqifibrax = 50588353;

		public const int SalamangreatAlmiraj = 60303245;

		public const int RelinquishedAnima = 94259633;

		public const int NaturalExterio = 99916754;

		public const int SwordsmanLV7 = 37267041;

		public const int Anti_Spell = 58921041;

		public const int Numbe41BagooskatheTerriblyTiredTapir = 90590303;

		public const int PerformapalFive_RainbowMagician = 19619755;

		public const int DimensionShifter = 91800273;

		public const int MacroCosmos = 30241314;

		public const int DimensionalFissure = 81674782;

		public const int BanisheroftheRadiance = 94853057;

		public const int BanisheroftheLight = 61528025;
	}

	private int Witchcraft_setcode = 296;

	private int TimeLord_setcode = 74;

	private int[] important_witchcraft = new int[2] { 84523092, 21522601 };

	private Dictionary<int, int> witchcraft_level = new Dictionary<int, int>
	{
		{ 71074418, 8 },
		{ 21522601, 7 },
		{ 84523092, 7 },
		{ 21744288, 4 },
		{ 95245544, 3 },
		{ 59851535, 2 },
		{ 64756282, 1 }
	};

	private List<int> Impermanence_list = new List<int>();

	private List<int> FirstCheckSS = new List<int>();

	private List<int> UseSSEffect = new List<int>();

	private List<int> ActivatedCards = new List<int>();

	private List<int> currentNegatingIdList = new List<int>();

	private bool MadameVerreGainedATK;

	private bool summoned;

	private bool enemy_activate_MaxxC;

	private bool enemy_activate_DimensionShifter;

	private bool MagiciansLeftHand_used;

	private bool MagicianRightHand_used;

	public WitchcraftExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 49238328, PotofExtravaganceActivate);
		AddExecutor(ExecutorType.SpellSet, SpellSetForFiveRainbow);
		AddExecutor(ExecutorType.Activate, 54693926, DarkRulerNoMoreActivate);
		AddExecutor(ExecutorType.Activate, 14532163, LightningStormActivate);
		AddExecutor(ExecutorType.Activate, 94259633);
		AddExecutor(ExecutorType.Activate, 21744288, DeckSSWitchcraft);
		AddExecutor(ExecutorType.Activate, 95245544, DeckSSWitchcraft);
		AddExecutor(ExecutorType.Activate, 59851535, DeckSSWitchcraft);
		AddExecutor(ExecutorType.Activate, 64756282, DeckSSWitchcraft);
		AddExecutor(ExecutorType.Activate, 38814750, PSYGammaActivate);
		AddExecutor(ExecutorType.Activate, 23434538, MaxxCActivate);
		AddExecutor(ExecutorType.Activate, 71074418, GolemAruruActivate);
		AddExecutor(ExecutorType.Activate, 27548199, BorreloadSavageDragonActivate);
		AddExecutor(ExecutorType.Activate, 10045474, InfiniteImpermanenceActivate);
		AddExecutor(ExecutorType.Activate, 14558127, AshBlossom_JoyousSpringActivate);
		AddExecutor(ExecutorType.Activate, 24224830, CalledbytheGraveActivate);
		AddExecutor(ExecutorType.Activate, 65681983, CrossoutDesignatorActivate);
		AddExecutor(ExecutorType.Activate, 87769556, SpellsActivate);
		AddExecutor(ExecutorType.Activate, 13758665, SpellsActivate);
		AddExecutor(ExecutorType.Activate, 70226289, UnveilingActivate);
		AddExecutor(ExecutorType.Activate, 56894757, DrapingActivate);
		AddExecutor(ExecutorType.Activate, 74586817, PSYOmegaActivate);
		AddExecutor(ExecutorType.Activate, 5041348, DracoBerserkeroftheTenyiActivate);
		AddExecutor(ExecutorType.Activate, 21522601, MadameVerreActivate);
		AddExecutor(ExecutorType.Activate, 84523092, HaineActivate);
		AddExecutor(ExecutorType.Activate, 60303245, SalamangreatAlmirajActivate);
		AddExecutor(ExecutorType.Activate, 8802510);
		AddExecutor(ExecutorType.SpSummon, 8802510, PSYLambdaSummon);
		AddExecutor(ExecutorType.SpSummon, 74586817, Lv8Summon);
		AddExecutor(ExecutorType.SpSummon, 27548199, BorreloadSavageDragonSummon);
		AddExecutor(ExecutorType.SpSummon, 5041348, Lv8Summon);
		AddExecutor(ExecutorType.SpSummon, 27548199, Lv8Summon);
		AddExecutor(ExecutorType.Activate, 83289866, WitchcraftRecycle);
		AddExecutor(ExecutorType.Activate, WitchcraftRecycle);
		AddExecutor(ExecutorType.Activate, 73594093);
		AddExecutor(ExecutorType.Activate, 98558751, TGWonderMagicianActivate);
		AddExecutor(ExecutorType.Activate, 38342335, KnightmareUnicornActivate);
		AddExecutor(ExecutorType.Activate, 2857636, KnightmarePhoenixActivate);
		AddExecutor(ExecutorType.Activate, 50588353, CrystronHalqifibraxActivate);
		AddExecutor(ExecutorType.Activate, 11110587, SpellsActivatewithCounter);
		AddExecutor(ExecutorType.Activate, 58577036, SpellsActivatewithCounter);
		AddExecutor(ExecutorType.Activate, 55072170, MasterpieceActivate);
		AddExecutor(ExecutorType.Activate, 94553671, PatronusActivate);
		AddExecutor(ExecutorType.Activate, 40252269, MagiciansRestageActivate);
		AddExecutor(ExecutorType.Activate, 83301414, HolidayActivate);
		AddExecutor(ExecutorType.Summon, 21744288, WitchcraftSummon);
		AddExecutor(ExecutorType.Summon, 95245544, WitchcraftSummon);
		AddExecutor(ExecutorType.Summon, 59851535, WitchcraftSummon);
		AddExecutor(ExecutorType.Summon, 64756282, WitchcraftSummon);
		AddExecutor(ExecutorType.Activate, 57916305, CreationActivate);
		AddExecutor(ExecutorType.Activate, 95245544, PittoreActivate);
		AddExecutor(ExecutorType.Activate, 21744288, SchmiettaActivate);
		AddExecutor(ExecutorType.Activate, 64756282, GenniActivate);
		AddExecutor(ExecutorType.Activate, 59851535, PotterieActivate);
		AddExecutor(ExecutorType.SpSummon, 2857636, KnightmarePhoenixSummon);
		AddExecutor(ExecutorType.SpSummon, 94259633, RelinquishedAnimaSummon);
		AddExecutor(ExecutorType.SpSummon, 50588353, CrystronHalqifibraxSummon);
		AddExecutor(ExecutorType.SpSummon, 85289965, BorrelswordDragonSummon);
		AddExecutor(ExecutorType.SpSummon, 38342335, KnightmareUnicornSummon);
		AddExecutor(ExecutorType.SpSummon, 60303245, SalamangreatAlmirajSummon);
		AddExecutor(ExecutorType.Summon, SummonForLink);
		AddExecutor(ExecutorType.Activate, 11110587, SpellsActivateNoCost);
		AddExecutor(ExecutorType.Activate, 58577036, SpellsActivateNoCost);
		AddExecutor(ExecutorType.Activate, 87769556, SpellsActivateNoCost);
		AddExecutor(ExecutorType.Activate, 13758665, SpellsActivateNoCost);
		AddExecutor(ExecutorType.Summon, WitchcraftSummonForRecycle);
		AddExecutor(ExecutorType.Repos, MonsterRepos);
		AddExecutor(ExecutorType.Activate, 83289866, WitchcrafterBystreetActivate);
		AddExecutor(ExecutorType.Activate, 19673561, ScrollActivate);
		AddExecutor(ExecutorType.SpellSet, SpellSet);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override void OnChainEnd()
	{
		currentNegatingIdList.Clear();
		base.OnChainEnd();
	}

	public override void OnChainSolved(int chainIndex)
	{
		ChainInfo currentSolvingChainInfo = base.Duel.GetCurrentSolvingChainInfo();
		if (currentSolvingChainInfo == null || currentSolvingChainInfo.ActivatePlayer != 1)
		{
			return;
		}
		if (base.Duel.IsCurrentSolvingChainNegated())
		{
			if (!MagicianRightHand_used && currentSolvingChainInfo.IsSpell() && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.IsFaceup()) != null && base.Bot.HasInSpellZone(87769556, notDisabled: true))
			{
				Logger.DebugWriteLine("MagicianRightHand negate: " + currentSolvingChainInfo.RelatedCard.Name);
				MagicianRightHand_used = true;
			}
			if (!MagiciansLeftHand_used && currentSolvingChainInfo.IsTrap() && currentSolvingChainInfo.ActivatePlayer == 1 && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.IsFaceup()) != null && base.Bot.HasInSpellZone(13758665, notDisabled: true))
			{
				Logger.DebugWriteLine("MagiciansLeftHand negate: " + currentSolvingChainInfo.RelatedCard.Name);
				MagiciansLeftHand_used = true;
			}
		}
		if (base.Duel.IsCurrentSolvingChainNegated())
		{
			return;
		}
		if (currentSolvingChainInfo.IsCode(23434538))
		{
			enemy_activate_MaxxC = true;
		}
		if (currentSolvingChainInfo.IsCode(91800273))
		{
			enemy_activate_DimensionShifter = true;
		}
		if (!currentSolvingChainInfo.IsCode(10045474))
		{
			return;
		}
		for (int num = 0; num < 5; num++)
		{
			if (base.Enemy.SpellZone[num] == currentSolvingChainInfo.RelatedCard)
			{
				Impermanence_list.Add(4 - num);
				break;
			}
		}
	}

	public override void OnNewTurn()
	{
		MadameVerreGainedATK = false;
		summoned = false;
		enemy_activate_MaxxC = false;
		enemy_activate_DimensionShifter = false;
		MagiciansLeftHand_used = false;
		MagicianRightHand_used = false;
		Impermanence_list.Clear();
		FirstCheckSS.Clear();
		UseSSEffect.Clear();
		ActivatedCards.Clear();
		currentNegatingIdList.Clear();
		base.OnNewTurn();
	}

	public override bool OnPreBattleBetween(ClientCard attacker, ClientCard defender)
	{
		if (!defender.IsMonsterHasPreventActivationEffectInBattle() && !MadameVerreGainedATK && base.Bot.HasInMonstersZone(21522601, notDisabled: true, hasXyzMaterial: false, faceUp: true) && attacker.HasSetcode(Witchcraft_setcode))
		{
			attacker.RealPower += CheckPlusAttackforMadameVerre();
		}
		return base.OnPreBattleBetween(attacker, defender);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		if (hint == 506)
		{
			bool flag = true;
			foreach (ClientCard card in cards)
			{
				if (!card.HasSetcode(Witchcraft_setcode) || card.Location != CardLocation.Removed || !card.IsSpell())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				Logger.DebugWriteLine("** Patronus recycle.");
				IList<ClientCard> list = new List<ClientCard>();
				for (int i = 1; i <= max; i++)
				{
					list.Add(cards[cards.Count - i]);
					Logger.DebugWriteLine("** Select " + cards[cards.Count - i].Name);
				}
				return list;
			}
		}
		if (hint == 509 && enemy_activate_MaxxC)
		{
			bool flag2 = true;
			List<int> list2 = new List<int>();
			List<int> list3 = new List<int> { 84523092, 21522601, 71074418 };
			List<ClientCard> list4 = new List<ClientCard> { null, null, null };
			foreach (ClientCard card2 in cards)
			{
				if (card2 != null && card2.Location == CardLocation.Deck && card2.Controller == 0 && card2.HasSetcode(Witchcraft_setcode))
				{
					for (int j = 0; j < 3; j++)
					{
						if (card2.Id == list3[j])
						{
							list4[j] = card2;
						}
					}
					if (!list2.Contains(card2.Level))
					{
						list2.Add(card2.Level);
					}
					continue;
				}
				flag2 = false;
				break;
			}
			if (flag2 && list2.Count > 1)
			{
				Logger.DebugWriteLine("SS with MaxxC.");
				IList<ClientCard> list5 = new List<ClientCard>();
				int num = CheckPlusAttackforMadameVerre(ignore_activated: true, check_recycle: true, force: true);
				int bestAttack = base.Util.GetBestAttack(base.Bot);
				if (CheckProblematicCards() != null && !base.Util.IsAllEnemyBetterThanValue(bestAttack + num, onlyATK: true) && !base.Bot.HasInMonstersZone(21522601) && list4[1] != null)
				{
					list5.Add(list4[1]);
					return list5;
				}
				for (int k = 0; k < 3; k++)
				{
					if (list4[k] != null)
					{
						list5.Add(list4[k]);
						return list5;
					}
				}
			}
		}
		if (hint == 526)
		{
			Logger.DebugWriteLine("** min-max: " + min + " / " + max);
			foreach (ClientCard card3 in cards)
			{
				Logger.DebugWriteLine(card3.Name ?? "???");
			}
			IList<ClientCard> list6 = new List<ClientCard>();
			for (int l = 1; l <= max; l++)
			{
				list6.Add(cards[cards.Count - l]);
				Logger.DebugWriteLine("** Select " + cards[cards.Count - l].Name);
			}
			return list6;
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (namedCard == null)
		{
			return base.OnSelectPosition(cardId, positions);
		}
		if ((!base.Enemy.HasInMonstersZone(55410871) && base.Duel.Player == 1 && (cardId == 21522601 || base.Util.GetOneEnemyBetterThanValue(namedCard.Attack + 1) != null)) || cardId == 23434538 || cardId == 14558127)
		{
			return CardPosition.FaceUpDefence;
		}
		if (cardId == 21522601 && base.Util.IsTurn1OrMain2())
		{
			return CardPosition.FaceUpDefence;
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public List<ClientCard> CardListShuffle(List<ClientCard> list)
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

	public List<ClientCard> CheckDangerousCardinEnemyGrave(bool onlyMonster = false)
	{
		return base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => (!onlyMonster || card.IsMonster()) && card.HasSetcode(283)).ToList();
	}

	public int CheckDiscardableSpellCount(ClientCard except = null)
	{
		int num = 0;
		int matchingCardsCount = base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.IsSpell() && card.HasSetcode(Witchcraft_setcode) && card != except);
		int num2 = CheckRemainInDeck(13758665, 87769556);
		int matchingCardsCount2 = base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.Id == 40252269 && card != except);
		int cardCount = base.Bot.Hand.GetCardCount(73594093);
		int matchingCardsCount3 = base.Bot.SpellZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup() && card.Id == 83289866 && !card.IsDisabled());
		if (matchingCardsCount2 > 0)
		{
			num += ((matchingCardsCount2 > num2) ? num2 : matchingCardsCount2);
		}
		if (!ActivatedCards.Contains(83289866) && (matchingCardsCount3 >= 2 || (matchingCardsCount3 >= 1 && base.Duel.Phase > DuelPhase.Battle)))
		{
			num++;
		}
		return num + (matchingCardsCount + cardCount);
	}

	public bool CheckLastChainNegated()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard == null || lastChainCard.Controller != 1)
		{
			return false;
		}
		if (lastChainCard.IsMonster() && lastChainCard.HasSetcode(TimeLord_setcode) && base.Duel.Phase == DuelPhase.Standby)
		{
			return false;
		}
		if (DefaultCheckWhetherCardIdIsNegated(lastChainCard.GetOriginCode()))
		{
			return false;
		}
		if (!MagicianRightHand_used && lastChainCard.IsSpell() && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.IsFaceup()) != null && base.Bot.HasInSpellZone(87769556, notDisabled: true))
		{
			return true;
		}
		if (!MagiciansLeftHand_used && lastChainCard.IsTrap() && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard c) => c.HasRace(CardRace.SpellCaster) && c.IsFaceup()) != null && base.Bot.HasInSpellZone(13758665, notDisabled: true))
		{
			return true;
		}
		return false;
	}

	public bool CheckLinkMaterialsMatch(int LinkCount, int MaterialCount, List<ClientCard> list, bool need_tune = false)
	{
		if (list.Count < MaterialCount)
		{
			return false;
		}
		int num = 0;
		foreach (ClientCard item in list)
		{
			num += ((!item.HasType(CardType.Link)) ? 1 : item.LinkCount);
		}
		if (num != LinkCount)
		{
			foreach (ClientCard item2 in list)
			{
				_ = item2;
				num++;
			}
			if (num != LinkCount)
			{
				return false;
			}
		}
		if (need_tune && list.GetFirstMatchingCard((ClientCard card) => card.IsTuner()) == null)
		{
			return false;
		}
		return true;
	}

	public List<ClientCard> CheckLinkMaterials(int LinkCount, int MaterialCount, bool need_tuner = false, List<ClientCard> extra = null)
	{
		List<int> psy_cardids = new List<int> { 38814750, 49036338 };
		List<ClientCard> list = base.Bot.MonsterZone.GetMatchingCards((ClientCard card) => card.IsFaceup() && psy_cardids.Contains(card.Id)).ToList();
		if (CheckLinkMaterialsMatch(LinkCount, MaterialCount, list, need_tuner))
		{
			return list;
		}
		List<ClientCard> list2 = base.Enemy.MonsterZone.GetMatchingCards((ClientCard c) => c.IsFaceup()).ToList();
		if (extra != null)
		{
			list2 = list2.Union(extra).ToList();
		}
		list2.Sort(CardContainer.CompareCardAttack);
		int num = CheckDiscardableSpellCount();
		int bestAttack = base.Util.GetBestAttack(base.Enemy);
		foreach (ClientCard item in list2)
		{
			if ((!item.HasSetcode(Witchcraft_setcode) || (item.Level < 5 && num < 2)) && item.Attack < bestAttack && (!item.HasType(CardType.Link) || item.LinkMarker <= 2))
			{
				list.Add(item);
				if (CheckLinkMaterialsMatch(LinkCount, MaterialCount, list, need_tuner))
				{
					return list;
				}
			}
		}
		if (!CheckLinkMaterialsMatch(LinkCount, MaterialCount, list, need_tuner))
		{
			list.Clear();
		}
		return list;
	}

	public int CheckPlusAttackforMadameVerre(bool ignore_activated = false, bool check_recycle = false, bool force = false)
	{
		if (!force && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.Id == 21522601 && !card.IsDisabled()) == null)
		{
			return 0;
		}
		if (!ignore_activated && MadameVerreGainedATK)
		{
			return 0;
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (ClientCard item in base.Bot.Hand)
		{
			if (item.IsSpell())
			{
				hashSet.Add(item.Id);
			}
		}
		if (check_recycle && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode)) != null)
		{
			foreach (int item2 in new List<int> { 83301414, 57916305, 56894757, 70226289, 10805153 })
			{
				if (base.Bot.HasInGraveyard(item2) && !ActivatedCards.Contains(item2))
				{
					hashSet.Add(base.Card.Id);
				}
			}
		}
		return ((hashSet.Count() >= 6) ? 6 : hashSet.Count()) * 1000;
	}

	public ClientCard CheckProblematicCards(bool canBeTarget = false, bool OnlyDanger = false)
	{
		ClientCard floodgate = base.Enemy.MonsterZone.GetFloodgate(canBeTarget);
		if (floodgate != null)
		{
			return floodgate;
		}
		floodgate = base.Enemy.MonsterZone.GetDangerousMonster(canBeTarget);
		if (floodgate != null && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			return floodgate;
		}
		floodgate = base.Enemy.MonsterZone.GetInvincibleMonster(canBeTarget);
		if (floodgate != null && (base.Duel.Player == 0 || (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2)))
		{
			return floodgate;
		}
		List<ClientCard> list = base.Enemy.MonsterZone.GetMatchingCards((ClientCard c) => c.IsFaceup()).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		foreach (ClientCard item in list)
		{
			if ((item.HasType(CardType.Fusion) || item.HasType(CardType.Ritual) || item.HasType(CardType.Synchro) || item.HasType(CardType.Xyz) || (item.HasType(CardType.Link) && item.LinkCount >= 2)) && (!canBeTarget || (!item.IsShouldNotBeTarget() && !item.IsShouldNotBeMonsterTarget())))
			{
				return item;
			}
		}
		if (OnlyDanger)
		{
			return null;
		}
		int num = base.Util.GetBestPower(base.Bot);
		if (!MadameVerreGainedATK && base.Bot.HasInMonstersZone(21522601, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			num += CheckPlusAttackforMadameVerre();
		}
		return base.Util.GetProblematicEnemyCard(num, canBeTarget);
	}

	public int CheckRecyclableCount(bool tohand = false, bool ignore_monster = false)
	{
		if (!ignore_monster && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode)) == null)
		{
			return 0;
		}
		int num = 0;
		List<int> list = new List<int> { 83301414, 57916305, 56894757, 70226289, 10805153 };
		if (!tohand)
		{
			list.Add(83289866);
			list.Add(19673561);
		}
		foreach (int item in list)
		{
			if (base.Bot.HasInGraveyard(item) && !ActivatedCards.Contains(item))
			{
				num++;
			}
		}
		return num;
	}

	public int CheckRemainInDeck(int id)
	{
		return id switch
		{
			49036338 => base.Bot.GetRemainingCount(49036338, 1), 
			71074418 => base.Bot.GetRemainingCount(71074418, 1), 
			21522601 => base.Bot.GetRemainingCount(21522601, 1), 
			84523092 => base.Bot.GetRemainingCount(84523092, 2), 
			21744288 => base.Bot.GetRemainingCount(21744288, 3), 
			95245544 => base.Bot.GetRemainingCount(95245544, 3), 
			14558127 => base.Bot.GetRemainingCount(14558127, 1), 
			38814750 => base.Bot.GetRemainingCount(38814750, 3), 
			23434538 => base.Bot.GetRemainingCount(23434538, 1), 
			59851535 => base.Bot.GetRemainingCount(59851535, 1), 
			64756282 => base.Bot.GetRemainingCount(64756282, 2), 
			10805153 => base.Bot.GetRemainingCount(10805153, 1), 
			11110587 => base.Bot.GetRemainingCount(11110587, 2), 
			14532163 => base.Bot.GetRemainingCount(14532163, 2), 
			49238328 => base.Bot.GetRemainingCount(49238328, 3), 
			54693926 => base.Bot.GetRemainingCount(54693926, 2), 
			57916305 => base.Bot.GetRemainingCount(57916305, 3), 
			58577036 => base.Bot.GetRemainingCount(58577036, 3), 
			73594093 => base.Bot.GetRemainingCount(73594093, 1), 
			83301414 => base.Bot.GetRemainingCount(83301414, 3), 
			24224830 => base.Bot.GetRemainingCount(24224830, 3), 
			56894757 => base.Bot.GetRemainingCount(56894757, 1), 
			65681983 => base.Bot.GetRemainingCount(65681983, 2), 
			70226289 => base.Bot.GetRemainingCount(70226289, 1), 
			13758665 => base.Bot.GetRemainingCount(13758665, 1), 
			19673561 => base.Bot.GetRemainingCount(19673561, 1), 
			40252269 => base.Bot.GetRemainingCount(40252269, 2), 
			83289866 => base.Bot.GetRemainingCount(83289866, 3), 
			87769556 => base.Bot.GetRemainingCount(87769556, 1), 
			10045474 => base.Bot.GetRemainingCount(10045474, 3), 
			55072170 => base.Bot.GetRemainingCount(55072170, 1), 
			94553671 => base.Bot.GetRemainingCount(94553671, 2), 
			_ => 0, 
		};
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

	public bool CheckWhetherWillbeRemoved()
	{
		if (enemy_activate_DimensionShifter)
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

	public bool SpellNegatable(bool isCounter = false, ClientCard target = null)
	{
		if (target == null)
		{
			target = base.Card;
		}
		if (CheckCalledbytheGrave(target.GetOriginCode()) > 0)
		{
			return true;
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
		return false;
	}

	public bool NegatedCheck(bool disablecheck = true)
	{
		if ((base.Card.IsSpell() || base.Card.IsTrap()) && SpellNegatable())
		{
			return true;
		}
		if (CheckCalledbytheGrave(base.Card.GetOriginCode()) > 0)
		{
			return true;
		}
		if (base.Card.IsMonster() && base.Card.Location == CardLocation.MonsterZone && base.Card.IsDefense() && (base.Enemy.MonsterZone.GetFirstMatchingFaceupCard((ClientCard card) => card.Id == 90590303 && card.IsDefense() && !card.IsDisabled()) != null || base.Bot.MonsterZone.GetFirstMatchingFaceupCard((ClientCard card) => card.Id == 90590303 && card.IsDefense() && !card.IsDisabled()) != null))
		{
			return true;
		}
		if (disablecheck)
		{
			return base.Card.IsDisabled();
		}
		return false;
	}

	public void SelectSTPlace(ClientCard card = null, bool avoid_Impermanence = false, List<int> avoid_list = null)
	{
		if (card == null)
		{
			card = base.Card;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < 5; i++)
		{
			if (base.Bot.SpellZone[i] == null && (!(card != null && card.Location == CardLocation.Hand && avoid_Impermanence) || !Impermanence_list.Contains(i)) && (avoid_list == null || !avoid_list.Contains(i)))
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
		if (avoid_Impermanence && base.Bot.GetMonsters().Any((ClientCard c) => c.IsFaceup() && !c.IsDisabled()))
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

	public bool SpellSet()
	{
		if (base.Duel.Phase == DuelPhase.Main1 && base.Bot.HasAttackingMonster() && base.Duel.Turn > 1)
		{
			return false;
		}
		if (base.Card.Id == 65681983 && base.Duel.Turn >= 5)
		{
			return false;
		}
		if (new int[2] { 55072170, 56894757 }.Contains(base.Card.Id) && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode)) == null)
		{
			return false;
		}
		if (base.Card.Id == 70226289)
		{
			return false;
		}
		if (base.Card.Id == 94553671)
		{
			int num = base.Bot.Banished.GetMatchingCardsCount((ClientCard card) => card.HasSetcode(Witchcraft_setcode));
			if (num == 0)
			{
				num += base.Bot.Graveyard.GetMatchingCardsCount((ClientCard card) => card.HasSetcode(Witchcraft_setcode));
			}
			if (num == 0)
			{
				return false;
			}
		}
		if (base.Card.IsSpell())
		{
			int num2 = CheckRecyclableCount() + base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.IsSpell());
			int num3 = 0;
			if (base.Bot.HasInMonstersZone(84523092))
			{
				num3++;
			}
			if (base.Bot.HasInMonstersZone(21522601))
			{
				num3++;
			}
			if (num3 >= num2)
			{
				return false;
			}
		}
		if (base.Card.IsTrap() || base.Card.HasType(CardType.QuickPlay))
		{
			List<int> list = new List<int>();
			int num4 = 0;
			for (int num5 = 0; num5 < 5; num5++)
			{
				if (base.Enemy.SpellZone[num5] != null && base.Enemy.SpellZone[num5].IsFaceup() && base.Bot.SpellZone[4 - num5] == null)
				{
					list.Add(4 - num5);
					num4 += (int)Math.Pow(2.0, 4 - num5);
				}
			}
			if (base.Bot.HasInHand(10045474))
			{
				if (base.Card.IsCode(10045474))
				{
					base.AI.SelectPlace(num4);
					return true;
				}
				SelectSTPlace(base.Card, avoid_Impermanence: false, list);
				return true;
			}
			SelectSTPlace();
			return true;
		}
		if ((base.Enemy.HasInSpellZone(58921041, notDisabled: true) || base.Bot.HasInSpellZone(58921041, notDisabled: true)) && base.Card.IsSpell() && base.Card.Id != 73594093)
		{
			SelectSTPlace();
			return true;
		}
		return false;
	}

	public bool SpellSetForFiveRainbow()
	{
		bool flag = false;
		List<ClientCard> list = new List<ClientCard>();
		if (base.Duel.IsNewRule || base.Duel.IsNewRule2020)
		{
			list.Add(base.Enemy.SpellZone[0]);
			list.Add(base.Enemy.SpellZone[4]);
		}
		else
		{
			list.Add(base.Enemy.SpellZone[6]);
			list.Add(base.Enemy.SpellZone[7]);
		}
		foreach (ClientCard item in list)
		{
			if (item != null && item.Id == 19619755)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() == 0 || base.Bot.SpellZone.GetFirstMatchingCard((ClientCard card) => card.IsFacedown()) != null)
		{
			return false;
		}
		if (base.Card.IsSpell())
		{
			SelectSTPlace(null, avoid_Impermanence: true);
			return true;
		}
		return false;
	}

	public bool MonsterRepos()
	{
		int num = base.Card.Attack + 1;
		int num2 = CheckPlusAttackforMadameVerre(ignore_activated: true, check_recycle: true);
		Logger.DebugWriteLine("self_attack of " + (base.Card.Name ?? "X") + ": " + num);
		if (base.Card.HasSetcode(Witchcraft_setcode))
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
			if (monster.HasSetcode(Witchcraft_setcode))
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
		if (base.Card.IsDefense() && !flag && num >= base.Card.Defense)
		{
			return true;
		}
		return false;
	}

	public void SelectDiscardSpell()
	{
		int num = CheckRemainInDeck(13758665, 87769556);
		int matchingCardsCount = base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.IsSpell() && card.HasSetcode(Witchcraft_setcode));
		int matchingCardsCount2 = base.Bot.SpellZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup() && card.Id == 83289866);
		if (base.Bot.HasInHand(40252269) && num > 0)
		{
			base.AI.SelectCard(40252269);
		}
		else if (base.Bot.HasInHand(73594093))
		{
			base.AI.SelectCard(73594093);
		}
		else if (!ActivatedCards.Contains(19673561) && base.Bot.SpellZone.GetCardCount(19673561) > 0)
		{
			base.AI.SelectCard(base.Bot.SpellZone.GetFirstMatchingFaceupCard((ClientCard card) => card.Id == 19673561));
			ActivatedCards.Add(19673561);
		}
		else if (!ActivatedCards.Contains(83289866) && matchingCardsCount2 >= 2)
		{
			base.AI.SelectCard(base.Bot.SpellZone.GetFirstMatchingFaceupCard((ClientCard card) => card.Id == 83289866));
			ActivatedCards.Add(83289866);
		}
		else if (matchingCardsCount > 0)
		{
			List<int> list = new List<int> { 19673561, 83289866, 10805153, 70226289, 56894757 };
			if (base.Duel.Player == 1)
			{
				list.Add(57916305);
				list.Add(83301414);
			}
			else
			{
				list.Add(83301414);
				list.Add(57916305);
			}
			foreach (int cardid in list)
			{
				IList<ClientCard> matchingCards = base.Bot.Hand.GetMatchingCards((ClientCard card) => card.Id == cardid);
				if (matchingCards.Count() > 0)
				{
					base.AI.SelectCard(matchingCards);
					return;
				}
			}
			base.AI.SelectCard(19673561, 83289866);
		}
		else if (base.Bot.HasInHand(49238328) && base.Bot.ExtraDeck.Count < 6)
		{
			base.AI.SelectCard(49238328);
		}
		else if (matchingCardsCount2 >= 1)
		{
			base.AI.SelectCard(base.Bot.SpellZone.GetFirstMatchingFaceupCard((ClientCard card) => card.Id == 83289866));
			ActivatedCards.Add(83289866);
		}
		else
		{
			base.AI.SelectCard(11110587, 14532163, 49238328, 13758665, 87769556, 65681983, 24224830);
		}
	}

	public bool SpellsActivate()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if (CheckDiscardableSpellCount() <= 1)
		{
			return false;
		}
		if ((base.Card.Id == 11110587 || base.Card.Id == 58577036) && CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if ((base.Card.Id == 13758665 || base.Card.Id == 87769556) && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasRace(CardRace.SpellCaster)) == null && (summoned || base.Bot.Hand.GetFirstMatchingCard((ClientCard card) => card.HasRace(CardRace.SpellCaster) && card.Level <= 4) == null))
		{
			return false;
		}
		SelectSTPlace(base.Card, avoid_Impermanence: true);
		return true;
	}

	public bool SpellsActivateNoCost()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if ((base.Card.Id == 11110587 || base.Card.Id == 58577036) && CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if ((base.Card.Id == 13758665 || base.Card.Id == 87769556) && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasRace(CardRace.SpellCaster)) == null && (summoned || base.Bot.Hand.GetFirstMatchingCard((ClientCard card) => card.HasRace(CardRace.SpellCaster) && card.Level <= 4) == null))
		{
			return false;
		}
		SelectSTPlace(base.Card, avoid_Impermanence: true);
		return true;
	}

	public bool SpellsActivatewithCounter()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if ((base.Card.Id == 11110587 || base.Card.Id == 58577036) && CheckWhetherWillbeRemoved())
		{
			return false;
		}
		int[] counter_cards = new int[3] { 38814750, 24224830, 65681983 };
		if (base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => counter_cards.Contains(card.Id)) + base.Bot.SpellZone.GetMatchingCardsCount((ClientCard card) => counter_cards.Contains(card.Id)) > 0 || base.Bot.Hand.GetCardCount(base.Card.Id) >= 2)
		{
			SelectSTPlace(base.Card, avoid_Impermanence: true);
			return true;
		}
		return Program.Rand.Next(2) > 0;
	}

	public bool WitchcraftSummon()
	{
		if (UseSSEffect.Contains(base.Card.Id))
		{
			return false;
		}
		int matchingCardsCount = base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.IsSpell());
		int num = CheckRemainInDeck(21522601, 84523092, 71074418);
		if (matchingCardsCount > 0 && num > 0)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool WitchcraftSummonForRecycle()
	{
		if (!base.Card.HasSetcode(Witchcraft_setcode) || base.Card.Level > 4)
		{
			return false;
		}
		if (CheckRecyclableCount(tohand: false, ignore_monster: true) > 0 && base.Bot.MonsterZone.GetFirstMatchingFaceupCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode)) == null)
		{
			summoned = true;
			return true;
		}
		return false;
	}

	public bool SummonForLink()
	{
		if (base.Card.Level >= 5)
		{
			return false;
		}
		summoned = true;
		if (BorrelswordDragonSummonCheck(base.Card).Count >= 3)
		{
			Logger.DebugWriteLine("Summon for BorrelswordDragon.");
			foreach (ClientCard item in BorrelswordDragonSummonCheck(base.Card))
			{
				Logger.DebugWriteLine(item.Name ?? "???");
			}
			return true;
		}
		if (KnightmareUnicornSummonCheck(base.Card).Count >= 2)
		{
			Logger.DebugWriteLine("Summon for KnightmareUnicorn.");
			return true;
		}
		if (KnightmarePhoenixSummonCheck(base.Card).Count >= 2)
		{
			Logger.DebugWriteLine("Summon for KnightmarePhoenix.");
			return true;
		}
		if (RelinquishedAnimaSummonCheck(base.Card) != -1)
		{
			Logger.DebugWriteLine("Summon for RelinquishedAnima.");
			return true;
		}
		if (SalamangreatAlmirajSummonCheck(base.Card))
		{
			Logger.DebugWriteLine("Summon for SalamangreatAlmiraj.");
			return true;
		}
		summoned = false;
		return false;
	}

	public bool DeckSSWitchcraft()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		if (NegatedCheck(disablecheck: false))
		{
			return false;
		}
		if (base.Duel.Player == 0 && !FirstCheckSS.Contains(base.Card.Id))
		{
			FirstCheckSS.Add(base.Card.Id);
			return false;
		}
		int num = CheckDiscardableSpellCount();
		if (num == 0 && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode) && card.Level >= 6) != null)
		{
			return false;
		}
		SelectDiscardSpell();
		bool flag = false;
		int num2 = CheckPlusAttackforMadameVerre(ignore_activated: true, check_recycle: false, force: true);
		int num3 = base.Util.GetBestAttack(base.Bot);
		if (CheckRemainInDeck(84523092) > 0 && num3 < 2400)
		{
			num3 = 2400;
		}
		Logger.DebugWriteLine("less summon check: " + (num3 + num2 - 1000) + " to " + (num3 + num2));
		if (base.Util.GetOneEnemyBetterThanValue(num3) != null && base.Util.GetOneEnemyBetterThanValue(num3 + num2) == null && base.Util.GetOneEnemyBetterThanValue(num3 + num2 - 1000) != null)
		{
			flag = true;
		}
		if (!enemy_activate_MaxxC && !flag && num >= 2 && base.Duel.Player == 0)
		{
			int[] array = new int[4] { 21744288, 95245544, 64756282, 59851535 };
			foreach (int cardid in array)
			{
				if (!UseSSEffect.Contains(cardid) && base.Card.Id != cardid && CheckRemainInDeck(cardid) > 0 && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.Id == cardid && card.IsFaceup()) == null)
				{
					UseSSEffect.Add(base.Card.Id);
					base.AI.SelectNextCard(cardid);
					return true;
				}
			}
		}
		if (((base.Util.GetOneEnemyBetterThanValue(base.Card.Attack) == null) ^ base.Card.IsDefense()) && base.Duel.Player == 1)
		{
			return false;
		}
		if (CheckRemainInDeck(84523092, 21522601, 71074418) == 0)
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(84523092) || (flag && !base.Bot.HasInMonstersZone(21522601, notDisabled: true)))
		{
			base.AI.SelectNextCard(21522601, 84523092, 71074418);
		}
		else
		{
			base.AI.SelectNextCard(84523092, 21522601, 71074418);
		}
		UseSSEffect.Add(base.Card.Id);
		return true;
	}

	public bool WitchcraftRecycle()
	{
		if (base.Card.IsSpell() && base.Card.HasSetcode(Witchcraft_setcode) && base.Card.Location == CardLocation.Grave)
		{
			ActivatedCards.Add(base.Card.Id);
			if (base.Card.HasType(CardType.Continuous))
			{
				SelectSTPlace(base.Card);
			}
			return true;
		}
		return false;
	}

	public bool GolemAruruActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(71074418, 2))
		{
			return true;
		}
		if (NegatedCheck())
		{
			return false;
		}
		ClientCard clientCard = CheckProblematicCards(canBeTarget: true);
		if (clientCard != null)
		{
			base.AI.SelectCard(clientCard);
			return true;
		}
		base.AI.SelectCard(83301414, 57916305, 56894757, 19673561, 83289866, 70226289, 10805153);
		return true;
	}

	public bool MadameVerreActivate()
	{
		if (NegatedCheck())
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(21522601, 1))
		{
			if (base.Card.IsDisabled())
			{
				return false;
			}
			if (CheckLastChainNegated())
			{
				return false;
			}
			if (base.Enemy.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsMonsterShouldBeDisabledBeforeItUseEffect() && !card.IsDisabled()) != null)
			{
				SelectDiscardSpell();
				return true;
			}
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone)
			{
				SelectDiscardSpell();
				return true;
			}
			if (base.Duel.Phase > DuelPhase.Main1 && base.Duel.Phase < DuelPhase.Main2 && base.Enemy.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsMonsterDangerous() || (base.Duel.Player == 0 && card.IsMonsterInvincible())) != null)
			{
				SelectDiscardSpell();
				return true;
			}
			return false;
		}
		ClientCard battlingMonster = base.Bot.BattlingMonster;
		ClientCard battlingMonster2 = base.Enemy.BattlingMonster;
		if (battlingMonster != null && battlingMonster2 != null)
		{
			int num = CheckPlusAttackforMadameVerre();
			int num2 = battlingMonster2.GetDefensePower() - battlingMonster.GetDefensePower();
			Logger.DebugWriteLine("power: " + num);
			Logger.DebugWriteLine("diff: " + num2);
			if (num2 > 0)
			{
				if (battlingMonster.IsDefense() && num < num2)
				{
					return false;
				}
				base.AI.SelectCard(base.Bot.Hand.GetMatchingCards((ClientCard card) => card.IsSpell()));
				MadameVerreGainedATK = true;
				return true;
			}
			if (base.Enemy.GetMonsterCount() == 1 || (battlingMonster2.IsAttack() && base.Enemy.LifePoints <= num2 + num))
			{
				base.AI.SelectCard(base.Bot.Hand.GetMatchingCards((ClientCard card) => card.IsSpell()));
				MadameVerreGainedATK = true;
				return true;
			}
		}
		return false;
	}

	public bool HaineActivate()
	{
		if (NegatedCheck() || base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		ClientCard clientCard = base.Enemy.MonsterZone.GetFloodgate(canBeTarget: true);
		if (clientCard == null)
		{
			Logger.DebugWriteLine("*** Haine 2nd check.");
			clientCard = base.Enemy.SpellZone.FirstOrDefault((ClientCard card) => card?.Data != null && card.IsFloodgate() && card.IsFaceup() && (!card.IsShouldNotBeTarget() || !base.Duel.ChainTargets.Contains(card)));
		}
		if (clientCard == null)
		{
			Logger.DebugWriteLine("*** Haine 3rd check.");
			clientCard = CheckProblematicCards(canBeTarget: true, base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase >= DuelPhase.Main2);
			if (clientCard != null && clientCard.HasSetcode(TimeLord_setcode) && !clientCard.IsDisabled())
			{
				clientCard = null;
			}
		}
		if (clientCard == null && base.Duel.LastChainPlayer == 1)
		{
			Logger.DebugWriteLine("*** Haine 4th check.");
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && !lastChainCard.IsDisabled() && !CheckLastChainNegated() && (lastChainCard.HasType(CardType.Continuous) || lastChainCard.HasType(CardType.Equip) || lastChainCard.HasType(CardType.Field)) && (lastChainCard.Location == CardLocation.SpellZone || lastChainCard.Location == CardLocation.FieldZone))
			{
				clientCard = lastChainCard;
			}
		}
		if (clientCard != null)
		{
			Logger.DebugWriteLine("*** Haine target: " + clientCard.Name);
			SelectDiscardSpell();
			base.AI.SelectNextCard(clientCard);
			return true;
		}
		if (!CheckLastChainNegated())
		{
			ClientCard clientCard2 = null;
			ClientCard clientCard3 = null;
			if (base.Duel.IsNewRule || base.Duel.IsNewRule2020)
			{
				clientCard2 = base.Enemy.SpellZone[0];
				clientCard3 = base.Enemy.SpellZone[4];
			}
			else
			{
				clientCard2 = base.Enemy.SpellZone[6];
				clientCard3 = base.Enemy.SpellZone[7];
			}
			if (clientCard2 != null && clientCard3 != null && clientCard2.LScale != clientCard3.RScale)
			{
				Logger.DebugWriteLine("*** Haine pendulum destroy");
				SelectDiscardSpell();
				base.AI.SelectNextCard((Program.Rand.Next(2) == 1) ? clientCard2 : clientCard3);
				return true;
			}
		}
		if (base.Duel.Player == 0 && base.Duel.Phase == DuelPhase.End)
		{
			Logger.DebugWriteLine("*** Haine self check");
			int num = 0;
			int[] array = new int[6] { 10805153, 70226289, 19673561, 83301414, 57916305, 56894757 };
			foreach (int num3 in array)
			{
				if (!ActivatedCards.Contains(num3) && base.Bot.HasInHand(num3))
				{
					num = num3;
					break;
				}
			}
			if (num == 0)
			{
				return false;
			}
			IList<ClientCard> matchingCards = base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFaceup());
			IList<ClientCard> matchingCards2 = base.Enemy.MonsterZone.GetMatchingCards((ClientCard card) => card.IsFaceup());
			List<ClientCard> list = matchingCards.Union(matchingCards2).ToList();
			if (list.Count == 0)
			{
				return false;
			}
			list = CardListShuffle(list);
			base.AI.SelectCard(num);
			base.AI.SelectNextCard(list);
			return true;
		}
		return false;
	}

	public bool SchmiettaActivate()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck(disablecheck: false) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		bool flag = base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode) && card.Id != 71074418) != null;
		if (flag)
		{
			int[] array = new int[7] { 83289866, 83301414, 57916305, 56894757, 19673561, 70226289, 10805153 };
			foreach (int num2 in array)
			{
				if (CheckRemainInDeck(num2) > 0 && !base.Bot.HasInHandOrInSpellZone(num2) && !base.Bot.HasInGraveyard(num2) && !ActivatedCards.Contains(num2))
				{
					base.AI.SelectCard(num2);
					ActivatedCards.Add(21744288);
					return true;
				}
			}
		}
		bool flag2 = base.Bot.HasInHandOrInSpellZone(83301414) || (flag && base.Bot.HasInGraveyard(83301414) && !ActivatedCards.Contains(83301414));
		if (base.Bot.HasInHand(important_witchcraft) && !base.Bot.HasInGraveyard(95245544) && !ActivatedCards.Contains(95245544) && CheckRemainInDeck(95245544) > 0 && flag2)
		{
			base.AI.SelectCard(95245544);
			ActivatedCards.Add(21744288);
			return true;
		}
		if (base.Bot.HasInHand(83301414) && !ActivatedCards.Contains(83301414) && !base.Bot.HasInGraveyard(important_witchcraft))
		{
			base.AI.SelectCard(important_witchcraft);
			ActivatedCards.Add(21744288);
			return true;
		}
		if (!ActivatedCards.Contains(64756282))
		{
			int num3 = (base.Bot.HasInGraveyard(64756282) ? 1 : 0);
			int num4 = (base.Bot.HasInGraveyard(83301414) ? 1 : 0);
			int num5 = (base.Bot.HasInGraveyard(important_witchcraft) ? 1 : 0);
			if (num3 + num4 + num5 == 2)
			{
				if (num3 == 0)
				{
					base.AI.SelectCard(64756282);
					ActivatedCards.Add(21744288);
					return true;
				}
				if (num4 == 0)
				{
					base.AI.SelectCard(83301414);
					ActivatedCards.Add(21744288);
					return true;
				}
				if (num5 == 0)
				{
					base.AI.SelectCard(important_witchcraft);
					ActivatedCards.Add(21744288);
					return true;
				}
			}
		}
		if (!ActivatedCards.Contains(95245544) && !base.Bot.HasInGraveyard(95245544) && PittoreActivate())
		{
			base.AI.SelectCard(95245544);
			ActivatedCards.Add(21744288);
			return true;
		}
		if (CheckRemainInDeck(55072170) >= 2)
		{
			base.AI.SelectCard(55072170);
			ActivatedCards.Add(21744288);
			return true;
		}
		return false;
	}

	public bool PittoreActivate()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck(disablecheck: false) || CheckWhetherWillbeRemoved())
		{
			return false;
		}
		if (base.Bot.Hand.GetFirstMatchingCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode)) == null)
		{
			return false;
		}
		if (base.Bot.Hand.GetFirstMatchingCard((ClientCard card) => card.Id == 21522601 || card.Id == 84523092) != null)
		{
			base.AI.SelectCard(21522601, 84523092);
			ActivatedCards.Add(95245544);
			return true;
		}
		int[] array = new int[7] { 19673561, 70226289, 10805153, 56894757, 83289866, 83301414, 57916305 };
		int[] array2 = array;
		foreach (int num2 in array2)
		{
			if (base.Bot.HasInHand(num2) && !ActivatedCards.Contains(num2))
			{
				base.AI.SelectCard(num2);
				ActivatedCards.Add(95245544);
				return true;
			}
		}
		if ((base.Bot.HasInHand(21744288) && !ActivatedCards.Contains(21744288)) || base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card.HasSetcode(Witchcraft_setcode) && card.Level <= 4) >= 2)
		{
			_ = new int[4] { 21744288, 95245544, 64756282, 59851535 };
			array2 = array;
			foreach (int cardId in array2)
			{
				if (base.Bot.HasInHand(cardId))
				{
					base.AI.SelectCard(cardId);
					ActivatedCards.Add(95245544);
					return true;
				}
			}
		}
		return false;
	}

	public bool AshBlossom_JoyousSpringActivate()
	{
		if (NegatedCheck() || CheckLastChainNegated())
		{
			return false;
		}
		return DefaultAshBlossomAndJoyousSpring();
	}

	public bool PSYGammaActivate()
	{
		if (NegatedCheck())
		{
			return false;
		}
		return true;
	}

	public bool MaxxCActivate()
	{
		if (NegatedCheck())
		{
			return false;
		}
		return DefaultMaxxC();
	}

	public bool PotterieActivate()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		if (!ActivatedCards.Contains(83301414) && base.Bot.HasInGraveyard(83301414) && base.Bot.HasInGraveyard(important_witchcraft))
		{
			base.AI.SelectCard(83301414);
			ActivatedCards.Add(59851535);
			return true;
		}
		if (CheckProblematicCards() == null)
		{
			int[] array = new int[2] { 94553671, 71074418 };
			foreach (int cardId in array)
			{
				if (base.Bot.HasInGraveyard(cardId))
				{
					base.AI.SelectCard(cardId);
					ActivatedCards.Add(59851535);
					return true;
				}
			}
		}
		return false;
	}

	public bool GenniActivate()
	{
		if (base.Card.Location != CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		int matchingCardsCount = base.Bot.Graveyard.GetMatchingCardsCount((ClientCard card) => card.Id == 83301414);
		int num = HolidayCheck(base.Card);
		if (matchingCardsCount > 0 && num > 0)
		{
			base.AI.SelectCard(83301414);
			base.AI.SelectNextCard(num);
			ActivatedCards.Add(64756282);
			return true;
		}
		if (base.Bot.HasInGraveyard(56894757) && base.Enemy.GetMonsterCount() == 0 && base.Duel.Phase == DuelPhase.Main1)
		{
			int num2 = 0;
			foreach (ClientCard monster in base.Bot.GetMonsters())
			{
				num2 += monster.Attack;
			}
			if (num2 >= base.Enemy.LifePoints)
			{
				int matchingCardsCount2 = base.Bot.MonsterZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode));
				IList<ClientCard> spells = base.Enemy.GetSpells();
				if (matchingCardsCount2 >= spells.Count())
				{
					base.AI.SelectCard(56894757);
					base.AI.SelectNextCard(spells);
					ActivatedCards.Add(64756282);
					return true;
				}
			}
		}
		return false;
	}

	public bool CollaborationActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		ClientCard target = base.Util.GetBestBotMonster(onlyATK: true);
		if (base.Util.GetOneEnemyBetterThanMyBest() == null && (base.Enemy.SpellZone.GetFirstMatchingCard((ClientCard card) => card.IsFacedown()) != null || base.Enemy.MonsterZone.GetMatchingCardsCount((ClientCard card) => card.GetDefensePower() < target.Attack) >= 2))
		{
			base.AI.SelectCard(target);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(10805153);
			return true;
		}
		return false;
	}

	public bool LightningStormActivate()
	{
		int bestPower = 0;
		foreach (ClientCard item in base.Bot.Hand)
		{
			if (item.IsMonster() && item.Level <= 4 && item.Attack > bestPower)
			{
				bestPower = item.Attack;
			}
		}
		int num = -1;
		if (base.Enemy.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFloodgate() && card.IsAttack()) != null || base.Enemy.MonsterZone.GetMatchingCardsCount((ClientCard card) => card.IsAttack() && card.Attack >= bestPower) >= 2)
		{
			num = 0;
		}
		else if (base.Enemy.GetSpellCount() >= 2 || base.Util.GetProblematicEnemySpell() != null)
		{
			num = 1;
		}
		if (num == -1)
		{
			return false;
		}
		if (base.Enemy.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsAttack()) == null || base.Enemy.GetSpellCount() == 0)
		{
			base.AI.SelectOption(0);
			SelectSTPlace(null, avoid_Impermanence: true);
			return true;
		}
		base.AI.SelectOption(num);
		SelectSTPlace(null, avoid_Impermanence: true);
		return true;
	}

	public bool PotofExtravaganceActivate()
	{
		if (SpellNegatable())
		{
			return false;
		}
		SelectSTPlace(base.Card, avoid_Impermanence: true);
		base.AI.SelectOption(1);
		return true;
	}

	public bool DarkRulerNoMoreActivate()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if (base.Enemy.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFloodgate() && !card.IsDisabled()) != null)
		{
			SelectSTPlace(null, avoid_Impermanence: true);
			return true;
		}
		return false;
	}

	public bool CreationActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		int num = (base.Bot.HasInMonstersZone(84523092) ? 1 : 0) + (base.Bot.HasInMonstersZone(21522601) ? 1 : 0);
		if (base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card != base.Card && card.IsSpell()) + CheckRecyclableCount() - 1 < num)
		{
			return false;
		}
		if (!summoned || (base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode)) == null && base.Bot.Hand.GetFirstMatchingCard((ClientCard card) => card.IsMonster() && card.HasSetcode(Witchcraft_setcode) && card.Level <= 4) == null))
		{
			base.AI.SelectCard(21744288, 95245544, 64756282, 59851535, 71074418);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(57916305);
			return true;
		}
		if (base.Bot.HasInHand(71074418))
		{
			return false;
		}
		if (base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode)) == null)
		{
			base.AI.SelectCard(71074418, 21744288, 95245544, 64756282, 59851535);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(57916305);
			return true;
		}
		base.AI.SelectCard(21744288, 95245544, 64756282, 59851535, 71074418);
		SelectSTPlace(null, avoid_Impermanence: true);
		ActivatedCards.Add(57916305);
		return true;
	}

	public int HolidayCheck(ClientCard except_card = null)
	{
		List<int> list = new List<int> { 84523092, 21522601, 71074418 };
		foreach (int cardid in list)
		{
			if (base.Bot.HasInGraveyard(cardid) && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.Id == cardid) == null)
			{
				Logger.DebugWriteLine("*** Holiday check 1st: " + cardid);
				return cardid;
			}
		}
		list.Clear();
		if (CheckProblematicCards() == null)
		{
			if (base.Bot.HasInGraveyard(71074418) && base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.IsFaceup() && card.HasSetcode(Witchcraft_setcode)) != null)
			{
				Logger.DebugWriteLine("*** Holiday check 2nd: GolemAruru");
				return 71074418;
			}
			list.Add(21744288);
			list.Add(95245544);
			list.Add(64756282);
			list.Add(59851535);
			foreach (int cardid2 in list)
			{
				if (!UseSSEffect.Contains(cardid2) && base.Bot.Graveyard.GetFirstMatchingCard((ClientCard card) => card.Id == cardid2 && card != except_card) != null && CheckDiscardableSpellCount(base.Card) > 0)
				{
					Logger.DebugWriteLine("*** Holiday check 3rd: " + cardid2);
					return cardid2;
				}
			}
		}
		else
		{
			list.Add(84523092);
			list.Add(21522601);
			list.Add(71074418);
			foreach (int cardid3 in list)
			{
				if (base.Bot.Graveyard.GetFirstMatchingCard((ClientCard card) => card.Id == cardid3 && card != except_card) != null)
				{
					return cardid3;
				}
			}
		}
		return 0;
	}

	public bool HolidayActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		int num = HolidayCheck();
		if (num != 0)
		{
			base.AI.SelectCard(num);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(83301414);
			return true;
		}
		return false;
	}

	public bool CalledbytheGraveActivate()
	{
		if (NegatedCheck())
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
				ClientCard firstMatchingCard = base.Enemy.Graveyard.GetFirstMatchingCard((ClientCard card) => card.IsMonster() && card.IsOriginalCode(code));
				if (firstMatchingCard != null)
				{
					if (base.Card.Location != CardLocation.SpellZone)
					{
						SelectSTPlace(null, avoid_Impermanence: true);
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
					int originCode = item.GetOriginCode();
					base.AI.SelectCard(item);
					currentNegatingIdList.Add(originCode);
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
					int originCode2 = list[0].GetOriginCode();
					base.AI.SelectCard(list);
					currentNegatingIdList.Add(originCode2);
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
			int originCode3 = list2[0].GetOriginCode();
			if (base.Card.Location != CardLocation.SpellZone)
			{
				SelectSTPlace(null, avoid_Impermanence: true);
			}
			base.AI.SelectCard(list2);
			currentNegatingIdList.Add(originCode3);
			return true;
		}
		return false;
	}

	public bool DrapingActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		IList<ClientCard> matchingCards = base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFloodgate() && !card.IsDisabled() && card.IsSpell());
		IList<ClientCard> matchingCards2 = base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFloodgate() && !card.IsDisabled() && card.IsTrap());
		List<ClientCard> second = CardListShuffle(base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFaceup() && card.IsSpell()).ToList());
		List<ClientCard> second2 = CardListShuffle(base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFaceup() && card.IsTrap()).ToList());
		List<ClientCard> second3 = CardListShuffle(base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFacedown()).ToList());
		if (base.Duel.Player == 0 || base.Duel.Phase == DuelPhase.End)
		{
			IList<ClientCard> list = matchingCards.Union(matchingCards2).Union(second).Union(second2)
				.Union(second3)
				.ToList();
			if (list.Count() == 0)
			{
				return false;
			}
			base.AI.SelectCard(list);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(56894757);
			return true;
		}
		IList<ClientCard> list2 = matchingCards2.Union(second2).ToList();
		if (list2.Count() == 0)
		{
			return false;
		}
		list2 = list2.Union(matchingCards).Union(second).Union(second3)
			.ToList();
		base.AI.SelectCard(list2);
		SelectSTPlace(null, avoid_Impermanence: true);
		ActivatedCards.Add(56894757);
		return true;
	}

	public bool CrossoutDesignatorActivate()
	{
		if (NegatedCheck() || CheckLastChainNegated())
		{
			return false;
		}
		if (base.Duel.LastChainPlayer == 1 && base.Util.GetLastChainCard() != null)
		{
			int originCode = base.Util.GetLastChainCard().GetOriginCode();
			if (originCode == 0)
			{
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
					SelectSTPlace(null, avoid_Impermanence: true);
				}
				base.AI.SelectAnnounceID(originCode);
				currentNegatingIdList.Add(originCode);
				return true;
			}
		}
		return false;
	}

	public bool UnveilingActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (NegatedCheck())
		{
			return false;
		}
		if (base.Bot.HasInHandOrInSpellZone(14532163) && base.Bot.SpellZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup()) + base.Bot.MonsterZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup()) == 0 && LightningStormActivate())
		{
			return false;
		}
		if (base.Bot.HasInHand(important_witchcraft))
		{
			base.AI.SelectCard(important_witchcraft);
			SelectSTPlace(null, avoid_Impermanence: true);
			ActivatedCards.Add(70226289);
			return true;
		}
		return false;
	}

	public bool ScrollActivate()
	{
		if (SpellNegatable() || base.Card.Location == CardLocation.Grave || base.Duel.Phase == DuelPhase.Main2)
		{
			return false;
		}
		if (base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasRace(CardRace.SpellCaster)) == null)
		{
			return false;
		}
		SelectSTPlace(null, avoid_Impermanence: true);
		return true;
	}

	public bool MagiciansRestageActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			if (base.Enemy.SpellZone.GetFirstMatchingCard((ClientCard card) => card.IsFacedown()) != null)
			{
				base.AI.SelectCard(13758665, 87769556);
			}
			else
			{
				base.AI.SelectCard(87769556, 13758665);
			}
			return true;
		}
		if (SpellNegatable())
		{
			return false;
		}
		if (CheckDiscardableSpellCount(base.Card) < 1)
		{
			return false;
		}
		int num = 0;
		int[] array = new int[3] { 64756282, 95245544, 59851535 };
		foreach (int num3 in array)
		{
			if (!UseSSEffect.Contains(num3) && base.Bot.HasInGraveyard(num3))
			{
				num = num3;
				break;
			}
		}
		if (num == 0)
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			SelectSTPlace(null, avoid_Impermanence: true);
			return true;
		}
		base.AI.SelectCard(num);
		return true;
	}

	public bool WitchcrafterBystreetActivate()
	{
		if (SpellNegatable() || base.Card.Location == CardLocation.Grave)
		{
			return false;
		}
		if (base.Bot.HasInSpellZone(83289866, notDisabled: true) || base.Bot.MonsterZone.GetFirstMatchingCard((ClientCard card) => card.HasSetcode(Witchcraft_setcode) && card.IsFaceup()) == null)
		{
			return false;
		}
		SelectSTPlace(null, avoid_Impermanence: true);
		return true;
	}

	public bool InfiniteImpermanenceActivate()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if (CheckLastChainNegated())
		{
			return false;
		}
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (monster.IsDisabled() || base.Duel.LastChainPlayer == 0 || (!monster.IsMonsterShouldBeDisabledBeforeItUseEffect() && !monster.IsFloodgate() && (base.Duel.Phase <= DuelPhase.Main1 || base.Duel.Phase >= DuelPhase.Main2 || (!monster.IsMonsterDangerous() && !monster.IsMonsterInvincible() && (!monster.IsMonsterHasPreventActivationEffectInBattle() || !base.Bot.HasInMonstersZone(21522601))))))
			{
				continue;
			}
			if (base.Card.Location == CardLocation.SpellZone)
			{
				for (int i = 0; i < 5; i++)
				{
					if (base.Bot.SpellZone[i] == base.Card)
					{
						Impermanence_list.Add(i);
						break;
					}
				}
			}
			if (base.Card.Location == CardLocation.Hand)
			{
				SelectSTPlace(base.Card, avoid_Impermanence: true);
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
				List<ClientCard> monsters = base.Enemy.GetMonsters();
				monsters.Sort(CardContainer.CompareCardAttack);
				monsters.Reverse();
				foreach (ClientCard item in monsters)
				{
					if (item.IsFaceup() && !item.IsShouldNotBeTarget() && !item.IsShouldNotBeSpellTrapTarget())
					{
						base.AI.SelectCard(item);
						Impermanence_list.Add(num);
						return true;
					}
				}
			}
		}
		if (lastChainCard == null || lastChainCard.Controller != 1 || lastChainCard.Location != CardLocation.MonsterZone || CheckLastChainNegated() || lastChainCard.IsShouldNotBeTarget() || lastChainCard.IsShouldNotBeSpellTrapTarget())
		{
			return false;
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			for (int k = 0; k < 5; k++)
			{
				if (base.Bot.SpellZone[k] == base.Card)
				{
					Impermanence_list.Add(k);
					break;
				}
			}
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			SelectSTPlace(base.Card, avoid_Impermanence: true);
		}
		if (lastChainCard != null)
		{
			base.AI.SelectCard(lastChainCard);
		}
		else
		{
			List<ClientCard> monsters2 = base.Enemy.GetMonsters();
			monsters2.Sort(CardContainer.CompareCardAttack);
			monsters2.Reverse();
			foreach (ClientCard item2 in monsters2)
			{
				if (item2.IsFaceup() && !item2.IsShouldNotBeTarget() && !item2.IsShouldNotBeSpellTrapTarget())
				{
					base.AI.SelectCard(item2);
					return true;
				}
			}
		}
		return true;
	}

	public bool MasterpieceActivate()
	{
		if (base.Card.Location == CardLocation.SpellZone)
		{
			if (NegatedCheck())
			{
				return false;
			}
			IList<ClientCard> matchingCards = base.Bot.Graveyard.GetMatchingCards((ClientCard card) => card.IsSpell() && CheckRemainInDeck(card.Id) > 0);
			IList<ClientCard> matchingCards2 = base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => card.IsSpell() && CheckRemainInDeck(card.Id) > 0);
			List<ClientCard> cards = CardListShuffle(matchingCards.Union(matchingCards2).ToList());
			base.AI.SelectCard(cards);
			return true;
		}
		if (base.Bot.HasInHandOrInSpellZone(14532163) && base.Bot.SpellZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup()) + base.Bot.MonsterZone.GetMatchingCardsCount((ClientCard card) => card.IsFaceup()) == 0 && LightningStormActivate())
		{
			return false;
		}
		List<ClientCard> list = CardListShuffle(base.Bot.Graveyard.GetMatchingCards((ClientCard card) => card.IsSpell() && !card.HasSetcode(Witchcraft_setcode) && card.Id != 73594093).ToList());
		if (base.Bot.HasInGraveyard(94553671))
		{
			list = CardListShuffle(base.Bot.Graveyard.GetMatchingCards((ClientCard card) => card.IsSpell() && card.HasSetcode(Witchcraft_setcode)).ToList()).Union(list).ToList();
		}
		int num = list.Count();
		int num2 = CheckDiscardableSpellCount();
		foreach (int item in new List<int> { 84523092, 21522601, 21744288, 95245544, 59851535, 64756282 })
		{
			if (base.Bot.HasInMonstersZone(item))
			{
				num2--;
			}
		}
		if (num2 >= 1 && base.Duel.Player == 0)
		{
			int[] array = new int[4] { 21744288, 95245544, 64756282, 59851535 };
			foreach (int num4 in array)
			{
				int num5 = witchcraft_level[num4];
				if ((!UseSSEffect.Contains(num4) & (CheckRemainInDeck(num4) > 0)) && num5 <= num)
				{
					base.AI.SelectNumber(num5);
					base.AI.SelectCard(list);
					base.AI.SelectNextCard(num4);
					return true;
				}
			}
		}
		List<int> list2 = new List<int>();
		if (base.Bot.HasInMonstersZone(84523092))
		{
			list2.Add(21522601);
			list2.Add(84523092);
		}
		else
		{
			list2.Add(84523092);
			list2.Add(21522601);
		}
		list2.Add(71074418);
		foreach (int item2 in list2)
		{
			int num6 = witchcraft_level[item2];
			if (CheckRemainInDeck(item2) > 0 && num6 <= num)
			{
				base.AI.SelectNumber(num6);
				base.AI.SelectCard(list);
				base.AI.SelectNextCard(item2);
				return true;
			}
		}
		return false;
	}

	public bool PatronusActivate()
	{
		if (base.ActivateDescription == 94)
		{
			return true;
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			if (NegatedCheck() || base.Duel.LastChainPlayer == 0)
			{
				return false;
			}
			int num = 0;
			int[] array = new int[7] { 83289866, 83301414, 57916305, 56894757, 19673561, 70226289, 10805153 };
			foreach (int num2 in array)
			{
				if (!base.Bot.HasInHandOrInSpellZone(num2) && !base.Bot.HasInGraveyard(num2))
				{
					num = num2;
					break;
				}
			}
			List<int> first = new List<int> { 84523092, 21522601, 71074418, 21744288, 95245544 };
			if (num == 0)
			{
				List<int> second = new List<int> { 95245544, 64756282, 21744288, 59851535 };
				first = first.Union(second).ToList();
			}
			else
			{
				List<int> second2 = new List<int> { 21744288, 95245544, 64756282, 59851535 };
				first = first.Union(second2).ToList();
			}
			foreach (int cardid in first)
			{
				ClientCard firstMatchingCard = base.Bot.Banished.GetFirstMatchingCard((ClientCard card) => card.Id == cardid);
				if (firstMatchingCard != null)
				{
					base.AI.SelectCard(firstMatchingCard);
					base.AI.SelectNextCard(num);
					return true;
				}
			}
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (base.Bot.HasInHandOrInSpellZoneOrInGraveyard(55072170))
			{
				return false;
			}
			IList<ClientCard> matchingCards = base.Bot.Banished.GetMatchingCards((ClientCard card) => card.IsSpell() && card.HasSetcode(Witchcraft_setcode));
			base.AI.SelectCard(matchingCards);
			return true;
		}
		return false;
	}

	public bool Lv8Summon()
	{
		if (base.Bot.HasInMonstersZone(38814750) && base.Bot.HasInMonstersZone(49036338))
		{
			List<int> ids = new List<int> { 49036338, 38814750 };
			base.AI.SelectMaterials(ids);
			return true;
		}
		return false;
	}

	public bool BorreloadSavageDragonSummon()
	{
		if (base.Bot.Graveyard.GetFirstMatchingCard((ClientCard card) => card.HasType(CardType.Link)) == null)
		{
			return false;
		}
		return Lv8Summon();
	}

	public static int BorreloadSavageDragonEquipCompare(ClientCard cardA, ClientCard cardB)
	{
		if (cardA.LinkCount > cardB.LinkCount)
		{
			return -1;
		}
		if (cardA.LinkCount < cardB.LinkCount)
		{
			return -1;
		}
		if (cardA.Attack > cardB.Attack)
		{
			return 1;
		}
		if (cardA.Attack < cardB.Attack)
		{
			return -1;
		}
		return 0;
	}

	public bool BorreloadSavageDragonActivate()
	{
		if (base.ActivateDescription == base.Util.GetStringId(27548199, 0))
		{
			List<ClientCard> list = base.Bot.Graveyard.GetMatchingCards((ClientCard card) => card.HasType(CardType.Link)).ToList();
			list.Sort(BorreloadSavageDragonEquipCompare);
			base.AI.SelectCard(list);
			return true;
		}
		if (NegatedCheck() || base.Duel.LastChainPlayer != 1)
		{
			return false;
		}
		if (base.Util.GetLastChainCard().HasSetcode(286))
		{
			_ = base.Util.GetLastChainCard().Location;
			_ = 2;
			return false;
		}
		return false;
	}

	public bool DracoBerserkeroftheTenyiActivate()
	{
		Logger.DebugWriteLine("DracoBerserkeroftheTenyi's Effect: " + base.ActivateDescription);
		return true;
	}

	public bool PSYOmegaActivate()
	{
		if (base.Duel.Phase == DuelPhase.Standby)
		{
			if (base.Bot.Banished.Count == 0)
			{
				return false;
			}
			List<ClientCard> cards = CardListShuffle(base.Bot.Banished.GetMatchingCards((ClientCard card) => card.HasSetcode(Witchcraft_setcode)).ToList());
			base.AI.SelectCard(cards);
			return true;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (base.Duel.Player == 1 || base.Bot.HasInMonstersZone(8802510) || base.Util.IsChainTarget(base.Card))
			{
				return true;
			}
			return base.Util.IsAllEnemyBetterThanValue(base.Card.Attack, onlyATK: true);
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			List<ClientCard> list = CheckDangerousCardinEnemyGrave();
			if (list.Count > 0)
			{
				base.AI.SelectCard(list);
				return true;
			}
			if (!base.Bot.HasInHandOrInSpellZoneOrInGraveyard(83301414) && base.Bot.HasInGraveyard(important_witchcraft))
			{
				base.AI.SelectCard(important_witchcraft);
				return true;
			}
			if (CheckProblematicCards() == null)
			{
				base.AI.SelectCard(24224830, 65681983, 23434538, 14558127, 87769556, 13758665, 40252269, 94553671, 14532163, 58577036);
				return true;
			}
		}
		return false;
	}

	public bool TGWonderMagicianActivate()
	{
		if (base.Card.Location != CardLocation.MonsterZone)
		{
			return true;
		}
		Logger.DebugWriteLine("TGWonderMagician: " + base.ActivateDescription);
		List<ClientCard> first = base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFloodgate()).ToList();
		List<ClientCard> second = base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFaceup()).ToList();
		List<ClientCard> cards = Enumerable.Union(second: base.Enemy.SpellZone.GetMatchingCards((ClientCard card) => card.IsFacedown()).ToList(), first: first.Union(second).ToList()).ToList();
		base.AI.SelectCard(cards);
		return true;
	}

	public List<ClientCard> BorrelswordDragonSummonCheck(ClientCard included = null)
	{
		List<ClientCard> result = new List<ClientCard>();
		List<ClientCard> list = new List<ClientCard>();
		if (included != null)
		{
			list.Add(included);
		}
		List<ClientCard> list2 = CheckLinkMaterials(4, 3, need_tuner: false, list);
		if (list2.Count < 3)
		{
			return result;
		}
		if (base.Util.GetOneEnemyBetterThanMyBest() != null)
		{
			return list2;
		}
		int num = 0;
		foreach (ClientCard item in list2)
		{
			num += item.Attack;
		}
		if (num >= 3000)
		{
			return result;
		}
		return list2;
	}

	public bool BorrelswordDragonSummon()
	{
		List<ClientCard> list = BorrelswordDragonSummonCheck();
		if (list.Count < 3)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	public bool BorrelswordDragonActivate()
	{
		if (base.ActivateDescription == -1)
		{
			ClientCard battlingMonster = base.Enemy.BattlingMonster;
			if (battlingMonster != null && battlingMonster.HasPosition(CardPosition.Attack))
			{
				return base.Card.Attack - battlingMonster.Attack < base.Enemy.LifePoints;
			}
			return true;
		}
		ClientCard bestEnemyMonster = base.Util.GetBestEnemyMonster(onlyFaceup: true);
		ClientCard lowestAttackMonster = base.Bot.GetMonsters().GetLowestAttackMonster();
		if (bestEnemyMonster == null || bestEnemyMonster.HasPosition(CardPosition.FaceDown))
		{
			return false;
		}
		if (lowestAttackMonster == null || lowestAttackMonster.HasPosition(CardPosition.FaceDown))
		{
			return false;
		}
		if (bestEnemyMonster.Attack >= lowestAttackMonster.RealPower)
		{
			base.AI.SelectCard(bestEnemyMonster);
			return true;
		}
		return false;
	}

	public List<ClientCard> KnightmareUnicornSummonCheck(ClientCard included = null)
	{
		List<ClientCard> result = new List<ClientCard>();
		List<ClientCard> list = new List<ClientCard>();
		if (included != null)
		{
			list.Add(included);
		}
		List<ClientCard> list2 = CheckLinkMaterials(3, 2, need_tuner: false, list);
		if (list2.Count < 2)
		{
			return result;
		}
		if (CheckProblematicCards(canBeTarget: true, OnlyDanger: true) != null)
		{
			if (base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card != base.Card) == 0)
			{
				return result;
			}
			return list2;
		}
		int num = 0;
		foreach (ClientCard item in list2)
		{
			num += item.Attack;
		}
		if (num >= 2200)
		{
			return result;
		}
		return list2;
	}

	public bool KnightmareUnicornSummon()
	{
		List<ClientCard> list = KnightmareUnicornSummonCheck();
		if (list.Count < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	public bool KnightmareUnicornActivate()
	{
		ClientCard clientCard = CheckProblematicCards(canBeTarget: true);
		if (clientCard == null)
		{
			return false;
		}
		IList<ClientCard> list = new List<ClientCard>();
		if (!clientCard.IsShouldNotBeMonsterTarget() && !clientCard.IsShouldNotBeTarget())
		{
			list.Add(clientCard);
		}
		foreach (ClientCard item in base.Enemy.GetMonstersInExtraZone())
		{
			if (item != null && !list.Contains(item) && !item.IsShouldNotBeMonsterTarget() && !item.IsShouldNotBeTarget())
			{
				list.Add(item);
			}
		}
		foreach (ClientCard item2 in base.Enemy.GetMonstersInMainZone())
		{
			if (item2 != null && !list.Contains(item2) && !item2.IsShouldNotBeMonsterTarget() && !item2.IsShouldNotBeTarget())
			{
				list.Add(item2);
			}
		}
		foreach (ClientCard spell in base.Enemy.GetSpells())
		{
			if (spell != null && !list.Contains(spell) && !spell.IsShouldNotBeMonsterTarget() && !spell.IsShouldNotBeTarget())
			{
				list.Add(spell);
			}
		}
		if (list.Count > 0)
		{
			SelectDiscardSpell();
			base.AI.SelectNextCard(list);
			return true;
		}
		return false;
	}

	public List<ClientCard> KnightmarePhoenixSummonCheck(ClientCard included = null)
	{
		List<ClientCard> result = new List<ClientCard>();
		List<ClientCard> list = new List<ClientCard>();
		if (included != null)
		{
			list.Add(included);
		}
		List<ClientCard> list2 = CheckLinkMaterials(2, 2, need_tuner: true, list);
		if (list2.Count < 2)
		{
			return result;
		}
		if (base.Util.GetProblematicEnemySpell() != null)
		{
			if (base.Bot.Hand.GetMatchingCardsCount((ClientCard card) => card != base.Card) == 0)
			{
				return result;
			}
			return list2;
		}
		if (list2[0].Attack + list2[1].Attack >= 1900)
		{
			return result;
		}
		return list2;
	}

	public bool KnightmarePhoenixSummon()
	{
		List<ClientCard> list = KnightmarePhoenixSummonCheck();
		if (list.Count < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	public bool KnightmarePhoenixActivate()
	{
		List<ClientCard> list = new List<ClientCard>();
		list.Add(base.Util.GetProblematicEnemySpell());
		List<ClientCard> spells = base.Enemy.GetSpells();
		List<ClientCard> list2 = new List<ClientCard>();
		List<ClientCard> list3 = new List<ClientCard>();
		CardListShuffle(spells);
		foreach (ClientCard item in spells)
		{
			if (item.HasPosition(CardPosition.FaceUp) && !item.IsShouldNotBeTarget() && !item.IsShouldNotBeMonsterTarget())
			{
				list2.Add(item);
			}
			else if (item.HasPosition(CardPosition.FaceDown))
			{
				list3.Add(item);
			}
		}
		list = list.Union(list2).Union(list3).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		SelectDiscardSpell();
		base.AI.SelectNextCard(list);
		return true;
	}

	public List<ClientCard> CrystronHalqifibraxSummonCheck(ClientCard included = null)
	{
		List<ClientCard> result = new List<ClientCard>();
		List<ClientCard> list = new List<ClientCard>();
		if (included != null)
		{
			list.Add(included);
		}
		if (CheckLinkMaterials(2, 2, need_tuner: true, list).Count < 2)
		{
			return result;
		}
		CheckRemainInDeck(38814750, 14558127);
		return result;
	}

	public bool CrystronHalqifibraxSummon()
	{
		List<ClientCard> list = CrystronHalqifibraxSummonCheck();
		if (list.Count < 2)
		{
			return false;
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	public bool CrystronHalqifibraxActivate()
	{
		if (base.Duel.Player == 0)
		{
			return true;
		}
		if (base.Util.IsChainTarget(base.Card) || base.Util.GetProblematicEnemySpell() != null)
		{
			return true;
		}
		if (base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.BattleStart && base.Util.IsOneEnemyBetterThanValue(1500, onlyATK: true))
		{
			if (base.Util.IsOneEnemyBetterThanValue(1900, onlyATK: true))
			{
				base.AI.SelectPosition(CardPosition.FaceUpDefence);
			}
			else
			{
				base.AI.SelectPosition(CardPosition.FaceUpAttack);
			}
			return true;
		}
		return false;
	}

	public bool SalamangreatAlmirajSummonCheck(ClientCard included = null)
	{
		if (CheckDiscardableSpellCount() >= 2)
		{
			return false;
		}
		List<ClientCard> monsters = base.Bot.GetMonsters();
		if (included != null)
		{
			monsters.Add(included);
		}
		if (monsters.GetCardCount(95245544) + monsters.GetCardCount(64756282) == 0)
		{
			return false;
		}
		if (base.Bot.HasInHand(important_witchcraft))
		{
			return true;
		}
		return false;
	}

	public bool SalamangreatAlmirajSummon()
	{
		if (!SalamangreatAlmirajSummonCheck())
		{
			return false;
		}
		List<int> ids = new List<int> { 95245544, 64756282 };
		base.AI.SelectMaterials(ids);
		return true;
	}

	public bool SalamangreatAlmirajActivate()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return true;
		}
		if (base.Duel.Player == 1)
		{
			base.AI.SelectCard(base.Util.GetBestBotMonster());
			return true;
		}
		return false;
	}

	public bool PSYLambdaSummon()
	{
		if (base.Bot.HasInMonstersZone(38814750) && base.Bot.HasInMonstersZone(49036338) && (base.Bot.HasInHand(38814750) || base.Bot.HasInMonstersZone(74586817)))
		{
			List<int> ids = new List<int> { 49036338, 38814750 };
			base.AI.SelectMaterials(ids);
			return true;
		}
		return false;
	}

	public int RelinquishedAnimaSummonCheck(ClientCard included = null)
	{
		if (CheckDiscardableSpellCount() >= 2)
		{
			return -1;
		}
		List<ClientCard> monsters = base.Bot.GetMonsters();
		if (included != null)
		{
			monsters.Add(included);
		}
		int result = -1;
		int num = base.Util.GetBestAttack(base.Bot);
		List<ClientCard> list = new List<ClientCard>
		{
			base.Enemy.MonsterZone[6],
			base.Enemy.MonsterZone[5]
		};
		List<int> list2 = new List<int> { 1, 3 };
		for (int i = 0; i < 2; i++)
		{
			ClientCard clientCard = list[i];
			int num2 = list2[i];
			if (clientCard != null && clientCard.HasLinkMarker(128) && clientCard.Attack > num && !clientCard.IsShouldNotBeMonsterTarget() && !clientCard.IsShouldNotBeTarget())
			{
				ClientCard clientCard2 = base.Bot.MonsterZone[num2];
				if (clientCard2 == null || clientCard2.Level == 1)
				{
					result = num2;
					num = clientCard.Attack;
				}
			}
		}
		list = new List<ClientCard>
		{
			base.Enemy.MonsterZone[3],
			base.Enemy.MonsterZone[1]
		};
		list2 = new List<int> { 5, 6 };
		for (int j = 0; j < 2; j++)
		{
			ClientCard clientCard3 = list[j];
			int num3 = list2[j];
			if (clientCard3 != null && clientCard3.Attack > num && !clientCard3.IsShouldNotBeMonsterTarget() && !clientCard3.IsShouldNotBeTarget() && base.Enemy.MonsterZone[11 - num3] == null)
			{
				ClientCard clientCard4 = base.Bot.MonsterZone[num3];
				if (clientCard4 == null || clientCard4.Level == 1)
				{
					result = num3;
					num = clientCard3.Attack;
				}
			}
		}
		return result;
	}

	public bool RelinquishedAnimaSummon()
	{
		int num = RelinquishedAnimaSummonCheck();
		Logger.DebugWriteLine("RelinquishedAnima summon check: " + num);
		if (num != -1)
		{
			int zones = (int)Math.Pow(2.0, num);
			base.AI.SelectPlace(zones);
			if (base.Bot.MonsterZone[num] != null && base.Bot.MonsterZone[num].Level == 1)
			{
				base.AI.SelectMaterials(base.Bot.MonsterZone[num]);
			}
			else
			{
				base.AI.SelectMaterials(64756282);
			}
			return true;
		}
		return false;
	}

	public bool ChickenGame()
	{
		if (SpellNegatable())
		{
			return false;
		}
		if (base.Bot.LifePoints <= 1000)
		{
			return false;
		}
		if (base.Bot.LifePoints - 1000 <= base.Enemy.LifePoints && base.ActivateDescription == base.Util.GetStringId(67616300, 0))
		{
			return true;
		}
		if (base.Bot.LifePoints - 1000 > base.Enemy.LifePoints && base.ActivateDescription == base.Util.GetStringId(67616300, 1))
		{
			return true;
		}
		return false;
	}

	protected override bool DefaultSetForDiabellze()
	{
		if (base.DefaultSetForDiabellze())
		{
			SelectSTPlace(null, avoid_Impermanence: true);
			return true;
		}
		return false;
	}
}
