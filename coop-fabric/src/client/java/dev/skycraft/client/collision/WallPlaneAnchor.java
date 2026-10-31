package dev.skycraft.client.collision;

/** A horizontal point on a triangle plane at the previous body's centre height. */
public final class WallPlaneAnchor {
    private WallPlaneAnchor() { }

    public static double coordinate(double vertex, double vertexY, double normal,
                                    double normalY, double horizontalNormalSquared, double bodyY) {
        if (horizontalNormalSquared < 1.0e-12) return vertex;
        return vertex - normal * normalY * (bodyY - vertexY) / horizontalNormalSquared;
    }
}
