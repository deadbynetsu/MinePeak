package dev.skycraft.mixin;

import dev.skycraft.SkyCraft;
import dev.skycraft.world.SkyCollision;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.tags.FluidTags;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.material.FlowingFluid;
import net.minecraft.world.level.material.FluidState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(FlowingFluid.class)
public abstract class FlowingFluidMixin {
   private static final float FALLING_SURFACE = 0.8888889F;
   private static final float MARGIN = 0.05F;
   private static long skycraft$lastLog;
   private static int skycraft$logged;

   @Inject(method = "canPassThroughWall", at = @At("HEAD"), cancellable = true)
   private static void skycraft$skyrimWall(
      Direction var0, BlockGetter var1, BlockPos var2, BlockState var3, BlockPos var4, BlockState var5, CallbackInfoReturnable<Boolean> var6
   ) {
      if (var5.isAir() && SkyCollision.active() && var0 != Direction.UP) {
         if (!SkyCollision.isKnown(var4.getX(), var4.getY(), var4.getZ())) {
            refused("unknown region", var0, var2, var3, var4, 0.0F);
            var6.setReturnValue(false);
         } else if (var0 == Direction.DOWN) {
            if (SkyCollision.hasGeometry(var2)) {
               refused("resting on ground", var0, var2, var3, var4, SkyCollision.groundTop(var2));
               var6.setReturnValue(false);
            } else {
               float var11 = SkyCollision.groundTop(var4);
               if (var11 >= 0.8388889F) {
                  refused("ground below", var0, var2, var3, var4, var11);
                  var6.setReturnValue(false);
               }
            }
         } else if (!SkyCollision.hasGeometry(var4)) {
            if (SkyCollision.hasGeometry(var4.above())) {
               refused("under ground", var0, var2, var3, var4, 1.0F);
               var6.setReturnValue(false);
            }
         } else {
            FluidState var7 = var3.getFluidState();
            float var8;
            if (var7.isEmpty()) {
               var8 = 0.8F;
            } else if (var7.getValue(FlowingFluid.FALLING)) {
               var8 = 0.7777778F;
            } else {
               int var9 = var7.is(FluidTags.LAVA) ? 2 : 1;
               var8 = Math.max(0, var7.getAmount() - var9) / 9.0F;
            }

            float var12 = SkyCollision.groundTop(var4);
            boolean var10 = var12 <= SkyCollision.groundTop(var2) + 0.13F;
            if (!var10 && var12 >= var8 - 0.05F) {
               refused("ground beside", var0, var2, var3, var4, var12);
               var6.setReturnValue(false);
            }
         }
      }
   }

   private static void refused(String var0, Direction var1, BlockPos var2, BlockState var3, BlockPos var4, float var5) {
      long var6 = System.currentTimeMillis();
      if (var6 - skycraft$lastLog > 1000L) {
         skycraft$lastLog = var6;
         skycraft$logged = 0;
      }

      if (skycraft$logged++ < 4) {
         FluidState var8 = var3.getFluidState();
         SkyCraft.LOG
            .info(
               "SkyCraft: fluid flow refused ({}): {} -> {} going {}, fluid {} amount {}, Skyrim ground height there {}",
               new Object[]{var0, var2.toShortString(), var4.toShortString(), var1, var8.getType(), var8.getAmount(), String.format("%.2f", var5)}
            );
      }
   }
}
