package net.mdpromod;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.api.distmarker.OnlyIn;

@OnlyIn(Dist.CLIENT)
public final class SummonMeshes {
    private static final Map<String, SummonMeshes.Mesh> CACHE = new HashMap<>();
    private static final Set<String> MISSING = new HashSet<>();

    private SummonMeshes() {
    }

    public static SummonMeshes.Mesh get(String name) {
        if (name != null && !name.isEmpty()) {
            SummonMeshes.Mesh cached = CACHE.get(name);
            if (cached != null) {
                return cached;
            }

            if (MISSING.contains(name)) {
                return null;
            }

            Path path = ModPaths.resolveFile("summonmesh", "mdpromod.summonMeshRoot", "D:\\MDProMod\\runtime\\summon_mesh", name + ".json");
            if (!Files.isRegularFile(path)) {
                if (ModBundle.ready()) {
                    MISSING.add(name);
                }

                return null;
            } else {
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
                        float x = v.get(0).getAsFloat();
                        float y = v.get(1).getAsFloat();
                        float z = v.get(2).getAsFloat();
                        flatPositions[i * 3] = x;
                        flatPositions[i * 3 + 1] = y;
                        flatPositions[i * 3 + 2] = z;
                        radius = Math.max(radius, (float)Math.sqrt(x * x + z * z));
                    }

                    float[] flatUvs = new float[uvs.size() * 2];

                    for (int i = 0; i < uvs.size(); i++) {
                        JsonArray uv = uvs.get(i).getAsJsonArray();
                        flatUvs[i * 2] = uv.get(0).getAsFloat();
                        flatUvs[i * 2 + 1] = uv.get(1).getAsFloat();
                    }

                    int[] flatIndices = new int[indices.size()];

                    for (int i = 0; i < indices.size(); i++) {
                        flatIndices[i] = indices.get(i).getAsInt();
                    }

                    int[] flatUvIndices = new int[uvIndices.size()];

                    for (int i = 0; i < uvIndices.size(); i++) {
                        flatUvIndices[i] = uvIndices.get(i).getAsInt();
                    }

                    SummonMeshes.Mesh mesh = new SummonMeshes.Mesh(flatPositions, flatUvs, flatIndices, flatUvIndices, radius);
                    CACHE.put(name, mesh);
                    return mesh;
                } catch (Exception ex) {
                    MISSING.add(name);
                    return null;
                }
            }
        } else {
            return null;
        }
    }

    public record Mesh(float[] positions, float[] uvs, int[] indices, int[] uvIndices, float radius) {
    }
}
