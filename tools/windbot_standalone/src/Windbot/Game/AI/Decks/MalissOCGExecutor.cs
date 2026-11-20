using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("MalissOCG", "AI_MalissOCG", "Normal")]
public class MalissOCGExecutor : DefaultExecutor
{
	public class SetCode
	{
		public const int Maliss = 447;
	}

	public class CardId
	{
		public const int Artifact_Lancea = 34267821;

		public const int Dimension_Shifter = 91800273;

		public const int MaxxG = 23434538;

		public const int Mulcharmy_Fuwalos = 42141493;

		public const int Infinite_Impermanence = 10045474;

		public const int Dominus_Impulse = 40366667;

		public const int AshBlossom = 14558127;

		public const int CalledbytheGrave = 24224830;

		public const int Gold_Sarcophagus = 75500286;

		public const int Wizard_Ignister = 3723262;

		public const int Backup_Ignister = 30118811;

		public const int Maliss_Chessy_Cat = 96676583;

		public const int Maliss_White_Rabbit = 69272449;

		public const int Maliss_Dormouse = 32061192;

		public const int Maliss_March_Hare = 20938824;

		public const int Maliss_in_the_Mirror = 93453053;

		public const int Maliss_in_Underground = 68337209;

		public const int Maliss_GWC_06 = 20726052;

		public const int Maliss_TB_11 = 57111661;

		public const int Maliss_MTP_07 = 94722358;

		public const int Mereologic_Aggregator = 9940036;

		public const int Cyberse_Desavewurm = 92422871;

		public const int Allied_Code_Talker_Ignister = 39138610;

		public const int Firewall_Dragon = 64211118;

		public const int Accesscode_Talker = 86066372;

		public const int Maliss_Hearts_Crypter = 21848500;

		public const int Maliss_Red_Ransom = 68059897;

		public const int Maliss_White_Binder = 95454996;

		public const int Transcode_Talker = 46947713;

		public const int Splash_Mage = 59859086;

		public const int Haggard_Lizardose = 9763474;

		public const int Cyberse_Wicckid = 52698008;

		public const int Link_Decoder = 30342076;
	}

	public class CardCount
	{
		public int Dimension_Shifter;

		public int Summon;

		public int Phase;

		public List<int> Activate = new List<int>();

		public List<int> ActivateRemoved = new List<int>();

		public List<int> Position = new List<int>();

		public List<int> Set = new List<int>();

		public List<int> Oppo = new List<int>();

		public void Clear()
		{
			Activate.Clear();
			ActivateRemoved.Clear();
			Position.Clear();
			Set.Clear();
			Oppo.Clear();
			if (Dimension_Shifter > 0)
			{
				Dimension_Shifter--;
			}
			if (Summon > 0)
			{
				Summon--;
			}
		}

		public void AddActivateOppo(int id)
		{
			Oppo.Add(id);
		}

		public bool CheckActivateOppo(int id)
		{
			return !Oppo.Contains(id);
		}

		public void AddSummon()
		{
			Summon = 1;
		}

		public void AddCard(int id)
		{
			if (id == 91800273)
			{
				Dimension_Shifter = 2;
			}
			else
			{
				Activate.Add(id);
			}
		}

		public void AddSet(int id)
		{
			Set.Add(id);
		}

		public bool CheckSet(int id)
		{
			return !Set.Contains(id);
		}

		public void AddCardRemoved(int id)
		{
			ActivateRemoved.Add(id);
		}

		public void AddPosition(int id)
		{
			Position.Add(id);
		}

		public void AddPhase()
		{
			Phase++;
		}

		public bool CheckCard(int id)
		{
			if (id == 91800273)
			{
				return Dimension_Shifter == 0;
			}
			return !Activate.Contains(id);
		}

		public bool CheckCardRemoved(int id)
		{
			return !ActivateRemoved.Contains(id);
		}

		public bool CheckPosition(int id)
		{
			return !Position.Contains(id);
		}

		public int CheckPhase()
		{
			return Phase;
		}

		public bool CheckSummon()
		{
			return Summon == 0;
		}
	}

	private struct ZoneData
	{
		public int Zone;

		public ClientCard[] CheckZone;
	}

	public CardCount Count = new CardCount();

	public MalissOCGExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Repos, MonsterRepos);
		AddExecutor(ExecutorType.GoToBattlePhase, GoToBattlePhase);
		AddExecutor(ExecutorType.Activate, 91800273, Effect_Enemy_Turn);
		AddExecutor(ExecutorType.Activate, 42141493, Effect_Enemy_Turn);
		AddExecutor(ExecutorType.Activate, 23434538, Effect_Enemy_Turn);
		AddExecutor(ExecutorType.Activate, 10045474, Effect_Infinite_Impermanence);
		AddExecutor(ExecutorType.Activate, 14558127, base.DefaultAshBlossomAndJoyousSpring);
		AddExecutor(ExecutorType.Activate, 40366667, Effect_Enemy_Chain);
		AddExecutor(ExecutorType.Activate, 24224830, base.DefaultCalledByTheGrave);
		AddExecutor(ExecutorType.Activate, 69272449, Effect_White_Rabbit);
		AddExecutor(ExecutorType.Activate, 9763474, Effect_Haggard_Lizardose);
		AddExecutor(ExecutorType.Activate, 59859086);
		AddExecutor(ExecutorType.Activate, 52698008);
		AddExecutor(ExecutorType.Activate, 92422871);
		AddExecutor(ExecutorType.Activate, 46947713);
		AddExecutor(ExecutorType.Activate, 9940036, Effect_Mereologic_Aggregator);
		AddExecutor(ExecutorType.Activate, 64211118, Effect_Firewall_Dragon);
		AddExecutor(ExecutorType.Activate, 39138610, Effect_Allied_Code_Talker_Ignister);
		AddExecutor(ExecutorType.Activate, 95454996, Effect_Maliss_Link);
		AddExecutor(ExecutorType.Activate, 68059897, Effect_Maliss_Link);
		AddExecutor(ExecutorType.Activate, 21848500, Effect_Maliss_Hearts_Crypter);
		AddExecutor(ExecutorType.Activate, 30342076);
		AddExecutor(ExecutorType.Summon, 32061192, Summon_Maliss_Dormouse);
		AddExecutor(ExecutorType.Summon, 69272449, Summon_Maliss_White_Rabbit);
		AddExecutor(ExecutorType.Summon, 96676583, Summon_Maliss_Chessy_Cat);
		AddExecutor(ExecutorType.Summon, 30118811, Summon_Backup_Ignister);
		AddExecutor(ExecutorType.Activate, 32061192, Effect_Maliss_Dormouse);
		AddExecutor(ExecutorType.SpSummon, 68059897, SP_Maliss_Link);
		AddExecutor(ExecutorType.SpellSet, SpellSet_Maliss);
		AddExecutor(ExecutorType.Activate, 96676583, Effect_Maliss_Chessy_Cat);
		AddExecutor(ExecutorType.SpSummon, 59859086, SP_Splash_Mage);
		AddExecutor(ExecutorType.SpSummon, 9763474, SP_Haggard_Lizardose);
		AddExecutor(ExecutorType.SpSummon, 30342076, SP_Link_Decoder);
		AddExecutor(ExecutorType.SpSummon, 52698008, SP_Cyberse_Wicckid);
		AddExecutor(ExecutorType.Activate, 68337209, Effect_Remove);
		AddExecutor(ExecutorType.Activate, 75500286, Effect_Remove);
		AddExecutor(ExecutorType.Activate, 57111661, Effect_Maliss_TB_11);
		AddExecutor(ExecutorType.Activate, 94722358, Effect_Maliss_MTP_07);
		AddExecutor(ExecutorType.Activate, 20938824, Effect_Maliss_March_Hare);
		AddExecutor(ExecutorType.Activate, 30118811);
		AddExecutor(ExecutorType.Activate, 3723262, Effect_Wizard_Ignister);
		AddExecutor(ExecutorType.Activate, 93453053, Effect_Maliss_in_the_Mirror);
		AddExecutor(ExecutorType.SpSummon, 21848500, SP_Maliss_Hearts_Crypter);
		AddExecutor(ExecutorType.SpSummon, 95454996, SP_Maliss_White_Binder);
		AddExecutor(ExecutorType.Activate, 20726052, Effect_Maliss_GWC_06);
		AddExecutor(ExecutorType.SpSummon, 64211118, SP_Firewall_Dragon);
		AddExecutor(ExecutorType.SpSummon, 39138610, SP_Allied_Code_Talker_Ignister);
		AddExecutor(ExecutorType.SpellSet, 93453053);
		AddExecutor(ExecutorType.SpSummon, 46947713, SP_Transcode_Talker);
		AddExecutor(ExecutorType.SpellSet, SpellSet);
	}

	public override void OnNewTurn()
	{
		Count.AddPhase();
		Count.Clear();
		base.OnNewTurn();
	}

	public override void OnChaining(int player, ClientCard card)
	{
		if (card.Id == 91800273 || card.Id == 34267821)
		{
			Count.AddCard(card.Id);
		}
		else if (player == 0)
		{
			if (card.Location == CardLocation.Removed)
			{
				Count.AddCardRemoved(card.Id);
			}
			else
			{
				Count.AddCard(card.Id);
			}
		}
		if (player == 1)
		{
			Count.AddActivateOppo(card.Id);
		}
	}

	public override void OnChainEnd()
	{
		if (DefaultCheckWhetherCardIdIsNegated(91800273) && !Count.CheckCard(base.Card.Id))
		{
			Count.Dimension_Shifter = 0;
		}
		Count.Oppo.Clear();
	}

	public override bool OnSelectYesNo(int desc)
	{
		if (desc == base.Util.GetStringId(95454996, 3))
		{
			return true;
		}
		if (desc == base.Util.GetStringId(94722358, 3))
		{
			return base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && (i.HasType((CardType)917504) || i.IsFacedown())) > 0;
		}
		return base.OnSelectYesNo(desc);
	}

	public override int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		if (player == 0 && location == CardLocation.MonsterZone)
		{
			if (new List<int> { 68059897, 59859086 }.Contains(cardId))
			{
				base.AI.SendCustomChat(0);
			}
			if (new List<int> { 52698008, 39138610 }.Contains(cardId))
			{
				if ((0x40 & available) > 0 && (base.Bot.MonsterZone[3] == null || base.Bot.MonsterZone[4] == null))
				{
					return 64;
				}
				if ((0x20 & available) > 0 && (base.Bot.MonsterZone[0] == null || base.Bot.MonsterZone[1] == null))
				{
					return 32;
				}
			}
			if (base.Bot.HasInMonstersZone(52698008) && Count.CheckCard(52698008))
			{
				int num = 0;
				for (int i = 0; i < 7; i++)
				{
					if (base.Bot.MonsterZone[i] != null && base.Bot.MonsterZone[i].IsCode(52698008))
					{
						num = i;
					}
				}
				switch (num)
				{
				case 5:
					if ((2 & available) > 0)
					{
						return 2;
					}
					if ((4 & available) > 0)
					{
						return 4;
					}
					break;
				case 6:
					if ((8 & available) > 0)
					{
						return 8;
					}
					if ((0x10 & available) > 0)
					{
						return 16;
					}
					break;
				}
			}
			if (cardId == 46947713)
			{
				if ((0x40 & available) > 0 && base.Bot.MonsterZone[3] == null)
				{
					return 64;
				}
				if ((0x20 & available) > 0 && base.Bot.MonsterZone[1] == null)
				{
					return 32;
				}
				if ((1 & available) > 0 && base.Bot.MonsterZone[1] == null)
				{
					return 1;
				}
				if ((2 & available) > 0 && base.Bot.MonsterZone[2] == null)
				{
					return 2;
				}
				if ((4 & available) > 0 && base.Bot.MonsterZone[3] == null)
				{
					return 4;
				}
				if ((8 & available) > 0 && base.Bot.MonsterZone[4] == null)
				{
					return 8;
				}
			}
			if (cardId == 39138610)
			{
				ZoneData[] obj = new ZoneData[7]
				{
					new ZoneData
					{
						Zone = 1,
						CheckZone = new ClientCard[1] { base.Bot.MonsterZone[1] }
					},
					new ZoneData
					{
						Zone = 2,
						CheckZone = new ClientCard[2]
						{
							base.Bot.MonsterZone[0],
							base.Bot.MonsterZone[2]
						}
					},
					new ZoneData
					{
						Zone = 4,
						CheckZone = new ClientCard[2]
						{
							base.Bot.MonsterZone[1],
							base.Bot.MonsterZone[3]
						}
					},
					new ZoneData
					{
						Zone = 8,
						CheckZone = new ClientCard[2]
						{
							base.Bot.MonsterZone[2],
							base.Bot.MonsterZone[4]
						}
					},
					new ZoneData
					{
						Zone = 16,
						CheckZone = new ClientCard[1] { base.Bot.MonsterZone[3] }
					},
					new ZoneData
					{
						Zone = 32,
						CheckZone = new ClientCard[3]
						{
							base.Bot.MonsterZone[0],
							base.Bot.MonsterZone[1],
							base.Bot.MonsterZone[2]
						}
					},
					new ZoneData
					{
						Zone = 64,
						CheckZone = new ClientCard[3]
						{
							base.Bot.MonsterZone[2],
							base.Bot.MonsterZone[3],
							base.Bot.MonsterZone[4]
						}
					}
				};
				int num2 = -1;
				int result = 0;
				ZoneData[] array = obj;
				for (int j = 0; j < array.Length; j++)
				{
					ZoneData zoneData = array[j];
					if ((zoneData.Zone & available) != 0)
					{
						int num3 = zoneData.CheckZone.Count((ClientCard card) => card == null);
						if (num3 > num2)
						{
							num2 = num3;
							result = zoneData.Zone;
						}
					}
				}
				if (num2 >= 0)
				{
					return result;
				}
			}
			if ((0x40 & available) > 0)
			{
				return 64;
			}
			if ((0x20 & available) > 0)
			{
				return 32;
			}
		}
		return base.OnSelectPlace(cardId, player, location, available);
	}

	public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		if (base.AI.HaveSelectedCards())
		{
			return null;
		}
		ClientCard clientCard = base.Duel.GetCurrentSolvingChainCard();
		if (clientCard == null)
		{
			clientCard = base.Card;
		}
		switch (clientCard.Id)
		{
		case 69272449:
			if (cards.Any((ClientCard i) => i.Id == 57111661) && Count.CheckCard(57111661))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 57111661).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 20726052) && Count.CheckCard(20726052))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20726052).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 94722358) && Count.CheckCard(94722358))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 94722358).ToList(), cards, min, max);
			}
			break;
		case 68337209:
			if (Count.CheckSummon())
			{
				if (cards.Any((ClientCard i) => i.Id == 32061192 && i.Location == CardLocation.Deck) && !base.Bot.HasInHand(32061192) && Check_Maliss_Dormouse() && Count.CheckCardRemoved(32061192))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192 && i.Location == CardLocation.Deck).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449 && i.Location == CardLocation.Deck) && !base.Bot.HasInHand(69272449) && Check_Maliss_White_Rabbit() && Count.CheckCardRemoved(69272449))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449 && i.Location == CardLocation.Deck).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 20938824 && i.Location == CardLocation.Deck) && Check_Maliss_March_Hare(CardLocation.Removed) && Count.CheckCard(20938824))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583 && i.Location == CardLocation.Deck) && !base.Bot.HasInHand(96676583) && Count.CheckCardRemoved(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583 && i.Location == CardLocation.Deck).ToList(), cards, min, max);
				}
			}
			else
			{
				if (cards.Any((ClientCard i) => i.Id == 32061192 && i.Location == CardLocation.Deck) && Count.CheckCardRemoved(32061192) && Check_Maliss_Dormouse())
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449 && i.Location == CardLocation.Deck) && Count.CheckCardRemoved(69272449) && Check_Maliss_White_Rabbit())
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 20938824 && i.Location == CardLocation.Deck) && Check_Maliss_March_Hare(CardLocation.Removed) && Count.CheckCard(20938824))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583 && i.Location == CardLocation.Deck) && Count.CheckCardRemoved(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
			}
			return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Location == CardLocation.Deck).ToList(), cards, min, max);
		case 75500286:
			if (Count.CheckSummon())
			{
				if (cards.Any((ClientCard i) => i.Id == 32061192) && !base.Bot.HasInHand(32061192) && Check_Maliss_Dormouse() && Count.CheckCardRemoved(32061192))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449) && !base.Bot.HasInHand(69272449) && Check_Maliss_White_Rabbit() && Count.CheckCardRemoved(69272449))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583) && !base.Bot.HasInHand(96676583) && Check_Maliss_Chessy_Cat() && Count.CheckCardRemoved(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				break;
			}
			if (cards.Any((ClientCard i) => i.Id == 32061192) && Count.CheckCardRemoved(32061192) && Check_Maliss_Dormouse())
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCardRemoved(69272449) && Check_Maliss_White_Rabbit())
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCardRemoved(96676583) && Check_Maliss_Chessy_Cat())
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
			}
			break;
		case 32061192:
			if (Count.CheckSummon())
			{
				if (cards.Any((ClientCard i) => i.Id == 69272449) && !base.Bot.HasInHand(69272449) && Check_Maliss_White_Rabbit() && Count.CheckCardRemoved(69272449))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583) && !base.Bot.HasInHand(96676583) && Check_Maliss_Chessy_Cat() && Count.CheckCardRemoved(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				break;
			}
			if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCardRemoved(69272449) && Check_Maliss_White_Rabbit())
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCardRemoved(96676583) && Check_Maliss_Chessy_Cat())
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.Id == 20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
			}
			break;
		case 57111661:
			switch (hint)
			{
			case 509:
				if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
				{
					if (cards.Any((ClientCard i) => i.Id == 32061192) && Count.CheckCardRemoved(32061192))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCardRemoved(69272449))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.Id == 20938824) && Count.CheckCardRemoved(20938824))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)).ToList(), cards, min, max);
					}
				}
				if (cards.Any((ClientCard i) => i.Id == 32061192) && Count.CheckCard(32061192))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCard(69272449))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCard(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
				break;
			case 503:
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447) && i.HasType(CardType.Link)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasType(CardType.Link)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 94722358:
			switch (hint)
			{
			case 506:
				if (base.Duel.Player == 1 && cards.Any((ClientCard i) => i.Id == 20938824) && Count.CheckCard(20938824) && Check_Maliss_March_Hare(CardLocation.Hand))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 32061192) && Count.CheckCard(32061192))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCard(69272449))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCard(96676583))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
				break;
			case 503:
				if (cards.Any((ClientCard i) => i.Controller == 1))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Controller == 1).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447) && i.HasType(CardType.Link)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasType(CardType.Link)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 9763474:
		case 52698008:
		case 59859086:
			switch (hint)
			{
			case 503:
				if (cards.Any((ClientCard i) => Count.CheckCard(i.Id) && Count.CheckCardRemoved(i.Id) && i.HasSetcode(447) && i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCard(i.Id) && Count.CheckCardRemoved(i.Id) && i.HasSetcode(447) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				break;
			case 509:
				if (cards.Any((ClientCard i) => i.HasSetcode(447)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447)).ToList(), cards, min, max);
				}
				break;
			case 551:
				if (cards.Any((ClientCard i) => i.IsCode(9763474)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(9763474)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 68059897:
			switch (hint)
			{
			case 506:
			{
				List<ClientCard> list = base.Bot.Graveyard.ToList();
				list.AddRange(base.Bot.GetSpells());
				list.AddRange(base.Bot.Hand);
				if (cards.Any((ClientCard i) => i.IsCode(93453053)) && Check_Maliss_in_the_Mirror(CardLocation.Removed) && list.Any((ClientCard i) => i.HasType(CardType.Trap)) && ((((base.Bot.HasInHand(96676583) && Count.CheckSummon()) || base.Bot.HasInMonstersZone(96676583)) && Count.CheckCard(96676583)) || (base.Bot.HasInHand(20938824) && Count.CheckCard(20938824))) && Count.CheckCard(93453053))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(93453053)).ToList(), cards, min, max);
				}
				if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(93453053)).ToList(), cards, min, max);
				}
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(68337209)).ToList(), cards, min, max);
			}
			case 503:
				if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
				{
					if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCardRemoved(96676583))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => !i.IsCode(96676583) && Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.IsCode(96676583) && Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)).ToList(), cards, min, max);
					}
				}
				if (cards.Any((ClientCard i) => i.Id == 20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 20938824).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 32061192) && Count.CheckCardRemoved(32061192) && Check_Maliss_Dormouse())
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 32061192).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 69272449) && Count.CheckCardRemoved(69272449) && Check_Maliss_White_Rabbit())
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 69272449).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.Id == 96676583) && Count.CheckCardRemoved(96676583) && Check_Maliss_Chessy_Cat())
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Id == 96676583).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 96676583:
			if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
			{
				if (cards.Any((ClientCard i) => i.IsCode(20938824)) && !base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447)) && !base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(20938824)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
			}
			if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.IsCode(93453053)) && Check_Maliss_in_the_Mirror(CardLocation.Grave))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.IsCode(93453053)).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id)))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
			}
			break;
		case 95454996:
			switch (hint)
			{
			case 503:
			{
				List<ClientCard> list2 = new List<ClientCard>();
				if (5 - base.Bot.GetMonstersInMainZone().Count > 0 && Count.CheckCard(39138610))
				{
					list2.AddRange(cards.Where((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && i.Controller == 0 && i.HasType(CardType.Link)));
					if (base.Duel.Player == 1)
					{
						list2.AddRange(cards.Where((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && i.Controller == 0 && i.IsCode(69272449)));
					}
					list2.AddRange(cards.Where((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && i.Controller == 0 && i.HasType(CardType.Monster)));
					list2.AddRange(cards.Where((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && i.Controller == 0 && i.HasType(CardType.Spell)));
				}
				list2.AddRange(cards.Where((ClientCard i) => i.Controller == 1));
				list2.AddRange(cards.Where((ClientCard i) => TrashCards(i.Id, CardLocation.Grave)));
				list2.AddRange(cards.Where((ClientCard i) => !i.HasSetcode(447) && !i.HasType(CardType.Trap)));
				list2.AddRange(cards.Where((ClientCard i) => !i.HasSetcode(447) && i.HasType(CardType.Trap)));
				if (list2.Count() > max)
				{
					list2 = list2.Take(max).ToList();
				}
				if (list2.Count() > 0)
				{
					return base.Util.CheckSelectCount(list2, cards, list2.Count(), list2.Count());
				}
				if (cards.Any((ClientCard i) => TrashCards(i.Id, CardLocation.Grave)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => TrashCards(i.Id, CardLocation.Grave)).ToList(), cards, min, min);
				}
				return base.Util.CheckSelectCount(cards, cards, min, min);
			}
			case 510:
				if (cards.Any((ClientCard i) => i.IsCode(20726052)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(20726052)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 20938824:
			switch (hint)
			{
			case 506:
				if (cards.Any((ClientCard i) => Count.CheckCard(i.Id) && i.IsCode(20938824)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCard(i.Id) && i.IsCode(20938824)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => Count.CheckCard(i.Id) && !i.HasType(CardType.Link)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCard(i.Id) && !i.HasType(CardType.Link)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => !i.HasType(CardType.Link)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.HasType(CardType.Link)).ToList(), cards, min, max);
				}
				break;
			case 503:
				if (base.Duel.Player == 1)
				{
					if (base.Bot.GetMonstersInMainZone().Count() > 3)
					{
						if (cards.Any((ClientCard i) => i.HasType(CardType.Spell) && i.Location == CardLocation.Grave))
						{
							return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasType(CardType.Spell) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
						}
						if (cards.Any((ClientCard i) => !i.HasType(CardType.Link) && i.Location == CardLocation.Grave))
						{
							return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.HasType(CardType.Link) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
						}
					}
					else if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasType(CardType.Link) && i.Location == CardLocation.Grave))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasType(CardType.Link) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
					}
				}
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap) && i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => !i.HasType(CardType.Trap) && i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.HasType(CardType.Trap) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 93453053:
			switch (hint)
			{
			case 503:
				if (!cards.Any((ClientCard i) => i.Location != CardLocation.Grave))
				{
					if (cards.Any((ClientCard i) => i.HasType(CardType.Trap)) && !base.Bot.HasInHandOrInSpellZoneOrInGraveyard(20726052) && !base.Bot.HasInBanished(20726052))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasType(CardType.Trap)).ToList(), cards, min, max);
					}
					break;
				}
				if (cards.Any((ClientCard i) => i.IsCode(68059897)) && Count.CheckCardRemoved(68059897))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(68059897)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasType(CardType.Link) && i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasType(CardType.Link) && i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
				break;
			case 506:
				if (cards.Any((ClientCard i) => i.IsCode(20726052)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(20726052)).ToList(), cards, min, max);
				}
				break;
			case 573:
				if (cards.Contains(base.Util.GetLastChainCard()))
				{
					return base.Util.CheckSelectCount(new List<ClientCard> { base.Util.GetLastChainCard() }, cards, min, max);
				}
				break;
			}
			break;
		case 20726052:
			switch (hint)
			{
			case 503:
				if (cards.Any((ClientCard i) => Count.CheckCardRemoved(i.Id)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
				}
				break;
			case 509:
				if (cards.Any((ClientCard i) => i.IsCode(95454996) && i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(95454996) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasType(CardType.Link) && !Count.CheckCardRemoved(i.Id) && i.Location == CardLocation.Grave))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasType(CardType.Link) && !Count.CheckCardRemoved(i.Id) && i.Location == CardLocation.Grave).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 9940036:
			if (cards.Any((ClientCard i) => i.Controller == 1 && Count.CheckActivateOppo(i.Id)))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Controller == 1 && Count.CheckActivateOppo(i.Id)).ToList(), cards, min, max);
			}
			return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Controller == 1).ToList(), cards, min, max);
		case 64211118:
			if (base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget() && i.IsFaceup()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && i.HasType((CardType)917504)) > 0 && base.Duel.Player == 1 && cards.Any((ClientCard i) => i.IsCode(9940036)))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(9940036)).ToList(), cards, min, max);
			}
			if (cards.Any((ClientCard i) => i.IsCode(92422871)))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(92422871)).ToList(), cards, min, max);
			}
			return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Location == CardLocation.Deck).ToList(), cards, min, max);
		case 39138610:
			switch (hint)
			{
			case 509:
				if (cards.Any((ClientCard i) => !i.IsCode(95454996)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.IsCode(95454996)).ToList(), cards, max, max);
				}
				return base.OnSelectCard(cards, max, max, hint, cancelable: false);
			case 500:
				if (cards.Any((ClientCard i) => i.LinkCount < 4))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.LinkCount < 4).ToList(), cards, max, max);
				}
				break;
			}
			break;
		case 30118811:
			switch (hint)
			{
			case 506:
				if (clientCard.Id == 91800273 || clientCard.Id == 34267821)
				{
					if (!base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)) && cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
					}
					if (!base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)) && cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
					}
				}
				if (cards.Any((ClientCard i) => i.IsCode(3723262)) && base.Bot.Hand.Count() > 0 && (base.Bot.Graveyard.Any((ClientCard i) => i.HasRace(CardRace.Cyberse)) || (base.Bot.HasInExtra(30342076) && base.Bot.GetMonsters().Any((ClientCard i) => i.Level <= 4 && i.HasRace(CardRace.Cyberse)) && Count.CheckCard(91800273)) || (base.Bot.HasInExtra(9763474) && !Count.CheckCard(91800273) && Count.CheckCard(34267821) && (from i in (from i in base.Bot.GetMonsters()
						where i.IsFaceup() && (!i.HasType(CardType.Link) || i.LinkCount < 2)
						select i).ToList()
					group i by i.Id into i
					select i.First()).Count() >= 2)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(3723262)).ToList(), cards, min, max);
				}
				if (base.Bot.HasInHand(20938824))
				{
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id)).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
					}
				}
				else if (cards.Any((ClientCard i) => i.IsCode(20938824)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(20938824)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => i.HasSetcode(447)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447)).ToList(), cards, min, max);
				}
				break;
			case 501:
				if (clientCard.Id == 91800273 || clientCard.Id == 34267821)
				{
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && !i.IsCode(20938824)).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
					}
				}
				if (base.Bot.HasInHand(20938824) && !base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447)))
				{
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id)).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824) && Count.CheckCardRemoved(i.Id)).ToList(), cards, min, max);
					}
					if (cards.Any((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824)))
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447) && !i.IsCode(20938824)).ToList(), cards, min, max);
					}
					if (cards.Count((ClientCard i) => i.IsCode(20938824)) > 1)
					{
						return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.IsCode(20938824)).ToList(), cards, min, max);
					}
				}
				if (cards.Any((ClientCard i) => TrashCards(i.Id, CardLocation.Hand)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => TrashCards(i.Id, CardLocation.Hand)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => !i.HasType(CardType.Monster)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.HasType(CardType.Monster)).ToList(), cards, min, max);
				}
				if (cards.Any((ClientCard i) => !i.HasRace(CardRace.Cyberse)))
				{
					return base.Util.CheckSelectCount(cards.Where((ClientCard i) => !i.HasRace(CardRace.Cyberse)).ToList(), cards, min, max);
				}
				break;
			}
			break;
		case 3723262:
			if (cards.Any((ClientCard i) => i.HasSetcode(447)))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.HasSetcode(447)).ToList(), cards, min, max);
			}
			break;
		case 21848500:
			if (hint == 503 && cards.Any((ClientCard i) => i.Controller == 1))
			{
				return base.Util.CheckSelectCount(cards.Where((ClientCard i) => i.Controller == 1).ToList(), cards, min, max);
			}
			break;
		}
		return base.OnSelectCard(cards, min, max, hint, cancelable);
	}

	private bool TrashCards(int code, CardLocation loc)
	{
		switch (loc)
		{
		case CardLocation.Grave:
			return new List<int> { 23434538, 34267821, 91800273, 42141493, 10045474, 40366667, 14558127, 24224830, 75500286 }.Contains(code);
		case CardLocation.Hand:
			if (base.Bot.GetFieldCount() > 0 && code == 42141493)
			{
				return true;
			}
			if (base.Bot.Graveyard.Count > 0 && code == 91800273)
			{
				return true;
			}
			break;
		}
		return false;
	}

	private bool MonsterRepos()
	{
		if (!base.Enemy.GetMonsters().Any((ClientCard i) => i.IsDefense()) && base.Util.GetTotalAttackingMonsterAttack(0) + base.Card.Attack >= base.Enemy.LifePoints + base.Util.GetTotalAttackingMonsterAttack(1) && base.Card.IsDefense())
		{
			return true;
		}
		return base.Card.IsFacedown();
	}

	private bool SpellSet()
	{
		return base.Card.HasType((CardType)65540);
	}

	private bool SpellSet_Maliss()
	{
		if (base.Card.HasType(CardType.Trap) && base.Card.HasSetcode(447))
		{
			return base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447));
		}
		return false;
	}

	private bool Effect_Enemy_Turn()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Id == 91800273 && base.Duel.Player == 1 && Count.CheckCard(base.Card.Id))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return base.Duel.Player == 1;
	}

	private bool Effect_Enemy_Chain()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		ClientCard lastChainCard = base.Util.GetLastChainCard();
		if (lastChainCard != null)
		{
			return lastChainCard.Controller == 1;
		}
		return false;
	}

	private bool Effect_Infinite_Impermanence()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
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
				if (Count.CheckPosition(num))
				{
					return false;
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
				Count.AddPosition(num);
				return true;
			}
		}
		else if (lastChainCard != null && lastChainCard.Controller == 1 && lastChainCard.Location == CardLocation.MonsterZone)
		{
			base.AI.SelectCard(lastChainCard);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_Removed(int lp = 300)
	{
		int num = 5 - base.Bot.GetMonstersInMainZone().Count;
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.HasType(CardType.Monster) && !base.Card.IsCode(20938824) && num - base.Duel.CurrentChain.Count((ClientCard i) => i.HasSetcode(447) && i.Location == CardLocation.Removed && i.HasType(CardType.Monster)) <= 0)
		{
			return false;
		}
		if (base.Bot.LifePoints > lp && base.Card.Location == CardLocation.Removed)
		{
			Count.AddCardRemoved(base.Card.Id);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_Chessy_Cat()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(96676583, 0))
		{
			if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
			{
				if (base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap) && (!i.IsCode(93453053) || Check_Maliss_in_the_Mirror(CardLocation.Removed))))
				{
					Count.AddCard(base.Card.Id);
					return true;
				}
				if (base.Bot.HasInHand(20938824) && !base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447)))
				{
					Count.AddCard(base.Card.Id);
					return true;
				}
				return false;
			}
			if (base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id) && !i.HasType(CardType.Trap)))
			{
				Count.AddCard(base.Card.Id);
				return true;
			}
			return false;
		}
		return Effect_Maliss_Removed();
	}

	private bool Effect_Maliss_March_Hare()
	{
		if (base.Util.GetLastChainCard() != null && base.Util.GetLastChainCard().IsCode(39138610))
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.Hand)
		{
			if (base.Duel.Player == 1 && (!base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Link)) || base.Bot.GetMonstersInMainZone().Count() > 3))
			{
				return false;
			}
			if (base.Bot.HasInMonstersZone(96676583) && Count.CheckCard(96676583))
			{
				return false;
			}
			if (Check_Maliss_March_Hare(CardLocation.Hand))
			{
				Count.AddCard(base.Card.Id);
				return true;
			}
			return false;
		}
		return Effect_Maliss_Removed();
	}

	private bool Effect_Maliss_Dormouse()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return Effect_Maliss_Removed();
	}

	private bool Summon_Maliss_Chessy_Cat()
	{
		if (Check_Maliss_Chessy_Cat())
		{
			Count.AddSummon();
			return true;
		}
		return false;
	}

	private bool Effect_White_Rabbit()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return Effect_Maliss_Removed();
	}

	private bool Summon_Maliss_Dormouse()
	{
		if (Check_Maliss_Dormouse())
		{
			Count.AddSummon();
			return true;
		}
		return false;
	}

	private bool Summon_Maliss_White_Rabbit()
	{
		if (Check_Maliss_White_Rabbit())
		{
			Count.AddSummon();
			return true;
		}
		return false;
	}

	private bool Check_Maliss_in_the_Mirror(CardLocation loc)
	{
		if (loc == CardLocation.Removed)
		{
			if (!Count.CheckCard(34267821) || !Count.CheckCardRemoved(20938824))
			{
				return false;
			}
			List<ClientCard> cards = base.Bot.Hand.ToList();
			cards.AddRange(base.Bot.GetMonsters());
			cards.AddRange(base.Bot.GetSpells());
			cards.AddRange(base.Bot.Graveyard);
			cards.AddRange(base.Bot.Banished);
			return base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447) && ((i.HasType(CardType.Monster) && cards.Count((ClientCard j) => j.HasType(CardType.Monster)) < 10) || (i.HasType(CardType.Spell) && cards.Count((ClientCard j) => j.HasType(CardType.Spell)) < 4) || (i.HasType(CardType.Trap) && cards.Count((ClientCard j) => j.HasType(CardType.Trap)) < 3)));
		}
		List<ClientCard> monsters = base.Bot.Hand.GetMonsters();
		monsters.AddRange(base.Bot.GetMonsters());
		return monsters.Any((ClientCard i) => Count.CheckCardRemoved(i.Id));
	}

	private bool Check_Maliss_Chessy_Cat()
	{
		if (base.Bot.Hand.Any((ClientCard i) => i.HasSetcode(447) && !i.HasType(CardType.Trap) && i != base.Card && Count.CheckCardRemoved(i.Id)))
		{
			return Count.CheckCard(96676583);
		}
		return false;
	}

	private bool Check_Maliss_White_Rabbit()
	{
		if (base.Bot.Graveyard.Count((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Trap)) < 3)
		{
			return Count.CheckCard(69272449);
		}
		return false;
	}

	private bool Check_Maliss_Dormouse()
	{
		List<ClientCard> list = base.Bot.Hand.ToList();
		list.AddRange(base.Bot.GetMonsters());
		list.AddRange(base.Bot.GetSpells());
		list.AddRange(base.Bot.Graveyard);
		list.AddRange(base.Bot.Banished);
		if (list.Count((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster)) < 9)
		{
			return Count.CheckCard(32061192);
		}
		return false;
	}

	private bool Check_Maliss_March_Hare(CardLocation loc)
	{
		if (loc == CardLocation.Removed)
		{
			if (!Count.CheckCard(34267821))
			{
				return false;
			}
			if (base.Bot.Banished.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster)) || Count.CheckCard(20938824))
			{
				return Count.CheckCardRemoved(20938824);
			}
			return false;
		}
		if (!Count.CheckCard(34267821))
		{
			return false;
		}
		return base.Bot.Graveyard.GetMonsters().Any((ClientCard i) => i.HasSetcode(447));
	}

	private bool Effect_Maliss_TB_11()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() > 1 && (!base.Bot.HasInMonstersZone(68059897) || !base.Bot.HasInMonstersZone(95454996)))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard i) => Count.CheckCardRemoved(i.Id)))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		if (base.Bot.HasInMonstersZone(68059897) && Count.CheckCardRemoved(68059897))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_MTP_07()
	{
		if (base.Duel.LastChainPlayer == 0)
		{
			return false;
		}
		if (DefaultCheckWhetherCardIsNegated(base.Card) || base.Duel.Player == 0 || base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && (i.HasType((CardType)917504) || i.IsFacedown())) == 0 || !base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Link)))
		{
			return false;
		}
		if (base.Bot.GetMonsterCount() > 1 && (!base.Bot.HasInMonstersZone(68059897) || !base.Bot.HasInMonstersZone(95454996)))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard i) => Count.CheckCardRemoved(i.Id)) && ((Count.CheckCard(20938824) && !base.Bot.HasInHand(20938824) && Check_Maliss_March_Hare(CardLocation.Hand)) || (Count.CheckSummon() && ((Count.CheckCard(32061192) && !base.Bot.HasInMonstersZone(32061192) && Check_Maliss_Dormouse()) || (Count.CheckCard(69272449) && !base.Bot.HasInMonstersZone(69272449) && Check_Maliss_White_Rabbit()) || (Count.CheckCard(96676583) && !base.Bot.HasInMonstersZone(96676583) && Check_Maliss_Chessy_Cat())))))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		if (base.Bot.HasInMonstersZone(68059897) && Count.CheckCardRemoved(68059897))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_GWC_06()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Duel.Player == 0 && base.Bot.HasInGraveyard(95454996))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard i) => Count.CheckCardRemoved(i.Id)) && base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Link)))
		{
			Count.AddCard(base.Card.Id);
			return true;
		}
		return false;
	}

	private bool Effect_Remove()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if ((base.Bot.HasInMonstersZone(32061192) || !Count.CheckCard(32061192) || !Count.CheckCardRemoved(32061192) || !Check_Maliss_Dormouse()) && (base.Bot.HasInMonstersZone(69272449) || !Count.CheckCard(69272449) || !Count.CheckCardRemoved(69272449) || !Check_Maliss_White_Rabbit()) && (base.Bot.HasInMonstersZone(96676583) || !Count.CheckCard(96676583) || !Count.CheckCardRemoved(69272449) || !Check_Maliss_Chessy_Cat()))
		{
			if (Count.CheckCardRemoved(20938824) && Check_Maliss_March_Hare(CardLocation.Removed))
			{
				return base.Bot.Banished.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster));
			}
			return false;
		}
		return true;
	}

	private bool SP_Splash_Mage()
	{
		if (base.Bot.GetMonsters().Count((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount < 2) < 2 && (!base.Bot.HasInMonstersZone(68059897) || !Count.CheckCardRemoved(68059897) || base.Bot.GetMonsters().Count() != 2 || !Count.CheckCard(95454996)))
		{
			return false;
		}
		bool flag = false;
		flag = Count.CheckCard(91800273) || !Count.CheckCard(34267821) || base.Bot.Graveyard.Any((ClientCard i) => i.HasType(CardType.Monster) && i.HasRace(CardRace.Cyberse) && !i.HasType(CardType.Link));
		if (flag)
		{
			List<ClientCard> cards = (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && !card.HasType(CardType.Link)
				select card).ToList();
			base.AI.SelectMaterials(cards);
		}
		return flag;
	}

	private bool SP_Cyberse_Wicckid()
	{
		if (!Count.CheckCard(34267821))
		{
			return false;
		}
		if (!Count.CheckCard(91800273) && !base.Bot.Graveyard.Any((ClientCard i) => i.HasRace(CardRace.Cyberse)))
		{
			return false;
		}
		if (base.Bot.HasInHand(30118811) || !Count.CheckCard(30118811))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Any((ClientCard i) => i.IsFaceup() && i.Level <= 4 && i.HasRace(CardRace.Cyberse)) && base.Bot.GetMonsterCount() == 3 && Count.CheckCard(30118811) && base.Bot.Hand.Count > 0 && base.Bot.HasInExtra(21848500) && base.Bot.HasInExtra(30342076))
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.Sequence > 4
				select card).ToList();
			list.AddRange((from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType(CardType.Link)
				select card).ToList());
			base.AI.SelectMaterials(list);
			return true;
		}
		if (base.Bot.GetMonsters().Count((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount < 2) < 2 || (base.Bot.MonsterZone[5] != null && base.Bot.MonsterZone[5].HasType(CardType.Link) && base.Bot.MonsterZone[5].LinkCount > 3) || (base.Bot.MonsterZone[6] != null && base.Bot.MonsterZone[6].HasType(CardType.Link) && base.Bot.MonsterZone[6].LinkCount > 3))
		{
			return false;
		}
		if ((base.Bot.HasInHand(30118811) && Count.CheckCard(30118811) && base.Bot.GetMonstersInMainZone().Count() < 5) || (base.Bot.HasInHand(3723262) && Count.CheckCard(3723262) && base.Bot.Graveyard.Any((ClientCard i) => i.HasRace(CardRace.Cyberse) && i.HasAttribute(CardAttribute.Dark)) && base.Bot.Graveyard.Count((ClientCard i) => i.HasRace(CardRace.Cyberse)) > 1 && base.Bot.GetMonstersInMainZone().Count() < 4) || (base.Bot.HasInHand(20938824) && Count.CheckCard(20938824) && Check_Maliss_March_Hare(CardLocation.Hand) && base.Bot.Graveyard.Count((ClientCard i) => i.HasRace(CardRace.Cyberse)) > 1 && base.Bot.GetMonstersInMainZone().Count() < 4))
		{
			List<ClientCard> list2 = (from card in base.Bot.GetMonsters()
				where card.IsFaceup() && card.Sequence > 4
				select card).ToList();
			list2.AddRange((from card in base.Bot.GetMonsters()
				where card.IsFaceup() && !card.HasType(CardType.Link)
				select card).ToList());
			base.AI.SelectMaterials(list2);
			return true;
		}
		return false;
	}

	private bool Effect_Haggard_Lizardose()
	{
		return base.Bot.Graveyard.Any((ClientCard i) => i.HasType(CardType.Monster) && i.Attack <= 2000);
	}

	private bool SP_Haggard_Lizardose()
	{
		List<ClientCard> list = (from i in (from i in base.Bot.GetMonsters()
				where i.IsFaceup() && (!i.HasType(CardType.Link) || i.LinkCount < 2)
				select i).ToList()
			group i by i.Id into i
			select i.First()).ToList();
		if (list.Count < 2)
		{
			return false;
		}
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821) && list.Any((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)))
		{
			List<ClientCard> list2 = list.Where((ClientCard i) => Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)).ToList();
			list2.AddRange(list.Where((ClientCard i) => !Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)));
			list2.AddRange(list.Where((ClientCard i) => !i.HasSetcode(447)));
			base.AI.SelectMaterials(list2);
			return true;
		}
		if (base.Bot.HasInExtra(59859086))
		{
			return false;
		}
		bool flag = false;
		flag = ((Count.CheckCard(91800273) || !Count.CheckCard(34267821)) ? (base.Bot.GetMonsters().Any((ClientCard i) => i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && i.BaseAttack <= 2000 && i.HasSetcode(447)) || base.Bot.Graveyard.Any((ClientCard i) => i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id) && i.BaseAttack <= 2000 && i.HasSetcode(447))) : base.Bot.Graveyard.Any((ClientCard i) => i.HasType(CardType.Monster) && i.BaseAttack <= 2000 && Count.CheckCardRemoved(i.Id) && Count.CheckCard(i.Id) && i.HasSetcode(447)));
		if (flag)
		{
			List<ClientCard> materials = list.Where((ClientCard i) => i.BaseAttack <= 2000 && Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)).ToList();
			materials.AddRange(list.Where((ClientCard i) => !materials.Contains(i)));
			base.AI.SelectMaterials(materials);
		}
		return flag;
	}

	private bool SP_Maliss_Link()
	{
		if ((base.Bot.GetMonsters().Any((ClientCard i) => i.HasType(CardType.Link) && i.LinkCount == 2) && base.Bot.GetMonsters().Any((ClientCard i) => !i.HasType(CardType.Link) && i.HasSetcode(447))) || (!Count.CheckCard(91800273) && Count.CheckCard(34267821) && (!base.Bot.HasInExtra(new int[2] { 52698008, 59859086 }) || !base.Bot.Graveyard.Any((ClientCard i) => i.HasRace(CardRace.Cyberse))) && (!base.Bot.HasInExtra(9763474) || !base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447) && i.HasType(CardType.Monster) && Count.CheckCardRemoved(i.Id)))))
		{
			List<ClientCard> list = (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && card.LinkCount == 2
				select card).ToList();
			foreach (ClientCard item in (from card in base.Bot.GetMonsters()
				where card != null && card.IsFaceup() && card.LinkCount < 2 && card.HasSetcode(447)
				select card).ToList())
			{
				if (list.Count == 2)
				{
					break;
				}
				if (item.LinkCount <= 2)
				{
					list.Add(item);
				}
			}
			base.AI.SelectMaterials(list);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_Link()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (base.Card.IsCode(95454996))
			{
				if (base.ActivateDescription == base.Util.GetStringId(base.Card.Id, 1))
				{
					Count.AddCard(base.Card.Id);
					return true;
				}
				if (!base.Bot.Graveyard.Any((ClientCard i) => i.HasSetcode(447) && Count.CheckCardRemoved(i.Id)))
				{
					base.Enemy.Graveyard.Count();
					_ = 3;
					Count.AddCard(base.Card.Id);
					return true;
				}
			}
			Count.AddCard(base.Card.Id);
			return true;
		}
		return Effect_Maliss_Removed(900);
	}

	private bool SP_Link_Decoder()
	{
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
		{
			if (base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447) && i.Level <= 4 && Count.CheckCardRemoved(i.Id)))
			{
				base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
					where i.HasSetcode(447) && i.Level <= 4 && Count.CheckCardRemoved(i.Id)
					select i).ToList());
				return true;
			}
			return false;
		}
		if (base.Bot.HasInHand(20938824) && Count.CheckCard(20938824) && base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447) && i.Level <= 4 && Count.CheckCardRemoved(i.Id)))
		{
			base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
				where i.HasSetcode(447) && i.Level <= 4 && Count.CheckCardRemoved(i.Id)
				select i).ToList());
			return true;
		}
		if (base.Bot.GetMonsters().Any((ClientCard i) => i.LinkCount < 3 && i.HasSetcode(447)) && base.Bot.GetMonsters().Count((ClientCard i) => i.LinkCount < 3) >= 3)
		{
			base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
				where i.LinkCount < 3 && i.HasSetcode(447)
				select i).ToList());
			return true;
		}
		if (base.Bot.HasInMonstersZone(52698008))
		{
			base.AI.SelectMaterials(52698008);
			return true;
		}
		if (base.Bot.HasInMonstersZone(30118811) && base.Bot.HasInHand(3723262))
		{
			base.AI.SelectMaterials(30118811);
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_in_the_Mirror()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.ActivateDescription == base.Util.GetStringId(93453053, 0))
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (base.Duel.Player == 1 && lastChainCard != null && lastChainCard.Controller == 1)
			{
				return lastChainCard.Location == CardLocation.MonsterZone;
			}
			return false;
		}
		return Effect_Maliss_Removed(0);
	}

	private bool SP_Maliss_Hearts_Crypter()
	{
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
		{
			if (base.Bot.HasInExtra(68059897) || base.Bot.GetMonsters().Count((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount < 2) < (base.Bot.GetMonsters().Any((ClientCard i) => i.HasType(CardType.Link) && i.LinkCount == 2) ? 1 : 3))
			{
				return false;
			}
			base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
				where !i.HasType(CardType.Link) || i.LinkCount < 2
				select i).ToList());
			return true;
		}
		if ((base.Bot.HasInMonstersZone(30342076) && base.Bot.GetMonsters().Count((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount <= 2) > 2) || base.Bot.GetMonsters().Count((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount <= 2) > 4)
		{
			base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
				where !i.HasType(CardType.Link) || i.LinkCount < 2
				select i).ToList());
			return true;
		}
		return false;
	}

	private bool SP_Maliss_White_Binder()
	{
		if (base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447) && i.LinkCount == 3 && Count.CheckCardRemoved(i.Id)))
		{
			base.AI.SelectMaterials((from i in base.Bot.GetMonsters()
				where (Count.CheckCardRemoved(i.Id) && i.HasSetcode(447)) || i.LinkCount < 3
				select i).ToList());
			return true;
		}
		return false;
	}

	private bool Effect_Maliss_Hearts_Crypter()
	{
		if (DefaultCheckWhetherCardIsNegated(base.Card))
		{
			return false;
		}
		if (base.Card.Location == CardLocation.MonsterZone)
		{
			if (base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && (i.HasType((CardType)917504) || i.IsFacedown())) > 0 && base.Duel.LastChainPlayer != 0)
			{
				Count.AddCard(base.Card.Id);
				return true;
			}
			return false;
		}
		return Effect_Maliss_Removed(900);
	}

	private bool SP_Firewall_Dragon()
	{
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
		{
			return false;
		}
		List<ClientCard> list = (from i in base.Bot.GetMonsters()
			where i.IsCode(95454996)
			select i).ToList();
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.Sequence > 4 && i.HasType(CardType.Link) && i.LinkCount <= 3
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.HasSetcode(447) && !i.HasType(CardType.Link)
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.HasSetcode(447) && i.HasType(CardType.Link)
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.Sequence < 5 && i.HasType(CardType.Link) && i.LinkCount <= 3
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where !i.HasType(CardType.Link)
			select i);
		if (list.Count > 3)
		{
			list = list.Take(3).ToList();
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool SP_Allied_Code_Talker_Ignister()
	{
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
		{
			return false;
		}
		if (base.Bot.GetMonsters().Count((ClientCard i) => i.LinkCount <= 3) < 3)
		{
			return false;
		}
		List<ClientCard> list = (from i in base.Bot.GetMonsters()
			where i.IsCode(95454996)
			select i).ToList();
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.Sequence > 4 && i.HasType(CardType.Link) && i.LinkCount <= 3
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.HasSetcode(447) && !i.HasType(CardType.Link)
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.HasSetcode(447) && i.HasType(CardType.Link)
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where i.Sequence < 5 && i.HasType(CardType.Link) && i.LinkCount <= 3
			select i);
		list.AddRange(from i in base.Bot.GetMonsters()
			where !i.HasType(CardType.Link)
			select i);
		if (list.Count > 3)
		{
			list = list.Take(3).ToList();
		}
		base.AI.SelectMaterials(list);
		return true;
	}

	private bool Effect_Allied_Code_Talker_Ignister()
	{
		if (base.ActivateDescription == base.Util.GetStringId(39138610, 1) && base.Duel.LastChainPlayer == 1)
		{
			if (base.Card.Sequence > 4)
			{
				return base.Bot.GetMonsters().Any((ClientCard i) => i.Sequence < 3 && (!i.HasType(CardType.Link) || (i.LinkCount <= 3 && Count.CheckCard(i.Id))));
			}
			return base.Bot.GetMonsters().Any((ClientCard i) => i.Sequence < 5 && (i.Sequence - base.Card.Sequence == 1 || base.Card.Sequence - i.Sequence == 1) && (!i.HasType(CardType.Link) || (i.LinkCount <= 3 && Count.CheckCard(i.Id))));
		}
		return true;
	}

	private bool Effect_Mereologic_Aggregator()
	{
		if (base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && (i.HasType((CardType)917504) || i.IsFacedown())) > 0)
		{
			ClientCard lastChainCard = base.Util.GetLastChainCard();
			if (lastChainCard != null && lastChainCard.Controller == 1 && (lastChainCard.Location == CardLocation.MonsterZone || lastChainCard.Location == CardLocation.SpellZone))
			{
				base.AI.SelectCard(lastChainCard);
			}
			Count.AddCard(base.Card.Id);
			return true;
		}
		return false;
	}

	private bool Effect_Firewall_Dragon()
	{
		if (!Count.CheckCard(91800273) && Count.CheckCard(34267821))
		{
			return false;
		}
		if (base.Duel.Player != 0)
		{
			if (base.Enemy.GetMonsters().Count((ClientCard i) => !i.IsShouldNotBeTarget() && i.IsFaceup()) + base.Enemy.GetSpells().Count((ClientCard i) => !i.IsShouldNotBeTarget() && i.HasType((CardType)917504)) > 0)
			{
				return base.Duel.LastChainPlayer != 0;
			}
			return false;
		}
		return true;
	}

	private bool Summon_Backup_Ignister()
	{
		if (base.Bot.GetMonsters().Any((ClientCard i) => i.HasType(CardType.Link)))
		{
			return false;
		}
		Count.AddSummon();
		return true;
	}

	private bool SP_Transcode_Talker()
	{
		if (base.Bot.GetMonsters().Any((ClientCard i) => i.HasSetcode(447)))
		{
			return false;
		}
		if (!base.Bot.GetMonsters().Any((ClientCard i) => !i.HasType(CardType.Link) || i.LinkCount < 2))
		{
			return false;
		}
		if (!base.Bot.GetMonsters().Any((ClientCard i) => i.HasType(CardType.Link) && i.LinkCount == 2))
		{
			return false;
		}
		List<ClientCard> materials = (from i in base.Bot.GetMonsters()
			where i.HasType(CardType.Link) && i.LinkCount == 2
			select i).ToList();
		materials.AddRange(from i in base.Bot.GetMonsters()
			where i.IsCode(30342076)
			select i);
		materials.AddRange(from i in base.Bot.GetMonsters()
			where !i.HasType(CardType.Link) || (i.LinkCount < 2 && i.Sequence == ((materials[0].Sequence <= 4) ? (materials[0].Sequence + 1) : ((materials[0].Sequence == 5) ? 1 : 3)))
			select i);
		materials.AddRange(from i in base.Bot.GetMonsters()
			where !i.HasType(CardType.Link) || i.LinkCount < 2
			select i);
		if (materials.Count > 2)
		{
			materials = materials.Take(2).ToList();
		}
		base.AI.SelectMaterials(materials);
		return true;
	}

	private bool Effect_Wizard_Ignister()
	{
		return base.Card.Location == CardLocation.Hand;
	}

	private bool GoToBattlePhase()
	{
		if (!base.Enemy.GetMonsters().Any((ClientCard i) => i.IsDefense()) && base.Util.GetTotalAttackingMonsterAttack(0) >= base.Enemy.LifePoints + base.Util.GetTotalAttackingMonsterAttack(1))
		{
			return true;
		}
		return false;
	}
}
