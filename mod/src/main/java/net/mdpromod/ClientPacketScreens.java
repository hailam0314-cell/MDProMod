package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

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
        Minecraft.getInstance()
            .setScreen(
                new UnselectCardScreen(
                    message.selectCodes(),
                    message.chosenCodes(),
                    message.min(),
                    message.max(),
                    message.finishable(),
                    message.selectAddrs(),
                    message.chosenAddrs(),
                    message.viewerSeat(),
                    message.hint()
                )
            );
    }

    public static void openPosition(ModNetwork.PositionList message) {
        Minecraft.getInstance().setScreen(new PositionScreen(message.values(), message.labels()));
    }

    public static void openSelectCard(ModNetwork.SelectList message) {
        Minecraft mc = Minecraft.getInstance();
        DuelControlScreen panel = mc.screen instanceof DuelControlScreen p ? p
            : mc.screen instanceof SelectCardScreen previous ? previous.returnPanel() : null;
        mc.setScreen(new SelectCardScreen(message, panel));
    }

    public static void openEffects(ModNetwork.EffectChoices message) {
        Minecraft.getInstance().setScreen(new EffectChoiceScreen(message));
    }

    public static void openChainChoice(ModNetwork.ChainChoice message) {
        Minecraft.getInstance()
            .setScreen(
                new ChainChoiceScreen(
                    message.codes(), message.forced(), message.timing(), message.opening(), message.addrs(), message.viewerSeat(), message.effectTexts()
                )
            );
    }

    public static void openSumChoice(ModNetwork.SumChoice message) {
        Minecraft.getInstance().setScreen(new SumChoiceScreen(message));
    }

    public static void openDecision(ModNetwork.DecisionPrompt message) {
        Minecraft.getInstance().setScreen(new DecisionScreen(message.function(), message.cardName(), message.question()));
    }

    public static void openPile(ModNetwork.PileView message) {
        Minecraft.getInstance()
            .setScreen(
                new PileScreen(
                    message.location(),
                    message.activateSeqs(),
                    message.activateCodes(),
                    message.summonSeqs(),
                    message.summonCodes(),
                    message.allCodes(),
                    message.hidden(),
                    message.fieldId()
                )
            );
    }

    public static void openAnnounce(ModNetwork.AnnounceList message) {
        Minecraft.getInstance().setScreen(new AnnounceScreen(message.kind(), message.pick(), message.values(), message.hint()));
    }

    public static void openRps() {
        ModLog.info("openRps: setting RpsScreen");
        Minecraft.getInstance().setScreen(new RpsScreen());
    }

    public static void openTp() {
        Minecraft.getInstance().setScreen(new TpScreen());
    }
}
