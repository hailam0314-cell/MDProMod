package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.resources.sounds.SimpleSoundInstance;
import net.minecraft.client.resources.sounds.SoundInstance;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.Mth;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.UUID;

/** Client presentation audio. All clips are Vorbis resources inside the mod. */
@OnlyIn(Dist.CLIENT)
public final class ClientDuelAudio {
    private static final boolean ENABLED =
            Boolean.parseBoolean(System.getProperty("mdpromod.duelAudio", "true"));
    private static final int MAX_PENDING = 512;
    private static final UUID LOCAL = new UUID(0L, 0L);
    private static final List<Pending> PENDING = new ArrayList<>();
    private static final List<Playing> PLAYING = new ArrayList<>();
    private static final Set<ResourceLocation> MISSING = new HashSet<>();
    private static final LinkedHashMap<UUID, Integer> SESSION_FIELDS = new LinkedHashMap<>();
    private static final LinkedHashMap<UUID, Boolean> CLOSED_SESSIONS = new LinkedHashMap<>();
    private static ClientLevel level;
    private static long previousNano = System.nanoTime();
    private static double clockMs;

    private record Pending(UUID session, int fieldId, ResourceLocation sound, Vec3 at,
                           double startMs, int durationMs, float volume, float pitch,
                           boolean loop, boolean relative, boolean music) {}
    private record Playing(UUID session, int fieldId, ResourceLocation sound,
                           SoundInstance instance, double stopMs, double startedMs, boolean music) {}

    /** 当前 BGM 属于哪一场决斗；玩家换曲子时按它重启。 */
    private record MusicContext(UUID session, int field) {}

    private static MusicContext MUSIC;
    /** 当前正放着哪个时机的哪首；没变就不重播。 */
    private static String playingMoment;
    private static String playingTrack;
    /** 试听：临时接管音乐，停止后自动恢复当前时机的那首。 */
    private static boolean previewing;
    private static SoundInstance previewInstance;
    /**
     * 和 MD 原版一致的**只前进不后退**阶段：0 常规 / 1 王牌 / 2 高潮。
     * 见 {@link DuelBgmRules#advanceBgmStage}。王牌只切一次、高潮把王牌顶掉且不再回头，
     * 所以多只王牌连续登场也不会把音乐反复重播。
     */
    private static int bgmStage = DuelBgmRules.STAGE_NORMAL;
    private static boolean cutinWasPlaying;

    private ClientDuelAudio() {}

    /** MD keys retain their original spelling at call sites; resource IDs use lower case. */
    public static ResourceLocation location(String key) {
        if (key == null || key.isBlank()) return null;
        String normalized = key.toLowerCase(Locale.ROOT);
        if (!normalized.matches("[a-z0-9_./-]+")) return null;
        return ResourceLocation.fromNamespaceAndPath(MDProMod.MODID,
                normalized.startsWith("mdpro.") ? normalized : "mdpro." + normalized);
    }

    public static void play(ModNetwork.DuelSound message) {
        double now = now();
        if (level == null) return;
        if (message.stop()) {
            if (message.key().isBlank() && message.fieldId() >= 0) {
                clearField(message.fieldId());
                rememberClosed(message.sessionId());
            } else stop(message.sessionId(), message.fieldId(), location(message.key()));
            return;
        }
        if (CLOSED_SESSIONS.containsKey(message.sessionId())) return;
        if (DuelBgmRules.isCue(message.key())) {
            UUID session = message.sessionId() == null ? LOCAL : message.sessionId();
            SESSION_FIELDS.put(session, message.fieldId());
            MUSIC = new MusicContext(session, message.fieldId());
            // 开局 = 原版 PlayBgmNormal()：阶段复位成「常规」，一场决斗只复位这一次
            bgmStage = DuelBgmRules.STAGE_NORMAL;
            cutinWasPlaying = false;
            String moment = activeMoment();
            String track = trackFor(moment);
            playingMoment = moment;
            playingTrack = track;
            startMusic(session, message.fieldId(), message.delayMs(), message.volume(), track);
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        boolean farSide = minecraft.player != null
                && level.getEntity(message.fieldId()) instanceof DuelFieldEntity field
                && field.playerSide(minecraft.player).equals("p2");
        ResourceLocation sound = location(DuelAudioPerspective.key(message.key(), farSide));
        if (!ENABLED || sound == null || !valid(message.at())
                || !Float.isFinite(message.volume()) || !Float.isFinite(message.pitch())) return;
        SESSION_FIELDS.put(message.sessionId(), message.fieldId());
        while (SESSION_FIELDS.size() > 64) SESSION_FIELDS.remove(SESSION_FIELDS.keySet().iterator().next());
        enqueue(new Pending(message.sessionId() == null ? LOCAL : message.sessionId(),
                message.fieldId(), sound, message.at(), now + Mth.clamp(
                        DuelAudioPerspective.delay(message.key(), message.delayMs(), farSide), 0, 30000),
                Mth.clamp(message.durationMs(), 0, 3600000),
                Mth.clamp(message.volume(), 0.0F, 4.0F), Mth.clamp(message.pitch(), 0.5F, 2.0F),
                message.loop(), false, false));
    }

    public static void play(String key, Vec3 at) { play(key, at, 0); }

    public static void play(String key, Vec3 at, int delayMs) {
        double now = now();
        ResourceLocation sound = location(key);
        if (!ENABLED || level == null || sound == null || !valid(at)) return;
        enqueue(new Pending(LOCAL, -1, sound, at, now + Mth.clamp(delayMs, 0, 30000),
                0, 1.0F, 1.0F, false, false, false));
    }

    /** Interface feedback is private to the player choosing a card or confirming a prompt. */
    public static void playUi(String key) {
        double now = now();
        ResourceLocation sound = location(key);
        if (!ENABLED || level == null || sound == null) return;
        enqueue(new Pending(LOCAL, -1, sound, Vec3.ZERO, now, 0, 0.8F, 1.0F, false, true, false));
    }

    public static void playTimeline(String group, String id, UUID session, int fieldId, Vec3 at) {
        playTimeline(group, id, session, fieldId, at, 0, false);
    }

    /** Rebase MD's combined material/summon timeline onto the mod's separate phases. */
    public static void playTimeline(String group, String id, UUID session, int fieldId, Vec3 at,
                                    int sourceOffsetMs, boolean omitMaterialCard) {
        double now = now();
        if (!ENABLED || level == null || !valid(at)) return;
        for (DuelAudioTimelines.Cue cue : DuelAudioTimelines.cues(group, id)) {
            if (omitMaterialCard && cue.key().startsWith("SE_SMN_CMN_CARD_")) continue;
            ResourceLocation sound = location(cue.key());
            if (sound == null) continue;
            enqueue(new Pending(session == null ? LOCAL : session, fieldId, sound, at,
                    now + Math.max(0, cue.timeMs() - sourceOffsetMs), cue.durationMs(), cue.volume(), cue.pitch(), cue.loop(), false, false));
        }
    }

    private static void enqueue(Pending sound) {
        // Installed AudioManager suppresses the same SE within 100 ms. Scope it
        // to the field so neighboring duels cannot suppress one another.
        if (PENDING.stream().anyMatch(p -> p.fieldId() == sound.fieldId()
                && p.sound().equals(sound.sound()) && Math.abs(p.startMs() - sound.startMs()) < 100)
                || PLAYING.stream().anyMatch(p -> p.fieldId() == sound.fieldId()
                && p.sound().equals(sound.sound()) && sound.startMs() - p.startedMs() >= 0
                && sound.startMs() - p.startedMs() < 100)) return;
        if (PENDING.size() >= MAX_PENDING) PENDING.remove(0);
        if (sound.loop()) stop(sound.session(), sound.fieldId(), sound.sound());
        PENDING.add(sound);
        if (sound.startMs() <= clockMs) tick(Minecraft.getInstance());
    }

    public static void tick(Minecraft minecraft) {
        double now = now();
        if (minecraft.level == null || minecraft.isPaused()) return;
        var manager = minecraft.getSoundManager();
        Iterator<Playing> playing = PLAYING.iterator();
        while (playing.hasNext()) {
            Playing sound = playing.next();
            if (now >= sound.stopMs() || fieldGone(sound.fieldId())) {
                manager.stop(sound.instance());
                playing.remove();
            } else if (now - sound.startedMs() > 1000.0 && !manager.isActive(sound.instance())) {
                playing.remove();
            }
        }
        Iterator<Pending> pending = PENDING.iterator();
        while (pending.hasNext()) {
            Pending sound = pending.next();
            if (fieldGone(sound.fieldId())) { pending.remove(); continue; }
            if (sound.startMs() > now) continue;
            pending.remove();
            ResourceLocation resolved = sound.sound();
            if (manager.getSoundEvent(resolved) == null && resolved.getPath().endsWith("_oneshot"))
                resolved = ResourceLocation.fromNamespaceAndPath(resolved.getNamespace(),
                        resolved.getPath().substring(0, resolved.getPath().length() - 8));
            if (manager.getSoundEvent(resolved) == null) {
                if (MISSING.add(resolved)) LoggerFactory.getLogger("mdpromod")
                        .warn("Missing bundled duel sound: {}", resolved);
                continue;
            }
            SoundInstance instance = new SimpleSoundInstance(resolved,
                    sound.music() ? SoundSource.MUSIC : SoundSource.PLAYERS,
                    sound.volume(), sound.pitch(), SoundInstance.createUnseededRandom(), sound.loop(), 0,
                    sound.relative() ? SoundInstance.Attenuation.NONE : SoundInstance.Attenuation.LINEAR,
                    sound.at().x, sound.at().y, sound.at().z, sound.relative());
            manager.play(instance);
            double stopAt = sound.durationMs() > 0 ? now + sound.durationMs() : Double.POSITIVE_INFINITY;
            PLAYING.add(new Playing(sound.session(), sound.fieldId(), sound.sound(), instance, stopAt, now, sound.music()));
        }
        // 时机（王牌登场 / 低 LP 高潮）或曲目变了就换一首；没变什么都不做
        advanceStage();
        refreshMusic();
        // 我们的 BGM 在放就别让原版音乐同时响（同一个 SoundSource.MUSIC）
        if (ownsMusic()) minecraft.getMusicManager().stopPlaying();
    }

    public static void clearSession(UUID session) {
        Integer fieldId = SESSION_FIELDS.remove(session);
        if (fieldId != null) clearField(fieldId);
        stop(session, -1, null);
        rememberClosed(session);
    }

    private static void rememberClosed(UUID session) {
        if (session == null) return;
        CLOSED_SESSIONS.put(session, true);
        while (CLOSED_SESSIONS.size() > 64) CLOSED_SESSIONS.remove(CLOSED_SESSIONS.keySet().iterator().next());
    }

    private static void clearField(int fieldId) {
        stop(null, fieldId, null);
        SESSION_FIELDS.values().removeIf(value -> value == fieldId);
        if (MUSIC != null && MUSIC.field() == fieldId) {
            MUSIC = null;
            playingMoment = null;
            playingTrack = null;
            bgmStage = DuelBgmRules.STAGE_NORMAL;
            cutinWasPlaying = false;
        }
        ClientCutinEffects.stopField(fieldId);
        ClientSummonEffects.stopField(fieldId);
    }

    private static boolean fieldGone(int fieldId) {
        return fieldId >= 0 && !(level.getEntity(fieldId) instanceof DuelFieldEntity);
    }

    /** A null sound stops the whole matching session/field, including queued clips. */
    public static void stop(UUID session, int fieldId, ResourceLocation sound) {
        PENDING.removeIf(p -> matches(session, fieldId, sound, p.session(), p.fieldId(), p.sound()));
        var manager = Minecraft.getInstance().getSoundManager();
        PLAYING.removeIf(p -> {
            if (!matches(session, fieldId, sound, p.session(), p.fieldId(), p.sound())) return false;
            manager.stop(p.instance());
            return true;
        });
    }

    private static void stopMusic(UUID session, int fieldId) {
        var manager = Minecraft.getInstance().getSoundManager();
        PLAYING.removeIf(p -> {
            if (!p.music() || !matches(session, fieldId, null, p.session(), p.fieldId(), p.sound())) return false;
            manager.stop(p.instance());
            return true;
        });
        PENDING.removeIf(p -> p.music() && matches(session, fieldId, null,
                p.session(), p.fieldId(), p.sound()));
    }

    /** 该放哪个时机：王牌登场（cut-in 期间）> 低 LP 高潮 > 决斗常驻。 */
    private static String activeMoment() {
        // 高潮一旦触发就压住王牌（和原版一样）；某一栏选「不播放」= 该时机不打断常驻 BGM
        if (bgmStage == DuelBgmRules.STAGE_CLIMAX && hasTrack(DuelBgmRules.MOMENT_CLIMAX)) {
            return DuelBgmRules.MOMENT_CLIMAX;
        }
        if (bgmStage >= DuelBgmRules.STAGE_KEYCARD && hasTrack(DuelBgmRules.MOMENT_KEYCARD)) {
            return DuelBgmRules.MOMENT_KEYCARD;
        }
        return DuelBgmRules.MOMENT_DUEL;
    }

    private static boolean hasTrack(String moment) {
        String track = ClientMusicSettingsBridge.trackFor(moment);
        return track != null && !track.isEmpty();
    }

    /**
     * 每 tick 推进阶段（照 MD 的 {@code PlayBgmKeyCard}/{@code PlayBgmClimax} 守卫）。
     * 阶段真的变了才把 {@code playingMoment/Track} 清空，让 {@link #refreshMusic()} 换一次曲；
     * **第二只王牌登场阶段已经是 1，什么都不会发生**——这就是"不再反复重播"的关键。
     */
    private static void advanceStage() {
        boolean cutinNow = ClientCutinEffects.isPlaying();
        boolean cutinStarted = cutinNow && !cutinWasPlaying;
        cutinWasPlaying = cutinNow;

        int next = DuelBgmRules.advanceBgmStage(bgmStage, cutinStarted, lowLifePoints());
        if (next != bgmStage) {
            bgmStage = next;
            // 阶段变了：让 refreshMusic 认为"变了"，于是换一次曲
            playingMoment = null;
            playingTrack = null;
        }
    }

    /** 本地玩家（不是对手）的 LP。 */
    private static boolean lowLifePoints() {
        MusicContext context = MUSIC;
        Minecraft minecraft = Minecraft.getInstance();
        return context != null && minecraft.player != null && level != null
                && level.getEntity(context.field()) instanceof DuelFieldEntity field
                && field.lifePoints(field.playerSide(minecraft.player)) <= DuelBgmRules.CLIMAX_LP;
    }

    /** 我们自己的 BGM 或试听正在放——此时要压住原版音乐，别两首一起响。 */
    private static boolean ownsMusic() {
        return previewing || (playingTrack != null && !playingTrack.isEmpty());
    }

    /** 该时机要放哪首；系统属性只覆盖「决斗」那一栏，方便调试。 */
    private static String trackFor(String moment) {
        if (DuelBgmRules.MOMENT_DUEL.equals(moment)) {
            String override = System.getProperty("mdpromod.duelBgm");
            if (override != null && !override.isBlank()) return DuelBgmRules.localChoice(override);
        }
        return ClientMusicSettingsBridge.trackFor(moment);
    }

    /**
     * 每 tick 都会问一次：时机或曲目变了才换，没变什么都不做。
     * 这样王牌登场（cut-in 开始/结束）和低 LP 高潮都是自动的。
     */
    public static void refreshMusic() {
        MusicContext context = MUSIC;
        if (context == null || previewing) return;
        String moment = activeMoment();
        String track = trackFor(moment);
        if (moment.equals(playingMoment) && track.equals(playingTrack)) return;
        playingMoment = moment;
        playingTrack = track;
        LoggerFactory.getLogger("mdpromod").info("BGM 切换：[{}] {}",
                DuelBgmRules.momentLabel(moment), track.isEmpty() ? "(不播放)" : track);
        startMusic(context.session(), context.field(), 0, 1.0F, track);
    }

    /** 换/开/关 BGM 都走这里：先停掉这一场原来的那首，再排新的。 */
    private static void startMusic(UUID session, int fieldId, int delayMs, float volume, String track) {
        stopMusic(session, fieldId);
        if (track == null || track.isEmpty()) return;      // 「不播放」
        ResourceLocation bgm = location(track);
        if (bgm == null) return;
        Pending pending = new Pending(session, fieldId, bgm, Vec3.ZERO,
                now() + Mth.clamp(delayMs, 0, 30000), 0,
                Mth.clamp(volume <= 0.0F ? 1.0F : volume, 0.0F, 1.0F), 1.0F, true, true, true);
        // 换曲子是玩家的主动操作，不该被 100 ms 去重挡掉
        PENDING.removeIf(p -> p.music() && p.fieldId() == fieldId);
        if (PENDING.size() >= MAX_PENDING) PENDING.remove(0);
        PENDING.add(pending);
        if (pending.startMs() <= clockMs) tick(Minecraft.getInstance());
    }

    /** 试听：临时接管音乐，停止后自动回到当前时机的曲子。 */
    public static void preview(String track) {
        stopPreview();
        String key = DuelBgmRules.localChoice(track);
        if (key.isEmpty() || level == null) return;
        ResourceLocation sound = location(key);
        if (sound == null) return;
        var manager = Minecraft.getInstance().getSoundManager();
        if (manager.getSoundEvent(sound) == null) return;
        previewing = true;
        // 先把对决 BGM 让出来，免得两首叠在一起
        stopMusic(null, -1);
        previewInstance = new SimpleSoundInstance(sound, SoundSource.MUSIC, 1.0F, 1.0F,
                SoundInstance.createUnseededRandom(), true, 0,
                SoundInstance.Attenuation.NONE, 0.0, 0.0, 0.0, true);
        manager.play(previewInstance);
    }

    public static boolean isPreviewing() {
        return previewing;
    }

    /** 关闭界面、按任意键、或开下一首试听都走这里。 */
    public static void stopPreview() {
        if (!previewing) return;
        previewing = false;
        if (previewInstance != null) {
            Minecraft.getInstance().getSoundManager().stop(previewInstance);
            previewInstance = null;
        }
        playingMoment = null;
        playingTrack = null;
        refreshMusic();
    }

    private static boolean matches(UUID wantedSession, int wantedField, ResourceLocation wantedSound,
                                   UUID session, int field, ResourceLocation sound) {
        return (wantedSession == null || wantedSession.equals(session))
                && (wantedField < 0 || wantedField == field)
                && (wantedSound == null || wantedSound.equals(sound));
    }

    public static void clear() {
        var manager = Minecraft.getInstance().getSoundManager();
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
        bgmStage = DuelBgmRules.STAGE_NORMAL;
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
        if (!minecraft.isPaused()) clockMs += Math.max(0.0, (nano - previousNano) / 1000000.0);
        previousNano = nano;
        return clockMs;
    }

    private static boolean valid(Vec3 at) {
        return at != null && Double.isFinite(at.x) && Double.isFinite(at.y) && Double.isFinite(at.z)
                && Math.abs(at.x) <= 30000064.0 && Math.abs(at.z) <= 30000064.0
                && Math.abs(at.y) <= 100000.0;
    }
}
