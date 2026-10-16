package net.mdpromod;

/**
 * 模组的日志出口：游戏里走 slf4j（和 Minecraft 的日志配置一致）。
 *
 * <p>离线测试的 classpath 上没有 slf4j，这时候静默跳过——"打日志"不该把测试
 * 或者后台线程搞崩，所以这里不用静态字段持有 logger（那会在类加载时就炸）。
 */
final class ModLog {
    private static final String NAME = "mdpromod";

    private ModLog() {
    }

    static void info(String message, Object... args) {
        write(false, message, args);
    }

    static void warn(String message, Object... args) {
        write(true, message, args);
    }

    private static void write(boolean warning, String message, Object... args) {
        try {
            org.slf4j.Logger logger = org.slf4j.LoggerFactory.getLogger(NAME);
            if (warning) logger.warn(message, args);
            else logger.info(message, args);
        } catch (Throwable ignored) {
            // 离线测试：没有 slf4j 就直接不记
        }
    }
}
