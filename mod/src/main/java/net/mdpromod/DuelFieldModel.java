package net.mdpromod;

import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import com.mojang.logging.LogUtils;
import java.io.BufferedInputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.LightTexture;
import net.minecraft.client.renderer.MultiBufferSource;
import net.minecraft.client.renderer.RenderType;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.resources.ResourceLocation;
import org.joml.Quaternionf;

final class DuelFieldModel {
    private record Gear(float x, float y, float z, float ax, float ay, float az, float speed) {}
    private record Vent(int kind, float x, float y, float z, float dx, float dy, float dz, float period, float delay) {}
    private record Batch(ResourceLocation texture, int kind, Gear gear, float[] vertices, int[] indices, VertexLoop loop, org.joml.Vector3f billboard) {}
    private record VertexLoop(float periodTicks, short[][] offsets, float[] pose, float unit) {
        static VertexLoop read(DataInputStream in, float[] vertices, float unit) throws IOException {
            float duration = in.readFloat();
            int frames = in.readUnsignedByte();
            int coords = vertices.length / 8 * 3;
            if (!Float.isFinite(duration) || duration <= 0 || frames < 2 || (long)frames * coords > 4_000_000) {
                throw new IOException("Invalid field animation size");
            }
            short[][] offsets = new short[frames][coords];
            for (short[] frame : offsets) {
                for (int i = 0; i < coords; i++) frame[i] = in.readShort();
            }
            return new VertexLoop(duration * 20, offsets, vertices.clone(), unit);
        }

        float[] sample(float[] base, long tick, float partialTick) {
            double time = (tick % (double)periodTicks + partialTick) * offsets.length / periodTicks;
            int step = (int)time;
            short[] from = offsets[step % offsets.length], to = offsets[(step + 1) % offsets.length];
            float blend = (float)(time - step);
            for (int i = 0, j = 0; i < base.length; i += 8) {
                for (int axis = 0; axis < 3; axis++, j++) {
                    pose[i + axis] = base[i + axis] + (from[j] + (to[j] - from[j]) * blend) * unit;
                }
            }
            return pose;
        }
    }
    private final List<Batch> batches = new ArrayList<>();
    private final List<Vent> vents = new ArrayList<>();
    private float sx, sy, sz;

    DuelFieldModel(DuelFieldTheme theme) {
        var file = ResourceLocation.fromNamespaceAndPath("mdpromod", "field/" + theme.id + "/scene.bin");
        try (var in = new DataInputStream(new BufferedInputStream(Minecraft.getInstance().getResourceManager().open(file)))) {
            int format = in.readInt();
            boolean catalog = format == 0x47544635;
            boolean vertexLoops = catalog || format == 0x47544634;
            boolean typedVents = vertexLoops || format == 0x47544633;
            boolean animated = typedVents || format == 0x47544632;
            if (!animated && format != 0x47544631) throw new IOException("Unknown duel field mesh format");
            int count = in.readUnsignedShort();
            if (animated) {
                sx = in.readFloat(); sy = in.readFloat(); sz = in.readFloat();
                if (sx == 0 || sy == 0 || sz == 0) throw new IOException("Invalid duel field scale");
            }
            for (int i = 0; i < count; i++) {
                var texture = ResourceLocation.fromNamespaceAndPath("mdpromod", "textures/field/" + theme.id + "/" + in.readUTF() + ".png");
                int kind = animated ? in.readUnsignedByte() : 0;
                if (kind > (catalog ? 11 : vertexLoops ? 10 : typedVents ? 7 : 3)) throw new IOException("Unknown duel field batch");
                Gear gear = kind == 1 ? new Gear(in.readFloat(), in.readFloat(), in.readFloat(),
                    in.readFloat(), in.readFloat(), in.readFloat(), in.readFloat()) : null;
                int vertices = in.readInt(), indices = in.readInt();
                if (vertices <= 0 || vertices > 65536 || indices <= 0 || indices > 1000000 || indices % 3 != 0) {
                    throw new IOException("Invalid duel field mesh size");
                }
                float[] data = new float[vertices * 8];
                for (int v = 0; v < data.length; v += 8) {
                    for (int j = 0; j < 3; j++) data[v+j] = in.readShort() / (catalog ? 128F : 1024F);
                    data[v+3] = in.readFloat();
                    data[v+4] = in.readFloat();
                    for (int j = 5; j < 8; j++) data[v+j] = in.readByte() / 127F;
                }
                int[] order = new int[indices];
                for (int j = 0; j < indices; j++) {
                    int index = in.readUnsignedShort();
                    if (index >= vertices) throw new IOException("Invalid duel field vertex index");
                    order[j] = index * 8;
                }
                var billboard = kind == 10 ? new org.joml.Vector3f(in.readFloat(), in.readFloat(), in.readFloat()) : null;
                VertexLoop loop = kind >= 8 && kind <= 10 ? VertexLoop.read(in, data, 1F / (catalog ? 256F : 4096F)) : null;
                batches.add(new Batch(texture, kind, gear, data, order, loop, billboard));
            }
            if (animated) {
                int ventCount = in.readUnsignedShort();
                for (int i = 0; i < ventCount; i++) {
                    vents.add(new Vent(typedVents ? in.readUnsignedByte() : 0, in.readFloat(), in.readFloat(), in.readFloat(), in.readFloat(),
                        in.readFloat(), in.readFloat(), in.readFloat(), in.readFloat()));
                }
            }
        } catch (IOException e) {
            batches.clear();
            vents.clear();
            LogUtils.getLogger().warn("Could not load duel field {}", file, e);
        }
    }

    boolean draw(PoseStack poses, MultiBufferSource buffers, int light, long tick, float partialTick, float fieldYaw) {
        var billboardRotation = new Quaternionf().rotationY((float)Math.toRadians(fieldYaw))
            .mul(Minecraft.getInstance().gameRenderer.getMainCamera().rotation());
        float gearSeconds = ((tick % 100) + partialTick) / 20F;
        float glow = (float)(0.5 + 0.5 * Math.cos(((tick % 80) + partialTick) * Math.PI / 40));
        float lavaPhase = ((tick % 1000) + partialTick) / 1000F;
        for (var batch : batches) {
            Gear g = batch.gear;
            if (g != null) {
                poses.pushPose();
                poses.translate(g.x, g.y, g.z);
                poses.scale(sx, sy, sz);
                poses.mulPose(new Quaternionf().rotationAxis((float)Math.toRadians(g.speed * gearSeconds), g.ax, g.ay, g.az));
                poses.scale(1/sx, 1/sy, 1/sz);
                poses.translate(-g.x, -g.y, -g.z);
            }
            boolean window = batch.kind == 2 || batch.kind == 3;
            boolean lava = batch.kind == 4;
            boolean glowMask = batch.kind == 5;
            boolean water = batch.kind == 6;
            boolean foam = batch.kind == 7;
            var layer = window ? RenderType.eyes(batch.texture) : glowMask ? RenderType.entityTranslucentEmissive(batch.texture)
                : water || foam ? RenderType.entityTranslucent(batch.texture) : RenderType.entityCutoutNoCull(batch.texture);
            var out = buffers.getBuffer(layer);
            int tint = 0xffffffff;
            if (window) {
                float intensity = (batch.kind == 2 ? 0.62F : 1F) * (0.58F + 0.42F * glow);
                tint = 0xff000000 | (int)(255 * intensity) << 16 | (int)(209 * intensity) << 8 | (int)(108 * intensity);
            }
            if (foam) {
                poses.pushPose();
                poses.translate(0, 0.008, 0);
            }
            float[] vertices = batch.loop == null ? batch.vertices : batch.loop.sample(batch.vertices, tick, partialTick);
            if (batch.billboard != null) {
                // Branches are flat foliage cards. Preserve the clip's translation
                // while rotating the card towards the active camera (including battle shots).
                float x = batch.billboard.x + vertices[0] - batch.vertices[0];
                float y = batch.billboard.y + vertices[1] - batch.vertices[1];
                float z = batch.billboard.z + vertices[2] - batch.vertices[2];
                poses.pushPose();
                poses.translate(x, y, z);
                poses.mulPose(billboardRotation);
                poses.translate(-x, -y, -z);
            }
            var pose = poses.last();
            int brightness = window || lava || glowMask || batch.kind == 11 ? LightTexture.FULL_BRIGHT : light;
            if (batch.kind == 9) {
                // Paper lanterns retain a warm light after sunset.
                brightness = LightTexture.pack(Math.max(11, LightTexture.block(light)), LightTexture.sky(light));
            }
            float flowU = lava ? lavaPhase : 0;
            float flowV = lava ? -2 * lavaPhase : 0;
            if (water || foam) {
                float phase = ((tick % 480) + partialTick) / 480F;
                flowU = water ? phase : -2 * phase;
                flowV = foam ? -phase : phase;
            }
            if (glowMask) tint = ((int)(198 + 57 * glow) << 24) | 0xffffff;
            for (int i = 0; i < batch.indices.length; i += 3) {
                vertex(out, pose, vertices, batch.indices[i], brightness, tint, flowU, flowV);
                vertex(out, pose, vertices, batch.indices[i+1], brightness, tint, flowU, flowV);
                vertex(out, pose, vertices, batch.indices[i+2], brightness, tint, flowU, flowV);
                vertex(out, pose, vertices, batch.indices[i+2], brightness, tint, flowU, flowV);
            }
            if (g != null || foam || batch.billboard != null) poses.popPose();
        }
        return !batches.isEmpty();
    }

    void emitAmbient(DuelFieldEntity field, long tick) {
        var level = field.level();
        double yaw = Math.toRadians(field.getYRot());
        double sin = Math.sin(yaw), cos = Math.cos(yaw);
        for (int i = 0; i < vents.size(); i++) {
            Vent vent = vents.get(i);
            long elapsed = Math.floorMod(tick - Math.round(20 * vent.delay) + i * 13L, Math.max(1, Math.round(20 * vent.period)));
            if (vent.kind == 0 && (elapsed > 22 || elapsed % 5 != 0)) continue;
            if (vent.kind == 1 && elapsed % 43 != 0 || vent.kind == 2 && elapsed % 23 != 0) continue;
            if (vent.kind == 3 && (elapsed > 14 || elapsed % 2 != 0)) continue;
            if (vent.kind == 4 && elapsed % 12 != 0) continue;
            if (vent.kind >= 5 && elapsed % (vent.kind == 5 ? 7 : 13) != 0) continue;
            double x = vent.x * cos - vent.z * sin, z = vent.x * sin + vent.z * cos;
            double dx = vent.dx * cos - vent.dz * sin, dz = vent.dx * sin + vent.dz * cos;
            if (vent.kind == 4) {
                var random = level.random;
                double along = 1 + random.nextDouble() * 8;
                double across = (random.nextDouble() - 0.5) * 18;
                // Spring petals enter with the wind from the edge of each half.
                level.addParticle(ParticleTypes.CHERRY_LEAVES,
                    field.getX() + x + dx * along - dz * across,
                    field.getY() + vent.y + random.nextDouble() * 0.8,
                    field.getZ() + z + dz * along + dx * across,
                    dx * 0.19, -0.008, dz * 0.19);
                continue;
            }
            if (vent.kind >= 5) {
                var random = level.random;
                double spread = vent.kind == 5 ? 0.12 : vent.kind == 9 ? 0.45 : 2.0;
                x += (random.nextDouble() - 0.5) * spread;
                z += (random.nextDouble() - 0.5) * spread;
            }
            var particle = switch (vent.kind) {
                case 1 -> ParticleTypes.LAVA;
                case 2 -> ParticleTypes.LARGE_SMOKE;
                case 3 -> ParticleTypes.SPLASH;
                case 5 -> ParticleTypes.FLAME;
                case 6 -> ParticleTypes.SNOWFLAKE;
                case 7 -> ParticleTypes.RAIN;
                case 8 -> ParticleTypes.BUBBLE_POP;
                case 9 -> ParticleTypes.ELECTRIC_SPARK;
                case 10 -> ParticleTypes.WHITE_ASH;
                default -> ParticleTypes.CAMPFIRE_COSY_SMOKE;
            };
            level.addParticle(particle,
                field.getX() + x, field.getY() + vent.y + 0.08, field.getZ() + z,
                dx * 0.15, vent.kind >= 5 ? (vent.kind == 6 || vent.kind == 7 ? -0.04 : 0.012) : 0.025 + Math.max(0, vent.dy) * 0.15, dz * 0.15);
        }
    }

    private static void vertex(VertexConsumer out, PoseStack.Pose pose, float[] v, int i, int light, int tint, float flowU, float flowV) {
        out.vertex(pose.pose(), v[i], v[i+1], v[i+2]).color(tint)
            .uv(v[i+3] + flowU, v[i+4] + flowV).overlayCoords(OverlayTexture.NO_OVERLAY).uv2(light)
            .normal(pose.normal(), v[i+5], v[i+6], v[i+7]).endVertex();
    }
}
