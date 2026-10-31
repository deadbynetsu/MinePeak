package dev.skycraft.client.mixin;

import dev.skycraft.client.collision.WallPlaneAnchor;
import dev.skycraft.world.SkyTri;
import dev.skycraft.world.TriCollider;
import java.util.List;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Redirect;

/**
 * The upstream wall response projects a body-height slice of each triangle into XZ.
 * Its zero-distance case must compare the previous body to that same plane slice,
 * rather than to an arbitrary original vertex at a completely different height.
 * These are the only reads of ax/az in pushOutOfWalls; floor/ceiling data is untouched.
 */
@Mixin(value = TriCollider.class, remap = false)
abstract class SlopedWallSideMixin {
    @Redirect(method = "pushOutOfWalls", at = @At(value = "FIELD", target = "Ldev/skycraft/world/SkyTri;ax:D"))
    private static double peakcraft$wallAnchorX(SkyTri triangle, List<SkyTri> triangles,
            double x, double feetY, double z, double radius, double height,
            double clearance, double maxStep, double previousX, double previousZ) {
        return WallPlaneAnchor.coordinate(triangle.ax, triangle.ay, triangle.nx, triangle.ny,
                triangle.nx * triangle.nx + triangle.nz * triangle.nz, feetY + height * .5);
    }

    @Redirect(method = "pushOutOfWalls", at = @At(value = "FIELD", target = "Ldev/skycraft/world/SkyTri;az:D"))
    private static double peakcraft$wallAnchorZ(SkyTri triangle, List<SkyTri> triangles,
            double x, double feetY, double z, double radius, double height,
            double clearance, double maxStep, double previousX, double previousZ) {
        return WallPlaneAnchor.coordinate(triangle.az, triangle.ay, triangle.nz, triangle.ny,
                triangle.nx * triangle.nx + triangle.nz * triangle.nz, feetY + height * .5);
    }
}
