package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.util.HashMap;
import java.util.Map;

/** 客户端镜像：每个「时机」各选一首曲子，供 {@link ClientDuelAudio} 取用。 */
@OnlyIn(Dist.CLIENT)
public final class ClientMusicSettingsBridge {

    private static final Map<String, String> SELECTED = new HashMap<>();

    private ClientMusicSettingsBridge() {
    }

    /** 某个时机当前该放哪首；没设过就是该时机的默认曲目。 */
    public static String trackFor(String moment) {
        String value = SELECTED.get(moment);
        return value != null ? value : DuelBgmRules.defaultTrack(moment);
    }

    /** 整批应用（上线同步或打开界面时）。 */
    public static void applyAll(String[] moments, String[] tracks) {
        if (moments == null || tracks == null) return;
        for (int i = 0; i < moments.length && i < tracks.length; i++) {
            if (DuelBgmRules.isMoment(moments[i])) {
                SELECTED.put(moments[i], DuelBgmRules.localChoice(tracks[i]));
            }
        }
        ClientDuelAudio.refreshMusic();
    }

    /** 设置单个时机；对局中会立刻生效。 */
    public static void apply(String moment, String track) {
        if (!DuelBgmRules.isMoment(moment)) return;
        SELECTED.put(moment, DuelBgmRules.localChoice(track));
        ClientDuelAudio.refreshMusic();
    }

    /** 按 {@link DuelBgmRules#MOMENTS} 的顺序导出当前选择。 */
    public static String[] snapshot() {
        String[] out = new String[DuelBgmRules.MOMENTS.size()];
        for (int i = 0; i < out.length; i++) {
            out[i] = trackFor(DuelBgmRules.MOMENTS.get(i));
        }
        return out;
    }

    /** 供界面显示：该时机当前曲目的可读名字。 */
    public static String label(String moment) {
        return DuelBgmTracks.label(trackFor(moment));
    }

    public static void open(ModNetwork.MusicSettingsOpen message) {
        // 时机列表两端同源（DuelBgmRules.MOMENTS），包里只带"每个时机选了什么"
        applyAll(DuelBgmRules.MOMENTS.toArray(new String[0]), message.selected());
        if (!message.open()) return;
        Minecraft.getInstance().setScreen(
                new MusicSettingsScreen(message.selected(), message.keys(), message.labels()));
    }
}
