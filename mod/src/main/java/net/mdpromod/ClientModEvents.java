package net.mdpromod;

import net.minecraft.world.entity.EntityType;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.EntityRenderersEvent.RegisterRenderers;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber;
import net.minecraftforge.fml.common.Mod.EventBusSubscriber.Bus;

@EventBusSubscriber(modid = "mdpromod", value = Dist.CLIENT, bus = Bus.MOD)
public final class ClientModEvents {
    private ClientModEvents() {
    }

    @SubscribeEvent
    public static void registerRenderers(RegisterRenderers event) {
        event.registerEntityRenderer((EntityType)ModEntities.DUEL_FIELD.get(), DuelFieldRenderer::new);
        event.registerEntityRenderer((EntityType)ModEntities.CARD_CASE.get(), CardCaseRenderer::new);
        event.registerEntityRenderer((EntityType)ModEntities.DUEL_CARD.get(), DuelCardRenderer::new);
        event.registerEntityRenderer((EntityType)ModEntities.HAND_CARD.get(), HandCardRenderer::new);
    }
}
