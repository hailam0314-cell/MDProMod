package net.mdpromod;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.UUID;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.resources.sounds.SimpleSoundInstance;
import net.minecraft.client.resources.sounds.SoundInstance;
import net.minecraft.client.resources.sounds.SoundInstance.Attenuation;
import net.minecraft.client.sounds.SoundManager;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.Mth;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.slf4j.LoggerFactory;

@OnlyIn(Dist.CLIENT)
public final class ClientDuelAudio {
    private static final boolean ENABLED = Boolean.parseBoolean(System.getProperty("mdpromod.duelAudio", "true"));
    private static final int MAX_PENDING = 512;
    private static final UUID LOCAL = new UUID(0L, 0L);
    private static final List<ClientDuelAudio.Pending> PENDING = new ArrayList<>();
    private static final List<ClientDuelAudio.Playing> PLAYING = new ArrayList<>();
    private static final Set<ResourceLocation> MISSING = new HashSet<>();
    private static final LinkedHashMap<UUID, Integer> SESSION_FIELDS = new LinkedHashMap<>();
    private static final LinkedHashMap<UUID, Boolean> CLOSED_SESSIONS = new LinkedHashMap<>();
    private static ClientLevel level;
    private static long previousNano = System.nanoTime();
    private static double clockMs;
    private static ClientDuelAudio.MusicContext MUSIC;
    private static String playingMoment;
    private static String playingTrack;
    private static boolean previewing;
    private static SoundInstance previewInstance;
    private static int bgmStage = 0;
    private static boolean cutinWasPlaying;

    private ClientDuelAudio() {
    }

    public static ResourceLocation location(String key) {
        if (key != null && !key.isBlank()) {
            String normalized = key.toLowerCase(Locale.ROOT);
            return !normalized.matches("[a-z0-9_./-]+")
                ? null
                : ResourceLocation.fromNamespaceAndPath("mdpromod", normalized.startsWith("mdpro.") ? normalized : "mdpro." + normalized);
        } else {
            return null;
        }
    }

    public static void play(ModNetwork.DuelSound message) {
        double now = now();
        if (level != null) {
            if (message.stop()) {
                if (message.key().isBlank() && message.fieldId() >= 0) {
                    clearField(message.fieldId());
                    rememberClosed(message.sessionId());
                } else {
                    stop(message.sessionId(), message.fieldId(), location(message.key()));
                }
            } else if (!CLOSED_SESSIONS.containsKey(message.sessionId())) {
                if (DuelBgmRules.isCue(message.key())) {
                    UUID session = message.sessionId() == null ? LOCAL : message.sessionId();
                    SESSION_FIELDS.put(session, message.fieldId());
                    MUSIC = new ClientDuelAudio.MusicContext(session, message.fieldId());
                    bgmStage = 0;
                    cutinWasPlaying = false;
                    String moment = activeMoment();
                    String track = trackFor(moment);
                    playingMoment = moment;
                    playingTrack = track;
                    startMusic(session, message.fieldId(), message.delayMs(), message.volume(), track);
                } else {
                    Minecraft minecraft = Minecraft.getInstance();
                    boolean farSide = minecraft.player != null
                        && level.getEntity(message.fieldId()) instanceof DuelFieldEntity field
                        && field.sideOfPlayer(minecraft.player).equals("p2");
                    ResourceLocation sound = location(DuelAudioPerspective.key(message.key(), farSide));
                    if (ENABLED && sound != null && valid(message.at()) && Float.isFinite(message.volume()) && Float.isFinite(message.pitch())) {
                        SESSION_FIELDS.put(message.sessionId(), message.fieldId());

                        while (SESSION_FIELDS.size() > 64) {
                            SESSION_FIELDS.remove(SESSION_FIELDS.keySet().iterator().next());
                        }

                        enqueue(
                            new ClientDuelAudio.Pending(
                                message.sessionId() == null ? LOCAL : message.sessionId(),
                                message.fieldId(),
                                sound,
                                message.at(),
                                now + Mth.clamp(DuelAudioPerspective.delay(message.key(), message.delayMs(), farSide), 0, 30000),
                                Mth.clamp(message.durationMs(), 0, 3600000),
                                Mth.clamp(message.volume(), 0.0F, 4.0F),
                                Mth.clamp(message.pitch(), 0.5F, 2.0F),
                                message.loop(),
                                false,
                                false
                            )
                        );
                    }
                }
            }
        }
    }

    public static void play(String key, Vec3 at) {
        play(key, at, 0);
    }

    public static void play(String key, Vec3 at, int delayMs) {
        double now = now();
        ResourceLocation sound = location(key);
        if (ENABLED && level != null && sound != null && valid(at)) {
            enqueue(new ClientDuelAudio.Pending(LOCAL, -1, sound, at, now + Mth.clamp(delayMs, 0, 30000), 0, 1.0F, 1.0F, false, false, false));
        }
    }

    public static void playUi(String key) {
        double now = now();
        ResourceLocation sound = location(key);
        if (ENABLED && level != null && sound != null) {
            enqueue(new ClientDuelAudio.Pending(LOCAL, -1, sound, Vec3.ZERO, now, 0, 0.8F, 1.0F, false, true, false));
        }
    }

    public static void playTimeline(String group, String id, UUID session, int fieldId, Vec3 at) {
        playTimeline(group, id, session, fieldId, at, 0, false);
    }

    public static void playTimeline(String group, String id, UUID session, int fieldId, Vec3 at, int sourceOffsetMs, boolean omitMaterialCard) {
        double now = now();
        if (ENABLED && level != null && valid(at)) {
            for (DuelAudioTimelines.Cue cue : DuelAudioTimelines.cues(group, id)) {
                if (!omitMaterialCard || !cue.key().startsWith("SE_SMN_CMN_CARD_")) {
                    ResourceLocation sound = location(cue.key());
                    if (sound != null) {
                        enqueue(
                            new ClientDuelAudio.Pending(
                                session == null ? LOCAL : session,
                                fieldId,
                                sound,
                                at,
                                now + Math.max(0, cue.timeMs() - sourceOffsetMs),
                                cue.durationMs(),
                                cue.volume(),
                                cue.pitch(),
                                cue.loop(),
                                false,
                                false
                            )
                        );
                    }
                }
            }
        }
    }

    private static void enqueue(ClientDuelAudio.Pending sound) {
        if (!PENDING.stream()
                .anyMatch(p -> p.fieldId() == sound.fieldId() && p.sound().equals(sound.sound()) && Math.abs(p.startMs() - sound.startMs()) < 100.0)
            && !PLAYING.stream()
                .anyMatch(
                    p -> p.fieldId() == sound.fieldId()
                        && p.sound().equals(sound.sound())
                        && sound.startMs() - p.startedMs() >= 0.0
                        && sound.startMs() - p.startedMs() < 100.0
                )) {
            if (PENDING.size() >= 512) {
                PENDING.remove(0);
            }

            if (sound.loop()) {
                stop(sound.session(), sound.fieldId(), sound.sound());
            }

            PENDING.add(sound);
            if (sound.startMs() <= clockMs) {
                tick(Minecraft.getInstance());
            }
        }
    }

    public static void tick(Minecraft minecraft) {
        double now = now();
        if (minecraft.level != null && !minecraft.isPaused()) {
            SoundManager manager = minecraft.getSoundManager();
            Iterator<ClientDuelAudio.Playing> playing = PLAYING.iterator();

            while (playing.hasNext()) {
                ClientDuelAudio.Playing sound = playing.next();
                if (!(now >= sound.stopMs()) && !fieldGone(sound.fieldId())) {
                    if (now - sound.startedMs() > 1000.0 && !manager.isActive(sound.instance())) {
                        playing.remove();
                    }
                } else {
                    manager.stop(sound.instance());
                    playing.remove();
                }
            }

            Iterator<ClientDuelAudio.Pending> pending = PENDING.iterator();

            while (pending.hasNext()) {
                ClientDuelAudio.Pending sound = pending.next();
                if (fieldGone(sound.fieldId())) {
                    pending.remove();
                } else if (!(sound.startMs() > now)) {
                    pending.remove();
                    ResourceLocation resolved = sound.sound();
                    if (manager.getSoundEvent(resolved) == null && resolved.getPath().endsWith("_oneshot")) {
                        resolved = ResourceLocation.fromNamespaceAndPath(
                            resolved.getNamespace(), resolved.getPath().substring(0, resolved.getPath().length() - 8)
                        );
                    }

                    if (manager.getSoundEvent(resolved) == null) {
                        if (MISSING.add(resolved)) {
                            LoggerFactory.getLogger("mdpromod").warn("Missing bundled duel sound: {}", resolved);
                        }
                    } else {
                        SoundInstance instance = new SimpleSoundInstance(
                            resolved,
                            sound.music() ? SoundSource.MUSIC : SoundSource.PLAYERS,
                            sound.volume(),
                            sound.pitch(),
                            SoundInstance.createUnseededRandom(),
                            sound.loop(),
                            0,
                            sound.relative() ? Attenuation.NONE : Attenuation.LINEAR,
                            sound.at().x,
                            sound.at().y,
                            sound.at().z,
                            sound.relative()
                        );
                        manager.play(instance);
                        double stopAt = sound.durationMs() > 0 ? now + sound.durationMs() : Double.POSITIVE_INFINITY;
                        PLAYING.add(new ClientDuelAudio.Playing(sound.session(), sound.fieldId(), sound.sound(), instance, stopAt, now, sound.music()));
                    }
                }
            }

            advanceStage();
            refreshMusic();
            if (ownsMusic()) {
                minecraft.getMusicManager().stopPlaying();
            }
        }
    }

    public static void clearSession(UUID session) {
        Integer fieldId = SESSION_FIELDS.remove(session);
        if (fieldId != null) {
            clearField(fieldId);
        }

        stop(session, -1, null);
        rememberClosed(session);
    }

    private static void rememberClosed(UUID session) {
        if (session != null) {
            CLOSED_SESSIONS.put(session, true);

            while (CLOSED_SESSIONS.size() > 64) {
                CLOSED_SESSIONS.remove(CLOSED_SESSIONS.keySet().iterator().next());
            }
        }
    }

    private static void clearField(int fieldId) {
        stop(null, fieldId, null);
        SESSION_FIELDS.values().removeIf(value -> value == fieldId);
        if (MUSIC != null && MUSIC.field() == fieldId) {
            MUSIC = null;
            playingMoment = null;
            playingTrack = null;
            bgmStage = 0;
            cutinWasPlaying = false;
        }

        ClientCutinEffects.stopField(fieldId);
        ClientSummonEffects.stopField(fieldId);
    }

    private static boolean fieldGone(int fieldId) {
        return fieldId >= 0 && !(level.getEntity(fieldId) instanceof DuelFieldEntity);
    }

    public static void stop(UUID session, int fieldId, ResourceLocation sound) {
        PENDING.removeIf(p -> matches(session, fieldId, sound, p.session(), p.fieldId(), p.sound()));
        SoundManager manager = Minecraft.getInstance().getSoundManager();
        PLAYING.removeIf(p -> {
            if (!matches(session, fieldId, sound, p.session(), p.fieldId(), p.sound())) {
                return false;
            }

            manager.stop(p.instance());
            return true;
        });
    }

    private static void stopMusic(UUID session, int fieldId) {
        SoundManager manager = Minecraft.getInstance().getSoundManager();
        PLAYING.removeIf(p -> {
            if (p.music() && matches(session, fieldId, null, p.session(), p.fieldId(), p.sound())) {
                manager.stop(p.instance());
                return true;
            } else {
                return false;
            }
        });
        PENDING.removeIf(p -> p.music() && matches(session, fieldId, null, p.session(), p.fieldId(), p.sound()));
    }

    private static String activeMoment() {
        if (bgmStage == 2 && hasTrack("climax")) {
            return "climax";
        } else {
            return bgmStage >= 1 && hasTrack("keycard") ? "keycard" : "duel";
        }
    }

    private static boolean hasTrack(String moment) {
        String track = ClientMusicSettingsBridge.trackFor(moment);
        return track != null && !track.isEmpty();
    }

    private static void advanceStage() {
        boolean cutinNow = ClientCutinEffects.isPlaying();
        boolean cutinStarted = cutinNow && !cutinWasPlaying;
        cutinWasPlaying = cutinNow;
        int next = DuelBgmRules.advanceBgmStage(bgmStage, cutinStarted, lowLifePoints());
        if (next != bgmStage) {
            bgmStage = next;
            playingMoment = null;
            playingTrack = null;
        }
    }

    private static boolean lowLifePoints() {
        ClientDuelAudio.MusicContext context = MUSIC;
        Minecraft minecraft = Minecraft.getInstance();
        return context != null
            && minecraft.player != null
            && level != null
            && level.getEntity(context.field()) instanceof DuelFieldEntity field
            && field.lifePoints(field.sideOfPlayer(minecraft.player)) <= 2000;
    }

    private static boolean ownsMusic() {
        return previewing || playingTrack != null && !playingTrack.isEmpty();
    }

    private static String trackFor(String moment) {
        if ("duel".equals(moment)) {
            String override = System.getProperty("mdpromod.duelBgm");
            if (override != null && !override.isBlank()) {
                return DuelBgmRules.localChoice(override);
            }
        }

        return ClientMusicSettingsBridge.trackFor(moment);
    }

    public static void refreshMusic() {
        ClientDuelAudio.MusicContext context = MUSIC;
        if (context != null && !previewing) {
            String moment = activeMoment();
            String track = trackFor(moment);
            if (!moment.equals(playingMoment) || !track.equals(playingTrack)) {
                playingMoment = moment;
                playingTrack = track;
                LoggerFactory.getLogger("mdpromod").info("BGM 切换：[{}] {}", DuelBgmRules.momentLabel(moment), track.isEmpty() ? "(不播放)" : track);
                startMusic(context.session(), context.field(), 0, 1.0F, track);
            }
        }
    }

    private static void startMusic(UUID session, int fieldId, int delayMs, float volume, String track) {
        stopMusic(session, fieldId);
        if (track != null && !track.isEmpty()) {
            ResourceLocation bgm = location(track);
            if (bgm != null) {
                ClientDuelAudio.Pending pending = new ClientDuelAudio.Pending(
                    session,
                    fieldId,
                    bgm,
                    Vec3.ZERO,
                    now() + Mth.clamp(delayMs, 0, 30000),
                    0,
                    Mth.clamp(volume <= 0.0F ? 1.0F : volume, 0.0F, 1.0F),
                    1.0F,
                    true,
                    true,
                    true
                );
                PENDING.removeIf(p -> p.music() && p.fieldId() == fieldId);
                if (PENDING.size() >= 512) {
                    PENDING.remove(0);
                }

                PENDING.add(pending);
                if (pending.startMs() <= clockMs) {
                    tick(Minecraft.getInstance());
                }
            }
        }
    }

    public static void preview(String track) {
        stopPreview();
        String key = DuelBgmRules.localChoice(track);
        if (!key.isEmpty() && level != null) {
            ResourceLocation sound = location(key);
            if (sound != null) {
                SoundManager manager = Minecraft.getInstance().getSoundManager();
                if (manager.getSoundEvent(sound) != null) {
                    previewing = true;
                    stopMusic(null, -1);
                    previewInstance = new SimpleSoundInstance(
                        sound, SoundSource.MUSIC, 1.0F, 1.0F, SoundInstance.createUnseededRandom(), true, 0, Attenuation.NONE, 0.0, 0.0, 0.0, true
                    );
                    manager.play(previewInstance);
                }
            }
        }
    }

    public static boolean isPreviewing() {
        return previewing;
    }

    public static void stopPreview() {
        if (previewing) {
            previewing = false;
            if (previewInstance != null) {
                Minecraft.getInstance().getSoundManager().stop(previewInstance);
                previewInstance = null;
            }

            playingMoment = null;
            playingTrack = null;
            refreshMusic();
        }
    }

    private static boolean matches(UUID wantedSession, int wantedField, ResourceLocation wantedSound, UUID session, int field, ResourceLocation sound) {
        return (wantedSession == null || wantedSession.equals(session))
            && (wantedField < 0 || wantedField == field)
            && (wantedSound == null || wantedSound.equals(sound));
    }

    public static void clear() {
        SoundManager manager = Minecraft.getInstance().getSoundManager();
        if (previewInstance != null) {
            manager.stop(previewInstance);
            previewInstance = null;
        }

        previewing = false;
        PLAYING.forEach(p -> manager.stop(p.instance()));
        PENDING.clear();
        PLAYING.clear();
        MISSING.clear();
        SESSION_FIELDS.clear();
        CLOSED_SESSIONS.clear();
        MUSIC = null;
        playingMoment = null;
        playingTrack = null;
        bgmStage = 0;
        cutinWasPlaying = false;
        clockMs = 0.0;
        previousNano = System.nanoTime();
    }

    private static double now() {
        Minecraft minecraft = Minecraft.getInstance();
        long nano = System.nanoTime();
        if (minecraft.level != level) {
            clear();
            level = minecraft.level;
            previousNano = nano;
        }

        if (!minecraft.isPaused()) {
            clockMs = clockMs + Math.max(0.0, (nano - previousNano) / 1000000.0);
        }

        previousNano = nano;
        return clockMs;
    }

    private static boolean valid(Vec3 at) {
        return at != null
            && Double.isFinite(at.x)
            && Double.isFinite(at.y)
            && Double.isFinite(at.z)
            && Math.abs(at.x) <= 3.0000064E7
            && Math.abs(at.z) <= 3.0000064E7
            && Math.abs(at.y) <= 100000.0;
    }

    private record MusicContext(UUID session, int field) {
    }

    private record Pending(
        UUID session,
        int fieldId,
        ResourceLocation sound,
        Vec3 at,
        double startMs,
        int durationMs,
        float volume,
        float pitch,
        boolean loop,
        boolean relative,
        boolean music
    ) {
    }

    private record Playing(UUID session, int fieldId, ResourceLocation sound, SoundInstance instance, double stopMs, double startedMs, boolean music) {
    }
}
