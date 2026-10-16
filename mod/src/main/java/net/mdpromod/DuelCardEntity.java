package net.mdpromod;

import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.network.syncher.EntityDataAccessor;
import net.minecraft.network.syncher.EntityDataSerializers;
import net.minecraft.network.syncher.SynchedEntityData;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

public class DuelCardEntity extends Entity {
    private java.util.UUID duelField;
    private String owner = "";
    private int privateCode;

    public void configureDuel(java.util.UUID field, String owner, int privateCode) {
        this.duelField = field;
        this.owner = owner == null ? "" : owner;
        this.privateCode = privateCode;
    }
    public boolean belongsTo(java.util.UUID field) { return field.equals(duelField); }
    private static final EntityDataAccessor<Integer> DATA_CARD_CODE =
            SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_FACE_UP =
            SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_DEFENSE =
            SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_ZONE_ID =
            SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.STRING);
    /** 效果被无效（核心 QUERY_STATUS bit0）：立绘/卡面据此变灰。 */
    private static final EntityDataAccessor<Boolean> DATA_DISABLED =
            SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);

    public DuelCardEntity(EntityType<DuelCardEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    @Override
    protected void defineSynchedData() {
        this.entityData.define(DATA_CARD_CODE, 0);
        this.entityData.define(DATA_FACE_UP, true);
        this.entityData.define(DATA_DEFENSE, false);
        this.entityData.define(DATA_ZONE_ID, "");
        this.entityData.define(DATA_DISABLED, false);
    }

    @Override
    protected void readAdditionalSaveData(CompoundTag tag) {
        setCardCode(tag.getInt("CardCode"));
        setFaceUp(tag.getBoolean("FaceUp"));
        setDefense(tag.getBoolean("Defense"));
        setZoneId(tag.getString("ZoneId"));
        setDisabled(tag.getBoolean("Disabled"));
    }

    @Override
    protected void addAdditionalSaveData(CompoundTag tag) {
        tag.putInt("CardCode", getCardCode());
        tag.putBoolean("FaceUp", isFaceUp());
        tag.putBoolean("Defense", isDefense());
        tag.putString("ZoneId", getZoneId());
        tag.putBoolean("Disabled", isDisabled());
    }

    @Override
    public void tick() {
        super.tick();
        this.setDeltaMovement(Vec3.ZERO);
        this.setNoGravity(true);
    }

    @Override
    public boolean isPickable() {
        return true;
    }

    public int getCardCode() {
        return this.entityData.get(DATA_CARD_CODE);
    }

    public void setCardCode(int code) {
        this.entityData.set(DATA_CARD_CODE, code);
    }

    public boolean isFaceUp() {
        return this.entityData.get(DATA_FACE_UP);
    }

    public void setFaceUp(boolean faceUp) {
        this.entityData.set(DATA_FACE_UP, faceUp);
    }

    public boolean isDefense() {
        return this.entityData.get(DATA_DEFENSE);
    }

    public void setDefense(boolean defense) {
        this.entityData.set(DATA_DEFENSE, defense);
    }

    public String getZoneId() {
        return this.entityData.get(DATA_ZONE_ID);
    }

    public void setZoneId(String zoneId) {
        this.entityData.set(DATA_ZONE_ID, zoneId == null ? "" : zoneId);
    }

    public boolean isDisabled() {
        return this.entityData.get(DATA_DISABLED);
    }

    public void setDisabled(boolean disabled) {
        if (this.entityData.get(DATA_DISABLED) != disabled) {
            this.entityData.set(DATA_DISABLED, disabled);
        }
    }

    /** 左键：让客户端弹动作按钮（攻击 / 表示变更 / 发动，按核心给的候选决定）。 */
    public void onLeftClick(net.minecraft.server.level.ServerPlayer player) {
        if (duelField == null) return;
        String zone = getZoneId();
        java.util.List<int[]> candidates = new java.util.ArrayList<>();
        if (zone.startsWith("shared_emz_")) {
            // 共享额外怪兽区：同一个物理格子对应两个引擎序号，谁的怪都有可能
            int[] asP1 = DuelFieldLayout.engineAddress(zone, 0);
            int[] asP2 = DuelFieldLayout.engineAddress(zone, 1);
            if (asP1 != null) candidates.add(asP1);
            if (asP2 != null) candidates.add(asP2);
        } else {
            int[] address = DuelFieldLayout.engineAddress(zone, 0);
            if (address != null) candidates.add(address);
        }
        MDProMod.sessions().offerCardActions(player, getId(), candidates.toArray(new int[0][]));
    }

    @Override
    public InteractionResult interact(Player player, InteractionHand hand) {
        if (this.level().isClientSide) return InteractionResult.SUCCESS;
        if (duelField != null) {
            if (player instanceof net.minecraft.server.level.ServerPlayer serverPlayer
                    && (isFaceUp() || owner.equals(player.getUUID().toString()))) {
                ModNetwork.sendTo(serverPlayer, new ModNetwork.OpenCard(privateCode));
            }
            return InteractionResult.CONSUME;
        }
        if (player.isShiftKeyDown()) {
            setDefense(!isDefense());
        } else {
            setFaceUp(!isFaceUp());
        }
        CardInfo info = CardRegistry.get(getCardCode()).orElse(null);
        String name = info == null ? ("#" + getCardCode()) : info.name();
        player.displayClientMessage(Component.literal(
                name + " faceUp=" + isFaceUp() + " defense=" + isDefense()), false);
        return InteractionResult.SUCCESS;
    }
}
