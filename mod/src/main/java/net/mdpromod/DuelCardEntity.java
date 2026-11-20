package net.mdpromod;

import java.util.ArrayList;
import java.util.List;
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

public class DuelCardEntity extends Entity {
    private UUID duelField;
    private String owner = "";
    private int privateCode;
    private static final EntityDataAccessor<Integer> DATA_CARD_CODE = SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_FACE_UP = SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_DEFENSE = SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_ZONE_ID = SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Boolean> DATA_DISABLED = SynchedEntityData.defineId(DuelCardEntity.class, EntityDataSerializers.BOOLEAN);

    public void configureDuel(UUID field, String owner, int privateCode) {
        this.duelField = field;
        this.owner = owner == null ? "" : owner;
        this.privateCode = privateCode;
    }

    public boolean belongsTo(UUID field) {
        return field.equals(this.duelField);
    }

    public DuelCardEntity(EntityType<DuelCardEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    protected void defineSynchedData() {
        this.entityData.define(DATA_CARD_CODE, 0);
        this.entityData.define(DATA_FACE_UP, true);
        this.entityData.define(DATA_DEFENSE, false);
        this.entityData.define(DATA_ZONE_ID, "");
        this.entityData.define(DATA_DISABLED, false);
    }

    protected void readAdditionalSaveData(CompoundTag tag) {
        this.setCardCode(tag.getInt("CardCode"));
        this.setFaceUp(tag.getBoolean("FaceUp"));
        this.setDefense(tag.getBoolean("Defense"));
        this.setZoneId(tag.getString("ZoneId"));
        this.setDisabled(tag.getBoolean("Disabled"));
    }

    protected void addAdditionalSaveData(CompoundTag tag) {
        tag.putInt("CardCode", this.getCardCode());
        tag.putBoolean("FaceUp", this.isFaceUp());
        tag.putBoolean("Defense", this.isDefense());
        tag.putString("ZoneId", this.getZoneId());
        tag.putBoolean("Disabled", this.isDisabled());
    }

    public void tick() {
        super.tick();
        this.setDeltaMovement(Vec3.ZERO);
        this.setNoGravity(true);
    }

    public boolean isPickable() {
        return true;
    }

    public int getCardCode() {
        return (Integer)this.entityData.get(DATA_CARD_CODE);
    }

    public void setCardCode(int code) {
        this.entityData.set(DATA_CARD_CODE, code);
    }

    public boolean isFaceUp() {
        return (Boolean)this.entityData.get(DATA_FACE_UP);
    }

    public void setFaceUp(boolean faceUp) {
        this.entityData.set(DATA_FACE_UP, faceUp);
    }

    public boolean isDefense() {
        return (Boolean)this.entityData.get(DATA_DEFENSE);
    }

    public void setDefense(boolean defense) {
        this.entityData.set(DATA_DEFENSE, defense);
    }

    public String getZoneId() {
        return (String)this.entityData.get(DATA_ZONE_ID);
    }

    public void setZoneId(String zoneId) {
        this.entityData.set(DATA_ZONE_ID, zoneId == null ? "" : zoneId);
    }

    public boolean isDisabled() {
        return (Boolean)this.entityData.get(DATA_DISABLED);
    }

    public void setDisabled(boolean disabled) {
        if ((Boolean)this.entityData.get(DATA_DISABLED) != disabled) {
            this.entityData.set(DATA_DISABLED, disabled);
        }
    }

    public void onLeftClick(ServerPlayer player) {
        if (this.duelField != null) {
            String zone = this.getZoneId();
            List<int[]> candidates = new ArrayList<>();
            if (zone.startsWith("shared_emz_")) {
                int[] asP1 = DuelFieldLayout.engineAddress(zone, 0);
                int[] asP2 = DuelFieldLayout.engineAddress(zone, 1);
                if (asP1 != null) {
                    candidates.add(asP1);
                }

                if (asP2 != null) {
                    candidates.add(asP2);
                }
            } else {
                int[] address = DuelFieldLayout.engineAddress(zone, 0);
                if (address != null) {
                    candidates.add(address);
                }
            }

            MDProMod.sessions(player).offerCardActions(player, this.getId(), candidates.toArray(new int[0][]));
        }
    }

    public InteractionResult interact(Player player, InteractionHand hand) {
        if (this.level().isClientSide) {
            return InteractionResult.SUCCESS;
        }

        if (this.duelField == null) {
            if (player.isShiftKeyDown()) {
                this.setDefense(!this.isDefense());
            } else {
                this.setFaceUp(!this.isFaceUp());
            }

            CardInfo info = CardRegistry.get(this.getCardCode()).orElse(null);
            String name = info == null ? "#" + this.getCardCode() : info.name();
            player.displayClientMessage(Component.literal(name + " faceUp=" + this.isFaceUp() + " defense=" + this.isDefense()), false);
            return InteractionResult.SUCCESS;
        } else {
            if (player instanceof ServerPlayer serverPlayer && (this.isFaceUp() || this.owner.equals(player.getUUID().toString()))) {
                ModNetwork.sendTo(serverPlayer, new ModNetwork.OpenCard(this.privateCode));
            }

            return InteractionResult.CONSUME;
        }
    }
}
