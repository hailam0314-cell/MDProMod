package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

/** 客户端侧的同步落地：更新卡组编辑器界面，并把 NBT 镜像到手上那件卡盒。 */
@OnlyIn(Dist.CLIENT)
public final class ClientCardCaseBridge {
    private ClientCardCaseBridge() {
    }

    public static void applySync(ModNetwork.CardCaseSync message) {
        Minecraft minecraft = Minecraft.getInstance();
        DeckData deck = message.toDeck();
        if (minecraft.screen instanceof CardCaseScreen screen) {
            screen.applyDeck(deck);
        }
        mirrorToHand(minecraft, deck);
    }

    public static void openScreen(ModNetwork.CardCaseOpen message) {
        Minecraft minecraft = Minecraft.getInstance();
        DeckData deck = DeckData.of(message.name(), message.main(), message.extra(), message.side());
        mirrorToHand(minecraft, deck);
        CardCaseScreen screen = new CardCaseScreen(deck);
        minecraft.setScreen(screen);
    }

    private static void mirrorToHand(Minecraft minecraft, DeckData deck) {
        if (minecraft.player == null) return;
        ItemStack stack = minecraft.player.getMainHandItem();
        if (!(stack.getItem() instanceof CardCaseItem)) {
            stack = minecraft.player.getOffhandItem();
        }
        if (stack.getItem() instanceof CardCaseItem) {
            CardCaseItem.setDeck(stack, deck);
        }
    }
}
