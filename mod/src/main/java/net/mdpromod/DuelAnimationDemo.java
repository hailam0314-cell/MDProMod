package net.mdpromod;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.HexFormat;
import java.util.List;

final class DuelAnimationDemo {
    static List<DuelVisualState.Event> events() {
        DuelVisualState s = new DuelVisualState();
        s.initializeCounts(0, 40, 15);
        s.initializeCounts(1, 40, 15);
        draw(s, 0, 89631139, 14558127, 83764718, 55144522, 32807846);
        draw(s, 1, 89631139);
        move(s, 89631139, a(0, 2, 0, 10), a(0, 4, 2, 1), 16);
        annotate(s, 60, 89631139, a(0, 4, 2, 1));
        move(s, 89631139, a(1, 2, 0, 10), a(1, 4, 2, 1), 16);
        annotate(s, 60, 89631139, a(1, 4, 2, 1));
        move(s, 32807846, a(0, 2, 3, 10), a(0, 8, 1, 10), 16);
        annotate(s, 54, 32807846, a(0, 8, 1, 10));
        move(s, 29401950, a(0, 1, 34, 10), a(0, 2, 3, 10), 64);
        ByteBuffer confirm = buffer(11);
        confirm.put((byte)31).put((byte)0).put((byte)0).put((byte)1).putInt(29401950).put((byte)0).put((byte)2).put((byte)3);
        apply(s, confirm);
        ByteBuffer attack = buffer(9);
        attack.put((byte)110);
        put(attack, a(0, 4, 2, 1));
        put(attack, a(1, 4, 2, 1));
        apply(s, attack);
        move(s, 89631139, a(1, 4, 2, 1), a(1, 16, 0, 1), 65);
        activate(s, 89631139, a(1, 16, 0, 1));
        move(s, 89631139, a(1, 16, 0, 1), a(1, 4, 2, 1), 64);
        annotate(s, 62, 89631139, a(1, 4, 2, 1));
        activate(s, 14558127, a(0, 2, 0, 10));
        move(s, 14558127, a(0, 2, 0, 10), a(0, 16, 0, 1), 128);
        move(s, 55144522, a(0, 2, 1, 10), a(0, 4, 1, 1), 16);
        annotate(s, 60, 55144522, a(0, 4, 1, 1));
        move(s, 89631139, a(0, 4, 2, 1), a(0, 16, 1, 1), 524296);
        move(s, 55144522, a(0, 4, 1, 1), a(0, 16, 2, 1), 524296);
        move(s, 44508094, a(0, 64, 14, 8), a(0, 4, 5, 1), 16);
        annotate(s, 62, 44508094, a(0, 4, 5, 1));
        activate(s, 44508094, a(0, 4, 5, 1));
        move(s, 44508094, a(0, 4, 5, 1), a(0, 32, 0, 1), 64);
        activate(s, 44508094, a(0, 32, 0, 1));
        move(s, 44508094, a(0, 32, 0, 1), a(0, 64, 14, 8), 64);
        move(s, 89631139, a(1, 4, 2, 1), a(1, 2, 0, 10), 64);
        move(s, 83764718, a(0, 2, 0, 10), a(0, 1, 34, 10), 64);
        s.apply("ShuffleDeck", "2000");
        move(s, 29401950, a(0, 1, 34, 10), a(0, 32, 1, 10), 64);
        return s.drainEvents();
    }

    static List<DuelVisualState.Event> pendulum() {
        DuelVisualState s = new DuelVisualState();
        s.initializeCounts(0, 40, 15);
        draw(s, 0, 15146890, 51531505, 46986414, 91152256);
        s.apply("demo", "290400");
        move(s, 15146890, a(0, 2, 0, 10), a(0, 8, 0, 5), 0x02000400);
        move(s, 51531505, a(0, 2, 0, 10), a(0, 8, 4, 5), 0x02000400);
        s.apply("demo", "530100080005");
        s.apply("demo", "530100080405");
        move(s, 46986414, a(0, 2, 0, 10), a(0, 4, 1, 1), 0x800);
        annotate(s, 62, 46986414, a(0, 4, 1, 1));
        move(s, 91152256, a(0, 2, 0, 10), a(0, 4, 3, 4), 0x800);
        annotate(s, 62, 91152256, a(0, 4, 3, 4));
        s.apply("demo", "3F");
        return s.drainEvents();
    }

    private static DuelVisualState.Address a(int side, int loc, int seq, int pos) {
        return new DuelVisualState.Address(side, loc, seq, pos);
    }

    private static ByteBuffer buffer(int size) {
        return ByteBuffer.allocate(size).order(ByteOrder.LITTLE_ENDIAN);
    }

    private static void put(ByteBuffer b, DuelVisualState.Address a) {
        b.put((byte)a.controller()).put((byte)a.location()).put((byte)a.sequence()).put((byte)a.position());
    }

    private static void apply(DuelVisualState s, ByteBuffer b) {
        s.apply("demo", HexFormat.of().formatHex(b.array()));
    }

    private static void draw(DuelVisualState s, int side, int... codes) {
        ByteBuffer b = buffer(3 + codes.length * 4);
        b.put((byte)90).put((byte)side).put((byte)codes.length);

        for (int code : codes) {
            b.putInt(code);
        }

        apply(s, b);
    }

    private static void move(DuelVisualState s, int code, DuelVisualState.Address from, DuelVisualState.Address to, int reason) {
        ByteBuffer b = buffer(17);
        b.put((byte)50).putInt(code);
        put(b, from);
        put(b, to);
        b.putInt(reason);
        apply(s, b);
    }

    private static void annotate(DuelVisualState s, int function, int code, DuelVisualState.Address to) {
        ByteBuffer b = buffer(9);
        b.put((byte)function).putInt(code);
        put(b, to);
        apply(s, b);
    }

    private static void activate(DuelVisualState s, int code, DuelVisualState.Address at) {
        ByteBuffer b = buffer(17);
        b.put((byte)70).putInt(code);
        put(b, at);
        b.put((byte)at.controller()).put((byte)at.location()).put((byte)at.sequence()).putInt(0).put((byte)1);
        apply(s, b);
    }
}
