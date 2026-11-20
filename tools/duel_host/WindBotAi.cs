using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;

internal sealed class WindBotAi
{
	private const byte StocGameMsg = 1;

	private const byte StocJoinGame = 18;

	private const byte StocTypeChange = 19;

	private const byte StocDuelStart = 21;

	private const byte StocHsPlayerEnter = 32;

	private const byte StocHsPlayerChange = 33;

	private readonly GameClient _client;

	private static readonly object RuntimeGate = new object();

	private static bool _runtimeReady;

	public int Seat { get; }

	public int SeededCards { get; private set; }

	/// <summary>Main and Extra Deck sizes of both duelists (main0, extra0, main1, extra1), set by the host when it loads the decks.</summary>
	internal static readonly int[] DeckCounts = new int[4];

	public WindBotAi(WindBotOptions options)
	{
		Seat = options.Seat;
		EnsureWindBotRuntime();
		WindBotInfo info = new WindBotInfo
		{
			Name = (string.IsNullOrWhiteSpace(options.Name) ? "MDProBot" : options.Name),
			Deck = ResolveExecutorName(options.Deck),
			DeckFile = (string.IsNullOrWhiteSpace(options.DeckFile) ? null : options.DeckFile),
			Dialog = (string.IsNullOrWhiteSpace(options.Dialog) ? "default" : options.Dialog),
			Host = "127.0.0.1",
			Port = 0,
			HostInfo = "",
			Version = 4962,
			Hand = 0,
			Debug = false,
			Chat = true
		};
		_client = new GameClient(info);
		_client.StartLocal();
		_client.Behavior.Duel.IsFirst = Seat == 0;
		Handshake();
		FeedGameStart();
		DrainResponses();
	}

	private void FeedGameStart()
	{
		Deck deck = _client.Behavior.Deck;
		int num = deck?.Cards.Count ?? 40;
		int num2 = deck?.ExtraCards.Count ?? 0;
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)1);
		binaryWriter.Write((byte)4);
		binaryWriter.Write((byte)Seat);
		binaryWriter.Write((byte)5);
		binaryWriter.Write(8000);
		binaryWriter.Write(8000);
		// The AI used to be told that both duelists have a deck of its own size. Against a larger deck, or an
		// Extra Deck when it has none itself, every card moved from beyond that size made it fail
		// ("AI feed failed: Object reference not set") and lose track of the duel.
		for (int player = 0; player < 2; player++)
		{
			bool known = DeckCounts[player * 2] > 0;
			binaryWriter.Write((short)(known ? DeckCounts[player * 2] : num));
			binaryWriter.Write((short)(known ? DeckCounts[player * 2 + 1] : num2));
		}
		_client.Feed(memoryStream.ToArray());
		SeededCards = num + num2;
	}

	public string DumpState()
	{
		ClientField clientField = _client.Behavior.Duel.Fields[0];
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ClientCard item in clientField.Hand)
		{
			stringBuilder.Append((item == null) ? "null" : (item.Id + ":type=" + item.Type + ":lv=" + item.Level + ":atk=" + item.Attack)).Append(',');
		}
		return "deck=" + clientField.Deck.Count + " hand=" + clientField.Hand.Count + " lp=" + clientField.LifePoints + " ids=[" + stringBuilder.ToString().TrimEnd(',') + "]";
	}

	private static string ResolveExecutorName(string requested)
	{
		Type[] types = typeof(DeckAttribute).Assembly.GetTypes();
		for (int i = 0; i < types.Length; i++)
		{
			if (Attribute.GetCustomAttribute(types[i], typeof(DeckAttribute)) is DeckAttribute deckAttribute && (string.Equals(deckAttribute.Name, requested, StringComparison.OrdinalIgnoreCase) || string.Equals(deckAttribute.File, requested, StringComparison.OrdinalIgnoreCase)))
			{
				return deckAttribute.Name;
			}
		}
		throw new ArgumentException("Unknown WindBot executor / deck: " + requested);
	}

	internal static void SetRandomSeed(uint seed)
	{
		EnsureWindBotRuntime();
		Program.Rand = new Random((int)seed);
	}

	private static void EnsureWindBotRuntime()
	{
		lock (RuntimeGate)
		{
			if (!_runtimeReady)
			{
				Config.Load(Array.Empty<string>());
				Program.InitDatas("cards.cdb");
				_runtimeReady = true;
			}
		}
	}

	private void Handshake()
	{
		FeedStoc(18, new byte[20]
		{
			0, 0, 0, 0, 0, 0, 5, 0, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		});
		FeedStoc(19, new byte[1] { (byte)((Seat != 0) ? 1u : 17u) });
		FeedStoc(32, PlayerEnter("MDProBot", Seat));
		FeedStoc(32, PlayerEnter("Duelist", 1 - Seat));
		FeedStoc(33, new byte[1] { (byte)Seat });
		FeedStoc(33, new byte[1] { (byte)(1 - Seat) });
		FeedStoc(21, Array.Empty<byte>());
	}

	private static byte[] PlayerEnter(string name, int position)
	{
		byte[] array = new byte[41];
		byte[] bytes = Encoding.Unicode.GetBytes(name ?? string.Empty);
		Array.Copy(bytes, array, Math.Min(bytes.Length, 40));
		array[40] = (byte)position;
		return array;
	}

	private void FeedStoc(byte function, byte[] payload)
	{
		byte[] array = new byte[payload.Length + 1];
		array[0] = function;
		Array.Copy(payload, 0, array, 1, payload.Length);
		_client.Feed(array);
	}

	public void FeedMessage(byte[] buffer, int length)
	{
		bool flag = length > 0;
		if (flag)
		{
			bool flag2;
			switch (buffer[0])
			{
			case 33:
			case 39:
			case 50:
			case 54:
			case 90:
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			buffer = buffer.AsSpan(0, length).ToArray();
			if (buffer[0] == 90 && length >= 3 && buffer[1] != Seat)
			{
				for (int i = 0; i < buffer[2] && 3 + 4 * (i + 1) <= length; i++)
				{
					int num = 3 + 4 * i;
					if ((buffer[num + 3] & 0x80) == 0)
					{
						Array.Clear(buffer, num, 4);
					}
				}
			}
			else if (buffer[0] == 50 && length == 17 && buffer[9] != Seat)
			{
				byte b = buffer[10];
				byte b2 = buffer[12];
				if ((b & 0x90) == 0 && ((b & 3) != 0 || (b2 & 0xA) != 0))
				{
					Array.Clear(buffer, 1, 4);
				}
			}
			else if (buffer[0] == 54 && length == 9)
			{
				Array.Clear(buffer, 1, 4);
			}
			else
			{
				byte b3 = buffer[0];
				flag = ((b3 == 33 || b3 == 39) ? true : false);
				if (flag && length >= 3 && buffer[1] != Seat)
				{
					Array.Clear(buffer, 3, length - 3);
				}
			}
		}
		// (The AI now reads ConfirmCards in the core's current layout, with the skip-panel byte; nothing to strip.)
		int num2;
		int num3;
		if (length >= 3)
		{
			num2 = ((buffer[0] == 90) ? 1 : 0);
			if (num2 != 0)
			{
				num3 = buffer[1] & 0xF;
				goto IL_019a;
			}
		}
		else
		{
			num2 = 0;
		}
		num3 = 0;
		goto IL_019a;
		IL_019a:
		int enginePlayer = num3;
		List<int> codes = ((num2 != 0) ? ReadDrawCodes(buffer, length) : null);
		byte[] array2 = new byte[length + 1];
		array2[0] = 1;
		Array.Copy(buffer, 0, array2, 1, length);
		_client.Feed(array2);
		if (num2 != 0)
		{
			ApplyDrawCodes(enginePlayer, codes);
		}
	}

	private static List<int> ReadDrawCodes(byte[] buffer, int length)
	{
		int num = buffer[2] & 0xFF;
		List<int> list = new List<int>();
		for (int i = 0; i < num; i++)
		{
			int num2 = 3 + i * 4;
			if (num2 + 4 > length)
			{
				break;
			}
			int num3 = buffer[num2] | (buffer[num2 + 1] << 8) | (buffer[num2 + 2] << 16) | (buffer[num2 + 3] << 24);
			if (num3 > 0 && num3 < 100000000)
			{
				list.Add(num3);
			}
		}
		return list;
	}

	private void ApplyDrawCodes(int enginePlayer, List<int>? codes)
	{
		if (codes == null || codes.Count == 0)
		{
			return;
		}
		Duel duel = _client.Behavior.Duel;
		int num = (duel.IsFirst ? enginePlayer : (1 - enginePlayer));
		IList<ClientCard> hand = duel.Fields[num].Hand;
		int num2 = 0;
		for (int i = Math.Max(0, hand.Count - codes.Count); i < hand.Count; i++)
		{
			if (num2 >= codes.Count)
			{
				break;
			}
			ClientCard clientCard = hand[i];
			if (clientCard != null)
			{
				if (clientCard.Id == 0)
				{
					clientCard.SetId(codes[num2]);
				}
				num2++;
			}
		}
	}

	public List<byte[]> DrainResponses()
	{
		List<byte[]> list = new List<byte[]>();
		while (true)
		{
			byte[] array = _client.Connection.TakeLocalPacket();
			if (array == null)
			{
				break;
			}
			if (array.Length >= 1 && array[0] == 1)
			{
				byte[] array2 = new byte[array.Length - 1];
				Array.Copy(array, 1, array2, 0, array2.Length);
				list.Add(array2);
			}
		}
		return list;
	}
}
