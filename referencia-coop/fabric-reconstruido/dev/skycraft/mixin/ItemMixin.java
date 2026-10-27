package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.world.SkyClip;
import net.minecraft.world.item.Item;
import net.minecraft.world.level.ClipContext;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.BlockHitResult;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;

@Mixin(Item.class)
public abstract class ItemMixin {
   @WrapOperation(
      method = "getPlayerPOVHitResult",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/Level;clip(Lnet/minecraft/world/level/ClipContext;)Lnet/minecraft/world/phys/BlockHitResult;"
      )
   )
   private static BlockHitResult skycraft$povSkyrim(Level var0, ClipContext var1, Operation<BlockHitResult> var2) {
      return SkyClip.refine(var1.getFrom(), var1.getTo(), (BlockHitResult)var2.call(new Object[]{var0, var1}), SkyClip.Use.PROJECTILE);
   }
}
