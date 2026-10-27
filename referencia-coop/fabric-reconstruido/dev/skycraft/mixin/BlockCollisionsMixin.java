package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.SkyDig;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.BlockCollisions;
import net.minecraft.world.level.CollisionGetter;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.EntityCollisionContext;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;

@Mixin(BlockCollisions.class)
public abstract class BlockCollisionsMixin {
   @WrapOperation(
      method = "computeNext",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/phys/shapes/CollisionContext;getCollisionShape(Lnet/minecraft/world/level/block/state/BlockState;Lnet/minecraft/world/level/CollisionGetter;Lnet/minecraft/core/BlockPos;)Lnet/minecraft/world/phys/shapes/VoxelShape;"
      )
   )
   private VoxelShape skycraft$addSkyrimShape(CollisionContext var1, BlockState var2, CollisionGetter var3, BlockPos var4, Operation<VoxelShape> var5) {
      VoxelShape var6 = (VoxelShape)var5.call(new Object[]{var1, var2, var3, var4});
      if (var2.isAir()) {
         VoxelShape var7 = SkyDig.wallShape(var3, var4);
         if (var7 != null) {
            var6 = var6.isEmpty() ? var7 : Shapes.or(var6, var7);
         }
      }

      if (var1 instanceof EntityCollisionContext var8 && SkyCollision.usesSmoothCollider(var8.getEntity())) {
         return var6;
      } else {
         VoxelShape var9 = SkyCollision.shapeAt(var4);
         if (var9 == null) {
            return var6;
         } else {
            return var6.isEmpty() ? var9 : Shapes.or(var6, var9);
         }
      }
   }
}
