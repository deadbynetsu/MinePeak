package dev.skycraft.client;

import dev.skycraft.client.render.WorldExporter;
import dev.skycraft.net.SkyNet;
import dev.skycraft.world.SkyClip;
import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.SkyDig;
import dev.skycraft.world.SkyRay;
import it.unimi.dsi.fastutil.longs.Long2IntLinkedOpenHashMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;
import it.unimi.dsi.fastutil.longs.LongOpenHashSet;
import java.util.ArrayList;
import java.util.List;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayNetworking;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.particle.TerrainParticle;
import net.minecraft.client.resources.sounds.SimpleSoundInstance;
import net.minecraft.client.resources.sounds.SoundInstance;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.level.ChunkPos;
import net.minecraft.world.level.GameType;
import net.minecraft.world.level.block.SoundType;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.chunk.LevelChunk;
import net.minecraft.world.level.chunk.status.ChunkStatus;
import org.jspecify.annotations.Nullable;

public final class SkyDigClient {
   private static final int REVEAL_RANGE = 24;
   private static final int REVEAL_TRIES = 60;
   private static @Nullable BlockPos mining;
   private static float progress;
   private static int miningTick;
   private static int hits;
   private static int ticks;
   private static final Long2IntLinkedOpenHashMap REVEAL = new Long2IntLinkedOpenHashMap();
   private static final Long2ObjectOpenHashMap<SkyDig.DugColumn> SEEN = new Long2ObjectOpenHashMap();
   private static final SkyDig.DugColumn NONE = new SkyDig.DugColumn(List.of());
   private static int seenWorld = Integer.MIN_VALUE;
   private static int pollTicks;

   private SkyDigClient() {
   }

   public static int world() {
      return SkyClient.sky().worldId;
   }

   public static boolean attack(Minecraft var0) {
      if (var0.hitResult instanceof SkyClip.SkyrimHitResult var1 && var0.level != null && var0.player != null && var0.gameMode != null) {
         GameType var10 = var0.gameMode.getPlayerMode();
         if (var10 != GameType.ADVENTURE && var10 != GameType.SPECTATOR && (SkyDig.destruction || !var0.hasSingleplayerServer())) {
            SkyRay.Hit var3 = var1.hit;
            if (var3.tri() != null && var3.tri().diggable) {
               int[] var4 = SkyRay.surfaceCell(var3);
               BlockPos var5 = new BlockPos(var4[0], var4[1], var4[2]);
               BlockState var6 = var0.level.getBlockState(var5);
               if (!var6.isAir() && !var6.canBeReplaced()) {
                  return false;
               }

               if (SkyDig.isDug(var0.level, world(), var5)) {
                  return false;
               }

               if (var5.equals(mining) && miningTick == ticks) {
                  return true;
               }

               if (!var5.equals(mining) || ticks - miningTick > 2) {
                  mining = var5;
                  progress = 0.0F;
                  hits = 0;
               }

               miningTick = ticks;
               int var7 = var3.tri().material == 0 ? 3 : var3.tri().material;
               BlockState var8 = SkyDig.materialState(var7);
               progress = progress + (var0.player.getAbilities().instabuild ? 1.0F : var8.getDestroyProgress(var0.player, var0.level, var5));
               if (hits++ % 4 == 0) {
                  SoundType var9 = var8.getSoundType();
                  var0.getSoundManager()
                     .play(
                        new SimpleSoundInstance(
                           var9.getHitSound(),
                           SoundSource.BLOCKS,
                           (var9.getVolume() + 1.0F) / 8.0F,
                           var9.getPitch() * 0.5F,
                           SoundInstance.createUnseededRandom(),
                           var5
                        )
                     );
                  var0.particleEngine
                     .add(
                        new TerrainParticle(
                              var0.level, var3.x() + var3.nx() * 0.05, var3.y() + var3.ny() * 0.05, var3.z() + var3.nz() * 0.05, 0.0, 0.0, 0.0, var8, var5
                           )
                           .setPower(0.2F)
                           .scale(0.6F)
                     );
               }

               if (progress >= 1.0F) {
                  ClientPlayNetworking.send(new SkyNet.DigOpen(world(), var5, var7));
                  REVEAL.put(var5.asLong(), 60);
                  mining = null;
                  progress = 0.0F;
               }

               return true;
            } else {
               return false;
            }
         } else {
            return false;
         }
      } else {
         return false;
      }
   }

   public static void blockChanged(ClientLevel var0, BlockPos var1, BlockState var2, BlockState var3) {
      if (!var2.isAir() && (var3.isAir() || var3.canBeReplaced())) {
         Minecraft var4 = Minecraft.getInstance();
         if (var4.player != null && !(var4.player.blockPosition().distSqr(var1) > 576.0)) {
            if (SkyDig.isDug(var0, world(), var1) && (SkyDig.destruction || !var4.hasSingleplayerServer())) {
               REVEAL.put(var1.asLong(), 60);
            }
         }
      }
   }

   public static void tick(Minecraft var0) {
      ticks++;
      ClientLevel var1 = var0.level;
      if (var1 != null && var0.player != null) {
         int var2 = world();
         SkyDig.clientWorld = var2;
         SkyDig.clientDug = (var2x, var3, var4) -> SkyDig.isDug(var1, var2, new BlockPos(var2x, var3, var4));
         reveal(var1);
         if (++pollTicks % 5 == 0) {
            pollDug(var0, var1);
         }

         SkyCollision.takeChangedRegions(var1x -> {
            int var3 = BlockPos.getX(var1x);
            int var4 = BlockPos.getY(var1x);
            int var5 = BlockPos.getZ(var1x);
            SkyDig.wallsChanged(var3, var4, var5, var3 + 7, var4 + 7, var5 + 7);

            for (int var6 = var3 - 1 >> 4; var6 <= var3 + 8 >> 4; var6++) {
               for (int var7 = var5 - 1 >> 4; var7 <= var5 + 8 >> 4; var7++) {
                  LevelChunk var8 = var1.getChunkSource().getChunk(var6, var7, ChunkStatus.FULL, false);
                  if (var8 != null) {
                     for (int var9 = var4 - 1 >> 4; var9 <= var4 + 8 >> 4; var9++) {
                        if (dugBits(var8, var9) != null) {
                           WorldExporter.markDirty(var6, var9, var7);
                        }
                     }
                  }
               }
            }
         });
      } else {
         REVEAL.clear();
         SEEN.clear();
         SkyDig.clientDug = null;
      }
   }

   private static void reveal(ClientLevel var0) {
      if (!REVEAL.isEmpty()) {
         int var1 = world();
         ArrayList var2 = new ArrayList();
         ArrayList var3 = new ArrayList();
         LongOpenHashSet var4 = new LongOpenHashSet();
         long[] var5 = REVEAL.keySet().toLongArray();

         for (long var9 : var5) {
            BlockPos var11 = BlockPos.of(var9);
            boolean var12 = false;

            for (Direction var16 : Direction.values()) {
               BlockPos var17 = var11.relative(var16);
               if (!var4.contains(var17.asLong()) && !SkyDig.isDug(var0, var1, var17)) {
                  if (!SkyCollision.isKnown(var17.getX(), var17.getY(), var17.getZ())) {
                     var12 = true;
                  } else {
                     int var18 = SkyDig.classify(var17.getX(), var17.getY(), var17.getZ());
                     if (var18 > 0) {
                        BlockState var19 = var0.getBlockState(var17);
                        if (!var19.isAir() && !var19.canBeReplaced()) {
                           var18 = 0;
                        }

                        var4.add(var17.asLong());
                        var2.add(var17);
                        var3.add(var18);
                     }
                  }
               }
            }

            int var22 = REVEAL.get(var9) - 1;
            if (var12 && var22 > 0) {
               REVEAL.put(var9, var22);
            } else {
               REVEAL.remove(var9);
            }
         }

         for (byte var20 = 0; var20 < var2.size(); var20 += 64) {
            int var21 = Math.min(var2.size(), var20 + 64);
            ClientPlayNetworking.send(new SkyNet.DigReveal(var1, List.copyOf(var2.subList(var20, var21)), List.copyOf(var3.subList(var20, var21))));
         }
      }
   }

   public static long @Nullable [] dugBits(LevelChunk var0, int var1) {
      SkyDig.DugColumn var2 = (SkyDig.DugColumn)var0.getAttached(SkyDig.DUG);
      return var2 == null ? null : var2.bits(world(), var1);
   }

   public static void resendAll() {
      SEEN.clear();
   }

   private static void pollDug(Minecraft var0, ClientLevel var1) {
      int var2 = world();
      boolean var3 = var2 != seenWorld;
      seenWorld = var2;
      if (var3) {
         SkyDig.wallsChanged();
      }

      int var4 = var0.options.getEffectiveRenderDistance() + 1;
      int var5 = var0.player.getBlockX() >> 4;
      int var6 = var0.player.getBlockZ() >> 4;

      for (int var7 = var5 - var4; var7 <= var5 + var4; var7++) {
         for (int var8 = var6 - var4; var8 <= var6 + var4; var8++) {
            long var9 = ChunkPos.pack(var7, var8);
            LevelChunk var11 = var1.getChunkSource().getChunk(var7, var8, ChunkStatus.FULL, false);
            if (var11 == null) {
               SEEN.remove(var9);
            } else {
               SkyDig.DugColumn var12 = (SkyDig.DugColumn)var11.getAttached(SkyDig.DUG);
               if (var12 == null) {
                  var12 = NONE;
               }

               SkyDig.DugColumn var13 = (SkyDig.DugColumn)SEEN.get(var9);
               if (var13 != var12 || var3) {
                  SEEN.put(var9, var12);

                  for (SkyDig.DugColumn var17 : new SkyDig.DugColumn[]{var12, var13 == null ? NONE : var13}) {
                     for (SkyDig.DugSection var19 : var17.sections()) {
                        if (var3 || var19.world() == var2) {
                           WorldExporter.markDirtyNow(var7, var19.sectionY(), var8);
                           WorldExporter.markDirty(var7 - 1, var19.sectionY(), var8);
                           WorldExporter.markDirty(var7 + 1, var19.sectionY(), var8);
                           WorldExporter.markDirty(var7, var19.sectionY() - 1, var8);
                           WorldExporter.markDirty(var7, var19.sectionY() + 1, var8);
                           WorldExporter.markDirty(var7, var19.sectionY(), var8 - 1);
                           WorldExporter.markDirty(var7, var19.sectionY(), var8 + 1);
                        }
                     }
                  }
               }
            }
         }
      }

      if (SEEN.size() > 8192) {
         SEEN.clear();
      }
   }
}
