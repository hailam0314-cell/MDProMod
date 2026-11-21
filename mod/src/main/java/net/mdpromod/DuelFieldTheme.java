package net.mdpromod;

import net.minecraft.nbt.CompoundTag;

enum DuelFieldTheme {
    CLASSIC("classic", 0.05, false),
    GEARTOWN("geartown", 0.05, false),
    VOLCANO("volcano", 1.145, true),
    RUINS("ruins", 2.86, true),
    CAPITAL("capital", 1.334, true),
    FIELD_001("001", 1.222, new ControlRows(5.22, 0.51, 3.5, 0.47), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_002("002", 1.192, new ControlRows(5.22, 0.63, 3.65, 0.59), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_003("003", 1.145, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_008("008", 1.607, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_009("009", 1.175, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.276, 0.276, 0.276, 0.276, 0.003, 0.003),
    FIELD_010("010", 1.175, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_011("011", 2.476, new ControlRows(5.22, 0.52, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_012("012", 1.175, new ControlRows(5.22, 2.27, 3.5, 0.65), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_013("013", 3.555, new ControlRows(5.22, 0.39, 3.5, 0.41), 0.314, 0.314, 0.314, 0.314, 0.003, 0.003),
    FIELD_014("014", 2.2, new ControlRows(5.22, 0.73, 3.65, 0.74), 0.326, 0.313, 0.323, 0.323, 0.003, 0.003),
    FIELD_015("015", 1.145, new ControlRows(5.22, 1.08, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_016("016", 1.175, new ControlRows(5.22, 1.13, 3.5, 1.45), 0.326, 0.326, 0.326, 0.326, 0.003, 0.003),
    FIELD_017("017", 1.18, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.286, 0.286, 0.286, 0.286, 0.003, 0.003),
    FIELD_018("018", 1.145, new ControlRows(5.22, 0.42, 3.5, 0.41), 0.321, 0.321, 0.321, 0.321, 0.003, 0.003),
    FIELD_019("019", 1.144, new ControlRows(5.85, 0.75, 3.9, 0.54), 0.322, 0.322, 0.322, 0.322, 0.003, 0.003),
    FIELD_020("020", 1.386, new ControlRows(5.22, 1.29, 3.5, 0.41), 0.289, 0.289, 0.289, 0.289, 0.003, 0.003),
    FIELD_021("021", 1.318, new ControlRows(5.22, 0.39, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_022("022", 3.167, new ControlRows(5.22, 0.59, 3.5, 0.38), 0.279, 0.281, 0.279, 0.281, 0.003, 0.003),
    FIELD_024("024", 1.16, new ControlRows(5.22, 0.86, 3.65, 0.43), 0.299, 0.299, 0.299, 0.299, 0.003, 0.003),
    FIELD_025("025", 1.145, new ControlRows(5.22, 0.39, 3.5, 0.36), 0.267, 0.267, 0.267, 0.267, 0.003, 0.003),
    FIELD_026("026", 4.28, new ControlRows(5.22, 0.4, 3.5, 0.41), 0.323, 0.323, 0.322, 0.322, 0.003, 0.003),
    FIELD_027("027", 1.47, new ControlRows(5.22, 0.48, 3.5, 1.45), 0.308, 0.308, 0.308, 0.308, 0.003, 0.003),
    FIELD_028("028", 1.291, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_029("029", 1.192, new ControlRows(5.22, 0.63, 3.65, 0.59), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_030("030", 1.192, new ControlRows(5.22, 0.63, 3.8, 0.84), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_031("031", 1.192, new ControlRows(5.85, 0.96, 3.5, 0.64), 0.323, 0.323, 0.323, 0.422, 0.003, 0.003),
    FIELD_034("034", 4.343, new ControlRows(5.22, 0.8, 3.5, 2.98), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_035("035", 1.175, new ControlRows(5.22, 2.31, 3.5, 0.71), 0.323, 0.323, 0.339, 0.339, 0.003, 0.003),
    FIELD_036("036", 4.13, new ControlRows(5.22, 2.86, 3.9, 0.51), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_038("038", 1.54, new ControlRows(5.22, 1.84, 3.65, 1.31), 0.209, 0.239, 0.107, 0.134, 0.003, 0.003),
    FIELD_039("039", 1.706, new ControlRows(5.22, 0.39, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_040("040", 1.254, new ControlRows(5.22, 0.69, 3.5, 0.41), 0.308, 0.308, 0.308, 0.308, 0.003, 0.003),
    FIELD_041("041", 1.22, new ControlRows(5.22, 0.51, 3.5, 0.36), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_042("042", 7.304, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_045("045", 3.555, new ControlRows(5.22, 0.39, 3.5, 0.41), 0.314, 0.314, 0.314, 0.314, 0.003, 0.003),
    FIELD_046("046", 1.177, new ControlRows(5.85, 0.91, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_048("048", 1.162, new ControlRows(5.22, 0.39, 3.9, 0.41), 0.301, 0.301, 0.299, 0.298, 0.003, 0.003),
    FIELD_049("049", 3.463, new ControlRows(5.22, 0.41, 3.5, 0.79), 0.309, 0.309, 0.309, 0.309, 0.003, 0.003),
    FIELD_050("050", 1.177, new ControlRows(5.85, 0.91, 3.5, 0.41), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003),
    FIELD_051("051", 3.51, new ControlRows(5.22, 0.69, 3.65, 0.37), 0.323, 0.323, 0.323, 0.32, 0.003, 0.003),
    FIELD_502("502", 1.145, new ControlRows(5.22, 0.92, 3.5, 0.4), 0.303, 0.303, 0.303, 0.303, 0.003, 0.003),
    FIELD_504("504", 1.145, new ControlRows(5.22, 0.92, 3.5, 0.4), 0.303, 0.303, 0.303, 0.303, 0.003, 0.003),
    FIELD_801("801", 1.175, new ControlRows(5.22, 0.41, 3.5, 0.41), 0.276, 0.276, 0.276, 0.276, 0.003, 0.003),
    FIELD_802("802", 1.222, new ControlRows(5.22, 0.51, 3.5, 0.47), 0.323, 0.323, 0.323, 0.323, 0.003, 0.003);

    final String id;

    final double placementHeight;
    final boolean raisedTrays;
    record ControlRows(double phaseZ, double phaseY, double panelZ, double panelY) {}
    final ControlRows controls;
    private final double[] pileHeights;

    DuelFieldTheme(String id, double placementHeight, ControlRows controls, double... pileHeights) {
        this.id = id;
        this.placementHeight = placementHeight;
        this.raisedTrays = true;
        this.controls = controls;
        this.pileHeights = pileHeights;
    }

    boolean originalTrays() {
        return pileHeights != null;
    }

    double surface(DuelFieldLayout.Zone zone) {
        boolean near = "p1".equals(zone.owner());
        return switch (zone.kind()) {
            case "deck" -> pileHeights[near ? 0 : 2];
            case "extra" -> pileHeights[near ? 1 : 3];
            case "graveyard" -> pileHeights[near ? 4 : 5];
            default -> 0.0;
        };
    }

    DuelFieldTheme(String id, double placementHeight, boolean raisedTrays) {
        this.placementHeight = placementHeight;
        this.raisedTrays = raisedTrays;
        this.controls = null;
        this.pileHeights = null;
        this.id = id;
    }

    static DuelFieldTheme byId(String id) {
        for (DuelFieldTheme theme : values()) {
            if (theme.id.equals(id)) return theme;
        }
        return CLASSIC;
    }

    static DuelFieldTheme read(CompoundTag tag) {
        if (tag == null) return CLASSIC;
        if (tag.contains("FieldTheme")) return byId(tag.getString("FieldTheme"));
        return tag.getBoolean("Geartown") ? GEARTOWN : CLASSIC;
    }

    void write(CompoundTag tag) {
        tag.remove("Geartown");
        if (this == CLASSIC) tag.remove("FieldTheme");
        else tag.putString("FieldTheme", id);
    }
}
