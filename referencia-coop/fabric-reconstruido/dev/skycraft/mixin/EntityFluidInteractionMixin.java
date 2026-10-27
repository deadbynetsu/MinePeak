package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.world.SkyWater;
import net.minecraft.core.BlockPos;
import net.minecraft.world.entity.EntityFluidInteraction;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.material.FluidState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;

@Mixin(EntityFluidInteraction.class)
public abstract class EntityFluidInteractionMixin {
   @WrapOperation(
      method = "update",
      at = @At(value = "INVOKE", target = "Lnet/minecraft/world/entity/EntityFluidInteraction;hasFluidAndLoaded(Lnet/minecraft/world/level/Level;IIIIII)Z")
   )
   private static boolean skycraft$skyrimWaterNearby(Level var0, int var1, int var2, int var3, int var4, int var5, int var6, Operation<Boolean> var7) {
      return (Boolean)var7.call(new Object[]{var0, var1, var2, var3, var4, var5, var6}) || SkyWater.anyIn(var1, var2, var3, var4, var5, var6);
   }

   @WrapOperation(
      method = "update",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/BlockGetter;getFluidState(Lnet/minecraft/core/BlockPos;)Lnet/minecraft/world/level/material/FluidState;"
      )
   )
   private FluidState skycraft$skyrimWater(BlockGetter var1, BlockPos var2, Operation<FluidState> var3) {
      FluidState var4 = (FluidState)var3.call(new Object[]{var1, var2});
      if (var4.isEmpty() && SkyWater.active()) {
         FluidState var5 = SkyWater.fluidAt(var1, var2);
         if (var5 != null) {
            return var5;
         }
      }

      return var4;
   }

   @WrapOperation(
      method = "update",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/material/FluidState;getHeight(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;)F"
      )
   )
   private float skycraft$skyrimWaterHeight(FluidState var1, BlockGetter var2, BlockPos var3, Operation<Float> var4) {
      float var5 = SkyWater.active() ? SkyWater.substitutedHeight(var2, var3) : -1.0F;
      return var5 >= 0.0F ? var5 : (Float)var4.call(new Object[]{var1, var2, var3});
   }

   @WrapOperation(
      method = "update",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/material/FluidState;getHeightForCamera(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;)F"
      )
   )
   private float skycraft$skyrimWaterEyeHeight(FluidState var1, BlockGetter var2, BlockPos var3, Operation<Float> var4) {
      float var5 = SkyWater.active() ? SkyWater.substitutedHeight(var2, var3) : -1.0F;
      return var5 >= 0.0F ? var5 : (Float)var4.call(new Object[]{var1, var2, var3});
   }
}
