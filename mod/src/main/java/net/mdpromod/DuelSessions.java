package net.mdpromod;

import com.mojang.logging.LogUtils;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import net.minecraft.server.level.ServerPlayer;
import org.slf4j.Logger;

/**
 * All duels of the server: one {@link DuelSessionManager} per duel field.
 *
 * <p>MDProMod used to have a single session for the whole server, so a second pair of players could not duel
 * while a duel was running, and everything a player did was applied to whichever field that one session was
 * last linked to. Requests are now routed to the session of the field they are about, or, for answers that
 * only say who sent them, to the session the player sits in.
 *
 * <p>{@link #shared()} is the session of the old days. It is kept for the debug commands and for add-ons
 * (MDWheel) that start their duels on it directly; a field that the shared session is linked to is routed to it.
 */
final class DuelSessions {
    private static final Logger LOGGER = LogUtils.getLogger();
    private static final int PRUNE_INTERVAL_TICKS = 200;
    private final DuelSessionManager shared = new DuelSessionManager();
    /** Answers from players who sit nowhere end up here; it never has a duel, so they are ignored. */
    private final DuelSessionManager nobody = new DuelSessionManager();
    private final Map<UUID, DuelSessionManager> byField = new LinkedHashMap<>();
    private int ticks;

    DuelSessionManager shared() {
        return this.shared;
    }

    /** The session of a duel field; created on first use. */
    synchronized DuelSessionManager of(DuelFieldEntity field) {
        if (field == null) {
            return this.nobody;
        }

        UUID id = field.getUUID();
        if (this.shared.isLinkedTo(id)) {
            return this.shared;
        }

        DuelSessionManager session = this.byField.get(id);
        if (session == null) {
            session = new DuelSessionManager();
            this.byField.put(id, session);
        }

        session.attach(field);
        return session;
    }

    /** The session a player sits in, preferring one with a duel being started or played. */
    synchronized DuelSessionManager of(ServerPlayer player) {
        if (player == null) {
            return this.nobody;
        }

        DuelSessionManager seated = null;

        for (DuelSessionManager session : this.all()) {
            if (session.involves(player)) {
                if (session.isBusy()) {
                    return session;
                }

                if (seated == null || session.isRunning()) {
                    seated = session;
                }
            }
        }

        return seated == null ? this.nobody : seated;
    }

    /** Another field than {@code field} where the player is in the middle of a duel, or null. */
    synchronized DuelSessionManager busyElsewhere(ServerPlayer player, DuelFieldEntity field) {
        DuelSessionManager here = this.of(field);

        for (DuelSessionManager session : this.all()) {
            if (session != here && session.involves(player) && session.isBusy()) {
                return session;
            }
        }

        return null;
    }

    /** A player sits at one board at a time: taking a seat gives up the seats at boards without a duel. */
    synchronized void vacateOthers(ServerPlayer player, DuelSessionManager keep) {
        for (DuelSessionManager session : this.all()) {
            if (session != keep && session.involves(player)) {
                session.vacate(player);
            }
        }
    }

    synchronized void handleFieldClick(ServerPlayer player, String controlId) {
        if (player == null || controlId == null || controlId.isBlank()) {
            return;
        }

        DuelFieldLayout.Control control = DuelFieldLayout.controlById(controlId).orElse(null);
        if (control == null) {
            return;
        }

        DuelFieldEntity target = null;

        for (DuelFieldEntity field : player.level().getEntitiesOfClass(DuelFieldEntity.class, player.getBoundingBox().inflate(24.0))) {
            DuelFieldLayout.Control aimed = field.controlUnderCrosshair(player);
            if (aimed != null && aimed.id().equals(control.id())) {
                target = field;
                break;
            }
        }

        if (target == null) {
            // The client decided what was clicked a moment earlier than the server sees the player's view.
            target = this.of(player).field(player.server);
        }

        if (target != null) {
            this.of(target).handleFieldClick(player, target, control);
        }
    }

    synchronized void tick() {
        for (DuelSessionManager session : this.all()) {
            try {
                session.tickField();
            } catch (RuntimeException ex) {
                // One broken duel must not stop the others (or the server tick).
                LOGGER.error("Duel session tick failed", ex);
            }
        }

        if (++this.ticks % PRUNE_INTERVAL_TICKS == 0) {
            this.byField.values().removeIf(DuelSessionManager::isDisposable);
        }
    }

    synchronized void onPlayerLeft(ServerPlayer player) {
        for (DuelSessionManager session : this.all()) {
            session.onPlayerLeft(player);
        }
    }

    /** A duel field entity was destroyed. */
    synchronized void fieldRemoved(DuelFieldEntity field) {
        UUID id = field.getUUID();
        DuelSessionManager session = this.shared.isLinkedTo(id) ? this.shared : this.byField.remove(id);
        if (session != null) {
            session.fieldRemoved(field);
        }
    }

    synchronized void forget(UUID fieldId, DuelSessionManager session) {
        this.byField.remove(fieldId, session);
    }

    /** Repairs a board whose saved state does not belong to any duel (see {@link DuelSessionManager#reconcile}). */
    void reconcile(DuelFieldEntity field) {
        this.of(field).reconcile(field);
    }

    synchronized void shutdown() {
        for (DuelSessionManager session : this.all()) {
            try {
                session.shutdown();
            } catch (RuntimeException ex) {
                LOGGER.error("Stopping a duel session failed", ex);
            }
        }

        this.byField.clear();
    }

    synchronized int running() {
        int count = 0;

        for (DuelSessionManager session : this.all()) {
            if (session.isRunning()) {
                count++;
            }
        }

        return count;
    }

    private List<DuelSessionManager> all() {
        List<DuelSessionManager> all = new ArrayList<>(this.byField.size() + 1);
        all.add(this.shared);
        all.addAll(this.byField.values());
        return all;
    }
}
