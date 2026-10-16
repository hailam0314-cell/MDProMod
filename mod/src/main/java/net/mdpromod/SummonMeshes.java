package net.mdpromod;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * 召唤特效用的 3D 网格（从 MD 的 AssetBundle 里用 UnityPy 导出成 JSON：顶点 / UV / 索引）。
 * 默认目录 {@code D:\MDProMod\runtime\summon_mesh}，可用 {@code -Dmdpromod.summonMeshRoot} 覆盖。
 *
 * 这些是 MD 演出真正的几何（同调星环、超量螺旋锥、仪式漏斗光柱、连接飘带、灵摆传送门…），
 * 我们只把"形状"搬过来，变换节奏自己写（见 ClientSummonEffects）。
 */
@OnlyIn(Dist.CLIENT)
public final class SummonMeshes {
    /** positions/uvs 是扁平的 xyz、uv 数组；indices 每三个一组是一个三角形。 */
    public record Mesh(float[] positions, float[] uvs, int[] indices, int[] uvIndices, float radius) {
    }

    private static final Map<String, Mesh> CACHE = new HashMap<>();
    private static final Set<String> MISSING = new HashSet<>();

    private SummonMeshes() {
    }

    public static Mesh get(String name) {
        if (name == null || name.isEmpty()) return null;
        Mesh cached = CACHE.get(name);
        if (cached != null) return cached;
        if (MISSING.contains(name)) return null;
        Path path = ModPaths.resolveFile("summonmesh", "mdpromod.summonMeshRoot",
                ModPaths.DEFAULT_SUMMON_MESH, name + ".json");
        if (!Files.isRegularFile(path)) {
            // 解包跑完之前不记"缺网格"，否则这次游戏里所有召唤特效都少一层
            if (ModBundle.ready()) MISSING.add(name);
            return null;
        }
        try {
            JsonObject root2 = JsonParser.parseString(Files.readString(path)).getAsJsonObject();
            JsonArray positions = root2.getAsJsonArray("positions");
            JsonArray uvs = root2.getAsJsonArray("uvs");
            JsonArray indices = root2.getAsJsonArray("indices");
            JsonArray uvIndices = root2.has("uvIndices") ? root2.getAsJsonArray("uvIndices") : indices;
            float[] flatPositions = new float[positions.size() * 3];
            float radius = 0.001F;
            for (int i = 0; i < positions.size(); i++) {
                JsonArray v = positions.get(i).getAsJsonArray();
                float x = v.get(0).getAsFloat(), y = v.get(1).getAsFloat(), z = v.get(2).getAsFloat();
                flatPositions[i * 3] = x;
                flatPositions[i * 3 + 1] = y;
                flatPositions[i * 3 + 2] = z;
                radius = Math.max(radius, (float) Math.sqrt(x * x + z * z));
            }
            float[] flatUvs = new float[uvs.size() * 2];
            for (int i = 0; i < uvs.size(); i++) {
                JsonArray uv = uvs.get(i).getAsJsonArray();
                flatUvs[i * 2] = uv.get(0).getAsFloat();
                flatUvs[i * 2 + 1] = uv.get(1).getAsFloat();
            }
            int[] flatIndices = new int[indices.size()];
            for (int i = 0; i < indices.size(); i++) flatIndices[i] = indices.get(i).getAsInt();
            int[] flatUvIndices = new int[uvIndices.size()];
            for (int i = 0; i < uvIndices.size(); i++) flatUvIndices[i] = uvIndices.get(i).getAsInt();
            Mesh mesh = new Mesh(flatPositions, flatUvs, flatIndices, flatUvIndices, radius);
            CACHE.put(name, mesh);
            return mesh;
        } catch (Exception ex) {
            MISSING.add(name);
            return null;
        }
    }
}
