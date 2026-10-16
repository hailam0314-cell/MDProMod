package net.mdpromod;

import net.minecraft.world.item.Item;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

public final class ModItems {
    public static final DeferredRegister<Item> ITEMS =
            DeferredRegister.create(ForgeRegistries.ITEMS, MDProMod.MODID);

    public static final RegistryObject<Item> DUEL_FIELD_ITEM =
            ITEMS.register("duel_field", () ->
                    new DuelFieldItem(new Item.Properties().stacksTo(1)));

    public static final RegistryObject<Item> CARD_CASE_ITEM =
            ITEMS.register("card_case", () ->
                    new CardCaseItem(new Item.Properties().stacksTo(1)));

    public static final RegistryObject<Item> DUEL_CARD_ITEM =
            ITEMS.register("duel_card", () ->
                    new DuelCardItem(new Item.Properties().stacksTo(64)));

    /** 「音乐设置」：右键打开曲目选择界面，选择结果记在物品自己的 NBT 里。 */
    public static final RegistryObject<Item> MUSIC_SETTINGS_ITEM =
            ITEMS.register("music_settings", () ->
                    new MusicSettingsItem(new Item.Properties().stacksTo(1)));

    /** 「人机卡组」：选一个人机，放到决斗盘远半场就能和它开打。 */
    public static final RegistryObject<Item> AI_DECK_ITEM =
            ITEMS.register("ai_deck", () ->
                    new AiDeckItem(new Item.Properties().stacksTo(1)));

    private ModItems() {
    }
}
