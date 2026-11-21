package net.mdpromod;

import net.minecraft.world.item.Item;
import net.minecraft.world.item.Item.Properties;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

public final class ModItems {
    public static final DeferredRegister<Item> ITEMS = DeferredRegister.create(ForgeRegistries.ITEMS, "mdpromod");
    public static final RegistryObject<Item> DUEL_FIELD_ITEM = ITEMS.register("duel_field", () -> new DuelFieldItem(new Properties().stacksTo(1)));
    public static final RegistryObject<Item> CARD_CASE_ITEM = ITEMS.register("card_case", () -> new CardCaseItem(new Properties().stacksTo(1)));
    public static final RegistryObject<Item> DUEL_CARD_ITEM = ITEMS.register("duel_card", () -> new DuelCardItem(new Properties().stacksTo(64)));
    public static final RegistryObject<Item> MUSIC_SETTINGS_ITEM = ITEMS.register("music_settings", () -> new MusicSettingsItem(new Properties().stacksTo(1)));
    public static final RegistryObject<Item> AI_DECK_ITEM = ITEMS.register("ai_deck", () -> new AiDeckItem(new Properties().stacksTo(1)));

    public static final RegistryObject<Item> APPEARANCE_SETTINGS_ITEM = ITEMS.register("appearance_settings", () -> new AppearanceSettingsItem(new Properties().stacksTo(1)));

    private ModItems() {
    }
}
