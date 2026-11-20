package net.mdpromod;

public record CardInfo(int code, String name, String desc, int ot, int alias, int type, int atk, int def, int level, int race, int attribute, int setcode) {
    public boolean isMonster() {
        return (this.type & 1) != 0;
    }

    public boolean isSpell() {
        return (this.type & 2) != 0;
    }

    public boolean isTrap() {
        return (this.type & 4) != 0;
    }

    public boolean isExtraDeck() {
        return (this.type & 75505728) != 0;
    }

    public boolean isToken() {
        return (this.type & 16384) != 0;
    }

    public String typeLabel() {
        if (this.isMonster()) {
            if ((this.type & 64) != 0) {
                return "融合怪兽";
            } else if ((this.type & 8192) != 0) {
                return "同步怪兽";
            } else if ((this.type & 8388608) != 0) {
                return "超量怪兽";
            } else if ((this.type & 67108864) != 0) {
                return "连接怪兽";
            } else {
                return (this.type & 128) != 0 ? "仪式怪兽" : "怪兽";
            }
        } else if (this.isSpell()) {
            return "魔法";
        } else {
            return this.isTrap() ? "陷阱" : "卡";
        }
    }

    public String attributeLabel() {
        int value = this.attribute;
        if ((value & 16) != 0) {
            return "光";
        } else if ((value & 32) != 0) {
            return "暗";
        } else if ((value & 1) != 0) {
            return "地";
        } else if ((value & 2) != 0) {
            return "水";
        } else if ((value & 4) != 0) {
            return "炎";
        } else if ((value & 8) != 0) {
            return "风";
        } else {
            return (value & 64) != 0 ? "神" : "";
        }
    }

    public String raceLabel() {
        return switch (this.race) {
            case 1 -> "战士族";
            case 2 -> "魔法师族";
            case 4 -> "天使族";
            case 8 -> "恶魔族";
            case 16 -> "不死族";
            case 32 -> "机械族";
            case 64 -> "水族";
            case 128 -> "炎族";
            case 256 -> "岩石族";
            case 512 -> "鸟兽族";
            case 1024 -> "植物族";
            case 2048 -> "昆虫族";
            case 4096 -> "雷族";
            case 8192 -> "龙族";
            case 16384 -> "兽族";
            case 32768 -> "兽战士族";
            case 65536 -> "恐龙族";
            case 131072 -> "鱼族";
            case 262144 -> "海龙族";
            case 524288 -> "爬虫类族";
            case 1048576 -> "念动力族";
            case 2097152 -> "幻神兽族";
            case 4194304 -> "创造神族";
            case 8388608 -> "幻龙族";
            case 16777216 -> "电子界族";
            case 33554432 -> "幻想魔族";
            default -> "";
        };
    }

    public String typeLine() {
        if (this.isMonster()) {
            String extra = this.isExtraDeck() ? "额外卡组" : "";
            return this.typeLabel() + " · " + this.attributeLabel() + " · " + this.level + " 星 · " + this.raceLabel() + (extra.isEmpty() ? "" : " · " + extra);
        } else {
            return this.subTypeLabel();
        }
    }

    public String subTypeLabel() {
        if (this.isSpell()) {
            if ((this.type & 65536) != 0) {
                return "速攻魔法";
            } else if ((this.type & 131072) != 0) {
                return "永续魔法";
            } else if ((this.type & 262144) != 0) {
                return "装备魔法";
            } else if ((this.type & 524288) != 0) {
                return "场地魔法";
            } else {
                return (this.type & 128) != 0 ? "仪式魔法" : "通常魔法";
            }
        } else if (this.isTrap()) {
            if ((this.type & 131072) != 0) {
                return "永续陷阱";
            } else if ((this.type & 1048576) != 0) {
                return "反击陷阱";
            } else {
                return (this.type & 262144) != 0 ? "装备陷阱" : "通常陷阱";
            }
        } else {
            return this.typeLabel();
        }
    }
}
