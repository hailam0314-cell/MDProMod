package net.mdpromod;

/**
 * 召唤演出的节奏，全部照 MDPro 实测值来（不是估的）：
 *
 * - **材料段 1.33 s**：MD 的 {@code TimeLineManager.SummonMaterial()} 里就是
 *   {@code WaitForSeconds(1.33f)}；音效 {@code SE_SUMMON_ADVANCE} 量出来 1.34 s，两边对得上。
 * - **召唤段**：取 MD 那套召唤音效的长度——MD 的音效和演出是同一段节奏，音效放完演出就收。
 *   数值是 {@code tmp\measure_summon_se.py} 读 {@code sound_assets_se_eff_summon_*.bundle}
 *   里的 wav 量出来的（全部 44100 Hz 立体声）：
 *   融合 3.31 / 同调 3.99 / 超量 3.87 / 连接 6.77 / 仪式 3.69 / 灵摆 2.42 秒。
 * - **落地 0.6 s**：MD 的 {@code DOMove(0.2 s)} + 翻转 {@code 0.4 s}。
 *
 * 三类时长加起来就是一次正规出场的时间：融合 1.33+3.31 = 4.64 s，同调 5.32 s，
 * 超量 5.20 s，连接 8.10 s，仪式 5.02 s，灵摆 3.75 s。
 */
final class SummonTiming {
    /** 素材展示 / 吸入段（MD 的固定值）。 */
    static final int MATERIAL_MS = 1330;
    /** 怪兽落位：位移 0.2 s + 翻转 0.4 s。 */
    static final int LAND_MS = 600;

    /** 0 融合 / 1 同调 / 2 超量 / 3 连接 / 4 仪式 / 5 灵摆 —— 和 ClientSummonEffects 的 kind 一致。 */
    private static final int[] SUMMON_MS = {3310, 3990, 3870, 6770, 3690, 2420};

    static int summonMs(int kind) {
        return kind >= 0 && kind < SUMMON_MS.length ? SUMMON_MS[kind] : 3200;
    }

    private SummonTiming() {
    }
}
