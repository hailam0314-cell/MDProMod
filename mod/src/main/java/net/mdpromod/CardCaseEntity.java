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
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.ItemLike;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

public class CardCaseEntity extends Entity {
    private static final EntityDataAccessor<String> DATA_DECK_NAME = SynchedEntityData.defineId(CardCaseEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_MAIN_COUNT = SynchedEntityData.defineId(CardCaseEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_EXTRA_COUNT = SynchedEntityData.defineId(CardCaseEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_SIDE_COUNT = SynchedEntityData.defineId(CardCaseEntity.class, EntityDataSerializers.INT);
    private DeckData deckData = new DeckData();

    public CardCaseEntity(EntityType<CardCaseEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    protected void defineSynchedData() {
        this.entityData.define(DATA_DECK_NAME, "starter");
        this.entityData.define(DATA_MAIN_COUNT, 0);
        this.entityData.define(DATA_EXTRA_COUNT, 0);
        this.entityData.define(DATA_SIDE_COUNT, 0);
    }

    protected void readAdditionalSaveData(CompoundTag tag) {
        if (tag.contains("deck")) {
            this.deckData = DeckData.load(tag.getCompound("deck"));
            this.syncDeckData();
        }
    }

    protected void addAdditionalSaveData(CompoundTag tag) {
        tag.put("deck", this.deckData.save());
    }

    public void tick() {
        super.tick();
        this.setDeltaMovement(Vec3.ZERO);
        this.setNoGravity(true);
    }

    public boolean isPickable() {
        return true;
    }

    public DeckData getDeckData() {
        return this.deckData;
    }

    public void setDeckData(DeckData data) {
        this.deckData = data == null ? new DeckData() : data;
        this.syncDeckData();
    }

    private void syncDeckData() {
        this.entityData.set(DATA_DECK_NAME, this.deckData.name());
        this.entityData.set(DATA_MAIN_COUNT, this.deckData.mainDeck().size());
        this.entityData.set(DATA_EXTRA_COUNT, this.deckData.extraDeck().size());
        this.entityData.set(DATA_SIDE_COUNT, this.deckData.sideDeck().size());
    }

    public String getDeckName() {
        return (String)this.entityData.get(DATA_DECK_NAME);
    }

    public int getMainCount() {
        return (Integer)this.entityData.get(DATA_MAIN_COUNT);
    }

    public int getExtraCount() {
        return (Integer)this.entityData.get(DATA_EXTRA_COUNT);
    }

    public int getSideCount() {
        return (Integer)this.entityData.get(DATA_SIDE_COUNT);
    }

    public InteractionResult interact(Player player, InteractionHand hand) {
        if (this.level().isClientSide) {
            return InteractionResult.SUCCESS;
        }

        if (player.isShiftKeyDown() && player.getMainHandItem().isEmpty() && player.getOffhandItem().isEmpty()) {
            ItemStack stack = new ItemStack((ItemLike)ModItems.CARD_CASE_ITEM.get());
            CardCaseItem.setDeck(stack, this.deckData);
            if (!player.getInventory().add(stack)) {
                player.drop(stack, false);
            }

            player.displayClientMessage(Component.literal("已收回卡盒：" + this.deckData.summary()), false);
            this.discard();
            return InteractionResult.CONSUME;
        } else {
            player.displayClientMessage(Component.literal("Card case: " + this.deckData.summary()), false);
            return InteractionResult.SUCCESS;
        }
    }
}
