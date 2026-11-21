package net.mdpromod;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.util.Mth;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientAnimBridge {
    private static final int MAX_SPRITES = 384;
    private static final Map<Integer, ClientAnimBridge.LegacyState> LEGACY = new HashMap<>();
    private static final LinkedHashMap<ClientAnimBridge.EventKey, ClientAnimBridge.SpriteState> SPRITES = new LinkedHashMap<>();
    private static final LinkedHashMap<ClientAnimBridge.EventKey, Boolean> RECENT = new LinkedHashMap<>();
    private static final LinkedHashMap<Integer, ClientAnimBridge.PrivateCard> PRIVATE_CARDS = new LinkedHashMap<>();
    private static final LinkedHashMap<UUID, Integer> SESSION_FIELDS = new LinkedHashMap<>();
    private static final LinkedHashMap<UUID, Boolean> CLOSED_SESSIONS = new LinkedHashMap<>();
    private static ClientLevel currentLevel;
    private static long previousNano = System.nanoTime();
    private static double clockMs;

    private ClientAnimBridge() {
    }

    public static void apply(ModNetwork.Anim message) {
        double now = now();
        if (currentLevel != null) {
            LEGACY.put(
                message.entityId(),
                new ClientAnimBridge.LegacyState(
                    message.kind(),
                    new Vec3(message.fromX(), message.fromY(), message.fromZ()),
                    new Vec3(message.toX(), message.toY(), message.toZ()),
                    now,
                    Mth.clamp(message.durationMs(), 80, 10000),
                    message.rollDegrees()
                )
            );
        }
    }

    public static void apply(ModNetwork.DuelAnimation message) {
        double now = now();
        if (currentLevel != null
            && message.sessionId() != null
            && valid(message.from())
            && valid(message.to())
            && Float.isFinite(message.fromYaw())
            && Float.isFinite(message.toYaw())
            && Float.isFinite(message.fromPitch())
            && Float.isFinite(message.toPitch())) {
            if (!CLOSED_SESSIONS.containsKey(message.sessionId())) {
                ClientAnimBridge.EventKey key = new ClientAnimBridge.EventKey(message.sessionId(), message.eventId());
                if (!RECENT.containsKey(key)) {
                    RECENT.put(key, Boolean.TRUE);
                    trim(RECENT, 2048);
                    SESSION_FIELDS.put(message.sessionId(), message.fieldId());
                    trim(SESSION_FIELDS, 64);
                    double start = now + Mth.clamp(message.delayMs(), 0, 30000);
                    ClientAnimBridge.Frame continued = null;
                    if (message.hideEntityId() >= 0) {
                        Iterator<ClientAnimBridge.SpriteState> iterator = SPRITES.values().iterator();

                        while (iterator.hasNext()) {
                            ClientAnimBridge.SpriteState previous = iterator.next();
                            if (previous.event.sessionId().equals(message.sessionId())
                                && previous.event.hideEntityId() == message.hideEntityId()
                                && !(previous.endMs() <= now)) {
                                if ("reposition".equals(message.kind()) && "reposition".equals(previous.event.kind())) {
                                    continued = sample(previous, clamp((now - previous.startMs) / previous.durationMs));
                                    if (previous.startMs > now) {
                                        start = Math.max(start, previous.startMs);
                                    }

                                    iterator.remove();
                                } else {
                                    start = Math.max(start, previous.endMs());
                                }
                            }
                        }
                    }

                    SpriteState sprite = new SpriteState(message, Math.min(now + 30000.0, start), continued);
                    SPRITES.put(key, sprite);
                    ClientBattleCamera.onAnimation(message, sprite.startMs, sprite.endMs());
                    if (message.kind().equals("pendulum_summon")) {
                        ClientPendulumEffects.land(message, sprite.endMs());
                    }
                    trim(SPRITES, 384);
                }
            }
        }
    }

    public static void applyCardKnowledge(int entityId, int cardCode) {
        double now = now();
        if (currentLevel != null) {
            ClientAnimBridge.PrivateCard card = new ClientAnimBridge.PrivateCard(cardCode, now);
            Entity entity = currentLevel.getEntity(entityId);
            if (entity != null) {
                card.entityUuid = entity.getUUID();
            }

            PRIVATE_CARDS.put(entityId, card);
            trim(PRIVATE_CARDS, 1024);
        }
    }

    public static int privateCardCode(int entityId, int publicCode) {
        now();
        if (publicCode > 0) {
            return publicCode;
        }

        ClientAnimBridge.PrivateCard knowledge = PRIVATE_CARDS.get(entityId);
        if (knowledge != null && currentLevel != null) {
            Entity entity = currentLevel.getEntity(entityId);
            if (entity == null) {
                return 0;
            }

            if (knowledge.entityUuid == null) {
                knowledge.entityUuid = entity.getUUID();
            }

            if (!knowledge.entityUuid.equals(entity.getUUID())) {
                PRIVATE_CARDS.remove(entityId);
                return 0;
            } else {
                return knowledge.code;
            }
        } else {
            return 0;
        }
    }

    public static void clearSession(UUID sessionId) {
        CLOSED_SESSIONS.put(sessionId, Boolean.TRUE);
        trim(CLOSED_SESSIONS, 64);
        Map<Integer, Boolean> fieldIds = new HashMap<>();
        Integer rememberedField = SESSION_FIELDS.remove(sessionId);
        if (rememberedField != null) {
            fieldIds.put(rememberedField, Boolean.TRUE);
        }

        SPRITES.values().removeIf(state -> {
            if (!state.event.sessionId().equals(sessionId)) {
                return false;
            }

            fieldIds.put(state.event.fieldId(), Boolean.TRUE);
            PRIVATE_CARDS.remove(state.event.hideEntityId());
            LEGACY.remove(state.event.hideEntityId());
            return true;
        });
        RECENT.keySet().removeIf(key -> key.session().equals(sessionId));
        if (currentLevel != null) {
            PRIVATE_CARDS.entrySet().removeIf(entry -> {
                Entity entity = currentLevel.getEntity(entry.getKey());
                if (entity instanceof HandCardEntity hand) {
                    for (int fieldId : fieldIds.keySet()) {
                        Entity field = currentLevel.getEntity(fieldId);
                        if (field != null && hand.fieldId().equals(field.getUUID().toString())) {
                            return true;
                        }
                    }

                    return false;
                } else {
                    return entity == null;
                }
            });
        }
    }

    public static void clear() {
        LEGACY.clear();
        SPRITES.clear();
        RECENT.clear();
        PRIVATE_CARDS.clear();
        SESSION_FIELDS.clear();
        CLOSED_SESSIONS.clear();
        clockMs = 0.0;
        previousNano = System.nanoTime();
    }

    public static int activeCount() {
        double now = now();
        cleanup(now);
        return LEGACY.size() + SPRITES.size();
    }

    public static boolean shouldHide(Entity entity) {
        double now = now();

        for (ClientAnimBridge.SpriteState state : SPRITES.values()) {
            if (state.event.hideEntityId() == entity.getId() && now < state.endMs()) {
                return true;
            }
        }

        return false;
    }

    public static ClientAnimBridge.Transform renderOffset(Entity entity, float partialTick) {
        double now = now();
        ClientAnimBridge.LegacyState state = LEGACY.get(entity.getId());
        if (state == null) {
            return null;
        } else {
            double t = (now - state.startMs()) / state.durationMs();
            if (t >= 1.0) {
                LEGACY.remove(entity.getId());
                return null;
            } else {
                t = clamp(t);
                Vec3 target = state.from().lerp(state.to(), smoother(t)).add(0.0, Math.sin(Math.PI * t) * liftHeight(state.kind()), 0.0);
                Vec3 origin = new Vec3(
                    Mth.lerp(partialTick, entity.xOld, entity.getX()),
                    Mth.lerp(partialTick, entity.yOld, entity.getY()),
                    Mth.lerp(partialTick, entity.zOld, entity.getZ())
                );
                return new ClientAnimBridge.Transform(target.subtract(origin), (float)(Math.sin(Math.PI * t) * state.roll()));
            }
        }
    }

    public static List<ClientAnimBridge.Frame> frames() {
        double now = now();
        List<ClientAnimBridge.Frame> frames = new ArrayList<>(SPRITES.size());
        Map<Integer, Boolean> displayedEntities = new HashMap<>();

        for (ClientAnimBridge.SpriteState state : SPRITES.values()) {
            if (!(now < state.startMs)) {
                double t = clamp((now - state.startMs) / state.durationMs);
                if ("attack".equals(state.event.kind()) && t >= 0.55 && !state.burstDone) {
                    state.burstDone = true;
                    Vec3 hit = state.event.to().add(0, 0.44, 0);
                    currentLevel.addParticle(ParticleTypes.FLASH, hit.x, hit.y, hit.z, 0, 0, 0);
                    for (int j = 0; j < 10; j++) {
                        double angle = j * Math.PI / 5;
                        currentLevel.addParticle(ParticleTypes.CRIT, hit.x, hit.y, hit.z,
                            0.23 * Math.cos(angle), 0.14, 0.23 * Math.sin(angle));
                    }
                }
                if ("destroy".equals(state.event.kind()) && t >= 0.2 && !state.burstDone) {
                    state.burstDone = true;
                    destructionBurst(cardCenter(state.event.from(), state.event.fromPitch(), height(state.event.fromPitch())));
                }

                if (!(now >= state.endMs())) {
                    frames.add(sample(state, t));
                    if (state.event.hideEntityId() >= 0) {
                        displayedEntities.put(state.event.hideEntityId(), Boolean.TRUE);
                    }
                }
            }
        }

        Map<Integer, ClientAnimBridge.SpriteState> pending = new HashMap<>();

        for (ClientAnimBridge.SpriteState state : SPRITES.values()) {
            int entityId = state.event.hideEntityId();
            if (entityId >= 0 && !(now >= state.startMs) && !displayedEntities.containsKey(entityId)) {
                ClientAnimBridge.SpriteState previous = pending.get(entityId);
                if (previous == null || state.startMs < previous.startMs) {
                    pending.put(entityId, state);
                }
            }
        }

        for (ClientAnimBridge.SpriteState state : pending.values()) {
            frames.add(sample(state, 0.0));
        }

        cleanup(now);
        return frames;
    }

    private static ClientAnimBridge.Frame sample(ClientAnimBridge.SpriteState state, double t) {
        ModNetwork.DuelAnimation e = state.event;
        float fromW = width(e.fromPitch());
        float fromH = height(e.fromPitch());
        float toW = width(e.toPitch());
        float toH = height(e.toPitch());
        Vec3 from = cardCenter(e.from(), e.fromPitch(), fromH);
        Vec3 to = cardCenter(e.to(), e.toPitch(), toH);
        float startYaw = e.fromYaw() - (e.fromDefense() ? 90.0F : 0.0F);
        float endYaw = e.toYaw() - (e.toDefense() ? 90.0F : 0.0F);
        float startPitch = e.fromPitch();
        if (state.continuedFrom != null) {
            from = state.continuedFrom.center();
            startYaw = state.continuedFrom.yaw();
            startPitch = state.continuedFrom.pitch();
            fromW = state.continuedFrom.width();
            fromH = state.continuedFrom.height();
        }

        double p = smoother(t);
        Vec3 center = from.lerp(to, p);
        float yaw = angle(startYaw, endYaw, p);
        float pitch = angle(startPitch, e.toPitch(), p);
        float roll = 0.0F;
        float scale = 1.0F;
        float cardW = (float)Mth.lerp(p, fromW, toW);
        float cardH = (float)Mth.lerp(p, fromH, toH);
        switch (e.kind()) {
            case "attack": {
                Vec3 toward = to.subtract(from);
                Vec3 direction = new Vec3(toward.x, 0.0, toward.z).normalize();
                if (direction.lengthSqr() < 0.001) {
                    direction = new Vec3(0.0, 0.0, -1.0).yRot(-((float)Math.toRadians(e.fieldYaw())));
                }

                Vec3 charge = from.subtract(direction.scale(0.52)).add(0.0, 0.65, 0.0);
                Vec3 impact = to.subtract(direction.scale(0.25)).add(0.0, 0.3, 0.0);
                if (t < 0.3) {
                    center = from.lerp(charge, smoother(t / 0.3));
                } else if (t < 0.55) {
                    double sprint = (t - 0.3) / 0.25;
                    center = charge.lerp(impact, sprint * sprint);
                } else if (t < 0.63) {
                    center = impact;
                } else {
                    center = impact.lerp(from, smoother((t - 0.63) / 0.37));
                }

                yaw = startYaw;
                pitch = startPitch;
                cardW = fromW;
                cardH = fromH;
                roll = (float)(-10.0 * Math.sin(Math.PI * t));
                break;
            }
            case "attack_declare":
                Vec3 direction = new Vec3(0.0, 0.0, -1.0).yRot(-((float)Math.toRadians(e.fieldYaw())));
                Vec3 charge = from.subtract(direction.scale(0.42)).add(0.0, 0.5, 0.0);
                if (t < 0.36) {
                    center = from.lerp(charge, smoother(t / 0.36));
                } else if (t < 0.68) {
                    center = charge;
                } else {
                    center = charge.lerp(from, smoother((t - 0.68) / 0.32));
                }

                yaw = startYaw;
                pitch = startPitch;
                roll = (float)(-6.0 * Math.sin(Math.PI * t));
                cardW = fromW;
                cardH = fromH;
                break;
            case "scale_place":
            case "scale_effect":
                // The scale stands up at its destination without travelling above the field.
                double standing = "scale_effect".equals(e.kind()) ? 1.0 : smoother(t);
                pitch = (float)(90.0 * (1.0 - standing));
                cardW = 0.859375F;
                cardH = 1.25F;
                center = e.to().add(0, 0.03 + 0.5 * cardH * Math.cos(Math.toRadians(pitch)), 0);
                yaw = endYaw;
                break;
            case "material":
                center = from.lerp(to, 0.18 * p).add(0.0, 1.25 * smoother(Math.min(1.0, t / 0.68)), 0.0);
                scale = (float)(1.0 - smoother(clamp((t - 0.42) / 0.58)));
                yaw = startYaw + (float)(35.0 * Math.sin(Math.PI * t));
                pitch = startPitch;
                cardW = fromW;
                cardH = fromH;
                break;
            case "effect":
            case "effect_reveal":
            case "reveal":
            case "search_reveal":
                double raise = t < 0.22 ? smoother(t / 0.22) : (t > 0.78 ? 1.0 - smoother((t - 0.78) / 0.22) : 1.0);
                center = from.add(0.0, (e.kind().startsWith("effect") ? 0.64 : 0.95) * raise, 0.0);
                pitch = "effect".equals(e.kind()) ? startPitch : angle(startPitch, 0.0F, raise);
                cardW = fromW;
                cardH = fromH;
                if (!"effect".equals(e.kind())) {
                    double turn = t < 0.16 ? smoother(t / 0.16) : (t < 0.47 ? 1.0 : (t < 0.66 ? 1.0 - smoother((t - 0.47) / 0.19) : 0.0));
                    yaw = startYaw + (float)(180.0 * turn);
                } else {
                    yaw = startYaw;
                }
                break;
            case "destroy":
                double fly = clamp((t - 0.22) / 0.78);
                p = smoother(fly);
                center = from.lerp(to, p).add(0.0, Math.sin(Math.PI * fly) * 0.65, 0.0);
                yaw = angle(startYaw, endYaw, p);
                pitch = angle(startPitch, e.toPitch(), p);
                roll = (float)(8.0 * Math.sin(Math.PI * fly));
                break;
            case "reposition":
                center = from.lerp(to, p);
                break;
            case "flip":
            case "position":
                center = from.lerp(to, p).add(0.0, Math.sin(Math.PI * t) * 0.35, 0.0);
                if ("flip".equals(e.kind())) {
                    roll = (float)(180.0 * Math.sin(Math.PI * t));
                }
                break;
            case "shuffle":
                double motion = Math.sin((Math.PI * 6) * t) * Math.sin(Math.PI * t);
                Vec3 lateral = new Vec3(0.18 * motion, 0.0, 0.0).yRot(-((float)Math.toRadians(e.fieldYaw())));
                center = from.add(lateral).add(0.0, Math.sin(Math.PI * t) * 0.22, 0.0);
                yaw = startYaw + (float)(motion * 8.0);
                pitch = startPitch;
                cardW = fromW;
                cardH = fromH;
                break;
            case "negate": {
                double envelope = Math.sin(Math.PI * t);
                Vec3 shake = new Vec3(Math.sin((Math.PI * 8) * t) * 0.08 * envelope, 0.0, 0.0).yRot(-((float)Math.toRadians(e.fieldYaw())));
                center = from.add(shake).add(0.0, 0.14 * envelope, 0.0);
                yaw = startYaw;
                pitch = startPitch;
                roll = (float)(Math.sin((Math.PI * 8) * t) * 8.0 * envelope);
                scale = (float)(1.0 - 0.08 * envelope);
                cardW = fromW;
                cardH = fromH;
                break;
            }
            case "attack_disabled": {
                double envelope = Math.sin(Math.PI * t);
                center = from.add(0.0, 0.12 * envelope, 0.0);
                roll = (float)(Math.sin((Math.PI * 10) * t) * 7.0 * envelope);
                yaw = startYaw;
                pitch = startPitch;
                cardW = fromW;
                cardH = fromH;
                break;
            }
            default:
                double distance = Math.sqrt(from.distanceToSqr(to));
                double lift = Math.min(1.25, liftHeight(e.kind()) + distance * 0.025);
                center = center.add(0.0, Math.sin(Math.PI * t) * lift, 0.0);
                roll = (float)(-8.0 * Math.sin(Math.PI * t));
        }

        return new ClientAnimBridge.Frame(
            e.sessionId(),
            e.eventId(),
            e.kind(),
            e.cardBack(),
            e.faceUp() ? Math.max(0, e.cardCode()) : 0,
            e.faceUp(),
            center,
            yaw,
            pitch,
            roll,
            cardW,
            cardH,
            Math.max(0.0F, scale)
        );
    }

    private static Vec3 cardCenter(Vec3 anchor, float pitch, float cardH) {
        double upright = 1.0 - clamp(Math.abs(pitch) / 90.0);
        return anchor.add(0.0, upright * cardH * 0.5 + (1.0 - upright) * 0.03, 0.0);
    }

    private static float width(float pitch) {
        return Math.abs(pitch) < 45.0F ? 0.55F : 0.859375F;
    }

    private static float height(float pitch) {
        return Math.abs(pitch) < 45.0F ? 0.8F : 1.25F;
    }

    private static float angle(float from, float to, double p) {
        return from + Mth.wrapDegrees(to - from) * (float)p;
    }

    private static double smoother(double t) {
        t = clamp(t);
        return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
    }

    private static double clamp(double t) {
        return Math.max(0.0, Math.min(1.0, t));
    }

    private static double liftHeight(String kind) {
        return switch (kind) {
            case "summon", "special_summon", "pendulum_summon" -> 0.85;
            case "set" -> 0.6;
            case "draw", "search" -> 0.45;
            default -> 0.55;
        };
    }

    private static boolean valid(Vec3 p) {
        return p != null
            && Double.isFinite(p.x)
            && Double.isFinite(p.y)
            && Double.isFinite(p.z)
            && Math.abs(p.x) <= 3.0000064E7
            && Math.abs(p.z) <= 3.0000064E7
            && Math.abs(p.y) <= 100000.0;
    }

    static double now() {
        Minecraft minecraft = Minecraft.getInstance();
        long nano = System.nanoTime();
        if (minecraft.level != currentLevel) {
            clear();
            currentLevel = minecraft.level;
            previousNano = nano;
        }

        if (!minecraft.isPaused()) {
            clockMs = clockMs + Math.max(0.0, (nano - previousNano) / 1000000.0);
        }

        previousNano = nano;
        return clockMs;
    }

    private static void cleanup(double now) {
        SPRITES.values().removeIf(state -> now >= state.endMs());
        LEGACY.values().removeIf(state -> now >= state.startMs() + state.durationMs());
        PRIVATE_CARDS.entrySet()
            .removeIf(entry -> currentLevel == null || now - entry.getValue().receivedMs > 5000.0 && currentLevel.getEntity(entry.getKey()) == null);
    }

    private static <K, V> void trim(LinkedHashMap<K, V> map, int max) {
        while (map.size() > max) {
            Iterator<K> iterator = map.keySet().iterator();
            iterator.next();
            iterator.remove();
        }
    }

    private static void destructionBurst(Vec3 center) {
        if (currentLevel != null) {
            currentLevel.addParticle(ParticleTypes.EXPLOSION, center.x, center.y + 0.15, center.z, 0.0, 0.0, 0.0);

            for (int i = 0; i < 18; i++) {
                double angle = i * Math.PI * 2.0 / 18.0;
                double speed = 0.04 + currentLevel.random.nextDouble() * 0.05;
                currentLevel.addParticle(
                    i % 3 == 0 ? ParticleTypes.SMOKE : ParticleTypes.CRIT,
                    center.x,
                    center.y + 0.15,
                    center.z,
                    Math.cos(angle) * speed,
                    0.035 + currentLevel.random.nextDouble() * 0.035,
                    Math.sin(angle) * speed
                );
            }
        }
    }

    private record EventKey(UUID session, long event) {
    }

    public record Frame(
        UUID sessionId,
        long eventId,
        String kind,
        int cardBack,
        int cardCode,
        boolean faceUp,
        Vec3 center,
        float yaw,
        float pitch,
        float roll,
        float width,
        float height,
        float scale
    ) {
    }

    private record LegacyState(String kind, Vec3 from, Vec3 to, double startMs, int durationMs, int roll) {
    }

    private static final class PrivateCard {
        final int code;
        final double receivedMs;
        UUID entityUuid;

        PrivateCard(int code, double receivedMs) {
            this.code = Math.max(0, code);
            this.receivedMs = receivedMs;
        }
    }

    private static final class SpriteState {
        final ModNetwork.DuelAnimation event;
        final double startMs;
        final int durationMs;
        final ClientAnimBridge.Frame continuedFrom;
        boolean burstDone;

        SpriteState(ModNetwork.DuelAnimation event, double startMs, ClientAnimBridge.Frame continuedFrom) {
            this.event = event;
            this.startMs = startMs;
            this.durationMs = Mth.clamp(event.durationMs(), 80, 10000);
            this.continuedFrom = continuedFrom;
        }

        double endMs() {
            return this.startMs + this.durationMs;
        }
    }

    public record Transform(Vec3 offset, float roll) {
        public static final ClientAnimBridge.Transform NONE = new ClientAnimBridge.Transform(Vec3.ZERO, 0.0F);
    }
}
