using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Tearlaments", "AI_Tearlaments", "Normal")]
internal class TearlamentsExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int ShaddollBeast = 3717252;

		public const int ShaddollDragon = 77723643;

		public const int TearlamentsScheiren = 572850;

		public const int TearlamentsReinoheart = 73956664;

		public const int KelbektheAncientVanguard = 25926710;

		public const int MudoratheSwordOracle = 99937011;

		public const int AgidotheAncientSentinel = 62320425;

		public const int KeldotheSacredProtector = 63542003;

		public const int NaelshaddollAriel = 97518132;

		public const int TearlamentsHavnis = 37961969;

		public const int TearlamentsMerrli = 74078255;

		public const int DivineroftheHerald = 92919429;

		public const int HeraldofOrangeLight = 17266660;

		public const int HeraldofGreenLight = 21074344;

		public const int Eva = 40177746;

		public const int TearlamentsScream = 6767771;

		public const int PrimevalPlanetPerlereino = 77103950;

		public const int TearlamentsSulliek = 74920585;

		public const int TearlamentsKaleidoHeart = 28226490;

		public const int TearlamentsRulkallos = 84330567;

		public const int PredaplantDragostapelia = 69946549;

		public const int TearlamentsKitkallos = 92731385;

		public const int ElShaddollWinda = 94977269;

		public const int ElderEntityNtss = 80532587;

		public const int BaronnedeFleur = 84815190;

		public const int FADawnDragster = 33158448;

		public const int AbyssDweller = 21044178;

		public const int UnderworldGoddessoftheClosedWorld = 98127546;

		public const int MekkKnightCrusadiaAvramax = 21887175;

		public const int KnightmareUnicorn = 38342335;

		public const int SprightElf = 27381364;

		public const int IP = 65741786;
	}

	private enum Flag
	{
		TearlamentsKitkallos = 1,
		TearlamentsRulkallos = 2,
		TearlamentsKaleidoHeart = 4,
		PredaplantDragostapelia = 8,
		ElShaddollWinda = 0x10
	}

	private const bool IS_YGOPRO = true;

	private int SETCODE = 385;

	private bool activate_TearlamentsScheiren_1;

	private bool activate_TearlamentsScheiren_2;

	private bool activate_TearlamentsReinoheart_1;

	private bool activate_TearlamentsReinoheart_2;

	private bool activate_TearlamentsHavnis_1;

	private bool activate_TearlamentsHavnis_2;

	private bool activate_TearlamentsMerrli_1;

	private bool activate_TearlamentsMerrli_2;

	private bool activate_TearlamentsKitkallos_1;

	private bool activate_TearlamentsKitkallos_2;

	private bool activate_TearlamentsKitkallos_3;

	private bool TearlamentsKitkallostohand = true;

	private bool activate_TearlamentsScream_1;

	private bool activate_TearlamentsScream_2;

	private bool activate_TearlamentsSulliek_1;

	private bool activate_TearlamentsSulliek_2;

	private bool activate_AgidotheAncientSentinel_2;

	private bool activate_KelbektheAncientVanguard_2;

	private bool activate_TearlamentsRulkallos_1;

	private bool activate_TearlamentsRulkallos_2;

	private bool activate_MudoratheSwordOracle_2;

	private bool activate_KeldotheSacredProtector_2;

	private bool activate_PrimevalPlanetPerlereino_1;

	private bool activate_PrimevalPlanetPerlereino_2;

	private bool activate_TearlamentsKaleidoHeart_1;

	private bool activate_TearlamentsKaleidoHeart_2;

	private bool activate_Eva;

	private bool activate_DivineroftheHerald;

	private bool summoned;

	private bool spsummoned;

	private bool TearlamentsKitkallos_summoned;

	private bool summon_SprightElf;

	private bool chainlist;

	private bool pre_activate_PrimevalPlanetPerlereino;

	private bool select_TearlamentsKitkallos;

	private List<ClientCard> remainCards = new List<ClientCard>();

	private List<ClientCard> fusionExtra = new List<ClientCard>();

	private List<ClientCard> fusionMaterial = new List<ClientCard>();

	private List<ClientCard> on_chaining_cards = new List<ClientCard>();

	private List<ClientCard> mcard_0 = new List<ClientCard> { null, null, null };

	private List<ClientCard> mcard_1 = new List<ClientCard> { null, null, null };

	private List<ClientCard> mcard_2 = new List<ClientCard> { null, null, null };

	private List<ClientCard> mcard_3 = new List<ClientCard> { null, null, null };

	private List<bool> ran_fusion_mode_0 = new List<bool> { false, false, false };

	private List<bool> ran_fusion_mode_1 = new List<bool> { false, false, false };

	private List<bool> ran_fusion_mode_2 = new List<bool> { false, false, false };

	private List<bool> ran_fusion_mode_3 = new List<bool> { false, false, false };

	private ClientCard _PredaplantDragostapelia;

	private ClientCard chain_PredaplantDragostapelia;

	private ClientCard chain_TearlamentsSulliek;

	private ClientCard tgcard;

	private ClientCard no_fusion_card;

	private List<ClientCard> e_PredaplantDragostapelia_cards = new List<ClientCard>();

	private ClientCard link_card;

	private List<int> no_link_ids = new List<int> { 84330567, 84815190, 21887175, 21044178, 69946549, 98127546, 33158448, 28226490 };

	private List<int> key_send_to_deck_ids = new List<int> { 74078255, 572850, 37961969, 73956664, 8736823, 98715423, 17484499 };

	private List<int> all_key_card_ids = new List<int>
	{
		55623480, 86682165, 60461804, 10000090, 28651380, 97565997, 87074380, 80208158, 95440946, 93880808,
		16261341, 91749600, 26866984, 5141117, 50383626, 30576089, 22586618, 7445307, 73478096, 18558867,
		51617185, 60880471, 34172284, 88774734, 25451383, 71197066, 24226942, 78077209, 98787535, 29601381,
		83203672, 62383431, 89552119, 92418590, 31042659, 83303851, 17502671, 11366199, 46668237, 64382839,
		46290741, 25607552, 1295442, 5560911, 56174248, 7407724, 1855886, 27198001, 11074235, 48372950,
		94142993, 81866673, 34966096, 42006475, 99733359, 20799347, 71985676, 55787576, 43266605, 3422200,
		97962972, 20773176, 84976088, 55151012, 80208323, 98881700, 56677752, 24506253, 6180710, 20056760,
		44928016, 45702014, 60316373, 37683547, 62962630, 20065259, 8571567, 21351206, 35998832, 26077387,
		37351133, 94730900, 83682209, 38695361, 59707204, 12469386, 1833916, 98806751, 82496097, 27182739,
		28762303, 74578072, 30303854, 5370235, 50546208, 44818, 67436768, 29169993, 10286023, 19667590,
		47897376, 74891384, 66752837, 43534808, 56815977, 81344070, 59724555, 93708824, 43411796, 7084129,
		51993760, 87988305, 23619206, 86962245, 48144778, 30068120, 9047460, 25538345, 69764158, 70645913,
		55702233, 10928224, 79531196, 23893227, 57421866, 94693857, 78080961, 9742784, 59185998, 93169863,
		46576366, 2830693, 68543408, 50820852, 2511, 8972398, 61488417, 71734607, 63180841, 51447164,
		74586817, 28403802, 91575236, 286392, 97584719, 60195675, 24701066, 20665527, 73345237, 23732205,
		146746, 72218246, 41999284, 60303245
	};

	private List<int> key_no_send_to_deck_ids = new List<int> { 84330567, 50588353, 44097050 };

	private List<int> key_remove_ids = new List<int>
	{
		74078255, 572850, 37961969, 73956664, 15291624, 11738489, 44097050, 15291624, 63288573, 70369116,
		83152482, 72329844, 24094258, 86066372, 74997493, 85289965, 21887175, 11738489, 98127546, 50588353,
		10389142, 90590303, 27548199
	};

	private List<int> key_no_remove_ids = new List<int>
	{
		18743376, 72355272, 81555617, 45960523, 29596581, 56713174, 80280944, 61103515, 28297833, 90020780,
		8736823
	};

	private List<int> bot_send_to_deck_ids = new List<int>();

	public TearlamentsExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 6767771, TearlamentsScreamEffect_1);
		AddExecutor(ExecutorType.Activate, 21044178, () => base.Duel.Player != 0);
		AddExecutor(ExecutorType.Activate, 28226490, TearlamentsKaleidoHeartEffect);
		AddExecutor(ExecutorType.Activate, 98127546);
		AddExecutor(ExecutorType.SpSummon, 33158448, FADawnDragsterSummon);
		AddExecutor(ExecutorType.Activate, 92731385, TearlamentsKitkallosEffect_2);
		AddExecutor(ExecutorType.SpSummon, 98127546, UnderworldGoddessoftheClosedWorldSummon);
		AddExecutor(ExecutorType.SpSummon, 27381364, SprightElfSummon_2);
		AddExecutor(ExecutorType.SpSummon, 65741786, IPSummon_2);
		AddExecutor(ExecutorType.Activate, 84815190, BaronnedeFleurEffect);
		AddExecutor(ExecutorType.Activate, 80532587, ElderEntityNtssEffect);
		AddExecutor(ExecutorType.Activate, 69946549, PredaplantDragostapeliaEffect);
		AddExecutor(ExecutorType.Activate, 17266660, base.DefaultTrap);
		AddExecutor(ExecutorType.Activate, 21074344, base.DefaultTrap);
		AddExecutor(ExecutorType.Activate, 84330567, TearlamentsRulkallosEffect);
		AddExecutor(ExecutorType.Activate, 33158448);
		AddExecutor(ExecutorType.Activate, 77103950, PrimevalPlanetPerlereinoEffect);
		AddExecutor(ExecutorType.Activate, 572850, TearlamentsScheirenEffect);
		AddExecutor(ExecutorType.Activate, 92731385, TearlamentsKitkallosEffect);
		AddExecutor(ExecutorType.Activate, 27381364, SprightElfEffect);
		AddExecutor(ExecutorType.SpSummon, 98127546, UnderworldGoddessoftheClosedWorldSummon_3);
		AddExecutor(ExecutorType.Activate, 99937011, MudoratheSwordOracleEffect);
		AddExecutor(ExecutorType.Activate, 92919429, DivineroftheHeraldEffect);
		AddExecutor(ExecutorType.Activate, 74078255, TearlamentsMerrliEffect);
		AddExecutor(ExecutorType.Activate, 37961969, TearlamentsHavnisEffect);
		AddExecutor(ExecutorType.Activate, 73956664, TearlamentsReinoheartEffect);
		AddExecutor(ExecutorType.Summon, 92919429, DivineroftheHeraldSummon);
		AddExecutor(ExecutorType.Summon, 74078255, delegate
		{
			summoned = true;
			return true;
		});
		AddExecutor(ExecutorType.Summon, 73956664, delegate
		{
			summoned = true;
			return true;
		});
		AddExecutor(ExecutorType.Activate, 62320425, AgidotheAncientSentinelEffect);
		AddExecutor(ExecutorType.Activate, 25926710, KelbektheAncientVanguardEffect);
		AddExecutor(ExecutorType.Activate, 97518132, NaelshaddollArielEffect);
		AddExecutor(ExecutorType.Activate, 77723643, ShaddollDragonEffect);
		AddExecutor(ExecutorType.Activate, 40177746, EvaEffect);
		AddExecutor(ExecutorType.Activate, 74920585, TearlamentsSulliekEffect);
		AddExecutor(ExecutorType.Activate, 6767771, () => !AllActivated());
		AddExecutor(ExecutorType.Activate, 63542003, MudoratheSwordOracleEffect);
		AddExecutor(ExecutorType.Activate, 3717252, () => base.Bot.Deck.Count > 0);
		AddExecutor(ExecutorType.Summon, 92919429, () => !base.Bot.HasInHand(21074344) && !base.Bot.HasInHand(17266660));
		AddExecutor(ExecutorType.SpSummon, 21044178, AbyssDwellerSummon_2);
		AddExecutor(ExecutorType.SpSummon, 27381364, SprightElfSummon);
		AddExecutor(ExecutorType.SpSummon, 84815190, BaronnedeFleurSummon);
		AddExecutor(ExecutorType.SpSummon, 33158448, delegate
		{
			SetSpSummon();
			return true;
		});
		AddExecutor(ExecutorType.SpSummon, 21044178, AbyssDwellerSummon);
		AddExecutor(ExecutorType.Activate, 65741786, IPEffect);
		AddExecutor(ExecutorType.SpSummon, 65741786, IPSummon);
		AddExecutor(ExecutorType.SpSummon, 98127546, UnderworldGoddessoftheClosedWorldSummon_2);
		AddExecutor(ExecutorType.Activate, 38342335, KnightmareUnicornEffect);
		AddExecutor(ExecutorType.SpSummon, 38342335, KnightmareUnicornSummon);
		AddExecutor(ExecutorType.Activate, 21887175, MekkKnightCrusadiaAvramaxEffect);
		AddExecutor(ExecutorType.SpSummon, 21887175, MekkKnightCrusadiaAvramaxSummon);
		AddExecutor(ExecutorType.SpSummon, 84815190, BaronnedeFleurSummon_2);
		AddExecutor(ExecutorType.SpSummon, 84815190, () => base.Bot.HasInMonstersZone(27381364, notDisabled: true, hasXyzMaterial: false, faceUp: true));
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.SpellSet, SpellSet);
	}

	public override bool OnSelectHand()
	{
		return true;
	}

	public override void OnNewTurn()
	{
		List<ClientCard> list = base.Bot.ExtraDeck.Where((ClientCard card) => card != null && card.Id == 69946549).ToList();
		if (_PredaplantDragostapelia == null && list.Count > 0)
		{
			_PredaplantDragostapelia = list.FirstOrDefault();
		}
		if (!key_send_to_deck_ids.Contains(all_key_card_ids[0]))
		{
			key_send_to_deck_ids.AddRange(all_key_card_ids);
		}
		if (!key_remove_ids.Contains(all_key_card_ids[0]))
		{
			key_remove_ids.AddRange(all_key_card_ids);
		}
		if (_PredaplantDragostapelia != null && (_PredaplantDragostapelia.Location != CardLocation.MonsterZone || _PredaplantDragostapelia.IsFacedown()))
		{
			e_PredaplantDragostapelia_cards.Clear();
		}
		foreach (ClientCard item in new List<ClientCard>(e_PredaplantDragostapelia_cards))
		{
			if (item == null || item.Location != CardLocation.MonsterZone || item.IsFacedown())
			{
				e_PredaplantDragostapelia_cards.Remove(item);
			}
		}
		activate_TearlamentsScheiren_1 = false;
		activate_TearlamentsScheiren_2 = false;
		activate_TearlamentsReinoheart_1 = false;
		activate_TearlamentsReinoheart_2 = false;
		activate_TearlamentsHavnis_1 = false;
		activate_TearlamentsHavnis_2 = false;
		activate_TearlamentsMerrli_1 = false;
		activate_TearlamentsMerrli_2 = false;
		activate_PrimevalPlanetPerlereino_1 = false;
		activate_PrimevalPlanetPerlereino_2 = false;
		activate_TearlamentsKitkallos_1 = false;
		activate_TearlamentsKitkallos_2 = false;
		activate_TearlamentsKitkallos_3 = false;
		activate_TearlamentsScream_1 = false;
		activate_TearlamentsScream_2 = false;
		activate_TearlamentsSulliek_1 = false;
		activate_TearlamentsSulliek_2 = false;
		activate_TearlamentsRulkallos_1 = false;
		activate_TearlamentsKaleidoHeart_1 = false;
		activate_TearlamentsKaleidoHeart_2 = false;
		activate_AgidotheAncientSentinel_2 = false;
		activate_KelbektheAncientVanguard_2 = false;
		activate_Eva = false;
		activate_DivineroftheHerald = false;
		summoned = false;
		spsummoned = false;
		summon_SprightElf = false;
		TearlamentsKitkallos_summoned = false;
		base.OnNewTurn();
	}

	private List<ClientCard> GetZoneCards(CardLocation loc, ClientField player)
	{
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		if ((loc & CardLocation.Hand) > (CardLocation)0)
		{
			list2 = player.Hand.Where((ClientCard card) => card != null).ToList();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		if ((loc & CardLocation.MonsterZone) > (CardLocation)0)
		{
			list2 = player.GetMonsters();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		if ((loc & CardLocation.SpellZone) > (CardLocation)0)
		{
			list2 = player.GetSpells();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		if ((loc & CardLocation.Grave) > (CardLocation)0)
		{
			list2 = player.Graveyard.Where((ClientCard card) => card != null).ToList();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		if ((loc & CardLocation.Removed) > (CardLocation)0)
		{
			list2 = player.Banished.Where((ClientCard card) => card != null).ToList();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		if ((loc & CardLocation.Extra) > (CardLocation)0)
		{
			list2 = player.ExtraDeck.Where((ClientCard card) => card != null).ToList();
			if (list2.Count() > 0)
			{
				list.AddRange(list2);
			}
		}
		return list;
	}

	private IList<ClientCard> CardsIdToClientCards(IList<int> cardsId, IList<ClientCard> cardsList, bool uniqueId = true, bool alias = true)
	{
		if ((cardsList != null && cardsList.Count() <= 0) || (cardsId != null && cardsId.Count() <= 0))
		{
			return new List<ClientCard>();
		}
		List<ClientCard> list = new List<ClientCard>();
		foreach (int cardid in cardsId)
		{
			List<ClientCard> list2 = cardsList.Where((ClientCard card) => card != null && (card.Id == cardid || (card.Alias != 0 && cardid == card.Alias && alias))).ToList();
			if (list2 == null || list2.Count > 0)
			{
				list2.Sort(CardContainer.CompareCardAttack);
				if (uniqueId)
				{
					list.Add(list2.First());
				}
				else
				{
					list.AddRange(list2);
				}
			}
		}
		return list.Distinct().ToList();
	}

	private List<ClientCard> GetKeyFusionCard(int key)
	{
		int num = 0;
		if (base.Duel.Player == 0)
		{
			switch (key)
			{
			case 0:
			case 2:
				num = 92731385;
				break;
			case 1:
			case 6:
				num = 84330567;
				break;
			case 3:
				num = 69946549;
				break;
			case 4:
				num = 28226490;
				break;
			case 5:
				num = 69946549;
				break;
			}
		}
		else
		{
			switch (key)
			{
			case 0:
			case 2:
				num = 94977269;
				break;
			case 1:
			case 4:
				num = 92731385;
				break;
			case 3:
				num = 84330567;
				break;
			case 5:
				num = 28226490;
				break;
			case 6:
				num = 69946549;
				break;
			}
		}
		List<ClientCard> list = new List<ClientCard>();
		int num2 = -1;
		for (int i = 0; i < fusionExtra.Count; i++)
		{
			ClientCard clientCard = fusionExtra[i];
			if (clientCard != null && clientCard.Id == num)
			{
				num2 = i;
				list.Add(clientCard);
				break;
			}
		}
		if (num2 > -1 && num2 < fusionExtra.Count)
		{
			fusionExtra.RemoveAt(num2);
		}
		return list;
	}

	private bool IsLastFusionCard()
	{
		int num = 0;
		if (activate_TearlamentsScheiren_2)
		{
			num++;
		}
		if (activate_TearlamentsHavnis_2)
		{
			num++;
		}
		if (activate_TearlamentsMerrli_2)
		{
			num++;
		}
		return num >= 2;
	}

	private bool CheckFusion(int listindex, int id)
	{
		int num = -1;
		if (base.Duel.Player == 0)
		{
			if (fusionExtra.Count > 0)
			{
				bool num2 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 69946549) > 0 && base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true);
				bool flag = ((base.Duel.Phase < DuelPhase.End && (!activate_TearlamentsKitkallos_1 || !activate_TearlamentsKitkallos_2)) || (base.Duel.Phase == DuelPhase.End && !activate_TearlamentsKitkallos_1 && !activate_TearlamentsKitkallos_2)) && fusionExtra.Count((ClientCard card) => card != null && card.Id == 92731385) > 0;
				bool flag2 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 84330567) > 0 && (base.Bot.HasInGraveyard(92731385) || base.Bot.GetMonsters().Count((ClientCard card) => card != null && card.Id == 92731385) > 1 || base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && card != base.Card && (card.Id == 572850 || card.Id == 37961969 || card.Id == 74078255)));
				bool flag3 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 28226490) > 0 && IsShouldSummonFusion(-1, -1, all: false, 4);
				bool flag4 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 69946549) > 0 && IsShouldSummonFusion(-1, -1, all: false, 8);
				bool flag5 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 84330567) > 0 && IsShouldSummonFusion(-1, -1, all: false, 2) && IsLastFusionCard();
				bool flag6 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 92731385) > 0;
				if (num2)
				{
					num = 3;
				}
				else if (flag)
				{
					num = 0;
				}
				else if (flag2)
				{
					num = 1;
				}
				else if (flag3)
				{
					num = 4;
				}
				else if (flag4)
				{
					num = 5;
				}
				else if (flag5)
				{
					num = 6;
				}
				else if (flag6)
				{
					num = 2;
				}
				if (num > -1)
				{
					List<ClientCard> list = new List<ClientCard>(fusionMaterial);
					list.Remove(base.Card);
					switch (num)
					{
					case 0:
					case 2:
						list = list.Where((ClientCard card) => card != null && (card.HasRace(CardRace.Aqua) || (card.HasType(CardType.Monster) && card.HasSetcode(SETCODE)))).ToList();
						break;
					case 1:
					case 6:
						list = list.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
						break;
					case 3:
						list = list.Where((ClientCard card) => card != null && card.Id == 94977269).ToList();
						break;
					case 4:
						list = list.Where((ClientCard card) => card != null && card.HasType(CardType.Monster) && card.HasSetcode(SETCODE)).ToList();
						break;
					case 5:
						list = list.Where((ClientCard card) => card != null && (card.Id != 84330567 || card.IsDisabled()) && card.HasType(CardType.Fusion)).ToList();
						break;
					default:
						return false;
					}
					list = list.Where((ClientCard card) => card != null && (card.HasRace(CardRace.Aqua) || (card.HasType(CardType.Monster) && card.HasSetcode(SETCODE)))).ToList();
					int num3 = on_chaining_cards.Count((ClientCard card) => card != null && (card.Id == 62320425 || card.Id == 25926710));
					int num4 = on_chaining_cards.Count((ClientCard card) => card != null && card.Id == 74920585);
					List<ClientCard> list2 = new List<ClientCard>(base.Duel.CurrentChain);
					IList<ClientCard> list3 = new List<ClientCard>();
					foreach (ClientCard item in list2)
					{
						if (item != null && item.Controller != 1 && ((item.Id == 74078255 && !HasInList(list3, 74078255)) || (item.Id == 37961969 && !HasInList(list3, 37961969)) || (item.Id == 572850 && !HasInList(list3, 572850))))
						{
							list3.Add(item);
						}
					}
					if ((list.Count <= 0 && num3 <= 0 && num4 <= 0) || list3.Count() >= 2)
					{
						if (!remainCards.Contains(base.Card))
						{
							remainCards.Add(base.Card);
						}
						return false;
					}
					switch (id)
					{
					case 572850:
						activate_TearlamentsScheiren_2 = true;
						break;
					case 37961969:
						activate_TearlamentsHavnis_2 = true;
						break;
					case 74078255:
						activate_TearlamentsMerrli_2 = true;
						break;
					}
					if (num3 > 0 || num4 > 0)
					{
						ran_fusion_mode_0[listindex] = true;
						ran_fusion_mode_1[listindex] = true;
						ran_fusion_mode_2[listindex] = true;
						return true;
					}
					list.Sort(CardContainer.CompareCardAttack);
					list = GetDefaultMaterial(list);
					List<ClientCard> keyFusionCard = GetKeyFusionCard(num);
					mcard_0[listindex] = keyFusionCard.ElementAtOrDefault(0);
					mcard_1[listindex] = base.Card;
					mcard_2[listindex] = list.ElementAtOrDefault(0);
					if (keyFusionCard.Any((ClientCard card) => card != null && card.Id == 28226490))
					{
						if (list.Count <= 1)
						{
							mcard_0[listindex] = null;
							mcard_1[listindex] = null;
							mcard_2[listindex] = null;
							ran_fusion_mode_0[listindex] = true;
							ran_fusion_mode_1[listindex] = true;
							ran_fusion_mode_2[listindex] = true;
							return true;
						}
						if (mcard_2[listindex] != null && mcard_2[listindex].Id == 73956664)
						{
							mcard_3[listindex] = list.ElementAtOrDefault(1);
						}
						else
						{
							List<ClientCard> list4 = list.Where((ClientCard card) => card != null && card.Id == 73956664).ToList();
							if (list4.Count <= 0)
							{
								mcard_0[listindex] = null;
								mcard_1[listindex] = null;
								mcard_2[listindex] = null;
								ran_fusion_mode_0[listindex] = true;
								ran_fusion_mode_1[listindex] = true;
								ran_fusion_mode_2[listindex] = true;
								return true;
							}
							mcard_3[listindex] = list4.ElementAtOrDefault(0);
						}
					}
					if (mcard_1[listindex] != null)
					{
						fusionMaterial.Remove(mcard_1[listindex]);
					}
					if (mcard_2[listindex] != null)
					{
						fusionMaterial.Remove(mcard_2[listindex]);
					}
					if (mcard_3[listindex] != null)
					{
						fusionMaterial.Remove(mcard_3[listindex]);
					}
					return true;
				}
			}
			return false;
		}
		if (fusionExtra.Count > 0)
		{
			bool num5 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 94977269) > 0 && (fusionExtra.Count((ClientCard card) => card != null && card.Id == 92731385) <= 0 || activate_TearlamentsKitkallos_1 || base.Bot.GetMonstersInMainZone().Count >= 4 || TearlamentsKitkallos_summoned) && base.Bot.GetGraveyardMonsters().Any((ClientCard card) => card?.HasSetcode(157) ?? false) && base.Card.HasAttribute(CardAttribute.Dark);
			bool flag7 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 92731385) > 0 && !activate_TearlamentsKitkallos_1;
			bool flag8 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 94977269) > 0 && base.Bot.GetGraveyardMonsters().Any((ClientCard card) => card?.HasSetcode(157) ?? false) && base.Card.HasAttribute(CardAttribute.Dark);
			bool flag9 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 84330567) > 0 && (base.Bot.HasInGraveyard(92731385) || base.Bot.HasInMonstersZone(92731385, notDisabled: false, hasXyzMaterial: false, faceUp: true));
			bool flag10 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 28226490) > 0 && !activate_TearlamentsKaleidoHeart_1 && GetZoneCards(CardLocation.Onfield, base.Enemy).Count((ClientCard card) => card != null && !card.IsShouldNotBeTarget()) > 0 && IsShouldSummonFusion(-1, -1, all: false, 4) && (!base.Bot.HasInSpellZone(77103950, notDisabled: true, faceUp: true) || (base.Bot.HasInSpellZone(77103950, notDisabled: true, faceUp: true) && activate_PrimevalPlanetPerlereino_2));
			bool flag11 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 69946549) > 0 && IsShouldSummonFusion(-1, -1, all: false, 8);
			bool flag12 = fusionExtra.Count((ClientCard card) => card != null && card.Id == 92731385) > 0;
			if (num5)
			{
				num = 0;
			}
			else if (flag7)
			{
				num = 1;
			}
			else if (flag8)
			{
				num = 2;
			}
			else if (flag9)
			{
				num = 3;
			}
			else if (flag10)
			{
				num = 5;
			}
			else if (flag11)
			{
				num = 6;
			}
			else if (flag12)
			{
				num = 4;
			}
			if (num > -1)
			{
				List<ClientCard> list5 = new List<ClientCard>(fusionMaterial);
				list5.Remove(base.Card);
				switch (num)
				{
				case 0:
				case 2:
					list5 = list5.Where((ClientCard card) => card != null && card.HasType(CardType.Monster) && card.HasSetcode(157)).ToList();
					break;
				case 1:
				case 4:
					list5 = list5.Where((ClientCard card) => card != null && (card.HasRace(CardRace.Aqua) || (card.HasType(CardType.Monster) && card.HasSetcode(SETCODE)))).ToList();
					break;
				case 3:
					list5 = list5.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
					break;
				case 5:
					list5 = list5.Where((ClientCard card) => card != null && card.HasType(CardType.Monster) && card.HasSetcode(SETCODE)).ToList();
					break;
				case 6:
					list5 = list5.Where((ClientCard card) => card != null && (card.Id != 84330567 || card.IsDisabled()) && card.HasType(CardType.Fusion)).ToList();
					break;
				default:
					return false;
				}
				int num6 = on_chaining_cards.Count((ClientCard card) => card != null && (card.Id == 62320425 || card.Id == 25926710));
				int num7 = on_chaining_cards.Count((ClientCard card) => card != null && card.Id == 74920585);
				if (list5.Count <= 0 && num6 <= 0 && num7 <= 0)
				{
					if (!remainCards.Contains(base.Card))
					{
						remainCards.Add(base.Card);
					}
					return false;
				}
				switch (id)
				{
				case 572850:
					activate_TearlamentsScheiren_2 = true;
					break;
				case 37961969:
					activate_TearlamentsHavnis_2 = true;
					break;
				case 74078255:
					activate_TearlamentsMerrli_2 = true;
					break;
				}
				if (num6 > 0 || num7 > 0)
				{
					ran_fusion_mode_0[listindex] = true;
					ran_fusion_mode_1[listindex] = true;
					ran_fusion_mode_2[listindex] = true;
					return true;
				}
				list5.Sort(CardContainer.CompareCardAttack);
				list5 = GetDefaultMaterial(list5);
				List<ClientCard> keyFusionCard2 = GetKeyFusionCard(num);
				mcard_0[listindex] = keyFusionCard2.ElementAtOrDefault(0);
				mcard_1[listindex] = base.Card;
				mcard_2[listindex] = list5.ElementAtOrDefault(0);
				if (keyFusionCard2.Any((ClientCard card) => card != null && card.Id == 28226490))
				{
					if (list5.Count <= 1)
					{
						mcard_0[listindex] = null;
						mcard_1[listindex] = null;
						mcard_2[listindex] = null;
						ran_fusion_mode_0[listindex] = true;
						ran_fusion_mode_1[listindex] = true;
						ran_fusion_mode_2[listindex] = true;
						return true;
					}
					if (mcard_2[listindex] != null && mcard_2[listindex].Id == 73956664)
					{
						mcard_3[listindex] = list5.ElementAtOrDefault(1);
					}
					else
					{
						List<ClientCard> list6 = list5.Where((ClientCard card) => card != null && card.Id == 73956664).ToList();
						if (list6.Count <= 0)
						{
							mcard_0[listindex] = null;
							mcard_1[listindex] = null;
							mcard_2[listindex] = null;
							ran_fusion_mode_0[listindex] = true;
							ran_fusion_mode_1[listindex] = true;
							ran_fusion_mode_2[listindex] = true;
							return true;
						}
						mcard_3[listindex] = list6.ElementAtOrDefault(0);
					}
				}
				if (mcard_1[listindex] != null)
				{
					fusionMaterial.Remove(mcard_1[listindex]);
				}
				if (mcard_2[listindex] != null)
				{
					fusionMaterial.Remove(mcard_2[listindex]);
				}
				if (mcard_3[listindex] != null)
				{
					fusionMaterial.Remove(mcard_3[listindex]);
				}
				if (listindex > 0 && mcard_0[0] != null && mcard_0[listindex] != null && mcard_0[listindex].Id == 94977269)
				{
					ClientCard value = mcard_0[0];
					mcard_0[0] = mcard_0[listindex];
					mcard_0[listindex] = value;
				}
				return true;
			}
		}
		return false;
	}

	private bool FusionEffect(int id)
	{
		if (!chainlist)
		{
			chainlist = true;
			fusionExtra = base.Bot.ExtraDeck.Where((ClientCard fcard) => fcard != null).ToList();
			fusionMaterial = (from mcard in GetZoneCards(CardLocation.MonsterZone, base.Bot)
				where mcard?.IsFaceup() ?? false
				select mcard).ToList();
			fusionMaterial.AddRange(GetZoneCards((CardLocation)18, base.Bot));
		}
		int listindex = 0;
		switch (id)
		{
		case 572850:
			listindex = 0;
			break;
		case 37961969:
			listindex = 1;
			break;
		case 74078255:
			listindex = 2;
			break;
		}
		if (!CheckFusion(listindex, id))
		{
			return false;
		}
		SetSpSummon();
		return true;
	}

	public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		NamedCard namedCard = NamedCard.Get(cardId);
		if (base.Duel.Turn > 1 && base.Enemy.GetMonsterCount() <= 0 && (namedCard.Attack > 0 || cardId == 33158448) && base.Duel.Player == 0)
		{
			return CardPosition.FaceUpAttack;
		}
		if (base.Duel.Player == 1)
		{
			if (namedCard.Attack < 2000)
			{
				return CardPosition.FaceUpDefence;
			}
			if (base.Util.GetBestAttack(base.Enemy) > namedCard.Attack)
			{
				return CardPosition.FaceUpDefence;
			}
			return CardPosition.FaceUpAttack;
		}
		if (namedCard.Attack <= 1000)
		{
			return CardPosition.FaceUpDefence;
		}
		return base.OnSelectPosition(cardId, positions);
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone)
		{
			if (cardId == 27381364)
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
			if (cardId == 84330567 && base.Bot.HasInExtra(27381364))
			{
				if ((4 & available) > 0)
				{
					return 4;
				}
				if ((1 & available) > 0)
				{
					return 1;
				}
			}
			ClientCard clientCard = base.Bot.MonsterZone[5];
			if (clientCard != null && clientCard.Id == 27381364)
			{
				if ((1 & available) > 0)
				{
					return 1;
				}
				if ((4 & available) > 0)
				{
					return 4;
				}
			}
			clientCard = base.Bot.MonsterZone[6];
			if (clientCard != null && clientCard.Id == 27381364)
			{
				if ((0x10 & available) > 0)
				{
					return 16;
				}
				if ((4 & available) > 0)
				{
					return 4;
				}
			}
		}
		if (player == 0 && location == CardLocation.SpellZone && location != CardLocation.FieldZone)
		{
			List<int> list = new List<int> { 0, 1, 2, 3, 4 };
			while (list.Count > 0)
			{
				int index = Program.Rand.Next(list.Count);
				int num = list[index];
				int num2 = 1 << num;
				if ((num2 & available) > 0)
				{
					return num2;
				}
				list.Remove(num);
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override int OnSelectOption(IList<int> options)
	{
		if (options.Count == 2 && options.Contains(1190))
		{
			if (!TearlamentsKitkallostohand)
			{
				return 1;
			}
			return 0;
		}
		return base.OnSelectOption(options);
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == 1233663200)
		{
			pre_activate_PrimevalPlanetPerlereino = true;
		}
		return base.OnSelectYesNo(desc);
	}

	public override void OnSelectChain(IList<ClientCard> cards)
	{
		if (on_chaining_cards.Count <= 0 && cards.Count > 0)
		{
			on_chaining_cards = new List<ClientCard>(cards);
		}
		base.OnSelectChain(cards);
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (!chainlist)
		{
			chainlist = true;
			remainCards.Clear();
			fusionExtra = base.Bot.ExtraDeck.Where((ClientCard fcard) => fcard != null).ToList();
			fusionMaterial = (from mcard in GetZoneCards(CardLocation.MonsterZone, base.Bot)
				where mcard?.IsFaceup() ?? false
				select mcard).ToList();
			fusionMaterial.AddRange(GetZoneCards((CardLocation)18, base.Bot));
		}
		base.OnChaining(player, card);
	}

	public override void OnChainEnd()
	{
		remainCards.Clear();
		fusionExtra.Clear();
		fusionMaterial.Clear();
		on_chaining_cards.Clear();
		tgcard = null;
		no_fusion_card = null;
		for (int i = 0; i < mcard_0.Count; i++)
		{
			mcard_0[i] = null;
		}
		for (int j = 0; j < mcard_1.Count; j++)
		{
			mcard_1[j] = null;
		}
		for (int k = 0; k < mcard_2.Count; k++)
		{
			mcard_2[k] = null;
		}
		for (int l = 0; l < mcard_3.Count; l++)
		{
			mcard_3[l] = null;
		}
		for (int m = 0; m < ran_fusion_mode_0.Count; m++)
		{
			ran_fusion_mode_0[m] = false;
		}
		for (int n = 0; n < ran_fusion_mode_1.Count; n++)
		{
			ran_fusion_mode_1[n] = false;
		}
		for (int num = 0; num < ran_fusion_mode_2.Count; num++)
		{
			ran_fusion_mode_2[num] = false;
		}
		chainlist = false;
		base.OnChainEnd();
	}

	private bool IsAvailableZone(int seq)
	{
		ClientCard clientCard = base.Bot.MonsterZone[seq];
		if (seq == 5 || seq == 6)
		{
			ClientCard clientCard2 = base.Bot.MonsterZone[5];
			ClientCard clientCard3 = base.Bot.MonsterZone[6];
			if (clientCard2 != null && clientCard2.Controller == 0 && no_link_ids.Contains(clientCard2.Id))
			{
				return false;
			}
			if (clientCard3 != null && clientCard3.Controller == 0 && no_link_ids.Contains(clientCard3.Id))
			{
				return false;
			}
		}
		if (clientCard == null)
		{
			return true;
		}
		if (clientCard.Controller != 0)
		{
			return false;
		}
		if (clientCard.IsFacedown())
		{
			return false;
		}
		if (clientCard.IsDisabled())
		{
			return true;
		}
		if (clientCard.Id == 27381364 && summon_SprightElf)
		{
			return false;
		}
		if (no_link_ids.Contains(clientCard.Id))
		{
			return false;
		}
		return true;
	}

	private bool IsAvailableLinkZone()
	{
		int num = 0;
		foreach (ClientCard item in (from card in base.Bot.GetMonstersInMainZone()
			where card?.IsFaceup() ?? false
			select card).ToList())
		{
			num |= item.GetLinkedZones();
		}
		ClientCard clientCard = base.Bot.MonsterZone[5];
		if (clientCard != null && clientCard.IsFaceup() && clientCard.HasType(CardType.Link))
		{
			if (clientCard.Controller == 0)
			{
				if (clientCard.HasLinkMarker(CardLinkMarker.BottomLeft))
				{
					num |= 1;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.Bottom))
				{
					num |= 2;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.BottomRight))
				{
					num |= 4;
				}
			}
			if (clientCard.Controller == 1)
			{
				if (clientCard.HasLinkMarker(CardLinkMarker.TopLeft))
				{
					num |= 4;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.Top))
				{
					num |= 2;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.TopRight))
				{
					num |= 1;
				}
			}
		}
		clientCard = base.Bot.MonsterZone[6];
		if (clientCard != null && clientCard.IsFaceup() && clientCard.HasType(CardType.Link))
		{
			if (clientCard.Controller == 0)
			{
				if (clientCard.HasLinkMarker(CardLinkMarker.BottomLeft))
				{
					num |= 4;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.Bottom))
				{
					num |= 8;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.BottomRight))
				{
					num |= 0x10;
				}
			}
			if (clientCard.Controller == 1)
			{
				if (clientCard.HasLinkMarker(CardLinkMarker.TopLeft))
				{
					num |= 0x10;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.Top))
				{
					num |= 8;
				}
				if (clientCard.HasLinkMarker(CardLinkMarker.TopRight))
				{
					num |= 4;
				}
			}
		}
		num &= 0x7F;
		link_card = null;
		if ((num & 1) > 0 && IsAvailableZone(0))
		{
			return GetZoneLinkCards(0);
		}
		if ((num & 2) > 0 && IsAvailableZone(1))
		{
			return GetZoneLinkCards(1);
		}
		if ((num & 4) > 0 && IsAvailableZone(2))
		{
			return GetZoneLinkCards(2);
		}
		if ((num & 8) > 0 && IsAvailableZone(3))
		{
			return GetZoneLinkCards(3);
		}
		if ((num & 0x10) > 0 && IsAvailableZone(4))
		{
			return GetZoneLinkCards(4);
		}
		if (IsAvailableZone(5))
		{
			return GetZoneLinkCards(5);
		}
		if (IsAvailableZone(6))
		{
			return GetZoneLinkCards(6);
		}
		return false;
	}

	private bool GetZoneLinkCards(int index)
	{
		if (index >= base.Bot.MonsterZone.Count())
		{
			index = 0;
		}
		link_card = base.Bot.MonsterZone[index];
		return true;
	}

	private List<ClientCard> GetDefaultMaterial(IList<ClientCard> cards)
	{
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		List<ClientCard> list3 = new List<ClientCard>();
		List<ClientCard> list4 = new List<ClientCard>();
		List<ClientCard> list5 = new List<ClientCard>();
		List<ClientCard> list6 = new List<ClientCard>();
		List<ClientCard> list7 = new List<ClientCard>();
		foreach (ClientCard item in base.Duel.CurrentChain)
		{
			if (item != null && ((item.Id == 572850 && !HasInList(list6, 572850)) || (item.Id == 74078255 && !HasInList(list6, 74078255)) || (item.Id == 37961969 && !HasInList(list6, 37961969))) && cards.Contains(item))
			{
				list6.Add(item);
			}
		}
		foreach (ClientCard remainCard in remainCards)
		{
			if (remainCard != null && !cards.Contains(remainCard))
			{
				list.Add(remainCard);
			}
		}
		foreach (ClientCard card in cards)
		{
			if (card == null || list2.Contains(card) || list.Contains(card) || list6.Contains(card) || list3.Contains(card) || list4.Contains(card) || list5.Contains(card))
			{
				continue;
			}
			if (card.Id == 94977269 && card.IsFaceup() && card.Location == CardLocation.MonsterZone && !card.IsDisabled())
			{
				list.Add(card);
			}
			else if (card.Id != 84330567 && list2.Count() <= 0 && card.Location == CardLocation.MonsterZone && base.Bot.GetMonstersInMainZone().Count > 4)
			{
				list2.Add(card);
			}
			else if (card.Location == CardLocation.Grave)
			{
				if ((((card.Id == 572850 && !activate_TearlamentsScheiren_2) || (card.Id == 74078255 && !activate_TearlamentsMerrli_2) || (card.Id == 37961969 && !activate_TearlamentsHavnis_2)) && list6.Count((ClientCard mcard) => mcard != null && mcard.Id == card.Id) <= 0 && on_chaining_cards.Count((ClientCard mcard) => mcard != null && mcard.Id == card.Id) > 0) || (on_chaining_cards.Count((ClientCard ccard) => ccard != null && ccard == card) > 0 && on_chaining_cards.Count((ClientCard cccard) => cccard != null && cccard.Id == card.Id) <= 1 && list6.Count((ClientCard mcard) => mcard != null && mcard.Id == card.Id) <= 0 && base.Duel.CurrentChain.Count((ClientCard cccard) => cccard != null && cccard.Id == card.Id) > 0) || (no_fusion_card != null && no_fusion_card == card))
				{
					list6.Add(card);
				}
				else
				{
					list3.Add(card);
				}
			}
			else if (card.Location == CardLocation.Hand)
			{
				if (card.Id == 73956664)
				{
					list5.Insert(0, card);
				}
				else if (base.Duel.Player == 1 && card.Id == 37961969 && !activate_TearlamentsHavnis_1)
				{
					list6.Add(card);
				}
				else
				{
					list5.Add(card);
				}
			}
			else if (card.Location == CardLocation.MonsterZone)
			{
				if (card.Id == 92731385 && card.IsDisabled())
				{
					list6.Add(card);
				}
				else if (card.Id == 92731385)
				{
					list7.Insert(0, card);
				}
				else if (card.Id == 84330567 || card.Id == 28226490 || card.Id == 69946549 || (base.Duel.Player == 1 && card.Id == 94977269))
				{
					list7.Add(card);
				}
				else
				{
					list4.Add(card);
				}
			}
			else
			{
				list4.Add(card);
			}
		}
		list.AddRange(list2);
		list.AddRange(list3);
		list.AddRange(list5);
		list.AddRange(list4);
		list.AddRange(list6);
		list.AddRange(list7);
		return list;
	}

	public override IList<ClientCard> OnSelectFusionMaterial(IList<ClientCard> cards, int min, int max)
	{
		if (base.AI.HaveSelectedCards())
		{
			return null;
		}
		for (int num = 2; num >= 0; num--)
		{
			List<ClientCard> list = cards.Where((ClientCard card) => card != null && card.Location != CardLocation.MonsterZone && card.Id == 73956664).ToList();
			if (ran_fusion_mode_1[num] || ran_fusion_mode_2[num] || ran_fusion_mode_3[num])
			{
				if (ran_fusion_mode_1[num])
				{
					ran_fusion_mode_1[num] = false;
				}
				else if (ran_fusion_mode_2[num])
				{
					ran_fusion_mode_2[num] = false;
				}
				else if (ran_fusion_mode_3[num])
				{
					ran_fusion_mode_3[num] = false;
				}
				cards.ToList().Sort(CardContainer.CompareCardAttack);
				if (list.Count <= 0)
				{
					return base.Util.CheckSelectCount(GetDefaultMaterial(cards), cards, min, max);
				}
				return base.Util.CheckSelectCount(list, cards, min, max);
			}
			List<ClientCard> list2 = null;
			if (mcard_1[num] != null)
			{
				if (list.Count() > 0)
				{
					mcard_1[num] = null;
					return base.Util.CheckSelectCount(list, cards, min, max);
				}
				mcard_1[num] = null;
				List<ClientCard> list3 = new List<ClientCard>(cards);
				list3.Sort(CardContainer.CompareCardAttack);
				list2 = GetDefaultMaterial(list3);
				return base.Util.CheckSelectCount(list2, cards, min, max);
			}
			if (mcard_2[num] != null)
			{
				mcard_2[num] = null;
				List<ClientCard> list4 = new List<ClientCard>(cards);
				list4.Sort(CardContainer.CompareCardAttack);
				list2 = GetDefaultMaterial(list4);
				return base.Util.CheckSelectCount(list2, cards, min, max);
			}
			if (mcard_3[num] != null)
			{
				mcard_3[num] = null;
				List<ClientCard> list5 = new List<ClientCard>(cards);
				list5.Sort(CardContainer.CompareCardAttack);
				list2 = GetDefaultMaterial(list5);
				return base.Util.CheckSelectCount(list2, cards, min, max);
			}
		}
		return base.OnSelectFusionMaterial(cards, min, max);
	}

	private bool HasInList(IList<ClientCard> cards, int id)
	{
		if (cards == null || cards.Count <= 0)
		{
			return false;
		}
		return cards.Any((ClientCard card) => card != null && card.Id == id);
	}

	private bool IsCanSpSummon()
	{
		if ((base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true) || base.Enemy.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true)) && spsummoned)
		{
			return false;
		}
		return true;
	}

	private void SetSpSummon()
	{
		if (base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true) || base.Enemy.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			spsummoned = true;
		}
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		if ((base.AI.HaveSelectedCards() && mcard_0.All((ClientCard card) => card == null) && ran_fusion_mode_0.All((bool flag) => !flag)) || hint == 511)
		{
			return null;
		}
		if (pre_activate_PrimevalPlanetPerlereino)
		{
			pre_activate_PrimevalPlanetPerlereino = false;
			List<int> cardsIdSendToHand = GetCardsIdSendToHand();
			return base.Util.CheckSelectCount(CardsIdToClientCards(cardsIdSendToHand, cards, uniqueId: false), cards, min, max);
		}
		switch (hint)
		{
		case 574:
		{
			IList<int> list3 = new List<int>();
			IList<ClientCard> list4 = new List<ClientCard>();
			if (base.Duel.Player == 0)
			{
				if (!activate_TearlamentsScheiren_1 && !base.Bot.HasInHand(572850) && HasInList(cards, 572850) && !AllActivated())
				{
					list3.Add(572850);
				}
				if (!activate_TearlamentsMerrli_1 && !base.Bot.HasInHand(74078255) && HasInList(cards, 74078255) && !AllActivated())
				{
					list3.Add(74078255);
				}
				if (!base.Bot.HasInHand(37961969) && HasInList(cards, 37961969))
				{
					list3.Add(37961969);
				}
				if (!activate_TearlamentsReinoheart_1 && !base.Bot.HasInHand(73956664) && HasInList(cards, 73956664))
				{
					list3.Add(73956664);
				}
				if (list3.Count() <= 0 && !activate_TearlamentsKitkallos_2 && (from card in GetZoneCards((CardLocation)18, base.Bot)
					where card != null && card.HasSetcode(SETCODE) && card.HasType(CardType.Monster)
					select card).ToList().Count() <= 0)
				{
					if (HasInList(cards, 572850))
					{
						list3.Add(572850);
					}
					if (HasInList(cards, 74078255))
					{
						list3.Add(74078255);
					}
					if (HasInList(cards, 73956664))
					{
						list3.Add(73956664);
					}
					if (HasInList(cards, 37961969))
					{
						list3.Add(37961969);
					}
				}
				if (HasInList(cards, 6767771) && !activate_TearlamentsScream_1)
				{
					list3.Add(6767771);
				}
				if (HasInList(cards, 74920585) && on_chaining_cards.Count((ClientCard card) => card != null && card.Id == 6767771) <= 0)
				{
					list3.Add(74920585);
				}
				if (HasInList(cards, 77103950) && !activate_PrimevalPlanetPerlereino_1)
				{
					list3.Add(77103950);
				}
				if (list3.Count > 0)
				{
					TearlamentsKitkallostohand = true;
				}
				else if (IsShouldSummonFusion(SETCODE, 64))
				{
					if (!activate_TearlamentsHavnis_2 && HasInList(cards, 37961969))
					{
						list3.Add(37961969);
					}
					if (!activate_TearlamentsScheiren_2 && HasInList(cards, 572850))
					{
						list3.Add(572850);
					}
					if (!activate_TearlamentsMerrli_2 && HasInList(cards, 74078255))
					{
						list3.Add(74078255);
					}
					if (list3.Count > 0)
					{
						TearlamentsKitkallostohand = false;
					}
				}
			}
			else
			{
				if (IsShouldSummonFusion(SETCODE, 64, all: true))
				{
					if (!activate_TearlamentsHavnis_2 && HasInList(cards, 37961969) && !base.Bot.HasInHand(37961969))
					{
						list3.Add(37961969);
					}
					if (!activate_TearlamentsScheiren_2 && HasInList(cards, 572850) && !base.Bot.HasInHand(572850))
					{
						list3.Add(572850);
					}
					if (!activate_TearlamentsMerrli_2 && HasInList(cards, 74078255) && !base.Bot.HasInHand(74078255))
					{
						list3.Add(74078255);
					}
					if (!activate_TearlamentsHavnis_2 && HasInList(cards, 37961969) && !list3.Contains(37961969))
					{
						list3.Add(37961969);
					}
					if (!activate_TearlamentsScheiren_2 && HasInList(cards, 572850) && !list3.Contains(572850))
					{
						list3.Add(572850);
					}
					if (!activate_TearlamentsMerrli_2 && HasInList(cards, 74078255) && !list3.Contains(74078255))
					{
						list3.Add(74078255);
					}
				}
				if (list3.Count > 0)
				{
					TearlamentsKitkallostohand = false;
				}
				else if (!base.Bot.HasInHand(37961969) && !activate_TearlamentsHavnis_1 && HasInList(cards, 37961969))
				{
					list3.Add(37961969);
					TearlamentsKitkallostohand = true;
				}
				else if (!activate_TearlamentsReinoheart_2 && HasInList(cards, 73956664) && base.Bot.Hand.Any((ClientCard card) => card?.HasSetcode(SETCODE) ?? false))
				{
					list3.Add(73956664);
					TearlamentsKitkallostohand = false;
				}
				else if (!activate_TearlamentsSulliek_2 && HasInList(cards, 74920585))
				{
					list3.Add(74920585);
					TearlamentsKitkallostohand = false;
				}
				else
				{
					if (!base.Bot.HasInHand(77103950) && HasInList(cards, 77103950))
					{
						list3.Add(77103950);
					}
					if (!base.Bot.HasInHand(572850) && HasInList(cards, 572850))
					{
						list3.Add(572850);
					}
					if (!base.Bot.HasInHand(74078255) && HasInList(cards, 74078255))
					{
						list3.Add(74078255);
					}
					if (!base.Bot.HasInHand(73956664) && HasInList(cards, 73956664))
					{
						list3.Add(73956664);
					}
					if (!base.Bot.HasInHand(6767771) && HasInList(cards, 6767771))
					{
						list3.Add(6767771);
					}
					if (!base.Bot.HasInHand(74920585) && HasInList(cards, 74920585))
					{
						list3.Add(74920585);
					}
					TearlamentsKitkallostohand = true;
				}
			}
			list4 = CardsIdToClientCards(list3, cards, uniqueId: false);
			if (list4.Count <= 0 || (base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true) && spsummoned))
			{
				TearlamentsKitkallostohand = true;
			}
			if (list4.Count <= 0)
			{
				return null;
			}
			return base.Util.CheckSelectCount(list4, cards, min, max);
		}
		case 503:
		{
			if (!cards.Any((ClientCard card) => card != null && ((card.HasAttribute(CardAttribute.Light) && card.HasRace(CardRace.Fairy)) || card.Controller == 1)))
			{
				break;
			}
			IList<ClientCard> list = CardsIdToClientCards(new List<int> { 21074344, 17266660, 92919429 }, cards.Where((ClientCard card) => card != null && card.Location == CardLocation.Grave).ToList(), uniqueId: false);
			List<ClientCard> list2 = cards.Where((ClientCard card) => card != null && card.Controller == 1 && !key_no_remove_ids.Contains(card.Id)).ToList();
			list2.Sort(CardContainer.CompareCardAttack);
			list2.Reverse();
			int num = ((list2.Count > max) ? max : ((list2.Count <= 0) ? min : list2.Count));
			int num2 = ((list.Count > max) ? max : ((list.Count <= 0) ? min : list.Count));
			if (list2.Count <= 0)
			{
				if (list.Count <= 0)
				{
					return null;
				}
				return base.Util.CheckSelectCount(list, cards, num2, num2);
			}
			return base.Util.CheckSelectCount(list2, cards, num, num);
		}
		}
		if (hint == 506 && cards.Any((ClientCard card) => card != null && card.Location == CardLocation.Deck))
		{
			List<int> list5 = new List<int> { 17266660, 21074344, 92919429 };
			if (!base.Bot.HasInSpellZoneOrInGraveyard(92919429) && (base.Bot.HasInHand(17266660) || base.Bot.HasInHand(21074344)))
			{
				list5.Clear();
				list5.AddRange(new List<int> { 92919429, 17266660, 21074344 });
			}
			list5.AddRange(GetCardsIdSendToHand().Distinct());
			IList<ClientCard> list6 = CardsIdToClientCards(list5, cards, uniqueId: false);
			if (list6.Count <= 0)
			{
				return null;
			}
			return base.Util.CheckSelectCount(list6, cards, max, max);
		}
		if (hint == 504 && cards.Any((ClientCard card) => card != null && card.Location == CardLocation.MonsterZone) && min == 1 && max == 1)
		{
			List<ClientCard> list7 = new List<ClientCard>();
			List<ClientCard> list8 = new List<ClientCard>();
			List<ClientCard> list9 = new List<ClientCard>();
			foreach (ClientCard card in cards)
			{
				if (card != null)
				{
					if (card.Location == CardLocation.Hand)
					{
						list7.Add(card);
					}
					else if (card.Location == CardLocation.SpellZone)
					{
						list9.Add(card);
					}
					else
					{
						list8.Add(card);
					}
				}
			}
			List<ClientCard> list10 = new List<ClientCard>();
			List<ClientCard> list11 = new List<ClientCard>();
			list10 = cards.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
			if (!activate_TearlamentsKitkallos_3 && list10.Count > 0 && !AllActivated() && IsShouldSummonFusion(SETCODE, 64) && ((CheckRemainInDeck(37961969) > 0 && !activate_TearlamentsHavnis_2) || (CheckRemainInDeck(74078255) > 0 && !activate_TearlamentsMerrli_2) || (CheckRemainInDeck(572850) > 0 && !activate_TearlamentsScheiren_2)))
			{
				return base.Util.CheckSelectCount(list10, cards, min, max);
			}
			if (IsShouldSummonFusion())
			{
				if (!activate_TearlamentsScheiren_2 && HasInList(cards, 572850))
				{
					list11 = cards.Where((ClientCard card) => card != null && card.Id == 572850 && card.Location == CardLocation.Hand).ToList();
					list10 = cards.Where((ClientCard card) => card != null && card.Id == 572850 && card.Location == CardLocation.MonsterZone).ToList();
				}
				else if (!activate_TearlamentsHavnis_2 && HasInList(cards, 37961969))
				{
					list11 = cards.Where((ClientCard card) => card != null && card.Id == 37961969 && card.Location == CardLocation.Hand).ToList();
					list10 = cards.Where((ClientCard card) => card != null && card.Id == 37961969 && card.Location == CardLocation.MonsterZone).ToList();
				}
				else if (!activate_TearlamentsMerrli_2 && HasInList(cards, 74078255))
				{
					list11 = cards.Where((ClientCard card) => card != null && card.Id == 74078255 && card.Location == CardLocation.Hand).ToList();
					list10 = cards.Where((ClientCard card) => card != null && card.Id == 74078255 && card.Location == CardLocation.MonsterZone).ToList();
				}
				else if (!activate_TearlamentsReinoheart_2 && (HasinZoneKeyCard(CardLocation.Hand) || (base.Bot.Hand.Count((ClientCard ccard) => ccard != null && ccard.HasSetcode(SETCODE) && ccard != cards.Where((ClientCard card) => card != null && card.Location == CardLocation.Hand && card.Id == 73956664).FirstOrDefault()) > 0 && HasinZoneKeyCard(CardLocation.Deck))))
				{
					list11 = cards.Where((ClientCard card) => card != null && card.Id == 73956664 && card.Location == CardLocation.Hand).ToList();
					list10 = cards.Where((ClientCard card) => card != null && card.Id == 73956664 && card.Location == CardLocation.MonsterZone).ToList();
				}
				if (list10.Count > 0 || list11.Count > 0)
				{
					if (base.Bot.GetMonstersInMainZone().Count < 5)
					{
						list11.AddRange(list10);
						return base.Util.CheckSelectCount(list11, cards, min, max);
					}
					list10.AddRange(list11);
					return base.Util.CheckSelectCount(list10, cards, min, max);
				}
			}
			list10 = cards.Where((ClientCard card) => card != null && card.Id == 28226490).ToList();
			if (!activate_TearlamentsKaleidoHeart_2 && IsCanSpSummon() && list10.Count > 0)
			{
				return base.Util.CheckSelectCount(list10, cards, min, max);
			}
			list10 = cards.Where((ClientCard card) => card != null && card.Id == 84330567).ToList();
			if (!activate_TearlamentsRulkallos_2 && IsCanSpSummon() && list10.Count > 0)
			{
				return base.Util.CheckSelectCount(list10, cards, min, max);
			}
			list9.AddRange(list7);
			list8.Sort(CardContainer.CompareCardAttack);
			list9.AddRange(list8);
			return base.Util.CheckSelectCount(list9, cards, min, max);
		}
		if (hint == 504 && cards.Any((ClientCard card) => card != null && card.Location == CardLocation.Hand) && min == 1 && max == 1)
		{
			IList<int> list12 = new List<int>();
			if (!activate_AgidotheAncientSentinel_2)
			{
				list12.Add(62320425);
			}
			if (!activate_KelbektheAncientVanguard_2)
			{
				list12.Add(25926710);
			}
			if (IsShouldSummonFusion(SETCODE, 64))
			{
				if (activate_TearlamentsScheiren_1 && !activate_TearlamentsScheiren_2)
				{
					list12.Add(572850);
				}
				if (summoned && !activate_TearlamentsMerrli_2)
				{
					list12.Add(74078255);
				}
				if (!activate_TearlamentsHavnis_2)
				{
					list12.Add(37961969);
				}
				if (!activate_TearlamentsMerrli_2)
				{
					list12.Add(74078255);
				}
				if (!activate_TearlamentsScheiren_2)
				{
					list12.Add(572850);
				}
			}
			if (base.Enemy.GetSpellCount() > 0)
			{
				list12.Add(77723643);
			}
			if (base.Bot.Deck.Count > 0)
			{
				list12.Add(3717252);
			}
			if (base.Enemy.Graveyard.Count > 0)
			{
				list12.Add(97518132);
			}
			if (!activate_Eva && base.Bot.Graveyard.Count((ClientCard card) => card != null && card.HasAttribute(CardAttribute.Light) && card.HasRace(CardRace.Fairy)) > 0 && (CheckRemainInDeck(21074344) > 0 || CheckRemainInDeck(17266660) > 0))
			{
				list12.Add(40177746);
			}
			if (((base.Bot.HasInHand(17266660) || base.Bot.HasInHand(21074344)) && base.Bot.Hand.Count((ClientCard card) => card?.HasRace(CardRace.Fairy) ?? false) > 2) || (!base.Bot.HasInHand(17266660) && !base.Bot.HasInHand(21074344)))
			{
				list12.Add(99937011);
				list12.Add(63542003);
			}
			if (!activate_TearlamentsScream_2 && CheckRemainInDeck(74920585) > 0)
			{
				list12.Add(6767771);
			}
			if (!activate_TearlamentsSulliek_2)
			{
				list12.Add(74920585);
			}
			if (!activate_TearlamentsReinoheart_2 && HasInList(cards, 73956664))
			{
				list12.Add(73956664);
			}
			if (!cards.Any((ClientCard card) => card != null && !card.HasRace(CardRace.Fairy)))
			{
				list12.Add(21074344);
				list12.Add(17266660);
			}
			list12 = list12.Distinct().ToList();
			IList<ClientCard> list13 = CardsIdToClientCards(list12, cards, uniqueId: false);
			List<ClientCard> list14 = new List<ClientCard>(cards);
			foreach (ClientCard card2 in cards)
			{
				if (list14.Count <= 1)
				{
					break;
				}
				if ((!summoned && card2.Id == 92919429) || card2.Id == 17266660 || card2.Id == 21074344 || (!summoned && card2.Id == 74078255 && !activate_TearlamentsHavnis_1))
				{
					list14.Remove(card2);
				}
			}
			list14.Sort(CardContainer.CompareCardAttack);
			if (list13.Count <= 0)
			{
				return base.Util.CheckSelectCount(list14, cards, min, max);
			}
			return base.Util.CheckSelectCount(list13, cards, min, max);
		}
		if (hint == 504 && cards.Any((ClientCard card) => card != null && (card.Location == CardLocation.Deck || card.Location == CardLocation.Extra)) && min == 1 && max == 1)
		{
			List<int> list15 = new List<int>();
			if (HasInList(cards, 62320425) && !activate_AgidotheAncientSentinel_2 && !AllActivated())
			{
				if (HasInList(cards, 25926710) && !activate_KelbektheAncientVanguard_2 && base.Bot.HasInHand(62320425))
				{
					list15.Add(25926710);
				}
				else
				{
					list15.Add(62320425);
				}
			}
			else if (HasInList(cards, 25926710) && !activate_KelbektheAncientVanguard_2 && !AllActivated())
			{
				list15.Add(25926710);
			}
			else if (HasInList(cards, 80532587) && GetZoneCards(CardLocation.Onfield, base.Enemy).Count((ClientCard card) => card != null && !card.IsShouldNotBeTarget()) > 0)
			{
				list15.Add(80532587);
			}
			if (HasInList(cards, 40177746) && !activate_Eva && (CheckRemainInDeck(17266660) > 0 || CheckRemainInDeck(21074344) > 0))
			{
				list15.Add(40177746);
			}
			if (HasInList(cards, 99937011))
			{
				list15.Add(99937011);
			}
			if (HasInList(cards, 63542003))
			{
				list15.Add(63542003);
			}
			if (IsShouldSummonFusion())
			{
				if (cards.Any((ClientCard card) => card != null && card.Id == 37961969) && !activate_TearlamentsHavnis_2)
				{
					list15.Add(37961969);
				}
				if (cards.Any((ClientCard card) => card != null && card.Id == 74078255) && !activate_TearlamentsMerrli_2)
				{
					list15.Add(74078255);
				}
				if (cards.Any((ClientCard card) => card != null && card.Id == 572850) && !activate_TearlamentsScheiren_2)
				{
					list15.Add(572850);
				}
			}
			if (cards.Any((ClientCard card) => card != null && card.Id == 74920585) && !activate_TearlamentsSulliek_2)
			{
				list15.Add(74920585);
			}
			if (cards.Any((ClientCard card) => card != null && card.Id == 6767771) && !activate_TearlamentsScream_2 && CheckRemainInDeck(74920585) > 0)
			{
				list15.Add(6767771);
			}
			IList<ClientCard> list16 = CardsIdToClientCards(list15, cards, uniqueId: false);
			if (list16.Count > 0)
			{
				no_fusion_card = list16[0];
			}
			if (list16.Count <= 0)
			{
				return null;
			}
			return base.Util.CheckSelectCount(list16, cards, min, max);
		}
		if (hint == 507 && cards.Any((ClientCard card) => card != null && card.Location == CardLocation.Grave))
		{
			List<ClientCard> list17 = new List<ClientCard>();
			List<ClientCard> list18 = new List<ClientCard>();
			List<ClientCard> e_temp_1 = new List<ClientCard>();
			List<ClientCard> e_temp_2 = new List<ClientCard>();
			List<ClientCard> list19 = new List<ClientCard>();
			List<ClientCard> e_temp_4 = new List<ClientCard>();
			foreach (ClientCard card3 in cards)
			{
				if (card3 != null)
				{
					if (card3.Controller == 0)
					{
						list17.Add(card3);
					}
					else
					{
						list18.Add(card3);
					}
				}
			}
			if (list18.Count <= 0 && base.Bot.Deck.Count > 2)
			{
				return null;
			}
			int num3 = ((list18.Count > max) ? max : ((list18.Count < 0) ? min : list18.Count));
			if (base.Duel.CurrentChain == null || base.Duel.ChainTargets == null)
			{
				return base.Util.CheckSelectCount(cards, cards, num3, num3);
			}
			foreach (ClientCard item in base.Duel.CurrentChain)
			{
				if (item != null && item.Controller != 0 && item.Location == CardLocation.Grave && cards.Contains(item) && !key_no_send_to_deck_ids.Contains(item.Id))
				{
					if (key_send_to_deck_ids.Contains(item.Id) && !e_temp_1.Contains(item))
					{
						e_temp_1.Add(item);
					}
					else if (!e_temp_2.Contains(item))
					{
						e_temp_2.Add(item);
					}
				}
			}
			foreach (ClientCard chainTarget in base.Duel.ChainTargets)
			{
				if (chainTarget != null && chainTarget.Controller != 0 && chainTarget.Location == CardLocation.Grave && cards.Contains(chainTarget) && !key_no_send_to_deck_ids.Contains(chainTarget.Id))
				{
					if (key_send_to_deck_ids.Contains(chainTarget.Id) && !e_temp_1.Contains(chainTarget))
					{
						e_temp_1.Add(chainTarget);
					}
					else if (!e_temp_2.Contains(chainTarget))
					{
						e_temp_2.Add(chainTarget);
					}
				}
			}
			foreach (ClientCard card4 in cards)
			{
				if (card4 != null && card4.Controller != 0 && card4.Location == CardLocation.Grave && key_send_to_deck_ids.Contains(card4.Id) && !e_temp_1.Contains(card4) && !e_temp_2.Contains(card4))
				{
					e_temp_2.Add(card4);
				}
			}
			if (base.Bot.Deck.Count <= 0 && base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && card.Id == 3717252))
			{
				bot_send_to_deck_ids = new List<int> { 572850, 74078255, 37961969 };
				IList<ClientCard> list20 = CardsIdToClientCards(bot_send_to_deck_ids, list17);
				if (list20.Count <= 0)
				{
					for (int num4 = 0; num4 < list17.Count && num4 < 2; num4++)
					{
						if (list17[num4] != null && !e_temp_1.Contains(list17[num4]))
						{
							e_temp_1.Insert(0, list17[num4]);
						}
					}
				}
				else
				{
					for (int num5 = 0; num5 < list20.Count && num5 < 2; num5++)
					{
						if (list20[num5] != null && !e_temp_1.Contains(list20[num5]))
						{
							e_temp_1.Insert(0, list20[num5]);
						}
					}
				}
			}
			if (base.Bot.Deck.Count < 3)
			{
				bot_send_to_deck_ids = new List<int>();
				IList<ClientCard> list21 = new List<ClientCard>();
				if (list17.Count > 0 && base.Bot.ExtraDeck.Any((ClientCard card) => card != null && card.HasType(CardType.Fusion) && card.Id != 80532587))
				{
					if (CheckRemainInDeck(572850) <= 0 && HasInList(cards, 572850))
					{
						bot_send_to_deck_ids.Add(572850);
					}
					if (CheckRemainInDeck(74078255) <= 0 && HasInList(cards, 74078255))
					{
						bot_send_to_deck_ids.Add(74078255);
					}
					if (CheckRemainInDeck(37961969) <= 0 && HasInList(cards, 37961969))
					{
						bot_send_to_deck_ids.Add(37961969);
					}
				}
				list21 = CardsIdToClientCards(bot_send_to_deck_ids, list17);
				if (list21.Count > 0)
				{
					for (int num6 = 0; num6 < list21.Count; num6++)
					{
						if (list21[num6] != null && !e_temp_4.Contains(list21[num6]) && !e_temp_1.Contains(list21[num6]) && !e_temp_2.Contains(list21[num6]))
						{
							e_temp_4.Add(list21[num6]);
						}
					}
				}
				bot_send_to_deck_ids.Clear();
				if (HasInList(cards, 97518132))
				{
					bot_send_to_deck_ids.Add(97518132);
				}
				if (CheckRemainInDeck(25926710) <= 0 && HasInList(cards, 25926710))
				{
					bot_send_to_deck_ids.Add(25926710);
				}
				if (CheckRemainInDeck(62320425) <= 0 && HasInList(cards, 62320425))
				{
					bot_send_to_deck_ids.Add(62320425);
				}
				if (CheckRemainInDeck(17266660) <= 0 && HasInList(cards, 17266660))
				{
					bot_send_to_deck_ids.Add(17266660);
				}
				if (CheckRemainInDeck(21074344) <= 0 && HasInList(cards, 21074344))
				{
					bot_send_to_deck_ids.Add(21074344);
				}
				list21 = CardsIdToClientCards(bot_send_to_deck_ids, list17);
				if (list21.Count > 0)
				{
					for (int num7 = 0; num7 < list21.Count; num7++)
					{
						if (list21[num7] != null && !e_temp_4.Contains(list21[num7]) && !e_temp_1.Contains(list21[num7]) && !e_temp_2.Contains(list21[num7]))
						{
							e_temp_4.Add(list21[num7]);
						}
					}
				}
				foreach (ClientCard item2 in list17)
				{
					if (item2 != null && !e_temp_4.Contains(item2) && !e_temp_1.Contains(item2) && !e_temp_2.Contains(item2))
					{
						e_temp_4.Add(item2);
					}
				}
			}
			list19 = list18.Where((ClientCard card) => card != null && !e_temp_1.Contains(card) && !e_temp_2.Contains(card) && !e_temp_4.Contains(card)).ToList();
			e_temp_1.AddRange(e_temp_2);
			e_temp_1.AddRange(e_temp_4);
			e_temp_1.AddRange(list19);
			num3 = ((e_temp_1.Count > max) ? max : ((e_temp_1.Count < 0) ? min : e_temp_1.Count));
			if (e_temp_1.Count <= 0)
			{
				return base.Util.CheckSelectCount(list18, cards, max, max);
			}
			return base.Util.CheckSelectCount(e_temp_1, cards, num3, num3);
		}
		if (hint == 509 && cards.Any((ClientCard card) => card != null && (card.Level == 2 || card.LinkCount == 2) && card.Location == CardLocation.Grave))
		{
			IList<ClientCard> list22 = new List<ClientCard>();
			List<int> list23 = new List<int>();
			if (base.Duel.Player == 0)
			{
				if (HasInList(cards, 74078255) && !activate_TearlamentsMerrli_1 && IsCanFusionSummon())
				{
					list23.Add(74078255);
				}
				else if (HasInList(cards, 92919429) && !activate_DivineroftheHerald)
				{
					list23.Add(92919429);
				}
				else if (HasInList(cards, 74078255) && !activate_TearlamentsMerrli_1)
				{
					list23.Add(74078255);
				}
				else if (HasInList(cards, 65741786) && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.LinkCount > 2) > 0)
				{
					list23.Add(65741786);
				}
				else
				{
					list23 = new List<int> { 92919429, 74078255, 17266660, 21074344 };
				}
			}
			else
			{
				if (HasInList(cards, 92919429) && !activate_DivineroftheHerald && FusionDeckCheck() && ((CheckRemainInDeck(62320425) > 0 && !activate_AgidotheAncientSentinel_2) || (CheckRemainInDeck(25926710) > 0 && !activate_KelbektheAncientVanguard_2)))
				{
					list23.Add(92919429);
				}
				if (HasInList(cards, 74078255) && !activate_TearlamentsMerrli_1 && IsShouldSummonFusion(-1, -1, all: true) && FusionDeckCheck())
				{
					list23.Add(74078255);
				}
				else if (HasInList(cards, 92919429) && base.Bot.HasInExtra(80532587) && GetZoneCards(CardLocation.Onfield, base.Enemy).Any((ClientCard card) => card != null && !card.IsShouldNotBeTarget()))
				{
					list23.Add(92919429);
				}
				else if (HasInList(cards, 65741786) && base.Bot.ExtraDeck.Any((ClientCard card) => card != null && card.LinkCount > 2))
				{
					list23.Add(65741786);
				}
				else
				{
					list23 = new List<int> { 92919429, 74078255, 17266660, 21074344 };
				}
			}
			list22 = CardsIdToClientCards(list23, cards, uniqueId: false);
			if (list22.Count <= 0)
			{
				return null;
			}
			return base.Util.CheckSelectCount(list22, cards, min, max);
		}
		switch (hint)
		{
		case 573:
			if (chain_TearlamentsSulliek != null && cards.Contains(chain_TearlamentsSulliek))
			{
				List<ClientCard> selected2 = new List<ClientCard> { chain_TearlamentsSulliek };
				return base.Util.CheckSelectCount(selected2, cards, min, max);
			}
			return null;
		case 514:
			if (cards.Any((ClientCard card) => card != null && card.Controller == 1))
			{
				if (chain_PredaplantDragostapelia != null && cards.Contains(chain_PredaplantDragostapelia))
				{
					List<ClientCard> selected = new List<ClientCard> { chain_PredaplantDragostapelia };
					e_PredaplantDragostapelia_cards.Add(chain_PredaplantDragostapelia);
					return base.Util.CheckSelectCount(selected, cards, min, max);
				}
				return null;
			}
			break;
		}
		for (int num8 = 2; num8 >= 0; num8--)
		{
			if (ran_fusion_mode_0[num8])
			{
				ran_fusion_mode_0[num8] = false;
				if (base.Duel.Player != 0)
				{
					List<ClientCard> list24 = cards.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
					if (list24.Count > 0 && !activate_TearlamentsKitkallos_1 && ((!activate_TearlamentsScheiren_2 && CheckRemainInDeck(572850) > 0) || (!activate_TearlamentsHavnis_2 && CheckRemainInDeck(37961969) > 0) || (!activate_TearlamentsMerrli_2 && CheckRemainInDeck(74078255) > 0)) && base.Bot.GetMonstersInMainZone().Count < 4)
					{
						TearlamentsKitkallos_summoned = true;
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 84330567).ToList();
					if (list24.Count > 0)
					{
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 28226490).ToList();
					if (list24.Count > 0 && GetZoneCards(CardLocation.Onfield, base.Enemy).Count((ClientCard card) => card != null && !card.IsShouldNotBeTarget()) > 0 && IsShouldSummonFusion(-1, -1, all: false, 4) && !activate_TearlamentsKaleidoHeart_1 && (!base.Bot.HasInSpellZone(77103950, notDisabled: true, faceUp: true) || (base.Bot.HasInSpellZone(77103950, notDisabled: true, faceUp: true) && activate_PrimevalPlanetPerlereino_2)))
					{
						ran_fusion_mode_3[num8] = true;
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 94977269).ToList();
					if (list24.Count > 0)
					{
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 69946549).ToList();
					if (list24.Count > 0 && IsShouldSummonFusion(-1, -1, all: false, 8))
					{
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 28226490).ToList();
					if (list24.Count > 0 && IsShouldSummonFusion(-1, -1, all: false, 4))
					{
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					list24 = cards.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
					if (list24.Count > 0)
					{
						TearlamentsKitkallos_summoned = true;
						return base.Util.CheckSelectCount(list24, cards, min, max);
					}
					return null;
				}
				List<ClientCard> list25 = cards.Where((ClientCard card) => card != null && card.Id == 69946549).ToList();
				if (list25.Count > 0 && base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true))
				{
					return base.Util.CheckSelectCount(list25, cards, min, max);
				}
				list25 = cards.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
				if (list25.Count > 0 && ((!activate_TearlamentsKitkallos_1 && ((!activate_TearlamentsScheiren_2 && CheckRemainInDeck(572850) > 0) || (!activate_TearlamentsHavnis_2 && CheckRemainInDeck(37961969) > 0) || (!activate_TearlamentsMerrli_2 && CheckRemainInDeck(74078255) > 0))) || !activate_TearlamentsKitkallos_2))
				{
					TearlamentsKitkallos_summoned = true;
					return base.Util.CheckSelectCount(list25, cards, min, max);
				}
				list25 = cards.Where((ClientCard card) => card != null && card.Id == 28226490).ToList();
				if (list25.Count > 0 && IsShouldSummonFusion(-1, -1, all: false, 4))
				{
					ran_fusion_mode_3[num8] = true;
					return base.Util.CheckSelectCount(list25, cards, min, max);
				}
				list25 = cards.Where((ClientCard card) => card != null && card.Id == 84330567).ToList();
				if (base.Bot.Graveyard.Count((ClientCard card) => card != null && card.Id == 92731385) > 0 || (base.Bot.MonsterZone.Count((ClientCard card) => card != null && card.Id == 92731385 && card.IsFaceup()) > 0 && list25.Count > 0))
				{
					activate_TearlamentsRulkallos_2 = false;
					return base.Util.CheckSelectCount(list25, cards, min, max);
				}
				list25 = cards.Where((ClientCard card) => card != null && card.Id == 69946549).ToList();
				if (list25.Count > 0 && IsShouldSummonFusion(-1, -1, all: false, 8))
				{
					return base.Util.CheckSelectCount(list25, cards, min, max);
				}
				List<ClientCard> list26 = cards.Where((ClientCard card) => card != null && card.Id == 92731385).ToList();
				if (list26.Count > 0)
				{
					List<ClientCard> defaultMaterial = GetDefaultMaterial(fusionMaterial);
					if (((defaultMaterial.Count((ClientCard mcard) => mcard != null && mcard.Id == 92731385 && mcard.Location == CardLocation.MonsterZone) > 0 && activate_TearlamentsKitkallos_1) || defaultMaterial.Count((ClientCard mcard) => mcard != null && mcard.Id == 92731385 && mcard.Location == CardLocation.MonsterZone) > 1) && defaultMaterial.Count <= 2)
					{
						if (list25.Count > 0)
						{
							activate_TearlamentsRulkallos_2 = false;
							return base.Util.CheckSelectCount(list25, cards, min, max);
						}
						TearlamentsKitkallos_summoned = true;
						return base.Util.CheckSelectCount(list26, cards, min, max);
					}
					return base.Util.CheckSelectCount(list26, cards, min, max);
				}
			}
			if (mcard_0[num8] != null)
			{
				if (cards.Contains(mcard_0[num8]))
				{
					List<ClientCard> selected3 = new List<ClientCard> { mcard_0[num8] };
					mcard_0[num8] = null;
					return base.Util.CheckSelectCount(selected3, cards, min, max);
				}
				return null;
			}
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	private bool IsCanFusionSummon()
	{
		if ((!activate_TearlamentsMerrli_2 && CheckRemainInDeck(74078255) > 0) || (!activate_TearlamentsHavnis_2 && CheckRemainInDeck(37961969) > 0) || (!activate_TearlamentsScheiren_2 && CheckRemainInDeck(572850) > 0))
		{
			return base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.HasType(CardType.Fusion) && card.Id != 80532587) > 0;
		}
		return false;
	}

	private bool BaronnedeFleurSummon_2()
	{
		if (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2)
		{
			return false;
		}
		List<ClientCard> source = (from card in base.Bot.GetMonsters()
			where card != null && card.Id == 92919429 && card.IsFaceup()
			select card).ToList();
		List<ClientCard> source2 = (from card in base.Bot.GetMonsters()
			where card != null && !card.HasType(CardType.Tuner) && card.IsFaceup()
			select card).ToList();
		if (source.Any((ClientCard card) => card != null && card.Level == 2) && source2.Count((ClientCard card) => card != null && card.Level == 4) > 1)
		{
			List<ClientCard> source3 = source.Where((ClientCard card) => card != null && card.Level == 2 && card.Id == 92919429 && card.IsFaceup()).ToList();
			List<ClientCard> list = new List<ClientCard> { source3.FirstOrDefault() };
			list.AddRange(source2.Where((ClientCard card) => card != null && card.Level == 4 && card.IsFaceup()));
			base.AI.SelectMaterials(list);
			SetSpSummon();
			return true;
		}
		return false;
	}

	private bool BaronnedeFleurSummon()
	{
		List<ClientCard> source = (from card in base.Bot.GetMonsters()
			where card != null && card.Id == 92919429 && card.IsFaceup()
			select card).ToList();
		List<ClientCard> source2 = (from card in base.Bot.GetMonsters()
			where card != null && !card.HasType(CardType.Synchro) && card.IsFaceup()
			select card).ToList();
		if (source.Any((ClientCard card) => card != null && card.Level == 2) && source2.Any((ClientCard card) => card != null && card.Level == 8 && card.Id != 84330567 && card.Id != 69946549))
		{
			SetSpSummon();
			return true;
		}
		if (source.Any((ClientCard card) => card != null && card.Level == 5) && source2.Any((ClientCard card) => card != null && card.Level == 5))
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card != null && card.Level == 5 && card.Id == 92919429 && card.IsFaceup()
				select card).ToList();
			if (list.Count >= 0)
			{
				List<ClientCard> list2 = new List<ClientCard> { list.FirstOrDefault() };
				list2.AddRange(from card in base.Bot.GetMonsters()
					where card != null && card.Level == 5 && card.Id == 94977269 && card.IsFaceup()
					select card);
				list2.AddRange(from card in base.Bot.GetMonsters()
					where card != null && card.Level == 5 && card.IsFaceup()
					select card);
				base.AI.SelectMaterials(list2);
				SetSpSummon();
				return true;
			}
		}
		else if (source.Any((ClientCard card) => card != null && card.Level == 6) && source2.Any((ClientCard card) => card != null && card.Level == 4))
		{
			SetSpSummon();
			return true;
		}
		return false;
	}

	private bool SprightElfEffect()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 0)
		{
			return false;
		}
		SetSpSummon();
		return true;
	}

	private bool FusionDeckCheck()
	{
		if ((CheckRemainInDeck(37961969) <= 0 || activate_TearlamentsHavnis_2) && (CheckRemainInDeck(74078255) <= 0 || activate_TearlamentsMerrli_2))
		{
			if (CheckRemainInDeck(572850) > 0)
			{
				return !activate_TearlamentsScheiren_2;
			}
			return false;
		}
		return true;
	}

	private List<int> GetCardsIdSendToHand()
	{
		List<int> list = new List<int>();
		if (!activate_TearlamentsScheiren_1 && !base.Bot.HasInHand(572850) && CheckRemainInDeck(572850) > 0 && base.Bot.Hand.Count((ClientCard card) => card?.HasType(CardType.Monster) ?? false) > 0)
		{
			list.Add(572850);
		}
		if (!activate_TearlamentsMerrli_1 && !base.Bot.HasInHand(74078255) && CheckRemainInDeck(74078255) > 0 && (!summoned || !activate_TearlamentsKitkallos_2))
		{
			list.Add(74078255);
		}
		if (!activate_TearlamentsReinoheart_1 && !base.Bot.HasInHand(73956664) && CheckRemainInDeck(73956664) > 0)
		{
			list.Add(73956664);
		}
		if (!activate_TearlamentsScheiren_1 && !base.Bot.HasInHand(572850) && CheckRemainInDeck(572850) > 0)
		{
			list.Add(572850);
		}
		if (!activate_TearlamentsHavnis_1 && !base.Bot.HasInHand(37961969) && CheckRemainInDeck(37961969) > 0)
		{
			if (base.Duel.Player == 0 && (base.Duel.Phase != DuelPhase.End || AllActivated()))
			{
				list.Add(37961969);
			}
			else
			{
				list.Insert(0, 37961969);
			}
		}
		list.AddRange(new List<int> { 572850, 74078255, 37961969, 73956664 });
		return list;
	}

	public int CheckRemainInDeck(int id)
	{
		return id switch
		{
			3717252 => base.Bot.GetRemainingCount(3717252, 1), 
			77723643 => base.Bot.GetRemainingCount(77723643, 1), 
			572850 => base.Bot.GetRemainingCount(572850, 3), 
			73956664 => base.Bot.GetRemainingCount(73956664, 2), 
			25926710 => base.Bot.GetRemainingCount(25926710, 3), 
			99937011 => base.Bot.GetRemainingCount(99937011, 3), 
			62320425 => base.Bot.GetRemainingCount(62320425, 3), 
			63542003 => base.Bot.GetRemainingCount(63542003, 2), 
			97518132 => base.Bot.GetRemainingCount(97518132, 1), 
			37961969 => base.Bot.GetRemainingCount(37961969, 3), 
			74078255 => base.Bot.GetRemainingCount(74078255, 3), 
			92919429 => base.Bot.GetRemainingCount(92919429, 3), 
			17266660 => base.Bot.GetRemainingCount(17266660, 3), 
			21074344 => base.Bot.GetRemainingCount(21074344, 3), 
			40177746 => base.Bot.GetRemainingCount(40177746, 1), 
			6767771 => base.Bot.GetRemainingCount(6767771, 1), 
			77103950 => base.Bot.GetRemainingCount(77103950, 2), 
			74920585 => base.Bot.GetRemainingCount(74920585, 2), 
			_ => 0, 
		};
	}

	private bool PredaplantDragostapeliaEffect()
	{
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller != 0 && lastChainCard.Location == CardLocation.MonsterZone && !lastChainCard.IsShouldNotBeTarget() && !e_PredaplantDragostapelia_cards.Contains(lastChainCard))
		{
			chain_PredaplantDragostapelia = lastChainCard;
			return true;
		}
		return false;
	}

	private bool UnderworldGoddessoftheClosedWorldSummon_2()
	{
		return UnderworldGoddessoftheClosedWorldLinkSummon(filter: false);
	}

	private bool UnderworldGoddessoftheClosedWorldLinkSummon(bool filter = true)
	{
		if (base.Duel.Turn == 1 || base.Enemy.GetMonsterCount() <= 0)
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Enemy.GetMonsters()
			where card != null && card.IsFaceup() && card.IsAttack()
			select card).ToList();
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card != null && card.IsFaceup() && card.IsAttack()
			select card).ToList();
		if (list.Count <= 0 || list2.Count <= 0 || base.Enemy.MonsterZone.GetDangerousMonster() == null)
		{
			return false;
		}
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		list2.Sort(CardContainer.CompareCardAttack);
		list2.Reverse();
		if ((list[0].Attack > list2[0].Attack && (list[0].IsShouldNotBeTarget() || list[0].Attack >= 2500)) || base.Enemy.MonsterZone.GetDangerousMonster() != null)
		{
			List<ClientCard> list3 = new List<ClientCard>();
			List<ClientCard> list4 = new List<ClientCard>();
			List<ClientCard> list5 = new List<ClientCard>();
			foreach (ClientCard monster in base.Enemy.GetMonsters())
			{
				if (monster != null && monster.HasType(CardType.Effect) && monster.IsFaceup())
				{
					list3.Add(monster);
				}
			}
			if (list3.Count() <= 0)
			{
				return false;
			}
			List<ClientCard> monsters = base.Bot.GetMonsters();
			monsters.Sort(CardContainer.CompareCardAttack);
			foreach (ClientCard item in monsters)
			{
				if (item != null && !item.IsFacedown() && (item.Id != 27381364 || !summon_SprightElf) && item.LinkCount < 3 && !(no_link_ids.Contains(item.Id) && filter) && item.IsFaceup() && item.HasType(CardType.Effect))
				{
					if (item.Id == 94977269)
					{
						list4.Insert(0, item);
					}
					else
					{
						list4.Add(item);
					}
				}
			}
			if (list4.Count() < 3)
			{
				return false;
			}
			int num = 0;
			int num2 = 0;
			list3.Sort(CardContainer.CompareCardAttack);
			list3.Reverse();
			foreach (ClientCard item2 in list3)
			{
				if (!list5.Contains(item2))
				{
					list5.Add(item2);
				}
				num2 += ((!item2.HasType(CardType.Link)) ? 1 : ((item2.LinkCount != 2) ? 1 : 2));
				if (num2 >= 1)
				{
					break;
				}
			}
			if (num2 <= 0)
			{
				return false;
			}
			num += num2;
			foreach (ClientCard item3 in list4)
			{
				if (num2 <= 1)
				{
					if (!list5.Contains(item3) && item3.LinkCount < 3)
					{
						list5.Add(item3);
						num += ((!item3.HasType(CardType.Link)) ? 1 : item3.LinkCount);
						if (num >= 5)
						{
							break;
						}
					}
				}
				else
				{
					list5.Add(item3);
					num++;
					if (num >= 5)
					{
						break;
					}
				}
			}
			if (num >= 5)
			{
				base.AI.SelectMaterials(list5);
				SetSpSummon();
				return true;
			}
		}
		return false;
	}

	private bool TearlamentsKaleidoHeartEffect()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			activate_TearlamentsKaleidoHeart_1 = true;
			return DestoryEnemyCard();
		}
		activate_TearlamentsKaleidoHeart_2 = true;
		return true;
	}

	private bool UnderworldGoddessoftheClosedWorldSummon()
	{
		if (base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			return UnderworldGoddessoftheClosedWorldLinkSummon();
		}
		return false;
	}

	private bool UnderworldGoddessoftheClosedWorldSummon_3()
	{
		return UnderworldGoddessoftheClosedWorldLinkSummon();
	}

	private bool SpellActivate()
	{
		if (base.Card.Location != CardLocation.Hand)
		{
			if (base.Card.Location == CardLocation.SpellZone)
			{
				return base.Card.IsFacedown();
			}
			return false;
		}
		return true;
	}

	private bool SprightElfSummon_2()
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card != null && card.Id == 94977269 && !card.IsDisabled() && card.IsFaceup()
			select card).ToList();
		if (list.Count <= 0 || list.Count > 1 || !IsAvailableLinkZone())
		{
			return false;
		}
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card != null && card.Level == 2 && card.Id != 94977269 && card.IsFaceup()
			select card).ToList();
		if (list2.Count <= 0)
		{
			return false;
		}
		list2.Sort(CardContainer.CompareCardAttack);
		list2.Insert(0, list[0]);
		base.AI.SelectMaterials(list2);
		SetSpSummon();
		return true;
	}

	private bool FADawnDragsterSummon()
	{
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card != null && card.Id == 94977269 && !card.IsDisabled() && card.IsFaceup()
			select card).ToList();
		if (list.Count <= 0 || list.Count > 1)
		{
			return false;
		}
		SetSpSummon();
		IList<ClientCard> list2 = CardsIdToClientCards(new List<int> { 92919429 }, (from card in base.Bot.GetMonsters()
			where card?.IsFaceup() ?? false
			select card).ToList());
		if (list2.Count > 0)
		{
			list.Insert(0, list2[0]);
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool IPSummon_2()
	{
		if ((base.Bot.GetMonsterCount() <= 2 && base.Bot.Hand.Count <= 2 && !base.Bot.HasInHand(572850) && !activate_TearlamentsScheiren_1 && !base.Bot.HasInHand(74078255) && !activate_TearlamentsMerrli_1) || AllActivated())
		{
			return false;
		}
		List<ClientCard> list = (from card in base.Bot.GetMonsters()
			where card != null && card.Id == 94977269 && !card.IsDisabled() && card.IsFaceup()
			select card).ToList();
		if (list.Count <= 0 || list.Count > 1 || !IsAvailableLinkZone())
		{
			return false;
		}
		List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
			where card != null && !card.HasType(CardType.Link) && !no_link_ids.Contains(card.Id) && card.IsFaceup()
			select card).ToList();
		list2.Sort(CardContainer.CompareCardAttack);
		list2.Insert(0, list[0]);
		base.AI.SelectMaterials(list2);
		SetSpSummon();
		return true;
	}

	private bool SprightElfSummon()
	{
		if (!IsAvailableLinkZone())
		{
			return false;
		}
		List<ClientCard> list = (from card in GetZoneCards(CardLocation.MonsterZone, base.Bot)
			where card?.IsFaceup() ?? false
			select card).ToList();
		list.Sort(CardContainer.CompareCardAttack);
		List<ClientCard> list2 = new List<ClientCard>();
		List<ClientCard> list3 = new List<ClientCard>();
		List<ClientCard> list4 = new List<ClientCard>();
		List<ClientCard> list5 = new List<ClientCard>();
		foreach (ClientCard item in list)
		{
			if (item != null)
			{
				if (item.Level == 2 && list2.Count <= 0)
				{
					list2.Add(item);
				}
				else if (link_card != null && item == link_card)
				{
					list3.Insert(0, link_card);
				}
				else if (!item.IsDisabled() && no_link_ids.Contains(item.Id))
				{
					list5.Add(item);
				}
				else if (item.Level <= 2)
				{
					list3.Add(item);
				}
				else
				{
					list4.Add(item);
				}
			}
		}
		if (list2.Count <= 0 || list3.Count + list4.Count <= 0)
		{
			return false;
		}
		list4.Sort(CardContainer.CompareCardLevel);
		list4.Reverse();
		list2.AddRange(list3);
		list2.AddRange(list4);
		summon_SprightElf = true;
		base.AI.SelectMaterials(list2);
		SetSpSummon();
		return true;
	}

	private bool AbyssDwellerSummon()
	{
		if (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2)
		{
			return false;
		}
		SetSpSummon();
		return true;
	}

	private bool AbyssDwellerSummon_2()
	{
		if (base.Bot.GetMonsters().Any((ClientCard card) => card != null && card.IsFaceup() && (card.Id == 84330567 || card.Id == 28226490)))
		{
			return true;
		}
		return false;
	}

	private bool IPEffect()
	{
		if (base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		if (!base.Bot.HasInExtra(38342335) && !base.Bot.HasInExtra(21887175) && !base.Bot.HasInExtra(98127546))
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		if (base.Bot.HasInExtra(98127546))
		{
			List<ClientCard> list3 = (from card in base.Enemy.GetMonsters()
				where card != null && card.IsFaceup() && card.IsAttack()
				select card).ToList();
			List<ClientCard> list4 = (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && card.IsAttack()
				select card).ToList();
			if (list3.Count > 0 && list4.Count > 0)
			{
				list3.Sort(CardContainer.CompareCardAttack);
				list3.Reverse();
				list4.Sort(CardContainer.CompareCardAttack);
				list4.Reverse();
				if ((list3[0].Attack > list4[0].Attack && (list3[0].IsShouldNotBeTarget() || list3[0].Attack >= 2500)) || base.Enemy.MonsterZone.GetDangerousMonster() != null)
				{
					list2 = (from card in base.Bot.GetMonsters()
						where card != null && card != base.Card && card.IsFaceup() && ((card.HasType(CardType.Link) && card.LinkCount < 3) || card.HasType((CardType)8396864)) && (!no_link_ids.Contains(card.Id) || card.IsDisabled())
						select card).ToList();
					List<ClientCard> list5 = new List<ClientCard>();
					list5.Add((base.Enemy.MonsterZone.GetDangerousMonster() == null) ? list3[0] : base.Enemy.MonsterZone.GetDangerousMonster());
					list5.AddRange(list3);
					int num = 0;
					foreach (ClientCard item in list2)
					{
						if (item != null && (item.Id != 27381364 || !summon_SprightElf))
						{
							num++;
							list.Add(item);
							if (num >= 2)
							{
								break;
							}
						}
					}
					if (num >= 2)
					{
						base.AI.SelectCard(98127546);
						list.Insert(0, base.Card);
						list.Add(list5.FirstOrDefault());
						base.AI.SelectMaterials(list);
						return true;
					}
				}
			}
		}
		if (base.Bot.HasInExtra(21887175))
		{
			list.Clear();
			list2 = (from card in base.Bot.GetMonsters()
				where card != null && card != base.Card && card.IsFaceup() && ((card.HasType(CardType.Link) && card.LinkCount < 3) || card.HasType((CardType)8396864)) && (!no_link_ids.Contains(card.Id) || card.IsDisabled())
				select card).ToList();
			if (list2.Count > 0)
			{
				list2.Sort(CardContainer.CompareCardAttack);
				int num2 = 0;
				foreach (ClientCard item2 in list2)
				{
					if (item2 != null && (item2.Id != 27381364 || !summon_SprightElf))
					{
						num2 += ((!item2.HasType(CardType.Link)) ? 1 : item2.LinkCount);
						list.Add(item2);
						if (num2 >= 2)
						{
							break;
						}
					}
				}
				if (num2 >= 2)
				{
					base.AI.SelectCard(21887175);
					list.Insert(0, base.Card);
					base.AI.SelectMaterials(list);
					return true;
				}
			}
		}
		if (base.Bot.HasInExtra(38342335))
		{
			List<ClientCard> zoneCards = GetZoneCards(CardLocation.Onfield, base.Enemy);
			if (base.Bot.Hand.Count > 0 && zoneCards.Count((ClientCard card) => card != null && !card.IsShouldNotBeTarget()) > 0)
			{
				list.Clear();
				list2 = (from card in base.Bot.GetMonsters()
					where card != null && card != base.Card && card.IsFaceup() && !no_link_ids.Contains(card.Id) && card.Id != base.Card.Id
					select card).ToList();
				if (list2.Count > 0)
				{
					list2.Sort(CardContainer.CompareCardAttack);
					int num3 = 0;
					foreach (ClientCard item3 in list2)
					{
						if (item3 != null && (item3.Id != 27381364 || !summon_SprightElf))
						{
							num3 += ((!item3.HasType(CardType.Link)) ? 1 : item3.LinkCount);
							list.Add(item3);
							if (num3 >= 1)
							{
								break;
							}
						}
					}
					if (num3 >= 1)
					{
						base.AI.SelectCard(38342335);
						list.Insert(0, base.Card);
						base.AI.SelectMaterials(list);
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool IPSummon()
	{
		if (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2)
		{
			return false;
		}
		if (!base.Bot.HasInMonstersZone(27381364) || base.Bot.GetMonsterCount() <= 2)
		{
			return false;
		}
		if (base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.LinkCount > 2) <= 0 || !IsAvailableLinkZone())
		{
			return false;
		}
		List<ClientCard> list = (from card in GetZoneCards(CardLocation.MonsterZone, base.Bot)
			where card != null && card.IsFaceup() && !card.HasType(CardType.Link)
			select card).ToList();
		List<ClientCard> list2 = new List<ClientCard>();
		foreach (ClientCard item in list)
		{
			if (item != null && (item.IsDisabled() || !no_link_ids.Contains(item.Id)) && item.LinkCount < 2)
			{
				if (link_card != null && item == link_card)
				{
					list2.Insert(0, link_card);
				}
				else
				{
					list2.Add(item);
				}
			}
		}
		if (list2.Count <= 1)
		{
			return false;
		}
		list2.Sort(CardContainer.CompareCardAttack);
		base.AI.SelectMaterials(list2);
		SetSpSummon();
		return true;
	}

	private bool KnightmareUnicornEffect()
	{
		List<ClientCard> zoneCards = GetZoneCards(CardLocation.Onfield, base.Enemy);
		zoneCards = zoneCards.Where((ClientCard card) => card != null && !card.IsShouldNotBeTarget() && (tgcard == null || card != tgcard)).ToList();
		if (zoneCards.Count <= 0)
		{
			return false;
		}
		List<int> list = new List<int>();
		if (!activate_KelbektheAncientVanguard_2)
		{
			list.Add(25926710);
		}
		if (!activate_AgidotheAncientSentinel_2)
		{
			list.Add(62320425);
		}
		list.AddRange(new List<int> { 99937011, 63542003, 3717252, 77723643, 74920585, 40177746, 73956664 });
		zoneCards.Sort(CardContainer.CompareCardAttack);
		zoneCards.Reverse();
		base.AI.SelectCard(list);
		base.AI.SelectNextCard(zoneCards);
		return true;
	}

	private bool MekkKnightCrusadiaAvramaxSummon()
	{
		if (!IsAvailableLinkZone() || (base.Duel.Turn > 1 && base.Duel.Phase < DuelPhase.Main2))
		{
			return false;
		}
		if (base.Bot.HasInMonstersZone(27381364, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !summon_SprightElf && base.Bot.HasInMonstersZone(65741786, notDisabled: false, hasXyzMaterial: false, faceUp: true))
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && card.Id == 27381364
				select card).ToList();
			List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && card.Id == 65741786
				select card).ToList();
			if (list.Count <= 0 || list2.Count <= 0)
			{
				return false;
			}
			List<ClientCard> list3 = new List<ClientCard> { list[0] };
			list3.Add(list2[0]);
			base.AI.SelectMaterials(list3);
			SetSpSummon();
			return true;
		}
		List<ClientCard> list4 = (from card in base.Bot.GetMonsters()
			where card != null && card.LinkCount != 2 && card.IsFaceup() && (!no_link_ids.Contains(card.Id) || card.IsDisabled()) && card.IsExtraCard() && card.Id != 38342335
			select card).ToList();
		List<ClientCard> list5 = (from card in base.Bot.GetMonsters()
			where card != null && card.IsFaceup() && card.Id == 38342335
			select card).ToList();
		if (list4.Count <= 0 || list5.Count <= 0)
		{
			return false;
		}
		list4.Sort(CardContainer.CompareCardAttack);
		List<ClientCard> list6 = new List<ClientCard>();
		list6.Add(list4.FirstOrDefault());
		list6.Add(list4.FirstOrDefault());
		base.AI.SelectMaterials(list6);
		SetSpSummon();
		return true;
	}

	private bool MekkKnightCrusadiaAvramaxEffect()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			return true;
		}
		return DestoryEnemyCard();
	}

	private bool KnightmareUnicornSummon()
	{
		if (base.Bot.Hand.Count <= 0)
		{
			return false;
		}
		if (!IsAvailableLinkZone())
		{
			return false;
		}
		if (GetZoneCards(CardLocation.Onfield, base.Enemy).Count((ClientCard clientCard) => clientCard != null && !clientCard.IsShouldNotBeTarget()) <= 0)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>();
		List<ClientCard> list2 = new List<ClientCard>();
		foreach (ClientCard card in base.Bot.GetMonsters())
		{
			if (card != null && !card.IsFacedown() && (card.Id != 27381364 || !summon_SprightElf) && !no_link_ids.Contains(card.Id) && card.Id != 92731385 && list.Count((ClientCard _card) => _card != null && _card.Id == card.Id) <= 0)
			{
				list.Add(card);
			}
		}
		int num = 0;
		list.Sort(CardContainer.CompareCardAttack);
		List<ClientCard> list3 = new List<ClientCard>();
		List<ClientCard> list4 = list.Where((ClientCard clientCard) => clientCard != null && clientCard.LinkCount == 2).ToList();
		List<ClientCard> collection = list.Where((ClientCard clientCard) => clientCard != null && clientCard.LinkCount != 2).ToList();
		if (list4.Count() >= 1)
		{
			list3.Add(list4.FirstOrDefault());
			list3.AddRange(collection);
		}
		else
		{
			list3.AddRange(collection);
			list3.AddRange(list4);
		}
		if (list3.Count((ClientCard clientCard) => clientCard != null && clientCard.LinkCount >= 2) > 1 && list3.Count((ClientCard clientCard) => clientCard != null && clientCard.LinkCount < 2) < 1)
		{
			return false;
		}
		foreach (ClientCard item in list3)
		{
			if (!list2.Contains(item) && item.LinkCount < 3)
			{
				list2.Add(item);
				num += ((!item.HasType(CardType.Link)) ? 1 : item.LinkCount);
				if (num >= 3)
				{
					break;
				}
			}
		}
		if (num >= 3)
		{
			base.AI.SelectMaterials(list2);
			SetSpSummon();
			return true;
		}
		return false;
	}

	private bool BaronnedeFleurEffect()
	{
		if (base.ActivateDescription == base.Util.GetStringId(84815190, 0))
		{
			return DestoryEnemyCard();
		}
		if (base.ActivateDescription == base.Util.GetStringId(84815190, 1))
		{
			return base.Duel.LastChainPlayer != 0;
		}
		return false;
	}

	private bool PrimevalPlanetPerlereinoEffect()
	{
		if (SpellActivate())
		{
			return true;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null && lastChainCard.Controller == 0 && lastChainCard.Id == 28226490)
		{
			return false;
		}
		return DestoryEnemyCard();
	}

	private bool DestoryEnemyCard()
	{
		if (base.Duel.Player == 0 && base.Card.Id == 77103950 && !on_chaining_cards.Any((ClientCard ccard) => ccard != null && !ccard.IsDisabled() && ccard.Controller == 0 && ccard.Id == 92731385) && base.Bot.HasInMonstersZone(92731385) && !activate_TearlamentsKitkallos_3 && !AllActivated())
		{
			List<ClientCard> list = new List<ClientCard>();
			List<ClientCard> list2 = new List<ClientCard>();
			foreach (ClientCard monster in base.Bot.GetMonsters())
			{
				if (monster != null)
				{
					if (monster.Id == 92731385 && monster.IsDisabled())
					{
						list.Add(monster);
					}
					else if (monster.Id == 92731385)
					{
						list2.Add(monster);
					}
				}
			}
			list.AddRange(list2);
			base.AI.SelectCard(list);
			activate_PrimevalPlanetPerlereino_2 = true;
			return true;
		}
		ClientCard problematicEnemyMonster = base.Util.GetProblematicEnemyMonster(0, canBeTarget: true);
		if (problematicEnemyMonster != null && (tgcard == null || tgcard != problematicEnemyMonster))
		{
			base.AI.SelectCard(problematicEnemyMonster);
			tgcard = problematicEnemyMonster;
			return true;
		}
		problematicEnemyMonster = base.Util.GetBestEnemySpell(onlyFaceup: true);
		if (problematicEnemyMonster != null && (tgcard == null || tgcard != problematicEnemyMonster))
		{
			base.AI.SelectCard(problematicEnemyMonster);
			tgcard = problematicEnemyMonster;
			return true;
		}
		List<ClientCard> zoneCards = GetZoneCards(CardLocation.Onfield, base.Enemy);
		zoneCards = zoneCards.Where((ClientCard tcard) => tcard != null && !tcard.IsShouldNotBeTarget() && (tgcard == null || tcard != tgcard)).ToList();
		if (zoneCards.Count <= 0)
		{
			return false;
		}
		zoneCards.Sort(CardContainer.CompareCardAttack);
		zoneCards.Reverse();
		tgcard = zoneCards[0];
		base.AI.SelectCard(zoneCards);
		if (base.Card.Id == 77103950)
		{
			activate_PrimevalPlanetPerlereino_2 = true;
		}
		return true;
	}

	private bool ElderEntityNtssEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return DestoryEnemyCard();
		}
		return true;
	}

	private bool DivineroftheHeraldSummon()
	{
		if ((CheckRemainInDeck(25926710) > 0 && !activate_KelbektheAncientVanguard_2) || (CheckRemainInDeck(62320425) > 0 && !activate_AgidotheAncientSentinel_2))
		{
			summoned = true;
			return true;
		}
		return false;
	}

	private bool EvaEffect()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if ((from card in base.Bot.GetGraveyardMonsters()
			where card != null && card.HasAttribute(CardAttribute.Light) && card.HasRace(CardRace.Fairy) && card != base.Card
			select card).ToList().Count <= 0)
		{
			return false;
		}
		activate_Eva = true;
		return true;
	}

	private bool DivineroftheHeraldEffect()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (base.Duel.Player == 0)
			{
				if (!IsCanSpSummon() && CheckRemainInDeck(99937011) <= 0 && CheckRemainInDeck(63542003) <= 0)
				{
					return false;
				}
				if ((AllActivated() || base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.HasType(CardType.Fusion) && card.Id != 80532587) <= 0) && CheckRemainInDeck(99937011) <= 0 && CheckRemainInDeck(63542003) <= 0)
				{
					return false;
				}
				if ((activate_KelbektheAncientVanguard_2 || CheckRemainInDeck(25926710) <= 0) && (activate_AgidotheAncientSentinel_2 || CheckRemainInDeck(62320425) <= 0) && base.Bot.HasInExtra(27381364) && (from card in base.Bot.GetMonsters()
					where card != null && card.IsFaceup() && !no_link_ids.Contains(card.Id) && card != base.Card
					select card).ToList().Count() >= 1)
				{
					return false;
				}
			}
			activate_DivineroftheHerald = true;
			return true;
		}
		return true;
	}

	private bool TearlamentsScreamEffect_1()
	{
		return SpellActivate();
	}

	private bool MudoratheSwordOracleEffect()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if ((base.Bot.Hand.Count((ClientCard card) => card != null && card.Id == 62320425) <= 0 || activate_AgidotheAncientSentinel_2) && (base.Bot.Hand.Count((ClientCard card) => card != null && card.Id == 25926710) <= 0 || activate_KelbektheAncientVanguard_2) && !summoned && !base.Bot.HasInHand(99937011) && !base.Bot.HasInHand(63542003))
			{
				return false;
			}
			IList<int> list = new List<int>();
			if (!activate_AgidotheAncientSentinel_2)
			{
				list.Add(62320425);
			}
			if (!activate_KelbektheAncientVanguard_2)
			{
				list.Add(25926710);
			}
			list.Add(63542003);
			list.Add(99937011);
			if (base.Card.Id == 99937011)
			{
				activate_MudoratheSwordOracle_2 = true;
			}
			else if (base.Card.Id == 63542003)
			{
				activate_KeldotheSacredProtector_2 = true;
			}
			base.AI.SelectCard(list);
			SetSpSummon();
			return true;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 0)
			{
				return false;
			}
			if (GetZoneCards(CardLocation.Grave, base.Enemy).Count() <= 0 && base.Bot.Deck.Count >= 3)
			{
				return false;
			}
			if ((base.Duel.CurrentChain == null || base.Duel.CurrentChain.Count <= 0) && base.Bot.Deck.Count >= 3)
			{
				return false;
			}
			if (base.Duel.CurrentChain.Any((ClientCard card) => (card != null && card.Controller == 0 && (card.Id == 99937011 || card.Id == 63542003)) || card.Id == 97518132))
			{
				return false;
			}
			if (base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && card.Id == 3717252) && base.Bot.Deck.Count <= 0)
			{
				if (base.Card.Id == 99937011)
				{
					activate_MudoratheSwordOracle_2 = true;
				}
				else if (base.Card.Id == 63542003)
				{
					activate_KeldotheSacredProtector_2 = true;
				}
				return true;
			}
			foreach (ClientCard chainTarget in base.Duel.ChainTargets)
			{
				if (chainTarget != null && ((chainTarget == base.Card && (base.Bot.Deck.Count < 5 || base.Enemy.Graveyard.Count((ClientCard ccard) => ccard != null && !key_no_send_to_deck_ids.Contains(ccard.Id)) > 0)) || (chainTarget.Controller == 1 && chainTarget.Location == CardLocation.Grave)))
				{
					if (base.Card.Id == 99937011)
					{
						activate_MudoratheSwordOracle_2 = true;
					}
					else if (base.Card.Id == 63542003)
					{
						activate_KeldotheSacredProtector_2 = true;
					}
					return true;
				}
			}
			foreach (ClientCard item in base.Duel.CurrentChain)
			{
				if (item != null && item.Controller != 0 && item.Location == CardLocation.Grave && key_send_to_deck_ids.Contains(item.Id))
				{
					if (base.Card.Id == 99937011)
					{
						activate_MudoratheSwordOracle_2 = true;
					}
					else if (base.Card.Id == 63542003)
					{
						activate_KeldotheSacredProtector_2 = true;
					}
					return true;
				}
			}
			if (base.Duel.Phase == DuelPhase.End && base.Bot.Deck.Count < 3 && base.Bot.Graveyard.Count > 0)
			{
				if (base.Card.Id == 99937011)
				{
					activate_MudoratheSwordOracle_2 = true;
				}
				else if (base.Card.Id == 63542003)
				{
					activate_KeldotheSacredProtector_2 = true;
				}
				return true;
			}
			return false;
		}
		if (base.Duel.Phase == DuelPhase.End && base.Bot.Deck.Count < 3 && base.Bot.Graveyard.Count > 0)
		{
			if (base.Card.Id == 99937011)
			{
				activate_MudoratheSwordOracle_2 = true;
			}
			else if (base.Card.Id == 63542003)
			{
				activate_KeldotheSacredProtector_2 = true;
			}
			return true;
		}
		return false;
	}

	private bool TearlamentsScheirenEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return FusionEffect(572850);
		}
		if (AllActivated() && (!base.Bot.GetMonsters().Any((ClientCard card) => card != null && card.IsFaceup() && card.Level == 4) || !base.Bot.HasInExtra(21044178)))
		{
			return false;
		}
		activate_TearlamentsScheiren_1 = true;
		SetSpSummon();
		return true;
	}

	private bool NaelshaddollArielEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			return base.Enemy.Graveyard.Count((ClientCard card) => card != null && !key_no_remove_ids.Contains(card.Id)) > 0;
		}
		SetSpSummon();
		return true;
	}

	private bool IsShouldSummonFusion(int setcode = -1, int race = -1, bool all = false, int flag = 31)
	{
		List<ClientCard> list = (from card in GetZoneCards(CardLocation.MonsterZone, base.Bot)
			where card?.IsFaceup() ?? false
			select card).ToList();
		list.AddRange(GetZoneCards((CardLocation)18, base.Bot));
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if ((flag & 1) > 0 && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.Id == 92731385) > 0)
		{
			foreach (ClientCard item in list)
			{
				if (item == null || (item.Id == 84330567 && item.Location == CardLocation.MonsterZone && !item.IsDisabled()))
				{
					continue;
				}
				if (item.HasSetcode(SETCODE) && item.HasType(CardType.Monster))
				{
					num++;
					if (item.HasRace(CardRace.Aqua))
					{
						num--;
						num3++;
					}
				}
				else if (item.HasRace(CardRace.Aqua))
				{
					num2++;
				}
			}
			if (setcode == SETCODE)
			{
				num++;
				if (race == 64)
				{
					num--;
					num3++;
				}
			}
			else if (race == 64)
			{
				num2++;
			}
			if (num3 > 1 || (num > 0 && num3 > 0) || (num > 0 && num2 > 0) || (num2 > 0 && num3 > 0))
			{
				return true;
			}
		}
		if ((flag & 2) > 0 && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.Id == 84330567) > 0)
		{
			num = 0;
			num2 = 0;
			foreach (ClientCard item2 in list)
			{
				if (item2 != null)
				{
					if (item2.Id == 92731385 && num <= 0)
					{
						num++;
					}
					else if (item2.HasSetcode(SETCODE) && item2.HasType(CardType.Monster))
					{
						num2++;
					}
				}
			}
			if (setcode == SETCODE)
			{
				num2++;
			}
			if (num > 0 && num2 > 0)
			{
				return true;
			}
		}
		if ((flag & 4) > 0 && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.Id == 28226490) > 0)
		{
			num = 0;
			num2 = 0;
			foreach (ClientCard item3 in list)
			{
				if (item3 != null && (item3.Id != 84330567 || item3.Location != CardLocation.MonsterZone || item3.IsDisabled()))
				{
					if (item3.Id == 73956664 && num <= 0)
					{
						num++;
					}
					else if (item3.HasRace(CardRace.Aqua))
					{
						num2++;
					}
				}
			}
			if (race == 64)
			{
				num2++;
			}
			if (num > 0 && num2 > 0)
			{
				return true;
			}
		}
		if ((flag & 8) > 0 && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.Id == 69946549) > 0)
		{
			num = 0;
			foreach (ClientCard item4 in list)
			{
				if (item4 != null && (item4.Id != 84330567 || item4.Location != CardLocation.MonsterZone || item4.IsDisabled()) && ((base.Bot.GetMonstersInMainZone().Count > 4 && item4.Location == CardLocation.MonsterZone) || base.Bot.GetMonstersInMainZone().Count <= 4) && item4.HasType(CardType.Fusion))
				{
					num++;
				}
			}
			if (num > 0)
			{
				return true;
			}
		}
		if (all && (flag & 0x10) > 0 && base.Bot.ExtraDeck.Count((ClientCard card) => card != null && card.Id == 94977269) > 0)
		{
			List<ClientCard> list2 = new List<ClientCard>();
			List<ClientCard> list3 = new List<ClientCard>();
			foreach (ClientCard item5 in list)
			{
				if (item5 != null)
				{
					if (item5.HasSetcode(157))
					{
						list2.Add(item5);
					}
					else if ((item5.Id == 37961969 || item5.Id == 74078255 || item5.Id == 572850) && item5.HasAttribute(CardAttribute.Dark))
					{
						list3.Add(item5);
					}
				}
			}
			num = list2.Count;
			num2 = list3.Count;
			if (setcode == SETCODE && race == 64)
			{
				num2++;
			}
			if (num > 0 && num2 > 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool TearlamentsRulkallosEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			activate_TearlamentsRulkallos_2 = true;
			SetSpSummon();
			return true;
		}
		if (base.Card.IsDisabled())
		{
			return false;
		}
		activate_TearlamentsRulkallos_1 = true;
		return true;
	}

	private bool AgidotheAncientSentinelEffect()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Duel.Player == 1)
			{
				return false;
			}
			if ((!base.Bot.HasInGraveyard(25926710) && !base.Bot.HasInGraveyard(99937011) && !base.Bot.HasInGraveyard(63542003) && base.Bot.Hand.Count((ClientCard card) => card != null && card.Id == 62320425) <= 1) || base.Bot.GetMonstersInMainZone().Count >= 4)
			{
				return false;
			}
			if (!activate_AgidotheAncientSentinel_2 && (base.Bot.HasInHand(17266660) || base.Bot.HasInHand(21074344)) && base.Bot.Hand.Count((ClientCard card) => card != null && card.Id == 62320425) <= 1)
			{
				return false;
			}
			SetSpSummon();
			return true;
		}
		if (AllActivated() && !base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && (card.Id == 37961969 || card.Id == 74078255 || card.Id == 572850)))
		{
			return false;
		}
		activate_AgidotheAncientSentinel_2 = true;
		return true;
	}

	private bool AllActivated()
	{
		if ((activate_TearlamentsScheiren_2 || CheckRemainInDeck(572850) <= 0) && (activate_TearlamentsHavnis_2 || CheckRemainInDeck(37961969) <= 0))
		{
			if (!activate_TearlamentsMerrli_2)
			{
				return CheckRemainInDeck(74078255) <= 0;
			}
			return true;
		}
		return false;
	}

	private bool KelbektheAncientVanguardEffect()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Duel.LastChainPlayer == 0)
			{
				return false;
			}
			if ((base.Bot.HasInHand(17266660) || base.Bot.HasInHand(21074344)) && !activate_KelbektheAncientVanguard_2)
			{
				return false;
			}
			List<ClientCard> monsters = base.Enemy.GetMonsters();
			monsters.Sort(CardContainer.CompareCardAttack);
			monsters.Reverse();
			base.AI.SelectCard(monsters);
			SetSpSummon();
			return true;
		}
		if (AllActivated() && !base.Duel.CurrentChain.Any((ClientCard card) => card != null && card.Controller == 0 && (card.Id == 37961969 || card.Id == 74078255 || card.Id == 572850)))
		{
			return false;
		}
		activate_KelbektheAncientVanguard_2 = true;
		return true;
	}

	private bool TearlamentsMerrliEffect()
	{
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (AllActivated())
			{
				return false;
			}
			activate_TearlamentsMerrli_1 = true;
			return true;
		}
		return FusionEffect(74078255);
	}

	private bool TearlamentsHavnisEffect()
	{
		if (base.Card.Location == CardLocation.Hand)
		{
			if (AllActivated())
			{
				return false;
			}
			activate_TearlamentsHavnis_1 = true;
			return true;
		}
		return FusionEffect(37961969);
	}

	private bool SpellSet()
	{
		if (base.Card.Id == 74920585)
		{
			return !base.Bot.GetSpells().Any((ClientCard card) => card != null && card.Id == 74920585 && (card.IsFacedown() || (card.IsFaceup() && !card.IsDisabled())));
		}
		if (!base.Card.HasType(CardType.QuickPlay))
		{
			return base.Card.HasType(CardType.Trap);
		}
		return true;
	}

	private bool TearlamentsReinoheartEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			List<ClientCard> list = base.Bot.Hand.Where((ClientCard card) => card?.HasSetcode(SETCODE) ?? false).ToList();
			foreach (ClientCard item in new List<ClientCard>(list))
			{
				if ((item.Id == 572850 && !activate_TearlamentsScheiren_1) || (item.Id == 74078255 && !summoned && !activate_TearlamentsMerrli_1) || (item.Id == 37961969 && list.Count((ClientCard ccard) => ccard != null && ccard.Id == 37961969) > 1))
				{
					list.Remove(item);
				}
			}
			if (list.Count() <= 0)
			{
				return false;
			}
			activate_TearlamentsReinoheart_2 = true;
			SetSpSummon();
			return true;
		}
		if (AllActivated())
		{
			return false;
		}
		activate_TearlamentsReinoheart_1 = true;
		return true;
	}

	private bool ShaddollDragonEffect()
	{
		if (base.Card.Location == CardLocation.Grave)
		{
			ClientCard bestEnemySpell = base.Util.GetBestEnemySpell();
			List<ClientCard> list = (from ccard in base.Enemy.GetSpells()
				where tgcard == null || tgcard != ccard
				select ccard).ToList();
			if (bestEnemySpell != null && (tgcard == null || tgcard != bestEnemySpell))
			{
				base.AI.SelectCard(bestEnemySpell);
				return true;
			}
			if (list.Count > 0)
			{
				base.AI.SelectCard(list);
				return true;
			}
			return false;
		}
		return true;
	}

	private bool TearlamentsKitkallosEffect_2()
	{
		if (base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true))
		{
			return TearlamentsKitkallosEffect();
		}
		return false;
	}

	private bool TearlamentsKitkallosEffect()
	{
		if (base.ActivateDescription == base.Util.GetStringId(92731385, 1))
		{
			if (base.Card.IsDisabled())
			{
				return false;
			}
			if (AllActivated() && !base.Bot.HasInGraveyard(28226490) && !base.Bot.HasInGraveyard(84330567) && !base.Bot.HasInGraveyard(92731385))
			{
				return false;
			}
			if (base.Bot.HasInMonstersZone(94977269, notDisabled: true, hasXyzMaterial: false, faceUp: true))
			{
				base.AI.SelectCard(94977269);
			}
			else if (base.Bot.HasInMonstersZone(92731385) && !activate_TearlamentsKitkallos_3 && !AllActivated())
			{
				base.AI.SelectCard(92731385);
			}
			else if (!activate_TearlamentsScheiren_2 && base.Bot.HasInMonstersZone(572850) && IsShouldSummonFusion())
			{
				base.AI.SelectCard(572850);
			}
			else if (!activate_TearlamentsMerrli_2 && base.Bot.HasInMonstersZone(74078255) && IsShouldSummonFusion())
			{
				base.AI.SelectCard(74078255);
			}
			else if (!activate_TearlamentsHavnis_2 && base.Bot.HasInMonstersZone(37961969) && IsShouldSummonFusion())
			{
				base.AI.SelectCard(37961969);
			}
			else if (base.Bot.HasInMonstersZone(73956664) && !activate_TearlamentsReinoheart_1 && !AllActivated())
			{
				base.AI.SelectCard(73956664);
			}
			else if (base.Bot.HasInMonstersZone(40177746) && base.Bot.Graveyard.Count((ClientCard card) => card != null && card.HasAttribute(CardAttribute.Light) && card.HasRace(CardRace.Fairy)) > 0 && (CheckRemainInDeck(21074344) > 0 || CheckRemainInDeck(17266660) > 0))
			{
				base.AI.SelectCard(40177746);
			}
			else if ((base.Bot.HasInExtra(84330567) && !base.Bot.HasInGraveyard(92731385)) || (base.Bot.HasInExtra(69946549) && base.Bot.Graveyard.Count((ClientCard card) => card?.HasType(CardType.Fusion) ?? false) <= 0))
			{
				base.AI.SelectCard(92731385);
			}
			else if (base.Bot.HasInMonstersZone(77723643) && base.Enemy.GetSpellCount() > 0)
			{
				base.AI.SelectCard(77723643);
			}
			else
			{
				List<ClientCard> zoneCards = GetZoneCards(CardLocation.MonsterZone, base.Bot);
				zoneCards.Sort(CardContainer.CompareCardAttack);
				base.AI.SelectCard(zoneCards);
			}
			if (!activate_TearlamentsKitkallos_1 && base.Bot.HasInGraveyard(92731385))
			{
				base.AI.SelectNextCard(base.Bot.Graveyard.Where((ClientCard card) => card != null && card.Id == 92731385).ToList());
			}
			if (!activate_TearlamentsMerrli_1 && base.Bot.HasInGraveyard(74078255) && !AllActivated())
			{
				base.AI.SelectNextCard(base.Bot.Graveyard.Where((ClientCard card) => card != null && card.Id == 74078255).ToList());
			}
			else if (!activate_TearlamentsMerrli_1 && base.Bot.HasInHand(74078255) && !AllActivated())
			{
				base.AI.SelectNextCard(base.Bot.Hand.Where((ClientCard card) => card != null && card.Id == 74078255).ToList());
			}
			else if (!activate_TearlamentsReinoheart_1 && base.Bot.HasInGraveyard(73956664) && !AllActivated())
			{
				base.AI.SelectNextCard(base.Bot.Graveyard.Where((ClientCard card) => card != null && card.Id == 73956664).ToList());
			}
			else if (!activate_TearlamentsReinoheart_1 && base.Bot.HasInHand(73956664) && !AllActivated())
			{
				base.AI.SelectNextCard(base.Bot.Hand.Where((ClientCard card) => card != null && card.Id == 73956664).ToList());
			}
			else
			{
				base.AI.SelectNextCard(28226490, 84330567, 92731385);
			}
			activate_TearlamentsKitkallos_2 = true;
			SetSpSummon();
			return true;
		}
		if (base.Card.Location == CardLocation.Grave)
		{
			if (AllActivated())
			{
				return false;
			}
			activate_TearlamentsKitkallos_3 = true;
			return true;
		}
		if (base.Card.IsDisabled())
		{
			return false;
		}
		activate_TearlamentsKitkallos_1 = true;
		return true;
	}

	private bool HasinZoneKeyCard(CardLocation loc)
	{
		if (loc == CardLocation.Hand)
		{
			if (!activate_TearlamentsScheiren_2 && base.Bot.HasInHand(572850))
			{
				return true;
			}
			if (!activate_TearlamentsHavnis_2 && base.Bot.HasInHand(37961969))
			{
				return true;
			}
			if (!activate_TearlamentsMerrli_2 && base.Bot.HasInHand(74078255))
			{
				return true;
			}
		}
		if (loc == CardLocation.Deck)
		{
			if (!activate_TearlamentsScheiren_2 && CheckRemainInDeck(572850) > 0)
			{
				return true;
			}
			if (!activate_TearlamentsHavnis_2 && CheckRemainInDeck(37961969) > 0)
			{
				return true;
			}
			if (!activate_TearlamentsMerrli_2 && CheckRemainInDeck(74078255) > 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool TearlamentsSulliekEffect()
	{
		if (SpellActivate())
		{
			return true;
		}
		if (base.Card.Location == CardLocation.SpellZone)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone && !lastChainCard.IsShouldNotBeTarget())
			{
				if (_PredaplantDragostapelia != null && _PredaplantDragostapelia.Location == CardLocation.MonsterZone && _PredaplantDragostapelia.IsFaceup() && e_PredaplantDragostapelia_cards.Contains(lastChainCard))
				{
					return false;
				}
				chain_TearlamentsSulliek = lastChainCard;
				return true;
			}
			if (base.Duel.Player == 1 || base.Duel.Phase == DuelPhase.End)
			{
				if (base.Duel.LastChainPlayer == 0 || base.Duel.CurrentChain.Count((ClientCard ccard) => ccard != null && ccard.Controller == 0 && (ccard.Id == 37961969 || ccard.Id == 74078255 || ccard.Id == 572850)) > 0)
				{
					return false;
				}
				if (((base.Bot.HasInMonstersZone(92731385, notDisabled: false, hasXyzMaterial: false, faceUp: true) && !activate_TearlamentsKitkallos_3 && IsShouldSummonFusion(SETCODE, 64) && ((CheckRemainInDeck(37961969) > 0 && !activate_TearlamentsHavnis_2) || (CheckRemainInDeck(74078255) > 0 && !activate_TearlamentsMerrli_2) || (CheckRemainInDeck(572850) > 0 && !activate_TearlamentsScheiren_2)) && base.Bot.GetMonsterCount() < 3) || (IsCanFusionSummon() && (base.Bot.HasInMonstersZone(572850) || base.Bot.HasInMonstersZone(37961969) || base.Bot.HasInMonstersZone(74078255))) || (!activate_TearlamentsReinoheart_2 && base.Bot.HasInMonstersZone(73956664) && (HasinZoneKeyCard(CardLocation.Hand) || (base.Bot.Hand.Count((ClientCard ccard) => ccard?.HasSetcode(SETCODE) ?? false) > 0 && HasinZoneKeyCard(CardLocation.Deck))) && IsShouldSummonFusion(SETCODE, 64))) && IsCanSpSummon())
				{
					List<ClientCard> list = (from ecard in base.Enemy.GetMonsters()
						where ecard != null && !ecard.IsShouldNotBeTarget() && ecard.IsFaceup()
						select ecard).ToList();
					if (list.Count > 0)
					{
						list.Sort(CardContainer.CompareCardAttack);
						list.Reverse();
						chain_TearlamentsSulliek = list[0];
					}
					else
					{
						chain_TearlamentsSulliek = null;
					}
					return true;
				}
			}
			return false;
		}
		return true;
	}
}
