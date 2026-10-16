package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.entity.EntityRenderer;
import net.minecraft.client.renderer.entity.EntityRendererProvider;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.client.gui.Font;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public class DuelFieldRenderer extends EntityRenderer<DuelFieldEntity> {
    private static final ResourceLocation FIELD =
            ResourceLocation.fromNamespaceAndPath(MDProMod.MODID, "textures/entity/duel_field_atlas.png");
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
    /** 贴图分块：{u0, v0, u1, v1}，与 assets/duel_controls/control_spec.json 同步。 */
    private static final float[][] FRAME_TILES = tiles(64, 48, 32, 8);
    private static final float[][] PHASE_TILES = tiles(160, 96, 72, 16);
    private static final float[][] CONFIRM_TILES = tiles(320, 100, 76, 16);
    private static final float[][] LP_TILES = tiles(336, 100, 76, 16);
    /** 按钮底座与键帽的模型单位高度（16 单位 = 1 方块）。 */
    private static final float FRAME_HEIGHT = 0.8F / 16.0F;
    private static final float CAP_HEIGHT = 0.8F / 16.0F;
    private static final float PHASE_CAP_W = 17.2F / 16.0F;
    private static final float PHASE_CAP_D = 7.8F / 16.0F;
    private static final float CONFIRM_CAP_W = 38.8F / 16.0F;
    private static final float CONFIRM_CAP_D = 9.4F / 16.0F;
    private static final float LP_RIM = 1.6F / 16.0F;
    private static final float LP_SLAB_TOP = 1.1F / 16.0F;
    /** 场地 16 x 14 方块，卡与卡背 13.75 x 20 模型单位。 */
    private static final float FIELD_W = 16.0F;
    private static final float FIELD_D = 14.0F;
    private static final float CARD_W = 13.75F / 16.0F;
    private static final float CARD_D = 20.0F / 16.0F;
    private final Font font;

    private static ResourceLocation texture(String name) {
        return ResourceLocation.fromNamespaceAndPath(MDProMod.MODID, "textures/entity/" + name + ".png");
    }

    public DuelFieldRenderer(EntityRendererProvider.Context context) {
        super(context);
        this.font = context.getFont();
    }

    @Override
    public ResourceLocation getTextureLocation(DuelFieldEntity entity) {
        return FIELD;
    }

    @Override
    public void render(DuelFieldEntity entity, float entityYaw, float partialTicks,
                       PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        poseStack.pushPose();
        poseStack.mulPose(Axis.YP.rotationDegrees(-entityYaw));
        VertexConsumer mat = buffer.getBuffer(
                net.minecraft.client.renderer.RenderType.entityCutoutNoCull(FIELD));
        RenderUtil.flatQuad(poseStack, mat, FIELD_W, FIELD_D, 0.01F,
                0.0F, 0.0F, 1.0F, 1.0F, packedLight);

        // 刚放下时只有场地本身；放上卡盒之后才出现卡组 / 额外卡组模型。
        VertexConsumer cardBack = buffer.getBuffer(
                net.minecraft.client.renderer.RenderType.entityCutoutNoCull(CardTextureCache.CARD_BACK));
        for (String side : new String[]{"p1", "p2"}) {
            if (!entity.hasDeck(side)) continue;
            final String boundSide = side;
            int remaining = Math.max(entity.remainingCount(boundSide), entity.mainCount(boundSide) > 0 ? 1 : 0);
            DuelFieldLayout.byId(side + "_deck").ifPresent(zone ->
                    drawStack(poseStack, cardBack, zone,
                            stackLayers(remaining, 10, 6), packedLight));
            DuelFieldLayout.byId(side + "_extra").ifPresent(zone ->
                    drawStack(poseStack, cardBack, zone,
                            stackLayers(entity.extraCount(boundSide), 5, 3), packedLight));
            DuelFieldLayout.byId(side + "_graveyard").ifPresent(zone -> {
                if (entity.graveyardCount(boundSide) > 0) {
                    drawStack(poseStack, cardBack, zone,
                            stackLayers(entity.graveyardCount(boundSide), 5, 6), packedLight);
                }
            });
            DuelFieldLayout.byId(side + "_banished").ifPresent(zone -> {
                if (entity.banishedCount(boundSide) > 0) {
                    drawStack(poseStack, cardBack, zone,
                            stackLayers(entity.banishedCount(boundSide), 5, 6), packedLight);
                }
            });
        }
        // Controls are deliberately rendered as small in-world models instead of HUD widgets.
        // Their state is replicated by DuelFieldEntity, so both players see the same physical board.
        for (DuelFieldLayout.Control control : DuelFieldLayout.controls()) {
            drawControl(entity, control, poseStack, buffer, packedLight);
        }
        poseStack.popPose();
        super.render(entity, entityYaw, partialTicks, poseStack, buffer, packedLight);
    }

    private void drawControl(DuelFieldEntity entity, DuelFieldLayout.Control control,
                             PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        poseStack.pushPose();
        poseStack.translate(control.x() / 16.0, 0.0, control.z() / 16.0);
        poseStack.mulPose(Axis.YP.rotationDegrees(-control.rotationY()));
        float top = (float) control.y() / 16.0F;
        float halfW = (float) control.width() / 32.0F;
        float halfD = (float) control.depth() / 32.0F;
        if ("lp_display".equals(control.kind())) {
            drawLifePanel(entity, control, top, halfW, halfD, poseStack, buffer, packedLight);
        } else {
            drawPushButton(control, entity.controlState(control), top, halfW, halfD,
                    poseStack, buffer, packedLight);
        }
        poseStack.popPose();
    }

    /** 底座 + 键帽的两段式按钮；键帽贴图按状态切换（灰=不可按，绿=可按，蓝=当前）。 */
    private void drawPushButton(DuelFieldLayout.Control control, String state, float top,
                                float halfW, float halfD, PoseStack poseStack,
                                MultiBufferSource buffer, int packedLight) {
        boolean confirm = "confirm".equals(control.kind());
        float capTop = top;
        float frameTop = capTop - CAP_HEIGHT;
        float frameBottom = frameTop - FRAME_HEIGHT;
        drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack,
                -halfW, frameBottom, -halfD, halfW, frameTop, halfD, packedLight);
        float capW = (confirm ? CONFIRM_CAP_W : PHASE_CAP_W) / 2.0F;
        float capD = (confirm ? CONFIRM_CAP_D : PHASE_CAP_D) / 2.0F;
        drawTileBox(buffer, stateTexture(control, state), confirm ? CONFIRM_TILES : PHASE_TILES,
                poseStack, -capW, frameTop, -capD, capW, capTop, capD, packedLight);
        String label = controlLabel(control, state);
        if (!label.isEmpty()) {
            float maxWidth = (confirm ? 34.0F : 14.0F) / 16.0F;
            float height = (confirm ? 4.6F : 3.6F) / 16.0F;
            drawFlatText(label, 0.0F, capTop + 0.004F, maxWidth, height, 0xFFFFFFFF,
                    poseStack, buffer, packedLight);
        }
    }

    /** LP 显示屏：下沉的面板 + 四根围栏，数字画在屏幕中央。 */
    private void drawLifePanel(DuelFieldEntity entity, DuelFieldLayout.Control control, float top,
                               float halfW, float halfD, PoseStack poseStack,
                               MultiBufferSource buffer, int packedLight) {
        float slabTop = top - 0.7F / 16.0F;
        float slabBottom = top - 1.6F / 16.0F;
        drawTileBox(buffer, LP_SCREEN, LP_TILES, poseStack,
                -halfW, slabBottom, -halfD, halfW, slabTop, halfD, packedLight);
        float innerZ = halfD - LP_RIM;
        drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack,
                -halfW, slabTop, -innerZ, -halfW + LP_RIM, top, innerZ, packedLight);
        drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack,
                halfW - LP_RIM, slabTop, -innerZ, halfW, top, innerZ, packedLight);
        drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack,
                -halfW, slabTop, innerZ, halfW, top, halfD, packedLight);
        drawTileBox(buffer, CONTROL_FRAME, FRAME_TILES, poseStack,
                -halfW, slabTop, -halfD, halfW, top, -innerZ, packedLight);
        String text = Integer.toString(entity.lifePoints(control.owner()));
        drawFlatText(text, 0.0F, slabTop + 0.004F, 30.0F / 16.0F, 5.5F / 16.0F, 0xFFB9E8B0,
                poseStack, buffer, packedLight);
    }

    private void drawTileBox(MultiBufferSource buffer, ResourceLocation texture, float[][] tiles,
                             PoseStack poseStack, float x0, float y0, float z0,
                             float x1, float y1, float z1, int packedLight) {
        VertexConsumer consumer = buffer.getBuffer(RenderType.entityCutoutNoCull(texture));
        float[][] uv = new float[][]{tiles[1], tiles[1], tiles[1], tiles[1], tiles[0], tiles[2]};
        RenderUtil.box(poseStack, consumer, x0, y0, z0, x1, y1, z1, uv, packedLight);
    }

    /** 平铺在顶面上的文字：内容居中，超出宽度时自动缩小。 */
    private void drawFlatText(String text, float x, float y, float maxWidth, float height,
                              int color, PoseStack poseStack, MultiBufferSource buffer, int packedLight) {
        if (text == null || text.isEmpty()) return;
        float width = this.font.width(text);
        float scale = height / 8.0F;
        if (width * scale > maxWidth) scale = maxWidth / width;
        poseStack.pushPose();
        poseStack.translate(x, y, 0.0F);
        poseStack.mulPose(Axis.XP.rotationDegrees(90.0F));
        poseStack.scale(scale, scale, scale);
        this.font.drawInBatch(Component.literal(text), -width / 2.0F, -4.0F, color, true,
                poseStack.last().pose(), buffer, Font.DisplayMode.NORMAL, 0, packedLight);
        poseStack.popPose();
    }

    private static ResourceLocation stateTexture(DuelFieldLayout.Control control, String state) {
        if ("confirm".equals(control.kind())) {
            if ("started".equals(state)) return CONFIRM_STARTED;
            if ("ready".equals(state)) return CONFIRM_READY;
            if ("my_turn".equals(state)) return CONFIRM_TURN_MINE;
            if ("opponent_turn".equals(state)) return CONFIRM_TURN_OPPONENT;
            if ("victory".equals(state)) return CONFIRM_VICTORY;
            if ("defeat".equals(state)) return CONFIRM_DEFEAT;
            return CONFIRM_DISABLED;
        }
        if ("current".equals(state)) return PHASE_CURRENT;
        if ("available".equals(state)) return PHASE_AVAILABLE;
        return PHASE_DISABLED;
    }

    private static String controlLabel(DuelFieldLayout.Control control, String state) {
        if ("confirm".equals(control.kind())) {
            if ("started".equals(state)) return "游戏中";
            if ("ready".equals(state)) return "准备";
            if ("my_turn".equals(state)) return "我的回合";
            if ("opponent_turn".equals(state)) return "对方回合";
            if ("victory".equals(state)) return "胜利";
            if ("defeat".equals(state)) return "失败";
            return "开始决斗";
        }
        return control.label();
    }

    /** [0] 顶面 / [1] 侧面 / [2] 底面，把像素分块换算成 0..1 的 UV。 */
    private static float[][] tiles(int width, int height, int topHeight, int sideHeight) {
        float h = height;
        float top = topHeight;
        float side = topHeight + sideHeight;
        float pad = side + 8.0F;
        return new float[][]{
                {0.0F, 0.0F, 1.0F, top / h},
                {0.0F, top / h, 1.0F, side / h},
                {0.0F, side / h, 1.0F, pad / h},
        };
    }

    private static int stackLayers(int cards, int perLayer, int max) {
        if (cards <= 0) return 1;
        return Math.max(1, Math.min(max, (cards + perLayer - 1) / perLayer));
    }

    private static void drawStack(PoseStack poseStack, VertexConsumer consumer,
                                  DuelFieldLayout.Zone zone, int layers, int packedLight) {
        poseStack.pushPose();
        poseStack.translate(zone.x() / 16.0, 0.02, zone.z() / 16.0);
        poseStack.mulPose(Axis.YP.rotationDegrees(-zone.rotationY()));
        for (int i = 0; i < layers; i++) {
            poseStack.pushPose();
            poseStack.translate(0.0, i * 0.006, 0.0);
            RenderUtil.flatQuad(poseStack, consumer, CARD_W, CARD_D, 0.0F,
                    0.0F, 0.0F, 1.0F, 1.0F, packedLight);
            poseStack.popPose();
        }
        poseStack.popPose();
    }
}
