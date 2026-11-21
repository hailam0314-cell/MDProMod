package net.mdpromod;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
final class AppearanceScreen extends Screen {
    private String fieldId;
    private int back;
    private static final int GAP = 8;
    private int left, top, column, previewH;

    private AppearanceScreen(DuelAppearance saved) {
        super(Component.translatable("item.mdpromod.appearance_settings"));
        fieldId = saved.fieldId();
        back = saved.cardBack();
    }

    static void open(DuelAppearance saved) {
        Minecraft.getInstance().setScreen(new AppearanceScreen(saved));
    }

    static Component fieldName(DuelFieldTheme theme) {
        return theme == DuelFieldTheme.CLASSIC ? label("classic") : Component.translatable("item.mdpromod.duel_field." + theme.id);
    }

    static Component label(String key) {
        return Component.translatable("appearance.mdpromod." + key);
    }

    static void fieldIcon(GuiGraphics g, DuelFieldTheme theme, int x, int y, int w, int h) {
        if (theme == DuelFieldTheme.CLASSIC) {
            int iw = Math.min(w, h * 8 / 7);
            int ih = iw * 7 / 8;
            g.blit(ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/duel_field_atlas.png"),
                x + (w-iw)/2, y + (h-ih)/2, iw, ih, 0, 0, 2048, 1792, 2048, 1792);
        } else {
            int iw = Math.min(w, h * 3 / 2);
            int ih = iw * 2 / 3;
            g.blit(ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/field_icon/" + theme.id + ".png"),
                x + (w-iw)/2, y + (h-ih)/2, iw, ih, 0, 0, 192, 128, 192, 128);
        }
    }

    @Override
    protected void init() {
        column = Math.min(150, (width - 26) / 2);
        previewH = Math.min(105, Math.max(48, height - 106));
        top = (height - previewH - 94) / 2;
        left = (width - column * 2 - GAP) / 2;
        int right = left + column + GAP;
        int y = top + 30;
        addRenderableWidget(Button.builder(Component.empty(), b -> minecraft.setScreen(new FieldChoiceScreen(this, fieldId, id -> fieldId = id)))
            .createNarration(message -> label("field").copy())
            .bounds(left,y,column,previewH).build());
        addRenderableWidget(Button.builder(Component.empty(), b -> minecraft.setScreen(new CardBackScreen(this, back, id -> back = id)))
            .createNarration(message -> label("back").copy())
            .bounds(right,y,column,previewH).build());
        int resetY = y + previewH + 3;
        addRenderableWidget(Button.builder(label("from_field"), b -> { fieldId = ""; rebuildWidgets(); })
            .bounds(left,resetY,column,17).build()).active = !fieldId.isEmpty();
        addRenderableWidget(Button.builder(label("from_case"), b -> { back = DuelAppearance.FROM_CASE; rebuildWidgets(); })
            .bounds(right,resetY,column,17).build()).active = back != DuelAppearance.FROM_CASE;
        addRenderableWidget(Button.builder(Component.translatable("gui.cancel"), b -> onClose())
            .bounds(width / 2 - 84,resetY + 25,80,20).build());
        addRenderableWidget(Button.builder(label("save"), b -> {
            ModNetwork.sendToServer(new ModNetwork.AppearanceSave(new DuelAppearance(fieldId, back)));
            onClose();
        }).bounds(width / 2 + 4,resetY + 25,80,20).build());
    }

    @Override
    public void render(GuiGraphics g, int mx, int my, float tick) {
        renderBackground(g);
        super.render(g, mx, my, tick);
        g.drawCenteredString(font, title, width/2, top, 0xffffff);
        int right = left + column + GAP;
        int y = top + 30;
        drawSlot(g, left, y, label("field"), fieldId.isEmpty() ? label("from_field") : fieldName(DuelFieldTheme.byId(fieldId)), mx, my);
        drawSlot(g, right, y, label("back"), back < 0 ? label("from_case") :
            Component.literal(CardBacks.entry(back)
                .name(minecraft.getLanguageManager().getSelected().startsWith("zh"))), mx, my);
        int artH = previewH - 19;
        fieldIcon(g, DuelFieldTheme.byId(fieldId), left + 4, y + 3, column - 8, artH);
        int artW = artH * 11 / 16;
        g.blit(CardBacks.texture(back), right + (column-artW)/2, y + 3, artW, artH, 0, 0, 176, 256, 176, 256);
    }

    private void drawSlot(GuiGraphics g, int x, int y, Component type, Component name, int mx, int my) {
        g.drawCenteredString(font, type, x+column/2, y-12, 0xd2dce4);
        g.fill(x+1, y+1, x+column-1, y+previewH-1, 0xff24303a);
        if (mx>=x && mx<x+column && my>=y && my<y+previewH) g.renderOutline(x,y,column,previewH,0xffe6eeee);
        g.drawCenteredString(font, font.plainSubstrByWidth(name.getString(), column-8), x+column/2, y+previewH-13, 0xffffff);
    }

    @Override public boolean isPauseScreen() { return false; }
}
