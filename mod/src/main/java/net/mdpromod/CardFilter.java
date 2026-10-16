package net.mdpromod;

/** 卡片池检索条件，主界面和第二层检索界面共用。 */
public final class CardFilter {
    public static final int SUB_QUICKPLAY = 0x10000;
    public static final int SUB_CONTINUOUS = 0x20000;
    public static final int SUB_EQUIP = 0x40000;
    public static final int SUB_FIELD = 0x80000;
    public static final int SUB_RITUAL = 0x80;
    public static final int SUB_COUNTER = 0x100000;
    public static final int SUB_MASK = SUB_QUICKPLAY | SUB_CONTINUOUS | SUB_EQUIP
            | SUB_FIELD | SUB_RITUAL | SUB_COUNTER;
    /** 数据里「通常魔法 / 通常陷阱」不带子类位，用自定义哨兵位表示。 */
    public static final int SPELL_NORMAL = 0x40000000;
    public static final int TRAP_NORMAL = 0x20000000;

    public boolean monster = true;
    public boolean spell = true;
    public boolean trap = true;
    public boolean extraOnly = false;
    public boolean limitedOnly = false;
    /** 属性位掩码，0 表示不限。 */
    public int attributeMask;
    /** 种族位掩码，0 表示不限。 */
    public int raceMask;
    /** 魔法种类位掩码（0x10 通常 / 0x20000 永续 / 0x40000 装备 / 0x10000 速攻 / 0x80 仪式 / 0x80000 场地）。 */
    public int spellSubMask;
    /** 陷阱种类位掩码（0x10 通常 / 0x20000 永续 / 0x100000 反击 / 0x40000 装备）。 */
    public int trapSubMask;
    public int levelMin;
    public int levelMax;
    public int atkMin;
    public int atkMax;
    public int defMin;
    public int defMax;
    /** 0 类型 / 1 等级 / 2 攻击 / 3 守备 / 4 卡号。 */
    public int sortMode;

    public void reset() {
        monster = true;
        spell = true;
        trap = true;
        extraOnly = false;
        limitedOnly = false;
        attributeMask = 0;
        raceMask = 0;
        spellSubMask = 0;
        trapSubMask = 0;
        levelMin = 0;
        levelMax = 0;
        atkMin = 0;
        atkMax = 0;
        defMin = 0;
        defMax = 0;
        sortMode = 0;
    }

    public boolean matches(CardInfo info) {
        if (extraOnly && !info.isExtraDeck()) return false;
        if (info.isExtraDeck() || info.isMonster()) {
            if (!monster) return false;
        } else if (info.isSpell()) {
            if (!spell) return false;
        } else if (info.isTrap()) {
            if (!trap) return false;
        }
        if (attributeMask != 0 && (info.attribute() & attributeMask) == 0) return false;
        if (raceMask != 0 && (info.race() & raceMask) == 0) return false;
        if (info.isSpell() && spellSubMask != 0) {
            int sub = info.type() & SUB_MASK;
            boolean normal = sub == 0 && (spellSubMask & SPELL_NORMAL) != 0;
            boolean typed = sub != 0 && (sub & spellSubMask) != 0;
            if (!normal && !typed) return false;
        }
        if (info.isTrap() && trapSubMask != 0) {
            int sub = info.type() & SUB_MASK;
            boolean normal = sub == 0 && (trapSubMask & TRAP_NORMAL) != 0;
            boolean typed = sub != 0 && (sub & trapSubMask) != 0;
            if (!normal && !typed) return false;
        }
        if (levelMin > 0 && info.level() < levelMin) return false;
        if (levelMax > 0 && info.level() > levelMax) return false;
        if (atkMin > 0 && info.atk() < atkMin) return false;
        if (atkMax > 0 && info.atk() > atkMax) return false;
        if (defMin > 0 && info.def() < defMin) return false;
        if (defMax > 0 && info.def() > defMax) return false;
        return !limitedOnly || DeckRules.limitOf(info.code()) < 3;
    }

    /** 统计生效中的条件条数，显示在主界面上。 */
    public int activeCount() {
        int count = 0;
        if (!monster) count++;
        if (!spell) count++;
        if (!trap) count++;
        if (extraOnly) count++;
        if (limitedOnly) count++;
        if (attributeMask != 0) count++;
        if (raceMask != 0) count++;
        if (spellSubMask != 0) count++;
        if (trapSubMask != 0) count++;
        if (levelMin > 0 || levelMax > 0) count++;
        if (atkMin > 0 || atkMax > 0) count++;
        if (defMin > 0 || defMax > 0) count++;
        return count;
    }

    public static String sortName(int mode) {
        return switch (mode) {
            case 1 -> "等级";
            case 2 -> "攻击";
            case 3 -> "守备";
            case 4 -> "卡号";
            default -> "类型";
        };
    }
}
