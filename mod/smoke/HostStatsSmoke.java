package net.mdpromod;

import com.mojang.logging.LogUtils;

import java.nio.file.Path;

/** 让决斗主机跑一局自动对局，检查 Draw / Move 消息解析出的实时统计。 */
public final class HostStatsSmoke {
    public static void main(String[] args) throws Exception {
        Path dotnet = Path.of("D:\\MDProMod\\tools\\dotnet\\dotnet.exe");
        Path dll = Path.of("D:\\MDProMod\\tools\\duel_host\\bin\\Release\\net8.0\\duel_host.dll");
        Path root = Path.of("D:\\MDProMod\\tools\\ygo_tcp_probe\\staging");
        DuelHostProcess host = new DuelHostProcess(LogUtils.getLogger(), dotnet, dll, root);
        long start = System.currentTimeMillis();
        host.start("starter.ydk", "starter.ydk", true);
        String last = "";
        long deadline = start + 120_000;
        while (System.currentTimeMillis() < deadline) {
            Thread.sleep(1000);
            String stats = host.statsSummary();
            if (!stats.equals(last)) {
                System.out.println("t=" + (System.currentTimeMillis() - start) + "ms " + stats);
                last = stats;
            }
            if (host.isEnded()) break;
        }
        System.out.println("ended=" + host.isEnded() + " winner=" + host.getWinner());
        System.out.println("final " + host.statsSummary());
        host.close();
    }
}
