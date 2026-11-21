package net.mdpromod;

import net.minecraft.nbt.CompoundTag;
import net.minecraft.nbt.Tag;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.player.Player;

/** Empty field and -1 sleeve independently defer to the placed item and bound deck. */
record DuelAppearance(String fieldId, int cardBack) {
    static final int FROM_CASE = -1;
    private static final String KEY = "mdpromod.DuelAppearance";
    static final DuelAppearance DEFAULT = new DuelAppearance("", FROM_CASE);

    boolean valid() {
        return (fieldId.isEmpty() || DuelFieldTheme.byId(fieldId).id.equals(fieldId))
            && (cardBack == FROM_CASE || CardBacks.validId(cardBack) == cardBack);
    }

    static DuelAppearance of(Player player) {
        if (player == null) return DEFAULT;
        CompoundTag tag = player.getPersistentData().getCompound(Player.PERSISTED_NBT_TAG).getCompound(KEY);
        String field = tag.getString("Field");
        int back = tag.contains("Back", Tag.TAG_INT) ? tag.getInt("Back") : FROM_CASE;
        return new DuelAppearance(DuelFieldTheme.byId(field).id.equals(field) ? field : "",
            CardBacks.validId(back) == back ? back : FROM_CASE);
    }

    void save(ServerPlayer player) {
        CompoundTag data = player.getPersistentData();
        CompoundTag persisted = data.getCompound(Player.PERSISTED_NBT_TAG);
        CompoundTag settings = new CompoundTag();
        if (!fieldId.isEmpty()) settings.putString("Field", fieldId);
        if (cardBack != FROM_CASE) settings.putInt("Back", cardBack);
        if (settings.isEmpty()) persisted.remove(KEY);
        else persisted.put(KEY, settings);
        data.put(Player.PERSISTED_NBT_TAG, persisted);
    }

    DuelFieldTheme fieldOr(DuelFieldTheme fallback) {
        return fieldId.isEmpty() ? fallback : DuelFieldTheme.byId(fieldId);
    }

    int backOr(int fallback) {
        return cardBack == FROM_CASE ? fallback : cardBack;
    }
}
