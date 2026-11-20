namespace WindBot.Game.AI.Decks;

[Deck("OldSchool", "AI_OldSchool", "Easy")]
public class OldSchoolExecutor : DefaultExecutor
{
	public class CardId
	{
		public const int AncientGearGolem = 83104731;

		public const int Frostosaurus = 6631034;

		public const int AlexandriteDragon = 43096270;

		public const int GeneWarpedWarwolf = 69247929;

		public const int GearGolemTheMovingFortress = 30190809;

		public const int EvilswarmHeliotrope = 77542832;

		public const int LusterDragon = 11091375;

		public const int InsectKnight = 35052053;

		public const int ArchfiendSoldier = 49881766;

		public const int HeavyStorm = 19613556;

		public const int DarkHole = 53129443;

		public const int Raigeki = 12580477;

		public const int HammerShot = 26412047;

		public const int Fissure = 66788016;

		public const int SwordsOfRevealingLight = 72302403;

		public const int DoubleSummon = 43422537;

		public const int MirrorForce = 44095762;

		public const int DimensionalPrison = 70342110;
	}

	private int _lastDoubleSummon;

	public OldSchoolExecutor(GameAI ai, Duel duel)
		: base(ai, duel)
	{
		AddExecutor(ExecutorType.Activate, 19613556, base.DefaultHeavyStorm);
		AddExecutor(ExecutorType.SpellSet, base.DefaultSpellSet);
		AddExecutor(ExecutorType.Activate, 53129443, base.DefaultDarkHole);
		AddExecutor(ExecutorType.Activate, 12580477, base.DefaultRaigeki);
		AddExecutor(ExecutorType.Activate, 26412047, base.DefaultHammerShot);
		AddExecutor(ExecutorType.Activate, 66788016);
		AddExecutor(ExecutorType.Activate, 72302403, SwordsOfRevealingLight);
		AddExecutor(ExecutorType.Activate, 43422537, DoubleSummon);
		AddExecutor(ExecutorType.Summon, 83104731, base.DefaultMonsterSummon);
		AddExecutor(ExecutorType.Summon, 6631034, base.DefaultMonsterSummon);
		AddExecutor(ExecutorType.SummonOrSet, 43096270);
		AddExecutor(ExecutorType.SummonOrSet, 69247929);
		AddExecutor(ExecutorType.MonsterSet, 30190809);
		AddExecutor(ExecutorType.SummonOrSet, 77542832);
		AddExecutor(ExecutorType.SummonOrSet, 11091375);
		AddExecutor(ExecutorType.SummonOrSet, 35052053);
		AddExecutor(ExecutorType.SummonOrSet, 49881766);
		AddExecutor(ExecutorType.Repos, base.DefaultMonsterRepos);
		AddExecutor(ExecutorType.Activate, 44095762, base.DefaultTrap);
		AddExecutor(ExecutorType.Activate, 70342110, base.DefaultTrap);
	}

	private bool DoubleSummon()
	{
		if (_lastDoubleSummon == base.Duel.Turn)
		{
			return false;
		}
		if (base.Duel.MainPhase.SummonableCards.Count == 0)
		{
			return false;
		}
		if (base.Duel.MainPhase.SummonableCards.Count == 1 && base.Duel.MainPhase.SummonableCards[0].Level < 5)
		{
			bool flag = false;
			foreach (ClientCard item in base.Bot.Hand)
			{
				if (item.IsMonster() && item.Level > 4 && item.Level < 6)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		int num = 0;
		foreach (ClientCard item2 in base.Bot.Hand)
		{
			if (item2.IsMonster())
			{
				num++;
			}
		}
		if (num <= 1)
		{
			return false;
		}
		_lastDoubleSummon = base.Duel.Turn;
		return true;
	}

	private bool SwordsOfRevealingLight()
	{
		foreach (ClientCard monster in base.Enemy.GetMonsters())
		{
			if (monster.IsFacedown())
			{
				return true;
			}
		}
		return base.Util.IsOneEnemyBetter(onlyATK: true);
	}
}
