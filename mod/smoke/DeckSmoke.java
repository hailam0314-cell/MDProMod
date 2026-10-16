package net.mdpromod;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

public final class DeckSmoke {
    public static void main(String[] args) throws Exception {
        CardRegistry.load();
        System.out.println("cards=" + CardRegistry.size());
        CardRegistry.get(89631139).ifPresent(info -> System.out.println(
                "sample=" + info.name() + " | " + info.typeLine() + " | desc="
                        + info.desc().substring(0, Math.min(24, info.desc().length()))));

        DeckData deck = new DeckData();
        deck.setName("青眼白龙");
        for (int i = 0; i < 40; i++) deck.mainDeck().add(89631139);
        deck.extraDeck().add(10406322);
        deck.sideDeck().add(14558127);
        List<String> problems = DeckRules.problems(deck);
        System.out.println("problems=" + problems);

        DeckData legal = new DeckData();
        legal.setName("smoke_test");
        for (int i = 0; i < 40; i++) legal.mainDeck().add(32864);
        legal.extraDeck().add(10406322);
        legal.sideDeck().add(14558127);
        System.out.println("legalProblems=" + DeckRules.problems(legal));

        Path exported = DeckYdk.write(legal, Path.of("D:\\MDProMod\\runtime\\decks"));
        System.out.println("exported=" + exported + " bytes=" + Files.size(exported));
        DeckData reread = DeckData.fromYdk(exported);
        System.out.println("reread=" + reread.summary());

        CardFilter filter = new CardFilter();
        filter.monster = false;
        filter.trap = false;
        filter.spellSubMask = CardFilter.SPELL_NORMAL;
        System.out.println("normalSpells=" + countMatches(filter));
        filter.reset();
        filter.monster = false;
        filter.trap = false;
        filter.spellSubMask = CardFilter.SUB_QUICKPLAY;
        System.out.println("quickSpells=" + countMatches(filter));
        filter.reset();
        filter.monster = false;
        filter.spell = false;
        filter.trapSubMask = CardFilter.TRAP_NORMAL;
        System.out.println("normalTraps=" + countMatches(filter));
        filter.reset();
        filter.monster = false;
        filter.spell = false;
        filter.trapSubMask = CardFilter.SUB_COUNTER;
        System.out.println("counterTraps=" + countMatches(filter));
        filter.reset();
        filter.attributeMask = 0x10;
        filter.raceMask = 0x2000;
        System.out.println("lightDragons=" + countMatches(filter));
        filter.reset();
        filter.atkMin = 3000;
        System.out.println("atk3000plus=" + countMatches(filter));

        for (int code : new int[]{89631139, 83764718, 32807846, 18144506, 483,
                29401950, 44095762, 51452091}) {
            CardRegistry.get(code).ifPresent(info -> System.out.println(
                    "line " + code + " " + info.name() + " -> " + info.typeLine()));
        }
    }

    private static int countMatches(CardFilter filter) {
        int count = 0;
        for (CardInfo info : CardRegistry.all()) {
            if (filter.matches(info)) count++;
        }
        return count;
    }
}
