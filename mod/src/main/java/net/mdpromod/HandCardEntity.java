package net.mdpromod;

import java.util.UUID;
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

public class HandCardEntity extends Entity {
    private int privateCode;
    private static final EntityDataAccessor<Integer> DATA_CARD = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_FIELD = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_SIDE = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_INDEX = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_REVEALED = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_OWNER = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.STRING);

    public HandCardEntity(EntityType<HandCardEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    private static final EntityDataAccessor<Integer> DATA_CARD_BACK = SynchedEntityData.defineId(HandCardEntity.class, EntityDataSerializers.INT);

    int cardBack() {
        return entityData.get(DATA_CARD_BACK);
    }

    void setCardBack(int id) {
        entityData.set(DATA_CARD_BACK, CardBacks.validId(id));
    }

    protected void defineSynchedData() {
        entityData.define(DATA_CARD_BACK, 0);
        this.entityData.define(DATA_CARD, 0);
        this.entityData.define(DATA_FIELD, "");
        this.entityData.define(DATA_SIDE, 0);
        this.entityData.define(DATA_INDEX, 0);
        this.entityData.define(DATA_REVEALED, false);
        this.entityData.define(DATA_OWNER, "");
    }

    protected void readAdditionalSaveData(CompoundTag tag) {
        setCardBack(tag.getInt("CardBack"));
        this.entityData.set(DATA_CARD, tag.getInt("Card"));
        this.entityData.set(DATA_FIELD, tag.getString("Field"));
        this.entityData.set(DATA_SIDE, tag.getInt("Side"));
        this.entityData.set(DATA_INDEX, tag.getInt("Index"));
        this.entityData.set(DATA_REVEALED, tag.getBoolean("Revealed"));
        this.entityData.set(DATA_OWNER, tag.getString("Owner"));
    }

    protected void addAdditionalSaveData(CompoundTag tag) {
        tag.putInt("CardBack", cardBack());
        tag.putInt("Card", this.getCardCode());
        tag.putString("Field", this.fieldId());
        tag.putInt("Side", this.side());
        tag.putInt("Index", this.getIndex());
        tag.putBoolean("Revealed", this.isRevealed());
        tag.putString("Owner", this.ownerId());
    }

    public void tick() {
        super.tick();
        this.setDeltaMovement(Vec3.ZERO);
        this.setNoGravity(true);
    }

    public boolean isPickable() {
        return true;
    }

    public boolean isPushable() {
        return false;
    }

    public boolean shouldRenderAtSqrDistance(double distance) {
        return distance < 9216.0;
    }

    public int getCardCode() {
        return (Integer)this.entityData.get(DATA_CARD);
    }

    public void setCardCode(int code) {
        if ((Integer)this.entityData.get(DATA_CARD) != code) {
            this.entityData.set(DATA_CARD, code);
        }
    }

    public void setPrivateCode(int code) {
        this.privateCode = code;
    }

    public int privateCode() {
        return this.privateCode;
    }

    public String fieldId() {
        return (String)this.entityData.get(DATA_FIELD);
    }

    public int side() {
        return (Integer)this.entityData.get(DATA_SIDE);
    }

    public int getIndex() {
        return (Integer)this.entityData.get(DATA_INDEX);
    }

    public boolean isRevealed() {
        return (Boolean)this.entityData.get(DATA_REVEALED);
    }

    public void setRevealed(boolean revealed) {
        if ((Boolean)this.entityData.get(DATA_REVEALED) != revealed) {
            this.entityData.set(DATA_REVEALED, revealed);
        }
    }

    public String ownerId() {
        return (String)this.entityData.get(DATA_OWNER);
    }

    public void configure(UUID fieldId, String ownerId, int side, int index) {
        this.entityData.set(DATA_FIELD, fieldId.toString());
        this.entityData.set(DATA_OWNER, ownerId == null ? "" : ownerId);
        this.entityData.set(DATA_SIDE, side);
        this.entityData.set(DATA_INDEX, index);
    }

    public boolean belongsTo(UUID fieldId, int side) {
        return fieldId != null && this.side() == side && fieldId.toString().equals(this.fieldId());
    }

    public boolean faceVisibleTo(Player player) {
        return this.isRevealed() ? true : !this.ownerId().isEmpty() && this.ownerId().equals(player.getUUID().toString());
    }

    public InteractionResult interact(Player player, InteractionHand hand) {
        if (this.level().isClientSide) {
            return InteractionResult.SUCCESS;
        }

        if (!this.faceVisibleTo(player)) {
            player.displayClientMessage(Component.literal("那是对方的手牌（未公开）"), true);
            return InteractionResult.SUCCESS;
        }

        if (player instanceof ServerPlayer serverPlayer) {
            ModNetwork.sendTo(serverPlayer, new ModNetwork.OpenCard(this.privateCode > 0 ? this.privateCode : this.getCardCode()));
        }

        return InteractionResult.CONSUME;
    }

    public void onLeftClick(ServerPlayer player) {
        boolean own = !this.ownerId().isEmpty() && this.ownerId().equals(player.getUUID().toString());
        if (!own) {
            player.displayClientMessage(Component.literal("那是对方的手牌，不能操作"), true);
        } else {
            int code = this.privateCode > 0 ? this.privateCode : this.getCardCode();
            String name = CardRegistry.get(code).map(CardInfo::name).orElse("#" + code);
            MDProMod.sessions(player).offerCardActions(player, this.getId(), new int[][]{{this.side(), 2, this.getIndex()}});
            player.displayClientMessage(Component.literal("「" + name + "」：点卡片前面的按钮执行 · 右键看大图"), true);
        }
    }
}
