package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.gui.Font;
import net.minecraft.client.gui.Font.DisplayMode;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider.Context;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class DuelFieldRenderer extends EntityRenderer<DuelFieldEntity> {
    private static final ResourceLocation FIELD = ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/duel_field_atlas.png");
    private static final ResourceLocation CONTROL_FRAME = texture("control_frame");
    private static final ResourceLocation PHASE_DISABLED = texture("control_phase_disabled");
    private static final ResourceLocation PHASE_AVAILABLE = texture("control_phase_available");
    private static final ResourceLocation PHASE_CURRENT = texture("control_phase_current");
    private static final ResourceLocation CONFIRM_DISABLED = texture("control_confirm_disabled");
    private static final ResourceLocation CONFIRM_READY = texture("control_confirm_ready");
    private static final ResourceLocation CONFIRM_STARTED = texture("control_confirm_started");
    private static final ResourceLocation CONFIRM_TURN_MINE = texture("control_confirm_turn_mine");
    private static final ResourceLocation CONFIRM_TURN_OPPONENT = texture("control_confirm_turn_opponent");
    private static final ResourceLocation CONFIRM_VICTORY = texture("control_confirm_victory");
    private static final ResourceLocation CONFIRM_DEFEAT = texture("control_confirm_defeat");
    private static final ResourceLocation LP_SCREEN = texture("control_lp_screen");
    private static final float[][] FRAME_TILES = tiles(64, 48, 32, 8);
    private static final float[][] PHASE_TILES = tiles(160, 96, 72, 16);
    private static final float[][] CONFIRM_TILES = tiles(320, 100, 76, 16);
    private static final float[][] LP_TILES = tiles(336, 100, 76, 16);
    private static final float FRAME_HEIGHT = 0.05F;
    private static final float CAP_HEIGHT = 0.05F;
    private static final float PHASE_CAP_W = 1.075F;
    private static final float PHASE_CAP_D = 0.4875F;
    private static final float CONFIRM_CAP_W = 2.425F;
    private static final float CONFIRM_CAP_D = 0.5875F;
    private static final float LP_RIM = 0.1F;
    private static final float LP_SLAB_TOP = 0.06875F;
    private static final float FIELD_W = 16.0F;
    private static final float FIELD_D = 14.0F;
    private static final float CARD_W = 0.859375F;
    private static final float CARD_D = 1.25F;
    private final Font font;
    private final java.util.Map<DuelFieldTheme, DuelFieldModel> models = new java.util.EnumMap<>(DuelFieldTheme.class);
    private final java.util.Map<DuelFieldEntity, Long> lastAmbientTick = new java.util.WeakHashMap<>();

    private static ResourceLocation texture(String name) {
        return ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/entity/" + name + ".png");
    }

    public DuelFieldRenderer(Context context) {
        super(context);
        this.font = context.getFont();
    }

    public ResourceLocation getTextureLocation(DuelFieldEntity entity) {
        return FIELD;
    }

    public void render(DuelFieldEntity entity, float entityYaw, float partialTicks, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        poseStack.pushPose();
        poseStack.mulPose(Axis.YP.rotationDegrees(-entityYaw));
        DuelFieldTheme theme = entity.theme();
        DuelFieldModel model = theme == DuelFieldTheme.CLASSIC ? null : models.computeIfAbsent(theme, DuelFieldModel::new);
        long tick = entity.level().getGameTime();
        if (model == null || !model.draw(poseStack, buffer, packedLight, tick, partialTicks, entityYaw)) {
            VertexConsumer mat = buffer.getBuffer(RenderType.entityCutoutNoCull(FIELD));
            RenderUtil.flatQuad(poseStack, mat, 16.0F, 14.0F, 0.01F, 0.0F, 0.0F, 1.0F, 1.0F, packedLight);
        } else if (lastAmbientTick.getOrDefault(entity, -1L) != tick) {
            lastAmbientTick.put(entity, tick);
            model.emitAmbient(entity, tick);
        }

        for (String side : new String[]{"p1", "p2"}) {
            if (entity.hasDeck(side)) {
                String boundSide = side;
                VertexConsumer cardBack = buffer.getBuffer(RenderType.entityCutoutNoCull(CardBacks.texture(entity.cardBack("p2".equals(side) ? 1 : 0))));
                int remaining = Math.max(entity.remainingCount(boundSide), entity.mainCount(boundSide) > 0 ? 1 : 0);
                DuelFieldLayout.byId(side + "_deck").ifPresent(zone -> drawStack(poseStack, cardBack, entity, zone, stackLayers(remaining, 10, 6), packedLight));
                DuelFieldLayout.byId(side + "_extra")
                    .ifPresent(zone -> drawStack(poseStack, cardBack, entity, zone, stackLayers(entity.extraCount(boundSide), 5, 3), packedLight));
                DuelFieldLayout.byId(side + "_graveyard").ifPresent(zone -> {
                    if (entity.graveyardCount(boundSide) > 0) {
                        drawStack(poseStack, cardBack, entity, zone, stackLayers(entity.graveyardCount(boundSide), 5, 6), packedLight);
                    }
                });
                DuelFieldLayout.byId(side + "_banished").ifPresent(zone -> {
                    if (entity.banishedCount(boundSide) > 0) {
                        drawStack(poseStack, cardBack, entity, zone, stackLayers(entity.banishedCount(boundSide), 5, 6), packedLight);
                    }
                });
            }
        }

        for (DuelFieldLayout.Control control : DuelFieldLayout.controls()) {
            this.drawControl(entity, control, poseStack, buffer, packedLight);
        }

        poseStack.popPose();
        super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
    }

    private void drawControl(DuelFieldEntity entity, DuelFieldLayout.Control control, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        poseStack.pushPose();
        var at = entity.controlLocalPosition(control);
        poseStack.translate(at.x, 0.0, at.z);
        poseStack.mulPose(Axis.YP.rotationDegrees(-control.rotationY()));
        float top = (float) at.y;
        float halfW = (float)control.width() / 32.0F;
        float halfD = (float)control.depth() / 32.0F;
        if ("lp_display".equals(control.kind())) {
            this.drawLifePanel(entity, control, top, halfW, halfD, poseStack, buffer, packedLight);
        } else {
            this.drawPushButton(control, entity.controlState(control), top, halfW, halfD, poseStack, buffer, packedLight);
        }

        poseStack.popPose();
    }

    private void drawPushButton(
        DuelFieldLayout.Control control, String state, float top, float halfW, float halfD, PoseStack poseStack, MultiBufferSource buffer, int packedLight
    ) {
        boolean confirm = "confirm".equals(control.kind());
        float capTop = top;
        float frameTop = capTop - 0.05F;
        float frameBottom = frameTop - 0.05F;
        this.drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack, -halfW, frameBottom, -halfD, halfW, frameTop, halfD, packedLight);
        float capW = (confirm ? 2.425F : 1.075F) / 2.0F;
        float capD = (confirm ? 0.5875F : 0.4875F) / 2.0F;
        this.drawTileBox(
            buffer, stateTexture(control, state), confirm ? CONFIRM_TILES : PHASE_TILES, poseStack, -capW, frameTop, -capD, capW, capTop, capD, packedLight
        );
        String label = controlLabel(control, state);
        if (!label.isEmpty()) {
            float maxWidth = (confirm ? 34.0F : 14.0F) / 16.0F;
            float height = (confirm ? 4.6F : 3.6F) / 16.0F;
            this.drawFlatText(label, 0.0F, capTop + 0.004F, maxWidth, height, -1, poseStack, buffer, packedLight);
        }
    }

    private void drawLifePanel(
        DuelFieldEntity entity,
        DuelFieldLayout.Control control,
        float top,
        float halfW,
        float halfD,
        PoseStack poseStack,
        MultiBufferSource buffer,
        int packedLight
    ) {
        float slabTop = top - 0.04375F;
        float slabBottom = top - 0.1F;
        this.drawTileBox(buffer, LP_SCREEN, LP_TILES, poseStack, -halfW, slabBottom, -halfD, halfW, slabTop, halfD, packedLight);
        float innerZ = halfD - 0.1F;
        this.drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack, -halfW, slabTop, -innerZ, -halfW + 0.1F, top, innerZ, packedLight);
        this.drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack, halfW - 0.1F, slabTop, -innerZ, halfW, top, innerZ, packedLight);
        this.drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack, -halfW, slabTop, innerZ, halfW, top, halfD, packedLight);
        this.drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack, -halfW, slabTop, -halfD, halfW, top, -innerZ, packedLight);
        String text = Integer.toString(entity.lifePoints(control.owner()));
        this.drawFlatText(text, 0.0F, slabTop + 0.004F, 1.875F, 0.34375F, -4593488, poseStack, buffer, packedLight);
    }

    private void drawTileBox(
        MultiBufferSource buffer,
        ResourceLocation texture,
        float[][] tiles,
        PoseStack poseStack,
        float x0,
        float y0,
        float z0,
        float x1,
        float y1,
        float z1,
        int packedLight
    ) {
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityCutoutNoCull(texture));
        float[][] uv = new float[][]{tiles[1], tiles[1], tiles[1], tiles[1], tiles[0], tiles[2]};
        RenderUtil.box(poseStack, consumer, x0, y0, z0, x1, y1, z1, uv, packedLight);
    }

    private void drawFlatText(
        String text, float x, float y, float maxWidth, float height, int color, PoseStack poseStack, MultiBufferSource buffer, int packedLight
    ) {
        if (text != null && !text.isEmpty()) {
            float width = this.font.width(text);
            float scale = height / 8.0F;
            if (width * scale > maxWidth) {
                scale = maxWidth / width;
            }

            poseStack.pushPose();
            poseStack.translate(x, y, 0.0F);
            poseStack.mulPose(Axis.XP.rotationDegrees(90.0F));
            poseStack.scale(scale, scale, scale);
            this.font
                .drawInBatch(
                    Component.literal(text), -width / 2.0F, -4.0F, color, true, poseStack.last().pose(), buffer, DisplayMode.NORMAL, 0, packedLight
                );
            poseStack.popPose();
        }
    }

    private static ResourceLocation stateTexture(DuelFieldLayout.Control control, String state) {
        if ("confirm".equals(control.kind())) {
            if ("started".equals(state)) {
                return CONFIRM_STARTED;
            } else if ("ready".equals(state)) {
                return CONFIRM_READY;
            } else if ("my_turn".equals(state)) {
                return CONFIRM_TURN_MINE;
            } else if ("opponent_turn".equals(state)) {
                return CONFIRM_TURN_OPPONENT;
            } else if ("victory".equals(state)) {
                return CONFIRM_VICTORY;
            } else {
                return "defeat".equals(state) ? CONFIRM_DEFEAT : CONFIRM_DISABLED;
            }
        } else if ("current".equals(state)) {
            return PHASE_CURRENT;
        } else {
            return "available".equals(state) ? PHASE_AVAILABLE : PHASE_DISABLED;
        }
    }

    private static String controlLabel(DuelFieldLayout.Control control, String state) {
        if ("confirm".equals(control.kind())) {
            if ("started".equals(state)) {
                return "游戏中";
            } else if ("ready".equals(state)) {
                return "准备";
            } else if ("my_turn".equals(state)) {
                return "我的回合";
            } else if ("opponent_turn".equals(state)) {
                return "对方回合";
            } else if ("victory".equals(state)) {
                return "胜利";
            } else {
                return "defeat".equals(state) ? "失败" : "开始决斗";
            }
        } else {
            return control.label();
        }
    }

    private static float[][] tiles(int width, int height, int topHeight, int sideHeight) {
        float h = height;
        float top = topHeight;
        float side = topHeight + sideHeight;
        float pad = side + 8.0F;
        return new float[][]{{0.0F, 0.0F, 1.0F, top / h}, {0.0F, top / h, 1.0F, side / h}, {0.0F, side / h, 1.0F, pad / h}};
    }

    private static int stackLayers(int cards, int perLayer, int max) {
        return cards <= 0 ? 1 : Math.max(1, Math.min(max, (cards + perLayer - 1) / perLayer));
    }

    private static void drawStack(PoseStack poseStack, VertexConsumer consumer, DuelFieldEntity field, DuelFieldLayout.Zone zone, int layers, int packedLight) {
        poseStack.pushPose();
        poseStack.translate(zone.x() / 16.0, field.zoneSurface(zone) + 0.02, field.zoneLocalZ(zone));
        poseStack.mulPose(Axis.YP.rotationDegrees(-zone.rotationY()));

        for (int i = 0; i < layers; i++) {
            poseStack.pushPose();
            poseStack.translate(0.0, i * 0.006, 0.0);
            RenderUtil.flatQuad(poseStack, consumer, 0.859375F, 1.25F, 0.0F, 0.0F, 0.0F, 1.0F, 1.0F, packedLight);
            poseStack.popPose();
        }

        poseStack.popPose();
    }
}
