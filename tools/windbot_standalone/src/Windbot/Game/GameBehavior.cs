using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using WindBot.Game.AI;
using YGOSharp.Network;
using YGOSharp.Network.Enums;
using YGOSharp.Network.Utils;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game;

public class GameBehavior
{
	private GameAI _ai;

	private IDictionary<StocMessage, Action<BinaryReader>> _packets;

	private IDictionary<GameMessage, Action<BinaryReader>> _messages;

	private Room _room;

	private Duel _duel;

	private int _hand;

	private bool _debug;

	private int _select_hint;

	private GameMessage _lastMessage;

	public GameClient Game { get; private set; }

	public YGOClient Connection { get; private set; }

	public Deck Deck { get; private set; }

	// MDProMod: the duel host reads the AI's view of the duel directly.
	public Duel Duel => _duel;

	public GameBehavior(GameClient game)
	{
		Game = game;
		Connection = game.Connection;
		_hand = game.Hand;
		_debug = game.Debug;
		_packets = new Dictionary<StocMessage, Action<BinaryReader>>();
		_messages = new Dictionary<GameMessage, Action<BinaryReader>>();
		RegisterPackets();
		_room = new Room();
		_duel = new Duel();
		_ai = new GameAI(Game, _duel);
		_ai.Executor = DecksManager.Instantiate(_ai, _duel);
		Deck = WindBot.Game.Deck.Load(Game.DeckFile ?? _ai.Executor.Deck);
		_select_hint = 0;
	}

	public int GetLocalPlayer(int player)
	{
		if (!_duel.IsFirst)
		{
			return 1 - player;
		}
		return player;
	}

	public void OnPacket(BinaryReader packet)
	{
		StocMessage stocMessage = (StocMessage)packet.ReadByte();
		if (stocMessage == StocMessage.GameMsg)
		{
			GameMessage gameMessage = (GameMessage)packet.ReadByte();
			if (_messages.ContainsKey(gameMessage))
			{
				_messages[gameMessage](packet);
			}
			_lastMessage = gameMessage;
		}
		else if (_packets.ContainsKey(stocMessage))
		{
			_packets[stocMessage](packet);
		}
	}

	private void RegisterPackets()
	{
		_packets.Add(StocMessage.JoinGame, OnJoinGame);
		_packets.Add(StocMessage.TypeChange, OnTypeChange);
		_packets.Add(StocMessage.HsPlayerEnter, OnPlayerEnter);
		_packets.Add(StocMessage.HsPlayerChange, OnPlayerChange);
		_packets.Add(StocMessage.SelectHand, OnSelectHand);
		_packets.Add(StocMessage.SelectTp, OnSelectTp);
		_packets.Add(StocMessage.TimeLimit, OnTimeLimit);
		_packets.Add(StocMessage.Replay, OnReplay);
		_packets.Add(StocMessage.DuelEnd, OnDuelEnd);
		_packets.Add(StocMessage.Chat, OnChat);
		_packets.Add(StocMessage.ChangeSide, OnChangeSide);
		_packets.Add(StocMessage.ErrorMsg, OnErrorMsg);
		_packets.Add(StocMessage.TeammateSurrender, OnTeammateSurrender);
		_messages.Add(GameMessage.Retry, OnRetry);
		_messages.Add(GameMessage.Start, OnStart);
		_messages.Add(GameMessage.Hint, OnHint);
		_messages.Add(GameMessage.Win, OnWin);
		_messages.Add(GameMessage.Draw, OnDraw);
		_messages.Add(GameMessage.ShuffleDeck, OnShuffleDeck);
		_messages.Add(GameMessage.ShuffleHand, OnShuffleHand);
		_messages.Add(GameMessage.ShuffleExtra, OnShuffleExtra);
		_messages.Add(GameMessage.ShuffleSetCard, OnShuffleSetCard);
		_messages.Add(GameMessage.SwapGraveDeck, OnSwapGraveDeck);
		_messages.Add(GameMessage.TagSwap, OnTagSwap);
		_messages.Add(GameMessage.NewTurn, OnNewTurn);
		_messages.Add(GameMessage.NewPhase, OnNewPhase);
		_messages.Add(GameMessage.Damage, OnDamage);
		_messages.Add(GameMessage.PayLpCost, OnDamage);
		_messages.Add(GameMessage.Recover, OnRecover);
		_messages.Add(GameMessage.LpUpdate, OnLpUpdate);
		_messages.Add(GameMessage.Move, OnMove);
		_messages.Add(GameMessage.Swap, OnSwap);
		_messages.Add(GameMessage.Attack, OnAttack);
		_messages.Add(GameMessage.Battle, OnBattle);
		_messages.Add(GameMessage.AttackDisabled, OnAttackDisabled);
		_messages.Add(GameMessage.PosChange, OnPosChange);
		_messages.Add(GameMessage.Chaining, OnChaining);
		_messages.Add(GameMessage.ChainSolving, OnChainSolving);
		_messages.Add(GameMessage.ChainNegated, OnChainNegated);
		_messages.Add(GameMessage.ChainDisabled, OnChainDisabled);
		_messages.Add(GameMessage.ChainSolved, OnChainSolved);
		_messages.Add(GameMessage.ChainEnd, OnChainEnd);
		_messages.Add(GameMessage.SortCard, OnCardSorting);
		_messages.Add(GameMessage.SortChain, OnChainSorting);
		_messages.Add(GameMessage.UpdateCard, OnUpdateCard);
		_messages.Add(GameMessage.UpdateData, OnUpdateData);
		_messages.Add(GameMessage.BecomeTarget, OnBecomeTarget);
		_messages.Add(GameMessage.SelectBattleCmd, OnSelectBattleCmd);
		_messages.Add(GameMessage.SelectCard, OnSelectCard);
		_messages.Add(GameMessage.SelectUnselect, OnSelectUnselectCard);
		_messages.Add(GameMessage.SelectChain, OnSelectChain);
		_messages.Add(GameMessage.SelectCounter, OnSelectCounter);
		_messages.Add(GameMessage.SelectDisfield, OnSelectDisfield);
		_messages.Add(GameMessage.SelectEffectYn, OnSelectEffectYn);
		_messages.Add(GameMessage.SelectIdleCmd, OnSelectIdleCmd);
		_messages.Add(GameMessage.SelectOption, OnSelectOption);
		_messages.Add(GameMessage.SelectPlace, OnSelectPlace);
		_messages.Add(GameMessage.SelectPosition, OnSelectPosition);
		_messages.Add(GameMessage.SelectSum, OnSelectSum);
		_messages.Add(GameMessage.SelectTribute, OnSelectTribute);
		_messages.Add(GameMessage.SelectYesNo, OnSelectYesNo);
		_messages.Add(GameMessage.AnnounceAttrib, OnAnnounceAttrib);
		_messages.Add(GameMessage.AnnounceCard, OnAnnounceCard);
		_messages.Add(GameMessage.AnnounceNumber, OnAnnounceNumber);
		_messages.Add(GameMessage.AnnounceRace, OnAnnounceRace);
		_messages.Add(GameMessage.RockPaperScissors, OnRockPaperScissors);
		_messages.Add(GameMessage.Equip, OnEquip);
		_messages.Add(GameMessage.Unequip, OnUnEquip);
		_messages.Add(GameMessage.CardTarget, OnCardTarget);
		_messages.Add(GameMessage.CancelTarget, OnCancelTarget);
		_messages.Add(GameMessage.Summoning, OnSummoning);
		_messages.Add(GameMessage.Summoned, OnSummoned);
		_messages.Add(GameMessage.SpSummoning, OnSpSummoning);
		_messages.Add(GameMessage.SpSummoned, OnSpSummoned);
		_messages.Add(GameMessage.FlipSummoning, OnSummoning);
		_messages.Add(GameMessage.FlipSummoned, OnSummoned);
		_messages.Add(GameMessage.ConfirmCards, OnConfirmCards);
		_messages.Add(GameMessage.PlayerHint, OnPlayerHint);
	}

	private void OnJoinGame(BinaryReader packet)
	{
		packet.ReadUInt32();
		packet.ReadByte();
		packet.ReadByte();
		int num = packet.ReadByte();
		_ai.Duel.IsNewRule = num >= 4;
		_ai.Duel.IsNewRule2020 = num >= 5;
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.UpdateDeck);
		binaryWriter.Write(Deck.Cards.Count + Deck.ExtraCards.Count);
		binaryWriter.Write(Deck.SideCards.Count);
		foreach (NamedCard card in Deck.Cards)
		{
			binaryWriter.Write(card.Id);
		}
		foreach (NamedCard extraCard in Deck.ExtraCards)
		{
			binaryWriter.Write(extraCard.Id);
		}
		foreach (NamedCard sideCard in Deck.SideCards)
		{
			binaryWriter.Write(sideCard.Id);
		}
		Connection.Send(binaryWriter);
		_ai.OnJoinGame();
	}

	private void OnChangeSide(BinaryReader packet)
	{
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.UpdateDeck);
		binaryWriter.Write(Deck.Cards.Count + Deck.ExtraCards.Count);
		binaryWriter.Write(Deck.SideCards.Count);
		foreach (NamedCard card in Deck.Cards)
		{
			binaryWriter.Write(card.Id);
		}
		foreach (NamedCard extraCard in Deck.ExtraCards)
		{
			binaryWriter.Write(extraCard.Id);
		}
		foreach (NamedCard sideCard in Deck.SideCards)
		{
			binaryWriter.Write(sideCard.Id);
		}
		Connection.Send(binaryWriter);
		_ai.OnJoinGame();
	}

	private void OnTypeChange(BinaryReader packet)
	{
		int num = packet.ReadByte();
		int num2 = num & 0xF;
		if (num2 < 0 || num2 > 3)
		{
			Connection.Close();
			return;
		}
		_room.Position = num2;
		_room.IsHost = ((num >> 4) & 0xF) != 0;
		_room.IsReady[num2] = true;
		Connection.Send(CtosMessage.HsReady);
	}

	private void OnPlayerEnter(BinaryReader packet)
	{
		string text = packet.ReadUnicode(20);
		int num = packet.ReadByte();
		if (num < 8)
		{
			_room.Names[num] = text;
		}
	}

	private void OnPlayerChange(BinaryReader packet)
	{
		byte num = packet.ReadByte();
		int num2 = (num >> 4) & 0xF;
		int num3 = num & 0xF;
		if (num2 > 3)
		{
			return;
		}
		if (num3 < 8)
		{
			string text = _room.Names[num2];
			_room.Names[num2] = null;
			_room.Names[num3] = text;
			_room.IsReady[num2] = false;
			_room.IsReady[num3] = false;
		}
		else
		{
			switch (num3)
			{
			case 9:
				_room.IsReady[num2] = true;
				break;
			case 10:
				_room.IsReady[num2] = false;
				break;
			case 8:
			case 11:
				_room.IsReady[num2] = false;
				_room.Names[num2] = null;
				break;
			}
		}
		if (_room.IsHost && _room.IsReady[0] && _room.IsReady[1])
		{
			Connection.Send(CtosMessage.HsStart);
		}
	}

	private void OnSelectHand(BinaryReader packet)
	{
		int num = ((_hand <= 0) ? _ai.OnRockPaperScissors() : _hand);
		Connection.Send(CtosMessage.HandResult, (byte)num);
	}

	private void OnSelectTp(BinaryReader packet)
	{
		bool flag = _ai.OnSelectHand();
		Connection.Send(CtosMessage.TpResult, (byte)(flag ? 1 : 0));
	}

	private void OnTimeLimit(BinaryReader packet)
	{
		if (GetLocalPlayer(packet.ReadByte()) == 0)
		{
			Connection.Send(CtosMessage.TimeConfirm);
		}
	}

	private void OnReplay(BinaryReader packet)
	{
		packet.ReadToEnd();
	}

	private void OnDuelEnd(BinaryReader packet)
	{
		Thread.Sleep(500);
		Connection.Close();
	}

	private void OnChat(BinaryReader packet)
	{
		int num = packet.ReadInt16();
		string text = packet.ReadUnicode(256);
		string text2 = ((num != 0) ? _room.Names[1] : _room.Names[0]);
		string text3 = ((num == 0) ? _room.Names[1] : _room.Names[0]);
		if (num < 4)
		{
			Logger.DebugWriteLine(text3 + " say to " + text2 + ": " + text);
		}
		else
		{
			Logger.DebugWriteLine("System message(" + num + "): " + text);
		}
	}

	private void OnErrorMsg(BinaryReader packet)
	{
		int num = packet.ReadByte();
		packet.ReadByte();
		packet.ReadByte();
		packet.ReadByte();
		int num2 = packet.ReadInt32();
		Logger.DebugWriteLine("Error message received: " + num + ", code: " + num2);
		if (num != 2)
		{
			return;
		}
		int id = num2 & 0xFFFFFFF;
		if (num2 >> 28 <= 5)
		{
			NamedCard namedCard = NamedCard.Get(id);
			if (namedCard != null)
			{
				_ai.OnDeckError(namedCard.Name);
			}
			else
			{
				_ai.OnDeckError("Unknown Card");
			}
		}
		else
		{
			_ai.OnDeckError("DECK");
		}
	}

	private void OnTeammateSurrender(BinaryReader packet)
	{
		Thread.Sleep(500);
		Game.Surrender();
	}

	private void OnRetry(BinaryReader packet)
	{
		_ai.OnRetry();
		Connection.Close();
		throw new Exception("Got MSG_RETRY. Last message is " + _lastMessage);
	}

	private void OnHint(BinaryReader packet)
	{
		byte num = packet.ReadByte();
		int player = packet.ReadByte();
		int num2 = packet.ReadInt32();
		if (num == 1 && num2 == 24)
		{
			_duel.Fields[0].UnderAttack = false;
			_duel.Fields[1].UnderAttack = false;
		}
		if (num == 3)
		{
			_select_hint = num2;
		}
		if (num == 4)
		{
			_ai.OnReceivingAnnouce(player, num2);
		}
		if (num == 11)
		{
			Logger.DebugWriteLine("HINT_ZONE received: player=" + player + ", zone=" + num2);
			_ai.OnHintZone(player, num2);
		}
	}

	private void OnStart(BinaryReader packet)
	{
		int num = packet.ReadByte();
		_duel.IsFirst = (num & 0xF) == 0;
		_duel.Turn = 0;
		_duel.LastChainLocation = (CardLocation)0;
		_duel.LastChainPlayer = -1;
		_duel.LastChainTargets.Clear();
		_duel.LastSummonedCards.Clear();
		_duel.LastSummonPlayer = -1;
		int num2 = packet.ReadByte();
		_ai.Duel.IsNewRule = num2 >= 4;
		_ai.Duel.IsNewRule2020 = num2 >= 5;
		_duel.Fields[GetLocalPlayer(0)].LifePoints = packet.ReadInt32();
		_duel.Fields[GetLocalPlayer(1)].LifePoints = packet.ReadInt32();
		int deck = packet.ReadInt16();
		int extra = packet.ReadInt16();
		_duel.Fields[GetLocalPlayer(0)].Init(deck, extra);
		deck = packet.ReadInt16();
		extra = packet.ReadInt16();
		_duel.Fields[GetLocalPlayer(1)].Init(deck, extra);
		_duel.CurrentChain.Clear();
		_duel.CurrentChainInfo.Clear();
		_duel.ChainTargets.Clear();
		_duel.ChainTargetOnly.Clear();
		_duel.SummoningCards.Clear();
		_duel.SolvingChainIndex = 0;
		_duel.NegatedChainIndexList.Clear();
		Logger.DebugWriteLine("Duel started: " + _room.Names[0] + " versus " + _room.Names[1]);
		_ai.OnStart();
	}

	private void OnWin(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		string text = ((_room.Position == 0) ? _room.Names[1] : _room.Names[0]);
		Logger.DebugWriteLine("Duel finished against " + text + ", result: " + localPlayer switch
		{
			0 => "Win", 
			2 => "Draw", 
			_ => "Lose", 
		});
	}

	private void OnDraw(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num = packet.ReadByte();
		if (_debug)
		{
			Logger.WriteLine("(" + localPlayer + " draw " + num + " card)");
		}
		for (int i = 0; i < num; i++)
		{
			// MDProMod: the host does not always tell the AI the real deck size; never draw from an empty list.
			ClientField clientField = _duel.Fields[localPlayer];
			if (clientField != null && clientField.Deck.Count > 0)
			{
				clientField.Deck.RemoveAt(clientField.Deck.Count - 1);
			}
			clientField?.Hand.Add(new ClientCard(0, CardLocation.Hand, -1));
		}
		_ai.OnDraw(localPlayer);
	}

	private void OnShuffleDeck(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		foreach (ClientCard item in _duel.Fields[localPlayer].Deck)
		{
			item.SetId(0);
		}
	}

	private void OnShuffleHand(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		packet.ReadByte();
		foreach (ClientCard item in _duel.Fields[localPlayer].Hand)
		{
			item.SetId(packet.ReadInt32());
		}
	}

	private void OnShuffleExtra(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		packet.ReadByte();
		foreach (ClientCard item in _duel.Fields[localPlayer].ExtraDeck)
		{
			if (!item.IsFaceup())
			{
				item.SetId(packet.ReadInt32());
			}
		}
	}

	private void OnShuffleSetCard(BinaryReader packet)
	{
		packet.ReadByte();
		int num = packet.ReadByte();
		ClientCard[] array = new ClientCard[5];
		for (int i = 0; i < num; i++)
		{
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			int loc = packet.ReadByte();
			int seq = packet.ReadByte();
			packet.ReadByte();
			ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
			if (card != null)
			{
				array[i] = card;
				card.SetId(0);
			}
		}
		for (int j = 0; j < num; j++)
		{
			int localPlayer2 = GetLocalPlayer(packet.ReadByte());
			int num2 = packet.ReadByte();
			int num3 = packet.ReadByte();
			packet.ReadByte();
			if (_duel.GetCard(localPlayer2, (CardLocation)num2, num3) != null)
			{
				((num2 == 4) ? _duel.Fields[localPlayer2].MonsterZone : _duel.Fields[localPlayer2].SpellZone)[num3] = array[j];
			}
		}
	}

	private void OnSwapGraveDeck(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		IList<ClientCard> list = _duel.Fields[localPlayer].Deck.ToList();
		_duel.Fields[localPlayer].Deck.Clear();
		int num = 0;
		foreach (ClientCard item in _duel.Fields[localPlayer].Graveyard)
		{
			if (item.IsExtraCard())
			{
				_duel.Fields[localPlayer].ExtraDeck.Add(item);
				item.Location = CardLocation.Extra;
				item.Position = 10;
			}
			else
			{
				_duel.Fields[localPlayer].Deck.Add(item);
				item.Location = CardLocation.Deck;
				item.Sequence = num++;
			}
		}
		_duel.Fields[localPlayer].Graveyard.Clear();
		foreach (ClientCard item2 in list)
		{
			_duel.Fields[localPlayer].Graveyard.Add(item2);
			item2.Location = CardLocation.Grave;
		}
	}

	private void OnTagSwap(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num = packet.ReadByte();
		int num2 = packet.ReadByte();
		packet.ReadByte();
		int num3 = packet.ReadByte();
		packet.ReadInt32();
		_duel.Fields[localPlayer].Deck.Clear();
		for (int i = 0; i < num; i++)
		{
			_duel.Fields[localPlayer].Deck.Add(new ClientCard(0, CardLocation.Deck, -1));
		}
		_duel.Fields[localPlayer].ExtraDeck.Clear();
		for (int j = 0; j < num2; j++)
		{
			int id = packet.ReadInt32() & 0x7FFFFFFF;
			_duel.Fields[localPlayer].ExtraDeck.Add(new ClientCard(id, CardLocation.Extra, -1));
		}
		_duel.Fields[localPlayer].Hand.Clear();
		for (int k = 0; k < num3; k++)
		{
			int id2 = packet.ReadInt32();
			_duel.Fields[localPlayer].Hand.Add(new ClientCard(id2, CardLocation.Hand, -1));
		}
	}

	private void OnNewTurn(BinaryReader packet)
	{
		_duel.Turn++;
		_duel.Player = GetLocalPlayer(packet.ReadByte());
		_ai.OnNewTurn();
	}

	private void OnNewPhase(BinaryReader packet)
	{
		_duel.Phase = (DuelPhase)packet.ReadInt16();
		if (_debug && _duel.Phase == DuelPhase.Standby)
		{
			Logger.WriteLine("*********Bot Hand*********");
			foreach (ClientCard item in _duel.Fields[0].Hand)
			{
				Logger.WriteLine(item.Name);
			}
			Logger.WriteLine("*********Bot Spell*********");
			ClientCard[] spellZone = _duel.Fields[0].SpellZone;
			for (int i = 0; i < spellZone.Length; i++)
			{
				Logger.WriteLine(spellZone[i]?.Name);
			}
			Logger.WriteLine("*********Bot Monster*********");
			spellZone = _duel.Fields[0].MonsterZone;
			for (int i = 0; i < spellZone.Length; i++)
			{
				Logger.WriteLine(spellZone[i]?.Name);
			}
			Logger.WriteLine("*********Finish*********");
		}
		if (_debug)
		{
			Logger.WriteLine("(Go to " + _duel.Phase.ToString() + ")");
		}
		_duel.LastSummonPlayer = -1;
		_duel.SummoningCards.Clear();
		_duel.LastSummonedCards.Clear();
		_duel.Fields[0].BattlingMonster = null;
		_duel.Fields[1].BattlingMonster = null;
		_duel.Fields[0].UnderAttack = false;
		_duel.Fields[1].UnderAttack = false;
		foreach (ClientCard monster in _duel.Fields[0].GetMonsters())
		{
			monster.Attacked = false;
		}
		_select_hint = 0;
		_ai.OnNewPhase();
	}

	private void OnDamage(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num = _duel.Fields[localPlayer].LifePoints - packet.ReadInt32();
		if (num < 0)
		{
			num = 0;
		}
		if (_debug)
		{
			Logger.WriteLine("(" + localPlayer + " got damage , LifePoint left = " + num + ")");
		}
		_duel.Fields[localPlayer].LifePoints = num;
	}

	private void OnRecover(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int lifePoints = _duel.Fields[localPlayer].LifePoints + packet.ReadInt32();
		if (_debug)
		{
			Logger.WriteLine("(" + localPlayer + " got healed , LifePoint left = " + lifePoints + ")");
		}
		_duel.Fields[localPlayer].LifePoints = lifePoints;
	}

	private void OnLpUpdate(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		_duel.Fields[localPlayer].LifePoints = packet.ReadInt32();
	}

	private void OnMove(BinaryReader packet)
	{
		int num = packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num2 = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadSByte();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int num3 = packet.ReadByte();
		int seq2 = packet.ReadSByte();
		int pos = packet.ReadSByte();
		packet.ReadInt32();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)num2, seq);
		if (card != null)
		{
			card.LastLocation = (CardLocation)num2;
		}
		if ((num2 & 0x80) != 0)
		{
			num2 &= 0x7F;
			card = _duel.GetCard(localPlayer, (CardLocation)num2, seq);
			if (card != null)
			{
				if (_debug)
				{
					Logger.WriteLine("(" + localPlayer + " 's " + (card.Name ?? "UnKnowCard") + " deattach " + NamedCard.Get(num)?.Name + ")");
				}
				card.Overlays.Remove(num);
			}
			num2 = 0;
		}
		else
		{
			_duel.RemoveCard((CardLocation)num2, card, localPlayer, seq);
		}
		if ((num3 & 0x80) != 0)
		{
			num3 &= 0x7F;
			card = _duel.GetCard(localPlayer2, (CardLocation)num3, seq2);
			if (card != null)
			{
				if (_debug)
				{
					Logger.WriteLine("(" + localPlayer + " 's " + (card.Name ?? "UnKnowCard") + " overlay " + NamedCard.Get(num)?.Name + ")");
				}
				card.Overlays.Add(num);
			}
		}
		else if (num2 == 0)
		{
			if (_debug)
			{
				string[] obj = new string[7]
				{
					"(",
					localPlayer.ToString(),
					" 's ",
					NamedCard.Get(num)?.Name,
					" appear in ",
					null,
					null
				};
				CardLocation cardLocation = (CardLocation)num3;
				obj[5] = cardLocation.ToString();
				obj[6] = ")";
				Logger.WriteLine(string.Concat(obj));
			}
			_duel.AddCard((CardLocation)num3, num, localPlayer2, seq2, pos);
		}
		else
		{
			_duel.AddCard((CardLocation)num3, card, localPlayer2, seq2, pos, num);
			if (card != null && num2 != num3)
			{
				card.IsSpecialSummoned = false;
			}
			if (_debug && card != null)
			{
				string[] obj2 = new string[9]
				{
					"(",
					localPlayer.ToString(),
					" 's ",
					card.Name ?? "UnKnowCard",
					" from ",
					null,
					null,
					null,
					null
				};
				CardLocation cardLocation = (CardLocation)num2;
				obj2[5] = cardLocation.ToString();
				obj2[6] = " move to ";
				cardLocation = (CardLocation)num3;
				obj2[7] = cardLocation.ToString();
				obj2[8] = ")";
				Logger.WriteLine(string.Concat(obj2));
			}
		}
		_ai.OnMove(card, localPlayer, num2, localPlayer2, num3);
	}

	private void OnSwap(BinaryReader packet)
	{
		int id = packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadByte();
		packet.ReadByte();
		int id2 = packet.ReadInt32();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int loc2 = packet.ReadByte();
		int seq2 = packet.ReadByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		ClientCard card2 = _duel.GetCard(localPlayer2, (CardLocation)loc2, seq2);
		if (card != null && card2 != null)
		{
			_duel.RemoveCard((CardLocation)loc, card, localPlayer, seq);
			_duel.RemoveCard((CardLocation)loc2, card2, localPlayer2, seq2);
			_duel.AddCard((CardLocation)loc2, card, localPlayer2, seq2, card.Position, id);
			_duel.AddCard((CardLocation)loc, card2, localPlayer, seq, card2.Position, id2);
		}
	}

	private void OnAttack(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadByte();
		packet.ReadByte();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int num = packet.ReadByte();
		int seq2 = packet.ReadByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		ClientCard card2 = _duel.GetCard(localPlayer2, (CardLocation)num, seq2);
		if (_debug)
		{
			if (card2 == null)
			{
				Logger.WriteLine("(" + (card.Name ?? "UnKnowCard") + " direct attack!!)");
			}
			else
			{
				Logger.WriteLine("(" + localPlayer + " 's " + (card.Name ?? "UnKnowCard") + " attack  " + localPlayer2 + " 's " + (card2.Name ?? "UnKnowCard") + ")");
			}
		}
		_duel.Fields[card.Controller].BattlingMonster = card;
		_duel.Fields[1 - card.Controller].BattlingMonster = card2;
		_duel.Fields[1 - card.Controller].UnderAttack = true;
		if (num == 0 && localPlayer != 0)
		{
			_ai.OnDirectAttack(card);
		}
	}

	private void OnBattle(BinaryReader packet)
	{
		_duel.Fields[0].UnderAttack = false;
		_duel.Fields[1].UnderAttack = false;
	}

	private void OnAttackDisabled(BinaryReader packet)
	{
		_duel.Fields[0].UnderAttack = false;
		_duel.Fields[1].UnderAttack = false;
	}

	private void OnPosChange(BinaryReader packet)
	{
		packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		int num = packet.ReadSByte();
		int num2 = packet.ReadSByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		if (card != null)
		{
			card.Position = num2;
			if ((num & 5) > 0 && (num2 & 0xA) > 0)
			{
				card.ClearCardTargets();
			}
			if (_debug)
			{
				string[] obj = new string[5]
				{
					"(",
					card.Name ?? "UnKnowCard",
					" change position to ",
					null,
					null
				};
				CardPosition cardPosition = (CardPosition)num2;
				obj[3] = cardPosition.ToString();
				obj[4] = ")";
				Logger.WriteLine(string.Concat(obj));
			}
		}
	}

	private void OnChaining(BinaryReader packet)
	{
		int id = packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num = packet.ReadByte();
		int seq = packet.ReadSByte();
		int subSeq = packet.ReadSByte();
		ClientCard card = _duel.GetCard(localPlayer, num, seq, subSeq);
		if (card.Id == 0)
		{
			card.SetId(id);
		}
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		// MDProMod: the description is appended by MDPro3's server; the core's own message ends before it.
		int desc = 0;
		if (packet.BaseStream.Length - packet.BaseStream.Position >= 6)
		{
			packet.ReadInt16();
			desc = packet.ReadInt32();
		}
		if (_debug && card != null)
		{
			string[] obj = new string[7]
			{
				"(",
				localPlayer2.ToString(),
				" 's ",
				card.Name ?? "UnKnowCard",
				" activate effect from ",
				null,
				null
			};
			CardLocation cardLocation = (CardLocation)num;
			obj[5] = cardLocation.ToString();
			obj[6] = ")";
			Logger.WriteLine(string.Concat(obj));
		}
		_duel.LastChainLocation = (CardLocation)num;
		_ai.OnChaining(card, localPlayer2);
		_duel.ChainTargetOnly.Clear();
		_duel.LastSummonPlayer = -1;
		_duel.CurrentChain.Add(card);
		_duel.CurrentChainInfo.Add(new ChainInfo(card, localPlayer2, desc));
		_duel.LastChainPlayer = localPlayer2;
	}

	private void OnChainSolving(BinaryReader packet)
	{
		int solvingChainIndex = packet.ReadByte();
		_duel.SolvingChainIndex = solvingChainIndex;
	}

	private void OnChainNegated(BinaryReader packet)
	{
		int item = packet.ReadByte();
		_duel.NegatedChainIndexList.Add(item);
	}

	private void OnChainDisabled(BinaryReader packet)
	{
		int item = packet.ReadByte();
		_duel.NegatedChainIndexList.Add(item);
	}

	private void OnChainSolved(BinaryReader packet)
	{
		int chainIndex = packet.ReadByte();
		_ai.OnChainSolved(chainIndex);
	}

	private void OnChainEnd(BinaryReader packet)
	{
		_ai.OnChainEnd();
		_duel.LastChainPlayer = -1;
		_duel.LastChainLocation = (CardLocation)0;
		_duel.CurrentChain.Clear();
		_duel.CurrentChainInfo.Clear();
		_duel.ChainTargets.Clear();
		_duel.LastChainTargets.Clear();
		_duel.ChainTargetOnly.Clear();
		_duel.SolvingChainIndex = 0;
		_duel.NegatedChainIndexList.Clear();
		_duel.SummoningCards.Clear();
	}

	private void OnCardSorting(BinaryReader packet)
	{
		GetLocalPlayer(packet.ReadByte());
		IList<ClientCard> list = new List<ClientCard>();
		IList<ClientCard> list2 = new List<ClientCard>();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			int num2 = packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			CardLocation cardLocation = (CardLocation)packet.ReadByte();
			int seq = packet.ReadByte();
			ClientCard clientCard = (((cardLocation & CardLocation.Overlay) == 0) ? _duel.GetCard(localPlayer, cardLocation, seq) : new ClientCard(num2, CardLocation.Overlay, -1));
			if (clientCard != null)
			{
				if (num2 != 0)
				{
					clientCard.SetId(num2);
				}
				list.Add(clientCard);
				list2.Add(clientCard);
			}
		}
		IList<ClientCard> list3 = _ai.OnCardSorting(list2);
		byte[] array = new byte[num];
		for (int j = 0; j < num; j++)
		{
			int num3 = 0;
			for (int k = 0; k < num; k++)
			{
				if (list3[k] != null && list3[k].Equals(list[j]))
				{
					num3 = k;
					break;
				}
			}
			array[j] = (byte)num3;
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnChainSorting(BinaryReader packet)
	{
		GamePacketFactory.Create(CtosMessage.Response);
		Connection.Send(CtosMessage.Response, -1);
	}

	private void OnUpdateCard(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadByte();
		packet.ReadInt32();
		_duel.GetCard(localPlayer, (CardLocation)loc, seq)?.Update(packet, _duel);
	}

	private void OnUpdateData(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		CardLocation cardLocation = (CardLocation)packet.ReadByte();
		IList<ClientCard> list = null;
		switch (cardLocation)
		{
		case CardLocation.Hand:
			list = _duel.Fields[localPlayer].Hand;
			break;
		case CardLocation.MonsterZone:
			list = _duel.Fields[localPlayer].MonsterZone;
			break;
		case CardLocation.SpellZone:
			list = _duel.Fields[localPlayer].SpellZone;
			break;
		case CardLocation.Grave:
			list = _duel.Fields[localPlayer].Graveyard;
			break;
		case CardLocation.Removed:
			list = _duel.Fields[localPlayer].Banished;
			break;
		case CardLocation.Deck:
			list = _duel.Fields[localPlayer].Deck;
			break;
		case CardLocation.Extra:
			list = _duel.Fields[localPlayer].ExtraDeck;
			break;
		}
		if (list == null)
		{
			return;
		}
		foreach (ClientCard item in list)
		{
			int num = packet.ReadInt32();
			long position = packet.BaseStream.Position;
			if (num > 8)
			{
				item.Update(packet, _duel);
			}
			packet.BaseStream.Position = position + num - 4;
		}
	}

	private void OnBecomeTarget(BinaryReader packet)
	{
		_duel.LastChainTargets.Clear();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			int num2 = packet.ReadByte();
			int seq = packet.ReadByte();
			packet.ReadByte();
			ClientCard card = _duel.GetCard(localPlayer, (CardLocation)num2, seq);
			if (card != null)
			{
				if (_debug)
				{
					string[] obj = new string[5] { "(", null, null, null, null };
					CardLocation cardLocation = (CardLocation)num2;
					obj[1] = cardLocation.ToString();
					obj[2] = " 's ";
					obj[3] = card.Name ?? "UnKnowCard";
					obj[4] = " become target)";
					Logger.WriteLine(string.Concat(obj));
				}
				_duel.ChainTargets.Add(card);
				_duel.LastChainTargets.Add(card);
				_duel.ChainTargetOnly.Add(card);
			}
		}
	}

	private void OnSelectBattleCmd(BinaryReader packet)
	{
		packet.ReadByte();
		_duel.BattlePhase = new BattlePhase();
		BattlePhase battlePhase = _duel.BattlePhase;
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			CardLocation loc = (CardLocation)packet.ReadByte();
			int seq = packet.ReadByte();
			int item = packet.ReadInt32();
			ClientCard card = _duel.GetCard(localPlayer, loc, seq);
			if (card != null)
			{
				card.ActionIndex[0] = i;
				battlePhase.ActivableCards.Add(card);
				battlePhase.ActivableDescs.Add(item);
			}
		}
		num = packet.ReadByte();
		for (int j = 0; j < num; j++)
		{
			packet.ReadInt32();
			int localPlayer2 = GetLocalPlayer(packet.ReadByte());
			CardLocation loc2 = (CardLocation)packet.ReadByte();
			int seq2 = packet.ReadByte();
			int num2 = packet.ReadByte();
			ClientCard card2 = _duel.GetCard(localPlayer2, loc2, seq2);
			if (card2 != null)
			{
				card2.ActionIndex[1] = j;
				if (num2 > 0)
				{
					card2.CanDirectAttack = true;
				}
				else
				{
					card2.CanDirectAttack = false;
				}
				battlePhase.AttackableCards.Add(card2);
				card2.Attacked = false;
			}
		}
		foreach (ClientCard monster in _duel.Fields[0].GetMonsters())
		{
			if (!battlePhase.AttackableCards.Contains(monster))
			{
				monster.Attacked = true;
			}
		}
		battlePhase.CanMainPhaseTwo = packet.ReadByte() != 0;
		battlePhase.CanEndPhase = packet.ReadByte() != 0;
		Connection.Send(CtosMessage.Response, _ai.OnSelectBattleCmd(battlePhase).ToValue());
	}

	private void InternalOnSelectCard(BinaryReader packet, Func<IList<ClientCard>, int, int, int, bool, IList<ClientCard>> func)
	{
		packet.ReadByte();
		bool flag = packet.ReadByte() != 0;
		int arg = packet.ReadByte();
		int arg2 = packet.ReadByte();
		IList<ClientCard> list = new List<ClientCard>();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			int id = packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			CardLocation cardLocation = (CardLocation)packet.ReadByte();
			int seq = packet.ReadByte();
			packet.ReadByte();
			ClientCard clientCard;
			if ((cardLocation & CardLocation.Overlay) != 0)
			{
				clientCard = new ClientCard(id, CardLocation.Overlay, -1);
				CardLocation loc = cardLocation ^ CardLocation.Overlay;
				ClientCard card = _duel.GetCard(localPlayer, loc, seq);
				if (card != null)
				{
					clientCard.OwnTargets.Add(card);
				}
			}
			else
			{
				clientCard = _duel.GetCard(localPlayer, cardLocation, seq);
				clientCard.Controller = localPlayer;
			}
			if (clientCard != null)
			{
				if (clientCard.Id == 0 || clientCard.Location == CardLocation.Deck)
				{
					clientCard.SetId(id);
				}
				list.Add(clientCard);
			}
		}
		if (_select_hint == 575 && flag)
		{
			_select_hint = 0;
			Connection.Send(CtosMessage.Response, -1);
			return;
		}
		IList<ClientCard> list2 = func(list, arg, arg2, _select_hint, flag);
		_select_hint = 0;
		if (list2.Count == 0 && flag)
		{
			Connection.Send(CtosMessage.Response, -1);
			return;
		}
		byte[] array = new byte[list2.Count + 1];
		array[0] = (byte)list2.Count;
		for (int j = 0; j < list2.Count; j++)
		{
			int num2 = 0;
			for (int k = 0; k < num; k++)
			{
				if (list[k] != null && list[k].Equals(list2[j]))
				{
					num2 = k;
					break;
				}
			}
			array[j + 1] = (byte)num2;
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void InternalOnSelectUnselectCard(BinaryReader packet, Func<IList<ClientCard>, int, int, int, bool, IList<ClientCard>> func)
	{
		packet.ReadByte();
		bool flag = packet.ReadByte() != 0;
		bool flag2 = packet.ReadByte() != 0 || flag;
		packet.ReadByte();
		packet.ReadByte();
		IList<ClientCard> list = new List<ClientCard>();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			int id = packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			CardLocation cardLocation = (CardLocation)packet.ReadByte();
			int seq = packet.ReadByte();
			packet.ReadByte();
			ClientCard clientCard = (((cardLocation & CardLocation.Overlay) == 0) ? _duel.GetCard(localPlayer, cardLocation, seq) : new ClientCard(id, CardLocation.Overlay, -1));
			if (clientCard != null)
			{
				if (clientCard.Id == 0 || clientCard.Location == CardLocation.Deck)
				{
					clientCard.SetId(id);
				}
				list.Add(clientCard);
			}
		}
		int num2 = packet.ReadByte();
		for (int j = 0; j < num2; j++)
		{
			int id2 = packet.ReadInt32();
			int localPlayer2 = GetLocalPlayer(packet.ReadByte());
			CardLocation cardLocation2 = (CardLocation)packet.ReadByte();
			int seq2 = packet.ReadByte();
			packet.ReadByte();
			ClientCard clientCard2 = (((cardLocation2 & CardLocation.Overlay) == 0) ? _duel.GetCard(localPlayer2, cardLocation2, seq2) : new ClientCard(id2, CardLocation.Overlay, -1));
			if (clientCard2 != null && (clientCard2.Id == 0 || clientCard2.Location == CardLocation.Deck))
			{
				clientCard2.SetId(id2);
			}
		}
		if (num2 == 0)
		{
			flag2 = false;
		}
		IList<ClientCard> list2 = func(list, (!flag) ? 1 : 0, 1, _select_hint, flag2);
		if (list2.Count == 0 && flag2)
		{
			Connection.Send(CtosMessage.Response, -1);
			return;
		}
		byte[] array = new byte[list2.Count + 1];
		array[0] = (byte)list2.Count;
		for (int k = 0; k < list2.Count; k++)
		{
			int num3 = 0;
			for (int l = 0; l < num; l++)
			{
				if (list[l] != null && list[l].Equals(list2[k]))
				{
					num3 = l;
					break;
				}
			}
			array[k + 1] = (byte)num3;
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnSelectCard(BinaryReader packet)
	{
		InternalOnSelectCard(packet, _ai.OnSelectCard);
	}

	private void OnSelectUnselectCard(BinaryReader packet)
	{
		InternalOnSelectUnselectCard(packet, _ai.OnSelectCard);
	}

	private void OnSelectChain(BinaryReader packet)
	{
		packet.ReadByte();
		int num = packet.ReadByte();
		packet.ReadByte();
		int num2 = packet.ReadInt32();
		int num3 = packet.ReadInt32();
		IList<ClientCard> list = new List<ClientCard>();
		IList<int> list2 = new List<int>();
		IList<bool> list3 = new List<bool>();
		for (int i = 0; i < num; i++)
		{
			packet.ReadByte();
			bool item = packet.ReadByte() != 0;
			int id = packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			int loc = packet.ReadByte();
			int seq = packet.ReadByte();
			int subSeq = packet.ReadByte();
			int num4 = packet.ReadInt32();
			if (num4 == 221)
			{
				num4 = 0;
			}
			ClientCard card = _duel.GetCard(localPlayer, loc, seq, subSeq);
			if (card.Id == 0)
			{
				card.SetId(id);
			}
			list.Add(card);
			list2.Add(num4);
			list3.Add(item);
		}
		if (list.Count == 0)
		{
			Connection.Send(CtosMessage.Response, -1);
		}
		else if (list.Count == 1 && list3[0])
		{
			Connection.Send(CtosMessage.Response, 0);
		}
		else
		{
			Connection.Send(CtosMessage.Response, _ai.OnSelectChain(list, list2, list3, num2 | num3));
		}
	}

	private void OnSelectCounter(BinaryReader packet)
	{
		packet.ReadByte();
		int type = packet.ReadInt16();
		int quantity = packet.ReadInt16();
		IList<ClientCard> list = new List<ClientCard>();
		IList<int> list2 = new List<int>();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			CardLocation loc = (CardLocation)packet.ReadByte();
			int seq = packet.ReadByte();
			int item = packet.ReadInt16();
			list.Add(_duel.GetCard(localPlayer, loc, seq));
			list2.Add(item);
		}
		IList<int> list3 = _ai.OnSelectCounter(type, quantity, list, list2);
		byte[] array = new byte[list3.Count * 2];
		for (int j = 0; j < list3.Count; j++)
		{
			array[j * 2] = (byte)(list3[j] & 0xFF);
			array[j * 2 + 1] = (byte)(list3[j] >> 8);
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnSelectDisfield(BinaryReader packet)
	{
		packet.ReadByte();
		packet.ReadByte();
		int num = ~packet.ReadInt32();
		int player;
		CardLocation cardLocation;
		int num2;
		if ((num & 0x7F0000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.MonsterZone;
			num2 = (num >> 16) & 0x7F;
		}
		else if ((num & 0x1F000000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.SpellZone;
			num2 = (num >> 24) & 0x1F;
		}
		else if ((num & 0x7F) != 0)
		{
			player = 0;
			cardLocation = CardLocation.MonsterZone;
			num2 = num & 0x7F;
		}
		else if ((num & 0x1F00) != 0)
		{
			player = 0;
			cardLocation = CardLocation.SpellZone;
			num2 = (num >> 8) & 0x1F;
		}
		else if ((num & 0x2000) != 0)
		{
			player = 0;
			cardLocation = CardLocation.FieldZone;
			num2 = 32;
		}
		else if ((num & 0xC000) != 0)
		{
			player = 0;
			cardLocation = CardLocation.PendulumZone;
			num2 = (num >> 14) & 3;
		}
		else if ((num & 0x20000000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.FieldZone;
			num2 = 32;
		}
		else
		{
			player = 1;
			cardLocation = CardLocation.PendulumZone;
			num2 = (num >> 30) & 3;
		}
		int num3 = _ai.OnSelectPlace(_select_hint, player, cardLocation, num2);
		_select_hint = 0;
		byte[] array = new byte[3]
		{
			(byte)GetLocalPlayer(player),
			0,
			0
		};
		if (cardLocation != CardLocation.PendulumZone && cardLocation != CardLocation.FieldZone)
		{
			array[1] = (byte)cardLocation;
			if ((num3 & num2) > 0)
			{
				num2 &= num3;
			}
			if ((num2 & 4) != 0)
			{
				array[2] = 2;
			}
			else if ((num2 & 2) != 0)
			{
				array[2] = 1;
			}
			else if ((num2 & 8) != 0)
			{
				array[2] = 3;
			}
			else if ((num2 & 1) != 0)
			{
				array[2] = 0;
			}
			else if ((num2 & 0x10) != 0)
			{
				array[2] = 4;
			}
			else if ((num2 & 0x40) != 0)
			{
				array[2] = 6;
			}
			else if ((num2 & 0x20) != 0)
			{
				array[2] = 5;
			}
		}
		else
		{
			array[1] = 8;
			if ((num3 & num2) > 0)
			{
				num2 &= num3;
			}
			if ((num2 & 0x20) != 0)
			{
				array[2] = 5;
			}
			if ((num2 & 1) != 0)
			{
				array[2] = 6;
			}
			if ((num2 & 2) != 0)
			{
				array[2] = 7;
			}
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnSelectEffectYn(BinaryReader packet)
	{
		packet.ReadByte();
		int id = packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		CardLocation loc = (CardLocation)packet.ReadByte();
		int seq = packet.ReadByte();
		packet.ReadByte();
		int num = packet.ReadInt32();
		if (num == 0 || num == 221)
		{
			num = -1;
		}
		ClientCard card = _duel.GetCard(localPlayer, loc, seq);
		if (card == null)
		{
			Connection.Send(CtosMessage.Response, 0);
			return;
		}
		if (card.Id == 0)
		{
			card.SetId(id);
		}
		int value = (_ai.OnSelectEffectYn(card, num) ? 1 : 0);
		Connection.Send(CtosMessage.Response, value);
	}

	private void OnSelectIdleCmd(BinaryReader packet)
	{
		packet.ReadByte();
		_duel.MainPhase = new MainPhase();
		MainPhase mainPhase = _duel.MainPhase;
		int num;
		for (int i = 0; i < 5; i++)
		{
			num = packet.ReadByte();
			for (int j = 0; j < num; j++)
			{
				packet.ReadInt32();
				int localPlayer = GetLocalPlayer(packet.ReadByte());
				CardLocation loc = (CardLocation)packet.ReadByte();
				int seq = packet.ReadByte();
				ClientCard card = _duel.GetCard(localPlayer, loc, seq);
				if (card != null)
				{
					card.ActionIndex[i] = j;
					switch (i)
					{
					case 0:
						mainPhase.SummonableCards.Add(card);
						break;
					case 1:
						mainPhase.SpecialSummonableCards.Add(card);
						break;
					case 2:
						mainPhase.ReposableCards.Add(card);
						break;
					case 3:
						mainPhase.MonsterSetableCards.Add(card);
						break;
					case 4:
						mainPhase.SpellSetableCards.Add(card);
						break;
					}
				}
			}
		}
		num = packet.ReadByte();
		for (int k = 0; k < num; k++)
		{
			packet.ReadInt32();
			int localPlayer2 = GetLocalPlayer(packet.ReadByte());
			CardLocation loc2 = (CardLocation)packet.ReadByte();
			int seq2 = packet.ReadByte();
			int num2 = packet.ReadInt32();
			ClientCard card2 = _duel.GetCard(localPlayer2, loc2, seq2);
			if (card2 != null)
			{
				card2.ActionIndex[5] = k;
				if (card2.ActionActivateIndex.ContainsKey(num2))
				{
					card2.ActionActivateIndex.Remove(num2);
				}
				card2.ActionActivateIndex.Add(num2, k);
				mainPhase.ActivableCards.Add(card2);
				mainPhase.ActivableDescs.Add(num2);
			}
		}
		mainPhase.CanBattlePhase = packet.ReadByte() != 0;
		mainPhase.CanEndPhase = packet.ReadByte() != 0;
		packet.ReadByte();
		Connection.Send(CtosMessage.Response, _ai.OnSelectIdleCmd(mainPhase).ToValue());
	}

	private void OnSelectOption(BinaryReader packet)
	{
		IList<int> list = new List<int>();
		packet.ReadByte();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			list.Add(packet.ReadInt32());
		}
		Connection.Send(CtosMessage.Response, _ai.OnSelectOption(list));
	}

	private void OnSelectPlace(BinaryReader packet)
	{
		packet.ReadByte();
		packet.ReadByte();
		int num = ~packet.ReadInt32();
		int player;
		CardLocation cardLocation;
		int num2;
		if ((num & 0x7F) != 0)
		{
			player = 0;
			cardLocation = CardLocation.MonsterZone;
			num2 = num & 0x7F;
		}
		else if ((num & 0x1F00) != 0)
		{
			player = 0;
			cardLocation = CardLocation.SpellZone;
			num2 = (num >> 8) & 0x1F;
		}
		else if ((num & 0x2000) != 0)
		{
			player = 0;
			cardLocation = CardLocation.FieldZone;
			num2 = 32;
		}
		else if ((num & 0xC000) != 0)
		{
			player = 0;
			cardLocation = CardLocation.PendulumZone;
			num2 = (num >> 14) & 3;
		}
		else if ((num & 0x7F0000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.MonsterZone;
			num2 = (num >> 16) & 0x7F;
		}
		else if ((num & 0x1F000000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.SpellZone;
			num2 = (num >> 24) & 0x1F;
		}
		else if ((num & 0x20000000) != 0)
		{
			player = 1;
			cardLocation = CardLocation.FieldZone;
			num2 = 32;
		}
		else
		{
			player = 1;
			cardLocation = CardLocation.PendulumZone;
			num2 = (num >> 30) & 3;
		}
		int num3 = _ai.OnSelectPlace(_select_hint, player, cardLocation, num2);
		_select_hint = 0;
		byte[] array = new byte[3]
		{
			(byte)GetLocalPlayer(player),
			0,
			0
		};
		if (cardLocation != CardLocation.PendulumZone && cardLocation != CardLocation.FieldZone)
		{
			array[1] = (byte)cardLocation;
			if ((num3 & num2) > 0)
			{
				num2 &= num3;
			}
			if ((num2 & 4) != 0)
			{
				array[2] = 2;
			}
			else if ((num2 & 2) != 0)
			{
				array[2] = 1;
			}
			else if ((num2 & 8) != 0)
			{
				array[2] = 3;
			}
			else if ((num2 & 1) != 0)
			{
				array[2] = 0;
			}
			else if ((num2 & 0x10) != 0)
			{
				array[2] = 4;
			}
			else if ((num2 & 0x40) != 0)
			{
				array[2] = 6;
			}
			else if ((num2 & 0x20) != 0)
			{
				array[2] = 5;
			}
		}
		else
		{
			array[1] = 8;
			if ((num3 & num2) > 0)
			{
				num2 &= num3;
			}
			if ((num2 & 0x20) != 0)
			{
				array[2] = 5;
			}
			if ((num2 & 1) != 0)
			{
				array[2] = 6;
			}
			if ((num2 & 2) != 0)
			{
				array[2] = 7;
			}
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnSelectPosition(BinaryReader packet)
	{
		packet.ReadByte();
		int cardId = packet.ReadInt32();
		int num = packet.ReadByte();
		if (num == 1 || num == 2 || num == 4 || num == 8)
		{
			Connection.Send(CtosMessage.Response, num);
			return;
		}
		IList<CardPosition> list = new List<CardPosition>();
		if ((num & 1) != 0)
		{
			list.Add(CardPosition.FaceUpAttack);
		}
		if ((num & 2) != 0)
		{
			list.Add(CardPosition.FaceDownAttack);
		}
		if ((num & 4) != 0)
		{
			list.Add(CardPosition.FaceUpDefence);
		}
		if ((num & 8) != 0)
		{
			list.Add(CardPosition.FaceDownDefence);
		}
		Connection.Send(CtosMessage.Response, (int)_ai.OnSelectPosition(cardId, list));
	}

	private void OnSelectSum(BinaryReader packet)
	{
		bool mode = packet.ReadByte() == 0;
		packet.ReadByte();
		int num = packet.ReadInt32();
		int min = packet.ReadByte();
		int num2 = packet.ReadByte();
		if (num2 <= 0)
		{
			num2 = 99;
		}
		IList<ClientCard> list = new List<ClientCard>();
		IList<ClientCard> list2 = new List<ClientCard>();
		for (int i = 0; i < 2; i++)
		{
			int num3 = packet.ReadByte();
			for (int j = 0; j < num3; j++)
			{
				int num4 = packet.ReadInt32();
				int localPlayer = GetLocalPlayer(packet.ReadByte());
				CardLocation loc = (CardLocation)packet.ReadByte();
				int seq = packet.ReadByte();
				ClientCard card = _duel.GetCard(localPlayer, loc, seq);
				if (num4 != 0 && card.Id != num4)
				{
					card.SetId(num4);
				}
				card.SelectSeq = j;
				int num5 = packet.ReadInt32();
				int num6 = num5 & 0xFFFF;
				int num7 = num5 >> 16;
				if ((num5 & 0x80000000u) > 0)
				{
					num6 = num5 & 0x7FFFFFFF;
					num7 = 0;
				}
				if (num7 > 0 && num6 > num7)
				{
					card.OpParam1 = num7;
					card.OpParam2 = num6;
				}
				else
				{
					card.OpParam1 = num6;
					card.OpParam2 = num7;
				}
				if (i == 0)
				{
					list.Add(card);
				}
				else
				{
					list2.Add(card);
				}
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			num -= list[k].OpParam1;
		}
		IList<ClientCard> list3 = _ai.OnSelectSum(list2, num, min, num2, _select_hint, mode);
		_select_hint = 0;
		byte[] array = new byte[list.Count + list3.Count + 1];
		int num8 = 0;
		array[num8++] = (byte)(list.Count + list3.Count);
		while (num8 <= list.Count)
		{
			array[num8++] = 0;
		}
		for (int l = 0; l < list3.Count; l++)
		{
			array[num8++] = (byte)list3[l].SelectSeq;
		}
		BinaryWriter binaryWriter = GamePacketFactory.Create(CtosMessage.Response);
		binaryWriter.Write(array);
		Connection.Send(binaryWriter);
	}

	private void OnSelectTribute(BinaryReader packet)
	{
		InternalOnSelectCard(packet, _ai.OnSelectTribute);
	}

	private void OnSelectYesNo(BinaryReader packet)
	{
		packet.ReadByte();
		int num = packet.ReadInt32();
		int value = ((num != 30) ? (_ai.OnSelectYesNo(num) ? 1 : 0) : (_ai.OnSelectBattleReplay() ? 1 : 0));
		Connection.Send(CtosMessage.Response, value);
	}

	private void OnAnnounceAttrib(BinaryReader packet)
	{
		IList<CardAttribute> list = new List<CardAttribute>();
		packet.ReadByte();
		int num = packet.ReadByte();
		int num2 = packet.ReadInt32();
		int num3 = 1;
		for (int i = 0; i < 7; i++)
		{
			if ((num2 & num3) != 0)
			{
				list.Add((CardAttribute)num3);
			}
			num3 <<= 1;
		}
		list = _ai.OnAnnounceAttrib(num, list);
		int num4 = 0;
		for (int j = 0; j < num; j++)
		{
			num4 = (int)(num4 + list[j]);
		}
		Connection.Send(CtosMessage.Response, num4);
	}

	private void OnAnnounceCard(BinaryReader packet)
	{
		IList<int> list = new List<int>();
		packet.ReadByte();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			list.Add(packet.ReadInt32());
		}
		IList<int> list2 = new List<int>();
		foreach (NamedCard allCard in NamedCardsManager.GetAllCards())
		{
			if (allCard.HasType(CardType.Token) || (allCard.Alias > 0 && allCard.Id - allCard.Alias < 10))
			{
				continue;
			}
			Stack<int> stack = new Stack<int>();
			for (int j = 0; j < list.Count; j++)
			{
				switch (list[j])
				{
				case 1073741824:
					if (stack.Count >= 2)
					{
						int num15 = stack.Pop();
						int num16 = stack.Pop();
						stack.Push(num16 + num15);
					}
					break;
				case 1073741825:
					if (stack.Count >= 2)
					{
						int num8 = stack.Pop();
						int num9 = stack.Pop();
						stack.Push(num9 - num8);
					}
					break;
				case 1073741826:
					if (stack.Count >= 2)
					{
						int num13 = stack.Pop();
						int num14 = stack.Pop();
						stack.Push(num14 * num13);
					}
					break;
				case 1073741827:
					if (stack.Count >= 2)
					{
						int num2 = stack.Pop();
						int num3 = stack.Pop();
						stack.Push(num3 / num2);
					}
					break;
				case 1073741828:
					if (stack.Count >= 2)
					{
						int num4 = stack.Pop();
						int num5 = stack.Pop();
						bool num6 = num4 != 0;
						bool flag = num5 != 0;
						if (num6 && flag)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073741829:
					if (stack.Count >= 2)
					{
						int num10 = stack.Pop();
						int num11 = stack.Pop();
						bool num12 = num10 != 0;
						bool flag2 = num11 != 0;
						if (num12 || flag2)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073741830:
					if (stack.Count >= 1)
					{
						int num7 = stack.Pop();
						stack.Push(-num7);
					}
					break;
				case 1073741831:
					if (stack.Count >= 1)
					{
						if (stack.Pop() != 0)
						{
							stack.Push(0);
						}
						else
						{
							stack.Push(1);
						}
					}
					break;
				case 1073742080:
					if (stack.Count >= 1)
					{
						if (stack.Pop() == allCard.Id)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073742081:
					if (stack.Count >= 1)
					{
						if (allCard.HasSetcode(stack.Pop()))
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073742082:
					if (stack.Count >= 1)
					{
						if ((stack.Pop() & allCard.Type) > 0)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073742083:
					if (stack.Count >= 1)
					{
						if ((stack.Pop() & allCard.Race) > 0)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				case 1073742084:
					if (stack.Count >= 1)
					{
						if ((stack.Pop() & allCard.Attribute) > 0)
						{
							stack.Push(1);
						}
						else
						{
							stack.Push(0);
						}
					}
					break;
				default:
					stack.Push(list[j]);
					break;
				}
			}
			if (stack.Count == 1 && stack.Pop() != 0)
			{
				list2.Add(allCard.Id);
			}
		}
		if (list2.Count == 0)
		{
			throw new Exception("No avail card found for announce!");
		}
		Connection.Send(CtosMessage.Response, _ai.OnAnnounceCard(list2));
	}

	private void OnAnnounceNumber(BinaryReader packet)
	{
		IList<int> list = new List<int>();
		packet.ReadByte();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			list.Add(packet.ReadInt32());
		}
		Connection.Send(CtosMessage.Response, _ai.OnAnnounceNumber(list));
	}

	private void OnAnnounceRace(BinaryReader packet)
	{
		IList<CardRace> list = new List<CardRace>();
		packet.ReadByte();
		int num = packet.ReadByte();
		int num2 = packet.ReadInt32();
		int num3 = 1;
		for (int i = 0; i < 26; i++)
		{
			if ((num2 & num3) != 0)
			{
				list.Add((CardRace)num3);
			}
			num3 <<= 1;
		}
		list = _ai.OnAnnounceRace(num, list);
		int num4 = 0;
		for (int j = 0; j < num; j++)
		{
			num4 = (int)(num4 + list[j]);
		}
		Connection.Send(CtosMessage.Response, num4);
	}

	private void OnRockPaperScissors(BinaryReader packet)
	{
		packet.ReadByte();
		int value = ((_hand <= 0) ? _ai.OnRockPaperScissors() : _hand);
		Connection.Send(CtosMessage.Response, value);
	}

	private void OnEquip(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadByte();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int loc2 = packet.ReadByte();
		int seq2 = packet.ReadSByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		ClientCard card2 = _duel.GetCard(localPlayer2, (CardLocation)loc2, seq2);
		if (card != null && card2 != null)
		{
			card.EquipTarget?.EquipCards.Remove(card);
			card.EquipTarget = card2;
			card2.EquipCards.Add(card);
		}
	}

	private void OnUnEquip(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		if (card != null && card.EquipTarget != null)
		{
			card.EquipTarget.EquipCards.Remove(card);
			card.EquipTarget = null;
		}
	}

	private void OnCardTarget(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadByte();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int loc2 = packet.ReadByte();
		int seq2 = packet.ReadSByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		ClientCard card2 = _duel.GetCard(localPlayer2, (CardLocation)loc2, seq2);
		if (card != null && card2 != null)
		{
			card.TargetCards.Add(card2);
			card2.OwnTargets.Add(card);
		}
	}

	private void OnCancelTarget(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadByte();
		int localPlayer2 = GetLocalPlayer(packet.ReadByte());
		int loc2 = packet.ReadByte();
		int seq2 = packet.ReadSByte();
		packet.ReadByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		ClientCard card2 = _duel.GetCard(localPlayer2, (CardLocation)loc2, seq2);
		if (card != null && card2 != null)
		{
			card.TargetCards.Remove(card2);
			card2.OwnTargets.Remove(card);
		}
	}

	private void OnSummoning(BinaryReader packet)
	{
		_duel.LastSummonedCards.Clear();
		packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadSByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		_duel.SummoningCards.Add(card);
		_duel.LastSummonPlayer = localPlayer;
	}

	private void OnSummoned(BinaryReader packet)
	{
		foreach (ClientCard summoningCard in _duel.SummoningCards)
		{
			_duel.LastSummonedCards.Add(summoningCard);
		}
		_duel.SummoningCards.Clear();
	}

	private void OnSpSummoning(BinaryReader packet)
	{
		_duel.LastSummonedCards.Clear();
		_ai.CleanSelectMaterials();
		packet.ReadInt32();
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int loc = packet.ReadByte();
		int seq = packet.ReadSByte();
		packet.ReadSByte();
		ClientCard card = _duel.GetCard(localPlayer, (CardLocation)loc, seq);
		_duel.SummoningCards.Add(card);
		_duel.LastSummonPlayer = localPlayer;
	}

	private void OnSpSummoned(BinaryReader packet)
	{
		foreach (ClientCard summoningCard in _duel.SummoningCards)
		{
			summoningCard.IsSpecialSummoned = true;
			_duel.LastSummonedCards.Add(summoningCard);
		}
		_ai.OnSpSummoned();
		_duel.SummoningCards.Clear();
	}

	private void OnConfirmCards(BinaryReader packet)
	{
		packet.ReadByte();
		packet.ReadByte();
		int num = packet.ReadByte();
		for (int i = 0; i < num; i++)
		{
			int num2 = packet.ReadInt32();
			int localPlayer = GetLocalPlayer(packet.ReadByte());
			int num3 = packet.ReadByte();
			int seq = packet.ReadByte();
			ClientCard card = _duel.GetCard(localPlayer, (CardLocation)num3, seq);
			if (num2 > 0)
			{
				card.SetId(num2);
			}
			if (_debug)
			{
				string[] obj = new string[7]
				{
					"(Confirm ",
					localPlayer.ToString(),
					"'s ",
					null,
					null,
					null,
					null
				};
				CardLocation cardLocation = (CardLocation)num3;
				obj[3] = cardLocation.ToString();
				obj[4] = " card: ";
				obj[5] = card.Name ?? "UnKnowCard";
				obj[6] = ")";
				Logger.WriteLine(string.Concat(obj));
			}
		}
	}

	private void OnPlayerHint(BinaryReader packet)
	{
		int localPlayer = GetLocalPlayer(packet.ReadByte());
		int num = packet.ReadByte();
		int description = packet.ReadInt32();
		string[] obj = new string[8]
		{
			"PlayerHint received: player=",
			localPlayer.ToString(),
			", hintType=",
			num.ToString(),
			" (",
			null,
			null,
			null
		};
		PlayerHintType playerHintType = (PlayerHintType)num;
		obj[5] = playerHintType.ToString();
		obj[6] = "), description=";
		obj[7] = description.ToString();
		Logger.DebugWriteLine(string.Concat(obj));
		_ai.OnPlayerHint(localPlayer, num, description);
	}
}
