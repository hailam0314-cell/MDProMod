package net.mdpromod;

import java.util.HexFormat;

/**
 * 「连锁中发动 / 被点名」卡面标记的回归测试：这两个数组就是客户端要画的东西，
 * 错一个字节就会在场上留下收不掉的标记，所以单独测。
 */
public final class DuelCardMarksTest {
    private static int assertions;
    /** 自己怪兽区3 / 自己魔陷区2 / 对方怪兽区4 / 自己手牌1 / 自己墓地1。 */
    private static final String OWN_MONSTER = "00040201";
    private static final String OWN_SPELL = "00080101";
    private static final String RIVAL_MONSTER = "01040301";
    private static final String OWN_HAND = "00020010";
    /** 注意位置字节是十六进制：墓地 = 0x10。 */
    private static final String OWN_GRAVE = "00100001";

    public static void main(String[] args) {
        chainLinksOnTheField();
        activationFromHandIsNotMarked();
        leavingTheFieldDropsTheMark();
        targetsFollowTheChain();
        multipleTargetsAreDeduplicated();
        targetsOutsideAChainAreIgnored();
        oneCardCanCarryBothMarks();
        resetClearsMarks();
        addressLookupAndLabels();
        System.out.println("DuelCardMarks passed " + assertions + " assertions");
    }

    /** 选卡界面是按地址查标记的（同一批标记既发实体 id 也发地址）。 */
    private static void addressLookupAndLabels() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        place(state, 202, RIVAL_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("BecomeTarget", becomeTarget(RIVAL_MONSTER));
        int[] chain = state.chainMarkAddresses(), target = state.targetMarkAddresses();
        check(DuelMarks.at(chain, target, pack(0, 4, 2)) == DuelMarks.CHAIN, "发动的卡按地址也能查到");
        check(DuelMarks.at(chain, target, pack(1, 4, 3)) == DuelMarks.TARGET, "被指定的卡按地址也能查到");
        check(DuelMarks.at(chain, target, pack(0, 8, 1)) == 0, "没标记的格子返回 0");
        check(DuelMarks.at(chain, target, pack(0, 4, 2) | 0x1000000) == 0, "位置字节不同不算同一张");
        check(DuelMarks.at(null, null, pack(0, 4, 2)) == 0, "空数组不炸");
        check(DuelMarks.label(DuelMarks.CHAIN | DuelMarks.TARGET).equals("连锁中发动 · 被指定为对象"),
                "两种标记都在时的说明");
        check(DuelMarks.label(DuelMarks.CHAIN).equals("连锁中发动"), "连锁标记的说明");
        check(DuelMarks.label(DuelMarks.TARGET).equals("被指定为对象"), "被指定标记的说明");
        check(DuelMarks.label(0).isEmpty(), "没有标记时没有说明");
    }

    private static void chainLinksOnTheField() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        int[] marks = state.chainMarkAddresses();
        check(marks.length == 1 && marks[0] == pack(0, 4, 2), "场上的发动卡被标记：" + show(marks));
        check(state.targetMarkAddresses().length == 0, "没有点名时没有蓝标");
        check(state.malformedMessages() == 0, "连锁报文解析正常");
    }

    private static void activationFromHandIsNotMarked() {
        DuelVisualState state = new DuelVisualState();
        place(state, 303, OWN_HAND);
        state.apply("Chaining", chaining(303, OWN_HAND, 1));
        check(state.chainMarkAddresses().length == 0, "手牌发动的效果没有卡面可标");
    }

    private static void leavingTheFieldDropsTheMark() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("Move", move(101, OWN_MONSTER, OWN_GRAVE));
        check(state.chainMarkAddresses().length == 0, "发动后送墓的卡不再标记");
    }

    private static void targetsFollowTheChain() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        place(state, 202, RIVAL_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("BecomeTarget", becomeTarget(RIVAL_MONSTER));
        int[] targets = state.targetMarkAddresses();
        check(targets.length == 1 && targets[0] == pack(1, 4, 3), "被点名的是对方的怪兽：" + show(targets));
        check(state.chainMarkAddresses().length == 1, "发动的卡同时在标记里");
        // 点名之后卡被移动：标记跟着新位置，而不是留在旧格子上
        state.apply("Move", move(202, RIVAL_MONSTER, OWN_GRAVE));
        check(state.targetMarkAddresses().length == 0, "离场的被点名卡不再标记");
        state.apply("ChainEnd", "4A");
        check(state.chainMarkAddresses().length == 0 && state.targetMarkAddresses().length == 0,
                "连锁结束两个标记都清空");
    }

    private static void multipleTargetsAreDeduplicated() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        place(state, 202, RIVAL_MONSTER);
        place(state, 303, OWN_SPELL);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("BecomeTarget", becomeTarget(RIVAL_MONSTER, OWN_SPELL, RIVAL_MONSTER));
        int[] targets = state.targetMarkAddresses();
        check(targets.length == 2, "一条报文点名多张卡都记账，重复的只留一份：" + show(targets));
        check(targets[0] == pack(1, 4, 3) && targets[1] == pack(0, 8, 1), "顺序稳定（去重后再发包不抖）");
    }

    private static void targetsOutsideAChainAreIgnored() {
        DuelVisualState state = new DuelVisualState();
        place(state, 202, RIVAL_MONSTER);
        state.apply("BecomeTarget", becomeTarget(RIVAL_MONSTER));
        check(state.targetMarkAddresses().length == 0, "不在连锁里的点名不记账，免得标记收不掉");
    }

    private static void oneCardCanCarryBothMarks() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("BecomeTarget", becomeTarget(OWN_MONSTER));
        check(state.chainMarkAddresses().length == 1 && state.targetMarkAddresses().length == 1,
                "同一张卡既发动又被指定时两边都要有");
    }

    private static void resetClearsMarks() {
        DuelVisualState state = new DuelVisualState();
        place(state, 101, OWN_MONSTER);
        state.apply("Chaining", chaining(101, OWN_MONSTER, 1));
        state.apply("BecomeTarget", becomeTarget(OWN_MONSTER));
        state.reset();
        check(state.chainMarkAddresses().length == 0 && state.targetMarkAddresses().length == 0,
                "重开一局不能带着上一局的标记");
    }

    private static void place(DuelVisualState state, int code, String address) {
        state.apply("Move", move(code, "00000000", address));
        state.drainEvents();
    }

    private static String show(int[] marks) {
        StringBuilder text = new StringBuilder();
        for (int mark : marks) {
            if (text.length() > 0) text.append(',');
            text.append(packName(mark));
        }
        return text.toString();
    }

    private static String packName(int packed) {
        return ((packed >> 16) & 1) + "/" + ((packed >> 8) & 0xFF) + "/" + (packed & 0xFF);
    }

    private static int pack(int controller, int location, int sequence) {
        return (controller << 16) | (location << 8) | sequence;
    }

    private static void check(boolean condition, String message) {
        assertions++;
        if (!condition) throw new AssertionError(message);
    }

    private static String chaining(int code, String address, int index) {
        return "46" + le32(code) + address + "000402" + le32(1234) + String.format("%02X", index);
    }

    private static String becomeTarget(String... addresses) {
        StringBuilder message = new StringBuilder(String.format("53%02X", addresses.length));
        for (String address : addresses) message.append(address);
        return message.toString();
    }

    private static String move(int code, String from, String to) {
        return "32" + le32(code) + from + to + le32(0);
    }

    private static String le32(int value) {
        return HexFormat.of().formatHex(new byte[]{(byte) value, (byte) (value >> 8),
                (byte) (value >> 16), (byte) (value >> 24)});
    }
}
