package net.mdpromod;

import java.util.HashMap;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientMusicSettingsBridge {
    private static final Map<String, String> SELECTED = new HashMap<>();

    private ClientMusicSettingsBridge() {
    }

    public static String trackFor(String moment) {
        String value = SELECTED.get(moment);
        return value != null ? value : DuelBgmRules.defaultTrack(moment);
    }

    public static void applyAll(String[] moments, String[] tracks) {
        if (moments != null && tracks != null) {
            for (int i = 0; i < moments.length && i < tracks.length; i++) {
                if (DuelBgmRules.isMoment(moments[i])) {
                    SELECTED.put(moments[i], DuelBgmRules.localChoice(tracks[i]));
                }
            }

            ClientDuelAudio.refreshMusic();
        }
    }

    public static void apply(String moment, String track) {
        if (DuelBgmRules.isMoment(moment)) {
            SELECTED.put(moment, DuelBgmRules.localChoice(track));
            ClientDuelAudio.refreshMusic();
        }
    }

    public static String[] snapshot() {
        String[] out = new String[DuelBgmRules.MOMENTS.size()];

        for (int i = 0; i < out.length; i++) {
            out[i] = trackFor(DuelBgmRules.MOMENTS.get(i));
        }

        return out;
    }

    public static String label(String moment) {
        return DuelBgmTracks.label(trackFor(moment));
    }

    public static void open(ModNetwork.MusicSettingsOpen message) {
        applyAll(DuelBgmRules.MOMENTS.toArray(new String[0]), message.selected());
        if (message.open()) {
            Minecraft.getInstance().setScreen(new MusicSettingsScreen(message.selected(), message.keys(), message.labels()));
        }
    }
}
