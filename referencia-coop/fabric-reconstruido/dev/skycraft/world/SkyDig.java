package dev.skycraft.world;

import com.mojang.serialization.Codec;
import com.mojang.serialization.codecs.RecordCodecBuilder;
import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import io.netty.buffer.ByteBuf;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.stream.LongStream;
import net.fabricmc.fabric.api.attachment.v1.AttachmentRegistry;
import net.fabricmc.fabric.api.attachment.v1.AttachmentSyncPredicate;
import net.fabricmc.fabric.api.attachment.v1.AttachmentType;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.SectionPos;
import net.minecraft.core.Direction.Axis;
import net.minecraft.network.codec.ByteBufCodecs;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.resources.Identifier;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.DyeColor;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.CollisionGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.chunk.LevelChunk;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.jspecify.annotations.Nullable;

public final class SkyDig {
   public static final AttachmentType<SkyDig.DugColumn> DUG = AttachmentRegistry.builder()
      .persistent(SkyDig.DugColumn.CODEC)
      .syncWith(SkyDig.DugColumn.STREAM_CODEC, AttachmentSyncPredicate.all())
      .buildAndRegister(Identifier.fromNamespaceAndPath("skycraft", "dug"));
   private static final double REACH = 8.0;
   public static volatile boolean destruction = false;
   public static final int KEEP = -1;
   public static final int AIR = 0;
   public static final double BEDROCK_DEPTH = 5.0;
   private static final int WHOLE = 25;
   private static final double SEARCH = 2.5;
   private static final double LAND_REACH = 16.0;
   private static final double[] SAMPLE = new double[]{0.16666666666666666, 0.5, 0.8333333333333334};
   private static final double COLUMN_REACH = 96.0;
   private static final SkyLink.SkyState SERVER_SKY = new SkyLink.SkyState();
   private static volatile int serverWorld;
   private static volatile long serverWorldAt;
   private static final ConcurrentHashMap<Long, ConcurrentHashMap<Long, VoxelShape>> WALLS = new ConcurrentHashMap<>();
   private static final VoxelShape NO_WALL = Shapes.empty();
   public static volatile int clientWorld;
   public static volatile SkyDig.@Nullable DugLookup clientDug;

   private SkyDig() {
   }

   public static void init() {
      SkyCraft.LOG.info("SkyCraft: digging into Skyrim registered ({})", DUG.identifier());
   }

   public static SkyDig.DugColumn column(LevelChunk var0) {
      SkyDig.DugColumn var1 = (SkyDig.DugColumn)var0.getAttached(DUG);
      return var1 != null ? var1 : SkyDig.DugColumn.EMPTY;
   }

   public static boolean isDug(Level var0, int var1, BlockPos var2) {
      return column(var0.getChunkAt(var2)).isDug(var1, var2.getX(), var2.getY(), var2.getZ());
   }

   private static boolean inReach(ServerPlayer var0, BlockPos var1, double var2) {
      return var0.getEyePosition().distanceToSqr(var1.getX() + 0.5, var1.getY() + 0.5, var1.getZ() + 0.5) <= var2 * var2;
   }

   public static void open(ServerPlayer var0, int var1, BlockPos var2, int var3) {
      ServerLevel var4 = var0.level();
      if (destruction && inReach(var0, var2, 8.0) && var4.isLoaded(var2) && !var0.isSpectator()) {
         LevelChunk var5 = var4.getChunkAt(var2);
         SkyDig.DugColumn var6 = column(var5);
         if (!var6.isDug(var1, var2.getX(), var2.getY(), var2.getZ())) {
            var5.setAttached(DUG, var6.with(var1, var2.getX(), var2.getY(), var2.getZ()));
            BlockState var7 = materialState(var3);
            if (!var0.isCreative()) {
               ItemStack var8 = var0.getMainHandItem();
               ItemStack var9 = var8.copy();
               boolean var10 = var0.hasCorrectToolForDrops(var7);
               var8.mineBlock(var4, var7, var2, var0);
               if (var10) {
                  var7.getBlock().playerDestroy(var4, var0, var2, var7, null, var9);
               }
            }

            var4.levelEvent(2001, var2, Block.getId(var7));
         }
      }
   }

   public static void reveal(ServerPlayer var0, int var1, List<BlockPos> var2, int[] var3) {
      if (destruction) {
         ServerLevel var4 = var0.level();

         for (int var5 = 0; var5 < var2.size() && var5 < var3.length; var5++) {
            BlockPos var6 = (BlockPos)var2.get(var5);
            if (inReach(var0, var6, 12.0) && var4.isLoaded(var6)) {
               digCell(var4, var1, var6, var3[var5]);
            }
         }
      }
   }

   public static boolean markDug(ServerLevel var0, int var1, BlockPos var2) {
      LevelChunk var3 = var0.getChunkAt(var2);
      SkyDig.DugColumn var4 = column(var3);
      if (var4.isDug(var1, var2.getX(), var2.getY(), var2.getZ())) {
         return false;
      }

      var3.setAttached(DUG, var4.with(var1, var2.getX(), var2.getY(), var2.getZ()));
      return true;
   }

   public static void digCell(ServerLevel var0, int var1, BlockPos var2, int var3) {
      if (markDug(var0, var1, var2)) {
         BlockState var4 = var0.getBlockState(var2);
         if (var3 > 0 && (var4.isAir() || var4.canBeReplaced())) {
            var0.setBlock(var2, blockFor(var0, var2, var3), 3);
         }
      }
   }

   public static BlockState materialState(int var0) {
      return switch (var0) {
         case 1 -> Blocks.GRASS_BLOCK.defaultBlockState();
         case 2 -> Blocks.DIRT.defaultBlockState();
         default -> Blocks.STONE.defaultBlockState();
         case 4 -> Blocks.COBBLESTONE.defaultBlockState();
         case 5 -> Blocks.SNOW_BLOCK.defaultBlockState();
         case 6 -> Blocks.PACKED_ICE.defaultBlockState();
         case 7 -> Blocks.SAND.defaultBlockState();
         case 8 -> Blocks.GRAVEL.defaultBlockState();
         case 9 -> Blocks.MUD.defaultBlockState();
         case 10 -> Blocks.OAK_LOG.defaultBlockState();
         case 11 -> Blocks.SPRUCE_LOG.defaultBlockState();
         case 12 -> Blocks.BIRCH_LOG.defaultBlockState();
         case 13 -> Blocks.SPRUCE_PLANKS.defaultBlockState();
         case 14 -> Blocks.COPPER_BLOCK.waxed().unaffected().defaultBlockState();
         case 15 -> Blocks.GLASS.defaultBlockState();
         case 16 -> Blocks.MOSS_BLOCK.defaultBlockState();
         case 17 -> Blocks.WOOL.pick(DyeColor.BROWN).defaultBlockState();
         case 18 -> Blocks.BONE_BLOCK.defaultBlockState();
         case 19 -> Blocks.COBWEB.defaultBlockState();
         case 20 -> Blocks.CONCRETE_POWDER.pick(DyeColor.LIGHT_GRAY).defaultBlockState();
         case 21 -> Blocks.BEDROCK.defaultBlockState();
      };
   }

   public static BlockState blockFor(ServerLevel var0, BlockPos var1, int var2) {
      return var2 != 3 && var2 != 0 ? materialState(var2) : stoneOrOre(var0.getSeed(), var1);
   }

   static BlockState stoneOrOre(long var0, BlockPos var2) {
      long var3 = mix(var0 ^ mix((var2.getX() >> 1) * 73856093L ^ (var2.getY() >> 1) * 19349663L ^ (var2.getZ() >> 1) * 83492791L));
      long var5 = mix(var3 ^ (var2.getX() & 1 | (var2.getY() & 1) << 1 | (var2.getZ() & 1) << 2));
      if ((var5 & 255L) >= 150L) {
         return Blocks.STONE.defaultBlockState();
      }

      int var7 = (int)((var3 >>> 16) % 10000L);
      var7 -= 330;
      if (var7 < 0) {
         return Blocks.COAL_ORE.defaultBlockState();
      }

      var7 -= 220;
      if (var7 < 0) {
         return Blocks.IRON_ORE.defaultBlockState();
      }

      var7 -= 170;
      if (var7 < 0) {
         return Blocks.COPPER_ORE.defaultBlockState();
      }

      var7 -= 80;
      if (var7 < 0) {
         return Blocks.REDSTONE_ORE.defaultBlockState();
      }

      var7 -= 45;
      if (var7 < 0) {
         return Blocks.GOLD_ORE.defaultBlockState();
      }

      var7 -= 35;
      if (var7 < 0) {
         return Blocks.LAPIS_ORE.defaultBlockState();
      }

      var7 -= 14;
      if (var7 < 0) {
         return Blocks.DIAMOND_ORE.defaultBlockState();
      }

      var7 -= 5;
      return var7 < 0 ? Blocks.EMERALD_ORE.defaultBlockState() : Blocks.STONE.defaultBlockState();
   }

   private static long mix(long var0) {
      var0 = (var0 ^ var0 >>> 30) * -4658895280553007687L;
      var0 = (var0 ^ var0 >>> 27) * -7723592293110705685L;
      return var0 ^ var0 >>> 31;
   }

   public static int classify(int var0, int var1, int var2) {
      SkyDig.Probe var3 = new SkyDig.Probe().around(var0, var1, var2, var0 + 1, var1 + 1, var2 + 1);
      int var4 = 0;
      int var5 = 3;
      double var6 = Double.POSITIVE_INFINITY;

      for (double var11 : SAMPLE) {
         for (double var16 : SAMPLE) {
            for (double var21 : SAMPLE) {
               int var23 = var3.test(var0 + var21, var1 + var11, var2 + var16);
               if (var23 == -1) {
                  return -1;
               }

               if (var23 != 0) {
                  var4++;
                  if (var21 == 0.5 && var11 == 0.5 && var16 == 0.5) {
                     var5 = var23;
                     var6 = var3.depth;
                  }
               }
            }
         }
      }

      if (var4 < 25) {
         return 0;
      }

      SkyTri var24 = landAbove(var0 + 0.5, var1 + 0.5, var2 + 0.5);
      if (var24 != null) {
         double var25 = var24.heightAt(var0 + 0.5, var2 + 0.5) - (var1 + 0.5);
         if (var25 >= 5.0) {
            return 21;
         }

         if (Double.isInfinite(var6)) {
            var5 = var24.material == 0 ? 3 : var24.material;
            var6 = var25;
         }
      }

      return underground(var5, var6);
   }

   public static @Nullable SkyTri landAbove(double var0, double var2, double var4) {
      ArrayList var6 = new ArrayList();
      SkyCollision.originalSurfacesNear(new AABB(var0 - 0.01, var2, var4 - 0.01, var0 + 0.01, var2 + 32.0, var4 + 0.01), var6);
      SkyTri var7 = null;
      double var8 = Double.POSITIVE_INFINITY;

      for (SkyTri var11 : var6) {
         if (var11.terrain) {
            double var12 = var11.heightAt(var0, var4);
            if (!Double.isNaN(var12) && var12 >= var2 && var12 < var8) {
               var8 = var12;
               var7 = var11;
            }
         }
      }

      return var7;
   }

   public static int underground(int var0, double var1) {
      return switch (var0) {
         case 1, 2 -> var1 < 3.5 ? 2 : 3;
         default -> 3;
         case 4 -> var1 < 1.5 ? 4 : 3;
         case 5 -> var1 < 1.5 ? 5 : (var1 < 3.5 ? 2 : 3);
         case 6 -> var1 < 2.5 ? 6 : 3;
         case 7 -> var1 < 3.5 ? 7 : 3;
         case 8 -> var1 < 2.5 ? 8 : 3;
         case 9 -> var1 < 3.5 ? 9 : 3;
         case 10, 11, 12 -> var0;
         case 13 -> var1 < 2.5 ? 13 : 3;
         case 14 -> var1 < 1.5 ? 14 : 3;
         case 15, 16, 17, 18, 19 -> var1 < 1.0 ? var0 : 3;
         case 20 -> var1 < 3.5 ? 20 : 3;
      };
   }

   public static void wallsChanged() {
      WALLS.clear();
   }

   public static void wallsChanged(int var0, int var1, int var2, int var3, int var4, int var5) {
      int var6 = (int)Math.ceil(2.5) + 1;
      int var7 = var6 + (int)Math.ceil(16.0);

      for (int var8 = var0 - var6 >> 4; var8 <= var3 + var6 >> 4; var8++) {
         for (int var9 = var1 - var7 >> 4; var9 <= var4 + var6 >> 4; var9++) {
            for (int var10 = var2 - var6 >> 4; var10 <= var5 + var6 >> 4; var10++) {
               WALLS.remove(SectionPos.asLong(var8, var9, var10));
            }
         }
      }
   }

   private static int worldFor(Level var0) {
      if (var0.isClientSide()) {
         SkyDig.DugLookup var6 = clientDug;
         return var6 != null ? clientWorld : 0;
      }

      long var1 = System.currentTimeMillis();
      if (var1 - serverWorldAt > 250L) {
         synchronized (SERVER_SKY) {
            if (SkyLink.readSkyState(SERVER_SKY)) {
               serverWorld = SERVER_SKY.worldId;
            }
         }

         serverWorldAt = var1;
      }

      return serverWorld;
   }

   private static SkyDig.@Nullable DugColumn columnFor(CollisionGetter var0, int var1, int var2) {
      return var0.getChunkForCollisions(var1 >> 4, var2 >> 4) instanceof LevelChunk var4 ? (SkyDig.DugColumn)var4.getAttached(DUG) : null;
   }

   public static @Nullable VoxelShape wallShape(CollisionGetter var0, BlockPos var1) {
      if (var0 instanceof Level var2 && SkyCollision.active()) {
         int var3 = var1.getX();
         int var4 = var1.getY();
         int var5 = var1.getZ();
         SkyDig.DugColumn var6 = columnFor(var0, var3, var5);
         int var7 = worldFor(var2);
         if (var6 != null && var6.isDug(var7, var3, var4, var5)) {
            return null;
         }

         boolean var8 = false;

         for (Direction var12 : Direction.values()) {
            SkyDig.DugColumn var13 = var12.getAxis() == Axis.Y ? var6 : columnFor(var0, var3 + var12.getStepX(), var5 + var12.getStepZ());
            if (var13 != null && var13.isDug(var7, var3 + var12.getStepX(), var4 + var12.getStepY(), var5 + var12.getStepZ())) {
               var8 = true;
               break;
            }
         }

         if (!var8) {
            return null;
         }

         if (WALLS.size() > 2048) {
            WALLS.clear();
         }

         ConcurrentHashMap var14 = WALLS.computeIfAbsent(SectionPos.asLong(var3 >> 4, var4 >> 4, var5 >> 4), var0x -> new ConcurrentHashMap<>());
         long var15 = var1.asLong();
         VoxelShape var16 = (VoxelShape)var14.get(var15);
         if (var16 != null) {
            return var16 == NO_WALL ? null : var16;
         }

         VoxelShape var17 = computeWall(var3, var4, var5);
         var14.put(var15, var17 == null ? NO_WALL : var17);
         return var17;
      } else {
         return null;
      }
   }

   private static @Nullable VoxelShape computeWall(int var0, int var1, int var2) {
      SkyDig.Probe var3 = new SkyDig.Probe().around(var0, var1, var2, var0 + 1, var1 + 1, var2 + 1);
      if (!var3.land.isEmpty()) {
         double var4 = Double.POSITIVE_INFINITY;

         for (double[] var9 : new double[][]{{0.05, 0.05}, {0.95, 0.05}, {0.05, 0.95}, {0.95, 0.95}, {0.5, 0.5}}) {
            double var10 = var3.landHeight(var0 + var9[0], var2 + var9[1]);
            if (!Double.isNaN(var10)) {
               var4 = Math.min(var4, var10);
            }
         }

         if (Double.isFinite(var4) && var4 - var1 > 0.05) {
            return var4 - var1 >= 0.999 ? Shapes.block() : Shapes.box(0.0, 0.0, 0.0, 1.0, var4 - var1, 1.0);
         }

         if (var3.tris.isEmpty()) {
            return null;
         }
      }

      return var3.test(var0 + 0.5, var1 + 0.5, var2 + 0.5) > 0 ? Shapes.block() : null;
   }

   static void closestPoint(SkyTri var0, double var1, double var3, double var5, double[] var7) {
      double var8 = var0.bx - var0.ax;
      double var10 = var0.by - var0.ay;
      double var12 = var0.bz - var0.az;
      double var14 = var0.cx - var0.ax;
      double var16 = var0.cy - var0.ay;
      double var18 = var0.cz - var0.az;
      double var20 = var1 - var0.ax;
      double var22 = var3 - var0.ay;
      double var24 = var5 - var0.az;
      double var26 = var8 * var20 + var10 * var22 + var12 * var24;
      double var28 = var14 * var20 + var16 * var22 + var18 * var24;
      if (var26 <= 0.0 && var28 <= 0.0) {
         set(var7, var0.ax, var0.ay, var0.az);
      } else {
         double var30 = var1 - var0.bx;
         double var32 = var3 - var0.by;
         double var34 = var5 - var0.bz;
         double var36 = var8 * var30 + var10 * var32 + var12 * var34;
         double var38 = var14 * var30 + var16 * var32 + var18 * var34;
         if (var36 >= 0.0 && var38 <= var36) {
            set(var7, var0.bx, var0.by, var0.bz);
         } else {
            double var40 = var26 * var38 - var36 * var28;
            if (var40 <= 0.0 && var26 >= 0.0 && var36 <= 0.0) {
               double var62 = var26 / (var26 - var36);
               set(var7, var0.ax + var8 * var62, var0.ay + var10 * var62, var0.az + var12 * var62);
            } else {
               double var42 = var1 - var0.cx;
               double var44 = var3 - var0.cy;
               double var46 = var5 - var0.cz;
               double var48 = var8 * var42 + var10 * var44 + var12 * var46;
               double var50 = var14 * var42 + var16 * var44 + var18 * var46;
               if (var50 >= 0.0 && var48 <= var50) {
                  set(var7, var0.cx, var0.cy, var0.cz);
               } else {
                  double var52 = var48 * var28 - var26 * var50;
                  if (var52 <= 0.0 && var28 >= 0.0 && var50 <= 0.0) {
                     double var63 = var28 / (var28 - var50);
                     set(var7, var0.ax + var14 * var63, var0.ay + var16 * var63, var0.az + var18 * var63);
                  } else {
                     double var54 = var36 * var50 - var48 * var38;
                     if (var54 <= 0.0 && var38 - var36 >= 0.0 && var48 - var50 >= 0.0) {
                        double var64 = (var38 - var36) / (var38 - var36 + (var48 - var50));
                        set(var7, var0.bx + (var0.cx - var0.bx) * var64, var0.by + (var0.cy - var0.by) * var64, var0.bz + (var0.cz - var0.bz) * var64);
                     } else {
                        double var56 = 1.0 / (var54 + var52 + var40);
                        double var58 = var52 * var56;
                        double var60 = var40 * var56;
                        set(var7, var0.ax + var8 * var58 + var14 * var60, var0.ay + var10 * var58 + var16 * var60, var0.az + var12 * var58 + var18 * var60);
                     }
                  }
               }
            }
         }
      }
   }

   private static void set(double[] var0, double var1, double var3, double var5) {
      var0[0] = var1;
      var0[1] = var3;
      var0[2] = var5;
   }

   public record DugColumn(List<SkyDig.DugSection> sections) {
      public static final SkyDig.DugColumn EMPTY = new SkyDig.DugColumn(List.of());
      static final Codec<SkyDig.DugColumn> CODEC = SkyDig.DugSection.CODEC.listOf().xmap(SkyDig.DugColumn::new, SkyDig.DugColumn::sections);
      static final StreamCodec<ByteBuf, SkyDig.DugColumn> STREAM_CODEC = new StreamCodec<ByteBuf, SkyDig.DugColumn>() {
         public SkyDig.DugColumn decode(ByteBuf var1) {
            int var2 = ByteBufCodecs.VAR_INT.decode(var1);
            ArrayList var3 = new ArrayList(var2);

            for (int var4 = 0; var4 < var2; var4++) {
               int var5 = var1.readInt();
               int var6 = ByteBufCodecs.VAR_INT.decode(var1);
               long[] var7 = new long[64];

               for (int var8 = 0; var8 < 64; var8++) {
                  var7[var8] = var1.readLong();
               }

               var3.add(new SkyDig.DugSection(var5, var6, var7));
            }

            return new SkyDig.DugColumn(List.copyOf(var3));
         }

         public void encode(ByteBuf var1, SkyDig.DugColumn var2) {
            ByteBufCodecs.VAR_INT.encode(var1, var2.sections.size());

            for (SkyDig.DugSection var4 : var2.sections) {
               var1.writeInt(var4.world);
               ByteBufCodecs.VAR_INT.encode(var1, var4.sectionY);

               for (int var5 = 0; var5 < 64; var5++) {
                  var1.writeLong(var4.bits.length == 64 ? var4.bits[var5] : 0L);
               }
            }
         }
      };

      public long @Nullable [] bits(int var1, int var2) {
         for (SkyDig.DugSection var4 : this.sections) {
            if (var4.world == var1 && var4.sectionY == var2 && var4.bits.length == 64) {
               return var4.bits;
            }
         }

         return null;
      }

      public boolean isDug(int var1, int var2, int var3, int var4) {
         long[] var5 = this.bits(var1, var3 >> 4);
         int var6 = (var2 & 15) + 16 * (var4 & 15) + 256 * (var3 & 15);
         return var5 != null && (var5[var6 >> 6] >>> (var6 & 63) & 1L) != 0L;
      }

      SkyDig.DugColumn with(int var1, int var2, int var3, int var4) {
         int var5 = var3 >> 4;
         int var6 = (var2 & 15) + 16 * (var4 & 15) + 256 * (var3 & 15);
         ArrayList var7 = new ArrayList(this.sections.size() + 1);
         boolean var8 = false;

         for (SkyDig.DugSection var10 : this.sections) {
            if (var10.world == var1 && var10.sectionY == var5 && var10.bits.length == 64) {
               long[] var11 = (long[])var10.bits.clone();
               var11[var6 >> 6] = var11[var6 >> 6] | 1L << (var6 & 63);
               var7.add(new SkyDig.DugSection(var1, var5, var11));
               var8 = true;
            } else {
               var7.add(var10);
            }
         }

         if (!var8) {
            long[] var12 = new long[64];
            var12[var6 >> 6] = var12[var6 >> 6] | 1L << (var6 & 63);
            var7.add(new SkyDig.DugSection(var1, var5, var12));
         }

         return new SkyDig.DugColumn(List.copyOf(var7));
      }
   }

   public interface DugLookup {
      boolean isDug(int var1, int var2, int var3);
   }

   public record DugSection(int world, int sectionY, long[] bits) {
      static final Codec<SkyDig.DugSection> CODEC = RecordCodecBuilder.create(
         var0 -> var0.group(
               Codec.INT.fieldOf("world").forGetter(SkyDig.DugSection::world),
               Codec.INT.fieldOf("y").forGetter(SkyDig.DugSection::sectionY),
               Codec.LONG_STREAM.xmap(LongStream::toArray, Arrays::stream).fieldOf("bits").forGetter(SkyDig.DugSection::bits)
            )
            .apply(var0, SkyDig.DugSection::new)
      );
   }

   public static final class Probe {
      public final List<SkyTri> land = new ArrayList<>();
      public final List<SkyTri> tris = new ArrayList<>();
      private final List<SkyTri> gathered = new ArrayList<>();
      private final double[] q = new double[3];
      public @Nullable SkyTri surface;
      public double depth;
      private double boxMinX;
      private double boxMinY;
      private double boxMinZ;
      private double boxMaxX;
      private double boxMaxY;
      private double boxMaxZ;
      private @Nullable List<SkyTri> column;
      private final Long2ObjectOpenHashMap<List<SkyTri>> columns = new Long2ObjectOpenHashMap();
      private final Long2ObjectOpenHashMap<List<SkyTri>> nearCells = new Long2ObjectOpenHashMap();

      public SkyDig.Probe around(double var1, double var3, double var5, double var7, double var9, double var11) {
         this.gathered.clear();
         this.land.clear();
         this.tris.clear();
         this.column = null;
         this.columns.clear();
         this.nearCells.clear();
         this.boxMinX = var1;
         this.boxMinY = var3;
         this.boxMinZ = var5;
         this.boxMaxX = var7;
         this.boxMaxY = var9;
         this.boxMaxZ = var11;
         SkyCollision.originalSurfacesNear(new AABB(var1 - 2.5, var3 - 2.5, var5 - 2.5, var7 + 2.5, var9 + 2.5 + 16.0, var11 + 2.5), this.gathered);

         for (SkyTri var14 : this.gathered) {
            if (!var14.stairHelper) {
               if (var14.terrain) {
                  this.land.add(var14);
               } else if (var14.maxY >= var3 - 2.5 && var14.minY <= var9 + 2.5) {
                  this.tris.add(var14);
               }
            }
         }

         return this;
      }

      public double landHeight(double var1, double var3) {
         double var5 = Double.NaN;

         for (SkyTri var8 : this.land) {
            double var9 = var8.heightAt(var1, var3);
            if (!Double.isNaN(var9) && (Double.isNaN(var5) || var9 > var5)) {
               var5 = var9;
               this.surface = var8;
            }
         }

         return var5;
      }

      public int test(double var1, double var3, double var5) {
         double var7 = this.landHeight(var1, var5);
         SkyTri var9 = this.surface;
         if (!Double.isNaN(var7) && var3 < var7) {
            this.surface = var9;
            this.depth = var7 - var3;
            return var9.material == 0 ? 3 : var9.material;
         }

         SkyTri var10 = null;
         double var11 = 6.25;
         double var13 = 0.0;
         double var15 = 0.0;
         double var17 = 0.0;

         for (SkyTri var20 : this.nearCell(var1, var3, var5)) {
            double var21 = Math.max(0.0, Math.max(var20.minX - var1, var1 - var20.maxX));
            double var23 = Math.max(0.0, Math.max(var20.minY - var3, var3 - var20.maxY));
            double var25 = Math.max(0.0, Math.max(var20.minZ - var5, var5 - var20.maxZ));
            if (!(var21 * var21 + var23 * var23 + var25 * var25 >= var11)) {
               SkyDig.closestPoint(var20, var1, var3, var5, this.q);
               double var27 = var1 - this.q[0];
               double var29 = var3 - this.q[1];
               double var31 = var5 - this.q[2];
               double var33 = var27 * var27 + var29 * var29 + var31 * var31;
               if (var33 < var11) {
                  var11 = var33;
                  var10 = var20;
                  var13 = this.q[0];
                  var15 = this.q[1];
                  var17 = this.q[2];
               }
            }
         }

         this.surface = var10;
         if (var10 == null) {
            this.depth = Double.POSITIVE_INFINITY;
            return this.farFromSurfaces(var1, var3, var5, var7);
         }

         if ((var1 - var13) * var10.nx + (var3 - var15) * var10.ny + (var5 - var17) * var10.nz >= 0.0) {
            return 0;
         }

         if (!var10.diggable) {
            return -1;
         }

         this.depth = Math.sqrt(var11);
         return var10.material == 0 ? 3 : var10.material;
      }

      private int farFromSurfaces(double var1, double var3, double var5, double var7) {
         boolean var9 = !Double.isNaN(var7);
         SkyTri var10 = null;
         double var11 = Double.POSITIVE_INFINITY;

         for (SkyTri var14 : this.columnAt(var1, var5)) {
            double var15 = var14.heightAt(var1, var5);
            if (!Double.isNaN(var15)) {
               if (var15 > var3) {
                  if (var15 < var11) {
                     var11 = var15;
                     var10 = var14;
                  }
               } else if (var14.terrain) {
                  var9 = true;
               }
            }
         }

         if (var10 != null && var10.ny > 0.0) {
            this.surface = var10;
            this.depth = var11 - var3;
            if (!var10.diggable) {
               return -1;
            } else {
               return var10.material == 0 ? 3 : var10.material;
            }
         } else {
            return var10 == null && !var9 ? 3 : 0;
         }
      }

      private List<SkyTri> nearCell(double var1, double var3, double var5) {
         int var7 = (int)Math.floor(var1);
         int var8 = (int)Math.floor(var3);
         int var9 = (int)Math.floor(var5);
         long var10 = BlockPos.asLong(var7, var8, var9);
         List var12 = (List)this.nearCells.get(var10);
         if (var12 == null) {
            var12 = new ArrayList();

            for (SkyTri var14 : this.tris) {
               if (var14.maxX >= var7 - 2.5
                  && var14.minX <= var7 + 1 + 2.5
                  && var14.maxY >= var8 - 2.5
                  && var14.minY <= var8 + 1 + 2.5
                  && var14.maxZ >= var9 - 2.5
                  && var14.minZ <= var9 + 1 + 2.5) {
                  var12.add(var14);
               }
            }

            this.nearCells.put(var10, var12);
         }

         return var12;
      }

      private List<SkyTri> columnAt(double var1, double var3) {
         if (this.column == null) {
            ArrayList var5 = new ArrayList();
            SkyCollision.originalSurfacesNear(
               new AABB(
                  Math.min(var1, this.boxMinX),
                  this.boxMinY - 96.0,
                  Math.min(var3, this.boxMinZ),
                  Math.max(var1, this.boxMaxX),
                  this.boxMaxY + 96.0,
                  Math.max(var3, this.boxMaxZ)
               ),
               var5
            );
            this.column = new ArrayList<>();

            for (SkyTri var7 : var5) {
               if (!var7.stairHelper && Math.abs(var7.ny) >= 0.05) {
                  this.column.add(var7);
               }
            }
         }

         int var12 = (int)Math.floor(var1);
         int var13 = (int)Math.floor(var3);
         long var14 = (long)var12 << 32 | var13 & 4294967295L;
         List var9 = (List)this.columns.get(var14);
         if (var9 == null) {
            var9 = new ArrayList();

            for (SkyTri var11 : this.column) {
               if (var11.maxX >= var12 && var11.minX <= var12 + 1 && var11.maxZ >= var13 && var11.minZ <= var13 + 1) {
                  var9.add(var11);
               }
            }

            this.columns.put(var14, var9);
         }

         return var9;
      }
   }
}
