package dev.skycraft.client;

import dev.skycraft.SkyCraft;
import dev.skycraft.combat.SkyCombat;
import dev.skycraft.coop.CoopBlocks;
import dev.skycraft.link.SkyLink;
import dev.skycraft.net.SkyNet;
import java.util.UUID;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayNetworking;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.PauseScreen;
import net.minecraft.client.gui.screens.options.OptionsScreen;
import net.minecraft.client.input.KeyEvent;
import net.minecraft.client.input.MouseButtonInfo;
import net.minecraft.client.server.IntegratedServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.phys.EntityHitResult;
import net.minecraft.world.phys.HitResult;
import org.lwjgl.sdl.SDLKeyboard;

public final class InputBridge {
   private static final boolean[] KEYS = new boolean[512];
   private static final boolean[] BUTTONS = new boolean[8];
   private static double cursorX;
   private static double cursorY;
   private static int modifiers;
   private static int clickLogs;

   private InputBridge() {
   }

   public static boolean isKeyDown(int var0) {
      return var0 >= 0 && var0 < KEYS.length && KEYS[var0];
   }

   public static void drain(Minecraft var0) {
      SkyLink.drainInput((var1, var2, var3, var4, var5) -> dispatch(var0, var1, var2, var3, var4, var5));
   }

   private static void dispatch(Minecraft var0, int var1, int var2, int var3, int var4, int var5) {
      long var6 = var0.getWindow().handle();
      switch (var1) {
         case 1:
            key(var0, var6, var2, var3 != 0);
            break;
         case 2:
            if (var2 > 0 && var2 < BUTTONS.length) {
               BUTTONS[var2] = var3 != 0;
            }

            if (var3 != 0 && clickLogs++ < 20) {
               HitResult var12 = var0.hitResult;
               SkyCraft.LOG
                  .info(
                     "SkyCraft: click {} -> {} {} (grabbed {}, screen {})",
                     new Object[]{
                        var2,
                        var12 == null ? "null" : var12.getType(),
                        var12 instanceof EntityHitResult var9 ? var9.getEntity().getName().getString() : (var12 == null ? "" : var12.getLocation()),
                        var0.mouseHandler.isMouseGrabbed(),
                        var0.gui.screen()
                     }
                  );
            }

            var0.mouseHandler.onButton(var6, new MouseButtonInfo(var2, modifiers), var3 != 0 ? 1 : 0);
            break;
         case 3:
            var0.mouseHandler.onScroll(var6, 0.0, var3 / 120.0);
            break;
         case 4:
            double var8 = var3 - cursorX;
            double var10 = var4 - cursorY;
            cursorX = var3;
            cursorY = var4;
            var0.mouseHandler.onMove(var6, var3, var4, var8, var10);
            break;
         case 5:
            if (var0.gui.screen() != null) {
               var0.keyboardHandler.textInput(var6, new String(Character.toChars(var3)));
            }
            break;
         case 6:
            releaseAll();
            break;
         case 7:
            hurt(var0, var2, var3 / 100.0F, var4, var5);
            break;
         case 8:
            if (var0.gui.screen() == null && var0.player != null) {
               releaseAll();
               var0.gui.setScreen(new PauseScreen(true));
            }
            break;
         case 9:
            CoopBlocks.applyRemote(var0.getSingleplayerServer(), var2, var3, var4, var5);
            break;
         case 10:
            if (var0.player != null) {
               releaseAll();
               var0.gui.setScreen(new OptionsScreen(null, var0.options));
            }
      }
   }

   private static void hurt(Minecraft var0, int var1, float var2, int var3, int var4) {
      IntegratedServer var5 = var0.getSingleplayerServer();
      if (var0.player != null) {
         if (var5 == null) {
            if (ClientPlayNetworking.canSend(SkyNet.Hurt.TYPE)) {
               ClientPlayNetworking.send(new SkyNet.Hurt(var1, var2, var3, var4));
            }
         } else {
            UUID var6 = var0.player.getUUID();
            var5.execute(() -> {
               ServerPlayer var6x = var5.getPlayerList().getPlayer(var6);
               if (var6x != null) {
                  SkyCombat.hurtPlayer(var6x, var1, var2, var3, var4);
               }
            });
         }
      }
   }

   private static void key(Minecraft var0, long var1, int var3, boolean var4) {
      if (var3 > 0 && var3 < KEYS.length) {
         boolean var5 = KEYS[var3];
         KEYS[var3] = var4;
         updateModifiers();
         int var6 = var4 ? (var5 ? -1 : 1) : 0;
         int var7 = SDLKeyboard.SDL_GetKeyFromScancode(var3, (short)modifiers, true);
         var0.keyboardHandler.keyPress(var1, var6, new KeyEvent(var3, var7, modifiers));
      }
   }

   private static void updateModifiers() {
      short var0 = 0;
      if (KEYS[225]) {
         var0 |= 1;
      }

      if (KEYS[229]) {
         var0 |= 2;
      }

      if (KEYS[224]) {
         var0 |= 64;
      }

      if (KEYS[228]) {
         var0 |= 128;
      }

      if (KEYS[226]) {
         var0 |= 256;
      }

      if (KEYS[230]) {
         var0 |= 512;
      }

      modifiers = var0;
   }

   public static void releaseAll() {
      Minecraft var0 = Minecraft.getInstance();
      long var1 = var0.getWindow().handle();

      for (int var3 = 0; var3 < KEYS.length; var3++) {
         if (KEYS[var3]) {
            KEYS[var3] = false;
            updateModifiers();
            var0.keyboardHandler.keyPress(var1, 0, new KeyEvent(var3, SDLKeyboard.SDL_GetKeyFromScancode(var3, (short)0, true), modifiers));
         }
      }

      for (int var4 = 1; var4 < BUTTONS.length; var4++) {
         if (BUTTONS[var4]) {
            BUTTONS[var4] = false;
            var0.mouseHandler.onButton(var1, new MouseButtonInfo(var4, 0), 0);
         }
      }
   }
}
