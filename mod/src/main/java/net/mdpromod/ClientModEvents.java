package net.mdpromod;

import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.EntityRenderersEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

@Mod.EventBusSubscriber(modid = MDProMod.MODID, value = Dist.CLIENT,
        bus = Mod.EventBusSubscriber.Bus.MOD)
public final class ClientModEvents {
    private ClientModEvents() {
    }

    @SubscribeEvent
    public static void registerRenderers(EntityRenderersEvent.RegisterRenderers event) {
        event.registerEntityRenderer(ModEntities.DUEL_FIELD.get(), DuelFieldRenderer::new);
        event.registerEntityRenderer(ModEntities.CARD_CASE.get(), CardCaseRenderer::new);
        event.registerEntityRenderer(ModEntities.DUEL_CARD.get(), DuelCardRenderer::new);
        event.registerEntityRenderer(ModEntities.HAND_CARD.get(), HandCardRenderer::new);
    }

}
