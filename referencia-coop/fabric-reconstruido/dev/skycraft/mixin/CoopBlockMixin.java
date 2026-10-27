package dev.skycraft.mixin;

import dev.skycraft.coop.CoopBlocks;
import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayerGameMode;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.context.BlockPlaceContext;
import net.minecraft.world.level.block.Blocks;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(BlockItem.class)
public abstract class CoopBlockMixin {
   @Inject(method = "place", at = @At("RETURN"))
   private void skycraft$placed(BlockPlaceContext var1, CallbackInfoReturnable<InteractionResult> var2) {
      if (((InteractionResult)var2.getReturnValue()).consumesAction() && !var1.getLevel().isClientSide()) {
         BlockPos var3 = var1.getClickedPos();
         CoopBlocks.report(var1.getLevel(), var3, var1.getLevel().getBlockState(var3));
      }
   }

   @Mixin(ServerPlayerGameMode.class)
   public abstract static class Destroy {
      @Shadow
      protected ServerLevel level;

      @Inject(method = "destroyBlock", at = @At("RETURN"))
      private void skycraft$broken(BlockPos var1, CallbackInfoReturnable<Boolean> var2) {
         if (var2.getReturnValueZ()) {
            CoopBlocks.report(this.level, var1, Blocks.AIR.defaultBlockState());
         }
      }
   }
}
