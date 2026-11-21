package net.mdpromod;

import java.nio.file.Path;
import java.util.List;
import java.util.Locale;
import java.util.Optional;
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
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

public class DuelFieldEntity extends Entity {
    private static final EntityDataAccessor<Boolean> DATA_P1_DECK = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_P2_DECK = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_P1_NAME = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_P2_NAME = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_P1_MAIN = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_MAIN = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P1_EXTRA = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_EXTRA = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P1_REMAIN = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_REMAIN = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P1_GRAVE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_GRAVE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P1_BANISH = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_BANISH = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_P1_READY = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_P2_READY = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_GAME_STARTED = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<String> DATA_P1_PHASE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_P2_PHASE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_P1_LP = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_LP = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_TURN_SIDE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_PROMPT_SIDE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Boolean> DATA_TO_BATTLE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_TO_END = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_TO_MAIN2 = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_OPENING = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Integer> DATA_RESULT = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_P2_AI = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_TIMING_MODE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_P2_AI_NAME = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_P2_AI_DIALOG = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_AI_SIDE = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_P1_ID = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_P2_ID = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_FIELD_THEME = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.STRING);
    private UUID placerId;

    private static final EntityDataAccessor<Integer> DATA_P1_BACK = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_P2_BACK = SynchedEntityData.defineId(DuelFieldEntity.class, EntityDataSerializers.INT);

    int cardBack(int side) {
        return entityData.get(side == 1 ? DATA_P2_BACK : DATA_P1_BACK);
    }

    void setCardBack(int seat, int back) {
        entityData.set(seat == 1 ? DATA_P2_BACK : DATA_P1_BACK, back);
    }

    DuelFieldTheme theme() {
        return DuelFieldTheme.byId(entityData.get(DATA_FIELD_THEME));
    }

    void setTheme(DuelFieldTheme theme) {
        entityData.set(DATA_FIELD_THEME, theme.id);
    }

    @Override
    public net.minecraft.world.phys.AABB getBoundingBoxForCulling() {
        return theme() != DuelFieldTheme.CLASSIC ? super.getBoundingBoxForCulling().inflate(7, 6, 7) : super.getBoundingBoxForCulling();
    }

    public DuelFieldEntity(EntityType<DuelFieldEntity> type, Level level) {
        super(type, level);
        this.noPhysics = true;
        this.setNoGravity(true);
        this.blocksBuilding = false;
    }

    protected void defineSynchedData() {
        this.entityData.define(DATA_FIELD_THEME, DuelFieldTheme.CLASSIC.id);
        this.entityData.define(DATA_P1_BACK, 0);
        this.entityData.define(DATA_P2_BACK, 0);
        this.entityData.define(DATA_P1_DECK, false);
        this.entityData.define(DATA_P2_DECK, false);
        this.entityData.define(DATA_P1_NAME, "");
        this.entityData.define(DATA_P2_NAME, "");
        this.entityData.define(DATA_P1_MAIN, 0);
        this.entityData.define(DATA_P2_MAIN, 0);
        this.entityData.define(DATA_P1_EXTRA, 0);
        this.entityData.define(DATA_P2_EXTRA, 0);
        this.entityData.define(DATA_P1_REMAIN, 0);
        this.entityData.define(DATA_P2_REMAIN, 0);
        this.entityData.define(DATA_P1_GRAVE, 0);
        this.entityData.define(DATA_P2_GRAVE, 0);
        this.entityData.define(DATA_P1_BANISH, 0);
        this.entityData.define(DATA_P2_BANISH, 0);
        this.entityData.define(DATA_P1_READY, false);
        this.entityData.define(DATA_P2_READY, false);
        this.entityData.define(DATA_GAME_STARTED, false);
        this.entityData.define(DATA_P1_PHASE, "DP");
        this.entityData.define(DATA_P2_PHASE, "DP");
        this.entityData.define(DATA_P1_LP, 8000);
        this.entityData.define(DATA_P2_LP, 8000);
        this.entityData.define(DATA_TURN_SIDE, 0);
        this.entityData.define(DATA_PROMPT_SIDE, -1);
        this.entityData.define(DATA_TO_BATTLE, false);
        this.entityData.define(DATA_TO_END, false);
        this.entityData.define(DATA_TO_MAIN2, false);
        this.entityData.define(DATA_OPENING, false);
        this.entityData.define(DATA_RESULT, 0);
        this.entityData.define(DATA_P2_AI, "");
        this.entityData.define(DATA_TIMING_MODE, DuelTimingMode.DEFAULT.ordinal());
        this.entityData.define(DATA_P2_AI_NAME, "");
        this.entityData.define(DATA_P2_AI_DIALOG, "");
        this.entityData.define(DATA_AI_SIDE, -1);
        this.entityData.define(DATA_P1_ID, "");
        this.entityData.define(DATA_P2_ID, "");
    }

    protected void readAdditionalSaveData(CompoundTag tag) {
        setTheme(DuelFieldTheme.read(tag));
        entityData.set(DATA_P1_BACK, CardBacks.validId(tag.getInt("P1Back")));
        entityData.set(DATA_P2_BACK, CardBacks.validId(tag.getInt("P2Back")));
        this.entityData.set(DATA_P1_DECK, tag.getBoolean("P1Deck"));
        this.entityData.set(DATA_P2_DECK, tag.getBoolean("P2Deck"));
        this.entityData.set(DATA_P1_NAME, tag.getString("P1Name"));
        this.entityData.set(DATA_P2_NAME, tag.getString("P2Name"));
        this.entityData.set(DATA_P1_MAIN, tag.getInt("P1Main"));
        this.entityData.set(DATA_P2_MAIN, tag.getInt("P2Main"));
        this.entityData.set(DATA_P1_EXTRA, tag.getInt("P1Extra"));
        this.entityData.set(DATA_P2_EXTRA, tag.getInt("P2Extra"));
        this.entityData.set(DATA_P1_REMAIN, tag.getInt("P1Remain"));
        this.entityData.set(DATA_P2_REMAIN, tag.getInt("P2Remain"));
        this.entityData.set(DATA_P1_GRAVE, tag.getInt("P1Grave"));
        this.entityData.set(DATA_P2_GRAVE, tag.getInt("P2Grave"));
        this.entityData.set(DATA_P1_BANISH, tag.getInt("P1Banish"));
        this.entityData.set(DATA_P2_BANISH, tag.getInt("P2Banish"));
        this.entityData.set(DATA_P1_READY, tag.getBoolean("P1Ready"));
        this.entityData.set(DATA_P2_READY, tag.getBoolean("P2Ready"));
        this.entityData.set(DATA_GAME_STARTED, tag.getBoolean("GameStarted"));
        this.entityData.set(DATA_P1_PHASE, tag.contains("P1Phase") ? tag.getString("P1Phase") : "DP");
        this.entityData.set(DATA_P2_PHASE, tag.contains("P2Phase") ? tag.getString("P2Phase") : "DP");
        this.entityData.set(DATA_P1_LP, tag.contains("P1Lp") ? tag.getInt("P1Lp") : 8000);
        this.entityData.set(DATA_P2_LP, tag.contains("P2Lp") ? tag.getInt("P2Lp") : 8000);
        this.entityData.set(DATA_TURN_SIDE, tag.contains("TurnSide") ? tag.getInt("TurnSide") : 0);
        this.entityData.set(DATA_P2_AI, tag.getString("P2Ai"));
        this.entityData.set(DATA_P2_AI_NAME, tag.getString("P2AiName"));
        this.entityData.set(DATA_P2_AI_DIALOG, tag.getString("P2AiDialog"));
        this.entityData.set(DATA_AI_SIDE, tag.contains("AiSide") ? tag.getInt("AiSide") : -1);
        this.entityData.set(DATA_P1_ID, tag.getString("P1Id"));
        this.entityData.set(DATA_P2_ID, tag.getString("P2Id"));
        this.placerId = tag.hasUUID("Placer") ? tag.getUUID("Placer") : null;
        this.entityData
            .set(
                DATA_TIMING_MODE,
                DuelTimingMode.byOrdinal(tag.contains("TimingMode") ? tag.getInt("TimingMode") : DuelTimingMode.DEFAULT.ordinal()).ordinal()
            );
    }

    protected void addAdditionalSaveData(CompoundTag tag) {
        theme().write(tag);
        tag.putInt("P1Back", cardBack(0));
        tag.putInt("P2Back", cardBack(1));
        tag.putBoolean("P1Deck", (Boolean)this.entityData.get(DATA_P1_DECK));
        tag.putBoolean("P2Deck", (Boolean)this.entityData.get(DATA_P2_DECK));
        tag.putString("P1Name", (String)this.entityData.get(DATA_P1_NAME));
        tag.putString("P2Name", (String)this.entityData.get(DATA_P2_NAME));
        tag.putInt("P1Main", (Integer)this.entityData.get(DATA_P1_MAIN));
        tag.putInt("P2Main", (Integer)this.entityData.get(DATA_P2_MAIN));
        tag.putInt("P1Extra", (Integer)this.entityData.get(DATA_P1_EXTRA));
        tag.putInt("P2Extra", (Integer)this.entityData.get(DATA_P2_EXTRA));
        tag.putInt("P1Remain", (Integer)this.entityData.get(DATA_P1_REMAIN));
        tag.putInt("P2Remain", (Integer)this.entityData.get(DATA_P2_REMAIN));
        tag.putInt("P1Grave", (Integer)this.entityData.get(DATA_P1_GRAVE));
        tag.putInt("P2Grave", (Integer)this.entityData.get(DATA_P2_GRAVE));
        tag.putInt("P1Banish", (Integer)this.entityData.get(DATA_P1_BANISH));
        tag.putInt("P2Banish", (Integer)this.entityData.get(DATA_P2_BANISH));
        tag.putBoolean("P1Ready", (Boolean)this.entityData.get(DATA_P1_READY));
        tag.putBoolean("P2Ready", (Boolean)this.entityData.get(DATA_P2_READY));
        tag.putBoolean("GameStarted", (Boolean)this.entityData.get(DATA_GAME_STARTED));
        tag.putString("P1Phase", (String)this.entityData.get(DATA_P1_PHASE));
        tag.putString("P2Phase", (String)this.entityData.get(DATA_P2_PHASE));
        tag.putInt("P1Lp", (Integer)this.entityData.get(DATA_P1_LP));
        tag.putInt("P2Lp", (Integer)this.entityData.get(DATA_P2_LP));
        tag.putInt("TurnSide", (Integer)this.entityData.get(DATA_TURN_SIDE));
        tag.putString("P2Ai", (String)this.entityData.get(DATA_P2_AI));
        tag.putString("P2AiName", (String)this.entityData.get(DATA_P2_AI_NAME));
        tag.putString("P2AiDialog", (String)this.entityData.get(DATA_P2_AI_DIALOG));
        tag.putInt("AiSide", (Integer)this.entityData.get(DATA_AI_SIDE));
        tag.putString("P1Id", (String)this.entityData.get(DATA_P1_ID));
        tag.putString("P2Id", (String)this.entityData.get(DATA_P2_ID));
        tag.putInt("TimingMode", (Integer)this.entityData.get(DATA_TIMING_MODE));
        if (this.placerId != null) {
            // Without this the player who placed the board could not pick it up any more after a restart.
            tag.putUUID("Placer", this.placerId);
        }
    }

    public void tick() {
        super.tick();
        this.setDeltaMovement(Vec3.ZERO);
        this.setNoGravity(true);
        if (!this.level().isClientSide && this.tickCount % 20 == 1) {
            // The board is saved with the world, its duel is not: see DuelSessionManager.reconcile.
            MDProMod.duels().reconcile(this);
        }
    }

    public void remove(Entity.RemovalReason reason) {
        if (!this.level().isClientSide && reason.shouldDestroy()) {
            MDProMod.duels().fieldRemoved(this);
        }

        super.remove(reason);
    }

    public boolean isPickable() {
        return true;
    }

    public boolean isPushable() {
        return false;
    }

    public boolean shouldRenderAtSqrDistance(double distance) {
        return distance < 65536.0;
    }

    public boolean hasDeck(String side) {
        return "p2".equals(side) ? (Boolean)this.entityData.get(DATA_P2_DECK) : (Boolean)this.entityData.get(DATA_P1_DECK);
    }

    public boolean hasAnyDeck() {
        return (Boolean)this.entityData.get(DATA_P1_DECK) || (Boolean)this.entityData.get(DATA_P2_DECK);
    }

    public String deckName(String side) {
        return "p2".equals(side) ? (String)this.entityData.get(DATA_P2_NAME) : (String)this.entityData.get(DATA_P1_NAME);
    }

    public int mainCount(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_MAIN) : (Integer)this.entityData.get(DATA_P1_MAIN);
    }

    public int extraCount(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_EXTRA) : (Integer)this.entityData.get(DATA_P1_EXTRA);
    }

    public int remainingCount(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_REMAIN) : (Integer)this.entityData.get(DATA_P1_REMAIN);
    }

    public int graveyardCount(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_GRAVE) : (Integer)this.entityData.get(DATA_P1_GRAVE);
    }

    public int banishedCount(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_BANISH) : (Integer)this.entityData.get(DATA_P1_BANISH);
    }

    private void setInt(EntityDataAccessor<Integer> key, int value) {
        if ((Integer)this.entityData.get(key) != value) {
            this.entityData.set(key, value);
        }
    }

    public void updateLiveStats(int draw0, int draw1, int grave0, int grave1, int banish0, int banish1) {
        boolean p1 = (Boolean)this.entityData.get(DATA_P1_DECK);
        boolean p2 = (Boolean)this.entityData.get(DATA_P2_DECK);
        if (p1) {
            this.setInt(DATA_P1_REMAIN, Math.max(0, (Integer)this.entityData.get(DATA_P1_MAIN) - draw0));
        }

        if (p2) {
            this.setInt(DATA_P2_REMAIN, Math.max(0, (Integer)this.entityData.get(DATA_P2_MAIN) - draw1));
        }

        this.setInt(DATA_P1_GRAVE, p1 ? Math.max(0, grave0) : 0);
        this.setInt(DATA_P2_GRAVE, p2 ? Math.max(0, grave1) : 0);
        this.setInt(DATA_P1_BANISH, p1 ? Math.max(0, banish0) : 0);
        this.setInt(DATA_P2_BANISH, p2 ? Math.max(0, banish1) : 0);
    }

    public void updateVisualCounts(int[] main, int[] extra, int[] grave, int[] banish) {
        this.setInt(DATA_P1_REMAIN, Math.max(0, main[0]));
        this.setInt(DATA_P2_REMAIN, Math.max(0, main[1]));
        this.setInt(DATA_P1_EXTRA, Math.max(0, extra[0]));
        this.setInt(DATA_P2_EXTRA, Math.max(0, extra[1]));
        this.setInt(DATA_P1_GRAVE, Math.max(0, grave[0]));
        this.setInt(DATA_P2_GRAVE, Math.max(0, grave[1]));
        this.setInt(DATA_P1_BANISH, Math.max(0, banish[0]));
        this.setInt(DATA_P2_BANISH, Math.max(0, banish[1]));
    }

    void bindPlayerDeck(String side, DeckData deck, ServerPlayer player) {
        bindDeck(side, deck);
        setCardBack("p2".equals(side) ? 1 : 0, DuelAppearance.of(player).backOr(deck.cardBack()));
    }

    public void bindDeck(String side, DeckData deck) {
        boolean p2 = "p2".equals(side);
        this.entityData.set(p2 ? DATA_P2_BACK : DATA_P1_BACK, deck.cardBack());
        this.entityData.set(p2 ? DATA_P2_DECK : DATA_P1_DECK, true);
        this.entityData.set(p2 ? DATA_P2_NAME : DATA_P1_NAME, deck.name());
        this.entityData.set(p2 ? DATA_P2_MAIN : DATA_P1_MAIN, deck.mainDeck().size());
        this.entityData.set(p2 ? DATA_P2_EXTRA : DATA_P1_EXTRA, deck.extraDeck().size());
        this.entityData.set(p2 ? DATA_P2_REMAIN : DATA_P1_REMAIN, deck.mainDeck().size());
        this.entityData.set(p2 ? DATA_P2_GRAVE : DATA_P1_GRAVE, 0);
        this.entityData.set(p2 ? DATA_P2_BANISH : DATA_P1_BANISH, 0);
        this.entityData.set(p2 ? DATA_P2_READY : DATA_P1_READY, true);
    }

    public boolean isReadyToStart() {
        return (Boolean)this.entityData.get(DATA_P1_READY) && (Boolean)this.entityData.get(DATA_P2_READY);
    }

    public void unbindDeck(String side) {
        boolean p2 = "p2".equals(side);
        int index = p2 ? 1 : 0;
        this.setSeatId(side, null);
        if ((Integer)this.entityData.get(DATA_AI_SIDE) == index) {
            this.entityData.set(DATA_P2_AI, "");
            this.entityData.set(DATA_P2_AI_NAME, "");
            this.entityData.set(DATA_P2_AI_DIALOG, "");
            this.entityData.set(DATA_AI_SIDE, -1);
        }

        this.entityData.set(p2 ? DATA_P2_BACK : DATA_P1_BACK, 0);
        this.entityData.set(p2 ? DATA_P2_DECK : DATA_P1_DECK, false);
        this.entityData.set(p2 ? DATA_P2_NAME : DATA_P1_NAME, "");
        this.entityData.set(p2 ? DATA_P2_MAIN : DATA_P1_MAIN, 0);
        this.entityData.set(p2 ? DATA_P2_EXTRA : DATA_P1_EXTRA, 0);
        this.entityData.set(p2 ? DATA_P2_REMAIN : DATA_P1_REMAIN, 0);
        this.entityData.set(p2 ? DATA_P2_GRAVE : DATA_P1_GRAVE, 0);
        this.entityData.set(p2 ? DATA_P2_BANISH : DATA_P1_BANISH, 0);
        this.entityData.set(p2 ? DATA_P2_READY : DATA_P1_READY, false);
    }

    public void bindAiDeck(String key, String name, String dialog, DeckData deck, String side) {
        this.entityData.set(DATA_P2_AI, key == null ? "" : key);
        this.entityData.set(DATA_P2_AI_NAME, name == null ? "" : name);
        this.entityData.set(DATA_P2_AI_DIALOG, dialog == null ? "" : dialog);
        this.entityData.set(DATA_AI_SIDE, "p2".equals(side) ? 1 : 0);
        this.bindDeck(side == null ? "p2" : side, deck);
    }

    public void bindAiDeck(String key, String name, String dialog, DeckData deck) {
        this.bindAiDeck(key, name, dialog, deck, "p2");
    }

    public String aiKey() {
        return (String)this.entityData.get(DATA_P2_AI);
    }

    public String aiName() {
        return (String)this.entityData.get(DATA_P2_AI_NAME);
    }

    public String aiDialog() {
        return (String)this.entityData.get(DATA_P2_AI_DIALOG);
    }

    public int aiSide() {
        return (Integer)this.entityData.get(DATA_AI_SIDE);
    }

    public void rotateHalfTurn() {
        this.setYRot(this.getYRot() + 180.0F);
    }

    public void swapSides() {
        this.swap(DATA_P1_BACK, DATA_P2_BACK);
        this.swap(DATA_P1_DECK, DATA_P2_DECK);
        this.swap(DATA_P1_NAME, DATA_P2_NAME);
        this.swap(DATA_P1_MAIN, DATA_P2_MAIN);
        this.swap(DATA_P1_EXTRA, DATA_P2_EXTRA);
        this.swap(DATA_P1_REMAIN, DATA_P2_REMAIN);
        this.swap(DATA_P1_GRAVE, DATA_P2_GRAVE);
        this.swap(DATA_P1_BANISH, DATA_P2_BANISH);
        this.swap(DATA_P1_READY, DATA_P2_READY);
        this.swap(DATA_P1_PHASE, DATA_P2_PHASE);
        this.swap(DATA_P1_LP, DATA_P2_LP);
        this.swap(DATA_P1_ID, DATA_P2_ID);
        int ai = (Integer)this.entityData.get(DATA_AI_SIDE);
        if (ai >= 0) {
            this.entityData.set(DATA_AI_SIDE, 1 - ai);
        }

        int result = (Integer)this.entityData.get(DATA_RESULT);
        if (result != 0) {
            this.entityData.set(DATA_RESULT, result == 1 ? 2 : 1);
        }

        this.entityData.set(DATA_TURN_SIDE, 1 - ((Integer)this.entityData.get(DATA_TURN_SIDE) & 1));
        int prompt = (Integer)this.entityData.get(DATA_PROMPT_SIDE);
        if (prompt >= 0) {
            this.entityData.set(DATA_PROMPT_SIDE, 1 - prompt);
        }
    }

    private <T> void swap(EntityDataAccessor<T> first, EntityDataAccessor<T> second) {
        T a = (T)this.entityData.get(first);
        T b = (T)this.entityData.get(second);
        this.entityData.set(first, b);
        this.entityData.set(second, a);
    }

    public boolean isGameStarted() {
        return (Boolean)this.entityData.get(DATA_GAME_STARTED);
    }

    public void setGameStarted(boolean value) {
        this.entityData.set(DATA_GAME_STARTED, value);
    }

    public void setSeatId(String side, UUID id) {
        boolean p2 = "p2".equals(side);
        this.entityData.set(p2 ? DATA_P2_ID : DATA_P1_ID, id == null ? "" : id.toString());
    }

    public String seatId(String side) {
        return (String)this.entityData.get("p2".equals(side) ? DATA_P2_ID : DATA_P1_ID);
    }

    public boolean isSeated(Player player) {
        if (player == null) {
            return false;
        }

        String id = player.getUUID().toString();
        return id.equals(this.seatId("p1")) || id.equals(this.seatId("p2"));
    }

    public UUID placer() {
        return this.placerId;
    }

    public void setPlacer(UUID id) {
        this.placerId = id;
    }

    public int duelResult() {
        return (Integer)this.entityData.get(DATA_RESULT);
    }

    public void setDuelResult(int value) {
        this.entityData.set(DATA_RESULT, value >= 0 && value <= 2 ? value : 0);
    }

    public boolean hasResult() {
        return this.duelResult() != 0;
    }

    public DuelTimingMode timingMode() {
        return DuelTimingMode.byOrdinal((Integer)this.entityData.get(DATA_TIMING_MODE));
    }

    public void setTimingMode(DuelTimingMode mode) {
        this.entityData.set(DATA_TIMING_MODE, (mode == null ? DuelTimingMode.DEFAULT : mode).ordinal());
    }

    public boolean isReady(String side) {
        return "p2".equals(side) ? (Boolean)this.entityData.get(DATA_P2_READY) : (Boolean)this.entityData.get(DATA_P1_READY);
    }

    public void setReady(String side, boolean value) {
        this.entityData.set("p2".equals(side) ? DATA_P2_READY : DATA_P1_READY, value);
    }

    public int lifePoints(String side) {
        return "p2".equals(side) ? (Integer)this.entityData.get(DATA_P2_LP) : (Integer)this.entityData.get(DATA_P1_LP);
    }

    public void setLifePoints(String side, int value) {
        this.entityData.set("p2".equals(side) ? DATA_P2_LP : DATA_P1_LP, Math.max(0, value));
    }

    public String currentPhase(String side) {
        return "p2".equals(side) ? (String)this.entityData.get(DATA_P2_PHASE) : (String)this.entityData.get(DATA_P1_PHASE);
    }

    public void setCurrentPhase(String side, String phase) {
        this.entityData.set("p2".equals(side) ? DATA_P2_PHASE : DATA_P1_PHASE, phase == null ? "DP" : phase);
    }

    public void setTurnSide(int side) {
        this.entityData.set(DATA_TURN_SIDE, side & 1);
    }

    public void setPhasePrompt(int promptedSeat, boolean toBattle, boolean toEnd, boolean toMain2) {
        this.entityData.set(DATA_PROMPT_SIDE, promptedSeat < 0 ? -1 : promptedSeat & 1);
        this.entityData.set(DATA_TO_BATTLE, toBattle);
        this.entityData.set(DATA_TO_END, toEnd);
        this.entityData.set(DATA_TO_MAIN2, toMain2);
    }

    public int promptSide() {
        return (Integer)this.entityData.get(DATA_PROMPT_SIDE);
    }

    public void setOpening(boolean opening) {
        this.entityData.set(DATA_OPENING, opening);
    }

    public boolean isOpening() {
        return (Boolean)this.entityData.get(DATA_OPENING);
    }

    public boolean canAdvance(String side, String phase) {
        int prompted = (Integer)this.entityData.get(DATA_PROMPT_SIDE);
        if (prompted < 0) {
            return false;
        }

        if (!side.equals(prompted == 1 ? "p2" : "p1")) {
            return false;
        }

        return switch (phase == null ? "" : phase.trim().toUpperCase(Locale.ROOT)) {
            case "BP" -> this.entityData.get(DATA_TO_BATTLE);
            case "M2" -> this.entityData.get(DATA_TO_MAIN2);
            case "EP" -> this.entityData.get(DATA_TO_END);
            default -> false;
        };
    }

    public String controlState(DuelFieldLayout.Control control) {
        String side = control.owner();
        if ("lp_display".equals(control.kind())) {
            return "display";
        }

        int result = this.duelResult();
        if ("confirm".equals(control.kind())) {
            if (result != 0) {
                boolean mine = "p1".equals(side) ? result == 1 : result == 2;
                return mine ? "victory" : "defeat";
            } else if (this.isOpening()) {
                return "started";
            } else if (this.isGameStarted()) {
                return side.equals(this.playerSideForTurn()) ? "my_turn" : "opponent_turn";
            } else if (this.isReadyToStart()) {
                return "started";
            } else {
                return this.isReady(side) ? "ready" : "disabled";
            }
        } else if (result != 0) {
            return "disabled";
        } else if (!this.isGameStarted() || this.isOpening()) {
            return "disabled";
        } else if (!side.equals(this.playerSideForTurn())) {
            return "disabled";
        } else if (control.phase().equalsIgnoreCase(this.currentPhase(side))) {
            return "current";
        } else {
            return this.canAdvance(side, control.phase()) ? "available" : "disabled";
        }
    }

    public String playerSideForTurn() {
        return this.entityData.get(DATA_TURN_SIDE) == 1 ? "p2" : "p1";
    }

    public boolean activateControl(ServerPlayer player, DuelFieldLayout.Control control) {
        if (player == null || control == null) {
            return false;
        }

        if (!this.ownsControl(player, control)) {
            return false;
        }

        if ("confirm".equals(control.kind())) {
            this.handleConfirmClick(player);
            return true;
        }

        if (!"phase".equals(control.kind())) {
            return false;
        }

        String state = this.controlState(control);
        if ("available".equals(state)) {
            try {
                MDProMod.sessions(this).submitPhase(this, player, control.phase());
                player.displayClientMessage(Component.literal("已请求进入" + control.label()), true);
            } catch (Exception ex) {
                player.displayClientMessage(Component.literal("切换失败：" + ex.getMessage()), true);
            }
        } else if ("current".equals(state)) {
            player.displayClientMessage(Component.literal("已经是" + control.label() + "阶段"), true);
        } else {
            player.displayClientMessage(Component.literal("现在还不能切换到" + control.label()), true);
        }

        return true;
    }

    public boolean ownsControl(Player player, DuelFieldLayout.Control control) {
        return control != null && player != null ? control.owner().equals(this.sideOfPlayer(player)) : false;
    }

    public String sideOfPlayer(Player player) {
        if (player == null) {
            return "p1";
        } else if (player instanceof ServerPlayer serverPlayer && MDProMod.sessions(this).isSecondPlayer(serverPlayer, this)) {
            return "p2";
        } else {
            String id = player.getUUID().toString();
            if (id.equals(this.seatId("p2"))) {
                return "p2";
            } else {
                return id.equals(this.seatId("p1")) ? "p1" : this.playerSide(player);
            }
        }
    }

    public String caseSideFor(Player player) {
        String aimed = this.aimedSide(player);
        return aimed != null ? aimed : this.playerSide(player);
    }

    public String aimedSide(Player player) {
        DuelFieldLayout.Zone zone = this.findAnyZone(player);
        if (zone != null && !zone.id().startsWith("shared_")) {
            return zone.id().startsWith("p2_") ? "p2" : "p1";
        } else {
            return null;
        }
    }

    public void handleConfirmClick(ServerPlayer player) {
        if (this.hasResult()) {
            MDProMod.sessions(this).resetForNewDuel(player, this);
        } else if (this.isGameStarted()) {
            if (!MDProMod.sessions(this).hasLiveDuel(this)) {
                MDProMod.sessions(this).resetForNewDuel(player, this);
            } else {
                player.displayClientMessage(Component.literal(this.sideOfPlayer(player).equals(this.playerSideForTurn()) ? "现在是我的回合" : "现在是对手的回合"), true);
            }
        } else {
            String side = this.sideOfPlayer(player);
            if (!this.isReady(side)) {
                player.displayClientMessage(Component.literal("先把卡组放到自己这一侧，按钮才会变绿"), true);
            } else if (!this.isReadyToStart()) {
                player.displayClientMessage(Component.literal(this.hasDeck("p2") ? "双方都放入卡组后才能开始决斗" : "等待对手选择卡组"), true);
            } else {
                try {
                    if (MDProMod.sessions(this).requestDuelStart(this, player)) {
                        player.displayClientMessage(Component.literal("猜拳！赢的人选先后手"), true);
                    }
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("开始决斗失败：" + ex.getMessage()), true);
                }
            }
        }
    }

    public InteractionResult interact(Player player, InteractionHand hand) {
        ItemStack held = player.getItemInHand(hand);
        if (held.is(ModItems.APPEARANCE_SETTINGS_ITEM.get())) return held.use(level(), player, hand).getResult();
        if (this.level().isClientSide) {
            return InteractionResult.SUCCESS;
        }

        MDProMod.duels().reconcile(this);
        if (player instanceof ServerPlayer serverPlayer) {
            DuelCardEntity aimedCard = this.aimedCard(player);
            if (aimedCard != null) {
                String aimedZone = aimedCard.getZoneId();
                if (isPile(aimedZone)) {
                    MDProMod.sessions(this).openPile(serverPlayer, this, pileSide(aimedZone), pileLocation(aimedZone));
                    return InteractionResult.CONSUME;
                }

                aimedCard.interact(player, hand);
                return InteractionResult.CONSUME;
            }

            DuelFieldLayout.Zone pileZone = this.findAnyZone(player);
            if (pileZone != null && isPile(pileZone.id())) {
                MDProMod.sessions(this).openPile(serverPlayer, this, pileSide(pileZone.id()), pileLocation(pileZone.id()));
                return InteractionResult.CONSUME;
            }

            DuelFieldLayout.Control control = this.findClickedControl(player);
            if (control != null) {
                if (this.ownsControl(serverPlayer, control)) {
                    this.activateControl(serverPlayer, control);
                } else {
                    player.displayClientMessage(Component.literal("这是对面那半场的按键"), true);
                }

                return InteractionResult.CONSUME;
            }

            if (player.isShiftKeyDown() && player.getMainHandItem().isEmpty() && player.getOffhandItem().isEmpty()) {
                MDProMod.sessions(this).reclaimField(serverPlayer, this);
                return InteractionResult.CONSUME;
            }
        }

        ItemStack stack = player.getItemInHand(hand);
        if (stack.isEmpty() && player instanceof ServerPlayer serverPlayer) {
            MDProMod.sessions(this).linkField(serverPlayer.server, this, serverPlayer);
            String side = this.sideOfPlayer(player);
            player.displayClientMessage(
                Component.literal(
                    "已绑定决斗盘（空手右键）。现在用卡盒右键这里放下卡组；" + ("p2".equals(side) ? "远半场" : "近半场") + (this.hasDeck(side) ? "已有卡组：" + this.deckName(side) : "还没有卡组")
                ),
                false
            );
            return InteractionResult.CONSUME;
        } else if (stack.getItem() instanceof CardCaseItem) {
            ServerPlayer serverPlayer = player instanceof ServerPlayer sp ? sp : null;
            String side = this.caseSideFor(player);
            boolean second = "p2".equals(side);
            if (serverPlayer == null) {
                return InteractionResult.FAIL;
            }

            if (this.isGameStarted() && !this.hasResult()) {
                player.displayClientMessage(Component.literal("决斗进行中，先打完这一局（或投降）再换卡组"), true);
                return InteractionResult.FAIL;
            }

            if (player.isShiftKeyDown()) {
                if (!this.hasDeck(side)) {
                    player.displayClientMessage(Component.literal("这一侧还没有卡组"), true);
                    return InteractionResult.CONSUME;
                } else if (MDProMod.sessions(this).sideTakenByOther(serverPlayer, side)) {
                    player.displayClientMessage(Component.literal("这一侧是对方放的卡组，得由对方来取"), false);
                    return InteractionResult.FAIL;
                } else {
                    MDProMod.sessions(this).clearCaseSide(serverPlayer, this, side);
                    player.displayClientMessage(Component.literal("已把" + (second ? "远半场" : "近半场") + "的卡组取下（卡盒还在你手上）"), false);
                    return InteractionResult.CONSUME;
                }
            } else {
                DeckData deck = CardCaseItem.getDeck(stack);
                List<String> problems = DeckRules.problems(deck);
                if (!problems.isEmpty()) {
                    player.displayClientMessage(Component.literal("卡组还没法放上场：" + String.join("；", problems)), false);
                    return InteractionResult.FAIL;
                }

                if (MDProMod.duels().busyElsewhere(serverPlayer, this) != null) {
                    player.displayClientMessage(Component.literal("你在另一个决斗盘上还有没打完的对局，先打完或投降再来这里"), false);
                    return InteractionResult.FAIL;
                }

                if (second) {
                    if (this.hasDeck("p2") && MDProMod.sessions(this).sideTakenByOther(serverPlayer, side)) {
                        player.displayClientMessage(Component.literal("远半场已经有对手的卡组了（想换自己的卡组，请对着自己那半场放）"), false);
                        return InteractionResult.FAIL;
                    }

                    MDProMod.sessions(this).bindCaseSide(serverPlayer, this, deck, side);
                } else {
                    if (this.hasDeck("p1") && MDProMod.sessions(this).sideTakenByOther(serverPlayer, side)) {
                        player.displayClientMessage(Component.literal("近半场已经有对方的卡组了（可以让对方蹲下右键把自己那份取走）"), false);
                        return InteractionResult.FAIL;
                    }

                    MDProMod.sessions(this).linkField(serverPlayer.server, this, serverPlayer);
                    MDProMod.sessions(this).bindCaseSide(serverPlayer, this, deck, side);
                }

                player.displayClientMessage(
                    Component.literal(
                        "已把卡组「"
                            + deck.name()
                            + "」放到"
                            + (second ? "远半场（你是 p2 / 对手位）" : "近半场（你是 p1）")
                            + "：主 "
                            + deck.mainDeck().size()
                            + " 张 / 额外 "
                            + deck.extraDeck().size()
                            + " 张"
                            + (this.isReadyToStart() ? "" : "（点自己那一侧的绿色按钮开始决斗）")
                    ),
                    false
                );
                if (this.isReadyToStart() && !this.isGameStarted()) {
                    try {
                        if (MDProMod.sessions(this).requestDuelStart(this, serverPlayer)) {
                            player.displayClientMessage(Component.literal("两侧都就位，猜拳！"), false);
                        }
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("自动开始决斗失败：" + ex.getMessage()), false);
                    }
                }

                return InteractionResult.CONSUME;
            }
        } else if (stack.getItem() instanceof AiDeckItem) {
            String side = this.caseSideFor(player);
            String humanSide = this.sideOfPlayer(player);
            if (side.equals(humanSide)) {
                player.displayClientMessage(Component.literal("人机卡组要放到对面那半场"), true);
                return InteractionResult.FAIL;
            }

            if (player instanceof ServerPlayer serverPlayer) {
                if (this.hasDeck(side) && MDProMod.sessions(this).sideTakenByOther(serverPlayer, side)) {
                    player.displayClientMessage(Component.literal("那一侧已经有别人的卡组了，不能盖成人机"), true);
                    return InteractionResult.FAIL;
                }

                MDProMod.sessions(this).bindAiSeat(this, side);
                MDProMod.sessions(this).linkField(serverPlayer.server, this, serverPlayer);
            }

            String key = AiDeckItem.getKey(stack);
            Path root = DuelSessionManager.runtimeRoot();
            AiDeckCatalog.Entry entry = AiDeckCatalog.find(root, key);

            try {
                DeckData deck = AiDeckCatalog.deck(root, key);
                this.bindAiDeck(key, entry == null ? "MDProBot" : entry.name(), entry == null ? "default" : entry.dialog(), deck, side);
                player.displayClientMessage(
                    Component.literal(
                        "人机「"
                            + AiDeckItem.getLabel(stack)
                            + "」已上场：主 "
                            + deck.mainDeck().size()
                            + " 张 / 额外 "
                            + deck.extraDeck().size()
                            + " 张"
                            + (this.isReadyToStart() ? "" : "（点自己那一侧的绿色按钮开始决斗）")
                    ),
                    false
                );
            } catch (Exception error) {
                player.displayClientMessage(Component.literal("放人机卡组失败：" + error.getMessage()), false);
                return InteractionResult.FAIL;
            }

            if (this.isReadyToStart() && !this.isGameStarted() && player instanceof ServerPlayer starter) {
                try {
                    if (MDProMod.sessions(this).requestDuelStart(this, starter)) {
                        player.displayClientMessage(Component.literal("两侧都就位，猜拳！"), false);
                    }
                } catch (Exception ex) {
                    player.displayClientMessage(Component.literal("自动开始决斗失败：" + ex.getMessage()), false);
                }
            }

            return InteractionResult.CONSUME;
        } else if (stack.getItem() instanceof DuelCardItem cardItem) {
            return this.placeCard(player, cardItem.getCardCode(stack), stack) ? InteractionResult.CONSUME : InteractionResult.FAIL;
        } else {
            String p1 = this.entityData.get(DATA_P1_DECK)
                ? "近半场 " + (String)this.entityData.get(DATA_P1_NAME) + "（主 " + this.entityData.get(DATA_P1_MAIN) + "）"
                : "近半场空";
            String p2 = this.entityData.get(DATA_P2_DECK)
                ? "远半场 " + (String)this.entityData.get(DATA_P2_NAME) + "（主 " + this.entityData.get(DATA_P2_MAIN) + "）"
                : "远半场空";
            player.displayClientMessage(Component.literal("决斗盘 " + DuelFieldLayout.zoneCount() + " 区：" + p1 + " / " + p2 + "（把卡盒右键上来放卡组）"), false);
            return InteractionResult.SUCCESS;
        }
    }

    public void onLeftClick(ServerPlayer player) {
        DuelCardEntity aimed = this.aimedCard(player);
        if (aimed != null) {
            String aimedZone = aimed.getZoneId();
            if (isPile(aimedZone)) {
                MDProMod.sessions(this).openPile(player, this, pileSide(aimedZone), pileLocation(aimedZone));
            } else {
                aimed.onLeftClick(player);
            }
        } else {
            DuelFieldLayout.Zone pile = this.findAnyZone(player);
            if (pile != null && isPile(pile.id())) {
                MDProMod.sessions(this).openPile(player, this, pileSide(pile.id()), pileLocation(pile.id()));
            } else if (MDProMod.sessions(this).hasPlacePrompt() && MDProMod.sessions(this).isAskingPlayer(player)) {
                DuelFieldLayout.Zone zone = this.findClickedZone(player);
                if (zone != null) {
                    int[] address = DuelFieldLayout.engineAddress(zone.id(), this.sideOfPlayer(player).equals("p2") ? 1 : 0);

                    try {
                        if (address != null && MDProMod.sessions(this).submitPlaceFor(address[0], address[1], address[2])) {
                            player.displayClientMessage(Component.literal("落点已选定"), true);
                        } else {
                            player.displayClientMessage(Component.literal("这个格子不是合法落点"), true);
                        }
                    } catch (Exception ex) {
                        player.displayClientMessage(Component.literal("选区域失败：" + ex.getMessage()), true);
                    }
                }
            }
        }
    }

    private static boolean isPile(String zoneId) {
        return zoneId.endsWith("_extra") || zoneId.endsWith("_graveyard") || zoneId.endsWith("_banished");
    }

    private static int pileLocation(String zoneId) {
        if (zoneId.endsWith("_extra")) {
            return 64;
        } else {
            return zoneId.endsWith("_graveyard") ? 16 : 32;
        }
    }

    private static int pileSide(String zoneId) {
        return zoneId.startsWith("p2_") ? 1 : 0;
    }

    private DuelCardEntity aimedCard(Player player) {
        Vec3 eye = player.getEyePosition();
        Vec3 end = eye.add(player.getViewVector(1.0F).scale(4.0));
        DuelCardEntity best = null;
        double bestDistance = Double.MAX_VALUE;

        for (DuelCardEntity card : this.level().getEntitiesOfClass(DuelCardEntity.class, this.getBoundingBox().inflate(20.0))) {
            Optional<Vec3> clip = card.getBoundingBox().inflate(0.15).clip(eye, end);
            if (!clip.isEmpty()) {
                double distance = clip.get().distanceToSqr(eye);
                if (!(distance >= bestDistance)) {
                    bestDistance = distance;
                    best = card;
                }
            }
        }

        return best;
    }

    Vec3 controlLocalPosition(DuelFieldLayout.Control control) {
        double z = control.z() / 16.0;
        double y = control.y() / 16.0;
        var rows = theme().controls;
        if (rows != null) {
            boolean panel = "confirm".equals(control.kind()) || "lp_display".equals(control.kind());
            z = Math.copySign(panel ? rows.panelZ() : rows.phaseZ(), z);
            y = panel ? rows.panelY() : rows.phaseY();
        } else if (theme() != DuelFieldTheme.CLASSIC) {
            z -= Math.copySign(1.31, z);
            y += 0.29;
        }
        return new Vec3(control.x() / 16.0, y, z);
    }

    Vec3 controlWorldPosition(DuelFieldLayout.Control control) {
        return position().add(controlLocalPosition(control).yRot(-(float)Math.toRadians(getYRot())));
    }

    private DuelFieldLayout.Control findClickedControl(Player player) {
        Vec3 eye = player.getEyePosition();
        Vec3 look = player.getViewVector(1.0F);
        double bestT = Double.MAX_VALUE;
        DuelFieldLayout.Control best = null;

        for (DuelFieldLayout.Control control : DuelFieldLayout.controls()) {
            Vec3 center = controlWorldPosition(control);
            Vec3 normal = new Vec3(0.0, 1.0, 0.0);
            double denom = look.dot(normal);
            if (!(Math.abs(denom) < 1.0E-6)) {
                double t = center.subtract(eye).dot(normal) / denom;
                if (!(t < 0.0) && !(t > 48.0) && !(t >= bestT)) {
                    Vec3 hit = eye.add(look.scale(t)).subtract(center);
                    hit = hit.yRot((float)Math.toRadians(this.getYRot()));
                    if (Math.abs(hit.x) <= control.width() / 32.0 && Math.abs(hit.z) <= control.depth() / 32.0) {
                        bestT = t;
                        best = control;
                    }
                }
            }
        }

        return best;
    }

    public DuelFieldLayout.Control controlUnderCrosshair(Player player) {
        return this.findClickedControl(player);
    }

    public boolean placeCard(Player player, int code, ItemStack stack) {
        CardInfo info = CardRegistry.get(code).orElse(null);
        if (info == null) {
            player.displayClientMessage(Component.literal("Unknown card: " + code), false);
            return false;
        }

        DuelFieldLayout.Zone zone = this.findClickedZone(player);
        if (zone == null) {
            player.displayClientMessage(Component.literal("Point at a specific field zone and right-click again"), false);
            return false;
        }

        String side = this.sideOfPlayer(player);
        if (!this.isLegalZone(info, zone, side)) {
            player.displayClientMessage(Component.literal("That zone is not legal for " + info.name() + " under the current placement rules"), false);
            return false;
        }

        if (this.isZoneOccupied(zone.id())) {
            player.displayClientMessage(Component.literal("That zone is already occupied"), false);
            return false;
        }

        DuelCardEntity card = (DuelCardEntity)((EntityType)ModEntities.DUEL_CARD.get()).create(this.level());
        if (card == null) {
            return false;
        }

        Vec3 position = this.zoneWorldPosition(zone);
        card.setCardCode(code);
        card.setFaceUp(true);
        card.setDefense(false);
        card.setCardBack(cardBack("p2".equals(side) ? 1 : 0));
        card.setZoneId(zone.id());
        card.moveTo(position.x, position.y, position.z, this.getYRot(), 0.0F);
        this.level().addFreshEntity(card);
        if (!player.isCreative()) {
            stack.shrink(1);
        }

        player.displayClientMessage(Component.literal("Placed " + info.name() + " at " + zone.id()), false);
        return true;
    }

    private DuelFieldLayout.Zone findClickedZone(Player player) {
        return this.findZone(player, true);
    }

    private DuelFieldLayout.Zone findAnyZone(Player player) {
        return this.findZone(player, false);
    }

    private DuelFieldLayout.Zone findZone(Player player, boolean placementOnly) {
        Vec3 look = player.getViewVector(1.0F);
        if (Math.abs(look.y) < 1.0E-6) return null;
        float yaw = (float)Math.toRadians(this.getYRot());
        Vec3 eye = player.getEyePosition().subtract(position()).yRot(yaw);
        look = look.yRot(yaw);
        DuelFieldLayout.Zone best = null;
        double bestDistance = 225.0;
        for (var zone : DuelFieldLayout.zones()) {
            if (placementOnly && !isPlacementZone(zone.kind())) continue;
            double t = (zoneSurface(zone) + 0.02 - eye.y) / look.y;
            if (t < 0 || t > 48) continue;
            double dx = zone.x() - (eye.x + t * look.x) * 16;
            double dz = (zoneLocalZ(zone) - eye.z - t * look.z) * 16;
            double distance = dx * dx + dz * dz;
            if (distance < bestDistance || best == null && distance == bestDistance) {
                best = zone;
                bestDistance = distance;
            }
        }
        return best;
    }

    public String playerSide(Player player) {
        Vec3 local = new Vec3(player.getX() - this.getX(), 0.0, player.getZ() - this.getZ()).yRot((float)Math.toRadians(this.getYRot()));
        return local.z >= 0.0 ? "p1" : "p2";
    }

    private boolean isPlacementZone(String kind) {
        return "monster".equals(kind) || "spell_trap".equals(kind) || "extra_monster".equals(kind) || "field".equals(kind);
    }

    private boolean isLegalZone(CardInfo info, DuelFieldLayout.Zone zone, String side) {
        String owner = zone.owner();
        String kind = zone.kind();
        if (info.isMonster()) {
            boolean pendulum = (info.type() & 16777216) != 0;
            boolean link = (info.type() & 67108864) != 0;
            if (link) {
                return "extra_monster".equals(kind) && "shared".equals(owner);
            }

            if (pendulum) {
                if ("monster".equals(kind) && side.equals(owner)) {
                    return true;
                }

                if ("spell_trap".equals(kind) && side.equals(owner) && this.isPendulumZone(zone.id())) {
                    return true;
                }
            }

            return info.isExtraDeck() && "extra_monster".equals(kind) && "shared".equals(owner) ? true : "monster".equals(kind) && side.equals(owner);
        } else if (info.isSpell()) {
            return (info.type() & 524288) != 0 ? "field".equals(kind) && side.equals(owner) : "spell_trap".equals(kind) && side.equals(owner);
        } else {
            return !info.isTrap() ? false : "spell_trap".equals(kind) && side.equals(owner);
        }
    }

    private boolean isPendulumZone(String zoneId) {
        return zoneId.endsWith("_0") || zoneId.endsWith("_4");
    }

    private boolean isZoneOccupied(String zoneId) {
        for (DuelCardEntity card : this.level().getEntitiesOfClass(DuelCardEntity.class, this.getBoundingBox().inflate(2.0))) {
            if (zoneId.equals(card.getZoneId())) {
                return true;
            }
        }

        return false;
    }

    double zoneLocalZ(DuelFieldLayout.Zone zone) {
        double z = zone.z() / 16.0;
        if (theme().originalTrays() && ("deck".equals(zone.kind()) || "extra".equals(zone.kind()))) {
            return Math.copySign(5.33, z);
        }
        return z;
    }

    double zoneSurface(DuelFieldLayout.Zone zone) {
        if (theme().originalTrays()) return theme().surface(zone);
        if (!theme().raisedTrays) return 0;
        return switch (zone.kind()) {
            case "deck", "extra" -> 0.321;
            case "graveyard" -> 0.035;
            default -> 0;
        };
    }

    public Vec3 zoneWorldPosition(DuelFieldLayout.Zone zone) {
        Vec3 local = new Vec3(zone.x() / 16.0, 0.0, zoneLocalZ(zone)).yRot(-((float)Math.toRadians(this.getYRot())));
        return new Vec3(this.getX() + local.x, this.getY() + zoneSurface(zone) + 0.03, this.getZ() + local.z);
    }
}
