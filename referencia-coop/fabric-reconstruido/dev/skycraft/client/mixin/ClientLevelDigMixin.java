package dev.skycraft.client.mixin;

import dev.skycraft.client.SkyDigClient;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.state.BlockState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ClientLevel.class)
public abstract class ClientLevelDigMixin {
   @Inject(method = "setBlocksDirty", at = @At("HEAD"))
   private void skycraft$blockChanged(BlockPos var1, BlockState var2, BlockState var3, CallbackInfo var4) {
      SkyDigClient.blockChanged((ClientLevel)this, var1, var2, var3);
   }
}
