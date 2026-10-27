package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.PressurePlateBlock;
import net.minecraft.world.level.block.WeightedPressurePlateBlock;
import net.minecraft.world.phys.AABB;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;

@Mixin({PressurePlateBlock.class, WeightedPressurePlateBlock.class})
public abstract class PressurePlateMixin {
   @Unique
   private static final AABB SKYCRAFT$TOUCH = new AABB(0.0625, 0.0, 0.0625, 0.9375, 1.0, 0.9375);

   @WrapOperation(
      method = "getSignalStrength",
      at = @At(value = "INVOKE", target = "Lnet/minecraft/world/phys/AABB;move(Lnet/minecraft/core/BlockPos;)Lnet/minecraft/world/phys/AABB;")
   )
   private AABB skycraft$wholeBlock(AABB var1, BlockPos var2, Operation<AABB> var3) {
      return (AABB)var3.call(new Object[]{SKYCRAFT$TOUCH, var2});
   }
}
