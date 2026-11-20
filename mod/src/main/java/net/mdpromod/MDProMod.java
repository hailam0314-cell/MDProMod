package net.mdpromod;

import net.minecraft.core.registries.Registries;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.CreativeModeTab;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.ItemLike;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockBehaviour.Properties;
import net.minecraft.world.level.material.MapColor;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.event.TickEvent.Phase;
import net.minecraftforge.event.TickEvent.ServerTickEvent;
import net.minecraftforge.event.entity.player.AttackEntityEvent;
import net.minecraftforge.event.entity.player.PlayerEvent.PlayerLoggedInEvent;
import net.minecraftforge.event.entity.player.PlayerEvent.PlayerLoggedOutEvent;
import net.minecraftforge.event.server.ServerStoppingEvent;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.javafmlmod.FMLJavaModLoadingContext;
import net.minecraftforge.fml.loading.FMLEnvironment;
import net.minecraftforge.fml.loading.FMLPaths;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

@Mod("mdpromod")
public final class MDProMod {
    public static final String MODID = "mdpromod";
    public static final DeferredRegister<Block> BLOCKS = DeferredRegister.create(ForgeRegistries.BLOCKS, "mdpromod");
    public static final DeferredRegister<Item> ITEMS = DeferredRegister.create(ForgeRegistries.ITEMS, "mdpromod");
    public static final DeferredRegister<CreativeModeTab> TABS = DeferredRegister.create(Registries.CREATIVE_MODE_TAB, "mdpromod");
    public static final RegistryObject<Block> DUEL_ANCHOR = BLOCKS.register(
        "duel_anchor", () -> new Block(Properties.of().mapColor(MapColor.COLOR_BLUE).strength(2.0F))
    );
    public static final RegistryObject<Item> DUEL_ANCHOR_ITEM = ITEMS.register(
        "duel_anchor", () -> new BlockItem((Block)DUEL_ANCHOR.get(), new net.minecraft.world.item.Item.Properties())
    );
    public static final RegistryObject<CreativeModeTab> DUEL_TAB = TABS.register(
        "duel",
        () -> CreativeModeTab.builder()
            .title(Component.translatable("itemGroup.mdpromod.duel"))
            .icon(() -> DuelCardItem.createStack(89631139, 1))
            .displayItems((parameters, output) -> {
                output.accept((ItemLike)DUEL_ANCHOR_ITEM.get());
                output.accept((ItemLike)ModItems.DUEL_FIELD_ITEM.get());
                output.accept((ItemLike)ModItems.CARD_CASE_ITEM.get());
                output.accept((ItemLike)ModItems.MUSIC_SETTINGS_ITEM.get());
                output.accept((ItemLike)ModItems.AI_DECK_ITEM.get());
                output.accept(DuelCardItem.createStack(89631139, 1));
            })
            .build()
    );
    private static final DuelSessions DUELS = new DuelSessions();

    /** Every duel of the server, one session per duel field. */
    static DuelSessions duels() {
        return DUELS;
    }

    /** The single session of older versions; still used by the debug commands and by add-ons such as MDWheel. */
    static DuelSessionManager sessions() {
        return DUELS.shared();
    }

    static DuelSessionManager sessions(DuelFieldEntity field) {
        return DUELS.of(field);
    }

    static DuelSessionManager sessions(ServerPlayer player) {
        return DUELS.of(player);
    }

    public MDProMod(FMLJavaModLoadingContext context) {
        IEventBus bus = context.getModEventBus();
        BLOCKS.register(bus);
        ModItems.ITEMS.register(bus);
        ModEntities.ENTITIES.register(bus);
        ITEMS.register(bus);
        TABS.register(bus);
        ModNetwork.register();
        ModPaths.useGameConfigDir(FMLPaths.CONFIGDIR.get());
        ModPaths.useGameDir(FMLPaths.GAMEDIR.get());
        ModBundle.start(FMLEnvironment.dist == Dist.CLIENT, ModPaths.dataRoot());
        ModPaths.applyBundledHost();
        ModPaths.seedDeckDir();
        CardRegistry.load();
        DuelFieldLayout.load();
        MinecraftForge.EVENT_BUS.register(new DuelCommands(DUELS.shared()));
        MinecraftForge.EVENT_BUS.register(this);
    }

    @SubscribeEvent
    public void onServerStopping(ServerStoppingEvent event) {
        DUELS.shutdown();
    }

    @SubscribeEvent
    public void onPlayerLoggedIn(PlayerLoggedInEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) {
            ItemStack stack = MusicSettingsItem.findSettings(player);
            if (stack != null) {
                ModNetwork.sendTo(player, ModNetwork.MusicSettingsOpen.sync(MusicSettingsItem.snapshot(stack)));
            }
        }
    }

    @SubscribeEvent
    public void onPlayerLoggedOut(PlayerLoggedOutEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) {
            DUELS.onPlayerLeft(player);
        }
    }

    @SubscribeEvent
    public void onServerTick(ServerTickEvent event) {
        if (event.phase == Phase.END) {
            DUELS.tick();
            DuelSelfTest.tick(event.getServer());
        }
    }

    @SubscribeEvent
    public void onAttackEntity(AttackEntityEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) {
            if (event.getTarget() instanceof HandCardEntity card) {
                event.setCanceled(true);
                card.onLeftClick(player);
            } else if (event.getTarget() instanceof DuelCardEntity card) {
                event.setCanceled(true);
                card.onLeftClick(player);
            } else if (event.getTarget() instanceof DuelFieldEntity field) {
                event.setCanceled(true);
                field.onLeftClick(player);
            }
        }
    }
}
