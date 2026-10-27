package dev.skycraft.mixin;

import dev.skycraft.world.SkyCollision;
import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.util.RandomSource;
import net.minecraft.world.level.block.FallingBlock;
import net.minecraft.world.level.block.state.BlockState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(FallingBlock.class)
public abstract class FallingBlockMixin {
   @Inject(method = "tick", at = @At("HEAD"), cancellable = true)
   private void skycraft$restOnSkyrim(BlockState var1, ServerLevel var2, BlockPos var3, RandomSource var4, CallbackInfo var5) {
      if (SkyCollision.supportsFromBelow(var3)) {
         var5.cancel();
      }
   }
}
