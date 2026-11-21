package net.mdpromod;

import com.mojang.blaze3d.vertex.BufferBuilder;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.math.Axis;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.util.Mth;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;
import org.joml.Matrix4f;

@OnlyIn(Dist.CLIENT)
final class ClientPendulumEffects {
    private record Show(ModNetwork.PendulumScales scales, double start) {}
    private record Landing(int field, Vec3 at, float yaw, boolean high, double start) {}
    private static final Map<Integer, Show> SHOWS = new HashMap<>();
    private static final List<Landing> LANDINGS = new ArrayList<>();
    private static final MultiBufferSource.BufferSource BUFFER = MultiBufferSource.immediate(new BufferBuilder(131072));
    private static ClientLevel level;

    static void start(ModNetwork.PendulumScales scales) {
        if (!ClientSummonEffects.ENABLED) return;
        tick(Minecraft.getInstance());
        SHOWS.put(scales.fieldId(), new Show(scales, ClientAnimBridge.now()));
    }

    static void land(ModNetwork.DuelAnimation card, double start) {
        if (!ClientSummonEffects.ENABLED) return;
        tick(Minecraft.getInstance());
        boolean high = CardRegistry.get(card.cardCode()).map(c -> DuelAudioRules.high(c.type(), c.level())).orElse(false);
        float yaw = card.toYaw() - (card.toDefense() ? 90 : 0);
        LANDINGS.add(new Landing(card.fieldId(), card.to(), yaw, high, start));
    }

    static void stop(int field) {
        SHOWS.remove(field);
        LANDINGS.removeIf(hit -> hit.field == field);
    }

    static void tick(Minecraft mc) {
        if (level != mc.level) {
            SHOWS.clear();
            LANDINGS.clear();
            level = mc.level;
        }
        if (SHOWS.isEmpty() && LANDINGS.isEmpty()) return;
        double now = ClientAnimBridge.now();
        SHOWS.values().removeIf(show -> now - show.start >= SummonTiming.summonMs(5));
        LANDINGS.removeIf(hit -> now - hit.start >= 2000);
    }

    static void render(Minecraft mc, PoseStack poses) {
        if (SHOWS.isEmpty() && LANDINGS.isEmpty()) return;
        Vec3 camera = mc.gameRenderer.getMainCamera().getPosition();
        double now = ClientAnimBridge.now();
        for (Show show : SHOWS.values()) {
            var s = show.scales;
            poses.pushPose();
            poses.translate(s.center().x-camera.x, s.center().y-camera.y, s.center().z-camera.z);
            poses.mulPose(Axis.YP.rotationDegrees(-s.yaw()));
            main(poses, s, (float)(now - show.start) / 1000);
            poses.popPose();
        }
        for (Landing hit : LANDINGS) {
            float t = (float)(now - hit.start) / 1000;
            if (t < 0 || t >= 2) continue;
            poses.pushPose();
            poses.translate(hit.at.x-camera.x, hit.at.y-camera.y+0.03, hit.at.z-camera.z);
            poses.mulPose(Axis.YP.rotationDegrees(-hit.yaw));
            landing(poses, t, hit.high);
            poses.popPose();
        }
        BUFFER.endBatch();
    }

    private static float ramp(float t, float start, float end) {
        float f = Mth.clamp((t-start)/(end-start), 0, 1);
        return f*f*(3-2*f);
    }

    private static float window(float t, float start, float fadeIn, float fadeOut, float end) {
        return ramp(t,start,fadeIn) * (1-ramp(t,fadeOut,end));
    }

    private static void main(PoseStack p, ModNetwork.PendulumScales s, float t) {
        float blue = window(t,0.667F,0.99F,3.25F,3.567F);
        floor(p,"pendulum_spot",0,0,0.20F,9,6,0,0x58CFFF,blue*0.8F);
        float orbit = window(t,1.117F,1.5F,3.1F,3.417F);
        floor(p,"pendulum_orbit",0,0,0.24F,10,8,0,0xB7F9FF,orbit);
        float elapsed = t - 0.65F;
        float duration = 2.9F;
        if (elapsed < 0 || elapsed > duration) return;
        float radius = (float) DuelFieldLayout.widthBlocks() * 0.50F;
        p.pushPose();
        // The lowest tip clears the tabletop. Its lateral extremes reach the two field edges.
        p.translate(0, radius + 0.28F, 0);
        ResourceLocation star = SummonFxTextures.get("star");
        for (int i = 7; i >= 0; i--) {
            float age = elapsed - i * 0.042F;
            if (age < 0) continue;
            float phase = Math.min(age / duration, 1) * 1.417F;
            float angle = 74 * Mth.sin((swing(phase) + 38) * Mth.DEG_TO_RAD);
            p.pushPose();
            p.mulPose(Axis.ZP.rotationDegrees(angle));
            if (i == 0) {
                float lengthScale = radius / 26.6409F;
                p.pushPose();
                // A uniform scale preserves the slender crystal profile of the MDPro3 mesh.
                p.scale(lengthScale, lengthScale, lengthScale);
                mesh(p, "pendulum_crystal");
                p.scale(1.6F, 1.0F, 1.6F);
                mesh(p, "pendulum_chain");
                p.popPose();
            }
            p.translate(0, -radius, 0);
            float size = 0.67F - i * 0.065F;
            quad(p, star, size, size, 0xA7FFFF, 0.92F - i * 0.11F);
            p.popPose();
        }
        p.popPose();
    }

    // Cubic coefficients from the two streamed Y rotation segments of ChokerOffset.
    private static float swing(float t) {
        if (t <= 0.566667F) return (1266.1501F*t - 1076.2278F)*t*t - 37.9858F;
        float d = t - 0.566667F;
        return (690.6276F*d - 880.5502F)*d*d - 153.1820F;
    }

    static void renderScale(PoseStack poses, MultiBufferSource buffers, DuelCardEntity card, float partialTicks) {
        if (!ClientSummonEffects.ENABLED) return;
        boolean left = card.getZoneId().endsWith("_0");
        String digits = Integer.toString(card.pendulumScale());
        int color = left ? 0x339CFF : 0xFF365D;
        for (int i = 0; i < digits.length(); i++) {
            ResourceLocation digit = SummonFxTextures.get((left ? "pendulum_l" : "pendulum_r") + digits.charAt(i));
            poses.pushPose();
            poses.translate((left ? -0.78 : 0.78) + (i - (digits.length() - 1) / 2F) * 0.29F, 0.60, 0);
            quad(buffers, poses, digit, 0.70F, 0.70F, 0xFFFFFF, 1);
            poses.popPose();
        }
        if (!card.hasScaleLink()) return;
        var blue = DuelFieldLayout.byId("p1_szone_0");
        var red = DuelFieldLayout.byId("p1_szone_4");
        if (blue.isEmpty() || red.isEmpty()) return;
        float span = (float) (red.get().x() - blue.get().x()) / 16;
        float time = ((card.tickCount % 200) + partialTicks) / 20;
        ResourceLocation star = SummonFxTextures.get("star");
        for (int side = 0; side < 2; side++) {
            int tint = side == 0 ? color : 0xFF365D;
            for (int i = 0; i < 28; i++) {
                float progress = ((i / 28F) + time * 0.5F) % 1F;
                float x = span * (side == 0 ? progress / 2 : 1 - progress / 2);
                float wave = (float) Math.sin(progress * 16 - time * Math.PI * 2);
                poses.pushPose();
                poses.translate(x, 0.42 + 0.055 * wave, 0.04);
                float size = 0.15F + (i % 4 == 0 ? 0.10F : 0);
                quad(buffers, poses, star, size, size, tint, 0.95F);
                poses.popPose();
            }
        }
    }

    private static void landing(PoseStack p, float t, boolean high) {
        float strength = high ? 1.38F : 1;
        float bloom = ramp(t,0,0.28F);
        float fade = 1-ramp(t,0.28F,1.55F);
        float size = (1.2F+2.5F*bloom)*strength;
        floor(p,"pendulum_land_rosette",0,0,0.04F,size,size,18*t,0x7CFFE7,fade);
        floor(p,"pendulum_land_impact",0,0,0.065F,2.3F+bloom,3.1F+bloom,0,0xFFFFFF,1-ramp(t,0.08F,0.60F));
        floor(p,"pendulum_land_rainbow",0,0,0.08F,size*1.25F,size*1.25F,0,0xFFFFFF,fade);
        if (high) {
            float ring = 2+4*ramp(t,0,0.78F);
            floor(p,"pendulum_land_ring",0,0,0.11F,ring,ring,-45*t,0xC2A4FF,1-ramp(t,0.18F,0.82F));
        }
        int count = high ? 24 : 14;
        ResourceLocation star = SummonFxTextures.get("star");
        for (int i=0; i<count; i++) {
            float age = t-(i%4)*0.065F;
            if (age<0 || age>=1.6F) continue;
            float a = i*2.39996F;
            float radius = strength*(0.24F+age*(0.55F+i%5*0.19F));
            float height = age*(1.9F+0.38F*(i%4))-0.8F*age*age;
            p.pushPose();
            p.translate(Math.cos(a)*radius,height,Math.sin(a)*radius);
            p.mulPose(Axis.YP.rotationDegrees(i*37));
            float sz = (0.15F+0.055F*(i%3))*strength;
            int tint = switch(i%3) {case 0 -> 0x83F6FF; case 1 -> 0xFFC6F8; default -> 0xFFF1A5;};
            quad(p,star,sz,sz,tint,1-ramp(age,0.3F,1.6F));
            p.popPose();
        }
    }

    private static void floor(PoseStack p,String texture,float x,float z,float y,float width,float height,
                              float spin,int color,float alpha) {
        if (alpha <= 0) return;
        p.pushPose();
        p.translate(x,y,z);
        p.mulPose(Axis.YP.rotationDegrees(spin));
        p.mulPose(Axis.XP.rotationDegrees(-90));
        quad(p,SummonFxTextures.get(texture),width,height,color,alpha);
        p.popPose();
    }

    private static void mesh(PoseStack p,String name) {
        SummonMeshes.Mesh m = SummonMeshes.get(name);
        ResourceLocation tex = SummonFxTextures.get(name);
        if (m == null || tex == null) return;
        VertexConsumer out = BUFFER.getBuffer(RenderType.entityTranslucentEmissive(tex));
        int[] ix = m.indices(), uv = m.uvIndices();
        Matrix4f matrix=p.last().pose();
        for (int i=0; i+2<ix.length; i+=3) {
            // Forge entity layers consume quads. Duplicate the third corner of each triangle.
            vertex(out,matrix,m,ix[i],uv[i]);
            vertex(out,matrix,m,ix[i+1],uv[i+1]);
            vertex(out,matrix,m,ix[i+2],uv[i+2]);
            vertex(out,matrix,m,ix[i+2],uv[i+2]);
        }
    }

    private static void vertex(VertexConsumer out, Matrix4f matrix, SummonMeshes.Mesh mesh, int vi, int ti) {
        float[] p=mesh.positions(), uv=mesh.uvs();
        vertex(out,matrix,p[3*vi],p[3*vi+1],p[3*vi+2],uv[2*ti],1-uv[2*ti+1],0xFFFFFF,255);
    }

    private static void quad(PoseStack p,ResourceLocation tex,float w,float h,int color,float opacity) {
        quad(BUFFER, p, tex, w, h, color, opacity);
    }

    private static void quad(MultiBufferSource buffers, PoseStack p, ResourceLocation tex, float w, float h, int color, float opacity) {
        if (tex == null || opacity <= 0) return;
        VertexConsumer out = buffers.getBuffer(RenderType.entityTranslucentEmissive(tex));
        Matrix4f m=p.last().pose();
        int a=Math.round(255*Mth.clamp(opacity,0,1));
        vertex(out,m,-w/2,-h/2,0,0,1,color,a);
        vertex(out,m, w/2,-h/2,0,1,1,color,a);
        vertex(out,m, w/2, h/2,0,1,0,color,a);
        vertex(out,m,-w/2, h/2,0,0,0,color,a);
    }

    private static void vertex(VertexConsumer out,Matrix4f m,float x,float y,float z,float u,float v,int col,int a) {
        out.vertex(m,x,y,z).color(col>>16 & 255,col>>8 & 255,col & 255,a).uv(u,v)
            .overlayCoords(OverlayTexture.NO_OVERLAY).uv2(15728880).normal(0,0,1).endVertex();
    }
}
