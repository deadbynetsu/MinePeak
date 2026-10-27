package dev.skycraft.client;

import dev.skycraft.SkyCraft;
import dev.skycraft.world.SkyDig;
import java.io.BufferedReader;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.NoSuchFileException;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Properties;
import net.fabricmc.fabric.api.client.screen.v1.ScreenEvents;
import net.fabricmc.fabric.api.client.screen.v1.Screens;
import net.fabricmc.fabric.api.client.screen.v1.ScreenEvents.AfterInit;
import net.fabricmc.loader.api.FabricLoader;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.client.gui.screens.PauseScreen;
import net.minecraft.network.chat.Component;

public final class DestructionToggle {
   private static final String KEY = "destruction";

   private DestructionToggle() {
   }

   private static Path file() {
      return FabricLoader.getInstance().getConfigDir().resolve("skycraft.properties");
   }

   public static void register() {
      load();
      ScreenEvents.AFTER_INIT.register((AfterInit)(var0, var1, var2, var3) -> {
         if (var1 instanceof PauseScreen var4 && var4.showsPauseMenu() && var0.player != null) {
            Screens.getWidgets(var1).add(button(var0));
         }
      });
   }

   private static Button button(Minecraft var0) {
      boolean var1 = var0.hasSingleplayerServer();
      Button var2 = Button.builder(label(), var0x -> {
            SkyDig.destruction = !SkyDig.destruction;
            var0x.setMessage(label());
            save();
            SkyCraft.LOG.info("SkyCraft: Skyrim destruction {}", SkyDig.destruction ? "on" : "off");
         })
         .bounds(4, 4, 150, 20)
         .tooltip(
            Tooltip.create(
               Component.literal(
                  var1
                     ? "Mining Skyrim's ground and rocks, and explosions, dig into Skyrim's world. Holes already dug stay either way."
                     : "In a friend's world, their setting decides."
               )
            )
         )
         .build();
      var2.active = var1;
      return var2;
   }

   private static Component label() {
      return Component.literal("Skyrim destruction: " + (SkyDig.destruction ? "On" : "Off"));
   }

   private static void load() {
      Properties var0 = new Properties();

      try (BufferedReader var1 = Files.newBufferedReader(file())) {
         var0.load(var1);
      } catch (NoSuchFileException var6) {
         return;
      } catch (IOException var7) {
         SkyCraft.LOG.warn("SkyCraft: couldn't read {}", file(), var7);
         return;
      }

      SkyDig.destruction = "true".equalsIgnoreCase(var0.getProperty("destruction", String.valueOf(SkyDig.destruction)).trim());
   }

   private static void save() {
      Path var0 = file();
      String var1 = "destruction=" + SkyDig.destruction;

      try {
         ArrayList var2 = Files.exists(var0) ? new ArrayList<>(Files.readAllLines(var0)) : new ArrayList<>(List.of("# SkyCraft"));
         boolean var3 = false;

         for (int var4 = 0; var4 < var2.size(); var4++) {
            if (((String)var2.get(var4)).trim().startsWith("destruction=")) {
               var2.set(var4, var1);
               var3 = true;
            }
         }

         if (!var3) {
            var2.add("# Mining and explosions dig into Skyrim's world (the pause menu's \"Skyrim destruction\" button).");
            var2.add(var1);
         }

         Files.createDirectories(var0.getParent());
         Files.write(var0, var2);
      } catch (IOException var5) {
         SkyCraft.LOG.warn("SkyCraft: couldn't save {}", var0, var5);
      }
   }
}
