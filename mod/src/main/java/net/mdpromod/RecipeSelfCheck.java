package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.crafting.RecipeManager;
import net.minecraftforge.event.server.ServerStartedEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;

@EventBusSubscriber(modid = "mdpromod")
public final class RecipeSelfCheck {
    private static final List<String> EXPECTED = List.of("card_case", "duel_field", "ai_deck", "music_settings");

    private RecipeSelfCheck() {
    }

    @SubscribeEvent
    public static void onServerStarted(ServerStartedEvent event) {
        RecipeManager manager = event.getServer().getRecipeManager();
        List<String> present = new ArrayList<>();
        List<String> missing = new ArrayList<>();

        for (String name : EXPECTED) {
            ResourceLocation id = ResourceLocation.fromNamespaceAndPath("mdpromod", name);
            if (manager.byKey(id).isPresent()) {
                present.add(name);
            } else {
                missing.add(name);
            }
        }

        long ours = manager.getRecipes().stream().filter(recipe -> recipe.getId().getNamespace().equals("mdpromod")).count();
        if (missing.isEmpty()) {
            ModLog.info("合成配方自检：{} 个都在，本模组共 {} 条配方（全库 {} 条）", present.size(), ours, manager.getRecipes().size());
        } else {
            ModLog.warn("合成配方自检：缺 {}（本模组共 {} 条配方，全库 {} 条）", missing, ours, manager.getRecipes().size());
        }
    }
}
