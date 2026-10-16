package net.mdpromod;

import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.MobCategory;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

public final class ModEntities {
    public static final DeferredRegister<EntityType<?>> ENTITIES =
            DeferredRegister.create(ForgeRegistries.ENTITY_TYPES, MDProMod.MODID);

    public static final RegistryObject<EntityType<DuelFieldEntity>> DUEL_FIELD =
            ENTITIES.register("duel_field", () -> EntityType.Builder
                    .<DuelFieldEntity>of(DuelFieldEntity::new, MobCategory.MISC)
                    .sized(16.0F, 0.4F)
                    .clientTrackingRange(32)
                    .updateInterval(20)
                    .build("duel_field"));

    public static final RegistryObject<EntityType<CardCaseEntity>> CARD_CASE =
            ENTITIES.register("card_case", () -> EntityType.Builder
                    .<CardCaseEntity>of(CardCaseEntity::new, MobCategory.MISC)
                    .sized(0.6F, 0.8F)
                    .clientTrackingRange(16)
                    .updateInterval(20)
                    .build("card_case"));

    public static final RegistryObject<EntityType<DuelCardEntity>> DUEL_CARD =
            ENTITIES.register("duel_card", () -> EntityType.Builder
                    .<DuelCardEntity>of(DuelCardEntity::new, MobCategory.MISC)
                    .sized(1.0F, 0.1F)
                    .clientTrackingRange(16)
                    .updateInterval(20)
                    .build("duel_card"));

    public static final RegistryObject<EntityType<HandCardEntity>> HAND_CARD =
            ENTITIES.register("hand_card", () -> EntityType.Builder
                    .<HandCardEntity>of(HandCardEntity::new, MobCategory.MISC)
                    .sized(0.6F, 0.85F)
                    .clientTrackingRange(16)
                    .updateInterval(10)
                    .build("hand_card"));

    private ModEntities() {
    }
}
