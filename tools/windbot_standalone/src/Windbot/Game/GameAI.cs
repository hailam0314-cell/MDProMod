using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindBot.Game.AI;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game;

public class GameAI
{
	private Dialogs _dialogs;

	private Dictionary<int, int> _activatedCards;

	private CardSelector m_materialSelector;

	private int m_materialSelectorHint;

	private int m_place;

	private int m_option;

	private int m_number;

	private int m_announce;

	private int m_yesno;

	private IList<CardAttribute> m_attributes = new List<CardAttribute>();

	private IList<CardSelector> m_selector = new List<CardSelector>();

	private IList<CardPosition> m_position = new List<CardPosition>();

	private int m_selector_pointer = -1;

	private IList<CardRace> m_races = new List<CardRace>();

	public GameClient Game { get; private set; }

	public Duel Duel { get; private set; }

	public Executor Executor { get; set; }

	public GameAI(GameClient game, Duel duel)
	{
		Game = game;
		Duel = duel;
		_dialogs = new Dialogs(game);
		_activatedCards = new Dictionary<int, int>();
	}

	private void CheckSurrender()
	{
		foreach (CardExecutor executor in Executor.Executors)
		{
			if (executor.Type == ExecutorType.Surrender && executor.Func())
			{
				_dialogs.SendSurrender();
				Game.Surrender();
			}
		}
	}

	public void OnRetry()
	{
		_dialogs.SendSorry();
	}

	public void OnDeckError(string card)
	{
		_dialogs.SendDeckSorry(card);
		Thread.Sleep(1000);
		_dialogs.SendSurrender();
		Game.Connection.Close();
	}

	public void OnJoinGame()
	{
		_dialogs.SendWelcome();
	}

	public void OnStart()
	{
		_dialogs.SendDuelStart();
	}

	public void SendCustomChat(int index, params object[] opts)
	{
		_dialogs.SendCustomChat(index, opts);
	}

	public int OnRockPaperScissors()
	{
		return Executor.OnRockPaperScissors();
	}

	public bool OnSelectHand()
	{
		return Executor.OnSelectHand();
	}

	public void OnDraw(int player)
	{
		Executor.OnDraw(player);
	}

	public void OnNewTurn()
	{
		_activatedCards.Clear();
		Executor.OnNewTurn();
	}

	public void OnNewPhase()
	{
		m_selector.Clear();
		m_position.Clear();
		m_selector_pointer = -1;
		m_materialSelector = null;
		m_materialSelectorHint = 0;
		m_option = -1;
		m_yesno = -1;
		m_announce = 0;
		m_place = 0;
		if (Duel.Player == 0 && Duel.Phase == DuelPhase.Draw)
		{
			_dialogs.SendNewTurn();
		}
		Executor.OnNewPhase();
		CheckSurrender();
	}

	public void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
		Executor.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
	}

	public void OnDirectAttack(ClientCard card)
	{
		_dialogs.SendOnDirectAttack(card.Name);
		CheckSurrender();
	}

	public void OnChaining(ClientCard card, int player)
	{
		Executor.OnChaining(player, card);
	}

	public void OnChainSolved(int chainIndex)
	{
		Executor.OnChainSolved(chainIndex);
	}

	public void OnSpSummoned()
	{
		Executor.OnSpSummoned();
	}

	public void OnChainEnd()
	{
		m_selector.Clear();
		m_selector_pointer = -1;
		Executor.OnChainEnd();
		CheckSurrender();
	}

	public void OnPlayerHint(int player, int hintType, int description)
	{
		Executor.OnPlayerHint(player, hintType, description);
	}

	public void OnHintZone(int player, int zone)
	{
		Executor.OnHintZone(player, zone);
	}

	public void OnReceivingAnnouce(int player, int data)
	{
		Executor.OnReceivingAnnouce(player, data);
	}

	public BattlePhaseAction OnSelectBattleCmd(BattlePhase battle)
	{
		foreach (CardExecutor executor in Executor.Executors)
		{
			if (executor.Type == ExecutorType.GoToMainPhase2 && battle.CanMainPhaseTwo && executor.Func())
			{
				return ToMainPhase2();
			}
			if (executor.Type == ExecutorType.GoToEndPhase && battle.CanEndPhase && executor.Func())
			{
				return ToEndPhase();
			}
			for (int i = 0; i < battle.ActivableCards.Count; i++)
			{
				ClientCard clientCard = battle.ActivableCards[i];
				if (ShouldExecute(executor, clientCard, ExecutorType.Activate, battle.ActivableDescs[i]))
				{
					_dialogs.SendChaining(clientCard.Name);
					return new BattlePhaseAction(BattlePhaseAction.BattleAction.Activate, clientCard.ActionIndex);
				}
			}
		}
		List<ClientCard> list = new List<ClientCard>(battle.AttackableCards);
		list.Sort(CardContainer.CompareCardAttack);
		list.Reverse();
		List<ClientCard> list2 = new List<ClientCard>(Duel.Fields[1].GetMonsters());
		list2.Sort(CardContainer.CompareDefensePower);
		list2.Reverse();
		ClientCard clientCard2 = Executor.OnSelectAttacker(list, list2);
		if (clientCard2 != null && list.Contains(clientCard2))
		{
			list.Remove(clientCard2);
			list.Insert(0, clientCard2);
		}
		BattlePhaseAction battlePhaseAction = Executor.OnBattle(list, list2);
		if (battlePhaseAction != null)
		{
			return battlePhaseAction;
		}
		if (list.Count == 0)
		{
			return ToMainPhase2();
		}
		if (list2.Count == 0)
		{
			ClientCard attacker = list[list.Count - 1];
			return Attack(attacker, null);
		}
		for (int j = 0; j < list.Count; j++)
		{
			ClientCard clientCard3 = list[j];
			clientCard3.IsLastAttacker = j == list.Count - 1;
			battlePhaseAction = Executor.OnSelectAttackTarget(clientCard3, list2);
			if (battlePhaseAction != null)
			{
				return battlePhaseAction;
			}
		}
		if (!battle.CanMainPhaseTwo)
		{
			return Attack(list[0], (list2.Count == 0) ? null : list2[0]);
		}
		return ToMainPhase2();
	}

	public IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		IList<ClientCard> list = Executor.OnSelectCard(cards, min, max, hint, cancelable);
		if (list != null)
		{
			return list;
		}
		if (hint == 509 && min == 1 && max > min)
		{
			list = Executor.OnSelectPendulumSummon(cards, max);
			if (list != null)
			{
				return list;
			}
		}
		CardSelector cardSelector = null;
		if (hint != 511 && hint != 512 && hint != 513 && hint != 533)
		{
			cardSelector = ((m_materialSelector == null || hint != m_materialSelectorHint) ? GetSelectedCards() : m_materialSelector);
		}
		else if (m_materialSelector != null)
		{
			cardSelector = m_materialSelector;
		}
		else
		{
			if (hint == 511)
			{
				list = Executor.OnSelectFusionMaterial(cards, min, max);
			}
			if (hint == 512)
			{
				list = Executor.OnSelectSynchroMaterial(cards, 0, min, max);
			}
			if (hint == 513)
			{
				list = Executor.OnSelectXyzMaterial(cards, min, max);
			}
			if (hint == 533)
			{
				list = Executor.OnSelectLinkMaterial(cards, min, max);
			}
			if (list != null)
			{
				return list;
			}
			cardSelector = GetSelectedCards();
		}
		if (cardSelector != null)
		{
			return cardSelector.Select(cards, min, max);
		}
		IList<ClientCard> list2 = new List<ClientCard>();
		if (hint == 549 && cancelable)
		{
			return list2;
		}
		if (cards.Count >= min)
		{
			for (int i = 0; i < min; i++)
			{
				list2.Add(cards[i]);
			}
		}
		return list2;
	}

	public int OnSelectChain(IList<ClientCard> cards, IList<int> descs, IList<bool> forces, int timing = -1)
	{
		Executor.OnSelectChain(cards);
		foreach (CardExecutor executor in Executor.Executors)
		{
			for (int i = 0; i < cards.Count; i++)
			{
				ClientCard clientCard = cards[i];
				if (ShouldExecute(executor, clientCard, ExecutorType.Activate, descs[i], timing))
				{
					_dialogs.SendChaining(clientCard.Name);
					return i;
				}
			}
		}
		for (int j = 0; j < forces.Count; j++)
		{
			if (forces[j])
			{
				_dialogs.SendChaining(cards[j].Name);
				return j;
			}
		}
		return -1;
	}

	public IList<int> OnSelectCounter(int type, int quantity, IList<ClientCard> cards, IList<int> counters)
	{
		int[] array = new int[counters.Count];
		int num = 0;
		while (quantity > 0)
		{
			if (counters[num] >= quantity)
			{
				array[num] = quantity;
				quantity = 0;
			}
			else
			{
				array[num] = counters[num];
				quantity -= counters[num];
			}
			num++;
		}
		return array;
	}

	public IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
	{
		IList<ClientCard> list = Executor.OnCardSorting(cards);
		if (list != null)
		{
			return list;
		}
		list = new List<ClientCard>();
		return cards.ToList();
	}

	public bool OnSelectEffectYn(ClientCard card, int desc)
	{
		foreach (CardExecutor executor in Executor.Executors)
		{
			if (ShouldExecute(executor, card, ExecutorType.Activate, desc))
			{
				return true;
			}
		}
		return false;
	}

	public MainPhaseAction OnSelectIdleCmd(MainPhase main)
	{
		CheckSurrender();
		foreach (CardExecutor executor in Executor.Executors)
		{
			if (executor.Type == ExecutorType.GoToEndPhase && main.CanEndPhase && executor.Func())
			{
				_dialogs.SendEndTurn();
				return new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase);
			}
			if (executor.Type == ExecutorType.GoToBattlePhase && main.CanBattlePhase && executor.Func())
			{
				return new MainPhaseAction(MainPhaseAction.MainAction.ToBattlePhase);
			}
			for (int i = 0; i < main.ActivableCards.Count; i++)
			{
				ClientCard clientCard = main.ActivableCards[i];
				if (ShouldExecute(executor, clientCard, ExecutorType.Activate, main.ActivableDescs[i]))
				{
					_dialogs.SendActivate(clientCard.Name);
					return new MainPhaseAction(MainPhaseAction.MainAction.Activate, clientCard.ActionActivateIndex[main.ActivableDescs[i]]);
				}
			}
			foreach (ClientCard monsterSetableCard in main.MonsterSetableCards)
			{
				if (ShouldExecute(executor, monsterSetableCard, ExecutorType.MonsterSet))
				{
					_dialogs.SendSetMonster();
					return new MainPhaseAction(MainPhaseAction.MainAction.SetMonster, monsterSetableCard.ActionIndex);
				}
			}
			foreach (ClientCard reposableCard in main.ReposableCards)
			{
				if (ShouldExecute(executor, reposableCard, ExecutorType.Repos))
				{
					return new MainPhaseAction(MainPhaseAction.MainAction.Repos, reposableCard.ActionIndex);
				}
			}
			foreach (ClientCard specialSummonableCard in main.SpecialSummonableCards)
			{
				if (ShouldExecute(executor, specialSummonableCard, ExecutorType.SpSummon))
				{
					_dialogs.SendSummon(specialSummonableCard.Name);
					return new MainPhaseAction(MainPhaseAction.MainAction.SpSummon, specialSummonableCard.ActionIndex);
				}
			}
			foreach (ClientCard summonableCard in main.SummonableCards)
			{
				if (ShouldExecute(executor, summonableCard, ExecutorType.Summon))
				{
					_dialogs.SendSummon(summonableCard.Name);
					return new MainPhaseAction(MainPhaseAction.MainAction.Summon, summonableCard.ActionIndex);
				}
				if (ShouldExecute(executor, summonableCard, ExecutorType.SummonOrSet))
				{
					if (main.MonsterSetableCards.Contains(summonableCard) && Executor.OnSelectMonsterSummonOrSet(summonableCard))
					{
						_dialogs.SendSetMonster();
						return new MainPhaseAction(MainPhaseAction.MainAction.SetMonster, summonableCard.ActionIndex);
					}
					_dialogs.SendSummon(summonableCard.Name);
					return new MainPhaseAction(MainPhaseAction.MainAction.Summon, summonableCard.ActionIndex);
				}
			}
			foreach (ClientCard spellSetableCard in main.SpellSetableCards)
			{
				if (ShouldExecute(executor, spellSetableCard, ExecutorType.SpellSet))
				{
					return new MainPhaseAction(MainPhaseAction.MainAction.SetSpell, spellSetableCard.ActionIndex);
				}
			}
		}
		if (main.CanBattlePhase && Duel.Fields[0].HasAttackingMonster())
		{
			return new MainPhaseAction(MainPhaseAction.MainAction.ToBattlePhase);
		}
		_dialogs.SendEndTurn();
		return new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase);
	}

	public int OnSelectOption(IList<int> options)
	{
		int num = Executor.OnSelectOption(options);
		if (num != -1)
		{
			return num;
		}
		if (m_option != -1 && m_option < options.Count)
		{
			return m_option;
		}
		return 0;
	}

	public int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		int place = m_place;
		m_place = 0;
		int num = Executor.OnSelectPlace(cardId, player, location, available);
		if ((num & available) > 0)
		{
			return num & available;
		}
		if ((place & available) > 0)
		{
			return place & available;
		}
		return 0;
	}

	public CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		CardPosition selectedPosition = GetSelectedPosition();
		CardPosition cardPosition = Executor.OnSelectPosition(cardId, positions);
		if (positions.Contains(cardPosition))
		{
			return cardPosition;
		}
		if (positions.Contains(selectedPosition))
		{
			return selectedPosition;
		}
		return positions[0];
	}

	public IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, int hint, bool mode)
	{
		IList<ClientCard> list = Executor.OnSelectSum(cards, sum, min, max, hint, mode);
		if (list != null)
		{
			return list;
		}
		if (hint == 500 || hint == 512)
		{
			if (m_materialSelector != null)
			{
				list = m_materialSelector.Select(cards, min, max);
			}
			else
			{
				switch (hint)
				{
				case 512:
					list = Executor.OnSelectSynchroMaterial(cards, sum, min, max);
					break;
				case 500:
					list = Executor.OnSelectRitualTribute(cards, sum, min, max);
					break;
				}
			}
			if (list != null)
			{
				int num = 0;
				int num2 = 0;
				foreach (ClientCard item in list)
				{
					num += item.OpParam1;
					num2 += ((item.OpParam2 != 0) ? item.OpParam2 : item.OpParam1);
				}
				if ((mode && (num == sum || num2 == sum)) || (!mode && (num >= sum || num2 >= sum)))
				{
					return list;
				}
			}
		}
		if (mode)
		{
			if (sum == 0 && min == 0)
			{
				return new List<ClientCard>();
			}
			if (min <= 1)
			{
				foreach (ClientCard card in cards)
				{
					if (card.OpParam2 == sum)
					{
						return new ClientCard[1] { card };
					}
				}
				foreach (ClientCard card2 in cards)
				{
					if (card2.OpParam1 == sum)
					{
						return new ClientCard[1] { card2 };
					}
				}
			}
			int num3 = 0;
			int num4 = 0;
			foreach (ClientCard card3 in cards)
			{
				num3 += card3.OpParam1;
				num4 += ((card3.OpParam2 != 0) ? card3.OpParam2 : card3.OpParam1);
			}
			if (num3 == sum || num4 == sum)
			{
				return cards;
			}
			for (int i = ((min <= 1) ? 2 : min); i <= max && i <= cards.Count; i++)
			{
				foreach (IEnumerable<ClientCard> combination in cards.GetCombinations(i))
				{
					Logger.DebugWriteLine("--");
					num3 = 0;
					num4 = 0;
					foreach (ClientCard item2 in combination)
					{
						num3 += item2.OpParam1;
						num4 += ((item2.OpParam2 != 0) ? item2.OpParam2 : item2.OpParam1);
					}
					if (num3 == sum || num4 == sum)
					{
						return combination.ToList();
					}
				}
			}
		}
		else
		{
			if (min <= 1)
			{
				foreach (ClientCard card4 in cards)
				{
					if (card4.OpParam2 >= sum)
					{
						return new ClientCard[1] { card4 };
					}
				}
				foreach (ClientCard card5 in cards)
				{
					if (card5.OpParam1 >= sum)
					{
						return new ClientCard[1] { card5 };
					}
				}
			}
			for (int j = ((min <= 1) ? 2 : min); j <= max && j <= cards.Count; j++)
			{
				foreach (IEnumerable<ClientCard> combination2 in cards.GetCombinations(j))
				{
					Logger.DebugWriteLine("----");
					int num5 = 0;
					int num6 = 0;
					foreach (ClientCard item3 in combination2)
					{
						num5 += item3.OpParam1;
						num6 += ((item3.OpParam2 != 0) ? item3.OpParam2 : item3.OpParam1);
					}
					if (num5 >= sum || num6 >= sum)
					{
						return combination2.ToList();
					}
				}
			}
		}
		Logger.WriteErrorLine("Fail to select sum.");
		return new List<ClientCard>();
	}

	public IList<ClientCard> OnSelectTribute(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		List<ClientCard> list = new List<ClientCard>();
		list.AddRange(cards);
		list.Sort(CardContainer.CompareCardAttack);
		IList<ClientCard> list2 = new List<ClientCard>();
		for (int i = 0; i < min && i < list.Count; i++)
		{
			list2.Add(list[i]);
		}
		return list2;
	}

	public bool OnSelectYesNo(int desc)
	{
		if (m_yesno != -1)
		{
			return m_yesno > 0;
		}
		return Executor.OnSelectYesNo(desc);
	}

	public bool OnSelectBattleReplay()
	{
		return Executor.OnSelectBattleReplay();
	}

	public int OnAnnounceCard(IList<int> avail)
	{
		int num = Executor.OnAnnounceCard(avail);
		if (avail.Contains(num))
		{
			return num;
		}
		if (avail.Contains(m_announce))
		{
			return m_announce;
		}
		if (m_announce > 0)
		{
			Logger.WriteErrorLine("Pre-announced card cant be used: " + m_announce);
		}
		return avail[0];
	}

	public void SelectCard(ClientCard card)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(card));
	}

	public void SelectCard(IList<ClientCard> cards)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(cards));
	}

	public void SelectCard(int cardId)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(cardId));
	}

	public void SelectCard(IList<int> ids)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(ids));
	}

	public void SelectCard(params int[] ids)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(ids));
	}

	public void SelectCard(CardLocation loc)
	{
		m_selector_pointer = m_selector.Count();
		m_selector.Add(new CardSelector(loc));
	}

	public void SelectNextCard(ClientCard card)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(card));
	}

	public void SelectNextCard(IList<ClientCard> cards)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(cards));
	}

	public void SelectNextCard(int cardId)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(cardId));
	}

	public void SelectNextCard(IList<int> ids)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(ids));
	}

	public void SelectNextCard(params int[] ids)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(ids));
	}

	public void SelectNextCard(CardLocation loc)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectNextCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(loc));
	}

	public void SelectThirdCard(ClientCard card)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(card));
	}

	public void SelectThirdCard(IList<ClientCard> cards)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(cards));
	}

	public void SelectThirdCard(int cardId)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(cardId));
	}

	public void SelectThirdCard(IList<int> ids)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(ids));
	}

	public void SelectThirdCard(params int[] ids)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(ids));
	}

	public void SelectThirdCard(CardLocation loc)
	{
		if (m_selector_pointer == -1)
		{
			Logger.WriteErrorLine("Error: Call SelectThirdCard() before SelectCard()");
			m_selector_pointer = 0;
		}
		m_selector.Insert(m_selector_pointer, new CardSelector(loc));
	}

	public void SelectMaterials(ClientCard card, int hint = 0)
	{
		m_materialSelector = new CardSelector(card);
		m_materialSelectorHint = hint;
	}

	public void SelectMaterials(IList<ClientCard> cards, int hint = 0)
	{
		m_materialSelector = new CardSelector(cards);
		m_materialSelectorHint = hint;
	}

	public void SelectMaterials(int cardId, int hint = 0)
	{
		m_materialSelector = new CardSelector(cardId);
		m_materialSelectorHint = hint;
	}

	public void SelectMaterials(IList<int> ids, int hint = 0)
	{
		m_materialSelector = new CardSelector(ids);
		m_materialSelectorHint = hint;
	}

	public void SelectMaterials(CardLocation loc, int hint = 0)
	{
		m_materialSelector = new CardSelector(loc);
		m_materialSelectorHint = hint;
	}

	public void CleanSelectMaterials()
	{
		m_materialSelector = null;
		m_materialSelectorHint = 0;
	}

	public bool HaveSelectedCards()
	{
		if (m_selector.Count <= 0)
		{
			return m_materialSelector != null;
		}
		return true;
	}

	public CardSelector GetSelectedCards()
	{
		CardSelector result = null;
		if (m_selector.Count > 0)
		{
			result = m_selector[m_selector.Count - 1];
			m_selector.RemoveAt(m_selector.Count - 1);
		}
		return result;
	}

	public CardPosition GetSelectedPosition()
	{
		CardPosition result = CardPosition.FaceUpAttack;
		if (m_position.Count > 0)
		{
			result = m_position[0];
			m_position.RemoveAt(0);
		}
		return result;
	}

	public void SelectPosition(CardPosition pos)
	{
		m_position.Add(pos);
	}

	public void SelectPlace(int zones)
	{
		m_place = zones;
	}

	public void SelectOption(int opt)
	{
		m_option = opt;
	}

	public void SelectNumber(int number)
	{
		m_number = number;
	}

	public void SelectAttribute(CardAttribute attribute)
	{
		m_attributes.Clear();
		m_attributes.Add(attribute);
	}

	public void SelectAttributes(CardAttribute[] attributes)
	{
		m_attributes.Clear();
		foreach (CardAttribute item in attributes)
		{
			m_attributes.Add(item);
		}
	}

	public void SelectRace(CardRace race)
	{
		m_races.Clear();
		m_races.Add(race);
	}

	public void SelectRaces(CardRace[] races)
	{
		m_races.Clear();
		foreach (CardRace item in races)
		{
			m_races.Add(item);
		}
	}

	public void SelectAnnounceID(int id)
	{
		m_announce = id;
	}

	public void SelectYesNo(bool opt)
	{
		m_yesno = (opt ? 1 : 0);
	}

	public int OnAnnounceNumber(IList<int> numbers)
	{
		if (numbers.Contains(m_number))
		{
			return numbers.IndexOf(m_number);
		}
		return Program.Rand.Next(0, numbers.Count);
	}

	public virtual IList<CardAttribute> OnAnnounceAttrib(int count, IList<CardAttribute> attributes)
	{
		IList<CardAttribute> list = m_attributes.Where(attributes.Contains).ToList();
		if (list.Count > 0)
		{
			return list;
		}
		return attributes;
	}

	public virtual IList<CardRace> OnAnnounceRace(int count, IList<CardRace> races)
	{
		IList<CardRace> list = m_races.Where(races.Contains).ToList();
		if (list.Count > 0)
		{
			return list;
		}
		return races;
	}

	public BattlePhaseAction Attack(ClientCard attacker, ClientCard defender)
	{
		Executor.SetCard(ExecutorType.Summon, attacker, -1);
		if (defender != null)
		{
			string defender2 = defender.Name ?? "monster";
			attacker.ShouldDirectAttack = false;
			_dialogs.SendAttack(attacker.Name, defender2);
			SelectCard(defender);
		}
		else
		{
			attacker.ShouldDirectAttack = true;
			_dialogs.SendDirectAttack(attacker.Name);
		}
		return new BattlePhaseAction(BattlePhaseAction.BattleAction.Attack, attacker.ActionIndex);
	}

	public BattlePhaseAction ToEndPhase()
	{
		_dialogs.SendEndTurn();
		return new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase);
	}

	public BattlePhaseAction ToMainPhase2()
	{
		return new BattlePhaseAction(BattlePhaseAction.BattleAction.ToMainPhaseTwo);
	}

	private bool ShouldExecute(CardExecutor exec, ClientCard card, ExecutorType type, int desc = -1, int timing = -1)
	{
		Executor.SetCard(type, card, desc, timing);
		if (card.Id != 0 && type == ExecutorType.Activate)
		{
			if (_activatedCards.ContainsKey(card.Id) && _activatedCards[card.Id] >= 9)
			{
				return false;
			}
			if (!Executor.OnPreActivate(card))
			{
				return false;
			}
		}
		bool flag = card != null && exec.Type == type && (exec.CardId == -1 || exec.CardId == card.Id) && (exec.Func == null || exec.Func());
		if (card.Id != 0 && type == ExecutorType.Activate && flag)
		{
			int num = ((!card.IsDisabled()) ? 1 : 3);
			if (!_activatedCards.ContainsKey(card.Id))
			{
				_activatedCards.Add(card.Id, num);
			}
			else
			{
				_activatedCards[card.Id] += num;
			}
		}
		return flag;
	}
}
