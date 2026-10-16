package net.mdpromod;

import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraftforge.event.server.ServerStartedEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

import java.util.ArrayList;
import java.util.List;

/**
 * 开局自检：四个合成配方到底进没进 {@link RecipeManager}。
 *
 * <p>配方 JSON 有问题时原版多数情况下是**静默少一条**（只在解析出错时才打日志），
 * 所以这里在服务器起来时把结果直接打出来：正常一行 INFO，缺了就是 WARN。
 */
@Mod.EventBusSubscriber(modid = MDProMod.MODID)
public final class RecipeSelfCheck {
    /** 必须存在的四个合成配方（和 data/mdpromod/recipes/ 同步）。 */
    private static final List<String> EXPECTED =
            List.of("card_case", "duel_field", "ai_deck", "music_settings");

    private RecipeSelfCheck() {
    }

    @SubscribeEvent
    public static void onServerStarted(ServerStartedEvent event) {
        RecipeManager manager = event.getServer().getRecipeManager();
        List<String> present = new ArrayList<>();
        List<String> missing = new ArrayList<>();
        for (String name : EXPECTED) {
            ResourceLocation id = ResourceLocation.fromNamespaceAndPath(MDProMod.MODID, name);
            if (manager.byKey(id).isPresent()) present.add(name);
            else missing.add(name);
        }
        long ours = manager.getRecipes().stream()
                .filter(recipe -> recipe.getId().getNamespace().equals(MDProMod.MODID))
                .count();
        if (missing.isEmpty()) {
            ModLog.info("合成配方自检：{} 个都在，本模组共 {} 条配方（全库 {} 条）",
                    present.size(), ours, manager.getRecipes().size());
        } else {
            ModLog.warn("合成配方自检：缺 {}（本模组共 {} 条配方，全库 {} 条）",
                    missing, ours, manager.getRecipes().size());
        }
    }
}
