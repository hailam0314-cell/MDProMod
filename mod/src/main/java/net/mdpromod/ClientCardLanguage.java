package net.mdpromod;

import java.util.Locale;
import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent.ClientTickEvent;
import net.minecraftforge.event.TickEvent.Phase;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT)
public final class ClientCardLanguage {
    private static boolean initialized;
    private static boolean applied;

    private ClientCardLanguage() {
    }

    @SubscribeEvent
    public static void onClientTick(ClientTickEvent event) {
        if (event.phase == Phase.END) {
            Minecraft minecraft = Minecraft.getInstance();
            if (minecraft.options != null) {
                boolean english = wantEnglish(minecraft.options.languageCode);
                if (!initialized || english != applied) {
                    initialized = true;
                    applied = english;
                    CardRegistry.setEnglish(english);
                }
            }
        }
    }

    private static boolean wantEnglish(String languageCode) {
        String forced = System.getProperty("mdpromod.cardLanguage", "").trim();
        if (forced.equalsIgnoreCase("en") || forced.equalsIgnoreCase("en_us")) {
            return true;
        } else {
            return !forced.equalsIgnoreCase("zh") && !forced.equalsIgnoreCase("zh_cn")
                ? languageCode != null && languageCode.toLowerCase(Locale.ROOT).startsWith("en")
                : false;
        }
    }
}
