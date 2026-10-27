package dev.skycraft.coop;

import dev.skycraft.SkyCraft;
import java.io.IOException;
import java.lang.management.ManagementFactory;
import java.nio.file.Files;
import java.nio.file.Path;

public final class CoopMode {
   private static final long STARTED = ManagementFactory.getRuntimeMXBean().getStartTime();
   private static long lastCheck;
   private static boolean logged;

   private CoopMode() {
   }

   public static Path runtimeDir() {
      String var0 = System.getenv("LOCALAPPDATA");
      return var0 != null ? Path.of(var0, "PeakCraftCoop") : Path.of(System.getProperty("user.home"), "PeakCraftCoop");
   }

   public static boolean viewerRequested() {
      long var0 = System.currentTimeMillis();
      if (var0 - lastCheck < 1000L) {
         return false;
      }

      lastCheck = var0;
      Path var2 = runtimeDir().resolve("viewer.flag");
      boolean var3 = false;

      try {
         var3 = Files.exists(var2) && Files.getLastModifiedTime(var2).toMillis() >= STARTED - 2000L;
      } catch (IOException var5) {
         var3 = false;
      }

      if (var3 && !logged) {
         logged = true;
         SkyCraft.LOG.info("SkyCraft: coop: viewer mode, Minecraft not needed; quitting");
      }

      return var3;
   }
}
