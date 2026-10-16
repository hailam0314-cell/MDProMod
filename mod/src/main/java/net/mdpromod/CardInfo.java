package net.mdpromod;

public record CardInfo(
        int code,
        String name,
        String desc,
        int ot,
        int alias,
        int type,
        int atk,
        int def,
        int level,
        int race,
        int attribute
) {
    public boolean isMonster() {
        return (type & 0x1) != 0;
    }

    public boolean isSpell() {
        return (type & 0x2) != 0;
    }

    public boolean isTrap() {
        return (type & 0x4) != 0;
    }

    public boolean isExtraDeck() {
        return (type & (0x40 | 0x2000 | 0x800000 | 0x4000000)) != 0;
    }

    public String typeLabel() {
        if (isMonster()) {
            if ((type & 0x40) != 0) return "融合怪兽";
            if ((type & 0x2000) != 0) return "同步怪兽";
            if ((type & 0x800000) != 0) return "超量怪兽";
            if ((type & 0x4000000) != 0) return "连接怪兽";
            if ((type & 0x80) != 0) return "仪式怪兽";
            return "怪兽";
        }
        if (isSpell()) return "魔法";
        if (isTrap()) return "陷阱";
        return "卡";
    }

    /** 属性位（ygopro：0x10 光 / 0x20 暗 / 0x01 地 / 0x02 水 / 0x04 炎 / 0x08 风 / 0x40 神）。 */
    public String attributeLabel() {
        int value = attribute;
        if ((value & 0x10) != 0) return "光";
        if ((value & 0x20) != 0) return "暗";
        if ((value & 0x01) != 0) return "地";
        if ((value & 0x02) != 0) return "水";
        if ((value & 0x04) != 0) return "炎";
        if ((value & 0x08) != 0) return "风";
        if ((value & 0x40) != 0) return "神";
        return "";
    }

    /** 种族位；找不到时返回空串。 */
    public String raceLabel() {
        return switch (race) {
            case 0x1 -> "战士族";
            case 0x2 -> "魔法师族";
            case 0x4 -> "天使族";
            case 0x8 -> "恶魔族";
            case 0x10 -> "不死族";
            case 0x20 -> "机械族";
            case 0x40 -> "水族";
            case 0x80 -> "炎族";
            case 0x100 -> "岩石族";
            case 0x200 -> "鸟兽族";
            case 0x400 -> "植物族";
            case 0x800 -> "昆虫族";
            case 0x1000 -> "雷族";
            case 0x2000 -> "龙族";
            case 0x4000 -> "兽族";
            case 0x8000 -> "兽战士族";
            case 0x10000 -> "恐龙族";
            case 0x20000 -> "鱼族";
            case 0x40000 -> "海龙族";
            case 0x80000 -> "爬虫类族";
            case 0x100000 -> "念动力族";
            case 0x200000 -> "幻神兽族";
            case 0x400000 -> "创造神族";
            case 0x800000 -> "幻龙族";
            case 0x1000000 -> "电子界族";
            case 0x2000000 -> "幻想魔族";
            default -> "";
        };
    }

    public String typeLine() {
        if (isMonster()) {
            String extra = isExtraDeck() ? "额外卡组" : "";
            return typeLabel() + " · " + attributeLabel() + " · " + level + " 星 · "
                    + raceLabel() + (extra.isEmpty() ? "" : " · " + extra);
        }
        return subTypeLabel();
    }

    /** 魔法 / 陷阱的子类名，例如「速攻魔法」「通常陷阱」。 */
    public String subTypeLabel() {
        if (isSpell()) {
            if ((type & 0x10000) != 0) return "速攻魔法";
            if ((type & 0x20000) != 0) return "永续魔法";
            if ((type & 0x40000) != 0) return "装备魔法";
            if ((type & 0x80000) != 0) return "场地魔法";
            if ((type & 0x80) != 0) return "仪式魔法";
            return "通常魔法";
        }
        if (isTrap()) {
            if ((type & 0x20000) != 0) return "永续陷阱";
            if ((type & 0x100000) != 0) return "反击陷阱";
            if ((type & 0x40000) != 0) return "装备陷阱";
            return "通常陷阱";
        }
        return typeLabel();
    }
}
