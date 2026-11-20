using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks;

[Deck("Horus", "AI_Horus", "Easy")]
public class HorusExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int AlexandriteDragon = 43096270;

		public const int LusterDragon = 11091375;

		public const int WhiteNightDragon = 79473793;

		public const int HorusTheBlackFlameDragonLv8 = 48229808;

		public const int HorusTheBlackFlameDragonLv6 = 11224103;

		public const int CyberDragon = 70095154;

		public const int AxeDragonute = 84914462;

		public const int DodgerDragon = 47013502;

		public const int GolemDragon = 9666558;

		public const int Raigeki = 12580477;

		public const int HammerShot = 26412047;

		public const int DarkHole = 53129443;

		public const int Fissure = 66788016;

		public const int StampingDestruction = 81385346;

		public const int FoolishBurial = 81439173;

		public const int MonsterReborn = 83764718;

		public const int MysticalSpaceTyphoon = 5318639;

		public const int BellowOfTheSilverDragon = 80600103;

		public const int Mountain = 50913601;

		public const int DragonsRebirth = 20638610;

		public const int MirrorForce = 44095762;

		public const int DimensionalPrison = 70342110;
	}

	public HorusExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.SpellSet, base.DefaultSpellSet);
		AddExecutor(ExecutorType.Activate, 11224103);
		AddExecutor(ExecutorType.Activate, 81385346, base.DefaultMysticalSpaceTyphoon);
		AddExecutor(ExecutorType.Activate, 5318639, base.DefaultMysticalSpaceTyphoon);
		AddExecutor(ExecutorType.Activate, 81439173, FoolishBurial);
		AddExecutor(ExecutorType.Activate, 50913601, base.DefaultField);
		AddExecutor(ExecutorType.Activate, 53129443, base.DefaultDarkHole);
		AddExecutor(ExecutorType.SpSummon, 70095154);
		AddExecutor(ExecutorType.Activate, 12580477, base.DefaultRaigeki);
		AddExecutor(ExecutorType.Activate, 26412047, base.DefaultHammerShot);
		AddExecutor(ExecutorType.Activate, 66788016);
		AddExecutor(ExecutorType.Activate, 80600103, BellowOfTheSilverDragon);
		AddExecutor(ExecutorType.Activate, 83764718, MonsterReborn);
		AddExecutor(ExecutorType.Summon, 79473793, WhiteNightDragon);
		AddExecutor(ExecutorType.Summon, 11224103, base.DefaultMonsterSummon);
		AddExecutor(ExecutorType.Summon, 43096270);
		AddExecutor(ExecutorType.SummonOrSet, 84914462);
		AddExecutor(ExecutorType.SummonOrSet, 47013502);
		AddExecutor(ExecutorType.MonsterSet, 9666558);
		AddExecutor(ExecutorType.SummonOrSet, 11091375);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.Activate, 48229808, HorusTheBlackFlameDragonLv8);
		AddExecutor(ExecutorType.Activate, 44095762, base.DefaultTrap);
		AddExecutor(ExecutorType.Activate, 70342110, base.DefaultTrap);
		AddExecutor(ExecutorType.Activate, 20638610, DragonsRebirth);
	}

	private bool FoolishBurial()
	{
		if (base.Bot.HasInGraveyard(79473793))
		{
			return false;
		}
		if (base.Bot.HasInHand(79473793))
		{
			return false;
		}
		int num = 2;
		foreach (ClientCard item in base.Bot.Banished)
		{
			if (item.IsCode(79473793))
			{
				num--;
			}
		}
		if (num > 0)
		{
			base.AI.SelectCard(79473793);
			return true;
		}
		return false;
	}

	private bool BellowOfTheSilverDragon()
	{
		if (base.Duel.Player == 0 && (base.Duel.Phase == DuelPhase.Draw || base.Duel.Phase == DuelPhase.Standby))
		{
			return false;
		}
		if (base.Duel.Player == 1 && base.Duel.Phase == DuelPhase.End)
		{
			return false;
		}
		List<ClientCard> list = new List<ClientCard>(base.Bot.Graveyard);
		list.Sort(CardContainer.CompareCardAttack);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard = list[num];
			if (clientCard.Attack < 1000)
			{
				return false;
			}
			if (clientCard.IsMonster() && clientCard.HasType(CardType.Normal))
			{
				base.AI.SelectCard(clientCard);
				return true;
			}
		}
		return false;
	}

	private bool MonsterReborn()
	{
		List<ClientCard> list = new List<ClientCard>(base.Bot.Graveyard.GetMatchingCards((ClientCard card) => card.IsCanRevive()));
		list.Sort(CardContainer.CompareCardAttack);
		ClientCard clientCard = null;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard2 = list[num];
			if (clientCard2.Attack < 1000)
			{
				break;
			}
			if (clientCard2.IsMonster())
			{
				clientCard = clientCard2;
				break;
			}
		}
		list = new List<ClientCard>(base.Enemy.Graveyard.GetMatchingCards((ClientCard card) => card.IsCanRevive()));
		list.Sort(CardContainer.CompareCardAttack);
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			ClientCard clientCard3 = list[num2];
			if (clientCard3.Attack < 1000)
			{
				break;
			}
			if (clientCard3.IsMonster() && clientCard3.HasType(CardType.Normal) && (clientCard == null || clientCard3.Attack > clientCard.Attack))
			{
				clientCard = clientCard3;
				break;
			}
		}
		if (clientCard != null)
		{
			base.AI.SelectCard(clientCard);
			return true;
		}
		return false;
	}

	private bool WhiteNightDragon()
	{
		if (base.Enemy.GetMonsterCount() != 0 && !base.Util.IsAllEnemyBetterThanValue(2299, onlyATK: false))
		{
			foreach (ClientCard summonableCard in base.Duel.MainPhase.SummonableCards)
			{
				if (summonableCard.IsCode(11224103))
				{
					return false;
				}
			}
		}
		return DefaultMonsterSummon();
	}

	private bool HorusTheBlackFlameDragonLv8()
	{
		return base.Duel.LastChainPlayer == 1;
	}

	private bool DragonsRebirth()
	{
		List<ClientCard> list = new List<ClientCard>(base.Bot.GetMonsters());
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort(CardContainer.CompareCardAttack);
		ClientCard clientCard = null;
		foreach (ClientCard item in list)
		{
			if (item.Attack > 2000)
			{
				return false;
			}
			if (!item.IsFacedown() && item.Race == 8192)
			{
				clientCard = item;
				break;
			}
		}
		if (clientCard == null)
		{
			return false;
		}
		list = new List<ClientCard>(base.Bot.Hand);
		list.AddRange(base.Bot.Graveyard);
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort(CardContainer.CompareCardAttack);
		ClientCard clientCard2 = null;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			ClientCard clientCard3 = list[num];
			if (clientCard3.Attack < 2300)
			{
				return false;
			}
			if (clientCard3.Race == 8192 && !clientCard3.IsCode(48229808))
			{
				clientCard2 = clientCard3;
				break;
			}
		}
		if (clientCard2 == null)
		{
			return false;
		}
		base.AI.SelectCard(clientCard);
		base.AI.SelectNextCard(clientCard2);
		return true;
	}
}
