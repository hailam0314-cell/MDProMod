package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.Tesselator;
import com.mojang.math.Axis;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font.DisplayMode;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.MultiBufferSource.BufferSource;
import net.minecraft.network.chat.Component;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class ClientChainState {
    private static final float CARD_W = 0.55F;
    private static final float CARD_H = 0.8F;
    private static final float SPACING = 0.62F;
    private static int fieldEntityId = -1;
    private static int[] codes = new int[0];
    private static int resolving;
    private static int[] owners = new int[0];
    private static int lastCount = -1;
    private static long changedAt;

    private ClientChainState() {
    }

    public static void apply(int fieldId, int[] newCodes, int newResolving, int[] ownerSides) {
        owners = ownerSides;
        if (newCodes != null && codes.length != newCodes.length) {
            changedAt = System.currentTimeMillis();
        }

        fieldEntityId = fieldId;
        codes = newCodes == null ? new int[0] : newCodes;
        resolving = newResolving;
    }

    public static void render(Minecraft minecraft, PoseStack poses) {
        if (codes.length != 0 && minecraft.level != null) {
            if (minecraft.level.getEntity(fieldEntityId) instanceof DuelFieldEntity field) {
                Vec3 cam = minecraft.gameRenderer.getMainCamera().getPosition();
                Vec3 toCamera = new Vec3(cam.x - field.getX(), 0.0, cam.z - field.getZ());
                float yaw = (float)Math.toDegrees(Math.atan2(toCamera.x, toCamera.z));
                BufferSource buffer = MultiBufferSource.immediate(Tesselator.getInstance().getBuilder());
                float total = codes.length * 0.62F;

                for (int i = 0; i < codes.length; i++) {
                    float offset = -total / 2.0F + 0.31F + i * 0.62F;
                    boolean popping = i == codes.length - 1 && System.currentTimeMillis() - changedAt < 400L;
                    float pop = popping ? 1.25F : 1.0F;
                    boolean active = resolving == i + 1;
                    poses.pushPose();
                    poses.translate(field.getX() - cam.x, field.getY() - cam.y + 1.5, field.getZ() - cam.z);
                    poses.mulPose(Axis.YP.rotationDegrees(yaw));
                    poses.translate(offset, 0.0F, 0.0F);
                    poses.mulPose(Axis.XP.rotationDegrees(15.0F));
                    poses.scale(pop, pop, pop);
                    int side = i < owners.length ? owners[i] : -1;
                    var back = CardBacks.texture(side < 0 ? 0 : field.cardBack(side));
                    ClientDuelAnimationRenderer.drawCard(poses, buffer, codes[i] > 0 ? CardTextureCache.get(codes[i]) : back, back, 0.55F, 0.8F, 15728880);
                    poses.popPose();
                    poses.pushPose();
                    poses.translate(field.getX() - cam.x, field.getY() - cam.y + 1.5, field.getZ() - cam.z);
                    poses.mulPose(Axis.YP.rotationDegrees(yaw));
                    poses.translate(offset, 0.49600002F, 0.0F);
                    poses.scale(-0.02F, -0.02F, 0.02F);
                    String label = Integer.toString(i + 1);
                    int width = minecraft.font.width(label);
                    minecraft.font
                        .drawInBatch(
                            Component.literal(label),
                            -width / 2.0F,
                            -4.0F,
                            active ? 16777045 : 16744576,
                            true,
                            poses.last().pose(),
                            buffer,
                            DisplayMode.SEE_THROUGH,
                            0,
                            15728880
                        );
                    poses.popPose();
                }

                buffer.endBatch();
            }
        }
    }
}
