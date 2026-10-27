package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.link.SkyLink;
import dev.skycraft.world.SkyDigBlast;
import java.util.List;
import java.util.Optional;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Explosion;
import net.minecraft.world.level.ExplosionDamageCalculator;
import net.minecraft.world.level.ServerExplosion;
import net.minecraft.world.level.Explosion.BlockInteraction;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.material.FluidState;
import net.minecraft.world.phys.Vec3;
import org.jspecify.annotations.Nullable;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.ModifyVariable;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(ServerExplosion.class)
public abstract class ServerExplosionMixin {
   @Unique
   private @Nullable SkyDigBlast skycraft$blast;

   @Inject(method = "explode", at = @At("HEAD"))
   private void skycraft$begin(CallbackInfoReturnable<Integer> var1) {
      this.skycraft$blast = SkyDigBlast.begin((ServerExplosion)this);
   }

   @WrapOperation(
      method = "calculateExplodedPositions",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/ExplosionDamageCalculator;getBlockExplosionResistance(Lnet/minecraft/world/level/Explosion;Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;Lnet/minecraft/world/level/block/state/BlockState;Lnet/minecraft/world/level/material/FluidState;)Ljava/util/Optional;"
      )
   )
   private Optional<Float> skycraft$skyrimResists(
      ExplosionDamageCalculator var1, Explosion var2, BlockGetter var3, BlockPos var4, BlockState var5, FluidState var6, Operation<Optional<Float>> var7
   ) {
      Optional var8 = (Optional)var7.call(new Object[]{var1, var2, var3, var4, var5, var6});
      return this.skycraft$blast != null ? this.skycraft$blast.resistance(var4, var8) : var8;
   }

   @ModifyVariable(method = "explode", at = @At("STORE"), ordinal = 0)
   private List<BlockPos> skycraft$skyrimBreaks(List<BlockPos> var1) {
      if (this.skycraft$blast != null) {
         ServerExplosion var2 = (ServerExplosion)this;
         this.skycraft$blast
            .materialize(var1, var2.getBlockInteraction() != BlockInteraction.KEEP && var2.getBlockInteraction() != BlockInteraction.TRIGGER_BLOCK);
      }

      return var1;
   }

   @Inject(method = "explode", at = @At("RETURN"))
   private void skycraft$tellSkyrim(CallbackInfoReturnable<Integer> var1) {
      if (this.skycraft$blast != null) {
         this.skycraft$blast.finish();
         this.skycraft$blast = null;
      }

      if (SkyLink.active()) {
         ServerExplosion var2 = (ServerExplosion)this;
         Vec3 var3 = var2.center();
         SkyLink.pushEvent(3, 0, (float)var3.x, (float)var3.y, (float)var3.z, var2.radius(), 0);
      }
   }
}
