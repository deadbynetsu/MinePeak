package dev.skycraft.client;

import dev.skycraft.SkyCraft;
import java.io.BufferedReader;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Properties;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.ConnectScreen;
import net.minecraft.client.gui.screens.DisconnectedScreen;
import net.minecraft.client.gui.screens.TitleScreen;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.multiplayer.ServerData;
import net.minecraft.client.multiplayer.ServerData.Type;
import net.minecraft.client.multiplayer.resolver.ServerAddress;
import net.minecraft.client.server.IntegratedServer;
import net.minecraft.core.registries.Registries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.Identifier;
import net.minecraft.resources.ResourceKey;
import net.minecraft.world.Difficulty;
import net.minecraft.world.level.GameType;
import net.minecraft.world.level.LevelSettings;
import net.minecraft.world.level.WorldDataConfiguration;
import net.minecraft.world.level.LevelSettings.DifficultySettings;
import net.minecraft.world.level.levelgen.WorldOptions;
import net.minecraft.world.level.levelgen.presets.WorldPreset;
import org.jspecify.annotations.Nullable;

public final class MirrorWorld {
   private static final ResourceKey<WorldPreset> PRESET = ResourceKey.create(Registries.WORLD_PRESET, Identifier.fromNamespaceAndPath("skycraft", "mirror"));
   private static boolean attempted;
   private static long lastLog;
   private static @Nullable String sessionJoin;
   private static @Nullable String pendingNote;
   private static final int MIRROR_MIN_Y = -1024;
   private static int wrongWorldLeaves;

   private MirrorWorld() {
   }

   private static @Nullable String joinAddress(Minecraft var0) {
      Path var1 = var0.gameDirectory.toPath().resolve("config").resolve("skycraft.properties");
      Properties var2 = new Properties();

      try {
         if (!Files.exists(var1)) {
            Files.createDirectories(var1.getParent());
            Files.writeString(
               var1,
               "# SkyCraft\n# To play in a friend's world instead of your own: put their address after join=\n# (the link e4mc shows them when they open their world to LAN), then restart Minecraft.\njoin=\n"
            );
         }

         try (BufferedReader var3 = Files.newBufferedReader(var1)) {
            var2.load(var3);
         }
      } catch (IOException var8) {
         SkyCraft.LOG.warn("SkyCraft: couldn't read {}", var1, var8);
         return null;
      }

      String var9 = var2.getProperty("join", "").trim();
      return var9.isEmpty() ? null : var9;
   }

   public static void joinFriend(Minecraft var0, String var1) {
      String var2 = var1.trim().replaceFirst("^[A-Za-z]+://", "").replaceAll("/+$", "");
      if (!var2.isEmpty()) {
         SkyCraft.LOG.info("SkyCraft: /join {}", var2);
         sessionJoin = var2;
         leaveWorld(var0);
      }
   }

   public static @Nullable String friendAddress(Minecraft var0) {
      if (sessionJoin != null) {
         return sessionJoin;
      }

      ServerData var1 = var0.isLocalServer() ? null : var0.getCurrentServer();
      return var1 != null ? var1.ip : null;
   }

   public static void leaveFriend(Minecraft var0) {
      if (sessionJoin == null) {
         var0.gui.hud.getChat().addClientSystemMessage(Component.literal("You're already in your own world."));
      } else {
         SkyCraft.LOG.info("SkyCraft: /leave {}", sessionJoin);
         sessionJoin = null;
         pendingNote = "Back in your own world.";
         leaveWorld(var0);
      }
   }

   private static void leaveWorld(Minecraft var0) {
      attempted = false;
      var0.disconnectFromWorld(ClientLevel.DEFAULT_QUIT_MESSAGE);
      var0.gui.setScreen(new TitleScreen());
   }

   public static void tick(Minecraft var0) {
      if (pendingNote != null && var0.player != null) {
         var0.gui.hud.getChat().addClientSystemMessage(Component.literal(pendingNote));
         pendingNote = null;
      }
   }

   private static boolean leaveWrongWorld(Minecraft var0) {
      if (var0.level != null && sessionJoin == null && var0.isLocalServer()) {
         if (var0.level.getMinY() <= -1024) {
            wrongWorldLeaves = 0;
            return false;
         }

         IntegratedServer var1 = var0.getSingleplayerServer();
         String var2 = var1 != null ? var1.getWorldData().getLevelName() : "?";
         if (wrongWorldLeaves >= 3) {
            if (System.currentTimeMillis() - lastLog > 10000L) {
               lastLog = System.currentTimeMillis();
               SkyCraft.LOG
                  .error(
                     "SkyCraft: still in {} (floor y {}) after 3 tries to open the mirror world; PEAK's ground is below its void", var2, var0.level.getMinY()
                  );
            }

            return false;
         } else {
            wrongWorldLeaves++;
            SkyCraft.LOG
               .info(
                  "SkyCraft: leaving {}: not the mirror world (its floor is y {}, PEAK's ground is near y -511); opening the mirror world",
                  var2,
                  var0.level.getMinY()
               );
            pendingNote = "PeakCraft needs its own world; switched from " + var2 + " to the PeakCraft world.";
            leaveWorld(var0);
            return true;
         }
      } else {
         return false;
      }
   }

   public static void openWhenReady(Minecraft var0) {
      if (var0.gui.screen() instanceof DisconnectedScreen && var0.level == null) {
         pendingNote = sessionJoin != null
            ? "Couldn't stay in " + sessionJoin + " (check the link, and that your friend's world is still open to LAN). You're back in your own world."
            : "Disconnected. You're back in your own world.";
         SkyCraft.LOG.info("SkyCraft: disconnected; back to the mirror world");
         sessionJoin = null;
         attempted = false;
         var0.gui.setScreen(new TitleScreen());
      } else if (!leaveWrongWorld(var0)) {
         if (attempted && var0.level == null && var0.gui.screen() != null && System.currentTimeMillis() - lastLog > 5000L) {
            lastLog = System.currentTimeMillis();
            SkyCraft.LOG.info("SkyCraft: still not in the mirror world; current screen {}", var0.gui.screen().getClass().getName());
         }

         if (!attempted && var0.level == null && var0.gui.overlay() == null) {
            if (!(var0.gui.screen() instanceof TitleScreen)) {
               if (var0.gui.screen() != null && System.currentTimeMillis() - lastLog > 5000L) {
                  lastLog = System.currentTimeMillis();
                  SkyCraft.LOG.info("SkyCraft: waiting on screen {} before opening the mirror world", var0.gui.screen().getClass().getName());
               }

               if (var0.gui.screen() == null || var0.gui.screen().getClass().getName().contains("Onboarding")) {
                  var0.gui.setScreen(new TitleScreen());
               }
            } else {
               TitleScreen var1 = (TitleScreen)var0.gui.screen();
               attempted = true;
               String var2 = sessionJoin != null ? sessionJoin : joinAddress(var0);
               if (var2 != null) {
                  SkyCraft.LOG.info("SkyCraft: joining {}", var2);
                  pendingNote = "Joined " + var2 + ". Type /leave to go back to your own world.";
                  ConnectScreen.startConnecting(var1, var0, ServerAddress.parseString(var2), new ServerData("SkyCraft", var2, Type.OTHER), false, null);
               } else if (var0.getLevelSource().levelExists("SkyCraft")) {
                  SkyCraft.LOG.info("SkyCraft: opening mirror world");
                  var0.createWorldOpenFlows().openWorld("SkyCraft", () -> var0.gui.setScreen(var1));
               } else {
                  SkyCraft.LOG.info("SkyCraft: creating mirror world");
                  LevelSettings var3 = new LevelSettings(
                     "SkyCraft", GameType.CREATIVE, new DifficultySettings(Difficulty.NORMAL, false, false), true, WorldDataConfiguration.DEFAULT
                  );
                  var0.createWorldOpenFlows()
                     .createFreshLevel(
                        "SkyCraft",
                        var3,
                        new WorldOptions(0L, false, false),
                        var0x -> var0x.lookupOrThrow(Registries.WORLD_PRESET).getOrThrow(PRESET).value().createWorldDimensions(),
                        var1
                     );
               }
            }
         }
      }
   }
}
