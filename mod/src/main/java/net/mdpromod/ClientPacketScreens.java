package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

/**
 * 数据包收到后要开的那些决策界面，统一关在这个纯客户端类里。
 *
 * <p><b>为什么不能写在数据包 record 的 handle 里</b>：record 是通用类，专用服务器加载它时会
 * 校验它引用的类型；界面类都继承 {@code net.minecraft.client.gui.screens.Screen}，而 Screen
 * 在服务端是被 Forge 清掉的类，于是服务器在模组构造阶段就崩：
 * {@code Attempted to load class net/minecraft/client/gui/screens/Screen for invalid dist DEDICATED_SERVER}。
 * 放在 {@code @OnlyIn(Dist.CLIENT)} 的类里，服务端这个方法体是空的，压根不会去碰客户端类——
 * 和 {@link ClientCardCaseBridge} 一个套路。
 */
@OnlyIn(Dist.CLIENT)
public final class ClientPacketScreens {
    private ClientPacketScreens() {
    }

    public static void openOptions(ModNetwork.OptionList message) {
        Minecraft.getInstance().setScreen(new OptionScreen(message.values(), message.labels()));
    }

    public static void openSortCard(ModNetwork.SortList message) {
        Minecraft.getInstance().setScreen(new SortCardScreen(message.codes()));
    }

    public static void openUnselectCard(ModNetwork.UnselectList message) {
        Minecraft.getInstance().setScreen(new UnselectCardScreen(
                message.selectCodes(), message.chosenCodes(), message.min(), message.max(),
                message.finishable(), message.selectAddrs(), message.chosenAddrs()));
    }

    public static void openPosition(ModNetwork.PositionList message) {
        Minecraft.getInstance().setScreen(new PositionScreen(message.values(), message.labels()));
    }

    public static void openSelectCard(ModNetwork.SelectList message) {
        Minecraft.getInstance().setScreen(new SelectCardScreen(
                message.codes(), message.min(), message.max(), message.addrs()));
    }

    public static void openChainChoice(ModNetwork.ChainChoice message) {
        Minecraft.getInstance().setScreen(new ChainChoiceScreen(
                message.codes(), message.forced(), message.timing(), message.opening(), message.addrs()));
    }

    public static void openDecision(ModNetwork.DecisionPrompt message) {
        Minecraft.getInstance().setScreen(new DecisionScreen(
                message.function(), message.cardName(), message.question()));
    }

    public static void openPile(ModNetwork.PileView message) {
        Minecraft.getInstance().setScreen(new PileScreen(
                message.location(), message.actionSeqs(), message.actionCodes(), message.allCodes()));
    }
}
