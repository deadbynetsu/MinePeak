package dev.skycraft.client;

import dev.skycraft.SkyCraft;
import dev.skycraft.client.render.WorldExporter;
import dev.skycraft.coop.CoopHeld;
import dev.skycraft.coop.CoopMode;
import dev.skycraft.link.SkyLink;
import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.SkyWater;
import java.util.UUID;
import net.minecraft.client.Camera;
import net.minecraft.client.Minecraft;
import net.minecraft.client.Options;
import net.minecraft.client.entity.ClientAvatarState;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.client.renderer.state.level.CameraEntityRenderState;
import net.minecraft.client.server.IntegratedServer;
import net.minecraft.client.tutorial.TutorialSteps;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.phys.Vec3;
import org.lwjgl.sdl.SDLVideo;

public final class SkyClient {
   private static final boolean SHOW_WINDOW = Boolean.getBoolean("skycraft.showWindow");
   private static final boolean START_HIDDEN = Boolean.getBoolean("skycraft.startHidden");
   private static boolean startedHidden;
   private static final SkyLink.SkyState sky = new SkyLink.SkyState();
   private static final SkyLink.McState mc = new SkyLink.McState();
   private static volatile boolean linked;
   private static boolean tookOver;
   private static boolean windowHidden;
   private static int appliedViewportW;
   private static int appliedViewportH;
   private static int lastTeleportSeq = -1;
   private static int teleportAck;
   private static boolean teleportPending;
   private static LocalPlayer lastPlayer;
   private static Vec3 holdPos;
   private static Vec3 unlinkedHold;
   private static long holdSince;
   private static long qpcFreq;
   private static LocalPlayer eyePlayer;
   private static float eyeSmoothed;
   private static long frameCounter;
   private static int lastPacedSeq;
   private static boolean skyrimStalled;
   private static int exporterErrors;
   private static final boolean QUIT_WITH_SKYRIM = Boolean.parseBoolean(System.getProperty("skycraft.quitWithSkyrim", "true"));
   private static long skyrimGoneSince;
   private static long nextSkyrimCheck;
   private static final long NEVER_CONNECTED_QUIT_MS = 600000L;
   private static final long STARTED_AT = System.currentTimeMillis();
   private static boolean gaveUpWaiting;

   private SkyClient() {
   }

   public static boolean linked() {
      return linked;
   }

   public static boolean tookOver() {
      return tookOver;
   }

   public static SkyLink.SkyState sky() {
      return sky;
   }

   public static void beginFrame() {
      SkyLink.poll();
      quitWithSkyrim(Minecraft.getInstance());
      if (CoopMode.viewerRequested()) {
         Minecraft.getInstance().stop();
      } else {
         if (START_HIDDEN && !startedHidden) {
            startedHidden = true;
            Minecraft var0 = Minecraft.getInstance();
            hideWindowOnce(var0);
            var0.options.getSoundSourceOptionInstance(SoundSource.MUSIC).set(0.0);
            var0.getMusicManager().stopPlaying();
         }

         boolean var3 = SkyLink.active();
         if (var3) {
            SkyLink.readSkyState(sky);
            SkyWater.refresh();
         } else {
            SkyWater.clear();
         }

         if (var3 != linked) {
            linked = var3;
            SkyCraft.LOG.info("SkyCraft: Skyrim link {}", linked ? "up" : "down");
            if (linked) {
               tookOver = true;
               unlinkedHold = null;
               SkyCollision.startConsumer();
               applyLinkedOptions();
            } else {
               InputBridge.releaseAll();
               LocalPlayer var1 = Minecraft.getInstance().player;
               unlinkedHold = var1 != null ? var1.position() : null;
            }
         }

         if (linked) {
            Minecraft var4 = Minecraft.getInstance();
            hideWindowOnce(var4);
            applyViewportSize(var4);
            MirrorWorld.openWhenReady(var4);
            if (sky.menuOpen() || sky.loading()) {
               InputBridge.releaseAll();
            }

            InputBridge.drain(var4);
            ProxySync.frame(var4);
            CoopHeld.tick(var4);
            LocalPlayer var2 = var4.player;
            if (var2 == null) {
               lastPlayer = null;
            } else {
               if (var2 != lastPlayer) {
                  lastPlayer = var2;
                  teleportPending = true;
               }

               if (sky.teleportSeq != lastTeleportSeq) {
                  lastTeleportSeq = sky.teleportSeq;
                  teleportPending = true;
               }

               if (teleportPending && sky.inGame() && !sky.loading()) {
                  requestTeleport(var4, sky.x, sky.y, sky.z, sky.yaw, sky.pitch);
                  teleportAck = sky.teleportSeq;
                  teleportPending = false;
                  holdPos = new Vec3(sky.x, sky.y, sky.z);
               }

               if (var4.gui.screen() == null) {
                  var2.setYRot(sky.yaw);
                  var2.setXRot(sky.pitch);
                  var2.yRotO = sky.yaw;
                  var2.xRotO = sky.pitch;
               }
            }
         }
      }
   }

   private static void quitWithSkyrim(Minecraft var0) {
      int var1 = SkyLink.skyrimPid();
      long var2 = System.currentTimeMillis();
      if (QUIT_WITH_SKYRIM && START_HIDDEN && var1 == 0 && !tookOver && !gaveUpWaiting && var2 - STARTED_AT > 600000L) {
         gaveUpWaiting = true;
         SkyCraft.LOG.warn("SkyCraft: started hidden but Skyrim never connected in {} minutes; quitting", 10L);
         var0.stop();
      } else if (QUIT_WITH_SKYRIM && var1 != 0 && var2 >= nextSkyrimCheck) {
         nextSkyrimCheck = var2 + 1000L;
         if (ProcessHandle.of(var1).map(ProcessHandle::isAlive).orElse(false)) {
            skyrimGoneSince = 0L;
         } else {
            if (skyrimGoneSince == 0L) {
               skyrimGoneSince = var2;
            } else if (var2 - skyrimGoneSince > 5000L) {
               SkyCraft.LOG.info("SkyCraft: Skyrim (pid {}) has closed; saving and quitting", var1);
               var0.stop();
            }
         }
      }
   }

   public static void clientTick(Minecraft var0) {
      MirrorWorld.tick(var0);
      DiscordPresence.tick(var0);
      SkyDigClient.tick(var0);
      freezeWhileUnlinked(var0);
      holdUntilReady(var0);
      publishTick(var0);
   }

   private static void freezeWhileUnlinked(Minecraft var0) {
      LocalPlayer var1 = var0.player;
      if (!linked && tookOver && var1 != null) {
         if (unlinkedHold == null) {
            unlinkedHold = var1.position();
         }

         var1.setDeltaMovement(Vec3.ZERO);
         var1.setPos(unlinkedHold.x, unlinkedHold.y, unlinkedHold.z);
         var1.xo = unlinkedHold.x;
         var1.yo = unlinkedHold.y;
         var1.zo = unlinkedHold.z;
         var1.resetFallDistance();
      }
   }

   private static void publishTick(Minecraft var0) {
      LocalPlayer var1 = var0.player;
      if (linked && var1 != null) {
         if (qpcFreq == 0L) {
            qpcFreq = SkyLink.qpcFrequency();
         }

         float var2 = var0.level != null ? var0.level.tickRateManager().millisecondsPerTick() : 50.0F;
         float var3 = var0.getDeltaTracker().getGameTimeDeltaPartialTick(false);
         mc.tickQpc = SkyLink.qpc() - (long)(var3 * var2 * (float)qpcFreq / 1000.0);
         mc.tickMs = var2;
         mc.prevX = var1.xo;
         mc.prevY = var1.yo;
         mc.prevZ = var1.zo;
         mc.curX = var1.getX();
         mc.curY = var1.getY();
         mc.curZ = var1.getZ();
         if (var1 != eyePlayer) {
            eyePlayer = var1;
            eyeSmoothed = var1.getEyeHeight();
         }

         mc.eyeHeightO = eyeSmoothed;
         eyeSmoothed = eyeSmoothed + (var1.getEyeHeight() - eyeSmoothed) * 0.5F;
         mc.eyeHeightT = eyeSmoothed;
         boolean var4 = var0.options.bobView().get();
         ClientAvatarState var5 = var1.avatarState();
         mc.walkDistO = var4 ? var5.getInterpolatedWalkDistance(0.0F) : 0.0F;
         mc.walkDist = var4 ? var5.getInterpolatedWalkDistance(1.0F) : 0.0F;
         mc.bobO = var4 ? var5.getInterpolatedBob(0.0F) : 0.0F;
         mc.bob = var4 ? var5.getInterpolatedBob(1.0F) : 0.0F;
         SkyLink.writeMcState(mc);
      }
   }

   private static void holdUntilReady(Minecraft var0) {
      LocalPlayer var1 = var0.player;
      if (linked && var1 != null) {
         if (!sky.inGame() || sky.loading()) {
            if (holdPos == null) {
               holdPos = var1.position();
            }

            teleportPending = true;
         }

         if (holdPos == null) {
            holdSince = 0L;
         } else {
            if (holdSince == 0L) {
               holdSince = System.currentTimeMillis();
            }

            int var2 = (int)Math.floor(holdPos.x);
            int var3 = (int)Math.floor(holdPos.y);
            int var4 = (int)Math.floor(holdPos.z);
            boolean var5 = SkyCollision.isKnown(var2, var3 - 1, var4) && SkyCollision.isKnown(var2, var3, var4) && SkyCollision.isKnown(var2, var3 - 8, var4);
            boolean var6 = var5 && (SkyCollision.hasSolidBelow(var2, var3, var4, 12) || System.currentTimeMillis() - holdSince > 6000L);
            if (var6 && sky.inGame() && !sky.loading()) {
               Vec3 var7 = liftOutOfGeometry(var1, holdPos);
               if (var7.y != holdPos.y) {
                  var1.setPos(var7.x, var7.y, var7.z);
                  var1.yo = var7.y;
                  SkyCraft.LOG.info("SkyCraft: lifted player {} blocks out of the ground", String.format("%.3f", var7.y - holdPos.y));
               }

               SkyCraft.LOG.info("SkyCraft: spawn hold: ground arrived after {} ms", System.currentTimeMillis() - holdSince);
               holdPos = null;
            } else {
               var1.setDeltaMovement(Vec3.ZERO);
               var1.setPos(holdPos.x, holdPos.y, holdPos.z);
               var1.xo = holdPos.x;
               var1.yo = holdPos.y;
               var1.zo = holdPos.z;
               var1.resetFallDistance();
            }
         }
      }
   }

   private static Vec3 liftOutOfGeometry(LocalPlayer var0, Vec3 var1) {
      double var2 = SkyCollider.groundAt(var1.x, var1.y, var1.z, 2.5);
      return !Double.isNaN(var2) && var2 > var1.y ? new Vec3(var1.x, var2, var1.z) : var1;
   }

   private static void requestTeleport(Minecraft var0, double var1, double var3, double var5, float var7, float var8) {
      LocalPlayer var9 = var0.player;
      var9.setPos(var1, var3, var5);
      var9.setDeltaMovement(Vec3.ZERO);
      var9.resetFallDistance();
      IntegratedServer var10 = var0.getSingleplayerServer();
      if (var10 != null) {
         UUID var11 = var9.getUUID();
         var10.execute(() -> {
            ServerPlayer var10x = var10.getPlayerList().getPlayer(var11);
            if (var10x != null) {
               var10x.teleportTo(var1, var3, var5);
               var10x.setYRot(var7);
               var10x.setXRot(var8);
               var10x.resetFallDistance();
            }
         });
      }

      SkyCraft.LOG.info("SkyCraft: teleported to {} {} {}", new Object[]{var1, var3, var5});
   }

   public static void afterRender() {
      if (linked) {
         Minecraft var0 = Minecraft.getInstance();
         LocalPlayer var1 = var0.player;
         short var2 = 0;
         if (var1 != null && var0.level != null) {
            float var3 = var0.getDeltaTracker().getGameTimeDeltaPartialTick(false);
            Vec3 var4 = var1.getPosition(var3);
            Camera var5 = var0.gameRenderer.mainCamera();
            var2 |= 1;
            if (var1.onGround()) {
               var2 |= 4;
            }

            if (var1.isShiftKeyDown()) {
               var2 |= 8;
            }

            if (var1.isSprinting()) {
               var2 |= 16;
            }

            if (var1.isDeadOrDying()) {
               var2 |= 32;
            }

            if (var1.isSwimming()) {
               var2 |= 64;
            }

            if (var1.getAbilities().flying) {
               var2 |= 128;
            }

            mc.x = var4.x;
            mc.y = var4.y;
            mc.z = var4.z;
            mc.yaw = var1.getYRot();
            mc.pitch = var1.getXRot();
            Vec3 var6 = var5.isDetached() ? var1.getEyePosition(var3) : var5.position();
            mc.eyeHeight = (float)(var6.y - var4.y);
            mc.eyeX = var6.x;
            mc.eyeY = var6.y;
            mc.eyeZ = var6.z;
            mc.fov = var5.getFov();
            mc.cameraMode = var0.options.getCameraType().ordinal();
            mc.cameraDistance = var5.isDetached() ? (float)var5.position().distanceTo(var1.getEyePosition(var3)) : 0.0F;
            CameraEntityRenderState var7 = var0.gameRenderer.gameRenderState().levelRenderState.cameraRenderState.entityRenderState;
            boolean var8 = var0.options.bobView().get() && var7.isPlayer;
            mc.bobPhase = var8 ? var7.backwardsInterpolatedWalkDistance : 0.0F;
            mc.bobAmount = var8 ? var7.bob : 0.0F;
         }

         if (var0.gui.screen() != null) {
            var2 |= 2;
         }

         mc.flags = var2;
         mc.sensitivity = var0.options.sensitivity().get().floatValue();
         mc.teleportAck = holdPos == null ? teleportAck : teleportAck - 1;
         mc.guiScale = var0.getWindow().getGuiScale();
         mc.frameCounter = ++frameCounter;
         SkyLink.writeMcState(mc);
         if ((var2 & 1) != 0) {
            try {
               WorldExporter.frame(var0, var0.getDeltaTracker().getGameTimeDeltaPartialTick(false));
            } catch (RuntimeException var9) {
               if (exporterErrors++ < 5) {
                  SkyCraft.LOG.error("SkyCraft: world export failed", var9);
               }
            }

            FrameExporter.capture(var0);
         }
      }
   }

   public static void paceFrame() {
      if (linked) {
         if (!skyrimStalled || SkyLink.skyStateSeq() >>> 1 != lastPacedSeq) {
            skyrimStalled = false;
            long var0 = System.nanoTime() + 25000000L;

            while (SkyLink.skyStateSeq() >>> 1 == lastPacedSeq && System.nanoTime() < var0) {
               Thread.onSpinWait();
               if (var0 - System.nanoTime() > 2000000L) {
                  Thread.yield();
               }
            }

            int var2 = SkyLink.skyStateSeq() >>> 1;
            skyrimStalled = var2 == lastPacedSeq;
            lastPacedSeq = var2;
         }
      }
   }

   private static void applyLinkedOptions() {
      Minecraft var0 = Minecraft.getInstance();
      Options var1 = var0.options;
      var1.pauseOnLostFocus = false;
      var1.vignette().set(false);
      var1.enableVsync().set(false);
      var1.framerateLimit().set(260);
      var1.renderDistance().set(8);
      var1.simulationDistance().set(8);
      var1.autoJump().set(false);
      var1.onboardAccessibility = false;
      if (var1.tutorialStep != TutorialSteps.NONE) {
         var0.getTutorial().setStep(TutorialSteps.NONE);
      }

      var1.getSoundSourceOptionInstance(SoundSource.MUSIC).set(0.0);
      var1.save();
   }

   private static void hideWindowOnce(Minecraft var0) {
      if (!windowHidden && !SHOW_WINDOW) {
         windowHidden = true;
         SDLVideo.SDL_HideWindow(var0.getWindow().handle());
         SkyCraft.LOG.info("SkyCraft: game window hidden (run with -Dskycraft.showWindow=true to keep it)");
      }
   }

   private static void applyViewportSize(Minecraft var0) {
      int var1 = Math.min(sky.viewportW, 3840);
      int var2 = Math.min(sky.viewportH, 2160);
      if (var1 > 0 && var2 > 0 && (var1 != appliedViewportW || var2 != appliedViewportH)) {
         appliedViewportW = var1;
         appliedViewportH = var2;
         var0.getWindow().setWindowed(var1, var2);
         SkyCraft.LOG.info("SkyCraft: sizing overlay to Skyrim viewport {}x{}", var1, var2);
      }
   }
}
