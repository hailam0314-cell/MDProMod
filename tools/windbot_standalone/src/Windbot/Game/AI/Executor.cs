using System;
using System.Collections.Generic;
using System.Linq;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI;

public abstract class Executor
{
	public string Deck { get; set; }

	public Duel Duel { get; private set; }

	public IList<CardExecutor> Executors { get; private set; }

	public GameAI AI { get; private set; }

	public AIUtil Util { get; private set; }

	protected ExecutorType Type { get; private set; }

	protected ClientCard Card { get; private set; }

	protected int ActivateDescription { get; private set; }

	protected int CurrentTiming { get; private set; }

	protected ClientField Bot { get; private set; }

	protected ClientField Enemy { get; private set; }

	protected Executor(GameAI ai, Duel duel)
	{
		Duel = duel;
		AI = ai;
		Util = new AIUtil(duel);
		Executors = new List<CardExecutor>();
		Bot = Duel.Fields[0];
		Enemy = Duel.Fields[1];
	}

	public virtual int OnRockPaperScissors()
	{
		return Program.Rand.Next(1, 4);
	}

	public virtual bool OnSelectHand()
	{
		return Program.Rand.Next(2) > 0;
	}

	public virtual BattlePhaseAction OnBattle(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		return null;
	}

	public virtual ClientCard OnSelectAttacker(IList<ClientCard> attackers, IList<ClientCard> defenders)
	{
		return null;
	}

	public virtual BattlePhaseAction OnSelectAttackTarget(ClientCard attacker, IList<ClientCard> defenders)
	{
		return null;
	}

	public virtual bool OnPreBattleBetween(ClientCard attacker, ClientCard defender)
	{
		return true;
	}

	public virtual bool OnPreActivate(ClientCard card)
	{
		return true;
	}

	public virtual void OnChaining(int player, ClientCard card)
	{
	}

	public virtual void OnChainSolved(int chainIndex)
	{
	}

	public virtual void OnChainEnd()
	{
	}

	public virtual void OnReceivingAnnouce(int player, int data)
	{
	}

	public virtual void OnNewPhase()
	{
	}

	public virtual void OnNewTurn()
	{
	}

	public virtual void OnDraw(int player)
	{
	}

	public virtual void OnPlayerHint(int player, int hintType, int description)
	{
	}

	public virtual void OnHintZone(int player, int zone)
	{
	}

	public virtual void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
	{
	}

	public virtual IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, int hint, bool mode)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectFusionMaterial(IList<ClientCard> cards, int min, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectSynchroMaterial(IList<ClientCard> cards, int sum, int min, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectXyzMaterial(IList<ClientCard> cards, int min, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectLinkMaterial(IList<ClientCard> cards, int min, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectRitualTribute(IList<ClientCard> cards, int sum, int min, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnSelectPendulumSummon(IList<ClientCard> cards, int max)
	{
		return null;
	}

	public virtual IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
	{
		return null;
	}

	public virtual void OnSelectChain(IList<ClientCard> cards)
	{
	}

	public virtual bool OnSelectYesNo(int desc)
	{
		return true;
	}

	public virtual int OnSelectOption(IList<int> options)
	{
		return -1;
	}

	public virtual int OnSelectPlace(int cardId, int player, CardLocation location, int available)
	{
		return 0;
	}

	public virtual CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
	{
		return (CardPosition)0;
	}

	public virtual bool OnSelectBattleReplay()
	{
		return false;
	}

	public virtual bool OnSelectMonsterSummonOrSet(ClientCard card)
	{
		return false;
	}

	public virtual int OnAnnounceCard(IList<int> avail)
	{
		return 0;
	}

	public virtual void OnSpSummoned()
	{
	}

	public void SetCard(ExecutorType type, ClientCard card, int description, int timing = -1)
	{
		Type = type;
		Card = card;
		ActivateDescription = description;
		CurrentTiming = timing;
	}

	public void AddExecutor(ExecutorType type, int cardId, Func<bool> func)
	{
		Executors.Add(new CardExecutor(type, cardId, func));
	}

	public void AddExecutor(ExecutorType type, int cardId)
	{
		Executors.Add(new CardExecutor(type, cardId, null));
	}

	public void AddExecutor(ExecutorType type, Func<bool> func)
	{
		Executors.Add(new CardExecutor(type, -1, func));
	}

	public void AddExecutor(ExecutorType type)
	{
		Executors.Add(new CardExecutor(type, -1, DefaultNoExecutor));
	}

	private bool DefaultNoExecutor()
	{
		return Executors.All((CardExecutor exec) => exec.Type != Type || exec.CardId != Card.Id);
	}
}
