package net.mdpromod;

import com.mojang.logging.LogUtils;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.BlockPos;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.EntityType;
import org.slf4j.Logger;

/**
 * Server-side check of the duel sessions, run on a dedicated test server with {@code -Dmdpromod.selftest=true}.
 *
 * <p>It needs no client: it places duel fields at the world spawn, lets the real duel host run on them and
 * checks what cannot be seen from a unit test - two duels at the same time, a board whose duel is gone,
 * a board whose decks are not known any more, and the start of a duel against an AI sitting on the near half.
 * The result is logged as {@code MDPROMOD SELFTEST PASS} or {@code MDPROMOD SELFTEST FAIL}, then the server stops.
 * Without the system property this class does nothing.
 */
final class DuelSelfTest {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final String PREFERRED_AI = System.getProperty("mdpromod.selftest.ai", "AI_MokeyMokey");
    private static final boolean ENABLED = Boolean.getBoolean("mdpromod.selftest");
    private static final List<String> FAILURES = new ArrayList<>();
    private static int step;
    private static int wait;
    private static int waited;
    private static DuelFieldEntity a;
    private static DuelFieldEntity b;
    private static DuelFieldEntity c;
    private static String aiKey;
    private static String aiName;
    private static String aiDialog;

    private DuelSelfTest() {
    }

    static void tick(MinecraftServer server) {
        if (!ENABLED || step < 0 || server == null) {
            return;
        }

        if (wait > 0) {
            wait--;
            return;
        }

        try {
            run(server);
        } catch (Throwable error) {
            LOGGER.error("MDPROMOD SELFTEST step {} threw", step, error);
            FAILURES.add("step " + step + " threw " + error);
            finish(server);
        }
    }

    private static void run(MinecraftServer server) throws Exception {
        ServerLevel level = server.overworld();
        DuelSessions duels = MDProMod.duels();
        switch (step) {
            case 0 -> {
                if (!ModBundle.ready()) {
                    wait = 20;
                    return;
                }

                Path root = DuelSessionManager.runtimeRoot();
                DeckData deck = null;

                for (AiDeckCatalog.Entry entry : AiDeckCatalog.entries(root)) {
                    DeckData candidate = AiDeckCatalog.deck(root, entry.key());
                    // Not every deck in the catalog has an AI behind it in the bundled duel host; this one does.
                    if (DeckRules.problems(candidate).isEmpty() && (deck == null || entry.key().equals(PREFERRED_AI))) {
                        aiKey = entry.key();
                        aiName = entry.name();
                        aiDialog = entry.dialog();
                        deck = candidate;
                    }
                }

                check(deck != null, "an AI deck that passes the deck rules exists");
                if (deck == null) {
                    finish(server);
                    return;
                }

                BlockPos spawn = level.getSharedSpawnPos();
                a = place(level, spawn, 0);
                b = place(level, spawn, 40);
                c = place(level, spawn, 80);

                // A and B: an unseated deck on the near half against the AI on the far half, started directly.
                for (DuelFieldEntity field : new DuelFieldEntity[]{a, b}) {
                    DuelSessionManager session = MDProMod.sessions(field);
                    session.setPendingDeck(field, "p1", deck);
                    field.bindDeck("p1", deck);
                    field.bindAiDeck(aiKey, aiName, aiDialog, AiDeckCatalog.deck(root, aiKey), "p2");
                    session.startForField(server, field, null);
                    field.setGameStarted(true);
                }

                check(MDProMod.sessions(a) != MDProMod.sessions(b), "two fields have two sessions");
                check(MDProMod.sessions(a) != duels.shared(), "a field's session is not the shared one");
                LOGGER.info("MDPROMOD SELFTEST: two duels started with AI deck {}", aiKey);
                next(200);
            }
            case 1 -> {
                check(MDProMod.sessions(a).hasLiveDuel(a), "duel A is running: " + MDProMod.sessions(a).statusSummary());
                check(MDProMod.sessions(b).hasLiveDuel(b), "duel B is running: " + MDProMod.sessions(b).statusSummary());
                check(duels.running() == 2, "two duel hosts at once (running=" + duels.running() + ")");
                check(cards(level, a) > 0, "duel A put cards on its field (" + cards(level, a) + ")");
                check(cards(level, b) > 0, "duel B put cards on its field (" + cards(level, b) + ")");

                // The duel of A disappears while the board still says "in a duel" (what a world reload leaves behind).
                MDProMod.sessions(a).stop();
                check(a.isGameStarted(), "board A is still flagged as in a duel right after its duel vanished");
                next(45);
            }
            case 2 -> {
                check(!a.isGameStarted() && !a.hasResult(), "stale board A went back to preparation");
                check(a.hasDeck("p1") && a.hasDeck("p2"), "board A kept both decks");
                check(a.lifePoints("p1") == 8000 && a.lifePoints("p2") == 8000, "board A shows 8000 LP again");
                check(MDProMod.sessions(b).hasLiveDuel(b), "duel B was not disturbed");

                // After a restart the session of A knows nothing: the human deck must go, the AI deck can stay.
                duels.forget(a.getUUID(), MDProMod.sessions(a));
                next(45);
            }
            case 3 -> {
                check(!a.hasDeck("p1"), "board A dropped the deck nobody knows any more");
                check(a.hasDeck("p2") && a.aiSide() == 1, "board A kept its AI deck");

                // C: the AI on the near half (seat 0), a deck on the far half - the case that used to be taken for
                // a player duel. Nobody is online, so the far seat picks at random when the 30 s run out.
                Path root = DuelSessionManager.runtimeRoot();
                DeckData deck = AiDeckCatalog.deck(root, aiKey);
                DuelSessionManager session = MDProMod.sessions(c);
                session.setPendingDeck(c, "p2", deck);
                c.bindDeck("p2", deck);
                c.bindAiDeck(aiKey, aiName, aiDialog, AiDeckCatalog.deck(root, aiKey), "p1");
                check(session.requestDuelStart(c, null), "rock-paper-scissors starts with the AI on the near half");
                check(session.isStarting() && session.isBusy(), "field C is in its start phase");
                check(!MDProMod.sessions(b).isStarting(), "field B is not affected by the start of C");
                waited = 0;
                next(20);
            }
            case 4 -> {
                waited += 20;
                if (!c.isGameStarted() && waited < 20 * 240) {
                    wait = 20;
                    return;
                }

                check(c.isGameStarted(), "duel C started after rock-paper-scissors (waited " + waited / 20 + " s)");
                next(100);
            }
            case 5 -> {
                check(MDProMod.sessions(c).hasLiveDuel(c), "duel C is running: " + MDProMod.sessions(c).statusSummary());
                check(MDProMod.sessions(b).hasLiveDuel(b), "duel B is still running next to C");
                check(duels.running() == 2, "B and C run at once (running=" + duels.running() + ")");

                // Destroying a board ends its duel.
                b.discard();
                check(duels.running() == 1, "removing board B stopped its duel (running=" + duels.running() + ")");
                check(cards(level, b) == 0, "the cards of board B are gone");

                // Server stop: nothing may stay "in a duel".
                duels.shutdown();
                check(duels.running() == 0, "no duel host survives the shutdown");
                check(!c.isGameStarted(), "board C is not saved as in a duel");
                check(cards(level, c) == 0, "the cards of board C are gone");
                finish(server);
            }
            default -> step = -1;
        }
    }

    private static DuelFieldEntity place(ServerLevel level, BlockPos spawn, int dx) {
        DuelFieldEntity field = (DuelFieldEntity)((EntityType<?>)ModEntities.DUEL_FIELD.get()).create(level);
        field.moveTo(spawn.getX() + dx + 0.5, spawn.getY() + 6.05, spawn.getZ() + 0.5, 0.0F, 0.0F);
        if (!level.addFreshEntity(field)) {
            throw new IllegalStateException("could not place a duel field at x+" + dx);
        }

        return field;
    }

    private static int cards(ServerLevel level, DuelFieldEntity field) {
        int count = 0;

        for (DuelCardEntity card : level.getEntitiesOfClass(DuelCardEntity.class, field.getBoundingBox().inflate(28.0))) {
            if (card.belongsTo(field.getUUID())) {
                count++;
            }
        }

        for (HandCardEntity hand : level.getEntitiesOfClass(HandCardEntity.class, field.getBoundingBox().inflate(28.0))) {
            if (field.getUUID().toString().equals(hand.fieldId())) {
                count++;
            }
        }

        return count;
    }

    private static void check(boolean ok, String what) {
        if (ok) {
            LOGGER.info("MDPROMOD SELFTEST ok: {}", what);
        } else {
            LOGGER.error("MDPROMOD SELFTEST NOT OK: {}", what);
            FAILURES.add(what);
        }
    }

    private static void next(int ticks) {
        step++;
        wait = ticks;
    }

    private static void finish(MinecraftServer server) {
        step = -1;

        for (DuelFieldEntity field : new DuelFieldEntity[]{a, b, c}) {
            if (field != null && !field.isRemoved()) {
                field.discard();
            }
        }

        if (FAILURES.isEmpty()) {
            LOGGER.info("MDPROMOD SELFTEST PASS");
        } else {
            LOGGER.error("MDPROMOD SELFTEST FAIL ({}): {}", FAILURES.size(), String.join(" | ", FAILURES));
        }

        server.halt(false);
    }
}
