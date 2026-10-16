package net.mdpromod;

import com.mojang.logging.LogUtils;

import java.nio.file.Path;

/** 手动模式跑决斗主机，检查发牌后手牌解析是否正确。 */
public final class HostHandSmoke {
    public static void main(String[] args) throws Exception {
        Path dotnet = Path.of("D:\\MDProMod\\tools\\dotnet\\dotnet.exe");
        Path dll = Path.of("D:\\MDProMod\\tools\\duel_host\\bin\\Release\\net8.0\\duel_host.dll");
        Path root = Path.of("D:\\MDProMod\\tools\\ygo_tcp_probe\\staging");
        DuelHostProcess host = new DuelHostProcess(LogUtils.getLogger(), dotnet, dll, root);
        host.start("starter.ydk", "starter.ydk", false);
        long deadline = System.currentTimeMillis() + 40_000;
        while (System.currentTimeMillis() < deadline && !host.isWaiting()) {
            Thread.sleep(100);
        }
        System.out.println("waiting=" + host.isWaiting() + " " + host.handSummary());
        int[] hand = host.handOf(0);
        System.out.println("p1 hand size=" + hand.length);
        StringBuilder names = new StringBuilder();
        for (int i = 0; i < Math.min(hand.length, 8); i++) {
            names.append(i > 0 ? ", " : "").append(hand[i]).append(':')
                    .append(CardRegistry.get(hand[i]).map(CardInfo::name).orElse("?"));
        }
        System.out.println("p1 hand = " + names);
        System.out.println("stats " + host.statsSummary());
        host.close();
    }
}
