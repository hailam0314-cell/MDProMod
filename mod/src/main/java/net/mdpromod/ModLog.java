package net.mdpromod;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

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
            Logger logger = LoggerFactory.getLogger("mdpromod");
            if (warning) {
                logger.warn(message, args);
            } else {
                logger.info(message, args);
            }
        } catch (Throwable var4) {
        }
    }
}
