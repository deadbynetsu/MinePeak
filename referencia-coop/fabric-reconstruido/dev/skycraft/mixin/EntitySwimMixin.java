package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.world.SkyWater;
import net.minecraft.core.BlockPos;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.material.FluidState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;

@Mixin(Entity.class)
public abstract class EntitySwimMixin {
   @WrapOperation(
      method = "updateSwimming",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/Level;getFluidState(Lnet/minecraft/core/BlockPos;)Lnet/minecraft/world/level/material/FluidState;"
      )
   )
   private FluidState skycraft$swimInSkyrimWater(Level var1, BlockPos var2, Operation<FluidState> var3) {
      FluidState var4 = (FluidState)var3.call(new Object[]{var1, var2});
      if (var4.isEmpty() && SkyWater.active()) {
         FluidState var5 = SkyWater.fluidAt(var1, var2);
         if (var5 != null) {
            return var5;
         }
      }

      return var4;
   }
}
