package net.mdpromod;

import net.minecraft.ChatFormatting;
import net.minecraft.core.BlockPos;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.level.Level;

import java.io.IOException;
import java.nio.file.Path;
import java.util.List;

/**
 * 手持卡盒：右键打开卡组编辑器，编辑结果写进物品自身的 NBT，
 * 物品名称跟随卡组名；潜行右键仍然可以放置实体卡盒。
 */
public class CardCaseItem extends Item {
    public static final String TAG_DECK = "Deck";

    public CardCaseItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResult useOn(UseOnContext context) {
        Player player = context.getPlayer();
        if (player == null || !player.isShiftKeyDown()) {
            // 不潜行时交回 use()，右键直接打开卡组编辑器
            return InteractionResult.PASS;
        }
        Level level = context.getLevel();
        if (!level.isClientSide) {
            BlockPos pos = context.getClickedPos().relative(context.getClickedFace());
            CardCaseEntity cardCase = ModEntities.CARD_CASE.get().create(level);
            if (cardCase != null) {
                float yaw = player.getYRot() + 180.0F;
                cardCase.moveTo(pos.getX() + 0.5, pos.getY() + 0.05, pos.getZ() + 0.5, yaw, 0.0F);
                cardCase.setDeckData(getDeck(context.getItemInHand()));
                level.addFreshEntity(cardCase);
                player.displayClientMessage(Component.literal(
                        "已放置卡盒实体：" + getDeck(context.getItemInHand()).summary()), false);
                if (!player.isCreative()) {
                    context.getItemInHand().shrink(1);
                }
            }
        }
        return InteractionResult.sidedSuccess(level.isClientSide);
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);
        if (!(stack.getItem() instanceof CardCaseItem)) {
            return InteractionResultHolder.pass(stack);
        }
        if (level.isClientSide) {
            return InteractionResultHolder.success(stack);
        }
        if (player instanceof ServerPlayer serverPlayer) {
            ModNetwork.sendTo(serverPlayer, ModNetwork.CardCaseOpen.of(getDeck(stack)));
        }
        return InteractionResultHolder.consume(stack);
    }

    @Override
    public Component getName(ItemStack stack) {
        CompoundTag tag = stack.getTag();
        if (tag == null || !tag.contains(TAG_DECK)) {
            return super.getName(stack);
        }
        String name = getDeck(stack).name();
        return name == null || name.isBlank() ? super.getName(stack) : Component.literal(name);
    }

    @Override
    public void appendHoverText(ItemStack stack, Level level,
                                List<Component> tooltip, TooltipFlag flag) {
        DeckData deck = getDeck(stack);
        tooltip.add(Component.literal("主卡组 " + deck.mainDeck().size()
                        + " / 额外卡组 " + deck.extraDeck().size()
                        + " / 副卡组 " + deck.sideDeck().size())
                .withStyle(ChatFormatting.GRAY));
        List<String> problems = DeckRules.problems(deck);
        if (problems.isEmpty()) {
            tooltip.add(Component.literal("卡组合法，可用于决斗").withStyle(ChatFormatting.GREEN));
        } else {
            for (int i = 0; i < Math.min(2, problems.size()); i++) {
                tooltip.add(Component.literal(problems.get(i)).withStyle(ChatFormatting.RED));
            }
        }
        tooltip.add(Component.literal("右键打开卡组编辑器，潜行右键放置实体卡盒")
                .withStyle(ChatFormatting.DARK_GRAY));
    }

    public static DeckData getDeck(ItemStack stack) {
        CompoundTag tag = stack.getTag();
        if (tag == null || !tag.contains(TAG_DECK)) {
            return new DeckData();
        }
        return DeckData.load(tag.getCompound(TAG_DECK));
    }

    public static void setDeck(ItemStack stack, DeckData deck) {
        DeckData target = deck == null ? new DeckData() : deck;
        stack.getOrCreateTag().put(TAG_DECK, target.save());
    }

    /** 先在双手中找，再翻一遍背包；返回背包里那件真实物品（不是副本）。 */
    public static ItemStack findCase(Player player) {
        ItemStack main = player.getMainHandItem();
        if (main.getItem() instanceof CardCaseItem) return main;
        ItemStack off = player.getOffhandItem();
        if (off.getItem() instanceof CardCaseItem) return off;
        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (stack.getItem() instanceof CardCaseItem) return stack;
        }
        return null;
    }

    /** 导出成 MDPro 可读的 YDK（主 / 额外 / 副三区）。 */
    public static Path exportYdk(DeckData deck, Path directory) throws IOException {
        return DeckYdk.write(deck, directory);
    }
}
