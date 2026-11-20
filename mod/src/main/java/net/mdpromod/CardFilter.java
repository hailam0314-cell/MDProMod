package net.mdpromod;

public final class CardFilter {
    public static final int MONSTER_NORMAL = 16;
    public static final int MONSTER_EFFECT = 32;
    public static final int MONSTER_FUSION = 64;
    public static final int MONSTER_RITUAL = 128;
    public static final int MONSTER_SPIRIT = 512;
    public static final int MONSTER_UNION = 1024;
    public static final int MONSTER_DUAL = 2048;
    public static final int MONSTER_TUNER = 4096;
    public static final int MONSTER_SYNCHRO = 8192;
    public static final int MONSTER_FLIP = 2097152;
    public static final int MONSTER_XYZ = 8388608;
    public static final int MONSTER_PENDULUM = 16777216;
    public static final int MONSTER_LINK = 67108864;
    public static final int SUB_QUICKPLAY = 65536;
    public static final int SUB_CONTINUOUS = 131072;
    public static final int SUB_EQUIP = 262144;
    public static final int SUB_FIELD = 524288;
    public static final int SUB_RITUAL = 128;
    public static final int SUB_COUNTER = 1048576;
    public static final int SUB_MASK = 2031744;
    public static final int SPELL_NORMAL = 1073741824;
    public static final int TRAP_NORMAL = 536870912;
    public boolean monster = true;
    public boolean spell = true;
    public boolean trap = true;
    public boolean extraOnly = false;
    public boolean limitedOnly = false;
    public int attributeMask;
    public int raceMask;
    public int monsterTypeMask;
    public int spellSubMask;
    public int trapSubMask;
    public int levelMin;
    public int levelMax;
    public int atkMin;
    public int atkMax;
    public int defMin;
    public int defMax;
    public int sortMode;

    public void reset() {
        this.monster = true;
        this.spell = true;
        this.trap = true;
        this.extraOnly = false;
        this.limitedOnly = false;
        this.attributeMask = 0;
        this.raceMask = 0;
        this.monsterTypeMask = 0;
        this.spellSubMask = 0;
        this.trapSubMask = 0;
        this.levelMin = 0;
        this.levelMax = 0;
        this.atkMin = 0;
        this.atkMax = 0;
        this.defMin = 0;
        this.defMax = 0;
        this.sortMode = 0;
    }

    public boolean matches(CardInfo info) {
        if (this.extraOnly && !info.isExtraDeck()) {
            return false;
        }

        if (!info.isExtraDeck() && !info.isMonster()) {
            if (info.isSpell()) {
                if (!this.spell) {
                    return false;
                }
            } else if (info.isTrap() && !this.trap) {
                return false;
            }
        } else if (!this.monster) {
            return false;
        }

        if (this.attributeMask != 0 && (info.attribute() & this.attributeMask) == 0) {
            return false;
        }

        if (this.raceMask != 0 && (info.race() & this.raceMask) == 0) {
            return false;
        }

        if (this.monsterTypeMask == 0 || info.isMonster() && (info.type() & this.monsterTypeMask) != 0) {
            if (info.isSpell() && this.spellSubMask != 0) {
                int sub = info.type() & 2031744;
                boolean normal = sub == 0 && (this.spellSubMask & 1073741824) != 0;
                boolean typed = sub != 0 && (sub & this.spellSubMask) != 0;
                if (!normal && !typed) {
                    return false;
                }
            }

            if (info.isTrap() && this.trapSubMask != 0) {
                int sub = info.type() & 2031744;
                boolean normal = sub == 0 && (this.trapSubMask & 536870912) != 0;
                boolean typed = sub != 0 && (sub & this.trapSubMask) != 0;
                if (!normal && !typed) {
                    return false;
                }
            }

            if (this.levelMin > 0 && info.level() < this.levelMin) {
                return false;
            } else if (this.levelMax > 0 && info.level() > this.levelMax) {
                return false;
            } else if (this.atkMin > 0 && info.atk() < this.atkMin) {
                return false;
            } else if (this.atkMax > 0 && info.atk() > this.atkMax) {
                return false;
            } else if (this.defMin > 0 && info.def() < this.defMin) {
                return false;
            } else {
                return this.defMax > 0 && info.def() > this.defMax ? false : !this.limitedOnly || DeckRules.limitOf(info.code()) < 3;
            }
        } else {
            return false;
        }
    }

    public int activeCount() {
        int count = 0;
        if (!this.monster) {
            count++;
        }

        if (!this.spell) {
            count++;
        }

        if (!this.trap) {
            count++;
        }

        if (this.extraOnly) {
            count++;
        }

        if (this.limitedOnly) {
            count++;
        }

        if (this.attributeMask != 0) {
            count++;
        }

        if (this.raceMask != 0) {
            count++;
        }

        if (this.monsterTypeMask != 0) {
            count++;
        }

        if (this.spellSubMask != 0) {
            count++;
        }

        if (this.trapSubMask != 0) {
            count++;
        }

        if (this.levelMin > 0 || this.levelMax > 0) {
            count++;
        }

        if (this.atkMin > 0 || this.atkMax > 0) {
            count++;
        }

        if (this.defMin > 0 || this.defMax > 0) {
            count++;
        }

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
