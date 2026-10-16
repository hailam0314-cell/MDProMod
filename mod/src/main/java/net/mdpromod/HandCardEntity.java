package net.mdpromod;

import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.network.syncher.EntityDataAccessor;
import net.minecraft.network.syncher.EntityDataSerializers;
import net.minecraft.network.syncher.SynchedEntityData;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

import java.util.UUID;

/**
 * 悬浮在世界里的手牌实体：竖直立在决斗盘己方一侧。
 * 卡号只对「本人」或「已公开」的手牌下发——对手的手牌卡号永远不会离开服务端，
 * 客户端只能渲染卡背，从根上防止偷看。
 */
public class HandCardEntity extends Entity {
    // Real code stays on the server; synced data is public information only.
    private int privateCode;
    private static final EntityDataAccessor<Integer> DATA_CARD =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_FIELD =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_SIDE =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_INDEX =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_REVEALED =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_OWNER =
            SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.STRING);

    public HandCardEntity(EntityType<HandCardEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    @Override
    protected void defineSynchedData() {
        this.entityData.define(DATA_CARD, 0);
        this.entityData.define(DATA_FIELD, "");
        this.entityData.define(DATA_SIDE, 0);
        this.entityData.define(DATA_INDEX, 0);
        this.entityData.define(DATA_REVEALED, false);
        this.entityData.define(DATA_OWNER, "");
    }

    @Override
    protected void readAdditionalSaveData(CompoundTag tag) {
        this.entityData.set(DATA_CARD, tag.getInt("Card"));
        this.entityData.set(DATA_FIELD, tag.getString("Field"));
        this.entityData.set(DATA_SIDE, tag.getInt("Side"));
        this.entityData.set(DATA_INDEX, tag.getInt("Index"));
        this.entityData.set(DATA_REVEALED, tag.getBoolean("Revealed"));
        this.entityData.set(DATA_OWNER, tag.getString("Owner"));
    }

    @Override
    protected void addAdditionalSaveData(CompoundTag tag) {
        tag.putInt("Card", getCardCode());
        tag.putString("Field", fieldId());
        tag.putInt("Side", side());
        tag.putInt("Index", getIndex());
        tag.putBoolean("Revealed", isRevealed());
        tag.putString("Owner", ownerId());
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

    @Override
    public boolean isPushable() {
        return false;
    }

    @Override
    public boolean shouldRenderAtSqrDistance(double distance) {
        return distance < 96.0 * 96.0;
    }

    public int getCardCode() {
        return this.entityData.get(DATA_CARD);
    }

    public void setCardCode(int code) {
        if (this.entityData.get(DATA_CARD) != code) this.entityData.set(DATA_CARD, code);
    }

    public void setPrivateCode(int code) { this.privateCode = code; }
    public int privateCode() { return privateCode; }

    public String fieldId() {
        return this.entityData.get(DATA_FIELD);
    }

    public int side() {
        return this.entityData.get(DATA_SIDE);
    }

    public int getIndex() {
        return this.entityData.get(DATA_INDEX);
    }

    public boolean isRevealed() {
        return this.entityData.get(DATA_REVEALED);
    }

    public void setRevealed(boolean revealed) {
        if (this.entityData.get(DATA_REVEALED) != revealed) this.entityData.set(DATA_REVEALED, revealed);
    }

    public String ownerId() {
        return this.entityData.get(DATA_OWNER);
    }

    public void configure(UUID fieldId, String ownerId, int side, int index) {
        this.entityData.set(DATA_FIELD, fieldId.toString());
        this.entityData.set(DATA_OWNER, ownerId == null ? "" : ownerId);
        this.entityData.set(DATA_SIDE, side);
        this.entityData.set(DATA_INDEX, index);
    }

    public boolean belongsTo(UUID fieldId, int side) {
        return fieldId != null && side() == side && fieldId.toString().equals(fieldId());
    }

    /** 卡面是否对该玩家可见。 */
    public boolean faceVisibleTo(Player player) {
        if (isRevealed()) return true;
        return !ownerId().isEmpty() && ownerId().equals(player.getUUID().toString());
    }

    @Override
    public InteractionResult interact(Player player, InteractionHand hand) {
        if (this.level().isClientSide) return InteractionResult.SUCCESS;
        if (!faceVisibleTo(player)) {
            player.displayClientMessage(Component.literal("那是对方的手牌（未公开）"), true);
            return InteractionResult.SUCCESS;
        }
        if (player instanceof ServerPlayer serverPlayer) {
            ModNetwork.sendTo(serverPlayer, new ModNetwork.OpenCard(
                    privateCode > 0 ? privateCode : getCardCode()));
        }
        return InteractionResult.CONSUME;
    }

    /** 左键：让客户端在这张卡前面弹出动作按钮（召唤 / 盖放 / 发动，按核心给的候选决定）。 */
    public void onLeftClick(ServerPlayer player) {
        boolean own = !ownerId().isEmpty() && ownerId().equals(player.getUUID().toString());
        if (!own) {
            player.displayClientMessage(Component.literal("那是对方的手牌，不能操作"), true);
            return;
        }
        int code = privateCode > 0 ? privateCode : getCardCode();
        String name = CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
        MDProMod.sessions().offerCardActions(player, getId(), new int[][]{{side(), 2, getIndex()}});
        player.displayClientMessage(Component.literal(
                "「" + name + "」：点卡片前面的按钮执行 · 右键看大图"), true);
    }
}
