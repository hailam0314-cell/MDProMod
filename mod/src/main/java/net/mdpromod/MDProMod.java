package net.mdpromod;

import net.minecraft.core.registries.Registries;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.CreativeModeTab;
import net.minecraft.world.item.Item;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockBehaviour;
import net.minecraft.world.level.material.MapColor;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.event.server.ServerStoppingEvent;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.event.entity.player.AttackEntityEvent;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.javafmlmod.FMLJavaModLoadingContext;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

/** First Forge-side projection layer for the MDProMod duel host. */
@Mod(MDProMod.MODID)
public final class MDProMod {
    public static final String MODID = "mdpromod";

    public static final DeferredRegister<Block> BLOCKS =
            DeferredRegister.create(ForgeRegistries.BLOCKS, MODID);
    public static final DeferredRegister<Item> ITEMS =
            DeferredRegister.create(ForgeRegistries.ITEMS, MODID);
    public static final DeferredRegister<CreativeModeTab> TABS =
            DeferredRegister.create(Registries.CREATIVE_MODE_TAB, MODID);

    public static final RegistryObject<Block> DUEL_ANCHOR = BLOCKS.register(
            "duel_anchor", () -> new Block(BlockBehaviour.Properties.of()
                    .mapColor(MapColor.COLOR_BLUE)
                    .strength(2.0f)));
    public static final RegistryObject<Item> DUEL_ANCHOR_ITEM = ITEMS.register(
            "duel_anchor", () -> new BlockItem(DUEL_ANCHOR.get(), new Item.Properties()));
    public static final RegistryObject<CreativeModeTab> DUEL_TAB = TABS.register(
            "duel", () -> CreativeModeTab.builder()
                    .title(net.minecraft.network.chat.Component.translatable("itemGroup.mdpromod.duel"))
                    .icon(() -> DuelCardItem.createStack(89631139, 1))
                    .displayItems((parameters, output) -> {
                        output.accept(DUEL_ANCHOR_ITEM.get());
                        output.accept(ModItems.DUEL_FIELD_ITEM.get());
                        output.accept(ModItems.CARD_CASE_ITEM.get());
                        output.accept(ModItems.MUSIC_SETTINGS_ITEM.get());
                        output.accept(ModItems.AI_DECK_ITEM.get());
                        output.accept(DuelCardItem.createStack(89631139, 1));
                    }).build());

    private static final DuelSessionManager DUEL_SESSIONS = new DuelSessionManager();

    /** 给决斗盘实体等地方取用对局管理器。 */
    static DuelSessionManager sessions() {
        return DUEL_SESSIONS;
    }

    public MDProMod(FMLJavaModLoadingContext context) {
        IEventBus bus = context.getModEventBus();
        BLOCKS.register(bus);
        ModItems.ITEMS.register(bus);
        ModEntities.ENTITIES.register(bus);
        ITEMS.register(bus);
        TABS.register(bus);
        ModNetwork.register();
        // 玩家要改的东西放游戏 config 目录（<游戏目录>/config/mdpromod/），别塞在决斗宿主的资源目录里
        ModPaths.useGameConfigDir(net.minecraftforge.fml.loading.FMLPaths.CONFIGDIR.get());
        // 卡图 / 立绘 / 召唤特效 / 决斗宿主运行时都随包带着，首次启动解到 <游戏目录>/mdpromod，
        // 这样玩家不需要另外装 MDPro3、也不用手工摆资源目录
        ModPaths.useGameDir(net.minecraftforge.fml.loading.FMLPaths.GAMEDIR.get());
        ModBundle.start(net.minecraftforge.fml.loading.FMLEnvironment.dist
                == net.minecraftforge.api.distmarker.Dist.CLIENT, ModPaths.dataRoot());
        ModPaths.applyBundledHost();
        ModPaths.seedDeckDir();
        CardRegistry.load();
        DuelFieldLayout.load();
        MinecraftForge.EVENT_BUS.register(new DuelCommands(DUEL_SESSIONS));
        MinecraftForge.EVENT_BUS.register(this);
    }

    @SubscribeEvent
    public void onServerStopping(ServerStoppingEvent event) {
        DUEL_SESSIONS.stop();
    }

    /**
     * 玩家上线时把「音乐设置」里存的那首曲子同步给客户端。
     * 曲子是每个客户端自己的选择，所以在客户端记住即可；不打开界面，只应用。
     */
    @SubscribeEvent
    public void onPlayerLoggedIn(net.minecraftforge.event.entity.player.PlayerEvent.PlayerLoggedInEvent event) {
        if (!(event.getEntity() instanceof ServerPlayer player)) return;
        net.minecraft.world.item.ItemStack stack = MusicSettingsItem.findSettings(player);
        if (stack == null) return;
        ModNetwork.sendTo(player, ModNetwork.MusicSettingsOpen.sync(MusicSettingsItem.snapshot(stack)));
    }

    /** 把决斗主机的实时统计推给已绑定的决斗盘。 */
    @SubscribeEvent
    public void onServerTick(TickEvent.ServerTickEvent event) {
        if (event.phase == TickEvent.Phase.END) {
            DUEL_SESSIONS.tickField();
        }
    }

    /** 左键点卡：手牌 = 打出/盖放，场上卡 = 攻击/表示变更/发动；一律取消普通攻击。 */
    @SubscribeEvent
    public void onAttackEntity(AttackEntityEvent event) {
        if (!(event.getEntity() instanceof ServerPlayer player)) return;
        if (event.getTarget() instanceof HandCardEntity card) {
            event.setCanceled(true);
            card.onLeftClick(player);
        } else if (event.getTarget() instanceof DuelCardEntity card) {
            event.setCanceled(true);
            card.onLeftClick(player);
        } else if (event.getTarget() instanceof DuelFieldEntity field) {
            // 左键点场地格子：核心正在问落点时就是"选这个格子"
            event.setCanceled(true);
            field.onLeftClick(player);
        }
    }
}
