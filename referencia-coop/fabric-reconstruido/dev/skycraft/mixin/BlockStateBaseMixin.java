package dev.skycraft.mixin;

import dev.skycraft.world.SkyCollision;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.block.SupportType;
import net.minecraft.world.level.block.state.BlockBehaviour.BlockStateBase;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(BlockStateBase.class)
public abstract class BlockStateBaseMixin {
   @Inject(
      method = "isFaceSturdy(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;Lnet/minecraft/core/Direction;Lnet/minecraft/world/level/block/SupportType;)Z",
      at = @At("HEAD"),
      cancellable = true
   )
   private void skycraft$skyrimIsSturdy(BlockGetter var1, BlockPos var2, Direction var3, SupportType var4, CallbackInfoReturnable<Boolean> var5) {
      if (((BlockStateBase)this).isAir()) {
         boolean var6 = var3 == Direction.UP ? SkyCollision.supportsFromBelow(var2.above()) : SkyCollision.solidFraction(var2) >= 0.4F;
         if (var6) {
            var5.setReturnValue(true);
         }
      }
   }
}
